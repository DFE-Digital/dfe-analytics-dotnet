using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.AspNetCore;

/// <summary>
/// Extension methods for using DfE Analytics in an ASP.NET Core application.
/// </summary>
public static class AspNetCoreExtensions
{
    /// <summary>
    /// Adds ASP.NET Core integration for DfE Analytics.
    /// </summary>
    /// <param name="builder">The <see cref="DfeAnalyticsBuilder" />.</param>
    /// <returns>The <see cref="DfeAnalyticsBuilder"/> so that additional calls can be chained.</returns>
    public static DfeAnalyticsBuilder AddAspNetCoreIntegration(this DfeAnalyticsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddAspNetCoreIntegration(_ => { });
    }

    /// <summary>
    /// Adds ASP.NET Core integration for DfE Analytics.
    /// </summary>
    /// <param name="builder">The <see cref="DfeAnalyticsBuilder" />.</param>
    /// <param name="setupAction">
    /// An <see cref="Action{DfeAnalyticsAspNetCoreOptions}"/> to configure the provided <see cref="DfeAnalyticsAspNetCoreOptions"/>.
    /// </param>
    /// <returns>The <see cref="DfeAnalyticsBuilder"/> so that additional calls can be chained.</returns>
    public static DfeAnalyticsBuilder AddAspNetCoreIntegration(
        this DfeAnalyticsBuilder builder,
        Action<DfeAnalyticsAspNetCoreOptions> setupAction)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(setupAction);

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<DfeAnalyticsAspNetCoreOptions>, DfeAnalyticsAspNetCoreConfigureOptions>());
        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<DfeAnalyticsAspNetCoreOptions>, DfeAnalyticsAspNetCorePostConfigureOptions>());
        builder.Services.Configure(setupAction);

        return builder;
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
    /// <returns>The <see cref="Event"/> or <see langword="null"/> if the <see cref="WebRequestEventFeature"/> is missing.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="httpContext"/> is <see langword="null" />.</exception>
    public static Event? GetWebRequestEvent(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var feature = httpContext.Features.Get<WebRequestEventFeature>();
        return feature?.Event;
    }

    /// <summary>
    /// Indicates that the web request event should not be sent to BigQuery.
    /// </summary>
    /// <exception cref="InvalidOperationException">The event has already been sent.</exception>
    public static void IgnoreWebRequestEvent(this HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var feature = httpContext.Features.Get<WebRequestEventFeature>();
        feature?.IgnoreEvent();
    }
}
