namespace KillerBench.Engine;

public static class CertificateOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck expiration, WorkflowCheck chainTrust,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(expiration);
        ArgumentNullException.ThrowIfNull(chainTrust);
        token.ThrowIfCancellationRequested();
        const string name = "Certificate operational conclusion";
        const string boundary = " This bounded local review does not verify revocation, server presentation, bindings, enrollment, private-key access, policy ownership, remote trust or application authentication. No certificate, trust setting, enrollment, binding, private key, application, service or policy is changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);
        if (expiration.Name != "Personal certificate expiration" || chainTrust.Name != "Personal certificate chain trust")
            return Unknown("Certificate evidence carried an unexpected identity.");
        var evidence = new[] { expiration, chainTrust };
        int incomplete = evidence.Count(check => check.EffectiveState == CheckState.Unknown);
        int attention = evidence.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed bounded LocalMachine and CurrentUser personal-certificate validity and cache-only local chain evidence. Results needing attention: {attention}; incomplete results: {incomplete}. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete certificate evidence before drawing a combined conclusion.");
        if (attention > 0)
            return new(name, CheckState.Attention, detail + "Review each exact certificate identity, intended use, issuing authority and approved renewal or trust process before taking action." + boundary);
        return new(name, CheckState.Information, detail + "No collected source requested review. Effective certificate use remains unverified." + boundary);
    }
}
