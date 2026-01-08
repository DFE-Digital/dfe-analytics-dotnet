using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Analytics;

/// <summary>
/// Used to configure DfE Analytics.
/// </summary>
public class DfeAnalyticsBuilder
{
    /// <summary>
    /// Initializes a new instance of <see cref="DfeAnalyticsBuilder"/>.
    /// </summary>
    /// <param name="services">The services being configured.</param>
    public DfeAnalyticsBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        Services = services;
    }

    /// <summary>
    /// The services being configured.
    /// </summary>
    public virtual IServiceCollection Services { get; }
}
