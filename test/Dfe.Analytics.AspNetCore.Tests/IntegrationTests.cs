using Google.Cloud.BigQuery.V2;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;

namespace Dfe.Analytics.AspNetCore.Tests;

public class IntegrationTests : IClassFixture<IntegrationTestsApplicationFactory>
{
    private readonly IntegrationTestsApplicationFactory _fixture;
    private readonly Mock<BigQueryClient> _bigQueryClient;

    public IntegrationTests(IntegrationTestsApplicationFactory fixture)
    {
        _fixture = fixture;
        _bigQueryClient = fixture.Services.GetRequiredService<Mock<BigQueryClient>>();
        _bigQueryClient.Reset();
    }

    [Fact]
    public async Task WritesEventToBigQuery()
    {
        using var waitHandle = new ManualResetEventSlim(false);
        BigQueryInsertRow? insertRow = null;

        _bigQueryClient.Setup(
            mock => mock.InsertRowAsync(
                IntegrationTestsStartup.DatasetId,
                IntegrationTestsStartup.TableId,
                It.IsAny<BigQueryInsertRow>(),
                It.IsAny<InsertOptions>(),
                It.IsAny<CancellationToken>()))
            .Callback(new InvocationAction(invocation =>
            {
                insertRow = (BigQueryInsertRow)invocation.Arguments[2];
                waitHandle.Set();
            }));

        var userId = "user-123";
        var referer = "http://example.org/";
        var userAgent = "TestClient";

        var httpClient = _fixture.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/test?foo=42&bar=69");
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        request.Headers.TryAddWithoutValidation("X-UserId", userId);
        request.Headers.TryAddWithoutValidation("Referer", referer);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        waitHandle.Wait(1000);
        Assert.NotNull(insertRow);

        Assert.Collection(
            insertRow!.Cast<KeyValuePair<string, object>>(),
            field =>
            {
                Assert.Equal("occurred_at", field.Key);
                var value = Assert.IsType<DateTime>(field.Value);
                Assert.Equal(DateTime.UtcNow, value, TimeSpan.FromSeconds(1));
            },
            field =>
            {
                Assert.Equal("event_type", field.Key);
                Assert.Equal("web_request", field.Value);
            },
            field =>
            {
                Assert.Equal("environment", field.Key);
                Assert.Equal(IntegrationTestsStartup.Environment, field.Value);
            },
            field =>
            {
                Assert.Equal("namespace", field.Key);
                Assert.Equal(IntegrationTestsStartup.Namespace, field.Value);
            },
            field =>
            {
                Assert.Equal("user_id", field.Key);
                Assert.Equal(userId, field.Value);
            },
            field =>
            {
                Assert.Equal("request_uuid", field.Key);
                Assert.NotEmpty(field.Value as string);
            },
            field =>
            {
                Assert.Equal("request_method", field.Key);
                Assert.Equal("GET", field.Value);
            },
            field =>
            {
                Assert.Equal("request_path", field.Key);
                Assert.Equal("/test", field.Value);
            },
            field =>
            {
                Assert.Equal("request_user_agent", field.Key);
                Assert.Equal(userAgent, field.Value);
            },
            field =>
            {
                Assert.Equal("request_referer", field.Key);
                Assert.Equal(referer, field.Value);
            },
            field =>
            {
                Assert.Equal("request_query", field.Key);
                Assert.Collection(
                    Assert.IsAssignableFrom<IEnumerable<BigQueryInsertRow>>(field.Value),
                    row =>
                    {
                        Assert.Collection(
                            row.Cast<KeyValuePair<string, object>>()!,
                            field =>
                            {
                                Assert.Equal("key", field.Key);
                                Assert.Equal("foo", field.Value);
                            },
                            field =>
                            {
                                Assert.Equal("value", field.Key);
                                Assert.Collection(Assert.IsAssignableFrom<IEnumerable<string>>(field.Value), v => Assert.Equal("42", v));
                            });
                    },
                    row =>
                    {
                        Assert.Collection(
                            row.Cast<KeyValuePair<string, object>>()!,
                            field =>
                            {
                                Assert.Equal("key", field.Key);
                                Assert.Equal("bar", field.Value);
                            },
                            field =>
                            {
                                Assert.Equal("value", field.Key);
                                Assert.Collection(Assert.IsAssignableFrom<IEnumerable<string>>(field.Value), v => Assert.Equal("69", v));
                            });
                    });
            },
            field =>
            {
                Assert.Equal("response_content_type", field.Key);
                Assert.Equal("text/plain", field.Value);
            },
            field =>
            {
                Assert.Equal("response_status", field.Key);
                Assert.Equal("200", field.Value);
            },
            field =>
            {
                Assert.Equal("data", field.Key);
                Assert.Collection(
                    Assert.IsAssignableFrom<IEnumerable<BigQueryInsertRow>>(field.Value),
                    row =>
                    {
                        Assert.Collection(
                            row.Cast<KeyValuePair<string, object>>()!,
                            field =>
                            {
                                Assert.Equal("key", field.Key);
                                Assert.Equal("data-key1", field.Value);
                            },
                            field =>
                            {
                                Assert.Equal("value", field.Key);
                                Assert.Collection(Assert.IsAssignableFrom<IEnumerable<string>>(field.Value), v => Assert.Equal("data-value1", v));
                            });
                    },
                    row =>
                    {
                        Assert.Collection(
                            row.Cast<KeyValuePair<string, object>>()!,
                            field =>
                            {
                                Assert.Equal("key", field.Key);
                                Assert.Equal("data-key2", field.Value);
                            },
                            field =>
                            {
                                Assert.Equal("value", field.Key);
                                Assert.Collection(Assert.IsAssignableFrom<IEnumerable<string>>(field.Value), v => Assert.Equal("data-value2", v));
                            });
                    });
            },
            field =>
            {
                Assert.Equal("entity_table_name", field.Key);
                Assert.Null(field.Value);
            },
            field =>
            {
                Assert.Equal("anonymised_user_agent_and_ip", field.Key);
            },
            field =>
            {
                Assert.Equal("event_tags", field.Key);
                Assert.Collection(
                    Assert.IsAssignableFrom<IEnumerable<string>>(field.Value),
                    t => Assert.Equal("tag1", t),
                    t => Assert.Equal("tag2", t));
            });
    }

