using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

/// <summary>
/// Extension methods for configuring DfE Analytics.
/// </summary>
#pragma warning disable CA1724
public static class Extensions
#pragma warning disable CA1724
{
    /// <summary>
    /// Adds DfE Analytics services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <returns>The <see cref="DfeAnalyticsBuilder"/> so that additional calls can be chained.</returns>
    public static DfeAnalyticsBuilder AddDfeAnalytics(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddDfeAnalytics(_ => { });
    }

    /// <summary>
    /// Adds DfE Analytics services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" />.</param>
    /// <param name="setupAction">
    /// An <see cref="Action{DfeAnalyticsAspNetCoreOptions}"/> to configure the provided <see cref="DfeAnalyticsOptions"/>.
    /// </param>
    /// <returns>The <see cref="DfeAnalyticsBuilder"/> so that additional calls can be chained.</returns>
    public static DfeAnalyticsBuilder AddDfeAnalytics(this IServiceCollection services, Action<DfeAnalyticsOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(setupAction);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<DfeAnalyticsOptions>, DfeAnalyticsConfigureOptions>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<DfeAnalyticsOptions>, DfeAnalyticsPostConfigureOptions>());
        services.TryAddSingleton(_ => TimeProvider.System);
        services.Configure(setupAction);

        return new DfeAnalyticsBuilder(services);
    }
}
