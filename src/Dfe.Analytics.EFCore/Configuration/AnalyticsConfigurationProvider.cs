using Dfe.Analytics.EFCore.Description;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Dfe.Analytics.EFCore.Configuration;

public class AnalyticsConfigurationProvider
{
    private const string NpgsqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

    public DatabaseSyncConfiguration GetConfiguration(DbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        ThrowIfUnsupportedProvider(dbContext);

        var tables = new List<TableSyncInfo>();

        foreach (var entityType in dbContext.Model.GetEntityTypes())
        {
            var tableSyncMetadata = entityType.FindAnnotation(AnnotationKeys.TableAnalyticsSyncMetadata)?.Value as TableSyncMetadata;

            if (tableSyncMetadata?.SyncTable is not true)
            {
                continue;
            }

            var tableName = entityType.GetTableName()!;
            var primaryKeySyncInfo = GetPrimaryKey(entityType);
            var columnSyncInfos = GetColumns(entityType, tableSyncMetadata);

            tables.Add(new TableSyncInfo
            {
                Name = tableName,
                PrimaryKey = primaryKeySyncInfo,
                Columns = columnSyncInfos
            });
        }

        return new DatabaseSyncConfiguration { Tables = tables.ToArray() };
    }

    private static TablePrimaryKeySyncInfo GetPrimaryKey(IEntityType entityType)
    {
        var primaryKey = entityType.GetKeys().SingleOrDefault(k => k.IsPrimaryKey());

        if (primaryKey is null)
        {
            throw new InvalidOperationException($"Entity '{entityType.Name}' does not have a primary key.");
        }

        // Check columns in the primary key are not explicitly marked to be excluded from the sync
        foreach (var pkProperty in primaryKey.Properties)
        {
            var columnSyncMetadata = pkProperty.FindAnnotation(AnnotationKeys.ColumnAnalyticsSyncMetadata)?.Value as ColumnSyncMetadata;

            if (columnSyncMetadata?.SyncColumn is false)
            {
                throw new InvalidOperationException(
                    $"Primary key column '{pkProperty.Name}' in entity '{entityType.Name}' cannot be excluded from analytics sync.");
            }
        }

        return new TablePrimaryKeySyncInfo { ColumnNames = primaryKey.Properties.Select(p => p.Name).ToArray() };
    }

#pragma warning disable CA1859
    private static IReadOnlyCollection<ColumnSyncInfo> GetColumns(IEntityType entityType, TableSyncMetadata tableSyncMetadata)
    {
        var columnSyncInfos = new List<ColumnSyncInfo>();

        foreach (var property in entityType.GetProperties())
        {
            var columnName = property.GetColumnName();
            var columnSyncMetadata = property.FindAnnotation(AnnotationKeys.ColumnAnalyticsSyncMetadata)?.Value as ColumnSyncMetadata;

            if (columnSyncMetadata?.SyncColumn is true || tableSyncMetadata.SyncAllColumns)
            {
                var isPii = columnSyncMetadata?.IsPii ?? tableSyncMetadata.ColumnsArePii ??
                    throw new InvalidOperationException($"Property '{property.Name}' in entity '{entityType.Name}' does not have a defined PII setting.");

                columnSyncInfos.Add(new ColumnSyncInfo
                {
                    Name = columnName,
                    IsPii = isPii
                });
            }
        }

        return columnSyncInfos.AsReadOnly();
    }
# pragma warning restore CA1859

    private static void ThrowIfUnsupportedProvider(DbContext dbContext)
    {
        if (dbContext.Database.ProviderName is not NpgsqlProviderName)
        {
            throw new NotSupportedException($"{dbContext.Database.ProviderName} is not a supported provider.");
        }
    }
}
