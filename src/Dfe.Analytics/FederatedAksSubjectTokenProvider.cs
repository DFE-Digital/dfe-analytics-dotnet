using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace Dfe.Analytics;

internal sealed class FederatedAksSubjectTokenProvider :
    ProgrammaticExternalAccountCredential.ISubjectTokenProvider, IDisposable
{
    public const string ClientIdEnvironmentVariableName = "AZURE_CLIENT_ID";
    public const string TokenPathEnvironmentVariableName = "AZURE_FEDERATED_TOKEN_FILE";
    public const string TenantIdEnvironmentVariableName = "AZURE_TENANT_ID";

    public const string SubjectTokenType = "urn:ietf:params:oauth:token-type:jwt";

    private readonly HttpClient _httpClient = new(new SocketsHttpHandler { PreAuthenticate = true });

    public async Task<string> GetSubjectTokenAsync(ProgrammaticExternalAccountCredential caller, CancellationToken taskCancellationToken)
    {
        var clientId = GetRequiredEnvironmentVariable(ClientIdEnvironmentVariableName);
        var tokenPath = GetRequiredEnvironmentVariable(TokenPathEnvironmentVariableName);
        var tenantId = GetRequiredEnvironmentVariable(TenantIdEnvironmentVariableName);

        var assertion = await File.ReadAllTextAsync(tokenPath, taskCancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://login.microsoftonline.com/{tenantId}/oauth2/v2.0/token");
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", clientId },
            { "scope", "api://AzureADTokenExchange/.default" },
            { "client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer" },
            { "client_assertion", assertion },
            { "grant_type", "client_credentials" }
        });

        var response = await _httpClient.SendAsync(request, taskCancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new DfeAnalyticsAuthenticationException(
                $"Failed acquiring access token from {request.RequestUri!.Host}; " +
                $"response status code does not indicate success: {response.StatusCode}.\n\n" +
                await response.Content.ReadAsStringAsync(taskCancellationToken));
        }

        var responseBody = await response.Content.ReadAsStringAsync(taskCancellationToken);
        using var responseBodyJson = JsonDocument.Parse(responseBody);
        var token = responseBodyJson.RootElement.TryGetProperty("access_token", out var accessTokenProperty) &&
            accessTokenProperty.GetString() is string accessToken
                ? accessToken
                : throw new InvalidOperationException($"Document was missing expected property: 'access_token'.");

        return token;

        static string GetRequiredEnvironmentVariable(string name) =>
            Environment.GetEnvironmentVariable(name) ??
            throw new InvalidOperationException($"The {name} environment variable is missing.");
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
