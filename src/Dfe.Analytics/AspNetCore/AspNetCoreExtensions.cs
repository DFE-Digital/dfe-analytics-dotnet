using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.AspNetCore;

/// <summary>
/// Extension methods for using DfE Analytics in an ASP.NET Core application.
/// </summary>
public static class AspNetCoreExtensions
{
    /// <summary>
    /// Adds DfE Analytics services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="setupAction">
    /// An <see cref="Action{DfeAnalyticsOptions}"/> to configure the provided <see cref="DfeAnalyticsOptions"/>.
    /// </param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddDfeAnalytics(
        this IServiceCollection services,
        Action<DfeAnalyticsOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(setupAction);

        // Ensure DefaultDfeAnalyticsConfigureOptions is the first IConfigureOptions<DfeAnalyticsOptions> to be resolved
        services.Insert(
            0,
            new ServiceDescriptor(
                typeof(IConfigureOptions<DfeAnalyticsOptions>),
                typeof(DefaultDfeAnalyticsConfigureOptions),
                ServiceLifetime.Singleton));

        services.Configure(setupAction);

        return services;
    }

    /// <summary>
    /// Adds a <see cref="DfeAnalyticsMiddleware"/> to the specified <see cref="IApplicationBuilder"/>.
    /// </summary>
    /// <remarks>
    /// The <see cref="DfeAnalyticsMiddleware"/> will insert a row into BigQuery for every request made to the application.
    /// </remarks>
    /// <param name="app">The <see cref="IApplicationBuilder"/> to add the middleware to.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IApplicationBuilder UseDfeAnalytics(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseMiddleware<DfeAnalyticsMiddleware>();

        return app;
    }

    /// <summary>
    /// Gets the <see cref="Event"/> for the specified <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>The <see cref="Event"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="httpContext"/> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// The <paramref name="httpContext"/> has no <see cref="Event"/> assigned.
    /// </exception>
    public static Event GetWebRequestEvent(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var feature = httpContext.Features.Get<WebRequestEventFeature>();

        if (feature is null)
        {
            ThrowFeatureMissingException();
        }

        return feature.Event;
    }

    [DoesNotReturn]
    private static void ThrowFeatureMissingException() =>
        throw new InvalidOperationException(
            $"{nameof(HttpContext)} does not contain a {nameof(WebRequestEventFeature)} feature. " +
            $"Ensure app.{nameof(UseDfeAnalytics)}() has been called from your startup class.");
}
