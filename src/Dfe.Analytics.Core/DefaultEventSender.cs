using Dfe.Analytics.Events;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

internal class DefaultEventSender(
    IOptions<DfeAnalyticsOptions> coreOptionsAccessor,
    IOptions<DfeAnalyticsEventsOptions> eventsOptionsAccessor) :
    IEventSender
{
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
