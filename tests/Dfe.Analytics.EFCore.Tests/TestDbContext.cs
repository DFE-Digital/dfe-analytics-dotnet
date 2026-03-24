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

        var baseEntityConfiguration = modelBuilder.Entity<BaseEntity>();
        baseEntityConfiguration.IncludeInAnalyticsSync(hidden: false);
        baseEntityConfiguration.HasKey(b => b.Id);
        baseEntityConfiguration.HasDiscriminator(b => b.Discriminator)
            .HasValue<DerivedEntity1>(nameof(DerivedEntity1))
            .HasValue<DerivedEntity2>(nameof(DerivedEntity2));

        var derivedEntity1Configuration = modelBuilder.Entity<DerivedEntity1>();
        derivedEntity1Configuration.IncludeInAnalyticsSync(includeAllColumns: false, hidden: false);

        var derivedEntity2Configuration = modelBuilder.Entity<DerivedEntity2>();
        derivedEntity2Configuration.IncludeInAnalyticsSync(includeAllColumns: true, hidden: false);
    }
}
