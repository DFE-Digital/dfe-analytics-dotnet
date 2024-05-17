using System.Diagnostics.CodeAnalysis;
using Google.Cloud.BigQuery.V2;

namespace Dfe.Analytics;

/// <summary>
/// Configuration for DfE Analytics.
/// </summary>
public class DfeAnalyticsOptions
{
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

    [MemberNotNull(nameof(BigQueryClient))]
    [MemberNotNull(nameof(DatasetId))]
    [MemberNotNull(nameof(TableId))]
    [MemberNotNull(nameof(Environment))]
    internal void ValidateOptions()
    {
        if (BigQueryClient is null)
        {
            throw new InvalidOperationException($"{nameof(BigQueryClient)} has not been configured.");
        }

        if (DatasetId is null)
        {
            throw new InvalidOperationException($"{nameof(DatasetId)} has not been configured.");
        }

        if (TableId is null)
        {
            throw new InvalidOperationException($"{nameof(TableId)} has not been configured.");
        }

        if (Environment is null)
        {
            throw new InvalidOperationException($"{nameof(Environment)} has not been configured.");
        }
    }
}
