using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dfe.Analytics.Cli;

internal static class DbContextHelper
{
    public static DbContext CreateDbContext(
        string dbContextAssemblyPath,
        string dbContextTypeName,
        string connectionString)
    {
        if (!File.Exists(dbContextAssemblyPath))
        {
            throw new FileNotFoundException($"The specified DbContext assembly could not be found: '{dbContextAssemblyPath}'.");
        }

        var appBasePath = Path.GetDirectoryName(dbContextAssemblyPath)!;
        var executingDllDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        try
        {
            var dbContextAssemblyName = AssemblyName.GetAssemblyName(dbContextAssemblyPath);
            var dbContextAssembly = Assembly.Load(dbContextAssemblyName);

            var dbContextType = dbContextAssembly.GetType(dbContextTypeName) ??
                throw new InvalidOperationException($"The specified DbContext type '{dbContextTypeName}' could not be found in assembly '{dbContextAssembly.FullName}'.");

            var dbContext = DbContextActivator.CreateInstance(dbContextType);
            dbContext.Database.SetConnectionString(connectionString);
            return dbContext;
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
        }

        Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
        {
            // Ensure DLLs in both the executing assembly directory and the DbContext assembly directory can be resolved

            var assemblyName = new AssemblyName(args.Name);

            var currentAssemblyBasePath = Path.Combine(executingDllDirectory, assemblyName.Name + ".dll");
            if (File.Exists(currentAssemblyBasePath))
            {
                return Assembly.LoadFrom(currentAssemblyBasePath);
            }

            var appAssemblyPath = Path.Combine(appBasePath, assemblyName.Name + ".dll");
            if (File.Exists(appAssemblyPath))
            {
                return Assembly.LoadFrom(appAssemblyPath);
            }

            return null;
        }
    }
}
