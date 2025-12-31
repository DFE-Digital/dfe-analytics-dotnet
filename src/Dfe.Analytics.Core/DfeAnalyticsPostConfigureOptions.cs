using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.BigQuery.V2;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

internal class DfeAnalyticsPostConfigureOptions : IPostConfigureOptions<DfeAnalyticsOptions>
{
    public void PostConfigure(string? name, DfeAnalyticsOptions options)
    {
        if (string.IsNullOrEmpty(options.CredentialsJson))
        {
            return;
        }

        // Configure missing properties from the credentials JSON if it's set

        using var credentialsJson = JsonDocument.Parse(options.CredentialsJson);

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
