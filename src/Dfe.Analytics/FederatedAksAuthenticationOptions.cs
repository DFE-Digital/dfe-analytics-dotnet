using System.Diagnostics.CodeAnalysis;

namespace Dfe.Analytics;

/// <summary>
/// Configuration for <see cref="AksFederatedBigQueryClientProvider"/>.
/// </summary>
public class FederatedAksAuthenticationOptions
{
    /// <summary>
    /// The workflow identity pool provider audience.
    /// </summary>
    [DisallowNull]
    public string? Audience { get; set; }

    /// <summary>
    /// The URL for retrieving an access token for accessing BigQuery.
    /// </summary>
    [DisallowNull]
    public string? GenerateAccessTokenUrl { get; set; }

    [MemberNotNull(nameof(Audience))]
    [MemberNotNull(nameof(GenerateAccessTokenUrl))]
    internal void ValidateOptions()
    {
        if (Audience is null)
        {
            throw new InvalidOperationException($"{nameof(Audience)} has not been configured.");
        }

        if (GenerateAccessTokenUrl is null)
        {
            throw new InvalidOperationException($"{nameof(GenerateAccessTokenUrl)} has not been configured.");
        }
    }
}
