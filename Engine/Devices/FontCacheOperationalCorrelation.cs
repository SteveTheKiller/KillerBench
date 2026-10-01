namespace KillerBench.Engine.Devices;

public static class FontCacheOperationalCorrelation
{
    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string boundary = " This conclusion combines bounded machine and current-user font registration evidence with the exact Windows Font Cache Service conclusion and its retained Service Control Manager history. It does not parse font files, inspect cache contents, render text or prove application behavior. It identifies no root cause, selects no repair and changes no font, registration, cache file, application data, service configuration, permission, policy or user data.";
        static WorkflowCheck Unknown(string detail) => new("Font cache readiness", CheckState.Unknown, detail + boundary);

        if (checks.Count == 0 || checks.Any(check => check is null))
            return Unknown("Font cache correlation evidence is missing or invalid.");

        var registrations = checks.Where(check => check.Name == "Font registrations").ToArray();
        var service = checks.Where(check => check.Name == "Focused service readiness").ToArray();
        if (registrations.Length != 1 || service.Length != 1)
            return Unknown("The expected font registration or focused-service conclusion was missing or duplicated.");

        int incomplete = registrations.Concat(service).Count(check => check.EffectiveState == CheckState.Unknown);
        int attention = registrations.Concat(service).Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string counts = $"Reviewed the bounded registration inventory and exact service conclusion. Results needing attention: {attention}; incomplete results: {incomplete}. ";
        if (incomplete > 0)
            return Unknown(counts + "Recollect the incomplete registration or service evidence before drawing a font-cache conclusion.");
        if (attention > 0)
            return new("Font cache readiness", CheckState.Attention,
                counts + "Review missing registered files and the current service finding by exact identity. Neither finding authorizes a cache rebuild or font change." + boundary);
        return new("Font cache readiness", CheckState.Information,
            counts + "No missing registered file or current service-availability concern was established within the checked scope." + boundary);
    }
}
