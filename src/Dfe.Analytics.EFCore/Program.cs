using System.CommandLine;
using Dfe.Analytics.EFCore.Cli;

namespace Dfe.Analytics.EFCore;

// Publishing an app creates copies of its deps.json and runtimeconfig.json named for this assembly, so the app's
// dependencies and shared frameworks resolve exactly as they do when the app itself runs. Invoked with either:
//   dotnet Dfe.Analytics.EFCore.dll <args>
//   dfe-analytics/dfe-analytics <args>
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
