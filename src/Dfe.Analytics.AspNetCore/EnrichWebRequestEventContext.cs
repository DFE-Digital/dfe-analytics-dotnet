using Dfe.Analytics.Events;
using Microsoft.AspNetCore.Http;

namespace Dfe.Analytics.AspNetCore;

/// <summary>
/// Contains the <see cref="Events.Event"/> and <see cref="Microsoft.AspNetCore.Http.HttpContext"/> for a request.
/// </summary>
public sealed class EnrichWebRequestEventContext
{
    private readonly Event _event;

    /// <summary>
    /// Initializes a new instance of <see cref="EnrichWebRequestEventContext"/>.
    /// </summary>
    /// <param name="event">The <see cref="Event"/>.</param>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    public EnrichWebRequestEventContext(Event @event, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(httpContext);

        _event = @event;
        HttpContext = httpContext;
    }

    /// <summary>
    /// The <see cref="Event"/>.
    /// </summary>
    public Event Event => _event;

    /// <summary>
    /// The <see cref="HttpContext"/> for the request.
    /// </summary>
    public HttpContext HttpContext { get; }
}
