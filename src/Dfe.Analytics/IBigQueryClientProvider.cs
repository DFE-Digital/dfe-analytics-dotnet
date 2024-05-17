using Google.Cloud.BigQuery.V2;

namespace Dfe.Analytics;

/// <summary>
/// Represents a type that can return an authenticated <see cref="BigQueryClient"/>.
/// </summary>
public interface IBigQueryClientProvider
{
    /// <summary>
    /// Gets a <see cref="BigQueryClient"/>.
    /// </summary>
    /// <returns>An authenticated <see cref="BigQueryClient"/>.</returns>
    ValueTask<BigQueryClient> GetBigQueryClientAsync();
}
