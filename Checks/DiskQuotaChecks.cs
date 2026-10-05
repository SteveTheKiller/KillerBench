using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class DiskQuotaChecks
{
    internal static async Task RunAsync()
    {
        object Row(string drive = "C:", long state = 2, long defaultLimitBytes = 10L << 30,
            long defaultWarningBytes = 9L << 30, bool limitNotification = true, bool warningNotification = true) =>
            new { drive, state, defaultLimitBytes, defaultWarningBytes, limitNotification, warningNotification };
        ToolResult Evidence(object[] settings, bool complete = true, int unresolved = 0) =>
            new(0, JsonSerializer.Serialize(new { complete, unresolved, settings }), "", false);

        var configuredEvidence = Evidence([Row(), Row("D:", 1, -1, 5L << 30, false, true)]);
        var configured = DiskQuotaDiagnostics.Interpret(configuredEvidence);
        Require(configured is { State: CheckState.Information, Repair: null } && ReferenceEquals(configured.Evidence, configuredEvidence) &&
            configured.Detail.Contains("C: enforced", StringComparison.Ordinal) && configured.Detail.Contains("D: tracked", StringComparison.Ordinal) &&
            !configured.Detail.Contains("S-1-", StringComparison.OrdinalIgnoreCase),
            "Configured volume quota settings remain informational and exclude user identities");
        Require(DiskQuotaDiagnostics.Interpret(Evidence([])).State == CheckState.Information,
            "An empty complete quota inventory remains informational");

        foreach (var invalid in new[]
        {
            Evidence([Row()], complete: false), Evidence([Row()], unresolved: 1), Evidence([Row(state: 3)]),
            Evidence([Row(defaultLimitBytes: 100, defaultWarningBytes: 101)]), Evidence([Row(defaultLimitBytes: -2)]),
            Evidence([Row(), Row("c:")]), Evidence([.. Enumerable.Range(0, 65).Select(index => Row(((char)('A' + index % 26)).ToString() + ":"))]),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            configuredEvidence with { ExitCode = 5 }, configuredEvidence with { Error = "denied" },
            configuredEvidence with { TimedOut = true }, configuredEvidence with { Truncated = true }
        }) Require(DiskQuotaDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Incomplete, unresolved, contradictory, duplicate, excessive, malformed and failed quota evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await DiskQuotaDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(configuredEvidence);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Last().Contains("Win32_QuotaSetting", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Win32_DiskQuota", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Set-CimInstance", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("fsutil quota", StringComparison.OrdinalIgnoreCase),
            "Native plan reads aggregate volume settings without user quota rows or changes");
        Require((await DiskQuotaDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"))).State == CheckState.Unknown,
            "Quota provider failure retains an unknown result");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await DiskQuotaDiagnostics.CheckAsync((_, _) => throw new Exception("Provider must not run"), token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents disk quota collection");
        Console.WriteLine("PASS: bounded disk quota state, defaults, notification settings, privacy and read-only collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
