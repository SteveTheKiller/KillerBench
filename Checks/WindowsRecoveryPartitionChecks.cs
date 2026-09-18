using System.Text.Json;
using KillerBench.Engine;

internal static class WindowsRecoveryPartitionChecks
{
    internal static void Run()
    {
        var enabled = Recovery(WindowsRecoveryState.Enabled, CheckState.Clear,
            @"\\?\GLOBALROOT\device\harddisk0\partition4\Recovery\WindowsRE");
        var exact = WindowsRecoveryPartitionCorrelation.Interpret(enabled, Partitions(true,
            new { disk = 0, number = 4, type = "Recovery", size = 900000000UL, hidden = true, readOnly = false }));
        Require(exact is { State: CheckState.Information, Repair: null } && exact.Detail.Contains("exact partition is present") &&
            WindowsRecoveryOperationalCorrelation.Check(enabled, exact) is { State: CheckState.Information, Repair: null },
            "Enabled Windows Recovery correlates one exact current partition without claiming boot readiness");

        var absent = WindowsRecoveryPartitionCorrelation.Interpret(enabled, Partitions(true,
            new { disk = 0, number = 3, type = "Basic", size = 900000000UL, hidden = true, readOnly = false }));
        Require(absent.State == CheckState.Attention && WindowsRecoveryOperationalCorrelation.Check(enabled, absent).State == CheckState.Attention,
            "An enabled configuration with no exact partition match requests review");

        var disabled = Recovery(WindowsRecoveryState.Disabled, CheckState.Repairable, null, RepairKind.WindowsRecoveryEnvironment);
        var skipped = WindowsRecoveryPartitionCorrelation.Interpret(disabled, null);
        Require(skipped.State == CheckState.Information && WindowsRecoveryOperationalCorrelation.Check(disabled, skipped).State == CheckState.Attention,
            "Disabled Windows Recovery does not claim a configured partition and remains eligible only through its guarded repair");

        foreach (var invalid in new ToolResult?[]
        {
            null, Partitions(false), Partitions(true) with { ExitCode = 1 }, Partitions(true) with { TimedOut = true },
            Partitions(true) with { Truncated = true }, Partitions(true) with { Error = "fixture" },
            new(0, "broken", "", false), Partitions(true,
                new { disk = 0, number = 4, type = "Recovery", size = 1UL, hidden = true, readOnly = false },
                new { disk = 0, number = 4, type = "Recovery", size = 1UL, hidden = true, readOnly = false })
        })
            Require(WindowsRecoveryPartitionCorrelation.Interpret(enabled, invalid).State == CheckState.Unknown,
                "Unavailable, incomplete, malformed and duplicate partition evidence fails closed");

        var noLocation = Recovery(WindowsRecoveryState.Enabled, CheckState.Clear, null);
        Require(WindowsRecoveryPartitionCorrelation.Interpret(noLocation, Partitions(true)).State == CheckState.Unknown,
            "Enabled recovery without a recognized local location remains unknown");
        Require(WindowsRecoveryPartitionCorrelation.Interpret(enabled with { State = CheckState.Repairable }, Partitions(true)).State == CheckState.Unknown,
            "Caller state cannot override contradictory retained recovery evidence");
        try { WindowsRecoveryPartitionCorrelation.Interpret(enabled, Partitions(true), new CancellationToken(true)); throw new Exception("Recovery partition cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Windows Recovery configured partition correlation is exact, bounded, read only and conservative about boot readiness.");
    }

    private static WorkflowCheck Recovery(WindowsRecoveryState state, CheckState checkState, string? location, RepairKind? repair = null)
    {
        var status = new WindowsRecoveryStatus(state, location, "fixture-bcd");
        return new("Windows Recovery Environment", checkState, "fixture", repair,
            new ToolResult(0, JsonSerializer.Serialize(status) + "\n\nfixture", "", false));
    }

    private static ToolResult Partitions(bool complete, params object[] rows) => new(0,
        JsonSerializer.Serialize(new { complete, partitions = rows }), "", false);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
