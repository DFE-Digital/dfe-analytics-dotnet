using Dfe.Analytics.EFCore.Description;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dfe.Analytics.EFCore;

public static class DbContextConfigurationExtensions
{
    public static ModelBuilder ConfigureAnalyticsSync(this ModelBuilder modelBuilder, AirbyteSyncMode airbyteSyncMode = AirbyteSyncMode.IncrementalAppend)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasAnnotation(
            AnnotationKeys.DatabaseSyncMetadata,
            new DatabaseSyncMetadata(airbyteSyncMode));

        return modelBuilder;
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public static T IncludeInAnalyticsSync<T>(this T builder, bool includeAllColumns = true, bool? hidden = null)
        where T : EntityTypeBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasAnnotation(
            AnnotationKeys.TableAnalyticsSyncMetadata,
            new TableSyncMetadata(new ColumnSyncMetadata(includeAllColumns, hidden)));

        return builder;
    }

    public static T ConfigureAnalyticsSync<T>(this T builder, bool included = true, bool? hidden = null)
        where T : PropertyBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasAnnotation(
            AnnotationKeys.ColumnAnalyticsSyncMetadata,
            new ColumnSyncMetadata(included, hidden));

        return builder;
    }
}
