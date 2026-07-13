using Dfe.Analytics.EFCore.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Dfe.Analytics.EFCore;

public static class AspNetCoreExtensions
{
    private const string DefaultConfigurationPath = "/_dfe-analytics/db-config.json";

    public static IEndpointConventionBuilder MapDfeAnalyticsDbConfiguration<TDbContext>(
        this IEndpointRouteBuilder endpointBuilder)
        where TDbContext : DbContext
    {
        return MapDfeAnalyticsDbConfiguration<TDbContext>(endpointBuilder, DefaultConfigurationPath);
    }

    public static IEndpointConventionBuilder MapDfeAnalyticsDbConfiguration<TDbContext>(
        this IEndpointRouteBuilder endpointBuilder,
        string path)
        where TDbContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(endpointBuilder);
        ArgumentNullException.ThrowIfNull(path);

        return endpointBuilder.MapGet(
            path,
            ctx =>
            {
                var configurationProvider = new AnalyticsConfigurationProvider();
                var dbContext = ctx.RequestServices.GetRequiredService<TDbContext>();
                var configuration = configurationProvider.GetConfiguration(dbContext);
                return ctx.Response.WriteAsJsonAsync(configuration, DatabaseSyncConfiguration.JsonSerializerOptions);
            });
    }
}
