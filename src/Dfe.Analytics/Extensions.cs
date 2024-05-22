using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

/// <summary>
/// Extension methods for configuring DfE Analytics.
/// </summary>
public static class Extensions
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
        services.TryAddSingleton<TimeProvider>(_ => TimeProvider.System);
        services.Configure(setupAction);
        services.TryAddSingleton<IBigQueryClientProvider, OptionsBigQueryClientProvider>();

        return new DfeAnalyticsBuilder(services);
    }

    /// <summary>
    /// Registers <see cref="AksFederatedBigQueryClientProvider"/> as the <see cref="IBigQueryClientProvider"/>.
    /// </summary>
    /// <param name="builder">The <see cref="DfeAnalyticsBuilder"/>.</param>
    /// <returns>The <see cref="DfeAnalyticsBuilder"/> so that additional calls can be chained.</returns>
    public static DfeAnalyticsBuilder UseFederatedAksBigQueryClientProvider(this DfeAnalyticsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.UseFederatedAksBigQueryClientProvider(_ => { });
    }

    /// <summary>
    /// Registers <see cref="AksFederatedBigQueryClientProvider"/> and configures as the <see cref="IBigQueryClientProvider"/>.
    /// </summary>
    /// <param name="builder">The <see cref="DfeAnalyticsBuilder"/>.</param>
    /// <param name="setupAction">
    /// An <see cref="Action{FederatedAksAuthenticationOptions}"/> to configure the provided <see cref="FederatedAksAuthenticationOptions"/>.
    /// </param>
    /// <returns>The <see cref="DfeAnalyticsBuilder"/> so that additional calls can be chained.</returns>
    public static DfeAnalyticsBuilder UseFederatedAksBigQueryClientProvider(this DfeAnalyticsBuilder builder, Action<FederatedAksAuthenticationOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        builder.Services.AddSingleton<IBigQueryClientProvider, AksFederatedBigQueryClientProvider>();
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<FederatedAksAuthenticationOptions>, FederatedAksAuthenticationConfigureOptions>());
        builder.Services.Configure(setupAction);

        return builder;
    }
}
