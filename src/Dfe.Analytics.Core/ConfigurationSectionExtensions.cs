using Microsoft.Extensions.Configuration;

namespace Dfe.Analytics;

internal static class ConfigurationSectionExtensions
{
    public static void AssignConfigurationValueIfNotEmpty(this IConfigurationSection section, string configKey, Action<string> assignValue)
    {
        var value = section[configKey];

        if (value is not null)
        {
            assignValue(value);
        }
    }
}
