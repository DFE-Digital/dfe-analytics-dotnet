using Dfe.Analytics.EFCore.Description;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dfe.Analytics.EFCore;

public static class DbContextConfigurationExtensions
{
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static T HasAnalyticsSync<T>(this T builder, bool syncTable = true, bool syncAllColumns = true, bool? columnsArePii = null)
        where T : EntityTypeBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasAnnotation(AnnotationKeys.TableAnalyticsSyncMetadata, new TableSyncMetadata(syncTable, syncAllColumns, columnsArePii));

        return builder;
    }

    public static T HasAnalyticsSync<T>(this T builder, bool syncColumn = true, bool? isPii = null)
        where T : PropertyBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasAnnotation(AnnotationKeys.ColumnAnalyticsSyncMetadata, new ColumnSyncMetadata(syncColumn, isPii));

        return builder;
    }
}
