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
    public BigQueryClient? BigQueryClient { get; set; }

    /// <summary>
    /// The federated AKS authentication options.
    /// </summary>
    public FederatedAksAuthenticationOptions? FederatedAksAuthentication { get; set; }

    /// <summary>
    /// The dataset ID to send events to.
    /// </summary>
    [DisallowNull]
    public string? DatasetId { get; set; }

    /// <summary>
    /// The project ID.
    /// </summary>
    public string? ProjectId { get; set; }

    /// <summary>
    /// The JSON credentials for authenticating with Google Cloud.
    /// </summary>
    public string? CredentialsJson { get; set; }
}
