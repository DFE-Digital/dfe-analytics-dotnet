using System.Data;
using System.Diagnostics;
using Dfe.Analytics.EFCore.AirbyteApi;
using Dfe.Analytics.EFCore.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Dfe.Analytics.EFCore;

public class AnalyticsDeployer(
    AnalyticsConfigurationProvider configurationProvider,
    AirbyteApiClient airbyteApiClient,
    IOptions<DfeAnalyticsOptions> optionsAccessor)
{
    private const string PublicationName = "airbyte_publication";

    private static readonly string[] _airbyteFieldNames = ["_ab_cdc_lsn", "_ab_cdc_deleted_at", "_ab_cdc_updated_at"];

    public async Task DeployAsync(
        DbContext dbContext,
        string airbyteConnectionId,
        string hiddenPolicyTagName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(airbyteConnectionId);

        if (dbContext.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("Cannot deploy analytics configuration within an active database transaction.");
        }

        var configuration = configurationProvider.GetConfiguration(dbContext);

        await ConfigurePublicationAsync(
            configuration,
            (NpgsqlConnection)dbContext.Database.GetDbConnection(),
            cancellationToken);

        await ApplyBigQueryPolicyTagsAsync(
            configuration,
            hiddenPolicyTagName,
            cancellationToken);

        await SetAirbyteConfigurationAsync(
            configuration,
            airbyteConnectionId,
            cancellationToken);
    }

    private async Task ConfigurePublicationAsync(
        DatabaseSyncConfiguration configuration,
        NpgsqlConnection connection,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (configuration.Tables.Count == 0)
        {
            throw new InvalidOperationException("No tables are configured for synchronization.");
        }

        var publicationSpec = "TABLE " +
            string.Join(
                ", ",
                configuration.Tables.Select(t => $"\"{t.Name}\" ({string.Join(", ", t.Columns.Select(c => $"\"{c.Name}\""))})"));

        var updatePublicationSql = $"ALTER PUBLICATION {PublicationName} SET {publicationSpec};";
        var createPublicationSql = $"CREATE PUBLICATION {PublicationName} FOR {publicationSpec}";
        var dropPublicationSql = $"DROP PUBLICATION {PublicationName};";

        var startedOpen = connection.State is ConnectionState.Open;
        if (!startedOpen)
        {
            Debug.Assert(connection.State is ConnectionState.Closed);
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await ExecuteSqlAsync(updatePublicationSql);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.ObjectNotInPrerequisiteState)
        {
            // Publication exists, but it's defined 'FOR ALL TABLES', so we need to drop and recreate it
            await ExecuteSqlAsync(dropPublicationSql);
            await ExecuteSqlAsync(createPublicationSql);
        }
        catch (PostgresException ex) when (ex.SqlState is PostgresErrorCodes.UndefinedObject)
        {
            // Publication does not exist, create it
            await ExecuteSqlAsync(createPublicationSql);
        }
        finally
        {
            if (!startedOpen)
            {
                await connection.CloseAsync();
            }
        }

        async Task ExecuteSqlAsync(string commandText)
        {
            await using var cmd = connection.CreateCommand();
#pragma warning disable CA2100
            cmd.CommandText = commandText;
#pragma warning restore CA2100
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
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
