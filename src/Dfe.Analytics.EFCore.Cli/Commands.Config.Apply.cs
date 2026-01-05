using System.CommandLine;

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

        command.SetAction(async parseResult =>
        {
            var appAssemblyPath = parseResult.GetRequiredValue<string>("--app-assembly-path");

            var options = new ApplyConfigurationOptions
            {
                ProjectId = parseResult.GetRequiredValue(projectIdOption),
                DatasetId = parseResult.GetRequiredValue(datasetIdOption),
                GoogleCredentialsJson = parseResult.GetRequiredValue(googleCredentialsOption),
                HiddenPolicyTagName = parseResult.GetRequiredValue(hiddenPolicyTagNameOption),
                AirbyteApiBaseAddress = parseResult.GetRequiredValue(airbyteApiBaseAddressOption),
                AirbyteClientId = parseResult.GetRequiredValue(airbyteClientIdOption),
                AirbyteClientSecret = parseResult.GetRequiredValue(airbyteClientSecretOption),
                AirbyteConnectionId = parseResult.GetRequiredValue(airbyteConnectionIdOption),
                ConfigurationFilePath = parseResult.GetRequiredValue(configurationPathOption)
            };

            var invoker = new ReflectionOperationInvoker();
            await invoker.InvokeAsync("ApplyConfigurationAsync", options.Serialize(), appAssemblyPath);
        });

        return command;
    }
}
