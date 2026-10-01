using KillerBench.Engine;

internal static class SystemStabilityOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("0 need attention and 0 incomplete") &&
            clear.Detail.Contains("No repair is selected"), "Complete stability evidence produces a bounded informational result");
        var attention = Check(device: CheckState.Attention, hardware: CheckState.Attention);
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("2 need attention"),
            "Stability attention remains review-only");
        var incomplete = Check(vital: CheckState.Unknown, device: CheckState.Attention);
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("1 incomplete"),
            "Incomplete stability evidence takes precedence over attention");
        Require(Invoke(vitals: [.. Vitals().Skip(1)]).State == CheckState.Unknown &&
            Invoke(crashes: [Info("Crash dump configuration"), Info("Crash dump files")]).State == CheckState.Unknown &&
            Invoke(devices: []).State == CheckState.Unknown, "Missing and empty stability groups fail closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Stability cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: system stability operational correlation combines current snapshots and retained history without selecting repair.");
    }

    private static WorkflowCheck Check(CheckState vital = CheckState.Information, CheckState device = CheckState.Information,
        CheckState hardware = CheckState.Information) => Invoke(vitals: Vitals(vital),
            devices: [new("Device problems", device, "fixture")],
            hardwareErrors: [new("Recent Windows hardware error records", hardware, "fixture")]);

    private static WorkflowCheck Invoke(IReadOnlyList<WorkflowCheck>? vitals = null, IReadOnlyList<WorkflowCheck>? devices = null,
        IReadOnlyList<WorkflowCheck>? hardwareErrors = null, IReadOnlyList<WorkflowCheck>? crashes = null,
        CancellationToken token = default) => SystemStabilityOperationalCorrelation.Check(vitals ?? Vitals(),
            devices ?? [Info("Device problems")], [Info("Reliability history")],
            hardwareErrors ?? [Info("Recent Windows hardware error records")], crashes ??
            [Info("Crash dump configuration"), Info("Crash dump files"), Info("Recent system crash reports")],
            [Info("Recent unexpected shutdowns")], token);

    private static WorkflowCheck[] Vitals(CheckState memory = CheckState.Information) =>
    [
        new("Memory snapshot", memory, "fixture"), Info("Physical disk health"), Info("System uptime"),
        Info("Windows Memory Diagnostic results")
    ];
    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
