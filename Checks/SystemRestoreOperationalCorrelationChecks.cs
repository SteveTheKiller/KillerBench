using KillerBench.Engine;

internal static class SystemRestoreOperationalCorrelationChecks
{
    internal static void Run()
    {
        var eligible = SystemRestoreOperationalCorrelation.Check(Inventory(), Creation(CheckState.Repairable, RepairKind.SystemRestorePoint));
        Require(eligible is { State: CheckState.Information, Repair: null } && eligible.Detail.Contains("separate confirmation"),
            "Eligible explicit creation remains informational in the combined conclusion");

        var blocked = SystemRestoreOperationalCorrelation.Check(Inventory(), Creation(CheckState.Attention));
        Require(blocked is { State: CheckState.Attention, Repair: null } && blocked.Detail.Contains("currently unavailable or blocked"),
            "Blocked creation eligibility requests review without selecting a repair in the conclusion");

        Require(SystemRestoreOperationalCorrelation.Check(Inventory(CheckState.Unknown), Creation(CheckState.Repairable, RepairKind.SystemRestorePoint)).State == CheckState.Unknown &&
            SystemRestoreOperationalCorrelation.Check(Inventory(), Creation(CheckState.Unknown)).State == CheckState.Unknown,
            "Incomplete System Restore evidence takes precedence");
        Require(SystemRestoreOperationalCorrelation.Check(new("Wrong inventory", CheckState.Information, "fixture"), Creation(CheckState.Attention)).State == CheckState.Unknown &&
            SystemRestoreOperationalCorrelation.Check(Inventory(), new("Wrong creation", CheckState.Attention, "fixture")).State == CheckState.Unknown,
            "Unexpected System Restore identities fail closed");

        try { SystemRestoreOperationalCorrelation.Check(Inventory(), Creation(CheckState.Attention), new CancellationToken(true)); throw new Exception("System Restore correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: System Restore conclusion combines inventory and explicit creation eligibility without claiming recoverability or selecting repair.");
    }

    private static WorkflowCheck Inventory(CheckState state = CheckState.Information) => new("System Restore points", state, "fixture");
    private static WorkflowCheck Creation(CheckState state, RepairKind? repair = null) => new("System Restore point creation", state, "fixture", repair);
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
