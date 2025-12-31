using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.BigQuery.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

internal class DfeAnalyticsConfigureOptions(IServiceProvider serviceProvider) : IConfigureOptions<DfeAnalyticsOptions>
{
    public void Configure(DfeAnalyticsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (serviceProvider.GetService<IConfiguration>() is { } configuration)
        {
            var configurationSection = configuration.GetSection(Constants.ConfigurationSectionName);

            configurationSection.AssignConfigurationValueIfNotEmpty("DatasetId", v => options.DatasetId = v);
            configurationSection.AssignConfigurationValueIfNotEmpty("ProjectId", v => options.ProjectId = v);
            configurationSection.AssignConfigurationValueIfNotEmpty("Audience", v =>
            {
                options.FederatedAksAuthentication ??= new();
                options.FederatedAksAuthentication.Audience = v;
            });
            configurationSection.AssignConfigurationValueIfNotEmpty("GenerateAccessTokenUrl", v =>
            {
                options.FederatedAksAuthentication ??= new();
                options.FederatedAksAuthentication.ServiceAccountImpersonationUrl = v;
            });
        }

        if (!string.IsNullOrEmpty(options.CredentialsJson))
        {
            using var credentialsJsonDoc = JsonDocument.Parse(options.CredentialsJson);
            AssignConfigurationFromCredentialsJson(options, credentialsJsonDoc);
        }
    }

    private void AssignConfigurationFromCredentialsJson(DfeAnalyticsOptions options, JsonDocument credentialsJson)
    {
        if (options.ProjectId is null &&
            credentialsJson.RootElement.TryGetProperty("project_id", out var projectIdElement))
        {
            options.ProjectId = projectIdElement.GetString();
        }

        if (options.FederatedAksAuthentication?.Audience is null &&
            credentialsJson.RootElement.TryGetProperty("audience", out var audienceElement))
        {
            options.FederatedAksAuthentication ??= new();
            options.FederatedAksAuthentication.Audience = audienceElement.GetString()!;
        }

        if (options.FederatedAksAuthentication?.ServiceAccountImpersonationUrl is null &&
            credentialsJson.RootElement.TryGetProperty("service_account_impersonation_url", out var impersonationUrlElement))
        {
            options.FederatedAksAuthentication ??= new();
            options.FederatedAksAuthentication.ServiceAccountImpersonationUrl = impersonationUrlElement.GetString()!;
        }

        if (options.BigQueryClient is null && options.ProjectId is { } projectId)
        {
            if (credentialsJson.RootElement.TryGetProperty("private_key", out _))
            {
                options.BigQueryClient = BigQueryClient.Create(
                    projectId,
                    GoogleCredential.FromJson(credentialsJson.ToString()));
            }
            else if (Environment.GetEnvironmentVariable(FederatedAksSubjectTokenProvider.TokenPathEnvironmentVariableName) is not null &&
                options.FederatedAksAuthentication is { Audience: { } audience, ServiceAccountImpersonationUrl: { } serviceAccountImpersonationUrl })
            {
                options.BigQueryClient = BigQueryClient.Create(
                    projectId,
                    GoogleCredential.FromProgrammaticExternalAccountCredential(
                        new ProgrammaticExternalAccountCredential(
                            new ProgrammaticExternalAccountCredential.Initializer(
                                tokenUrl: "https://sts.googleapis.com/v1/token",
                                audience,
                                FederatedAksSubjectTokenProvider.SubjectTokenType,
#pragma warning disable CA2000
                                new FederatedAksSubjectTokenProvider())
                            {
                                ServiceAccountImpersonationUrl = serviceAccountImpersonationUrl
                            })));
#pragma warning restore CA2000
            }
        }
    }
}
