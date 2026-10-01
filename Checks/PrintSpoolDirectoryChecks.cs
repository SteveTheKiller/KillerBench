using KillerBench.Engine;
using System.IO;

internal static class PrintSpoolDirectoryChecks
{
    public static void Run()
    {
        static PrintSpoolDirectorySnapshot Snapshot(bool exists = true, bool? reparse = false, DriveType? driveType = DriveType.Fixed,
            long? available = 10L * 1024 * 1024 * 1024, bool complete = true, string? issue = null) =>
            new(complete, Path.Combine(Environment.SystemDirectory, "spool", "PRINTERS"), Path.Combine(Environment.SystemDirectory, "spool", "PRINTERS"), exists, reparse, driveType, available, true, issue);
        var healthy = PrintSpoolDirectoryDiagnostics.Interpret(Snapshot());
        Require(healthy.State == CheckState.Information && healthy.Repair is null && healthy.Evidence is not null &&
            healthy.Detail.Contains("Windows default", StringComparison.Ordinal) && healthy.Detail.Contains("10.0 GiB", StringComparison.Ordinal),
            "Existing fixed-drive spool directory remains informational and non-mutating");
        var repairable = PrintSpoolDirectoryDiagnostics.Interpret(Snapshot(exists: false, reparse: null));
        Require(repairable.State == CheckState.Repairable && repairable.Repair == RepairKind.PrintSpoolDirectory,
            "Only a missing exact Windows default spool directory on a suitable fixed drive is repairable");
        foreach (var attention in new[]
        {
            PrintSpoolDirectoryDiagnostics.Interpret(Snapshot(reparse: true)),
            PrintSpoolDirectoryDiagnostics.Interpret(Snapshot(driveType: DriveType.Network)),
            PrintSpoolDirectoryDiagnostics.Interpret(Snapshot(available: 512L * 1024 * 1024)),
            PrintSpoolDirectoryDiagnostics.Interpret(Snapshot(available: null)),
            PrintSpoolDirectoryDiagnostics.Interpret(Snapshot(exists: false, reparse: null) with { UsedWindowsDefault = false })
        }) Require(attention.State == CheckState.Attention && attention.Repair is null,
            "Redirected, non-fixed, low-capacity and unavailable-capacity spool observations request review without repair");
        foreach (var invalid in new[]
        {
            Snapshot(complete: false), Snapshot(issue: "denied"), Snapshot() with { ConfiguredPath = "" },
            Snapshot() with { ResolvedPath = "relative" }, Snapshot() with { Exists = null },
            Snapshot() with { ReparsePoint = null }, Snapshot(available: -1)
        }) Require(PrintSpoolDirectoryDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Incomplete and malformed spool directory evidence remains unknown");
        var plan = Repairs.Plan(RepairKind.PrintSpoolDirectory);
        Require(plan.Steps.Count == 2 && !plan.Steps[0].Verification && plan.Steps[1].Verification &&
            plan.Steps.All(step => step.Tool == PrintSpoolDirectoryRecovery.Tool),
            "Print spool directory repair is bounded to one managed create operation and exact verification");
        var verifiedSnapshot = Snapshot();
        var verified = new ToolResult(0, System.Text.Json.JsonSerializer.Serialize(verifiedSnapshot, Capture.JsonOptions), "", false);
        Require(PrintSpoolDirectoryRecovery.Verify(verified) &&
            !PrintSpoolDirectoryRecovery.Verify(verified with { Output = "{}" }) &&
            !PrintSpoolDirectoryRecovery.Verify(verified with { Truncated = true }),
            "Print spool directory verification requires complete exact post-change evidence");

        var eligibleHost = new Host(Snapshot(exists: false, reparse: null), Snapshot());
        var created = PrintSpoolDirectoryRecovery.ExecuteAsync(PrintSpoolDirectoryRecovery.Step(false), eligibleHost).GetAwaiter().GetResult();
        Require(eligibleHost.Reads == 2 && eligibleHost.Creates == 1 && PrintSpoolDirectoryRecovery.Verify(created),
            "Fresh exact missing-directory evidence dispatches one creation and retains exact verification");

        var existingHost = new Host(Snapshot(), Snapshot());
        try
        {
            PrintSpoolDirectoryRecovery.ExecuteAsync(PrintSpoolDirectoryRecovery.Step(false), existingHost).GetAwaiter().GetResult();
            throw new Exception("An existing spool directory reached creation");
        }
        catch (RepairStepSkippedException skipped)
        {
            Require(PrintSpoolDirectoryRecovery.Verify(skipped.Evidence) && existingHost.Reads == 1 && existingHost.Creates == 0,
                "A state change to an existing exact spool directory skips creation with retained evidence");
        }

        var changedHost = new Host(Snapshot(exists: false, reparse: null) with { UsedWindowsDefault = false }, Snapshot());
        try
        {
            PrintSpoolDirectoryRecovery.ExecuteAsync(PrintSpoolDirectoryRecovery.Step(false), changedHost).GetAwaiter().GetResult();
            throw new Exception("Changed spool configuration reached directory creation");
        }
        catch (RepairDispatchDeferredException error)
        {
            Require(error.Message.Contains("fresh exact missing-directory evidence") && changedHost.Reads == 1 && changedHost.Creates == 0,
                "Changed or incomplete spool configuration defers creation before dispatch");
        }

        Console.WriteLine("PASS: spool directory creation requires fresh exact missing-default evidence, reports no-dispatch changes and verifies exact state; fixtures only.");
    }

    private sealed class Host(PrintSpoolDirectorySnapshot before, PrintSpoolDirectorySnapshot after) : IPrintSpoolDirectoryRecoveryHost
    {
        internal int Reads { get; private set; }
        internal int Creates { get; private set; }

        public PrintSpoolDirectorySnapshot Read() => ++Reads == 1 ? before : after;
        public void CreateDefault() => Creates++;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
