using KillerBench.Engine;

internal static class SearchOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } &&
            clear.Detail.Contains("0 need attention and 0 incomplete", StringComparison.Ordinal) &&
            clear.Detail.Contains("result completeness and freshness remain unverified", StringComparison.Ordinal),
            "Complete Search evidence produces a bounded informational result without claiming query behavior");

        var attention = Check(catalog: CheckState.Attention, history: CheckState.Attention);
        Require(attention is { State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("2 need attention", StringComparison.Ordinal) &&
            attention.Detail.Contains("before selecting a focused service or index action", StringComparison.Ordinal),
            "Current Search and historical attention produces review without selecting repair");

        var incomplete = Check(events: CheckState.Unknown, history: CheckState.Attention);
        Require(incomplete is { State: CheckState.Unknown, Repair: null } &&
            incomplete.Detail.Contains("1 incomplete", StringComparison.Ordinal),
            "Incomplete Search event evidence takes precedence over historical attention");

        Require(SearchOperationalCorrelation.Check(Info("Wrong catalog"), Info("Recent Windows Search events"),
            Info("Windows Search service failure history")).State == CheckState.Unknown,
            "Unexpected Search evidence identities fail closed");

        try
        {
            SearchOperationalCorrelation.Check(Info("Windows Search catalog"), Info("Recent Windows Search events"),
                Info("Windows Search service failure history"), new CancellationToken(true));
            throw new Exception("Search operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: Search catalog, application-event and exact service-history correlation is deterministic and read-only.");
    }

    private static WorkflowCheck Check(CheckState catalog = CheckState.Information,
        CheckState events = CheckState.Information, CheckState history = CheckState.Information) =>
        SearchOperationalCorrelation.Check(new("Windows Search catalog", catalog, "fixture"),
            new("Recent Windows Search events", events, "fixture"),
            new("Windows Search service failure history", history, "fixture"));

    private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "fixture");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
