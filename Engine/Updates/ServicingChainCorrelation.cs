namespace KillerBench.Engine;

public static class ServicingChainCorrelation
{
    private static readonly IReadOnlyList<string> StateNames = Array.AsReadOnly(
        ServicingServiceStateDiagnostics.Services.Select(service => service.DisplayName + " state").ToArray());

    private static readonly IReadOnlyList<string> HistoryNames = Array.AsReadOnly(new[]
    {
        "Windows Update service failure history",
        "BITS service failure history",
        "Cryptographic Services failure history",
        "Windows Modules Installer failure history",
        "Windows Update Medic failure history",
        "Update Orchestrator failure history"
    });

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> states, WorkflowCheck serviceDlls,
        IReadOnlyList<WorkflowCheck> context, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(states);
        ArgumentNullException.ThrowIfNull(serviceDlls);
        ArgumentNullException.ThrowIfNull(context);
        token.ThrowIfCancellationRequested();
        if (!ExactSet(states, StateNames) || serviceDlls.Name != "Windows Update service DLLs" ||
            context.Count is 0 or > 512)
            return Unknown("The expected six-service state, DLL or history evidence set was incomplete or inconsistent.");
        var histories = context.Where(check => HistoryNames.Contains(check.Name, StringComparer.Ordinal)).ToArray();
        if (!ExactSet(histories, HistoryNames))
            return Unknown("The expected six exact servicing service histories were incomplete or inconsistent.");

        var evidence = states.Concat([serviceDlls]).Concat(histories).ToArray();
        int currentAttention = states.Count(check => NeedsAttention(check.EffectiveState));
        int currentUnknown = states.Count(check => check.EffectiveState == CheckState.Unknown);
        int historyAttention = histories.Count(check => NeedsAttention(check.EffectiveState));
        int historyUnknown = histories.Count(check => check.EffectiveState == CheckState.Unknown);
        bool dllAttention = NeedsAttention(serviceDlls.EffectiveState);
        bool dllUnknown = serviceDlls.EffectiveState == CheckState.Unknown;
        string counts = $"Reviewed six current service states, one registered-DLL result and six exact service histories. Current state: {currentAttention} need attention and {currentUnknown} incomplete. Registered DLLs: {(dllAttention ? "review" : dllUnknown ? "incomplete" : "clear or informational")}. History: {historyAttention} need attention and {historyUnknown} incomplete.";
        const string boundary = " Running or registered state does not prove update, transfer, catalog, certificate or package success. Historical failures do not establish a current outage. No service, DLL, update, transfer, cache, source, policy, package, certificate, catalog or startup setting changed, and no repair is selected.";
        if (currentUnknown > 0 || dllUnknown || historyUnknown > 0)
            return new("Windows Update servicing-chain correlation", CheckState.Unknown,
                counts + " The chain cannot be fully correlated until every required observation is complete." + boundary);
        if (currentAttention > 0 || dllAttention || historyAttention > 0)
            return new("Windows Update servicing-chain correlation", CheckState.Attention,
                counts + " Review the exact underlying results together before selecting a focused action." + boundary);
        return new("Windows Update servicing-chain correlation", CheckState.Clear,
            counts + " No issue was established within this bounded servicing-chain evidence." + boundary);
    }

    private static bool ExactSet(IReadOnlyList<WorkflowCheck> checks, IReadOnlyList<string> expected) =>
        checks.Count == expected.Count && checks.All(check => check is not null) &&
        checks.Select(check => check.Name).Distinct(StringComparer.Ordinal).Count() == expected.Count &&
        checks.Select(check => check.Name).ToHashSet(StringComparer.Ordinal).SetEquals(expected);

    private static bool NeedsAttention(CheckState state) => state is CheckState.Attention or CheckState.Repairable;

    private static WorkflowCheck Unknown(string detail) => new("Windows Update servicing-chain correlation",
        CheckState.Unknown, detail + " No repair is selected and no service, file, update, transfer, cache, source, policy, package, certificate, catalog or startup setting changed.");
}
