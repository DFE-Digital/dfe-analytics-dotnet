using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dfe.Analytics.EFCore.Operations;

internal static class DbContextHelper
{
    public static DbContext CreateDbContext(string dbContextAssemblyPath, string dbContextTypeName)
    {
        if (!File.Exists(dbContextAssemblyPath))
        {
            throw new FileNotFoundException($"The specified DbContext assembly could not be found: '{dbContextAssemblyPath}'.");
        }

        var appBasePath = Path.GetDirectoryName(dbContextAssemblyPath)!;

        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        try
        {
            var dbContextAssemblyName = AssemblyName.GetAssemblyName(dbContextAssemblyPath);
            var dbContextAssembly = Assembly.Load(dbContextAssemblyName);

            var dbContextType = dbContextAssembly.GetType(dbContextTypeName) ??
                throw new InvalidOperationException($"The specified DbContext type '{dbContextTypeName}' could not be found in assembly '{dbContextAssembly.FullName}'.");

            var dbContext = DbContextActivator.CreateInstance(dbContextType);
            return dbContext;
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
        }

        Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            var appAssemblyPath = Path.Combine(appBasePath, assemblyName.Name + ".dll");
            if (File.Exists(appAssemblyPath))
            {
                return Assembly.LoadFrom(appAssemblyPath);
            }

            return null;
        }
    }
}
