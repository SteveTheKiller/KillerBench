using KillerBench.Engine;

internal static class DriverHistoryOperationalCorrelationChecks
{
    internal static void Run()
    {
        var quiet = DriverHistoryOperationalCorrelation.Check([Info("Driver installation history")]);
        Require(quiet is { State: CheckState.Information, Repair: null } && quiet.Detail.Contains("does not establish driver or device health"),
            "Complete quiet driver history remains informational");

        var historical = DriverHistoryOperationalCorrelation.Check([
            Attention("Driver installation failure: PCI\\FIXTURE"),
            Info("Historical driver target not currently reported: PCI\\FIXTURE")]);
        Require(historical is { State: CheckState.Information, Repair: null } && historical.Detail.Contains("remain historical context"),
            "Unmatched retained failures do not become current device faults");

        var current = DriverHistoryOperationalCorrelation.Check([
            Attention("Driver installation failure: PCI\\FIXTURE"),
            Attention("Current state after driver failure: Fixture device")]);
        Require(current is { State: CheckState.Attention, Repair: null } &&
            current.Detail.Contains("exact current device identity"),
            "Exact current nonzero device codes request review without selecting repair");

        Require(DriverHistoryOperationalCorrelation.Check([
            Attention("Driver installation failure: PCI\\FIXTURE"),
            new("Current device correlation", CheckState.Unknown, "fixture")]).State == CheckState.Unknown,
            "Incomplete current inventory takes precedence over historical and current concerns");
        Require(DriverHistoryOperationalCorrelation.Check([]).State == CheckState.Unknown &&
            DriverHistoryOperationalCorrelation.Check([Info("Wrong result")]).State == CheckState.Unknown &&
            DriverHistoryOperationalCorrelation.Check([.. Enumerable.Range(0, 67).Select(index => Info(index == 0 ? "Driver installation history" : "Result " + index))]).State == CheckState.Unknown,
            "Missing, unexpected and oversized driver history inputs fail closed");

        try { DriverHistoryOperationalCorrelation.Check([Info("Driver installation history")], new CancellationToken(true)); throw new Exception("Driver history correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: driver history conclusion separates exact current device problems from historical SetupAPI failures without selecting repairs.");
    }

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");
    private static WorkflowCheck Attention(string name) => new(name, CheckState.Attention, "fixture");
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
