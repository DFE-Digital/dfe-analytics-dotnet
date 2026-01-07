using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dfe.Analytics.EFCore.Cli;

internal static class DbContextHelper
{
    public static DbContext CreateDbContext(string dbContextTypeName)
    {
        ArgumentNullException.ThrowIfNull(dbContextTypeName);

        AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

        try
        {
            var dbContextType = Type.GetType(dbContextTypeName) ??
                throw new InvalidOperationException($"Could not load '${dbContextTypeName}'.");

            return DbContextActivator.CreateInstance(dbContextType);
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;
        }

        Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
        {
            var appBasePath = AppContext.BaseDirectory;
            var assemblyName = new AssemblyName(args.Name);

            var assemblyPath = Path.Combine(appBasePath, assemblyName.Name + ".dll");
            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFrom(assemblyPath);
            }

            return null;
        }
    }
}
