using Microsoft.AspNetCore.Http;

namespace Dfe.Analytics.AspNetCore;

/// <summary>
/// Provides access to the <see cref="Event"/> for the current request.
/// </summary>
public class WebRequestEventFeature
{
    /// <summary>
    /// Creates a new <see cref="WebRequestEventFeature"/>.
    /// </summary>
    /// <param name="event">The <see cref="Event"/> for the current request.</param>
    /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
    public WebRequestEventFeature(Event @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        Event = @event;
    }

    /// <summary>
    /// Gets the <see cref="Event"/> for the current request.
    /// </summary>
    public Event Event { get; }

    /// <summary>
    /// Gets whether the event has been ignored.
    /// </summary>
    public bool IsEventIgnored { get; private set; }

    /// <summary>
    /// Marks the event ignored and does not send it to BigQuery.
    /// </summary>
    /// <exception cref="InvalidOperationException">The event has already been sent.</exception>
    public void IgnoreEvent()
    {
        if (EventSent)
        {
            throw new InvalidOperationException("The event has already been sent.");
        }

        IsEventIgnored = true;
    }

    /// <summary>
    /// Whether an event has been sent for this <see cref="HttpContext"/>.
    /// </summary>
    internal bool EventSent { get; private set; } = false;

    internal void MarkEventSent()
    {
        if (EventSent)
        {
            throw new InvalidOperationException("Event has already been sent.");
        }

        EventSent = true;
    }
}
