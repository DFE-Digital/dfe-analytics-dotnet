using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.EFCore.AirbyteApi;

public class AirbyteApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

    public async Task UpdateConnectionDetailsAsync(string connectionId, UpdateConnectionDetailsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);

        using var content = CreateJsonContent(request);

#pragma warning disable CA2234
        var response = await httpClient.PatchAsync(
#pragma warning restore CA2234
            $"api/public/v1/connections/{Uri.EscapeDataString(connectionId)}",
            content,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    internal static void ConfigureHttpClient(IHttpClientBuilder clientBuilder)
    {
        ArgumentNullException.ThrowIfNull(clientBuilder);

        clientBuilder
            .ConfigureHttpClient((sp, client) =>
            {
                var optionsAccessor = sp.GetRequiredService<IOptions<AirbyteApiOptions>>();
                client.BaseAddress = new Uri(optionsAccessor.Value.BaseAddress);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            })
            .AddHttpMessageHandler(sp => new AuthenticatingHandler(sp.GetRequiredService<IOptions<AirbyteApiOptions>>()));
    }

#pragma warning disable CA1859
    private static HttpContent CreateJsonContent(object value)
#pragma warning restore CA1859
    {
        // Airbyte API is fussy about the Content-Type header; it must be exactly "application/json"
        return new StringContent(
            JsonSerializer.Serialize(value, _serializerOptions),
            encoding: null,
            "application/json");
    }

    private class AuthenticatingHandler(IOptions<AirbyteApiOptions> optionsAccessor) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Access tokens only live for 3 minutes - Airbyte docs recommends getting a new token for each request

            var accessToken = await EnsureAccessTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<string> EnsureAccessTokenAsync(CancellationToken cancellationToken)
        {
            var options = optionsAccessor.Value;

            var requestBody = new
            {
                client_id = options.ClientId,
                client_secret = options.ClientSecret,
                grant_type = "client_credentials"
            };

            using var content = CreateJsonContent(requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, options.BaseAddress.TrimEnd('/') + "/api/public/v1/applications/token")
            {
                Content = content
            };

            var response = await base.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var root = JsonDocument.Parse(responseJson).RootElement;

            return root.GetProperty("access_token").GetString() ?? throw new InvalidOperationException("Could not extract access token from response.");
        }
    }
}
