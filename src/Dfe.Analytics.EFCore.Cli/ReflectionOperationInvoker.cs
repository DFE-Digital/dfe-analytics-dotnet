using System.Reflection;
using System.Runtime.Loader;

namespace Dfe.Analytics.EFCore.Cli;

internal class ReflectionOperationInvoker
{
    public async Task InvokeAsync(string name, string serializedOptions, string appAssemblyPath)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(serializedOptions);
        ArgumentNullException.ThrowIfNull(appAssemblyPath);

        var resolver = new AssemblyDependencyResolver(appAssemblyPath);
        var loadContext = new AppAssemblyLoadContext(resolver);

        using (loadContext.EnterContextualReflection())
        {
            var analyticsLibAssembly = loadContext.LoadFromAssemblyName(new AssemblyName("Dfe.Analytics.EFCore"));

            var operationsTypeName = "Dfe.Analytics.EFCore.Operations.DfeAnalyticsEFCoreOperations";
            var operationsType = analyticsLibAssembly.GetType(operationsTypeName) ??
                throw new InvalidOperationException($"Could not find type '{operationsTypeName}' in assembly '{analyticsLibAssembly.FullName}'.");
            var operationsInstance = Activator.CreateInstance(operationsType) ??
                throw new InvalidOperationException($"Could not create instance of type '{operationsType.FullName}'.'");

            var commandMethod = operationsType.GetMethod(name) ??
                throw new InvalidOperationException($"Could not find method '${name}' in type '{operationsType.FullName}'.");

            var commandOptionsType = commandMethod.GetParameters()[0].ParameterType;
            var commandOptionsFromJsonMethod = commandOptionsType.GetMethod("FromJson", BindingFlags.Public | BindingFlags.Static) ??
                throw new InvalidOperationException($"Could not find method 'FromJson' in type '{commandOptionsType.FullName}'.");
            var commandOptions = commandOptionsFromJsonMethod.Invoke(null, [serializedOptions]);

            await (Task)commandMethod.Invoke(operationsInstance, [commandOptions])!;
        }
    }
}
