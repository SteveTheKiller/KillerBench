namespace KillerBench.Engine.Applications;

public static class StartupOperationalCorrelation
{
    private static readonly string[] RequiredNames =
        ["Startup applications", "Startup approval state", "Startup scheduled tasks"];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> evidence, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        token.ThrowIfCancellationRequested();
        const string name = "Startup environment conclusion";
        const string boundary = " This is a bounded inventory of registration names, Windows approval markers and boot or logon task identities. It does not collect commands, arguments, accounts or credentials, measure startup cost, prove an item ran, classify an item as wanted or unwanted, or establish that an item is safe to disable. No registration, approval marker, file, task, application, service or policy changed, and no repair is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        var selected = evidence.Where(item => RequiredNames.Contains(item.Name, StringComparer.Ordinal)).ToArray();
        bool exact = selected.Length == RequiredNames.Length && RequiredNames.All(required =>
            selected.Count(item => string.Equals(item.Name, required, StringComparison.Ordinal)) == 1);
        if (!exact)
            return Unknown("Startup evidence was missing, duplicated or carried an unexpected identity.");

        int incomplete = selected.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = selected.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed startup registrations, approval markers and scheduled boot or logon triggers. Evidence needing review: {attention}; incomplete sources: {incomplete}. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete sources before comparing startup entries or choosing a targeted follow-up.");
        if (attention > 0)
            return new(name, CheckState.Attention, detail + "Review each retained source separately before choosing a targeted startup diagnostic." + boundary);
        return new(name, CheckState.Information, detail + "The inventory completed without establishing a startup fault. Measure an observed delay and verify the owning application before considering a change." + boundary);
    }
}
