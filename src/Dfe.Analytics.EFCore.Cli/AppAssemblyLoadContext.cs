using System.Reflection;
using System.Runtime.Loader;

namespace Dfe.Analytics.EFCore.Cli;

internal class AppAssemblyLoadContext(AssemblyDependencyResolver resolver) : AssemblyLoadContext
{
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        ArgumentNullException.ThrowIfNull(assemblyName);

        var assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);

        if (assemblyPath is not null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }
}
