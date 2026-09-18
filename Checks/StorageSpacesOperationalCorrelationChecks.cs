using KillerBench.Engine;

internal static class StorageSpacesOperationalCorrelationChecks
{
    internal static void Run()
    {
        Require(Invoke() is { State: CheckState.Clear, Repair: null }, "Complete healthy Storage Spaces evidence produces a bounded clear conclusion");
        Require(Invoke(pools: Check("Storage pools", CheckState.Attention)).State == CheckState.Attention,
            "Pool concerns request review");
        Require(Invoke(disks: Check("Storage virtual disks", CheckState.Attention)).State == CheckState.Attention,
            "Virtual-disk concerns request review");
        Require(Invoke(pools: Check("Storage pools", CheckState.Unknown)).State == CheckState.Unknown,
            "Incomplete pool evidence takes precedence");
        Require(Invoke(pools: Check("Storage pools", CheckState.Information),
            disks: Check("Storage virtual disks", CheckState.Information)).State == CheckState.Information,
            "A complete empty Storage Spaces inventory remains informational");
        Require(Invoke(disks: Check("Unexpected", CheckState.Clear)).State == CheckState.Unknown,
            "Unexpected source identity fails closed");
        try { Invoke(token: new CancellationToken(true)); throw new Exception("Storage Spaces correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Storage Spaces correlation combines pool and virtual-disk state without selecting storage changes.");
    }

    private static WorkflowCheck Invoke(WorkflowCheck? pools = null, WorkflowCheck? disks = null,
        CancellationToken token = default) => StorageSpacesOperationalCorrelation.Check(
            pools ?? Check("Storage pools", CheckState.Clear),
            disks ?? Check("Storage virtual disks", CheckState.Clear), token);

    private static WorkflowCheck Check(string name, CheckState state) => new(name, state, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
