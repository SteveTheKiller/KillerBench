using System.Text.Json;
using KillerBench.Engine;

internal static class WindowsCapabilityChecks
{
    internal static async Task RunAsync()
    {
        ToolResult Evidence(object[] capabilities, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new { complete, capabilities }, Capture.JsonOptions), "", false);
        object Capability(string name, string state) => new { name, state };

        var stable = WindowsCapabilityDiagnostics.Interpret(Evidence([
            Capability("OpenSSH.Client~~~~0.0.1.0", "Installed"),
            Capability("Rsat.Dns.Tools~~~~0.0.1.0", "NotPresent"),
            Capability("XPS.Viewer~~~~0.0.1.0", "Staged"),
            Capability("Legacy~~~~0.0.1.0", "Removed"),
            Capability("Old~~~~0.0.1.0", "Superseded")
        ]));
        Require(stable is { State: CheckState.Information, Repair: null } && stable.Detail.Contains("Installed: 1", StringComparison.Ordinal) &&
            stable.Detail.Contains("NotPresent: 1", StringComparison.Ordinal), "Stable capability states remain informational");

        var pending = WindowsCapabilityDiagnostics.Interpret(Evidence([
            Capability("Language.OCR~~~en-US~0.0.1.0", "InstallPending"),
            Capability("OpenSSH.Server~~~~0.0.1.0", "UninstallPending"),
            Capability("Media.WindowsMediaPlayer~~~~0.0.12.0", "PartiallyInstalled")
        ]));
        Require(pending is { State: CheckState.Attention, Repair: null } && pending.Detail.Contains("Pending or partial capabilities", StringComparison.Ordinal),
            "Pending capability transitions request review without selecting an action");
        Require(WindowsCapabilityDiagnostics.Interpret(Evidence([])).State == CheckState.Information,
            "An empty complete capability inventory remains informational");

        foreach (var invalid in new[]
        {
            Evidence([], false), Evidence([new { name = "OpenSSH.Client~~~~0.0.1.0" }]),
            Evidence([Capability(" capability", "Installed")]), Evidence([Capability("Capability", "FutureState")]),
            Evidence([Capability("Capability", "Installed"), Capability("capability", "NotPresent")]),
            new ToolResult(1, "{}", "failed", false), new ToolResult(0, "{}", "", true)
        }) Require(WindowsCapabilityDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Incomplete, malformed, duplicate, failed and truncated capability evidence remains unknown");

        var capped = Evidence(Enumerable.Range(1, 4097).Select(index => Capability("Capability" + index, "NotPresent")).ToArray());
        Require(WindowsCapabilityDiagnostics.Interpret(capped).State == CheckState.Unknown,
            "Oversized capability evidence cannot claim complete coverage");

        RepairStep? observed = null;
        var collected = await WindowsCapabilityDiagnostics.CheckAsync(execute: (step, _) =>
        {
            observed = step;
            return Task.FromResult(Evidence([]));
        });
        Require(observed is { Verification: true } && observed.Tool == PrintRecovery.Tool &&
            observed.Arguments.Last().Contains("Get-WindowsCapability -Online", StringComparison.Ordinal) &&
            observed.Arguments.Last().Contains("4097", StringComparison.Ordinal) && collected.State == CheckState.Information,
            "Native capability plan is bounded, online-image scoped and read only");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await WindowsCapabilityDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("provider should not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents capability collection");
        Console.WriteLine("PASS: bounded Windows capability states, pending review, malformed evidence and cancellation; fixtures only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
