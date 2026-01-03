using Dfe.Analytics.EFCore;
using Dfe.Analytics.EFCore.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Analytics.Cli;

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
            var dbContext = DbContextHelper.CreateDbContext(
                parseResult.GetRequiredValue(dbContextAssemblyOption),
                parseResult.GetRequiredValue(dbContextNameOption));

            var configurationProvider = new AnalyticsConfigurationProvider();
            var configuration = configurationProvider.GetConfiguration(dbContext);

            var configurationPath = parseResult.GetRequiredValue(configurationPathOption);
            configuration.WriteToFile(configurationPath);
        });

        return command;
    }
}
