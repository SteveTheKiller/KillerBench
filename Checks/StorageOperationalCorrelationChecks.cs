using KillerBench.Engine;

internal static class StorageOperationalCorrelationChecks
{
    public static void Run()
    {
        var clear = StorageOperationalCorrelation.Check(Capacity(), Dirty(), Jobs(), Physical(), Events(), History());
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("0 need attention and 0 incomplete") &&
            clear.Detail.Contains("no repair is selected"), "Complete clear storage evidence produces a bounded informational result");

        var attention = StorageOperationalCorrelation.Check(Capacity(CheckState.Attention), Dirty(), Jobs(), Physical(), Events(),
            History(CheckState.Information, CheckState.Attention));
        Require(attention is { State: CheckState.Attention, Repair: null } && attention.Detail.Contains("2 need attention") &&
            attention.Detail.Contains("Historical service failures do not establish"), "Current and historical storage attention remains review-only");

        var incomplete = StorageOperationalCorrelation.Check(Capacity(CheckState.Attention), Dirty(CheckState.Unknown), Jobs(), Physical(), Events(), History());
        Require(incomplete is { State: CheckState.Unknown, Repair: null } && incomplete.Detail.Contains("1 incomplete"),
            "Incomplete storage evidence takes precedence over attention");

        var wrongIdentity = StorageOperationalCorrelation.Check(Capacity() with { Name = "Other capacity" }, Dirty(), Jobs(), Physical(), Events(), History());
        var duplicateHistory = StorageOperationalCorrelation.Check(Capacity(), Dirty(), Jobs(), Physical(), Events(),
            [History()[0], History()[0]]);
        Require(wrongIdentity.State == CheckState.Unknown && duplicateHistory.State == CheckState.Unknown,
            "Missing, duplicate and unexpected storage identities fail closed");

        try
        {
            StorageOperationalCorrelation.Check(Capacity(), Dirty(), Jobs(), Physical(), Events(), History(), new CancellationToken(true));
            throw new Exception("Storage operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: storage operational correlation combines capacity, dirty state, jobs, disks, events and exact service history without selecting repair.");
    }

    private static WorkflowCheck Capacity(CheckState state = CheckState.Information) => new("Drive-letter volume capacity", state, "fixture");
    private static WorkflowCheck Dirty(CheckState state = CheckState.Information) => new("Volume dirty bit", state, "fixture");
    private static WorkflowCheck Jobs(CheckState state = CheckState.Information) => new("Storage jobs", state, "fixture");
    private static WorkflowCheck Physical(CheckState state = CheckState.Information) => new("Physical disk evidence correlation", state, "fixture");
    private static WorkflowCheck Events(CheckState state = CheckState.Information) => new("Storage event correlation", state, "fixture");
    private static WorkflowCheck[] History(CheckState storage = CheckState.Information, CheckState optimize = CheckState.Information) =>
    [
        new("Storage Service failure history", storage, "fixture"),
        new("Optimize Drives failure history", optimize, "fixture"),
        new("Volume Shadow Copy failure history", CheckState.Information, "fixture"),
        new("Software Shadow Copy Provider failure history", CheckState.Information, "fixture")
    ];

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
