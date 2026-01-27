using System.Diagnostics;
using System.Text.Json.Nodes;
using Dfe.Analytics.EFCore.AirbyteApi;
using Dfe.Analytics.EFCore.Configuration;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.EFCore;

public class AnalyticsDeployer(
    AirbyteApiClient airbyteApiClient,
    IOptions<DfeAnalyticsOptions> optionsAccessor)
{
    private static readonly string[] _airbyteFieldNames = ["_ab_cdc_lsn", "_ab_cdc_deleted_at", "_ab_cdc_updated_at"];

    public async Task DeployAsync(
        DatabaseSyncConfiguration configuration,
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
            () => ApplyAirbyteConfigurationAsync(configuration, airbyteConnectionId, progressReporter, cancellationToken),
            progressReporter,
            "Applying Airbyte configuration");

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
                    Streams = configuration.Tables.Select(t => new UpdateConnectionDetailsRequestConfigurationStream
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
                }
            ]
        };

        await airbyteApiClient.UpdateConnectionDetailsAsync(connectionId, updateRequest, cancellationToken);
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
        catch (Exception e)
        {
            progressReporter.WriteLine($"{messagePrefix} FAILED in {GetDurationString()}");
            progressReporter.WriteLine(string.Empty);
            progressReporter.WriteLine(e.ToString());
            throw;
        }

        string GetDurationString() => $"{Math.Round(sw.Elapsed.TotalSeconds, MidpointRounding.AwayFromZero)}s";
#pragma warning restore CA1849
    }

    private record CompletionWithProgressMessage(string? Message);

    private record CompletionWithProgressMessage<T>(T Result, string? Message);
}
