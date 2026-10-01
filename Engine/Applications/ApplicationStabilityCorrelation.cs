namespace KillerBench.Engine.Applications;

public static class ApplicationStabilityCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck applicationEvents,
        IReadOnlyList<WorkflowCheck> reliabilityHistory, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(applicationEvents);
        ArgumentNullException.ThrowIfNull(reliabilityHistory);
        token.ThrowIfCancellationRequested();
        const string name = "Application stability history conclusion";
        const string boundary = " This is a bounded review of retained local history, not a current application-health verdict or proof of root cause. Application launch, dependencies, files, settings, accounts, licensing, network access and vendor-specific logs remain outside this workflow. No process was started or stopped, no application was repaired or removed, and no repair is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        bool invalidReliability = reliabilityHistory.Count is 0 or > 65 || reliabilityHistory.Any(item =>
            item is null || string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 512);
        if (applicationEvents.Name != "Recent application errors" || invalidReliability)
            return Unknown("Application stability inputs were missing, exceeded their bounds or carried an unexpected identity.");

        var all = reliabilityHistory.Prepend(applicationEvents).ToArray();
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int historicalReview = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed the bounded Application error log and {reliabilityHistory.Count} Reliability Monitor result{(reliabilityHistory.Count == 1 ? "" : "s")}. Historical observations needing review: {historicalReview}; incomplete sources: {incomplete}. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete history before using this review to choose a narrower diagnostic workflow.");
        if (historicalReview > 0)
            return new(name, CheckState.Information, detail + "Match the application, time, reported module and repeated symptom before selecting application, installation, device or system diagnostics." + boundary);
        return new(name, CheckState.Information, detail + "No retained source requested review. Absence of retained failures does not establish current application health." + boundary);
    }
}
