using Google.Cloud.BigQuery.V2;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

/// <summary>
/// An implementation of <see cref="IBigQueryClientProvider"/> that retrieves a <see cref="BigQueryClient"/> from <see cref="DfeAnalyticsOptions"/>.
/// </summary>
public class OptionsBigQueryClientProvider : IBigQueryClientProvider
{
    private readonly IOptions<DfeAnalyticsOptions> _optionsAccessor;

    /// <summary>
    /// Initializes a new instance of <see cref="OptionsBigQueryClientProvider"/>.
    /// </summary>
    /// <param name="optionsAccessor">The <see cref="DfeAnalyticsOptions"/>.</param>
    public OptionsBigQueryClientProvider(IOptions<DfeAnalyticsOptions> optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);
        _optionsAccessor = optionsAccessor;
    }

    /// <inheritdoc/>
    public ValueTask<BigQueryClient> GetBigQueryClientAsync()
    {
        var configuredClient = _optionsAccessor.Value.BigQueryClient ??
            throw new InvalidOperationException($"No {nameof(DfeAnalyticsOptions.BigQueryClient)} has been configured.");

        return new ValueTask<BigQueryClient>(configuredClient);
    }
}
