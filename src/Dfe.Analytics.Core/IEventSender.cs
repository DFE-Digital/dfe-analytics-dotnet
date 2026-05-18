using Dfe.Analytics.Events;

namespace Dfe.Analytics;

/// <summary>
/// Represents a type that can send an <see cref="Event"/> to BigQuery.
/// </summary>
public interface IEventSender
{
    /// <summary>
    /// Sends an event to BigQuery.
    /// </summary>
    /// <param name="event">The <see cref="Event"/> to send.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task SendEventAsync(Event @event);
}
