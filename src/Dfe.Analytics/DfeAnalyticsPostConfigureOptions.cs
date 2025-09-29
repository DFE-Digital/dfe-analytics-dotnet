using System.Reflection;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics;

#pragma warning disable CA1812
internal class DfeAnalyticsPostConfigureOptions : IPostConfigureOptions<DfeAnalyticsOptions>
#pragma warning restore CA1812
{
    public void PostConfigure(string? name, DfeAnalyticsOptions options)
    {
        options.Namespace ??= Assembly.GetEntryAssembly()?.GetName().Name;
        options.TableId ??= "events";
    }
}
