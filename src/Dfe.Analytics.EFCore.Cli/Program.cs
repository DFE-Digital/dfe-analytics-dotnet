using System.CommandLine;
using Dfe.Analytics.EFCore.Cli;

var appAssemblyPathOption = new Option<string>("--app-assembly-path")
{
    Required = true,
    Recursive = true
};

var configCommand = new Command("config")
{
    Commands.GetConfigApplyCommand(),
    Commands.GetConfigCreateCommand(),
    appAssemblyPathOption
};

var rootCommand = new RootCommand("Tools for DfE Analytics.") { configCommand };

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
