using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.BigQuery.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

internal class DfeAnalyticsConfigureOptions : IConfigureOptions<DfeAnalyticsOptions>
{
    private readonly IConfiguration _configuration;

    public DfeAnalyticsConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(DfeAnalyticsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var section = _configuration.GetSection(Constants.RootConfigurationSectionName);

        section.AssignConfigurationValueIfNotEmpty("DatasetId", v => options.DatasetId = v);
        section.AssignConfigurationValueIfNotEmpty("Environment", v => options.Environment = v);
        section.AssignConfigurationValueIfNotEmpty("Namespace", v => options.Namespace = v);
        section.AssignConfigurationValueIfNotEmpty("TableId", v => options.TableId = v);
        section.AssignConfigurationValueIfNotEmpty("ProjectId", v => options.ProjectId = v);

        var credentialsJson = section["CredentialsJson"];

        if (!string.IsNullOrEmpty(credentialsJson))
        {
            var credentialsJsonDoc = JsonDocument.Parse(credentialsJson);

            var projectId = options.ProjectId;

            // We don't have ProjectId configured explicitly; see if it's set in the JSON credentials
            if (projectId is null &&
                credentialsJsonDoc.RootElement.TryGetProperty("project_id", out var projectIdElement) &&
                projectIdElement.ValueKind == JsonValueKind.String)
            {
                projectId = projectIdElement.GetString();
            }

            if (credentialsJsonDoc.RootElement.TryGetProperty("private_key", out _) && projectId is not null)
            {
                var creds = GoogleCredential.FromJson(credentialsJson);
                options.BigQueryClient = BigQueryClient.Create(projectId, creds);
            }
        }
    }
}
