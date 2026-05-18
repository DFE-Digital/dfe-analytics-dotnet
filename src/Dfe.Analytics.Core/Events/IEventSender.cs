namespace Dfe.Analytics.Events;

/// <summary>
/// Represents a type that can send an <see cref="Event"/> to BigQuery.
/// </summary>
public interface IEventSender
{
    /// <summary>
    /// Gets the <see cref="IEventFactory"/>.
    /// </summary>
    IEventFactory EventFactory { get; }

    /// <summary>
    /// Sends an event to BigQuery.
    /// </summary>
    /// <param name="event">The <see cref="Event"/> to send.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SendEventAsync(Event @event);
}

/// <summary>
/// Extension methods for <see cref="IEventSender"/>.
/// </summary>
public static class EventSenderExtensions
{
    /// <inheritdoc cref="IEventFactory.CreateEvent"/>
    public static Event CreateEvent(this IEventSender eventSender, string eventName)
    {
        ArgumentNullException.ThrowIfNull(eventSender);

        return eventSender.EventFactory.CreateEvent(eventName);
    }
}
