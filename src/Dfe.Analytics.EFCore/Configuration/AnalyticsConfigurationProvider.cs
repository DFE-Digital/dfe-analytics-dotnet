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

        var dbContextName = dbContext.GetType().AssemblyQualifiedName ??
            throw new InvalidOperationException("Failed to get the assembly qualified name of the DbContext.");

        var tables = new List<TableSyncInfo>();

        foreach (var rootEntityType in dbContext.Model.GetEntityTypes())
        {
            // Only consider root entity types in the inheritance hierarchy to avoid duplicate table configurations for derived types sharing the same table;
            // the columns from all derived types will be covered below
            if (rootEntityType.BaseType is not null)
            {
                continue;
            }

            var entityTypes = rootEntityType.GetDerivedTypesInclusive().ToArray();

            // We don't currently support more than one level of inheritance
            if (entityTypes.Any(e => e.BaseType?.BaseType is not null))
            {
                throw new NotSupportedException(
                    $"Entity '{rootEntityType.Name}' has more than one level of inheritance, which is not currently supported.");
            }

            var rootTableSyncMetadata = rootEntityType.FindAnnotation(AnnotationKeys.TableAnalyticsSyncMetadata)?.Value as TableSyncMetadata;

            if (rootTableSyncMetadata is null)
            {
                if (entityTypes.Any(e => e.GetAnnotations().Any(a => a.Name == AnnotationKeys.TableAnalyticsSyncMetadata)))
                {
                    throw new InvalidOperationException(
                        $"Entity '{rootEntityType.Name}' does not have table sync metadata, but one or more of its derived types do." +
                        $" Table sync metadata must be defined on the root entity type in the inheritance hierarchy.");
                }

                continue;
            }

            var tableName = rootEntityType.GetTableName()!;
            var primaryKeySyncInfo = GetPrimaryKey(rootEntityType);

            var columnSyncInfos = entityTypes
                .Select(et => new
                {
                    EntityType = et,
                    TableSyncMetadata = et.FindAnnotation(AnnotationKeys.TableAnalyticsSyncMetadata)?.Value as TableSyncMetadata
                })
                .Where(t => t.TableSyncMetadata is not null)
                .SelectMany(t => GetColumns(t.EntityType, t.TableSyncMetadata!))
                .ToArray();

            tables.Add(new TableSyncInfo
            {
                Name = tableName,
                PrimaryKey = primaryKeySyncInfo,
                Columns = columnSyncInfos
            });
        }

        return new DatabaseSyncConfiguration { DbContextName = dbContextName, Tables = tables.ToArray() };
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

            if (columnSyncMetadata?.Included is false)
            {
                throw new InvalidOperationException(
                    $"Primary key column '{pkProperty.Name}' in entity '{entityType.Name}' cannot be excluded from analytics sync.");
            }
        }

        return new TablePrimaryKeySyncInfo { ColumnNames = primaryKey.Properties.Select(p => p.GetColumnName()).ToArray() };
    }

#pragma warning disable CA1859
    private static IReadOnlyCollection<ColumnSyncInfo> GetColumns(IEntityType entityType, TableSyncMetadata tableSyncMetadata)
    {
        var columnSyncInfos = new List<ColumnSyncInfo>();

        foreach (var property in entityType.GetDeclaredProperties())
        {
            var columnName = property.GetColumnName();
            var columnSyncMetadata = property.FindAnnotation(AnnotationKeys.ColumnAnalyticsSyncMetadata)?.Value as ColumnSyncMetadata;

            if (columnSyncMetadata?.Included ?? tableSyncMetadata.DefaultColumnSettings.Included)
            {
                var hidden = columnSyncMetadata?.Hidden ?? tableSyncMetadata.DefaultColumnSettings.Hidden ??
                    throw new InvalidOperationException(
                        $"The 'hidden' attribute of the '{property.Name}' property in entity '{entityType.Name}' is not set.");

                columnSyncInfos.Add(new ColumnSyncInfo
                {
                    Name = columnName,
                    Hidden = hidden
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
