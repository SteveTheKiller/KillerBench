using KillerBench.Engine;

internal static class DeliveryOptimizationOperationalCorrelationChecks
{
    internal static void Run()
    {
        var complete = DeliveryOptimizationOperationalCorrelation.Check(Service(), Activity(), Policy(), History());
        Require(complete is { State: CheckState.Information, Repair: null } &&
            complete.Detail.Contains("0 need attention and 0 incomplete", StringComparison.Ordinal) &&
            complete.Detail.Contains("does not prove transfer progress", StringComparison.Ordinal),
            "Complete Delivery Optimization evidence produces one bounded informational conclusion");

        var paused = DeliveryOptimizationOperationalCorrelation.Check(Service(CheckState.Attention), Activity(), Policy(), History());
        Require(paused is { State: CheckState.Attention, Repair: null } &&
            paused.Detail.Contains("Only exact Paused current service evidence", StringComparison.Ordinal),
            "Paused read-only service evidence requests review without selecting continuation");

        var managed = DeliveryOptimizationOperationalCorrelation.Check(Service(), Activity(), Policy(CheckState.Attention), History());
        Require(managed is { State: CheckState.Attention, Repair: null } &&
            managed.Detail.Contains("current, policy and historical results", StringComparison.Ordinal),
            "Policy attention remains review-only and does not select a service action");

        var historical = DeliveryOptimizationOperationalCorrelation.Check(Service(), Activity(), Policy(), History(CheckState.Attention));
        Require(historical is { State: CheckState.Attention, Repair: null } &&
            historical.Detail.Contains("Historical failures cannot authorize continuation", StringComparison.Ordinal),
            "Historical Delivery Optimization failures remain review-only");

        var incomplete = DeliveryOptimizationOperationalCorrelation.Check(
            Service(CheckState.Attention), Activity(CheckState.Unknown), Policy(CheckState.Attention), History(CheckState.Attention));
        Require(incomplete is { State: CheckState.Unknown, Repair: null } &&
            incomplete.Detail.Contains("1 incomplete", StringComparison.Ordinal),
            "Incomplete Delivery Optimization evidence takes precedence over attention");

        Require(DeliveryOptimizationOperationalCorrelation.Check(
            Service() with { Name = "Other service" }, Activity(), Policy(), History()).State == CheckState.Unknown,
            "Mismatched Delivery Optimization evidence fails closed");

        try
        {
            DeliveryOptimizationOperationalCorrelation.Check(Service(), Activity(), Policy(), History(), new CancellationToken(true));
            throw new Exception("Delivery Optimization correlation cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: Delivery Optimization service, activity and policy evidence produce one conservative read-only conclusion.");
    }

    private static WorkflowCheck Service(CheckState state = CheckState.Information) =>
        new("Delivery Optimization", state, "fixture");
    private static WorkflowCheck Activity(CheckState state = CheckState.Information) =>
        new("Delivery Optimization activity", state, "fixture");
    private static WorkflowCheck Policy(CheckState state = CheckState.Information) =>
        new("Delivery Optimization policy", state, "fixture");
    private static WorkflowCheck History(CheckState state = CheckState.Information) =>
        new("Delivery Optimization failure history", state, "fixture");
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
