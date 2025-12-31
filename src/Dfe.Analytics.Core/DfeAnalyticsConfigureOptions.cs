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
    }
}
