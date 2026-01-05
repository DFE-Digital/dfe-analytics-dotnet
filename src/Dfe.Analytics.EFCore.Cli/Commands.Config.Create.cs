using System.CommandLine;

namespace Dfe.Analytics.EFCore.Cli;

internal static partial class Commands
{
    public static Command GetConfigCreateCommand()
    {
        var configurationPathOption = new Option<string>("--path") { Required = true };
        var dbContextNameOption = new Option<string>("--dbcontext-name") { Required = true };
        var dbContextAssemblyOption = new Option<string>("--dbcontext-assembly") { Required = true };

        var command = new Command("create", "Creates a configuration file from an Entity Framework Core DbContext.")
        {
            configurationPathOption,
            dbContextNameOption,
            dbContextAssemblyOption
        };

        command.SetAction(async parseResult =>
        {
            var appAssemblyPath = parseResult.GetRequiredValue<string>("--app-assembly-path");

            var options = new CreateConfigurationFileOptions
            {
                ConfigurationFilePath = parseResult.GetRequiredValue(configurationPathOption),
                DbContextTypeName = parseResult.GetRequiredValue(dbContextNameOption),
                DbContextAssemblyPath = parseResult.GetRequiredValue(dbContextAssemblyOption)
            };

            var invoker = new ReflectionOperationInvoker();
            await invoker.InvokeAsync("CreateConfigurationFileAsync", options.Serialize(), appAssemblyPath);
        });

        return command;
    }
}
