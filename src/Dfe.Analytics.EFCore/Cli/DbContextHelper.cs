using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dfe.Analytics.EFCore.Cli;

// Creates a DbContext the same way EF Core's DbContextActivator does, but without needing
// Microsoft.EntityFrameworkCore.Design at runtime; apps commonly reference that package with PrivateAssets="all",
// which keeps it out of the deps.json the tools run with. Everything used here lives in Microsoft.EntityFrameworkCore.
internal static class DbContextHelper
{
    public static DbContext CreateDbContext(string dbContextTypeName)
    {
        ArgumentNullException.ThrowIfNull(dbContextTypeName);

        var dbContextType = Type.GetType(dbContextTypeName) ??
            throw new InvalidOperationException($"Could not load '{dbContextTypeName}'.");

        if (!typeof(DbContext).IsAssignableFrom(dbContextType))
        {
            throw new InvalidOperationException($"'{dbContextTypeName}' is not a DbContext.");
        }

        EF.IsDesignTime = true;
        SetEnvironment();

        try
        {
            return CreateFromDesignTimeFactory(dbContextType) ?? CreateFromServiceProvider(dbContextType);
        }
        catch (Exception ex)
        {
            var innerException = ex is TargetInvocationException { InnerException: { } tie } ? tie : ex;

            throw new InvalidOperationException(
                $"Unable to create a '{dbContextType.Name}': {innerException.Message}",
                innerException);
        }
    }

    private static DbContext? CreateFromDesignTimeFactory(Type dbContextType)
    {
        var factoryInterface = typeof(IDesignTimeDbContextFactory<>).MakeGenericType(dbContextType);

        var factoryType = GetConstructibleTypes(dbContextType.Assembly).FirstOrDefault(factoryInterface.IsAssignableFrom);
        if (factoryType is null)
        {
            return null;
        }

        // IDesignTimeDbContextFactory<TContext> is covariant
        var factory = (IDesignTimeDbContextFactory<DbContext>)Activator.CreateInstance(factoryType)!;
        return factory.CreateDbContext([]);
    }

    private static DbContext CreateFromServiceProvider(Type dbContextType)
    {
        var services = GetApplicationServices(dbContextType.Assembly) ?? new ServiceCollection().BuildServiceProvider();

        var dbContextFactoryInterface = typeof(IDbContextFactory<>).MakeGenericType(dbContextType);
        if (services.GetService(dbContextFactoryInterface) is { } dbContextFactory)
        {
            return (DbContext)dbContextFactoryInterface.GetMethod(nameof(IDbContextFactory<DbContext>.CreateDbContext))!
                .Invoke(dbContextFactory, null)!;
        }

        return (DbContext)ActivatorUtilities.GetServiceOrCreateInstance(services, dbContextType);
    }

    private static IServiceProvider? GetApplicationServices(Assembly startupAssembly)
    {
        // Only finds anything when the DbContext lives in the application's own assembly, as with DbContextActivator
        var serviceProviderFactory = HostFactoryResolver.ResolveServiceProviderFactory(startupAssembly);
        if (serviceProviderFactory is null)
        {
            return null;
        }

        try
        {
#pragma warning disable CA2000  // The scope has to outlive the DbContext resolved from it; the process ends soon after
            return serviceProviderFactory([])?.CreateScope().ServiceProvider;
#pragma warning restore CA2000
        }
#pragma warning disable CA1031  // DbContextActivator ignores failures here too, and falls back to an empty service provider
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    private static void SetEnvironment()
    {
        // Matches the design-time default, so the app's host is built with the same configuration as under the EF tools
        var aspNetCoreEnvironment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        var dotnetEnvironment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var environment = aspNetCoreEnvironment ?? dotnetEnvironment ?? "Development";

        if (aspNetCoreEnvironment is null)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", environment);
        }

        if (dotnetEnvironment is null)
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", environment);
        }
    }

    private static IEnumerable<Type> GetConstructibleTypes(Assembly assembly)
    {
        Type?[] types;

        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types;
        }

        return types.OfType<Type>().Where(t => t is { IsAbstract: false, IsGenericTypeDefinition: false });
    }
}
