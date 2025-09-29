using Microsoft.AspNetCore.Http;

namespace Dfe.Analytics.AspNetCore;

/// <summary>
/// Contains the <see cref="Analytics.Event"/> and <see cref="Microsoft.AspNetCore.Http.HttpContext"/> for a request.
/// </summary>
public sealed class EnrichWebRequestEventContext
{
    private readonly WebRequestEventFeature _feature;

    /// <summary>
    /// Initializes a new instance of <see cref="EnrichWebRequestEventContext"/>.
    /// </summary>
    /// <param name="feature">The <see cref="WebRequestEventFeature"/>.</param>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    public EnrichWebRequestEventContext(WebRequestEventFeature feature, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(feature);
        ArgumentNullException.ThrowIfNull(httpContext);

        _feature = feature;
        HttpContext = httpContext;
    }

    /// <inheritdoc cref="WebRequestEventFeature.Event"/>
    public Event Event => _feature.Event;

    /// <summary>
    /// The <see cref="HttpContext"/> for the request.
    /// </summary>
    public HttpContext HttpContext { get; }

    /// <inheritdoc cref="WebRequestEventFeature.IgnoreEvent"/>
    public void IgnoreEvent() => _feature.IgnoreEvent();
}
