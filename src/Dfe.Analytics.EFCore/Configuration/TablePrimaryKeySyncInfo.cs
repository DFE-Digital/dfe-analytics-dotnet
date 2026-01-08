namespace Dfe.Analytics.EFCore.Configuration;

public sealed class TablePrimaryKeySyncInfo : IEquatable<TablePrimaryKeySyncInfo>
{
    public required IReadOnlyCollection<string> ColumnNames { get; init; }

    public bool Equals(TablePrimaryKeySyncInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return ColumnNames.SequenceEqual(other.ColumnNames);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is TablePrimaryKeySyncInfo other && Equals(other);
    }

    public override int GetHashCode()
    {
        return ColumnNames.GetHashCode();
    }
}
