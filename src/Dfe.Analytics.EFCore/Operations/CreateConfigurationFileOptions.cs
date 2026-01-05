namespace Dfe.Analytics.EFCore.Operations;

internal record CreateConfigurationFileOptions : OperationOptionsBase
{
    public required string ConfigurationFilePath { get; init; }
    public required string DbContextAssemblyPath { get; init; }
    public required string DbContextTypeName { get; init; }

    public static CreateConfigurationFileOptions FromJson(string json) => Deserialize<CreateConfigurationFileOptions>(json);
}
