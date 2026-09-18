namespace KillerBench.Engine;

public static class HardwareOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck hardwareHistory, WorkflowCheck memoryHistory,
        IReadOnlyList<WorkflowCheck> deviceProblems, WorkflowCheck physicalDisks,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(hardwareHistory);
        ArgumentNullException.ThrowIfNull(memoryHistory);
        ArgumentNullException.ThrowIfNull(deviceProblems);
        ArgumentNullException.ThrowIfNull(physicalDisks);
        token.ThrowIfCancellationRequested();
        const string name = "Hardware operational conclusion";
        const string boundary = " This bounded correlation is not a memory test, disk surface test, SMART verdict, firmware diagnostic, vendor hardware test, backup check or proof of fault-free operation. WHEA records do not identify a replaceable component by themselves. No hardware, firmware, driver, disk, pool, volume, file, service, power, restart or setting change is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);
        bool invalidDevices = deviceProblems.Count is < 1 or > 256 || deviceProblems.Any(check => check is null ||
            !(check.Name == "Device problems" || check.Name.StartsWith("Problems ", StringComparison.Ordinal)));
        if (hardwareHistory.Name != "Recent Windows hardware error records" ||
            memoryHistory.Name != "Windows Memory Diagnostic results" ||
            physicalDisks.Name != "Physical disk evidence correlation" || invalidDevices)
            return Unknown("Hardware evidence was missing, oversized or carried an unexpected identity.");
        var evidence = deviceProblems.Prepend(hardwareHistory).Prepend(memoryHistory).Append(physicalDisks).ToArray();
        int incomplete = evidence.Count(check => check.EffectiveState == CheckState.Unknown);
        int attention = evidence.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed WHEA history, Windows Memory Diagnostic history, {deviceProblems.Count} current device result{(deviceProblems.Count == 1 ? "" : "s")} and exact physical-disk reliability correlation. Results needing attention: {attention}; incomplete results: {incomplete}. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete sources before drawing a combined hardware conclusion.");
        if (attention > 0)
            return new(name, CheckState.Attention,
                detail + "Correlate exact identities and timestamps with firmware and vendor diagnostics before selecting a hardware or driver action." + boundary);
        return new(name, CheckState.Information,
            detail + "No collected source requested review. Current physical behavior and future reliability remain unverified." + boundary);
    }
}
