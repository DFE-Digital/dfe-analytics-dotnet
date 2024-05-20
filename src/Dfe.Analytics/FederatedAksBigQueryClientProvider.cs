using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.BigQuery.V2;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

/// <summary>
/// An implementation of <see cref="IBigQueryClientProvider"/> for workloads running in Azure Kubernetes Service
/// with workload identity federation.
/// </summary>
public sealed class AksFederatedBigQueryClientProvider : IBigQueryClientProvider, IDisposable
{
    /// <summary>
    /// The duration before the token actually expires at which we should refresh the credentials.
    /// </summary>
    private static readonly TimeSpan _expirationAllowance = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The cached <see cref="BigQueryClient"/>.
    /// </summary>
    private BigQueryClient? _client;

    /// <summary>
    /// The time that <see cref="_client"/> expires.
    /// </summary>
    private DateTimeOffset _clientExpiry;

    private readonly TimeProvider _timeProvider;
    private readonly FederatedAksAuthenticationOptions _options;
    private readonly HttpClient _httpClient;
    private bool _disposed;

    private string _clientId = default!;
    private string _tokenPath = default!;
    private string _tenantId = default!;

    /// <summary>
    /// Initializes a new instance of <see cref="AksFederatedBigQueryClientProvider"/>.
    /// </summary>
    /// <param name="timeProvider">The <see cref="TimeProvider"/>.</param>
    /// <param name="optionsAccessor">The <see cref="FederatedAksAuthenticationOptions"/>.</param>
    public AksFederatedBigQueryClientProvider(
        TimeProvider timeProvider,
        IOptions<FederatedAksAuthenticationOptions> optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(optionsAccessor);

        optionsAccessor.Value.ValidateOptions();

        _timeProvider = timeProvider;
        _options = optionsAccessor.Value;
        GetConfigurationFromEnvironment();
        _httpClient = new HttpClient(new SocketsHttpHandler() { PreAuthenticate = true });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _client?.Dispose();
        _httpClient.Dispose();
        _client = null;
        _disposed = true;
    }

    /// <inheritdoc/>
    public ValueTask<BigQueryClient> GetBigQueryClientAsync()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var now = _timeProvider.GetUtcNow();
        if (_client is not null && (_clientExpiry - _expirationAllowance) > now)
        {
            return new ValueTask<BigQueryClient>(_client);
        }

        return CreateClientAsync();

        async ValueTask<BigQueryClient> CreateClientAsync()
        {
            await CreateAndAssignClientAsync();
            return _client!;
        }
    }

    private void GetConfigurationFromEnvironment()
    {
        _clientId = GetRequiredEnvironmentVariable("AZURE_CLIENT_ID");
        _tokenPath = GetRequiredEnvironmentVariable("AZURE_FEDERATED_TOKEN_FILE");
        _tenantId = GetRequiredEnvironmentVariable("AZURE_TENANT_ID");

        static string GetRequiredEnvironmentVariable(string name) =>
            Environment.GetEnvironmentVariable(name) ??
                throw new InvalidOperationException($"The {name} environment variable is missing.");
    }

    private async Task CreateAndAssignClientAsync()
    {
        _client?.Dispose();

        var (token, expires) = await GetAccessTokenAsync();
        var credential = GoogleCredential.FromAccessToken(token);
        _client = BigQueryClient.Create(_options.ProjectNumber!, credential);
        _clientExpiry = expires;
    }

    private async Task<(string AccessToken, DateTimeOffset Expires)> GetAccessTokenAsync()
    {
        var azureToken = await GetAzureAccessTokenAsync();
        var googleToken = await ExchangeTokenAsync(azureToken);
        return await GetBigQueryTokenAsync(googleToken);

        async Task<string> GetAzureAccessTokenAsync()
        {
            var assertion = await File.ReadAllTextAsync(_tokenPath);

            var request = new HttpRequestMessage(HttpMethod.Get, $"https://login.microsoftonline.com/{_tenantId}/oauth2/v2.0/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "client_id", _clientId },
                    { "scope", "api://AzureADTokenExchange/.default" },
                    { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
                    { "client_assertion", assertion },
                    { "grant_type", "client_credentials" }
                })
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureTokenAcquisitionSucceeded(exceptionMessage: "Failed acquiring access token from Azure.");

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseBodyJson = JsonDocument.Parse(responseBody);
            var token = responseBodyJson.GetRequiredProperty("access_token").GetString()!;
            return token;
        }

        async Task<string> ExchangeTokenAsync(string azureToken)
        {
            var audience = $"//iam.googleapis.com/projects/{Uri.EscapeDataString(_options.ProjectNumber!)}/" +
                $"locations/global/workloadIdentityPools/{Uri.EscapeDataString(_options.WorkloadIdentityPoolName!)}/" +
                $"providers/{Uri.EscapeDataString(_options.WorkloadIdentityPoolProviderName!)}";

            var request = new HttpRequestMessage(HttpMethod.Post, "https://sts.googleapis.com/v1/token")
            {
                Content = JsonContent.Create(new JsonObject()
                {
                    { "grantType", "urn:ietf:params:oauth:grant-type:token-exchange" },
                    { "audience", audience },
                    { "scope", "https://www.googleapis.com/auth/cloud-platform" },
                    { "requestedTokenType", "urn:ietf:params:oauth:token-type:access_token" },
                    { "subjectToken", azureToken },
                    { "subjectTokenType", "urn:ietf:params:oauth:token-type:jwt" }
                })
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureTokenAcquisitionSucceeded(exceptionMessage: "Failed exchanging access token.");

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseBodyJson = JsonDocument.Parse(responseBody);
            var token = responseBodyJson.GetRequiredProperty("access_token").GetString()!;
            return token;
        }

        async Task<(string AccessToken, DateTimeOffset Expires)> GetBigQueryTokenAsync(string googleToken)
        {
            var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://iamcredentials.googleapis.com/v1/projects/-/serviceAccounts/{Uri.EscapeDataString(_options.ServiceAccountEmail!)}:generateAccessToken")
            {
                Headers =
                {
                    Authorization = new("Bearer", googleToken)
                },
                Content = JsonContent.Create(new JsonObject()
                {
                    { "scope", new JsonArray("https://www.googleapis.com/auth/cloud-platform") }
                })
            };

            var response = await _httpClient.SendAsync(request);
            response.EnsureTokenAcquisitionSucceeded(exceptionMessage: "Failed acquiring access token from Google.");

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseBodyJson = JsonDocument.Parse(responseBody);
            var token = responseBodyJson.GetRequiredProperty("accessToken").GetString()!;
            var expireTime = responseBodyJson.GetRequiredProperty("expireTime").GetDateTimeOffset();
            return (token, expireTime);
        }
    }
}

file static class Extensions
{
    public static JsonElement GetRequiredProperty(this JsonDocument doc, string propertyName)
    {
        if (!doc.RootElement.TryGetProperty(propertyName, out var result) || result.ValueKind == JsonValueKind.Null)
        {
            throw new InvalidOperationException($"Document was missing expected property: '{propertyName}'.");
        }

        return result;
    }

    public static void EnsureTokenAcquisitionSucceeded(this HttpResponseMessage response, string exceptionMessage)
    {
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new DfeAnalyticsAuthenticationException(exceptionMessage, ex);
        }
    }
}
