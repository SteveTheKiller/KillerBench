using KillerBench.Engine;

internal static class HardwareOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Invoke() is { State: CheckState.Information, Repair: null },
            "Complete quiet hardware evidence remains informational");
        Require(Invoke(history: Check("Recent Windows hardware error records", CheckState.Attention)).State == CheckState.Attention,
            "WHEA concerns request review");
        Require(Invoke(memory: Check("Windows Memory Diagnostic results", CheckState.Attention)).State == CheckState.Attention,
            "Memory diagnostic concerns request review");
        Require(Invoke(devices: [Check("Problems device: fixture", CheckState.Attention)]).State == CheckState.Attention,
            "Current device concerns request review");
        Require(Invoke(disks: Check("Physical disk evidence correlation", CheckState.Unknown)).State == CheckState.Unknown,
            "Incomplete disk evidence takes precedence");
        Require(Invoke(devices: []).State == CheckState.Unknown &&
            Invoke(devices: [Check("Unexpected", CheckState.Information)]).State == CheckState.Unknown,
            "Missing or unexpected device evidence fails closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Hardware correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: hardware correlation combines WHEA, memory, device and physical-disk evidence without selecting repairs.");
    }

    private static WorkflowCheck Invoke(WorkflowCheck? history = null, WorkflowCheck? memory = null,
        IReadOnlyList<WorkflowCheck>? devices = null, WorkflowCheck? disks = null,
        CancellationToken token = default) => HardwareOperationalCorrelation.Check(
            history ?? Check("Recent Windows hardware error records", CheckState.Information),
            memory ?? Check("Windows Memory Diagnostic results", CheckState.Information),
            devices ?? [Check("Device problems", CheckState.Information)],
            disks ?? Check("Physical disk evidence correlation", CheckState.Information), token);

    private static WorkflowCheck Check(string name, CheckState state) => new(name, state, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
