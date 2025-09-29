using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace Dfe.Analytics.AspNetCore;

/// <summary>
/// Options to configure <see cref="DfeAnalyticsMiddleware"/>.
/// </summary>
public class DfeAnalyticsAspNetCoreOptions
{
    /// <summary>
    /// A delegate that returns the signed in user's ID, if any.
    /// </summary>
    /// <remarks>
    /// The default returns the value of the first <see cref="UserIdClaimType"/> claim from <see cref="HttpContext.User"/> property.
    /// </remarks>
    public Func<HttpContext, string?>? GetUserIdFromRequest { get; set; }

    /// <summary>
    /// The claim type that contains the signed in user's ID.
    /// </summary>
    public string? UserIdClaimType { get; set; }

    /// <summary>
    /// A filter that controls whether a web request event is sent for a given <see cref="HttpContext"/>.
    /// </summary>
    public Func<HttpContext, bool>? RequestFilter { get; set; }

    /// <summary>
    /// Limits the rate of web request events sent to BigQuery.
    /// </summary>
    public PartitionedRateLimiter<HttpContext>? RateLimiter { get; set; }

    /// <summary>
    /// Whether the original path and query string should be used for requests that have been reexecuted
    /// using StatusCodePagesMiddleware or ExceptionHandlerMiddleware.
    /// </summary>
    /// <remarks>
    /// The default is <see langword="true"/>.
    /// </remarks>
    public bool RestoreOriginalPathAndQueryString { get; set; } = true;

    /// <summary>
    /// Whether the original response status code should be used for requests that have been reexecuted
    /// using StatusCodePagesMiddleware.
    /// </summary>
    public bool RestoreOriginalStatusCode { get; set; } = false;

    /// <summary>
    /// Gets the current user's ID from the <see cref="HttpContext"/> using the <see cref="UserIdClaimType"/> claim.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/>.</param>
    /// <returns>The value of the <see cref="UserIdClaimType"/> if set or <see langword="null"/>.</returns>
    /// <exception cref="InvalidOperationException"><see cref="UserIdClaimType"/> is not configured.</exception>
    public string? GetUserIdFromUserClaims(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        if (UserIdClaimType is null)
        {
            throw new InvalidOperationException($"{nameof(UserIdClaimType)} is not configured.");
        }

        return httpContext.User.FindFirstValue(UserIdClaimType);
    }
}
