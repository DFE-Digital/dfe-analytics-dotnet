using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.Events;

internal class DfeAnalyticsEventsConfigureOptions(IConfiguration configuration) : IConfigureOptions<DfeAnalyticsEventsOptions>
{
    public void Configure(DfeAnalyticsEventsOptions options)
    {
        var configurationSection = configuration.GetSection(Constants.ConfigurationSectionName);

        options.Namespace ??= Assembly.GetEntryAssembly()?.GetName().Name;
        options.TableId ??= "events";
        configurationSection.AssignConfigurationValueIfNotEmpty("Environment", v => options.Environment = v);
        configurationSection.AssignConfigurationValueIfNotEmpty("Namespace", v => options.Namespace = v);
        configurationSection.AssignConfigurationValueIfNotEmpty("TableId", v => options.TableId = v);
    }
}
