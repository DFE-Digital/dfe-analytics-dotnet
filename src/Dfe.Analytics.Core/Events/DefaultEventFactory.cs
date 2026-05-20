using Microsoft.Extensions.Options;

namespace Dfe.Analytics.Events;

internal class DefaultEventFactory(IOptions<DfeAnalyticsEventsOptions> optionsAccessor, TimeProvider timeProvider) : IEventFactory
{
    public Event CreateEvent(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        var options = optionsAccessor.Value;

        return new Event
        {
            EventType = eventType,
            Environment = options.Environment!,
            Namespace = options.Namespace,
            OccurredAt = timeProvider.GetUtcNow().DateTime
        };
    }
}
