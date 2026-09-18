using KillerBench.Engine;

internal static class InstallerPendingOperationCorrelationChecks
{
    internal static void Run()
    {
        var pending = Check("Pending file operations", CheckState.Attention);
        var clear = Check("Pending file operations", CheckState.Information);
        var active = Check("Installer activity", CheckState.Attention);
        var idle = Check("Installer activity", CheckState.Information);

        Require(InstallerPendingOperationCorrelation.Check(pending, active) is { State: CheckState.Attention, Repair: null } busy &&
            busy.Detail.Contains("Allow known deployment work to finish") && !busy.Detail.Contains("restart or retry is safe"),
            "Active Installer evidence prioritizes waiting before restart guidance");
        Require(InstallerPendingOperationCorrelation.Check(pending, idle) is { State: CheckState.Attention, Repair: null } restart &&
            restart.Detail.Contains("user-controlled restart") && restart.Detail.Contains("fresh collection"),
            "Idle Installer snapshot with pending operations recommends restart and recollection");
        Require(InstallerPendingOperationCorrelation.Check(clear, active) is { State: CheckState.Information, Repair: null } absent &&
            absent.Detail.Contains("no pending-operation action"),
            "No pending operations do not inherit an Installer activity warning");

        foreach (var pair in new[]
        {
            (pending with { Name = "Foreign" }, idle),
            (pending with { Evidence = null }, idle),
            (pending with { Evidence = pending.Evidence! with { Truncated = true } }, idle),
            (pending with { State = CheckState.Unknown }, idle),
            (pending, active with { Evidence = active.Evidence! with { TimedOut = true } }),
            (pending, active with { State = CheckState.Clear })
        })
            Require(InstallerPendingOperationCorrelation.Check(pair.Item1, pair.Item2) is { State: CheckState.Unknown, Repair: null },
                "Mismatched, incomplete and unsupported correlation evidence remains unknown");

        using var stop = new CancellationTokenSource();
        stop.Cancel();
        try
        {
            InstallerPendingOperationCorrelation.Check(pending, idle, stop.Token);
            throw new InvalidOperationException("Canceled correlation continued.");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: pending file operations and Installer activity produce bounded wait or restart guidance without selecting repair.");
    }

    private static WorkflowCheck Check(string name, CheckState state) =>
        new(name, state, "fixture", Evidence: new(0, "{}", "", false));

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
