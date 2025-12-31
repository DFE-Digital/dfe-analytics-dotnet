namespace Dfe.Analytics.EFCore.Description;

internal record TableSyncMetadata(bool SyncTable, bool SyncAllColumns, bool? ColumnsArePii);
