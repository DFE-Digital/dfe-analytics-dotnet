using System.CommandLine;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dfe.Analytics.EFCore.Cli;

internal static partial class Commands
{
    public static Command GetRuntimeConfigCreateCommand()
    {
        var sourcePathOption = new Option<string>("--source") { Required = true };
        var runtimeConfigPathOption = new Option<string>("--path") { Required = true };

        var command = new Command(
            "create",
            "Creates a copy of an application's runtimeconfig.json file with the DfE Analytics startup hook enabled.")
        {
            sourcePathOption,
            runtimeConfigPathOption
        };

        command.SetAction(async parseResult =>
        {
            var sourcePath = parseResult.GetRequiredValue(sourcePathOption);
            var runtimeConfigPath = parseResult.GetRequiredValue(runtimeConfigPathOption);

            var runtimeConfig = JsonNode.Parse(await File.ReadAllTextAsync(sourcePath))!.AsObject();
            AddStartupHook(runtimeConfig);

            await File.WriteAllTextAsync(
                runtimeConfigPath,
                runtimeConfig.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        });

        return command;
    }

    internal static void AddStartupHook(JsonObject runtimeConfig)
    {
        var runtimeOptions = GetOrAddObject(runtimeConfig, "runtimeOptions");
        var configProperties = GetOrAddObject(runtimeOptions, "configProperties");

        // Equivalent to setting DOTNET_STARTUP_HOOKS, without needing an environment variable (and therefore a shell)
        configProperties["STARTUP_HOOKS"] = typeof(StartupHook).Assembly.GetName().Name;
        configProperties["System.StartupHookProvider.IsSupported"] = true;

        static JsonObject GetOrAddObject(JsonObject parent, string propertyName)
        {
            if (parent[propertyName] is JsonObject existing)
            {
                return existing;
            }

            var created = new JsonObject();
            parent[propertyName] = created;
            return created;
        }
    }
}
