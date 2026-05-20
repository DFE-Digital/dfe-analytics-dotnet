using System.Globalization;
using System.Threading.RateLimiting;
using Dfe.Analytics.Events;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.AspNetCore;

internal class AspNetCoreEventSender : IEventSender, IEventFactory
{
    private readonly DefaultEventFactory _innerEventFactory;
    private readonly DefaultEventSender _innerSender;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IEnumerable<IWebRequestEventEnricher> _webRequestEventEnrichers;
    private readonly IOptions<DfeAnalyticsAspNetCoreOptions> _optionsAccessor;
    private readonly ILogger<AspNetCoreEventSender> _logger;

    public AspNetCoreEventSender(IHttpContextAccessor httpContextAccessor,
        TimeProvider timeProvider,
        IEnumerable<IWebRequestEventEnricher> webRequestEventEnrichers,
        IOptions<DfeAnalyticsAspNetCoreOptions> optionsAccessor,
        IOptions<DfeAnalyticsOptions> coreOptionsAccessor,
        ILogger<AspNetCoreEventSender> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _webRequestEventEnrichers = webRequestEventEnrichers;
        _optionsAccessor = optionsAccessor;
        _logger = logger;
        _innerEventFactory = new DefaultEventFactory(optionsAccessor, timeProvider);
        _innerSender = new DefaultEventSender(this, coreOptionsAccessor, optionsAccessor);
    }

    public IEventFactory EventFactory => this;

    public Event CreateEvent(string eventType)
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext.");

        var @event = _innerEventFactory.CreateEvent(eventType);
        PopulateEventFromRequest(@event, httpContext);

        return @event;
    }

    public async Task SendEventAsync(Event @event)
    {
        var httpContext = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HttpContext.");

        if (httpContext.Response.HasStarted)
        {
            await SendEventCoreAsync();
        }
        else
        {
            httpContext.Response.OnCompleted(SendEventCoreAsync);
        }

        async Task SendEventCoreAsync()
        {
            var options = _optionsAccessor.Value;

            RateLimitLease? rateLimitLease = null;
            try
            {
                if (options.RequestFilter?.Invoke(httpContext) == false)
                {
                    return;
                }

                PopulateEventFromResponse(@event, httpContext);

                var enrichContext = new EnrichWebRequestEventContext(@event, httpContext);
                foreach (var enricher in _webRequestEventEnrichers)
                {
                    await enricher.EnrichEventAsync(enrichContext);
                }

                // If this event is the web_request event, check if it's been ignored (or sent already)
                WebRequestEventFeature? webRequestEventFeature = httpContext.Features.Get<WebRequestEventFeature>();
                if (webRequestEventFeature is not null && ReferenceEquals(webRequestEventFeature.Event, @event) &&
                    (webRequestEventFeature.EventSent || webRequestEventFeature.IsEventIgnored))
                {
                    return;
                }

                if (options.RateLimiter is not null)
                {
                    rateLimitLease = await options.RateLimiter.AcquireAsync(httpContext);

                    if (!rateLimitLease.IsAcquired)
                    {
                        _logger.LogDebug("Event for {RequestAddress} was dropped due to an exceeded rate limit", httpContext.Request.GetEncodedPathAndQuery());
                        return;
                    }
                }

                await _innerSender.SendEventAsync(@event);

                webRequestEventFeature?.MarkEventSent();

                _logger.LogInformation("Sent {EventType} event to Big Query for {RequestAddress}", @event.EventType, httpContext.Request.GetEncodedPathAndQuery());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed sending {EventType} event to BigQuery table for {RequestAddress}", @event.EventType, httpContext.Request.GetEncodedPathAndQuery());
                throw;
            }
            finally
            {
                rateLimitLease?.Dispose();
            }
        }
    }

    private void PopulateEventFromRequest(Event @event, HttpContext context)
    {
        var options = _optionsAccessor.Value;

        @event.AnonymizedUserAgentAndIp = GetAnonymizedUserAgentAndIp(context);
        @event.RequestId = context.TraceIdentifier;
        @event.RequestMethod = context.Request.Method;
        @event.RequestPath = context.Request.PathBase + context.Request.Path;
        @event.RequestQuery = context.Request.Query
            .ToDictionary(q => q.Key, q => q.Value.Where(v => v is not null).Select(v => v!).ToArray());
        @event.RequestReferer = context.Request.Headers.Referer;
        @event.RequestUserAgent = context.Request.Headers.UserAgent;
        @event.UserId = options.GetUserIdFromRequest?.Invoke(context);

        if (options.RestoreOriginalPathAndQueryString)
        {
            if (context.Features.Get<IExceptionHandlerFeature>() is IExceptionHandlerFeature exceptionHandlerFeature)
            {
                @event.RequestPath = context.Request.PathBase + exceptionHandlerFeature.Path;
            }
            else if (context.Features.Get<IStatusCodeReExecuteFeature>() is IStatusCodeReExecuteFeature statusCodeReExecuteFeature)
            {
                @event.RequestPath = statusCodeReExecuteFeature.OriginalPathBase + statusCodeReExecuteFeature.OriginalPath;
                @event.RequestQuery = QueryHelpers.ParseQuery(statusCodeReExecuteFeature.OriginalQueryString)
                    .ToDictionary(q => q.Key, q => q.Value.Where(v => v is not null).Select(v => v!).ToArray());
            }
        }
    }

    private void PopulateEventFromResponse(Event @event, HttpContext context)
    {
        var options = _optionsAccessor.Value;

        @event.ResponseContentType = context.Response.ContentType;
        @event.ResponseStatus = context.Response.StatusCode.ToString(CultureInfo.InvariantCulture);

        if (options.RestoreOriginalStatusCode &&
            context.Features.Get<IStatusCodeReExecuteFeature>() is IStatusCodeReExecuteFeature statusCodeReExecuteFeature)
        {
            @event.ResponseStatus = statusCodeReExecuteFeature.OriginalStatusCode.ToString(CultureInfo.InvariantCulture);
        }

        // We may not have been able to get the user the first time around (depending on the order middleware is registered);
        // if UserId is not set then try to get it now.

        @event.UserId ??= options.GetUserIdFromRequest?.Invoke(context);
    }

    private string? GetAnonymizedUserAgentAndIp(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return context.Connection.RemoteIpAddress is not null ? Event.Anonymize(context.Request.Headers.UserAgent + context.Connection.RemoteIpAddress) : null;
    }
}
