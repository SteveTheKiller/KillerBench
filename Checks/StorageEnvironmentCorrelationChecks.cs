using KillerBench.Engine;

internal static class StorageEnvironmentCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("0 need attention and 0 incomplete"),
            "Complete storage environment evidence produces a bounded informational result");
        var attention = Check(pageFile: Fixed("Paging file", CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("1 need attention"),
            "Current storage configuration attention requests review without selecting repair");
        var history = Check(diskHistory: Fixed("Windows disk diagnostic correlation", CheckState.Attention));
        Require(history is { State: CheckState.Information, Repair: null } &&
            history.Detail.Contains("Current environment evidence did not establish", StringComparison.Ordinal),
            "Historical disk attention alone does not establish a current storage problem");
        Require(Check(quotas: Fixed("Disk quotas", CheckState.Unknown)).State == CheckState.Unknown,
            "Incomplete current storage evidence takes precedence");
        Require(Check(spaces: [Fixed("Storage pools")]).State == CheckState.Unknown &&
            Check(delete: [Fixed("Other delete state")]).State == CheckState.Unknown,
            "Missing groups and unexpected identities fail closed");
        try
        {
            Check(token: new CancellationToken(true));
            throw new InvalidOperationException("Canceled storage environment correlation completed unexpectedly");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: storage environment correlation combines configuration, tasks and retained disk history without selecting repair.");
    }

    private static WorkflowCheck Check(IReadOnlyList<WorkflowCheck>? spaces = null, WorkflowCheck? shadow = null,
        WorkflowCheck? selfHealing = null, IReadOnlyList<WorkflowCheck>? delete = null, WorkflowCheck? pageFile = null,
        WorkflowCheck? quotas = null, WorkflowCheck? sensePolicy = null, WorkflowCheck? senseTask = null,
        WorkflowCheck? cleanup = null, WorkflowCheck? diskHistory = null, CancellationToken token = default) =>
        StorageEnvironmentCorrelation.Check(spaces ?? [Fixed("Storage pools"), Fixed("Storage virtual disks")],
            shadow ?? Fixed("VSS shadow storage"), selfHealing ?? Fixed("NTFS self-healing: C:"),
            delete ?? [Fixed("NTFS delete notifications"), Fixed("ReFS delete notifications")],
            pageFile ?? Fixed("Paging file"), quotas ?? Fixed("Disk quotas"),
            sensePolicy ?? Fixed("Storage Sense machine policy"), senseTask ?? Fixed("Storage Sense scheduled task"),
            cleanup ?? Fixed("Disk Cleanup scheduled task"), diskHistory ?? Fixed("Windows disk diagnostic correlation"), token);

    private static WorkflowCheck Fixed(string name, CheckState state = CheckState.Information) => new(name, state, "fixture");
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
