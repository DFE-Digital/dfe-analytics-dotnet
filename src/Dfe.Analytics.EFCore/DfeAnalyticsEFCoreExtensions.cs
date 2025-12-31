using Dfe.Analytics.EFCore.AirbyteApi;
using Dfe.Analytics.EFCore.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.EFCore;

// ReSharper disable once InconsistentNaming
public static class DfeAnalyticsEFCoreExtensions
{
    public static DfeAnalyticsBuilder AddDeploymentTools(this DfeAnalyticsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddOptions<AirbyteApiOptions>()
            .ValidateOnStart();
        builder.Services.AddTransient<IPostConfigureOptions<AirbyteApiOptions>, ConfigureAirbyteApiOptionsFromEnvironment>();

        builder.Services
            .AddSingleton<AirbyteApiClient>()
            .AddSingleton<AnalyticsConfigurationProvider>()
            .AddTransient<AnalyticsDeployer>();

        var airbyteApiClientBuilder = builder.Services.AddHttpClient<AirbyteApiClient>();
        AirbyteApiClient.ConfigureHttpClient(airbyteApiClientBuilder);

        return builder;
    }

    public static DfeAnalyticsBuilder ConfigureAirbyteApi(this DfeAnalyticsBuilder builder, Action<AirbyteApiOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        builder.Services.Configure(configureOptions);

        return builder;
    }
}
