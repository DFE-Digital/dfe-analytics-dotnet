using System.CommandLine;
using Dfe.Analytics.EFCore.Configuration;

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

        command.SetAction(parseResult =>
        {
            var configurationFilePath = parseResult.GetRequiredValue(configurationPathOption);
            var dbContextTypeName = parseResult.GetRequiredValue(dbContextNameOption);
            var dbContextAssemblyPath = parseResult.GetRequiredValue(dbContextAssemblyOption);

            using var dbContext = DbContextHelper.CreateDbContext(
                dbContextAssemblyPath,
                dbContextTypeName);

            var configurationProvider = new AnalyticsConfigurationProvider();
            var configuration = configurationProvider.GetConfiguration(dbContext);

            configuration.WriteToFile(configurationFilePath);
        });

        return command;
    }
}
