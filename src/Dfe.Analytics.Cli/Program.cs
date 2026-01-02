using System.CommandLine;

var rootCommand = new RootCommand("Tools for DfE Analytics.");

var parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
