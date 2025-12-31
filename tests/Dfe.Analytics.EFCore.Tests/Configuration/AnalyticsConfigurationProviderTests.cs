using Dfe.Analytics.EFCore.Configuration;
using Microsoft.EntityFrameworkCore;

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
                        Assert.True(column.IsPii);
                    },
                    column =>
                    {
                        Assert.Equal("Name", column.Name);
                        Assert.False(column.IsPii);
                    },
                    column =>
                    {
                        Assert.Equal("TestEntityId", column.Name);
                        Assert.False(column.IsPii);
                    }
                );
            });
    }

    private class TestDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            var testEntityConfiguration = modelBuilder.Entity<TestEntity>();
            testEntityConfiguration.HasAnalyticsSync(columnsArePii: false);
            testEntityConfiguration.HasKey(t => t.TestEntityId);
            testEntityConfiguration.Property(t => t.Name);
            testEntityConfiguration.Property(t => t.DateOfBirth).HasAnalyticsSync(isPii: true);
            testEntityConfiguration.Ignore(t => t.Ignored);
        }
    }

    private class TestEntity
    {
        public int TestEntityId { get; set; }
        public string? Name { get; set; }
        public string? Ignored { get; set; }
        public DateOnly DateOfBirth { get; set; }
    }
}
