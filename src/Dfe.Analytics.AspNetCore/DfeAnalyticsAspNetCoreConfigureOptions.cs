using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.AspNetCore;

internal class DfeAnalyticsAspNetCoreConfigureOptions(IConfiguration configuration) : IConfigureOptions<DfeAnalyticsAspNetCoreOptions>
{
    public void Configure(DfeAnalyticsAspNetCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var section = configuration.GetSection(Constants.ConfigurationSectionName).GetSection("AspNetCore");

        section.AssignConfigurationValueIfNotEmpty("UserIdClaimType", v => options.UserIdClaimType = v);
        section.AssignConfigurationValueIfNotEmpty("RestoreOriginalPathAndQueryString", v => options.RestoreOriginalPathAndQueryString = bool.Parse(v));
        section.AssignConfigurationValueIfNotEmpty("RestoreOriginalStatusCode", v => options.RestoreOriginalStatusCode = bool.Parse(v));
    }
}
