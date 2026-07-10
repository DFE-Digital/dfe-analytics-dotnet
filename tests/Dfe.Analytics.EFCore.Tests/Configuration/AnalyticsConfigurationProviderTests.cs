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
                Assert.Equal("BaseEntity", table.Name);
                Assert.Equal(["Id"], table.PrimaryKey.ColumnNames);
                Assert.Collection(
                    table.Columns.OrderBy(c => c.Name),
                    column =>
                    {
                        Assert.Equal("BaseProperty", column.Name);
                        Assert.False(column.Hidden);
                        Assert.Null(column.PolicyTag);
                    },
                    column =>
                    {
                        Assert.Equal("DerivedProperty2", column.Name);
                        Assert.False(column.Hidden);
                        Assert.Null(column.PolicyTag);
                    },
                    column =>
                    {
                        Assert.Equal("Discriminator", column.Name);
                        Assert.False(column.Hidden);
                        Assert.Null(column.PolicyTag);
                    },
                    column =>
                    {
                        Assert.Equal("Id", column.Name);
                        Assert.False(column.Hidden);
                        Assert.Null(column.PolicyTag);
                    }
                );
            },
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
                        Assert.Null(column.PolicyTag);
                    },
                    column =>
                    {
                        // Specifying a policy tag implies the column is hidden
                        Assert.Equal("Email", column.Name);
                        Assert.True(column.Hidden);
                        Assert.Equal("email", column.PolicyTag);
                    },
                    column =>
                    {
                        // Hidden columns without an explicit policy tag get the default hidden policy tag
                        Assert.Equal("Name", column.Name);
                        Assert.True(column.Hidden);
                        Assert.Null(column.PolicyTag);
                    },
                    column =>
                    {
                        Assert.Equal("TestEntityId", column.Name);
                        Assert.False(column.Hidden);
                        Assert.Null(column.PolicyTag);
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
