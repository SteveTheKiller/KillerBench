using KillerBench.Engine;

internal static class DeviceOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Invoke() is { State: CheckState.Information, Repair: null }, "Complete device evidence remains informational");
        Require(Invoke(devices: [Attention("Problems device: fixture")]).State == CheckState.Attention,
            "Current device problems produce attention");
        Require(Invoke(drivers: [Attention("Current state after driver failure: fixture")]).State == CheckState.Attention,
            "Exact current driver correlation produces attention");
        var historical = Invoke(drivers: [Attention("Driver installation failure: fixture")]);
        Require(historical.State == CheckState.Information && historical.Detail.Contains("Historical failures direct narrower"),
            "Historical driver failures do not become a current fault");
        Require(Invoke(setup: Unknown("Recent device setup events")).State == CheckState.Unknown,
            "Incomplete evidence takes precedence");
        Require(Invoke(pnp: Info("Unexpected")).State == CheckState.Unknown, "Unexpected history identity fails closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Device correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: device operational correlation combines current problems and bounded setup, Plug and Play and driver history without selecting repair.");
    }

    private static WorkflowCheck Invoke(IReadOnlyList<WorkflowCheck>? devices = null,
        IReadOnlyList<WorkflowCheck>? drivers = null, WorkflowCheck? setup = null,
        WorkflowCheck? pnp = null, CancellationToken token = default) => DeviceOperationalCorrelation.Check(
            devices ?? [Info("Device problems")], drivers ?? [Info("Driver installation history")],
            setup ?? Info("Recent device setup events"), pnp ?? Info("Recent Plug and Play configuration events"), token);

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static WorkflowCheck Attention(string name) => new(name, CheckState.Attention, "fixture");
    private static WorkflowCheck Unknown(string name) => new(name, CheckState.Unknown, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
