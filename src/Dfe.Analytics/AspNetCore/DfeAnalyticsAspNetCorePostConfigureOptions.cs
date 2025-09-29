using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace Dfe.Analytics.AspNetCore;

internal class DfeAnalyticsAspNetCorePostConfigureOptions : IPostConfigureOptions<DfeAnalyticsAspNetCoreOptions>
{
    public void PostConfigure(string? name, DfeAnalyticsAspNetCoreOptions options)
    {
        options.UserIdClaimType ??= ClaimTypes.NameIdentifier;
        options.GetUserIdFromRequest ??= options.GetUserIdFromUserClaims;
    }
}
