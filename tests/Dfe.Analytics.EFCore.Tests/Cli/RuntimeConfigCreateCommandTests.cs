using System.Text.Json.Nodes;
using Dfe.Analytics.EFCore.Cli;

namespace Dfe.Analytics.EFCore.Tests.Cli;

public class RuntimeConfigCreateCommandTests
{
    [Fact]
    public void AddStartupHook_ExistingConfigProperties_AddsStartupHookAndPreservesExistingProperties()
    {
        // Arrange
        var runtimeConfig = JsonNode.Parse(
            """
            {
              "runtimeOptions": {
                "tfm": "net8.0",
                "framework": { "name": "Microsoft.AspNetCore.App", "version": "8.0.0" },
                "configProperties": { "System.GC.Server": true }
              }
            }
            """)!.AsObject();

        // Act
        Commands.AddStartupHook(runtimeConfig);

        // Assert
        var runtimeOptions = runtimeConfig["runtimeOptions"]!;
        Assert.Equal("net8.0", runtimeOptions["tfm"]!.GetValue<string>());
        Assert.Equal("Microsoft.AspNetCore.App", runtimeOptions["framework"]!["name"]!.GetValue<string>());
        var configProperties = runtimeOptions["configProperties"]!;
        Assert.True(configProperties["System.GC.Server"]!.GetValue<bool>());
        Assert.Equal("Dfe.Analytics.EFCore", configProperties["STARTUP_HOOKS"]!.GetValue<string>());
        Assert.True(configProperties["System.StartupHookProvider.IsSupported"]!.GetValue<bool>());
    }

    [Fact]
    public void AddStartupHook_NoConfigProperties_AddsConfigPropertiesWithStartupHook()
    {
        // Arrange
        var runtimeConfig = JsonNode.Parse(
            """
            {
              "runtimeOptions": {
                "tfm": "net8.0",
                "framework": { "name": "Microsoft.NETCore.App", "version": "8.0.0" }
              }
            }
            """)!.AsObject();

        // Act
        Commands.AddStartupHook(runtimeConfig);

        // Assert
        Assert.Equal(
            "Dfe.Analytics.EFCore",
            runtimeConfig["runtimeOptions"]!["configProperties"]!["STARTUP_HOOKS"]!.GetValue<string>());
    }
}
