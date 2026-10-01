namespace KillerBench.Engine.Devices;

public static class DriverHistoryOperationalCorrelation
{
    private const string FailurePrefix = "Driver installation failure: ";
    private const string CurrentPrefix = "Current state after driver failure: ";

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> results, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(results);
        token.ThrowIfCancellationRequested();
        const string name = "Driver installation history conclusion";
        const string boundary = " This is a bounded correlation of retained SetupAPI history and current Plug and Play configuration codes. It does not establish driver correctness, version suitability, device feature operation or root cause. No device, driver, driver-store content, firmware, service, file or setting changed, and no repair is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        bool invalid = results.Count is 0 or > 66 || results.Any(item => item is null ||
            string.IsNullOrWhiteSpace(item.Name) || item.Name.Length > 1024);
        bool hasHistory = results.Any(item => item.Name == "Driver installation history" ||
            item.Name == "Driver installation history coverage" || item.Name.StartsWith(FailurePrefix, StringComparison.Ordinal));
        if (invalid || !hasHistory)
            return Unknown("Driver history results were missing, exceeded their bounds or carried an unexpected shape.");

        int incomplete = results.Count(item => item.EffectiveState == CheckState.Unknown);
        int historicalFailures = results.Count(item => item.Name.StartsWith(FailurePrefix, StringComparison.Ordinal));
        int currentProblems = results.Count(item => item.Name.StartsWith(CurrentPrefix, StringComparison.Ordinal) &&
            item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed {results.Count} bounded result{(results.Count == 1 ? "" : "s")}. Retained failed installation sections: {historicalFailures}; exact current devices with a nonzero configuration code: {currentProblems}; incomplete results: {incomplete}. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete history or current device inventory before choosing a driver workflow.");
        if (currentProblems > 0)
            return new(name, CheckState.Attention, detail + "Review each exact current device identity and Device Manager code against an approved vendor package before any driver action." + boundary);
        if (historicalFailures > 0)
            return new(name, CheckState.Information, detail + "The retained failures are not accompanied by a current matched device problem in the evaluated inventory. They remain historical context." + boundary);
        return new(name, CheckState.Information, detail + "No failed installation section was retained. Absence of retained failures does not establish driver or device health." + boundary);
    }
}
