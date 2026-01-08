namespace Dfe.Analytics.EFCore.Configuration;

public sealed class ColumnSyncInfo : IEquatable<ColumnSyncInfo>
{
    public required string Name { get; init; }
    public required bool Hidden { get; init; }

    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        return Equals((ColumnSyncInfo)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name, Hidden);
    }

    public bool Equals(ColumnSyncInfo? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Name == other.Name && Hidden == other.Hidden;
    }
}
