using Dfe.Analytics.Cli;

var rootCommand = new RootCommand("Tools for DfE Analytics.")
{
    Commands.GetConfigureDbCommand()
};

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
