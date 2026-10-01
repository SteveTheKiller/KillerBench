using KillerBench.Engine;

internal static class BootPerformanceEventChecks
{
    public static void Run()
    {
        static string Entry(int id, int level, ulong record, string extra = "") =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='Microsoft-Windows-Diagnostics-Performance'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/><Computer>PRIVATE-PC</Computer></System><EventData><Data Name='BootDuration'>120000</Data><Data Name='MainPathBootTime'>45000</Data><Data Name='Path'>C:\\PRIVATE\\app.exe</Data><Data Name='User'>PRIVATE\\User</Data>{extra}</EventData></Event>";
        var raw = new ToolResult(0, "<Events>" + Entry(100, 4, 1) + Entry(101, 3, 2) + Entry(201, 2, 3, "<Data Name='ShutdownDuration'>9000</Data>") + "</Events>", "", false);
        var sanitized = BootPerformanceEvents.Sanitize(raw);
        Require(sanitized.ExitCode == 0 && string.IsNullOrEmpty(sanitized.Error) && sanitized.Output.Contains("BootDuration", StringComparison.Ordinal) &&
            sanitized.Output.Contains("ShutdownDuration", StringComparison.Ordinal) && !sanitized.Output.Contains("C:\\PRIVATE", StringComparison.Ordinal) &&
            !sanitized.Output.Contains("PRIVATE\\User", StringComparison.Ordinal) && !sanitized.Output.Contains("Name=\"Path\"", StringComparison.Ordinal),
            "Startup performance evidence retains bounded durations and removes private payload");
        var result = BootPerformanceEvents.Interpret(sanitized);
        Require(result.State == CheckState.Attention && result.Repair is null && result.Detail.Contains("3 records retained; 2 warning", StringComparison.Ordinal) &&
            result.Detail.Contains("Event 101, level 3: 1 records", StringComparison.Ordinal) && result.Detail.Contains("Event 201, level 2: 1 records", StringComparison.Ordinal) &&
            !result.Detail.Contains("PRIVATE", StringComparison.Ordinal), "Startup performance history groups review events without inferring a repair");
        Require(BootPerformanceEvents.Interpret(BootPerformanceEvents.Sanitize(raw with { Output = "<Events/>" })).State == CheckState.Information,
            "Empty complete startup performance history remains informational");
        var malformed = BootPerformanceEvents.Sanitize(raw with { Output = "<Events>" });
        Require(malformed.Output.Length == 0 && malformed.Error.Length > 0 && BootPerformanceEvents.Interpret(malformed).State == CheckState.Unknown,
            "Malformed startup performance XML is discarded instead of retaining unsanitized payload");
        foreach (var invalid in new[]
        {
            sanitized with { ExitCode = 5 }, sanitized with { Error = "denied" }, sanitized with { TimedOut = true }, sanitized with { Truncated = true },
            BootPerformanceEvents.Sanitize(raw with { Output = raw.Output.Replace("Microsoft-Windows-Diagnostics-Performance", "Other", StringComparison.Ordinal) }),
            BootPerformanceEvents.Sanitize(raw with { Output = "<Events>" + Entry(101, 3, 1) + Entry(101, 3, 1) + "</Events>" }),
            BootPerformanceEvents.Sanitize(raw with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(101, 3, (ulong)index))) + "</Events>" })
        })
            Require(BootPerformanceEvents.Interpret(invalid).State == CheckState.Unknown,
                "Incomplete, duplicate, unexpected and capped startup performance history remains unknown");
        var badDuration = BootPerformanceEvents.Sanitize(raw with { Output = "<Events>" + Entry(100, 4, 1, "<Data Name='ShutdownDuration'>PRIVATE</Data><Data Name='BootPostBootTime'>999999999999</Data>") + "</Events>" });
        Require(!badDuration.Output.Contains("ShutdownDuration", StringComparison.Ordinal) && !badDuration.Output.Contains("BootPostBootTime", StringComparison.Ordinal),
            "Malformed and excessive duration fields are removed");
        try { BootPerformanceEvents.Interpret(sanitized, new CancellationToken(true)); throw new Exception("Startup performance history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: sanitized startup and shutdown performance history, bounded durations, incomplete evidence and cancellation; no startup changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
