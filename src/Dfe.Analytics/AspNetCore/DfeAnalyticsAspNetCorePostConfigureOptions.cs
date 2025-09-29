using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.AspNetCore;

#pragma warning disable CA1812
internal class DfeAnalyticsAspNetCorePostConfigureOptions : IPostConfigureOptions<DfeAnalyticsAspNetCoreOptions>
#pragma warning restore CA1812
{
    public void PostConfigure(string? name, DfeAnalyticsAspNetCoreOptions options)
    {
        options.UserIdClaimType ??= ClaimTypes.NameIdentifier;
        options.GetUserIdFromRequest ??= options.GetUserIdFromUserClaims;
    }
}
