using Dfe.Analytics.Events;
using Google.Cloud.BigQuery.V2;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.AspNetCore;

/// <summary>
/// Middleware to write request and response information Google BigQuery.
/// </summary>
public class DfeAnalyticsMiddleware
{
    private const string EventType = "web_request";

    private readonly RequestDelegate _next;
    private readonly IEventFactory _eventFactory;
    private readonly IEventSender _eventSender;

    /// <summary>
    /// Creates a new <see cref="DfeAnalyticsMiddleware"/>.
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="eventFactory">The <see cref="IEventFactory"/> to create events with.</param>
    /// <param name="eventSender">The <see cref="IEventSender"/> to send events with.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
    /// <param name="optionsAccessor">The configuration options.</param>
    /// <param name="aspNetCoreOptionsAccessor">The middleware configuration options.</param>
    public DfeAnalyticsMiddleware(
        RequestDelegate next,
        IEventFactory eventFactory,
        IEventSender eventSender,
        TimeProvider timeProvider,
        IOptions<DfeAnalyticsOptions> optionsAccessor,
        IOptions<DfeAnalyticsAspNetCoreOptions> aspNetCoreOptionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(eventFactory);
        ArgumentNullException.ThrowIfNull(eventSender);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        ArgumentNullException.ThrowIfNull(aspNetCoreOptionsAccessor);

        _next = next;
        _eventFactory = eventFactory;
        _eventSender = eventSender;
        TimeProvider = timeProvider;
        Options = optionsAccessor.Value;
        AspNetCoreOptions = aspNetCoreOptionsAccessor.Value;
    }

    /// <summary>
    /// The configuration options.
    /// </summary>
    protected DfeAnalyticsOptions Options { get; }

    /// <summary>
    /// The middleware configuration options.
    /// </summary>
    protected DfeAnalyticsAspNetCoreOptions AspNetCoreOptions { get; }

    /// <summary>
    /// The <see cref="TimeProvider"/>.
    /// </summary>
    protected TimeProvider TimeProvider { get; }

    /// <summary>
    /// Invokes the logic of the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the middleware has completed processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        ValidateOptions();

        var @event = _eventFactory.CreateEvent(EventType);
        context.Features.Set(new WebRequestEventFeature(@event));
        await _eventSender.SendEventAsync(@event);

        await _next(context);
    }

    internal void ValidateOptions()
    {
        if (Options.BigQueryClient is null)
        {
            throw new InvalidOperationException($"{nameof(BigQueryClient)} has not been configured.");
        }

        if (Options.DatasetId is null)
        {
            throw new InvalidOperationException($"{nameof(Options.DatasetId)} has not been configured.");
        }

        if (AspNetCoreOptions.TableId is null)
        {
            throw new InvalidOperationException($"{nameof(AspNetCoreOptions.TableId)} has not been configured.");
        }

        if (AspNetCoreOptions.Environment is null)
        {
            throw new InvalidOperationException($"{nameof(AspNetCoreOptions.Environment)} has not been configured.");
        }
    }
}
