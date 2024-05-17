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

        AssignConfigurationValueIfNotEmpty("UserIdClaimType", v => options.UserIdClaimType = v);
        AssignConfigurationValueIfNotEmpty("PseudonymizeUserId", v => options.PseudonymizeUserId = bool.Parse(v));

        void AssignConfigurationValueIfNotEmpty(string configKey, Action<string> assignValue)
        {
            var value = section[configKey];

            if (value is not null)
            {
                assignValue(value);
            }
        }
    }
}
