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

        var configCommand = new Command("config")
        {
            Commands.GetConfigApplyCommand(),
            Commands.GetConfigCreateCommand()
        };

        var rootCommand = new RootCommand("Tools for DfE Analytics.") { configCommand };

        var args = Environment.GetCommandLineArgs().SkipWhile(arg => arg != "--").Skip(1).ToArray();

        var parseResult = rootCommand.Parse(args);
        var result = parseResult.Invoke();
        Environment.Exit(result);
    }
}
