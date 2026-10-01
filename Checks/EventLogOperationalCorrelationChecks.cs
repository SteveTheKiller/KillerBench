using KillerBench.Engine;

internal static class EventLogOperationalCorrelationChecks
{
    internal static void Run()
    {
        var clear = Check();
        Require(clear is { State: CheckState.Information, Repair: null } &&
            clear.Detail.Contains("4 exact diagnostic channel", StringComparison.Ordinal) &&
            clear.Detail.Contains("0 need attention and 0 incomplete", StringComparison.Ordinal) &&
            clear.Detail.Contains("retained-record completeness remain unverified", StringComparison.Ordinal),
            "Complete Event Log evidence remains informational without claiming delivery or completeness");

        var attention = Check(channelState: CheckState.Attention, historyState: CheckState.Attention);
        Require(attention is { State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("2 need attention", StringComparison.Ordinal) &&
            attention.Detail.Contains("before selecting a focused channel or service action", StringComparison.Ordinal),
            "Channel and service-history attention requests exact review without selecting repair");

        var incomplete = Check(channelState: CheckState.Unknown, historyState: CheckState.Attention);
        Require(incomplete is { State: CheckState.Unknown, Repair: null } &&
            incomplete.Detail.Contains("1 incomplete", StringComparison.Ordinal),
            "Incomplete channel evidence takes precedence over service-history attention");

        var duplicate = Channels();
        duplicate[1] = duplicate[0];
        Require(EventLogOperationalCorrelation.Check(duplicate, History()).State == CheckState.Unknown,
            "Duplicate Event Log channel identities fail closed");
        Require(EventLogOperationalCorrelation.Check(Channels()[..3], History()).State == CheckState.Unknown,
            "Missing Event Log channel evidence fails closed");
        Require(EventLogOperationalCorrelation.Check(Channels(), History("Wrong history")).State == CheckState.Unknown,
            "Unexpected Event Log service-history identity fails closed");

        try
        {
            EventLogOperationalCorrelation.Check(Channels(), History(), new CancellationToken(true));
            throw new Exception("Event Log operational cancellation ignored");
        }
        catch (OperationCanceledException) { }

        Console.WriteLine("PASS: Event Log channel-capacity and exact service-history correlation is deterministic and read-only.");
    }

    private static WorkflowCheck Check(CheckState channelState = CheckState.Information,
        CheckState historyState = CheckState.Information)
    {
        var channels = Channels();
        channels[0] = channels[0] with { State = channelState };
        return EventLogOperationalCorrelation.Check(channels, History(state: historyState));
    }

    private static WorkflowCheck[] Channels() => [.. EventLogCapacityDiagnostics.Channels
        .Select(channel => new WorkflowCheck("Event log capacity: " + channel, CheckState.Information, "fixture"))];

    private static WorkflowCheck History(string name = "Windows Event Log service failure history",
        CheckState state = CheckState.Information) => new(name, state, "fixture");

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
