using System.Diagnostics.CodeAnalysis;

namespace Dfe.Analytics;

/// <summary>
/// Configuration for <see cref="AksFederatedBigQueryClientProvider"/>.
/// </summary>
public class FederatedAksAuthenticationOptions
{
    /// <summary>
    /// The project number.
    /// </summary>
    [DisallowNull]
    public string? ProjectNumber { get; set; }

    /// <summary>
    /// The workflow identity pool name.
    /// </summary>
    [DisallowNull]
    public string? WorkloadIdentityPoolName { get; set; }

    /// <summary>
    /// The workflow identity pool provider name.
    /// </summary>
    [DisallowNull]
    public string? WorkloadIdentityPoolProviderName { get; set; }

    /// <summary>
    /// The service account email.
    /// </summary>
    [DisallowNull]
    public string? ServiceAccountEmail { get; set; }

    [MemberNotNull(nameof(ProjectNumber))]
    [MemberNotNull(nameof(WorkloadIdentityPoolName))]
    [MemberNotNull(nameof(WorkloadIdentityPoolProviderName))]
    [MemberNotNull(nameof(ServiceAccountEmail))]
    internal void ValidateOptions()
    {
        if (ProjectNumber is null)
        {
            throw new InvalidOperationException($"{nameof(ProjectNumber)} has not been configured.");
        }

        if (WorkloadIdentityPoolName is null)
        {
            throw new InvalidOperationException($"{nameof(WorkloadIdentityPoolName)} has not been configured.");
        }

        if (WorkloadIdentityPoolProviderName is null)
        {
            throw new InvalidOperationException($"{nameof(WorkloadIdentityPoolProviderName)} has not been configured.");
        }

        if (ServiceAccountEmail is null)
        {
            throw new InvalidOperationException($"{nameof(ServiceAccountEmail)} has not been configured.");
        }
    }
}
