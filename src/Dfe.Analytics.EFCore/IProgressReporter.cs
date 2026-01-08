namespace Dfe.Analytics.EFCore;

public interface IProgressReporter
{
    void WriteLine(string line);
}

public class ConsoleProgressReporter : IProgressReporter
{
    public void WriteLine(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        Console.WriteLine(line);
    }
}
