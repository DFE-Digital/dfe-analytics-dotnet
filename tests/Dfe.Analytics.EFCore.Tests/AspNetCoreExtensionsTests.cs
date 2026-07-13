using System.Net;
using System.Text.Json;
using Dfe.Analytics.EFCore.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dfe.Analytics.EFCore.Tests;

public class AspNetCoreExtensionsTests
{
    [Fact]
    public async Task MapDfeAnalyticsDbConfiguration_ReturnsConfigurationForDbContext()
    {
        // Arrange
        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddScoped(_ => new TestDbContext());
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapDfeAnalyticsDbConfiguration<TestDbContext>("/db-configuration");
                        });
                    });
            })
            .StartAsync(TestContext.Current.CancellationToken);

        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/db-configuration", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var configuration = JsonSerializer.Deserialize<DatabaseSyncConfiguration>(
            json,
            DatabaseSyncConfiguration.JsonSerializerOptions);

        var expectedConfiguration = new AnalyticsConfigurationProvider().GetConfiguration(new TestDbContext());

        Assert.NotNull(configuration);
        Assert.Equal(expectedConfiguration, configuration);
    }
}
