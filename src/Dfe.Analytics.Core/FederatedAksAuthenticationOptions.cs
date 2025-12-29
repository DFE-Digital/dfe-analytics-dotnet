namespace Dfe.Analytics;

/// <summary>
/// Configuration for federated AKS authentication.
/// </summary>
public class FederatedAksAuthenticationOptions
{
    /// <summary>
    /// The workflow identity pool provider audience.
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// The URL for the service account impersonation request.
    /// </summary>
#pragma warning disable CA1056
    public string? ServiceAccountImpersonationUrl { get; set; }
#pragma warning restore CA1056
}
