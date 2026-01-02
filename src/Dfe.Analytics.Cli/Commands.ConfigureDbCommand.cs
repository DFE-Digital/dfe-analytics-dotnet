using Dfe.Analytics.EFCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Analytics.Cli;

internal static partial class Commands
{
    public static Command GetConfigureDbCommand()
    {
        // DB context options
        var dbContextOption = new Option<string>("--context") { Required = true };
        var dbContextAssemblyOption = new Option<string>("--context-assembly") { Required = true };

        // DB connection options
        var connectionStringOption = new Option<string>("--connection-string") { Required = true };

        // BQ options
        var googleCredentialsOption = new Option<string>("--google-credentials") { Required = true };
        var projectIdOption = new Option<string>("--project-id") { Required = false };
        var datasetIdOption = new Option<string>("--dataset-id") { Required = false };
        var hiddenPolicyTagNameOption = new Option<string>("--hidden-policy-tag-name") { Required = true };

        // Airbyte options
        var airbyteApiBaseAddressOption = new Option<string>("--airbyte-api-base-address") { Required = true };
        var airbyteClientIdOption = new Option<string>("--airbyte-client-id") { Required = true };
        var airbyteClientSecretOption = new Option<string>("--airbyte-client-secret") { Required = true };
        var airbyteConnectionIdOption = new Option<string>("--airbyte-connection-id") { Required = true };

        var command = new Command("configure-db", "Configures the database, Airbyte and BigQuery for analytics sync.")
        {
            dbContextOption,
            dbContextAssemblyOption,
            connectionStringOption,
            googleCredentialsOption,
            projectIdOption,
            datasetIdOption,
            hiddenPolicyTagNameOption,
            airbyteApiBaseAddressOption,
            airbyteClientIdOption,
            airbyteClientSecretOption,
            airbyteConnectionIdOption
        };

        command.SetAction(async parseResult =>
        {
            var services = new ServiceCollection()
                .AddDfeAnalytics(options =>
                {
                    options.DatasetId = parseResult.GetRequiredValue(datasetIdOption);
                    options.ProjectId = parseResult.GetValue(projectIdOption);
                    options.CredentialsJson = parseResult.GetRequiredValue(googleCredentialsOption);
                })
                .ConfigureAirbyteApi(options =>
                {
                    options.BaseAddress = parseResult.GetRequiredValue(airbyteApiBaseAddressOption);
                    options.ClientId = parseResult.GetRequiredValue(airbyteClientIdOption);
                    options.ClientSecret = parseResult.GetRequiredValue(airbyteClientSecretOption);
                })
                .AddDeploymentTools()
                .Services
                .BuildServiceProvider();

            var dbContext = DbContextHelper.CreateDbContext(
                parseResult.GetRequiredValue(dbContextAssemblyOption),
                parseResult.GetRequiredValue(dbContextOption),
                parseResult.GetRequiredValue(connectionStringOption));

            var airbyteConnectionId = parseResult.GetRequiredValue(airbyteConnectionIdOption);
            var hiddenPolicyTagName = parseResult.GetRequiredValue(hiddenPolicyTagNameOption);

            using var scope = services.CreateScope();
            var analyticsDeployer = scope.ServiceProvider.GetRequiredService<AnalyticsDeployer>();
            await analyticsDeployer.DeployAsync(dbContext, airbyteConnectionId, hiddenPolicyTagName);
        });

        return command;
    }
}
