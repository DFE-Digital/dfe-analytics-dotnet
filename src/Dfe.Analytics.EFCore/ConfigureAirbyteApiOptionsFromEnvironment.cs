using Dfe.Analytics.EFCore.AirbyteApi;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.EFCore;

internal class ConfigureAirbyteApiOptionsFromEnvironment : IPostConfigureOptions<AirbyteApiOptions>
{
    public void PostConfigure(string? name, AirbyteApiOptions options)
    {
        // ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract

        if (options.ClientId is null && Environment.GetEnvironmentVariable("AIRBYTE-CLIENT-ID") is { } clientId)
        {
            options.ClientId = clientId;
        }

        if (options.ClientSecret is null && Environment.GetEnvironmentVariable("AIRBYTE-CLIENT-SECRET") is { } clientSecret)
        {
            options.ClientSecret = clientSecret;
        }

        // ReSharper restore ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
    }
}
