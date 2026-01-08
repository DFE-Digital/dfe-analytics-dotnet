using System.Text.Json.Nodes;
using Dfe.Analytics.EFCore.AirbyteApi;
using Dfe.Analytics.EFCore.Configuration;
using Google.Apis.Bigquery.v2.Data;
using Google.Cloud.BigQuery.V2;
using JustEat.HttpClientInterception;
using Microsoft.Extensions.Options;
using Moq;

namespace Dfe.Analytics.EFCore.Tests;

public class AnalyticsDeployerTests
{
    private const string ProjectId = "dummy-project";
    private const string DatasetId = "dummy-dataset";

    private const string HiddenPolicyTagName = "projects/dummy-project/locations/us/taxonomies/dummy-taxonomy/policyTags/dummy-policy-tag";

    [Fact]
    public async Task ApplyAirbyteConfigurationAsync_CallsAirbyteApiWithExpectedPayload()
    {
        // Arrange
        var configuration = GetConfiguration();
        var connectionId = Guid.NewGuid().ToString();

        string? capturedRequestContent = null;

        var httpClientOptions = new HttpClientInterceptorOptions()
            .ThrowsOnMissingRegistration();
        new HttpRequestInterceptionBuilder()
            .Requests()
            .ForMethod(new HttpMethod("PATCH"))
            .ForHttps()
            .ForAnyHost()
            .ForPath($"api/public/v1/connections/{Uri.EscapeDataString(connectionId)}")
            .WithInterceptionCallback(req => capturedRequestContent = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult())
            .RegisterWith(httpClientOptions);
        var httpClient = httpClientOptions.CreateHttpClient();
        httpClient.BaseAddress = new Uri("https://dummy-airbyte/");

        var progressReporter = new RecordingProgressReporter();

        var deployer = CreateDeployer(httpClient);

        // Act
        await deployer.ApplyAirbyteConfigurationAsync(configuration, connectionId, progressReporter, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(capturedRequestContent);

        var expectedJson = JsonNode.Parse("""
        {
            "configurations": [
                {
                    "streams": [
                        {
                            "name": "TestEntity",
                            "syncMode": "incremental_append",
                            "cursorField": [
                                "_ab_cdc_lsn"
                            ],
                            "primaryKey": [
                                [
                                    "TestEntityId"
                                ]
                            ],
                            "selectedFields": [
                                {
                                    "fieldPath": [ "_ab_cdc_lsn" ]
                                },
                                {
                                    "fieldPath": [ "_ab_cdc_deleted_at" ]
                                },
                                {
                                    "fieldPath": [ "_ab_cdc_updated_at" ]
                                },
                                {
                                    "fieldPath": [ "TestEntityId" ]
                                },
                                {
                                    "fieldPath": [ "Name" ]
                                },
                                {
                                    "fieldPath": [ "DateOfBirth" ]
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """);

        var actualJson = JsonNode.Parse(capturedRequestContent!);

        Assert.True(JsonNode.DeepEquals(expectedJson, actualJson));
    }

    [Fact]
    public async Task UpdateBigQueryPolicyTagsAsync_ThrowsIfColumnIsMissingFromBqTable()
    {
        // Arrange
        var configuration = GetConfiguration();
        var progressReporter = new RecordingProgressReporter();

        using var httpClient = new HttpClient();

        var bigQueryClientMock = new Mock<BigQueryClient>();
        bigQueryClientMock
            .Setup(mock => mock.GetTableAsync(ProjectId, DatasetId, configuration.Tables.Single().Name, It.IsAny<GetTableOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var table = new BigQueryTable(
                    bigQueryClientMock.Object,
                    new Table
                    {
                        Schema = new TableSchema
                        {
                            Fields =
                            [
                                new TableFieldSchema { Name = "TestEntityId", Type = "INTEGER" },
                                // Note: "Name" column is missing
                                new TableFieldSchema { Name = "DateOfBirth", Type = "DATE" }
                            ]
                        }
                    });

                return table;
            });

        var deployer = CreateDeployer(httpClient, bigQueryClientMock.Object);

        // Act
        var ex = await Record.ExceptionAsync(
            () => deployer.UpdateBigQueryPolicyTagsAsync(configuration, HiddenPolicyTagName, progressReporter, TestContext.Current.CancellationToken));

        // Assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Equal($"Column 'Name' not found in BigQuery table 'TestEntity'.", ex.Message);
    }

    [Fact]
    public async Task UpdateBigQueryPolicyTagsAsync_ColumnIsMissingHiddenPolicyTag_AddsHiddenPolicyTagToSchema()
    {
        // Arrange
        var configuration = GetConfiguration();
        var progressReporter = new RecordingProgressReporter();

        using var httpClient = new HttpClient();

        var bigQueryClientMock = new Mock<BigQueryClient>();
        bigQueryClientMock
            .Setup(mock => mock.GetTableAsync(ProjectId, DatasetId, configuration.Tables.Single().Name, It.IsAny<GetTableOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var table = new BigQueryTable(
                    bigQueryClientMock.Object,
                    new Table
                    {
                        Schema = new TableSchema
                        {
                            Fields =
                            [
                                new TableFieldSchema { Name = "TestEntityId", Type = "INTEGER" },
                                new TableFieldSchema { Name = "Name", Type = "STRING" },
                                new TableFieldSchema { Name = "DateOfBirth", Type = "DATE" }
                            ]
                        }
                    });

                return table;
            });

        bigQueryClientMock
            .Setup(mock => mock.PatchTableAsync(
                ProjectId,
                DatasetId,
                configuration.Tables.Single().Name,
                It.Is<Table>(t =>
                    t.Schema.Fields.Single(f => f.Name == "Name").PolicyTags.Names.Contains(HiddenPolicyTagName)),
                It.IsAny<PatchTableOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null)
            .Verifiable();

        var deployer = CreateDeployer(httpClient, bigQueryClientMock.Object);

        // Act
        await deployer.UpdateBigQueryPolicyTagsAsync(configuration, HiddenPolicyTagName, progressReporter, TestContext.Current.CancellationToken);

        // Assert
        bigQueryClientMock.Verify();
    }

    [Fact]
    public async Task UpdateBigQueryPolicyTagsAsync_ColumnHasHiddenPolicyTagButSchemaDoesNotHaveHiddenFlag_RemovesHiddenPolicyTagFromSchema()
    {
        // Arrange
        var configuration = GetConfiguration();
        var progressReporter = new RecordingProgressReporter();

        using var httpClient = new HttpClient();

        var bigQueryClientMock = new Mock<BigQueryClient>();
        bigQueryClientMock
            .Setup(mock => mock.GetTableAsync(ProjectId, DatasetId, configuration.Tables.Single().Name, It.IsAny<GetTableOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var table = new BigQueryTable(
                    bigQueryClientMock.Object,
                    new Table
                    {
                        Schema = new TableSchema
                        {
                            Fields =
                            [
                                new TableFieldSchema { Name = "TestEntityId", Type = "INTEGER" },
                                new TableFieldSchema
                                {
                                    Name = "Name",
                                    Type = "STRING",
                                    PolicyTags = new TableFieldSchema.PolicyTagsData
                                    {
                                        Names = [ HiddenPolicyTagName ]
                                    }
                                },
                                new TableFieldSchema
                                {
                                    Name = "DateOfBirth",
                                    Type = "DATE",
                                    PolicyTags = new TableFieldSchema.PolicyTagsData
                                    {
                                        Names = [ HiddenPolicyTagName ]
                                    }
                                }
                            ]
                        }
                    });

                return table;
            });

        bigQueryClientMock
            .Setup(mock => mock.PatchTableAsync(
                ProjectId,
                DatasetId,
                configuration.Tables.Single().Name,
                It.Is<Table>(t =>
                    t.Schema.Fields.Single(f => f.Name == "Name").PolicyTags.Names.Contains(HiddenPolicyTagName) &&
                    !t.Schema.Fields.Single(f => f.Name == "DateOfBirth").PolicyTags.Names.Contains(HiddenPolicyTagName)),
                It.IsAny<PatchTableOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => null)
            .Verifiable();

        var deployer = CreateDeployer(httpClient, bigQueryClientMock.Object);

        // Act
        await deployer.UpdateBigQueryPolicyTagsAsync(configuration, HiddenPolicyTagName, progressReporter, TestContext.Current.CancellationToken);

        // Assert
        bigQueryClientMock.Verify();
    }

    [Fact]
    public async Task UpdateBigQueryPolicyTagsAsync_BigQuerySchemaMatchesConfiguration_DoesNotCallApiToUpdateTable()
    {
        // Arrange
        var configuration = GetConfiguration();
        var progressReporter = new RecordingProgressReporter();

        using var httpClient = new HttpClient();

        var bigQueryClientMock = new Mock<BigQueryClient>();
        bigQueryClientMock
            .Setup(mock => mock.GetTableAsync(ProjectId, DatasetId, configuration.Tables.Single().Name, It.IsAny<GetTableOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                var table = new BigQueryTable(
                    bigQueryClientMock.Object,
                    new Table
                    {
                        Schema = new TableSchema
                        {
                            Fields =
                            [
                                new TableFieldSchema { Name = "TestEntityId", Type = "INTEGER" },
                                new TableFieldSchema
                                {
                                    Name = "Name",
                                    Type = "STRING",
                                    PolicyTags = new TableFieldSchema.PolicyTagsData
                                    {
                                        Names = [ HiddenPolicyTagName ]
                                    }
                                },
                                new TableFieldSchema { Name = "DateOfBirth", Type = "DATE" }
                            ]
                        }
                    });

                return table;
            });

        var deployer = CreateDeployer(httpClient, bigQueryClientMock.Object);

        // Act
        await deployer.UpdateBigQueryPolicyTagsAsync(configuration, HiddenPolicyTagName, progressReporter, TestContext.Current.CancellationToken);

        // Assert
        bigQueryClientMock.Verify(
            mock => mock.PatchTableAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Table>(),
                It.IsAny<PatchTableOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private AnalyticsDeployer CreateDeployer(
        HttpClient airbyteHttpClient,
        BigQueryClient? bigQueryClient = null)
    {
        var airbyteApiClient = new AirbyteApiClient(airbyteHttpClient);

        var options = Options.Create(new DfeAnalyticsOptions
        {
            BigQueryClient = bigQueryClient,
            ProjectId = ProjectId,
            DatasetId = DatasetId
        });

        return new AnalyticsDeployer(airbyteApiClient, options);
    }

    private DatabaseSyncConfiguration GetConfiguration() =>
        new()
        {
            Tables =
            [
                new TableSyncInfo
                {
                    Name = "TestEntity",
                    PrimaryKey = new TablePrimaryKeySyncInfo { ColumnNames = ["TestEntityId"] },
                    Columns =
                    [
                        new ColumnSyncInfo { Name = "TestEntityId", Hidden = false },
                        new ColumnSyncInfo { Name = "Name", Hidden = true },
                        new ColumnSyncInfo { Name = "DateOfBirth", Hidden = false }
                    ]
                }
            ]
        };

    private class RecordingProgressReporter : IProgressReporter
    {
        private readonly List<string> _lines = new();

        public IReadOnlyCollection<string> WrittenLines => _lines;

        public void WriteLine(string line)
        {
            ArgumentNullException.ThrowIfNull(line);

            _lines.Add(line);
        }
    }
}
