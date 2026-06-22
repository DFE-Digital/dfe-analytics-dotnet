using Dfe.Analytics.EFCore.Description;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Dfe.Analytics.EFCore.Tests;

public class TestDbContext(AirbyteSyncMode? airbyteSyncMode = null) : DbContext
{
    private AirbyteSyncMode? AirbyteSyncMode => airbyteSyncMode;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql();

        // EF Core caches the built model per context type by default. Vary the cache key by the configured
        // sync mode so tests exercising different sync modes don't reuse each other's cached model.
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, SyncModeAwareModelCacheKeyFactory>();
    }

    private sealed class SyncModeAwareModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) =>
            (context.GetType(), (context as TestDbContext)?.AirbyteSyncMode, designTime);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (airbyteSyncMode is { } syncMode)
        {
            modelBuilder.ConfigureAnalyticsSync(syncMode);
        }

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
