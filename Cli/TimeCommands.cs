using KillerBench.Engine;
using System.Text.Json;

internal static class TimeCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        using var stop = new CancellationTokenSource();
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            if (args[0] == "time-recover")
            {
                if (args.Length != 3 || args[1] != "--folder" || string.IsNullOrWhiteSpace(args[2]))
                    throw new ArgumentException("Use time-recover --folder PATH to check an interrupted time operation without retrying it.");
                var recovery = await TimeConfigurationRecovery.ReconcileAsync(Path.GetFullPath(args[2]), token: stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(recovery, Capture.JsonOptions));
                return recovery.GetExitCode();
            }
            var source = TimeSourceMode.Existing;
            bool sourceSet = false;
            bool execute = false;
            var peers = new List<string>();
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--execute" && args[0] == "time-configure" && !execute)
                { execute = true; continue; }
                if (args[i] == "--peer" && i + 1 < args.Length) { peers.Add(args[++i]); continue; }
                if (args[i] == "--source" && !sourceSet && i + 1 < args.Length &&
                    Enum.TryParse(args[++i], true, out source) && Enum.IsDefined(source)) { sourceSet = true; continue; }
                throw new ArgumentException("Use time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...].");
            }
            var options = new TimeOptions(source, peers);
            if (args[0] == "time-configure" && source is not (TimeSourceMode.CustomPeers or TimeSourceMode.DomainHierarchy))
                throw new ArgumentException("time-configure requires --source DomainHierarchy or CustomPeers. CustomPeers also requires at least one --peer HOST.");
            if (args[0] == "time-verify")
            {
                if (source is not (TimeSourceMode.CustomPeers or TimeSourceMode.DomainHierarchy))
                    throw new ArgumentException("time-verify requires --source DomainHierarchy or CustomPeers.");
                bool verified = await TimeConfiguration.VerifyAsync(options, token: stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(new { Verified = verified, Note = verified
                    ? "Stored configuration and reported synchronization match the selected peers. Absolute clock accuracy is not measured. No settings changed."
                    : "Selected synchronization could not be verified. Configuration, policy ownership or status may be unavailable or different. No settings changed." }, Capture.JsonOptions));
                return verified ? 0 : 3;
            }
            var (environment, plan) = await TimePlanning.PreviewAsync(options, token: stop.Token);
            if (execute)
            {
                string timeFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Time");
                if (TimeConfigurationRecovery.HasPendingRecord(timeFolder))
                {
                    Console.Error.WriteLine("A pending time-source change will be checked without retrying configuration or synchronization.");
                    var recovery = await TimeConfigurationRecovery.ReconcileAsync(timeFolder, token: stop.Token);
                    Console.WriteLine(JsonSerializer.Serialize(recovery, Capture.JsonOptions));
                    return recovery.GetExitCode();
                }
                if (!plan.ReviewPassed)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { Environment = environment, Plan = plan,
                        Note = "Prerequisites did not pass. No source change started." }, Capture.JsonOptions));
                    return 3;
                }
                Console.Error.WriteLine(source == TimeSourceMode.DomainHierarchy
                    ? "Selecting the Active Directory domain hierarchy and requesting source rediscovery. The system clock may change; no automatic rollback."
                    : "Changing configured NTP peers and requesting synchronization. The system clock may change; no automatic rollback.");
                var result = await NativeTimeConfiguration.RunAsync(options,
                    timeFolder, new TimeProgress(), stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return stop.IsCancellationRequested ? 130 : result.Status == "Verified" ? 0 : 3;
            }
            Console.WriteLine(JsonSerializer.Serialize(new { Environment = environment, Plan = plan, Note = "Preview only. No clock, service or source settings changed." }, Capture.JsonOptions));
            return plan.ReviewPassed ? 0 : 3;
        }
        catch (OperationCanceledException) { return 130; }
        catch (ArgumentException error) { Console.Error.WriteLine(error.Message); return 2; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        { Console.Error.WriteLine("Time operation could not complete: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
    private sealed class TimeProgress : IProgress<string>
    {
        public void Report(string value) => Console.Error.WriteLine(value);
    }
}
