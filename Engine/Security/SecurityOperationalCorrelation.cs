namespace KillerBench.Engine;

public static class SecurityOperationalCorrelation
{
    private static readonly string[] CertificateNames =
        ["Personal certificate expiration", "Personal certificate chain trust"];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> security, IReadOnlyList<WorkflowCheck> certificates,
        IReadOnlyList<WorkflowCheck> licensing, IReadOnlyList<WorkflowCheck> products, WorkflowCheck threats,
        WorkflowCheck defenderEvents, IReadOnlyList<WorkflowCheck> restartState, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(security); ArgumentNullException.ThrowIfNull(certificates);
        ArgumentNullException.ThrowIfNull(licensing); ArgumentNullException.ThrowIfNull(products);
        ArgumentNullException.ThrowIfNull(threats); ArgumentNullException.ThrowIfNull(defenderEvents);
        ArgumentNullException.ThrowIfNull(restartState); token.ThrowIfCancellationRequested();
        const string name = "Security operational correlation";
        const string boundary = " This is a bounded configuration and history review, not proof of protection, compliance, threat removal, trustworthiness or future security. No scan, quarantine, exclusion, signature, certificate, license, policy, service, hardware or restart state changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);
        var license = licensing.Where(item => item.Name == "Windows activation").ToArray();
        var productSummary = products.Where(item => item.Name == "Registered antivirus products").ToArray();
        var restart = restartState.Where(item => item.Name == "Restart").ToArray();
        if (security.Count is 0 or > 128 || !ExactNames(certificates, CertificateNames) || license.Length != 1 ||
            productSummary.Length != 1 || threats.Name != "Microsoft Defender threat history" ||
            defenderEvents.Name != "Recent Microsoft Defender events" || restart.Length != 1)
            return Unknown("Security correlation inputs were missing, duplicated, oversized or carried an unexpected identity.");
        var all = security.Concat(certificates).Concat(license).Concat(products).Append(threats).Append(defenderEvents).Concat(restart).ToArray();
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed {all.Length} security hardware, policy, certificate, licensing, product, Defender and restart results. Correlated evidence: {attention} need attention and {incomplete} incomplete. ";
        if (incomplete > 0) return Unknown(detail + "Recollect incomplete evidence before drawing a combined security conclusion.");
        if (attention > 0) return new(name, CheckState.Attention, detail + "Review the detailed current state, policy ownership and historical evidence before choosing a targeted security action." + boundary);
        return new(name, CheckState.Information, detail + "No collected source requested review. Effective protection and compliance remain unverified." + boundary);
    }

    private static bool ExactNames(IReadOnlyList<WorkflowCheck> checks, IReadOnlyList<string> names) =>
        checks.Count == names.Count && checks.Select(item => item.Name).OrderBy(value => value, StringComparer.Ordinal)
            .SequenceEqual(names.OrderBy(value => value, StringComparer.Ordinal), StringComparer.Ordinal);
}
