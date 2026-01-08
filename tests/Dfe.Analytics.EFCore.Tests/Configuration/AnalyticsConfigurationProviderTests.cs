using Dfe.Analytics.EFCore.Configuration;

namespace Dfe.Analytics.EFCore.Tests.Configuration;

public class AnalyticsConfigurationProviderTests
{
    [Fact]
    public void GetConfiguration_CreatesValidConfigurationFromDbContext()
    {
        // Arrange
        var dbContext = new TestDbContext();

        var provider = new AnalyticsConfigurationProvider();

        // Act
        var configuration = provider.GetConfiguration(dbContext);

        // Assert
        Assert.Collection(
            configuration.Tables,
            table =>
            {
                Assert.Equal("TestEntity", table.Name);
                Assert.Equal(["TestEntityId"], table.PrimaryKey.ColumnNames);
                Assert.Collection(
                    table.Columns.OrderBy(c => c.Name),
                    column =>
                    {
                        Assert.Equal("DateOfBirth", column.Name);
                        Assert.False(column.Hidden);
                    },
                    column =>
                    {
                        Assert.Equal("Name", column.Name);
                        Assert.True(column.Hidden);
                    },
                    column =>
                    {
                        Assert.Equal("TestEntityId", column.Name);
                        Assert.False(column.Hidden);
                    }
                );
            });
    }

    [Fact]
    public async Task ReadAndWriteConfigurationToFile()
    {
        // Arrange
        var tempFilePath = Path.GetTempFileName();

        var dbContext = new TestDbContext();

        var provider = new AnalyticsConfigurationProvider();
        var configuration = provider.GetConfiguration(dbContext);

        // Act
        await configuration.WriteToFileAsync(tempFilePath, TestContext.Current.CancellationToken);

        var configurationFromFile = await DatabaseSyncConfiguration.ReadFromFileAsync(tempFilePath, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(configuration, configurationFromFile);
    }
}
