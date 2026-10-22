using Dfe.Analytics.EFCore.Cli;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dfe.Analytics.EFCore.Tests;

// Only cases that resolve without the application's service provider are covered here: falling through to it
// would run this test assembly's own entry point.
public class DbContextHelperTests
{
    [Fact]
    public void CreateDbContext_DbContextHasDesignTimeFactory_CreatesDbContextWithFactory()
    {
        // Arrange
        var dbContextTypeName = typeof(FactoryCreatedTestDbContext).AssemblyQualifiedName!;

        // Act
        using var dbContext = DbContextHelper.CreateDbContext(dbContextTypeName);

        // Assert
        var factoryCreatedDbContext = Assert.IsType<FactoryCreatedTestDbContext>(dbContext);
        Assert.Equal(nameof(FactoryCreatedTestDbContextFactory), factoryCreatedDbContext.CreatedBy);
        Assert.True(EF.IsDesignTime);
    }

    [Fact]
    public void CreateDbContext_TypeDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbContextTypeName = "Dfe.Analytics.EFCore.Tests.MissingDbContext, Dfe.Analytics.EFCore.Tests";

        // Act
        var ex = Record.Exception(() => DbContextHelper.CreateDbContext(dbContextTypeName));

        // Assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Equal($"Could not load '{dbContextTypeName}'.", ex.Message);
    }

    [Fact]
    public void CreateDbContext_TypeIsNotDbContext_ThrowsInvalidOperationException()
    {
        // Arrange
        var dbContextTypeName = typeof(TestEntity).AssemblyQualifiedName!;

        // Act
        var ex = Record.Exception(() => DbContextHelper.CreateDbContext(dbContextTypeName));

        // Assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Equal($"'{dbContextTypeName}' is not a DbContext.", ex.Message);
    }

    [Fact]
    public void CreateDbContext_DesignTimeFactoryThrows_ThrowsInvalidOperationExceptionWrappingFactoryException()
    {
        // Arrange
        var dbContextTypeName = typeof(FailingFactoryTestDbContext).AssemblyQualifiedName!;

        // Act
        var ex = Record.Exception(() => DbContextHelper.CreateDbContext(dbContextTypeName));

        // Assert
        Assert.IsType<InvalidOperationException>(ex);
        Assert.Equal($"Unable to create a '{nameof(FailingFactoryTestDbContext)}': No connection string.", ex.Message);
        Assert.IsType<NotSupportedException>(ex.InnerException);
    }
}

public class FactoryCreatedTestDbContext(string createdBy) : TestDbContext
{
    public string CreatedBy { get; } = createdBy;
}

public class FactoryCreatedTestDbContextFactory : IDesignTimeDbContextFactory<FactoryCreatedTestDbContext>
{
    public FactoryCreatedTestDbContext CreateDbContext(string[] args) => new(nameof(FactoryCreatedTestDbContextFactory));
}

public class FailingFactoryTestDbContext : TestDbContext;

public class FailingFactoryTestDbContextFactory : IDesignTimeDbContextFactory<FailingFactoryTestDbContext>
{
    public FailingFactoryTestDbContext CreateDbContext(string[] args) => throw new NotSupportedException("No connection string.");
}
