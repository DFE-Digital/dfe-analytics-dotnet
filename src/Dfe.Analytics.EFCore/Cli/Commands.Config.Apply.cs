using System.CommandLine;
using Dfe.Analytics.EFCore.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Analytics.EFCore.Cli;

internal static partial class Commands
{
    public static Command GetConfigApplyCommand()
    {
        // Configuration options
        var configurationPathOption = new Option<string>("--path") { Required = true };

        // BQ options
        var googleCredentialsOption = new Option<string>("--google-credentials") { Required = true };
        var projectIdOption = new Option<string>("--project-id") { Required = true };
        var datasetIdOption = new Option<string>("--dataset-id") { Required = true };
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

        // Ignore unmatched tokens to allow for extra arguments (e.g. from future versions of the Terraform module that invokes this command)
        command.TreatUnmatchedTokensAsErrors = false;

        command.SetAction(async parseResult =>
        {
            var googleCredentialsJson = parseResult.GetRequiredValue(googleCredentialsOption);
            var projectId = parseResult.GetRequiredValue(projectIdOption);
            var datasetId = parseResult.GetRequiredValue(datasetIdOption);
            var hiddenPolicyTagName = parseResult.GetRequiredValue(hiddenPolicyTagNameOption);
            var airbyteApiBaseAddress = parseResult.GetRequiredValue(airbyteApiBaseAddressOption);
            var airbyteClientId = parseResult.GetRequiredValue(airbyteClientIdOption);
            var airbyteClientSecret = parseResult.GetRequiredValue(airbyteClientSecretOption);
            var airbyteConnectionId = parseResult.GetRequiredValue(airbyteConnectionIdOption);
            var configurationPath = parseResult.GetRequiredValue(configurationPathOption);

            var services = new ServiceCollection()
                .AddDfeAnalytics(o =>
                {
                    o.DatasetId = datasetId;
                    o.ProjectId = projectId;
                    o.CredentialsJson = googleCredentialsJson;
                })
                .ConfigureAirbyteApi(o =>
                {
                    o.BaseAddress = airbyteApiBaseAddress;
                    o.ClientId = airbyteClientId;
                    o.ClientSecret = airbyteClientSecret;
                })
                .AddDeploymentTools()
                .Services
                .BuildServiceProvider();

            using var scope = services.CreateScope();

            var configuration = DatabaseSyncConfiguration.ReadFromFile(configurationPath);

            var analyticsDeployer = scope.ServiceProvider.GetRequiredService<AnalyticsDeployer>();
            await analyticsDeployer.DeployAsync(configuration, airbyteConnectionId, hiddenPolicyTagName);
        });

        return command;
    }
}
