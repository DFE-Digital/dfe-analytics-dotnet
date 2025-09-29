using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

#pragma warning disable CA1812
internal class FederatedAksAuthenticationConfigureOptions(IConfiguration configuration) : IConfigureOptions<FederatedAksAuthenticationOptions>
#pragma warning restore CA1812
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(FederatedAksAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var section = _configuration.GetSection(Constants.RootConfigurationSectionName);

        section.AssignConfigurationValueIfNotEmpty("Audience", v => options.Audience = v);
        section.AssignConfigurationValueIfNotEmpty("GenerateAccessTokenUrl", v => options.GenerateAccessTokenUrl = v);

        if (options.Audience is null &&
            section["ProjectNumber"] is string projectNumber &&
            section["WorkloadIdentityPoolName"] is string workloadIdentityPoolName &&
            section["WorkloadIdentityPoolProviderName"] is string workloadIdentityPoolProviderName)
        {
            options.Audience = $"//iam.googleapis.com/projects/{Uri.EscapeDataString(projectNumber)}/" +
                $"locations/global/workloadIdentityPools/{Uri.EscapeDataString(workloadIdentityPoolName)}/" +
                $"providers/{Uri.EscapeDataString(workloadIdentityPoolProviderName)}";
        }

        if (options.GenerateAccessTokenUrl is null &&
            section["ServiceAccountEmail"] is string serviceAccountEmail)
        {
            options.GenerateAccessTokenUrl = $"https://iamcredentials.googleapis.com/v1/projects/-/serviceAccounts/{Uri.EscapeDataString(serviceAccountEmail)}:generateAccessToken";
        }

        var credentialsJson = section["CredentialsJson"];

        if (!string.IsNullOrEmpty(credentialsJson))
        {
            using var credentialsJsonDoc = JsonDocument.Parse(credentialsJson);

            if (options.Audience is null &&
                credentialsJsonDoc.RootElement.TryGetProperty("audience", out var audienceElement) &&
                audienceElement.ValueKind == JsonValueKind.String)
            {
                options.Audience = audienceElement.GetString()!;
            }

            if (options.GenerateAccessTokenUrl is null &&
                credentialsJsonDoc.RootElement.TryGetProperty("service_account_impersonation_url", out var impersonationUrlElement) &&
                impersonationUrlElement.ValueKind == JsonValueKind.String)
            {
                options.GenerateAccessTokenUrl = impersonationUrlElement.GetString()!;
            }
        }
    }
}
