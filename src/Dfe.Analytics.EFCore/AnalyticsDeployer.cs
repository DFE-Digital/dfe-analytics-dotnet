using System.Data;
using System.Diagnostics;
using Dfe.Analytics.EFCore.AirbyteApi;
using Dfe.Analytics.EFCore.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dfe.Analytics.EFCore;

public class AnalyticsDeployer(AnalyticsConfigurationProvider configurationProvider, AirbyteApiClient airbyteApiClient)
{
    private const string PublicationName = "airbyte_publication";

    public async Task DeployAsync(DbContext dbContext, string airbyteConnectionId, CancellationToken cancellationToken = default)
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

        var syncedTableNames = configuration.Tables.Select(t => t.Name).ToArray();
        if (syncedTableNames.Length == 0)
        {
            throw new InvalidOperationException("No tables are configured for synchronization.");
        }

        var updatePublicationSql =
            $"""
             ALTER PUBLICATION {PublicationName}
             SET TABLE {string.Join(", ", syncedTableNames.Select(n => $"\"{n}\""))};
             """;

        var createPublicationSql =
            $"""
             CREATE PUBLICATION {PublicationName}
             FOR TABLE {string.Join(", ", syncedTableNames.Select(n => $"\"{n}\""))};
             """;

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
            cmd.CommandText = commandText;
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
                        SelectedFields = new[] { "_ab_cdc_lsn", "_ab_cdc_deleted_at", "_ab_cdc_updated_at" }.Concat(t.Columns.Select(c => c.Name))
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
}
