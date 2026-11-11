using Dfe.Analytics.EFCore.Description;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dfe.Analytics.EFCore;

public static class DbContextConfigurationExtensions
{
    private const string DefaultPolicyTagName = "hidden";

    /// <summary>
    /// Configures the Airbyte sync mode used for every table in the model that doesn't specify its own sync mode.
    /// </summary>
    /// <remarks>
    /// This takes precedence over the sync mode passed to the deploy command.
    /// </remarks>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static ModelBuilder ConfigureAnalyticsSync(this ModelBuilder modelBuilder, AirbyteSyncMode airbyteSyncMode)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasAnnotation(
            AnnotationKeys.DatabaseSyncMetadata,
            new DatabaseSyncMetadata(airbyteSyncMode));

        return modelBuilder;
    }

    /// <summary>
    /// Includes the entity's table in the analytics sync.
    /// </summary>
    /// <param name="builder">The entity type builder.</param>
    /// <param name="includeAllColumns">Whether columns are included in the sync unless configured otherwise.</param>
    /// <param name="hidden">Whether columns are hidden unless configured otherwise.</param>
    /// <param name="airbyteSyncMode">
    /// The Airbyte sync mode for this table. This takes precedence over the sync mode configured for the model with
    /// <see cref="ConfigureAnalyticsSync(ModelBuilder, AirbyteSyncMode)"/> and the sync mode passed to the deploy command.
    /// It can only be set on the root entity type in an inheritance hierarchy.
    /// </param>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public static T IncludeInAnalyticsSync<T>(
        this T builder,
        bool includeAllColumns = true,
        bool? hidden = null,
        AirbyteSyncMode? airbyteSyncMode = null)
        where T : EntityTypeBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasAnnotation(
            AnnotationKeys.TableAnalyticsSyncMetadata,
            new TableSyncMetadata(new ColumnSyncMetadata(includeAllColumns, hidden, PolicyTag: DefaultPolicyTagName), airbyteSyncMode));

        return builder;
    }

    public static T ConfigureAnalyticsSync<T>(this T builder, bool included = true, bool? hidden = null, string? policyTag = null)
        where T : PropertyBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (hidden is null && policyTag is not null)
        {
            hidden = true;
        }

        builder.HasAnnotation(
            AnnotationKeys.ColumnAnalyticsSyncMetadata,
            new ColumnSyncMetadata(included, hidden, policyTag ?? DefaultPolicyTagName));

        return builder;
    }
}
