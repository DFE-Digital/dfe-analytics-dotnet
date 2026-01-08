using System.Diagnostics.CodeAnalysis;

namespace Dfe.Analytics.Events;

/// <summary>
/// Options to configure events sent to BigQuery.
/// </summary>
public class DfeAnalyticsEventsOptions
{
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
}
