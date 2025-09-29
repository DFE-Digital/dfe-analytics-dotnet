using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.BigQuery.V2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

#pragma warning disable CA1812
internal class DfeAnalyticsConfigureOptions(IConfiguration configuration) : IConfigureOptions<DfeAnalyticsOptions>
#pragma warning restore CA1812
{
    private readonly IConfiguration _configuration = configuration;

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
            using var credentialsJsonDoc = JsonDocument.Parse(credentialsJson);

            // We don't have ProjectId configured explicitly; see if it's set in the JSON credentials
            if (options.ProjectId is null &&
                credentialsJsonDoc.RootElement.TryGetProperty("project_id", out var projectIdElement) &&
                projectIdElement.ValueKind == JsonValueKind.String)
            {
                options.ProjectId = projectIdElement.GetString();
            }

            if (credentialsJsonDoc.RootElement.TryGetProperty("private_key", out _) && options.ProjectId is string projectId)
            {
                var creds = GoogleCredential.FromJson(credentialsJson);
                options.BigQueryClient = BigQueryClient.Create(projectId, creds);
            }
        }
    }
}
