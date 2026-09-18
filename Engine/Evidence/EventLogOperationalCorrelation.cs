namespace KillerBench.Engine;

public static class EventLogOperationalCorrelation
{
    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> channels, WorkflowCheck serviceHistory,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(channels);
        ArgumentNullException.ThrowIfNull(serviceHistory);
        token.ThrowIfCancellationRequested();

        const string name = "Event Log operational correlation";
        const string boundary = " This bounded correlation does not read event content or prove retained-record completeness, provider delivery, subscription operation, forwarding, archive creation, available archive storage or consumer behavior. Historical service failures do not establish a current outage. No channel, retention, size, archive, service, subscription, forwarding, log, policy or repair setting changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        string[] expected = EventLogCapacityDiagnostics.Channels.Select(channel => "Event log capacity: " + channel).ToArray();
        if (channels.Count != expected.Length || channels.Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() != expected.Length ||
            expected.Any(identity => channels.Count(item => item.Name == identity) != 1) ||
            serviceHistory.Name != "Windows Event Log service failure history")
            return Unknown("Event Log correlation inputs were missing, duplicated or carried an unexpected identity.");

        WorkflowCheck[] all = channels.Append(serviceHistory).ToArray();
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed {channels.Count} exact diagnostic channel capacity results and exact Windows Event Log service history. Correlated evidence: {attention} need attention and {incomplete} incomplete.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete channel or service-history evidence before interpreting Event Log readiness.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + " Review the exact channel and historical results before selecting a focused channel or service action." + boundary);
        return new(name, CheckState.Information,
            detail + " No correlated observation requested review. Event delivery and retained-record completeness remain unverified." + boundary);
    }
}
