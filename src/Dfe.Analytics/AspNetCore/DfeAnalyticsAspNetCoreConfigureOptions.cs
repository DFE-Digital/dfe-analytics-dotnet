using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.AspNetCore;

#pragma warning disable CA1812
internal class DfeAnalyticsAspNetCoreConfigureOptions(IConfiguration configuration) : IConfigureOptions<DfeAnalyticsAspNetCoreOptions>
#pragma warning restore CA1812
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(DfeAnalyticsAspNetCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var section = _configuration.GetSection(Constants.RootConfigurationSectionName).GetSection("AspNetCore");

        section.AssignConfigurationValueIfNotEmpty("UserIdClaimType", v => options.UserIdClaimType = v);
        section.AssignConfigurationValueIfNotEmpty("RestoreOriginalPathAndQueryString", v => options.RestoreOriginalPathAndQueryString = bool.Parse(v));
        section.AssignConfigurationValueIfNotEmpty("RestoreOriginalStatusCode", v => options.RestoreOriginalStatusCode = bool.Parse(v));
    }
}
