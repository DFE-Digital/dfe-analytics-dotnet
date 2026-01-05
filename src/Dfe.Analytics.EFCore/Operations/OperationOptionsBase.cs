using System.Text.Json;

namespace Dfe.Analytics.EFCore.Operations;

internal record OperationOptionsBase
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.General);

    // ReSharper disable once UnusedMember.Global
    public string Serialize() => JsonSerializer.Serialize(this, GetType(), _serializerOptions);

    protected static T Deserialize<T>(string json) where T : OperationOptionsBase
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<T>(json, _serializerOptions)!;
    }
}
