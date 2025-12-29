using Dfe.Analytics.EFCore.AirbyteApi;
using Dfe.Analytics.EFCore.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.EFCore;

#pragma warning disable CA1724
public static class Extensions
#pragma warning restore CA1724
{
    public static IHostApplicationBuilder AddDfeAnalyticsDeploymentTools(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddDfeAnalyticsDeploymentTools();

        builder.Services.Configure<AirbyteApiOptions>(options =>
        {
            builder.Configuration.GetSection("DfeAnalytics:Airbyte").Bind(options);
        });

        return builder;
    }

    public static IServiceCollection AddDfeAnalyticsDeploymentTools(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<AirbyteApiOptions>()
            .ValidateOnStart();
        services.AddTransient<IPostConfigureOptions<AirbyteApiOptions>, ConfigureAirbyteApiOptionsFromEnvironment>();

        services
            .AddSingleton<AirbyteApiClient>()
            .AddSingleton<AnalyticsConfigurationProvider>()
            .AddTransient<AnalyticsDeployer>();

        var airbyteApiClientBuilder = services.AddHttpClient<AirbyteApiClient>();
        AirbyteApiClient.ConfigureHttpClient(airbyteApiClientBuilder);

        return services;
    }

    public static IServiceCollection ConfigureAirbyteApi(this IServiceCollection services, Action<AirbyteApiOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);

        return services;
    }
}
