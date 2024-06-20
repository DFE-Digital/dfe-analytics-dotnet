using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.AspNetCore;

internal class DfeAnalyticsAspNetCoreConfigureOptions : IConfigureOptions<DfeAnalyticsAspNetCoreOptions>
{
    private readonly IConfiguration _configuration;

    public DfeAnalyticsAspNetCoreConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(DfeAnalyticsAspNetCoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var section = _configuration.GetSection(Constants.RootConfigurationSectionName).GetSection("AspNetCore");

        section.AssignConfigurationValueIfNotEmpty("UserIdClaimType", v => options.UserIdClaimType = v);
        section.AssignConfigurationValueIfNotEmpty("PseudonymizeUserId", v => options.PseudonymizeUserId = bool.Parse(v));
        section.AssignConfigurationValueIfNotEmpty("RestoreOriginalPathAndQueryString", v => options.RestoreOriginalPathAndQueryString = bool.Parse(v));
        section.AssignConfigurationValueIfNotEmpty("RestoreOriginalStatusCode", v => options.RestoreOriginalStatusCode = bool.Parse(v));
    }
}
