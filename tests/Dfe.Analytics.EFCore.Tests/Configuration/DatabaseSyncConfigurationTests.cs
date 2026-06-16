using Dfe.Analytics.EFCore.Configuration;

namespace Dfe.Analytics.EFCore.Tests.Configuration;

public class DatabaseSyncConfigurationTests
{
    [Fact]
    public void Equals_ConfigurationsAreEquivalent_ReturnsTrue()
    {
        // Arrange
        var first = CreateConfiguration();
        var second = CreateConfiguration();

        // Act & Assert
        Assert.True(first.Equals(second));
    }

    [Fact]
    public void Equals_DifferentAirbyteSyncMode_ReturnsFalse()
    {
        // Arrange
        var first = CreateConfiguration(airbyteSyncMode: "incremental_append");
        var second = CreateConfiguration(airbyteSyncMode: "incremental_append_dedup");

        // Act & Assert
        Assert.False(first.Equals(second));
    }

    [Fact]
    public void Equals_DifferentDbContextName_ReturnsFalse()
    {
        // Arrange
        var first = CreateConfiguration(dbContextName: "ContextA");
        var second = CreateConfiguration(dbContextName: "ContextB");

        // Act & Assert
        Assert.False(first.Equals(second));
    }

    private static DatabaseSyncConfiguration CreateConfiguration(
        string dbContextName = "TestContext",
        string airbyteSyncMode = "incremental_append") =>
        new()
        {
            DbContextName = dbContextName,
            AirbyteSyncMode = airbyteSyncMode,
            Tables =
            [
                new TableSyncInfo
                {
                    Name = "TestEntity",
                    PrimaryKey = new TablePrimaryKeySyncInfo { ColumnNames = ["TestEntityId"] },
                    Columns =
                    [
                        new ColumnSyncInfo { Name = "TestEntityId", Hidden = false }
                    ]
                }
            ]
        };
}
