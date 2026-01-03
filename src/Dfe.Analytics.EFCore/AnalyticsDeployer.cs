using System.Data;
using System.Diagnostics;
using Dfe.Analytics.EFCore.AirbyteApi;
using Dfe.Analytics.EFCore.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

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
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(airbyteConnectionId);
        ArgumentNullException.ThrowIfNull(hiddenPolicyTagName);

        await ApplyBigQueryPolicyTagsAsync(
            configuration,
            hiddenPolicyTagName,
            cancellationToken);

        await SetAirbyteConfigurationAsync(
            configuration,
            airbyteConnectionId,
            cancellationToken);
    }

    private async Task SetAirbyteConfigurationAsync(
        DatabaseSyncConfiguration configuration,
        string connectionId,
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

    private async Task ApplyBigQueryPolicyTagsAsync(
        DatabaseSyncConfiguration configuration,
        string hiddenPolicyTagName,
        CancellationToken cancellationToken = default)
    {
        var bigQueryClient = optionsAccessor.Value.BigQueryClient ?? throw new InvalidOperationException("BigQuery client is not configured.");
        var projectId = optionsAccessor.Value.ProjectId ?? throw new InvalidOperationException("BigQuery project ID is not configured.");
        var datasetId = optionsAccessor.Value.DatasetId ?? throw new InvalidOperationException("BigQuery dataset ID is not configured.");

        foreach (var table in configuration.Tables)
        {
            var tableId = table.Name;
            var bqTable = await bigQueryClient.GetTableAsync(projectId, datasetId, tableId, cancellationToken: cancellationToken);
            var schema = bqTable.Schema;

            foreach (var column in table.Columns)
            {
                var bqField = schema.Fields.SingleOrDefault(f => f.Name == column.Name);

                if (bqField is null)
                {
                    throw new InvalidOperationException(
                        $"Column '{column.Name}' not found in BigQuery table '{tableId}'.");
                }

                bqField.PolicyTags ??= new();
                bqField.PolicyTags.Names = new List<string>();

                if (column.IsPii)
                {
                    bqField.PolicyTags.Names.Add(hiddenPolicyTagName);
                }
            }

            await bigQueryClient.PatchTableAsync(projectId, datasetId, tableId, bqTable.Resource, cancellationToken: cancellationToken);
        }
    }
}
