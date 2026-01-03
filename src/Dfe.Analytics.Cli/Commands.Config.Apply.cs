using Dfe.Analytics.EFCore;
using Dfe.Analytics.EFCore.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Analytics.Cli;

internal static partial class Commands
{
    public static Command GetConfigApplyCommand()
    {
        // Configuration options
        var configurationPathOption = new Option<string>("--path") { Required = true };

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

        var command = new Command("apply", "Configures Airbyte and BigQuery with the specified configuration.")
        {
            configurationPathOption,
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

            using var scope = services.CreateScope();

            var configurationPath = parseResult.GetRequiredValue(configurationPathOption);
            var configuration = DatabaseSyncConfiguration.ReadFromFile(configurationPath);

            var airbyteConnectionId = parseResult.GetRequiredValue(airbyteConnectionIdOption);
            var hiddenPolicyTagName = parseResult.GetRequiredValue(hiddenPolicyTagNameOption);

            var analyticsDeployer = scope.ServiceProvider.GetRequiredService<AnalyticsDeployer>();
            await analyticsDeployer.DeployAsync(configuration, airbyteConnectionId, hiddenPolicyTagName);
        });

        return command;
    }
}