    [Fact]
    public async Task EventIgnored_DoesNotSendEventToBigQuery()
    {
        var userId = "user-123";
        var referer = "http://example.org/";
        var userAgent = "TestClient";

        var httpClient = _fixture.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/skip-event");
        request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
        request.Headers.TryAddWithoutValidation("X-UserId", userId);
        request.Headers.TryAddWithoutValidation("Referer", referer);

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        _bigQueryClient.Verify(
            mock => mock.InsertRowAsync(
                IntegrationTestsStartup.DatasetId,
                IntegrationTestsStartup.TableId,
                It.IsAny<BigQueryInsertRow>(),
                It.IsAny<InsertOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never());
    }
}

public class IntegrationTestsApplicationFactory : WebApplicationFactory<IntegrationTestsStartup>
{
    protected override IWebHostBuilder? CreateWebHostBuilder() => new WebHostBuilder();

    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder
        .UseContentRoot(".")
        .UseStartup<IntegrationTestsStartup>();
}

public class IntegrationTestsStartup
{
    public const string DatasetId = "test-dataset";
    public const string Environment = "test-environment";
    public const string Namespace = "test-namespace";
    public const string TableId = "test-table";

    public void Configure(IApplicationBuilder app)
    {
        app.UseDfeAnalytics();

        app.UseRouting();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("/test", async ctx =>
            {
                var feature = ctx.Features.Get<WebRequestEventFeature>()!;
                feature.Event.AddData("data-key1", "data-value1");
                feature.Event.AddData("data-key2", "data-value2");
                feature.Event.AddTags("tag1", "tag2");

                ctx.Response.ContentType = "text/plain";
                await ctx.Response.WriteAsync("Ok");
            });

            endpoints.MapGet("/skip-event", async ctx =>
            {
                var feature = ctx.Features.Get<WebRequestEventFeature>()!;
                feature.IgnoreEvent();

                ctx.Response.ContentType = "text/plain";
                await ctx.Response.WriteAsync("Ok");
            });
        });
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRouting();

        var bigQueryClientMock = new Mock<BigQueryClient>();
        services.AddSingleton(bigQueryClientMock);

        services.AddDfeAnalytics(options =>
        {
            options.BigQueryClient = bigQueryClientMock.Object;
            options.DatasetId = DatasetId;
            options.Environment = Environment;
            options.Namespace = Namespace;
            options.TableId = TableId;

            options.GetUserIdFromRequest = ctx => ctx.Request.Headers["X-UserId"];
        });
    }
}
