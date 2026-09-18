namespace KillerBench.Engine;

public static class ReliabilityOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck applicationHistory, WorkflowCheck shutdownHistory,
        IReadOnlyList<WorkflowCheck> reliabilityHistory, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(applicationHistory);
        ArgumentNullException.ThrowIfNull(shutdownHistory);
        ArgumentNullException.ThrowIfNull(reliabilityHistory);
        token.ThrowIfCancellationRequested();
        const string name = "Reliability history conclusion";
        const string boundary = " This conclusion covers bounded retained local history only. It is not a current health verdict, root-cause diagnosis or repair authorization. Application launch, dump contents, hardware tests, drivers, firmware, services, files, power and restart behavior remain outside this workflow. No process, application, service, file, driver, hardware, power, restart or setting state changed, and no repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        bool invalidReliability = reliabilityHistory.Count is 0 or > 65 || reliabilityHistory.Any(item =>
            item is null || string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 512);
        if (applicationHistory.Name != "Recent application errors" ||
            shutdownHistory.Name != "Recent unexpected shutdowns" || invalidReliability)
            return Unknown("Reliability inputs were missing, exceeded their bounds or carried an unexpected identity.");

        var all = reliabilityHistory.Prepend(shutdownHistory).Prepend(applicationHistory).ToArray();
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int historicalReview = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed Application error history, unexpected shutdown history and {reliabilityHistory.Count} Reliability Monitor result{(reliabilityHistory.Count == 1 ? "" : "s")}. Historical observations needing review: {historicalReview}; incomplete sources: {incomplete}. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete history before choosing a narrower diagnostic workflow.");
        if (historicalReview > 0)
            return new(name, CheckState.Information, detail + "Match exact application, provider, event and timestamp evidence to the reported symptom before selecting application, hardware, driver or power diagnostics." + boundary);
        return new(name, CheckState.Information, detail + "No retained source requested review. Absence of retained failures does not establish current reliability." + boundary);
    }
}
