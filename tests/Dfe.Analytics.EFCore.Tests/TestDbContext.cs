using Microsoft.EntityFrameworkCore;

namespace Dfe.Analytics.EFCore.Tests;

public class TestDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var testEntityConfiguration = modelBuilder.Entity<TestEntity>();
        testEntityConfiguration.IncludeInAnalyticsSync(hidden: false);
        testEntityConfiguration.HasKey(t => t.TestEntityId);
        testEntityConfiguration.Property(t => t.Name).ConfigureAnalyticsSync(hidden: true);
        testEntityConfiguration.Property(t => t.DateOfBirth);
        testEntityConfiguration.Ignore(t => t.Ignored);
    }
}
