using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.BigQuery.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

internal class DfeAnalyticsConfigureOptions(IConfiguration configuration) : IConfigureOptions<DfeAnalyticsOptions>
{
    public void Configure(DfeAnalyticsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var section = configuration.GetSection(Constants.ConfigurationSectionName);

        section.AssignConfigurationValueIfNotEmpty("DatasetId", v => options.DatasetId = v);
        section.AssignConfigurationValueIfNotEmpty("Environment", v => options.Environment = v);
        section.AssignConfigurationValueIfNotEmpty("Namespace", v => options.Namespace = v);
        section.AssignConfigurationValueIfNotEmpty("TableId", v => options.TableId = v);
        section.AssignConfigurationValueIfNotEmpty("ProjectId", v => options.ProjectId = v);
        section.AssignConfigurationValueIfNotEmpty("Audience", v =>
        {
            options.FederatedAksAuthentication ??= new();
            options.FederatedAksAuthentication.Audience = v;
        });
        section.AssignConfigurationValueIfNotEmpty("GenerateAccessTokenUrl", v =>
        {
            options.FederatedAksAuthentication ??= new();
            options.FederatedAksAuthentication.ServiceAccountImpersonationUrl = v;
        });

        var credentialsJson = section["CredentialsJson"];
        if (!string.IsNullOrEmpty(credentialsJson))
        {
            using var credentialsJsonDoc = JsonDocument.Parse(credentialsJson);
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
