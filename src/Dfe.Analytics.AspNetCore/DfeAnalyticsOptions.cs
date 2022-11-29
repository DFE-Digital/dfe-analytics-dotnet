using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Claims;
using Google.Cloud.BigQuery.V2;
using Microsoft.AspNetCore.Http;

namespace Dfe.Analytics.AspNetCore;

/// <summary>
/// Options to configure <see cref="DfeAnalyticsMiddleware"/>.
/// </summary>
public class DfeAnalyticsOptions
{
    /// <summary>
    /// Creates a new <see cref="DfeAnalyticsOptions"/>.
    /// </summary>
    public DfeAnalyticsOptions()
    {
        Namespace = Assembly.GetEntryAssembly()?.GetName().Name;
        UserIdClaimType = ClaimTypes.NameIdentifier;
        GetUserIdFromRequest = httpContext => httpContext.User.FindFirstValue(UserIdClaimType);
        TableId = "events";
    }

    /// <summary>
    /// The <see cref="BigQueryClient"/> to send events with.
    /// </summary>
    [DisallowNull]
    public BigQueryClient? BigQueryClient { get; set; }

    /// <summary>
    /// The dataset ID to send events to.
    /// </summary>
    [DisallowNull]
    public string? DatasetId { get; set; }

    /// <summary>
    /// The table ID to send events to.
    /// </summary>
    /// <remarks>
    /// The default is <c>events</c>.
    /// </remarks>
    [DisallowNull]
    public string? TableId { get; set; }

    /// <summary>
    /// The environment name.
    /// </summary>
    [DisallowNull]
    public string? Environment { get; set; }

    /// <summary>
    /// The namespace.
    /// </summary>
    /// <remarks>
    /// The default is the current assembly's full name.
    /// </remarks>
    public string? Namespace { get; set; }

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

    [MemberNotNull(nameof(BigQueryClient))]
    [MemberNotNull(nameof(DatasetId))]
    [MemberNotNull(nameof(TableId))]
    [MemberNotNull(nameof(Environment))]
    internal void ValidateOptions()
    {
        if (BigQueryClient is null)
        {
            throw new DfeAnalyticsConfigurationException($"{nameof(BigQueryClient)} has not been configured.");
        }

        if (DatasetId is null)
        {
            throw new DfeAnalyticsConfigurationException($"{nameof(DatasetId)} has not been configured.");
        }

        if (TableId is null)
        {
            throw new DfeAnalyticsConfigurationException($"{nameof(TableId)} has not been configured.");
        }

        if (Environment is null)
        {
            throw new DfeAnalyticsConfigurationException($"{nameof(Environment)} has not been configured.");
        }
    }
}
