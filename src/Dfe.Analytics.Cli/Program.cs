using Dfe.Analytics.Cli;

var configCommand = new Command("config")
{
    Commands.GetConfigApplyCommand(),
    Commands.GetConfigCreateCommand()
};

var rootCommand = new RootCommand("Tools for DfE Analytics.")
{
    configCommand
};

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
