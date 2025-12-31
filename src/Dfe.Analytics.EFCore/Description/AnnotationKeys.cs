namespace Dfe.Analytics.EFCore.Description;

internal static class AnnotationKeys
{
    private const string BaseKeyName = "DfeAnalytics";

    internal const string TableAnalyticsSyncMetadata = BaseKeyName + nameof(TableAnalyticsSyncMetadata);
    internal const string ColumnAnalyticsSyncMetadata = BaseKeyName + nameof(ColumnAnalyticsSyncMetadata);
}
