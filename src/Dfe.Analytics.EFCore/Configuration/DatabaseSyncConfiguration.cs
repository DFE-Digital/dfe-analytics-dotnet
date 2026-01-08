using System.Text.Json;

namespace Dfe.Analytics.EFCore.Configuration;

public sealed class DatabaseSyncConfiguration : IEquatable<DatabaseSyncConfiguration>
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public required IReadOnlyCollection<TableSyncInfo> Tables { get; init; }

    public static async Task<DatabaseSyncConfiguration> ReadFromFileAsync(string configurationPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configurationPath);

        if (!File.Exists(configurationPath))
        {
            throw new FileNotFoundException($"Configuration file not found at path: '{configurationPath}'.");
        }

        var json = await File.ReadAllTextAsync(configurationPath, cancellationToken);

        return JsonSerializer.Deserialize<DatabaseSyncConfiguration>(json, _jsonSerializerOptions)
            ?? throw new InvalidOperationException("Failed to deserialize DatabaseSyncConfiguration.");
    }

    public Task WriteToFileAsync(string configurationPath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configurationPath);

        var json = JsonSerializer.Serialize(this, _jsonSerializerOptions);

        return File.WriteAllTextAsync(configurationPath, json, cancellationToken);
    }

    public bool Equals(DatabaseSyncConfiguration? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Tables.SequenceEqual(other.Tables);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is DatabaseSyncConfiguration other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Tables.Select(t => t.GetHashCode()));
    }
}
