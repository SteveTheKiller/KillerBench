namespace KillerBench.Engine.Updates;

public static class CryptographicServiceOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck service, WorkflowCheck registration,
        WorkflowCheck history, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(history);
        token.ThrowIfCancellationRequested();

        const string name = "Cryptographic Services conclusion";
        const string boundary = " This bounded conclusion does not validate certificates, signatures, catalogs, trust chains, update payloads or application behavior. It does not replace a DLL, rebuild a catalog, clear a cache, change trust, policy or startup settings, or select a repair.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (service.Name != "Cryptographic Services state" ||
            registration.Name != "Cryptographic Services registration" ||
            history.Name != "Cryptographic Services failure history")
            return Unknown("Cryptographic Services inputs were missing or carried an unexpected identity.");

        WorkflowCheck[] checks = [service, registration, history];
        int incomplete = checks.Count(check => check.EffectiveState == CheckState.Unknown);
        int attention = checks.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed exact current service state, the registered service DLL and bounded exact service-failure history. Correlated evidence: {attention} need attention and {incomplete} incomplete.";
        if (incomplete > 0)
            return Unknown(detail + " Recollect incomplete service, registration or history evidence before interpreting Cryptographic Services readiness.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + " Review the exact current, registration and historical results. Historical failures and DLL observations cannot authorize a service start or component replacement." + boundary);
        return new(name, CheckState.Information,
            detail + " No current concern was established within the collected evidence." + boundary);
    }
}
