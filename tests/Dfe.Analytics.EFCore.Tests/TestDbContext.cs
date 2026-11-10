using Dfe.Analytics.EFCore.Description;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.Internal;

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
        testEntityConfiguration.Property(t => t.Email).ConfigureAnalyticsSync(policyTag: "email");
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

/// <summary>
/// A <see cref="TestDbContext"/> with a model-level sync mode and a table-level sync mode override for <see cref="TestEntity"/>.
/// </summary>
public class SyncModeTestDbContext : TestDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ConfigureAnalyticsSync(AirbyteSyncMode.IncrementalAppendDeduped);
        modelBuilder.Entity<TestEntity>().IncludeInAnalyticsSync(hidden: false, airbyteSyncMode: AirbyteSyncMode.IncrementalAppend);
    }
}

/// <summary>
/// A <see cref="TestDbContext"/> with an (invalid) sync mode configured on a derived entity type.
/// </summary>
public class DerivedEntitySyncModeTestDbContext : TestDbContext
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<DerivedEntity2>()
            .IncludeInAnalyticsSync(includeAllColumns: true, hidden: false, airbyteSyncMode: AirbyteSyncMode.IncrementalAppendDeduped);
    }
}

/// <summary>
/// A <see cref="TestDbContext"/> that reports no pending migrations without connecting to a database.
/// </summary>
public class NoPendingMigrationsTestDbContext : TestDbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.ReplaceService<IHistoryRepository, EmptyHistoryRepository>();
    }

#pragma warning disable EF1001  // Internal EF Core API usage
    private sealed class EmptyHistoryRepository(HistoryRepositoryDependencies dependencies) : NpgsqlHistoryRepository(dependencies)
    {
        // Returning false here means no attempt is made to read applied migrations from the database
        public override bool Exists() => false;

        public override Task<bool> ExistsAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);
    }
#pragma warning restore EF1001
}
