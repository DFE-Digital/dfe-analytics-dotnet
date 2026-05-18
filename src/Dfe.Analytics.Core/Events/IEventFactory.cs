namespace Dfe.Analytics.Events;

/// <summary>
/// Represents a type that can create a new <see cref="Event"/>.
/// </summary>
public interface IEventFactory
{
    /// <summary>
    /// Creates a new <see cref="Event"/>.
    /// </summary>
    Event CreateEvent(string eventType);
}
