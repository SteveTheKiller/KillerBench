namespace KillerBench.Engine.Workflows;

internal sealed class DiagnosticProgress(IProgress<string> observer) : IProgress<string>
{
    public void Report(string value)
    {
        try { observer.Report(value); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}
