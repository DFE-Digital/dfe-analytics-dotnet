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
    private readonly IBigQueryClientProvider _bigQueryClientProvider;
    private readonly IEnumerable<IWebRequestEventEnricher> _webRequestEventEnrichers;
    private readonly ILogger<DfeAnalyticsMiddleware> _logger;

    /// <summary>
    /// Creates a new <see cref="DfeAnalyticsMiddleware"/>.
    /// </summary>
    /// <param name="next">The <see cref="RequestDelegate"/> representing the next middleware in the pipeline.</param>
    /// <param name="bigQueryClientProvider">The <see cref="IBigQueryClientProvider"/>.</param>
    /// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
    /// <param name="optionsAccessor">The configuration options.</param>
    /// <param name="aspNetCoreOptionsAccessor">The middleware configuration options.</param>
    /// <param name="webRequestEventEnrichers">The collection of <see cref="IWebRequestEventEnricher"/>.</param>
    /// <param name="logger">The logger instance.</param>
    public DfeAnalyticsMiddleware(
        RequestDelegate next,
        IBigQueryClientProvider bigQueryClientProvider,
        TimeProvider timeProvider,
        IOptions<DfeAnalyticsOptions> optionsAccessor,
        IOptions<DfeAnalyticsAspNetCoreOptions> aspNetCoreOptionsAccessor,
        IEnumerable<IWebRequestEventEnricher> webRequestEventEnrichers,
        ILogger<DfeAnalyticsMiddleware> logger)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(bigQueryClientProvider);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        ArgumentNullException.ThrowIfNull(aspNetCoreOptionsAccessor);
        ArgumentNullException.ThrowIfNull(webRequestEventEnrichers);
        ArgumentNullException.ThrowIfNull(logger);

        _next = next;
        _bigQueryClientProvider = bigQueryClientProvider;
        TimeProvider = timeProvider;
        _webRequestEventEnrichers = webRequestEventEnrichers;
        Options = optionsAccessor.Value;
        AspNetCoreOptions = aspNetCoreOptionsAccessor.Value;
        _logger = logger;
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

        Options.ValidateOptions();

        var @event = InitializeEvent(context);
        context.Features.Set(new WebRequestEventFeature(@event));

        PopulateEventFromRequest(@event, context);

        context.Response.OnCompleted(async () =>
        {
            var feature = context.Features.Get<WebRequestEventFeature>();

            if (feature is null || feature.IsEventIgnored || feature.EventSent || AspNetCoreOptions.RequestFilter?.Invoke(context) == false)
            {
                return;
            }

            var @event = feature.Event;
            PopulateEventFromResponse(@event, context);

            var enrichContext = new EnrichWebRequestEventContext(feature, context);
            foreach (var enricher in _webRequestEventEnrichers)
            {
                await enricher.EnrichEvent(enrichContext);
            }

            if (feature.IsEventIgnored)
            {
                return;
            }

            var row = @event.ToBigQueryInsertRow();

            try
            {
                var bigQueryClient = await _bigQueryClientProvider.GetBigQueryClientAsync();

                await bigQueryClient.InsertRowAsync(
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
            OccurredAt = TimeProvider.GetUtcNow().UtcDateTime,
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
        @event.UserId = AspNetCoreOptions.GetUserIdFromRequest?.Invoke(context);

        if (@event.UserId is not null && AspNetCoreOptions.PseudonymizeUserId)
        {
            @event.UserId = Event.Pseudonymize(@event.UserId);
        }
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
            Event.Pseudonymize(context.Request.Headers.UserAgent.ToString() + context.Connection.RemoteIpAddress) :
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

        @event.UserId ??= AspNetCoreOptions.GetUserIdFromRequest?.Invoke(context);
    }
}
