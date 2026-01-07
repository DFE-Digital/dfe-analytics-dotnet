using System.CommandLine;
using Dfe.Analytics.EFCore.Cli;

namespace Dfe.Analytics.EFCore;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
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
