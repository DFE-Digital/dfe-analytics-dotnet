using System.CommandLine;
using Dfe.Analytics.EFCore.Cli;

namespace Dfe.Analytics.EFCore;

// Invoked with the application's deps.json and runtimeconfig.json so that the application's dependencies and shared
// frameworks are resolved exactly as they are when the application itself runs:
//   dotnet exec --depsfile MyApp.deps.json --runtimeconfig MyApp.runtimeconfig.json Dfe.Analytics.EFCore.dll <args>
internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        // Resolve relative paths against the application's directory so the tools can be invoked from anywhere
        // (e.g. directly from a container orchestrator, without a shell to change directory first).
        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        var configCommand = new Command("config")
        {
            Commands.GetConfigApplyCommand(),
            Commands.GetConfigCreateCommand()
        };

        var rootCommand = new RootCommand("Tools for DfE Analytics.") { configCommand };

        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }
}
