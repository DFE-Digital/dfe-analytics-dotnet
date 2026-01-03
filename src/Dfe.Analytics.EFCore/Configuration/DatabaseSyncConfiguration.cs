using System.Text.Json;

namespace Dfe.Analytics.EFCore.Configuration;

public record DatabaseSyncConfiguration
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public required IReadOnlyCollection<TableSyncInfo> Tables { get; init; }

    public static DatabaseSyncConfiguration ReadFromFile(string configurationPath)
    {
        ArgumentNullException.ThrowIfNull(configurationPath);

        if (!File.Exists(configurationPath))
        {
            throw new FileNotFoundException($"Configuration file not found at path: '{configurationPath}'.");
        }

        var json = File.ReadAllText(configurationPath);

        return JsonSerializer.Deserialize<DatabaseSyncConfiguration>(json, _jsonSerializerOptions)
            ?? throw new InvalidOperationException("Failed to deserialize DatabaseSyncConfiguration.");
    }

    public void WriteToFile(string configurationPath)
    {
        ArgumentNullException.ThrowIfNull(configurationPath);

        var json = JsonSerializer.Serialize(this, _jsonSerializerOptions);

        File.WriteAllText(configurationPath, json);
    }
}
