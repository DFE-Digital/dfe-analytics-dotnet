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
        TextWriter? logger = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(airbyteConnectionId);
        ArgumentNullException.ThrowIfNull(hiddenPolicyTagName);

        logger ??= Console.Out;

        await WithProgressLoggingAsync(
            () => UpdateBigQueryPolicyTagsAsync(configuration, hiddenPolicyTagName, logger, cancellationToken),
            logger,
            "Updating BigQuery policy tags");

        await WithProgressLoggingAsync(
            () => ApplyAirbyteConfigurationAsync(configuration, airbyteConnectionId, logger, cancellationToken),
            logger,
            "Applying Airbyte configuration");
    }

    private async Task ApplyAirbyteConfigurationAsync(
        DatabaseSyncConfiguration configuration,
        string connectionId,
        TextWriter logger,
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

    private async Task UpdateBigQueryPolicyTagsAsync(
        DatabaseSyncConfiguration configuration,
        string hiddenPolicyTagName,
        TextWriter logger,
        CancellationToken cancellationToken = default)
    {
        var bigQueryClient = optionsAccessor.Value.BigQueryClient ?? throw new InvalidOperationException("BigQuery client is not configured.");
        var projectId = optionsAccessor.Value.ProjectId ?? throw new InvalidOperationException("BigQuery project ID is not configured.");
        var datasetId = optionsAccessor.Value.DatasetId ?? throw new InvalidOperationException("BigQuery dataset ID is not configured.");

        foreach (var table in configuration.Tables)
        {
            await WithProgressLoggingAsync(
                () => ProcessTableAsync(table),
                logger,
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

    private static Task WithProgressLoggingAsync(Func<Task> action, TextWriter logger, string messagePrefix) =>
        WithProgressLoggingAsync(
            async () =>
            {
                await action();
                return null;
            },
            logger,
            messagePrefix);

    private static async Task WithProgressLoggingAsync(Func<Task<string?>> action, TextWriter logger, string messagePrefix)
    {
#pragma warning disable CA1849
        var sw = Stopwatch.StartNew();

        logger.WriteLine($"{messagePrefix}... ");

        try
        {
            var completedMessage = await action();
            logger.WriteLine($"{messagePrefix} {("DONE " + completedMessage).TrimEnd()} in {GetDurationString()}");
        }
        catch (Exception e)
        {
            logger.WriteLine($"{messagePrefix} FAILED in {GetDurationString()}");
            logger.WriteLine();
            logger.WriteLine(e.ToString());
            throw;
        }

        string GetDurationString() => $"{Math.Round(sw.Elapsed.TotalSeconds, MidpointRounding.AwayFromZero)}s";

#pragma warning restore CA1849
    }
}
