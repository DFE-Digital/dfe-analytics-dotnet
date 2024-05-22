using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

internal class FederatedAksAuthenticationConfigureOptions : IConfigureOptions<FederatedAksAuthenticationOptions>
{
    private readonly IConfiguration _configuration;

    public FederatedAksAuthenticationConfigureOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(FederatedAksAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var section = _configuration.GetSection(Constants.RootConfigurationSectionName);

        section.AssignConfigurationValueIfNotEmpty("ProjectId", v => options.ProjectNumber = v);
        section.AssignConfigurationValueIfNotEmpty("WorkloadIdentityPoolName", v => options.WorkloadIdentityPoolName = v);
        section.AssignConfigurationValueIfNotEmpty("WorkloadIdentityPoolProviderName", v => options.WorkloadIdentityPoolProviderName = v);
        section.AssignConfigurationValueIfNotEmpty("ServiceAccountEmail", v => options.ServiceAccountEmail = v);
    }
}
