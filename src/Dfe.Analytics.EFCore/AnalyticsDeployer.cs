using System.Diagnostics;
using System.Text.Json.Nodes;
using Dfe.Analytics.EFCore.AirbyteApi;
using Dfe.Analytics.EFCore.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.EFCore;

public class AnalyticsDeployer(
    AirbyteApiClient airbyteApiClient,
    IOptions<DfeAnalyticsOptions> optionsAccessor)
{
    private static readonly string[] _airbyteFieldNames = ["_ab_cdc_lsn", "_ab_cdc_deleted_at", "_ab_cdc_updated_at"];

    public async Task DeployAsync(
        DatabaseSyncConfiguration configuration,
        DbContext dbContext,
        string airbyteConnectionId,
        string hiddenPolicyTagName,
        IProgressReporter? progressReporter = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(airbyteConnectionId);
        ArgumentNullException.ThrowIfNull(hiddenPolicyTagName);

        progressReporter ??= new ConsoleProgressReporter();

        await WithProgressReportingAsync(
            () => WaitForMigrationsAsync(dbContext, cancellationToken),
            progressReporter,
            "Waiting for migrations to be applied");

        await WithProgressReportingAsync(
            () => ApplyAirbyteConfigurationAsync(configuration, airbyteConnectionId, progressReporter, cancellationToken),
            progressReporter,
            "Applying Airbyte configuration");

        await WithProgressReportingAsync(
            () => CompleteAirbyteSyncAsync(airbyteConnectionId, progressReporter, cancellationToken),
            progressReporter,
            "Waiting for sync to complete");

        await WithProgressReportingAsync(
            () => UpdateBigQueryPolicyTagsAsync(configuration, hiddenPolicyTagName, progressReporter, cancellationToken),
            progressReporter,
            "Updating BigQuery policy tags");
    }

    // internal for testing
    internal async Task ApplyAirbyteConfigurationAsync(
        DatabaseSyncConfiguration configuration,
        string connectionId,
        IProgressReporter progressReporter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);

        // Retrieve connection details to get source ID
        var connectionResponse = await WithProgressReportingAsync(
            () => airbyteApiClient.ConfigurationApiPostAsync(
                "/api/v1/connections/get",
                new JsonObject
                {
                    ["connectionId"] = connectionId
                },
                cancellationToken),
            progressReporter,
            "  Retrieving Airbyte connection details");

        var sourceId = connectionResponse["sourceId"]?.GetValue<string>() ??
            throw new InvalidOperationException("'sourceId' is missing from '/v1/connections/get' response.");

        // Trigger schema discovery to ensure Airbyte has the latest schema
        await WithProgressReportingAsync(
            () => airbyteApiClient.ConfigurationApiPostAsync(
                "/api/v1/sources/discover_schema",
                new JsonObject
                {
                    ["sourceId"] = sourceId,
                    ["disable_cache"] = true,
                    ["priority"] = "high"
                },
                cancellationToken),
            progressReporter,
            "  Discovering source schema");

        // Update connection with new streams configuration
        var updateRequest = new UpdateConnectionDetailsRequest
        {
            Configurations =
            [
                new UpdateConnectionDetailsRequestConfiguration
                {
                    Streams = configuration.Tables
                        .Select(t => new UpdateConnectionDetailsRequestConfigurationStream
                        {
                            Name = t.Name,
                            SyncMode = "incremental_append",
                            CursorField = ["_ab_cdc_lsn"],
                            PrimaryKey = [t.PrimaryKey.ColumnNames],
                            SelectedFields = _airbyteFieldNames.Concat(t.Columns.Select(c => c.Name))
                                .Select(n => new UpdateConnectionDetailsRequestConfigurationStreamField
                                {
                                    FieldPath = [n]
                                })
                        })
                        .Append(new UpdateConnectionDetailsRequestConfigurationStream
                        {
                            Name = "airbyte_heartbeat",
                            SyncMode = "full_refresh_overwrite",
                            CursorField = [],
                            PrimaryKey = [["id"]],
                            SelectedFields = [
                                new UpdateConnectionDetailsRequestConfigurationStreamField
                                {
                                    FieldPath = ["id"]
                                },
                                new UpdateConnectionDetailsRequestConfigurationStreamField
                                {
                                    FieldPath = ["last_heartbeat"]
                                }
                            ]
                        })
                }
            ]
        };

        await airbyteApiClient.UpdateConnectionDetailsAsync(connectionId, updateRequest, cancellationToken);
    }

    // internal for testing
    internal async Task CompleteAirbyteSyncAsync(
        string connectionId,
        IProgressReporter progressReporter,
        CancellationToken cancellationToken = default)
    {
        var triggerJobResponse = await WithProgressReportingAsync(
            () => airbyteApiClient.TriggerJobAsync(
                new TriggerJobRequest
                {
                    ConnectionId = connectionId,
                    JobType = JobType.Sync
                },
                cancellationToken),
            progressReporter,
            "  Creating Airbyte sync job");

        var jobId = triggerJobResponse.JobId;

        await WithProgressReportingAsync(
            WaitForCompletionAsync,
            progressReporter,
            "  Waiting for job to complete");

        async Task WaitForCompletionAsync()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

            do
            {
                var jobStatusResponse = await airbyteApiClient.GetJobStatusAsync(jobId, cancellationToken);

                if (jobStatusResponse.Status is JobStatus.Succeeded)
                {
                    return;
                }

                if (jobStatusResponse.Status is not JobStatus.Pending and not JobStatus.Running)
                {
                    throw new InvalidOperationException($"Unexpected job status: '{jobStatusResponse.Status}'.");
                }
            }
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
    }

    // internal for testing
    internal async Task UpdateBigQueryPolicyTagsAsync(
        DatabaseSyncConfiguration configuration,
        string hiddenPolicyTagName,
        IProgressReporter progressReporter,
        CancellationToken cancellationToken = default)
    {
        var bigQueryClient = optionsAccessor.Value.BigQueryClient ?? throw new InvalidOperationException("BigQuery client is not configured.");
        var projectId = optionsAccessor.Value.ProjectId ?? throw new InvalidOperationException("BigQuery project ID is not configured.");
        var datasetId = optionsAccessor.Value.DatasetId ?? throw new InvalidOperationException("BigQuery dataset ID is not configured.");

        var allTableIds = new List<string>();
        await foreach (var table in bigQueryClient.ListTablesAsync(projectId, datasetId).WithCancellation(cancellationToken))
        {
            allTableIds.Add(table.Reference.TableId);
        }

        foreach (var table in configuration.Tables)
        {
            await WithProgressReportingAsync(
                () => ProcessTableAsync(table),
                progressReporter,
                $"  {table.Name}");
        }

        async Task<string?> ProcessTableAsync(TableSyncInfo table)
        {
            var tableId = table.Name;

            bool schemaUpdateRequired;

            if (!allTableIds.Contains(tableId))
            {
                schemaUpdateRequired = table.Columns.Any(c => c.Hidden);

                if (schemaUpdateRequired)
                {
                    throw new InvalidOperationException(
                        $"Table '{tableId}' not found in BigQuery dataset '{datasetId}'.");
                }

                return "[skipped - table does not exist]";
            }

            var bqTable = await bigQueryClient.GetTableAsync(projectId, datasetId, tableId, cancellationToken: cancellationToken);
            var schema = bqTable.Schema;

            schemaUpdateRequired = false;

            foreach (var column in table.Columns)
            {
                var bqField = schema.Fields.SingleOrDefault(f => f.Name == column.Name);

                if (bqField is null)
                {
                    throw new InvalidOperationException(
                        $"Column '{column.Name}' not found in BigQuery table '{tableId}'.");
                }

                var existingPolicyTagNames = new HashSet<string>(bqField.PolicyTags?.Names ?? []);

                bqField.PolicyTags ??= new();
                bqField.PolicyTags.Names = new List<string>();

                if (column.Hidden)
                {
                    bqField.PolicyTags.Names.Add(hiddenPolicyTagName);
                }

                var policyTagNamesChanged = !existingPolicyTagNames.SetEquals(bqField.PolicyTags.Names);
                schemaUpdateRequired |= policyTagNamesChanged;
            }

            if (schemaUpdateRequired)
            {
                await bigQueryClient.PatchTableAsync(projectId, datasetId, tableId, bqTable.Resource, cancellationToken: cancellationToken);
                return "[schema updated]";
            }
            else
            {
                return "[skipped - no changes required]";
            }
        }
    }

    internal async Task WaitForMigrationsAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));

        do
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);

            if (!pendingMigrations.Any())
            {
                return;
            }
        }
        while (await timer.WaitForNextTickAsync(cancellationToken));
    }

    private static Task WithProgressReportingAsync(Func<Task> action, IProgressReporter progressReporter, string messagePrefix) =>
        WithProgressReportingAsync(
            async () =>
            {
                await action();
                return new CompletionWithProgressMessage(null);
            },
            progressReporter,
            messagePrefix);

    private static Task<T> WithProgressReportingAsync<T>(Func<Task<T>> action, IProgressReporter progressReporter, string messagePrefix) =>
        WithProgressReportingAsync(
            async () =>
            {
                var result = await action();
                return new CompletionWithProgressMessage<T>(result, null);
            },
            progressReporter,
            messagePrefix);

    private static async Task WithProgressReportingAsync(
        Func<Task<CompletionWithProgressMessage>> action,
        IProgressReporter progressReporter,
        string messagePrefix)
    {
        await WithProgressReportingAsync(
            async () =>
            {
                var completion = await action();
                return new CompletionWithProgressMessage<bool>(true, completion.Message);
            },
            progressReporter,
            messagePrefix);
    }

    private static async Task<T> WithProgressReportingAsync<T>(
        Func<Task<CompletionWithProgressMessage<T>>> action,
        IProgressReporter progressReporter,
        string messagePrefix)
    {
#pragma warning disable CA1849
        var sw = Stopwatch.StartNew();

        progressReporter.WriteLine($"{messagePrefix}... ");

        try
        {
            var completion = await action();
            progressReporter.WriteLine($"{messagePrefix} {("DONE " + completion.Message).TrimEnd()} in {GetDurationString()}");
            return completion.Result;
        }
        catch (Exception)
        {
            progressReporter.WriteLine($"{messagePrefix} FAILED in {GetDurationString()}");
            throw;
        }

        string GetDurationString() => $"{Math.Round(sw.Elapsed.TotalSeconds, MidpointRounding.AwayFromZero)}s";
#pragma warning restore CA1849
    }

    private record CompletionWithProgressMessage(string? Message);

    private record CompletionWithProgressMessage<T>(T Result, string? Message);
}
