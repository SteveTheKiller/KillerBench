using KillerBench.Engine;

internal static class CaptureCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool scan = args[0] == "scan";
        bool json = args[^1] == "--json";
        int count = args.Length - (json ? 1 : 0);
        if (scan ? count != 3 || args[1] != "--output" : count != 2)
        { Console.Error.WriteLine("Usage: scan --output <new-file.kbench> [--json] | open <file.kbench> [--json]"); return 2; }
        using var stop = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        ConsoleCancelEventHandler handler = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            CaptureSession session;
            if (scan)
            {
                if (File.Exists(args[2])) throw new IOException("Output already exists. Choose a new capture filename.");
                session = await Capture.CollectAsync(WindowsUpdateCollectors.Create(), new ConsoleProgress(), stop.Token);
                await Capture.SaveAsync(session, args[2], overwrite: false);
                Console.Error.WriteLine("Saved capture: " + Path.GetFullPath(args[2]));
            }
            else session = await Capture.OpenAsync(args[1], stop.Token);
            if (json) Console.WriteLine(Capture.ReportJson(session));
            else
            {
                Console.WriteLine($"{session.Machine} | {session.OperatingSystem} | {session.Elevation}\n{session.StartedUtc:O} through {session.FinishedUtc:O}");
                foreach (var source in session.Sources)
                {
                    Console.WriteLine($"\n{source.Name}: {source.Status}\n{source.Detail}");
                    if (source.Evidence is null) continue;
                    foreach (var finding in Capture.Analyze(source).Findings)
                    {
                        Console.WriteLine($"{finding.RuleId}: {finding.Title}\n{finding.Explanation}\nNext: {finding.NextStep}");
                        foreach (var reference in finding.Evidence) Console.WriteLine($"  {reference.Source}:{reference.LineNumber} {reference.Excerpt}");
                    }
                }
                Console.WriteLine("\nNo current-health verdict. No repairs executed.");
            }
            return stop.IsCancellationRequested ? 130 : session.Partial ? 3 : 0;
        }
        catch (OperationCanceledException) { Console.Error.WriteLine("Operation stopped."); return 130; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        { Console.Error.WriteLine(ex.Message); return 1; }
        finally { Console.CancelKeyPress -= handler; }
    }
    private sealed class ConsoleProgress : IProgress<string>
    { public void Report(string value) => Console.Error.WriteLine(value); }
}
