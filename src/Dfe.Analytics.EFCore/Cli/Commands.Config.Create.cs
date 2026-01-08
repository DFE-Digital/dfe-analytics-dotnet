using System.CommandLine;
using Dfe.Analytics.EFCore.Configuration;

namespace Dfe.Analytics.EFCore.Cli;

internal static partial class Commands
{
    public static Command GetConfigCreateCommand()
    {
        var configurationPathOption = new Option<string>("--path") { Required = true };
        var dbContextOption = new Option<string>("--dbcontext") { Required = true };

        var command = new Command("create", "Creates a configuration file from an Entity Framework Core DbContext.")
        {
            configurationPathOption,
            dbContextOption
        };

        command.SetAction(parseResult =>
        {
            var configurationFilePath = parseResult.GetRequiredValue(configurationPathOption);
            var dbContextName = parseResult.GetRequiredValue(dbContextOption);

            using var dbContext = DbContextHelper.CreateDbContext(dbContextName);

            var configurationProvider = new AnalyticsConfigurationProvider();
            var configuration = configurationProvider.GetConfiguration(dbContext);

            return configuration.WriteToFileAsync(configurationFilePath);
        });

        return command;
    }
}
