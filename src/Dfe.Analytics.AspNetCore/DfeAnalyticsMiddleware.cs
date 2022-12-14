using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.AspNetCore;

/// <summary>
/// Middleware to write request and response information Google Big Query.
/// </summary>
public class DfeAnalyticsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DfeAnalyticsMiddleware> _logger;

    /// <summary>
    /// Creates a new <see cref="DfeAnalyticsMiddleware"/>.
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="options">The configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public DfeAnalyticsMiddleware(
        RequestDelegate next,
        IOptions<DfeAnalyticsOptions> options,
        ILogger<DfeAnalyticsMiddleware> logger)
    {
        _next = next;
        Options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// The configuration options.
    /// </summary>
    protected DfeAnalyticsOptions Options { get; }

    /// <summary>
    /// Invokes the logic of the middleware.
    /// </summary>
    /// <param name="context">The <see cref="HttpContext"/>.</param>
    /// <returns>A <see cref="Task"/> that completes when the middleware has completed processing.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        Options.ValidateOptions();

        var @event = InitializeEvent(context);
        context.Features.Set(new WebRequestEventFeature(@event));

        PopulateEventFromRequest(@event, context);

        context.Response.OnCompleted(async () =>
        {
            var feature = context.Features.Get<WebRequestEventFeature>();

            if (feature is null || feature.IsEventIgnored || feature.EventSent)
            {
                return;
            }

            var @event = feature.Event;
            PopulateEventFromResponse(@event, context);
            var row = @event.ToBigQueryInsertRow();

            try
            {
                await Options.BigQueryClient.InsertRowAsync(
                    Options.DatasetId,
                    Options.TableId,
                    row);

                _logger.LogInformation("Sent {EventType} event to Big Query for {RequestAddress}", @event.EventType, context.Request.GetEncodedPathAndQuery());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending {EventType} event to BigQuery table for {RequestAddress}", @event.EventType, context.Request.GetEncodedPathAndQuery());
                throw;
            }

            feature.MarkEventSent();
        });

        await _next(context);
    }

    /// <summary>
    /// Initializes a new event for this request.
    /// </summary>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <returns>The initialized event.</returns>
    protected virtual Event InitializeEvent(HttpContext context)
    {
        Options.ValidateOptions();

        return new()
        {
            OccurredAt = DateTime.UtcNow,
            Environment = Options.Environment,
            Namespace = Options.Namespace
        };
    }

    /// <summary>
    /// Populates the request properties on <paramref name="event"/> with the information in <paramref name="context"/>.
    /// </summary>
    /// <param name="event">The event to populate.</param>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="event"/> is <see langword="null"/>.</exception>
    protected virtual void PopulateEventFromRequest(Event @event, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);

        Options.ValidateOptions();
        @event.AnonymizedUserAgentAndIp = GetAnonymizedUserAgentAndIp(context);
        @event.RequestId = context.TraceIdentifier;
        @event.RequestMethod = context.Request.Method;
        @event.RequestPath = context.Request.Path;
        @event.RequestQuery = context.Request.Query.ToDictionary(q => q.Key, q => q.Value.Where(v => v is not null).Select(v => v!).ToArray());
        @event.RequestReferer = context.Request.Headers.Referer;
        @event.RequestUserAgent = context.Request.Headers.UserAgent;
        @event.UserId = Options.GetUserIdFromRequest?.Invoke(context);
    }

    /// <summary>
    /// Gets an anonymized form of the client's IP address and user agent.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="null"/> if <see cref="ConnectionInfo.RemoteIpAddress"/> on <paramref name="context"/> is <see langword="null"/>.
    /// </remarks>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <returns>A <see cref="string"/> with an anonymized form of the client's IP address and user agent.</returns>
    protected virtual string? GetAnonymizedUserAgentAndIp(HttpContext context) =>
        context.Connection.RemoteIpAddress is not null ?
            Event.Anonymize(context.Request.Headers.UserAgent.ToString() + context.Connection.RemoteIpAddress) :
            null;

    /// <summary>
    /// Populates the response properties on <paramref name="event"/> with the information in <paramref name="context"/>.
    /// </summary>
    /// <param name="event">The event to populate.</param>
    /// <param name="context">The current <see cref="HttpContext"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> or <paramref name="event"/> is <see langword="null"/>.</exception>
    protected virtual void PopulateEventFromResponse(Event @event, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(@event);
        ArgumentNullException.ThrowIfNull(context);

        @event.ResponseContentType = context.Response.ContentType;
        @event.ResponseStatus = context.Response.StatusCode.ToString();

        // We may not have been able to get the user the first time around (depending on the order middleware is registered);
        // if UserId is not set then try to get it now.

        @event.UserId ??= Options.GetUserIdFromRequest?.Invoke(context);
    }
}
