using System.CommandLine;
using System.Reflection;
using System.Runtime.Loader;
using Dfe.Analytics.EFCore.Cli;

internal class StartupHook
{
    public static void Initialize()
    {
        var appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
        {
            var assemblyPath = Path.Combine(appDirectory, assemblyName.Name + ".dll");
            return File.Exists(assemblyPath) ? context.LoadFromAssemblyPath(assemblyPath) : null;
        };

        // Resolve relative paths against the application's directory so the tools can be invoked from anywhere
        // (e.g. directly from a container orchestrator, without a shell to change directory first).
        Directory.SetCurrentDirectory(appDirectory);

        var configCommand = new Command("config")
        {
            Commands.GetConfigApplyCommand(),
            Commands.GetConfigCreateCommand()
        };

        var runtimeConfigCommand = new Command("runtimeconfig")
        {
            Commands.GetRuntimeConfigCreateCommand()
        };
        runtimeConfigCommand.Hidden = true;

        var rootCommand = new RootCommand("Tools for DfE Analytics.") { configCommand, runtimeConfigCommand };

        var args = Environment.GetCommandLineArgs().SkipWhile(arg => arg != "--").Skip(1).ToArray();

        var parseResult = rootCommand.Parse(args);
        var result = parseResult.Invoke();
        Environment.Exit(result);
    }
}
