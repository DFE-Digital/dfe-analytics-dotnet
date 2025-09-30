using System.Reflection;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

internal class DfeAnalyticsPostConfigureOptions : IPostConfigureOptions<DfeAnalyticsOptions>
{
    public void PostConfigure(string? name, DfeAnalyticsOptions options)
    {
        options.Namespace ??= Assembly.GetEntryAssembly()?.GetName().Name;
        options.TableId ??= "events";
    }
}
