using System.Text.Json.Serialization;

namespace Dfe.Analytics.EFCore.Configuration;

public sealed class TableSyncInfo : IEquatable<TableSyncInfo>
{
    public required string Name { get; init; }
    public required TablePrimaryKeySyncInfo PrimaryKey { get; init; }
    public required IReadOnlyCollection<ColumnSyncInfo> Columns { get; init; }

    /// <summary>
    /// The Airbyte sync mode configured for this table, if any.
    /// When <see langword="null"/> the sync mode passed to the deploy command (or its default) is used.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AirbyteSyncMode { get; init; }

    public bool Equals(TableSyncInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name && PrimaryKey.Equals(other.PrimaryKey) && Columns.SequenceEqual(other.Columns) &&
            AirbyteSyncMode == other.AirbyteSyncMode;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is TableSyncInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Name);
        hashCode.Add(PrimaryKey);
        hashCode.Add(AirbyteSyncMode);

        foreach (var column in Columns)
        {
            hashCode.Add(column);
        }

        return hashCode.ToHashCode();
    }
}
