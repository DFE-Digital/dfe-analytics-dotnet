using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.EFCore.AirbyteApi;

#pragma warning disable CA2234
public class AirbyteApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public async Task<JsonObject> ConfigurationApiPostAsync(string path, JsonObject body, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(body);

        using var content = CreateJsonContent(body);

        var response = await httpClient.PostAsync(path, content, cancellationToken);

        await response.EnsureSuccessStatusCodeWithContentAsync();

        return (await response.Content.ReadFromJsonAsync<JsonObject>(_serializerOptions, cancellationToken))!;
    }

    public async Task<GetJobStatusResponse> GetJobStatusAsync(long jobId, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"api/public/v1/jobs/{jobId}",
            cancellationToken);

        await response.EnsureSuccessStatusCodeWithContentAsync();

        return (await response.Content.ReadFromJsonAsync<GetJobStatusResponse>(_serializerOptions, cancellationToken))!;
    }

    public async Task<TriggerJobResponse> TriggerJobAsync(TriggerJobRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var content = CreateJsonContent(request);

        var response = await httpClient.PostAsync(
            "api/public/v1/jobs",
            content,
            cancellationToken);

        await response.EnsureSuccessStatusCodeWithContentAsync();

        return (await response.Content.ReadFromJsonAsync<TriggerJobResponse>(_serializerOptions, cancellationToken))!;
    }

    public async Task UpdateConnectionDetailsAsync(string connectionId, UpdateConnectionDetailsRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(connectionId);
        ArgumentNullException.ThrowIfNull(request);

        using var content = CreateJsonContent(request);

        var response = await httpClient.PatchAsync(
            $"api/public/v1/connections/{Uri.EscapeDataString(connectionId)}",
            content,
            cancellationToken);

        await response.EnsureSuccessStatusCodeWithContentAsync();
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

file static class HttpResponseMessageExtensions
{
    public static async Task EnsureSuccessStatusCodeWithContentAsync(this HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();

            throw new HttpRequestException(
                $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Content:\n{content}",
                null,
                response.StatusCode);
        }
    }
}
