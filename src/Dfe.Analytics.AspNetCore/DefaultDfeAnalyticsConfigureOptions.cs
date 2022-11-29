using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.BigQuery.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.AspNetCore;

internal class DefaultDfeAnalyticsConfigureOptions : IConfigureOptions<DfeAnalyticsOptions>
{
    private readonly IConfiguration _configuration;

    public DefaultDfeAnalyticsConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(DfeAnalyticsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        const string configurationSectionName = "DfeAnalytics";
        var section = _configuration.GetSection(configurationSectionName);

        AssignConfigurationValueIfNotEmpty("DatasetId", v => options.DatasetId ??= v);
        AssignConfigurationValueIfNotEmpty("Environment", v => options.Environment ??= v);
        AssignConfigurationValueIfNotEmpty("Namespace", v => options.Namespace ??= v);
        AssignConfigurationValueIfNotEmpty("TableId", v => options.TableId ??= v);
        AssignConfigurationValueIfNotEmpty("UserIdClaimType", v => options.UserIdClaimType ??= v);

        var credentialsJson = section["CredentialsJson"];

        if (!string.IsNullOrEmpty(credentialsJson))
        {
            var projectId = section["ProjectId"];

            if (projectId is null)
            {
                // We don't have ProjectId configured explicitly; see if it's set in the JSON credentials

                var credentialsJsonDoc = JsonDocument.Parse(credentialsJson);

                if (credentialsJsonDoc.RootElement.TryGetProperty("project_id", out var projectIdElement) &&
                    projectIdElement.ValueKind == JsonValueKind.String)
                {
                    projectId = projectIdElement.GetString();
                }
            }

            if (projectId is not null)
            {
                var creds = GoogleCredential.FromJson(credentialsJson);
                options.BigQueryClient = BigQueryClient.Create(projectId, creds);
            }
        }

        void AssignConfigurationValueIfNotEmpty(string configKey, Action<string> assignValue)
        {
            var value = section[configKey];

            if (!string.IsNullOrEmpty(value))
            {
                assignValue(value);
            }
        }
    }
}
