using System.Diagnostics;
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
            () => UpdateBigQueryPolicyTagsAsync(configuration, hiddenPolicyTagName, progressReporter, cancellationToken),
            progressReporter,
            "Updating BigQuery policy tags");

        await WithProgressReportingAsync(
            () => ApplyAirbyteConfigurationAsync(configuration, airbyteConnectionId, progressReporter, cancellationToken),
            progressReporter,
            "Applying Airbyte configuration");
    }

    // internal for testing
    internal async Task ApplyAirbyteConfigurationAsync(
        DatabaseSyncConfiguration configuration,
        string connectionId,
        IProgressReporter progressReporter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);

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
            var bqTable = await bigQueryClient.GetTableAsync(projectId, datasetId, tableId, cancellationToken: cancellationToken);
            var schema = bqTable.Schema;

            var schemaChanged = false;

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
                schemaChanged |= policyTagNamesChanged;
            }

            if (schemaChanged)
            {
                await bigQueryClient.PatchTableAsync(projectId, datasetId, tableId, bqTable.Resource, cancellationToken: cancellationToken);
                return "[schema updated]";
            }
            else
            {
                return "[no changes required]";
            }
        }
    }

    private static Task WithProgressReportingAsync(Func<Task> action, IProgressReporter progressReporter, string messagePrefix) =>
        WithProgressReportingAsync(
            async () =>
            {
                await action();
                return null;
            },
            progressReporter,
            messagePrefix);

    private static async Task WithProgressReportingAsync(Func<Task<string?>> action, IProgressReporter progressReporter, string messagePrefix)
    {
#pragma warning disable CA1849
        var sw = Stopwatch.StartNew();

        progressReporter.WriteLine($"{messagePrefix}... ");

        try
        {
            var completedMessage = await action();
            progressReporter.WriteLine($"{messagePrefix} {("DONE " + completedMessage).TrimEnd()} in {GetDurationString()}");
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
}
