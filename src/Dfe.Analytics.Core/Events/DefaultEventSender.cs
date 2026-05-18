using Microsoft.Extensions.Options;

namespace Dfe.Analytics.Events;

internal class DefaultEventSender(
    IEventFactory eventFactory,
    IOptions<DfeAnalyticsOptions> coreOptionsAccessor,
    IOptions<DfeAnalyticsEventsOptions> eventsOptionsAccessor) :
    IEventSender
{
    public IEventFactory EventFactory => eventFactory;

    public Task SendEventAsync(Event @event)
    {
        var coreOptions = coreOptionsAccessor.Value;
        var eventsOptions = eventsOptionsAccessor.Value;

        var bigQueryClient = coreOptions.BigQueryClient ?? throw new InvalidOperationException("No BigQueryClient configured.");

        var row = @event.ToBigQueryInsertRow();

        return bigQueryClient.InsertRowAsync(
            coreOptions.DatasetId,
            eventsOptions.TableId,
            row);
    }
}
