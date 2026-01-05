namespace Dfe.Analytics.EFCore.Operations;

internal record ApplyConfigurationOptions : OperationOptionsBase
{
    public required string ProjectId { get; init; }
    public required string DatasetId { get; init; }
    public required string GoogleCredentialsJson { get; init; }
    public required string HiddenPolicyTagName { get; init; }
    public required string AirbyteApiBaseAddress { get; init; }
    public required string AirbyteClientId { get; init; }
    public required string AirbyteClientSecret { get; init; }
    public required string AirbyteConnectionId { get; init; }
    public required string ConfigurationFilePath { get; init; }

    public static ApplyConfigurationOptions FromJson(string json) => Deserialize<ApplyConfigurationOptions>(json);
}
