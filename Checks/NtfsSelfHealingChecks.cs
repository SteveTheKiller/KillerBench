using System.IO;
using KillerBench.Engine;

internal static class NtfsSelfHealingChecks
{
    internal static async Task RunAsync()
    {
        static ToolResult Evidence(string volume, string flags) => new(0,
            $"Self healing state on {volume} is: {flags}\r\n", "", false);

        var enabled = NtfsSelfHealingDiagnostics.Interpret("C:", Evidence("C:", "0x1"));
        Require(enabled is { State: CheckState.Clear, Repair: null } && enabled.Detail.Contains("0x1", StringComparison.Ordinal),
            "General repair flags are retained as clear configuration evidence");
        Require(NtfsSelfHealingDiagnostics.Interpret("C:", Evidence("c:", "0x09")).State == CheckState.Clear,
            "Warn-on-data-loss repair mode is recognized without case-sensitive volume mismatch");
        var disabled = NtfsSelfHealingDiagnostics.Interpret("D:", Evidence("D:", "0x00"));
        Require(disabled is { State: CheckState.Attention, Repair: null } && disabled.Detail.Contains("management source", StringComparison.Ordinal),
            "Disabled self-healing requests review without selecting a setting change");

        foreach (var invalid in new[]
        {
            Evidence("D:", "0x1"), Evidence("C:", "0x10"), Evidence("C:", "1"),
            new ToolResult(0, "Self healing state on C: is: 0x1\nSelf healing state on C: is: 0x9", "", false),
            new ToolResult(1, "Self healing state on C: is: 0x1", "denied", false),
            Evidence("C:", "0x1") with { TimedOut = true }, Evidence("C:", "0x1") with { Truncated = true }
        })
            Require(NtfsSelfHealingDiagnostics.Interpret("C:", invalid).State == CheckState.Unknown,
                "Mismatched, unsupported, duplicate, failed and incomplete evidence remains unknown");

        RepairStep? planned = null;
        bool boundedToken = false;
        var collected = await NtfsSelfHealingDiagnostics.CheckAsync("c:", execute: (step, providerToken) =>
        {
            planned = step;
            boundedToken = providerToken.CanBeCanceled;
            return Task.FromResult(Evidence("C:", "0x9"));
        });
        Require(collected.State == CheckState.Clear && boundedToken && planned is { Tool: "fsutil.exe", Verification: true } &&
            planned.Arguments.SequenceEqual(["repair", "query", "C:"]),
            "Native collection uses only the exact selected-volume query form");

        var failure = await NtfsSelfHealingDiagnostics.CheckAsync("C:", execute: (_, _) => throw new IOException("fixture failure"));
        Require(failure is { State: CheckState.Unknown, Evidence.ExitCode: -1 }, "Provider failure remains unknown with evidence");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await NtfsSelfHealingDiagnostics.CheckAsync("C:", (_, _) => throw new Exception("Provider must not run"), token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents collection");
        Console.WriteLine("PASS: NTFS self-healing flags, exact volume identity, incomplete evidence and read-only planning; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
