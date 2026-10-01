namespace KillerBench.Engine.Devices;

public static class DeviceOperationalCorrelation
{
    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> currentDevices,
        IReadOnlyList<WorkflowCheck> driverHistory, WorkflowCheck setupHistory,
        WorkflowCheck plugAndPlayHistory, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(currentDevices);
        ArgumentNullException.ThrowIfNull(driverHistory);
        ArgumentNullException.ThrowIfNull(setupHistory);
        ArgumentNullException.ThrowIfNull(plugAndPlayHistory);
        token.ThrowIfCancellationRequested();
        const string name = "Device operational conclusion";
        const string boundary = " This conclusion does not verify physical operation, firmware, peripheral power, cables, ports, package compatibility, update availability or vendor support. No device, driver, package, service, configuration, rescan, restart or repair action is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (currentDevices.Count is < 1 or > 2048 || driverHistory.Count is < 1 or > 128 ||
            currentDevices.Any(check => check is null) || driverHistory.Any(check => check is null) ||
            setupHistory.Name != "Recent device setup events" ||
            plugAndPlayHistory.Name != "Recent Plug and Play configuration events")
            return Unknown("Device evidence was missing, exceeded its bound or carried an unexpected identity.");

        var currentDriver = driverHistory.Where(check => check.Name.StartsWith("Current state after driver failure: ", StringComparison.Ordinal) ||
            check.Name == "Current device correlation").ToArray();
        var historicalDriver = driverHistory.Except(currentDriver).ToArray();
        var all = currentDevices.Concat(driverHistory).Append(setupHistory).Append(plugAndPlayHistory).ToArray();
        int incomplete = all.Count(check => check.EffectiveState == CheckState.Unknown);
        int currentAttention = currentDevices.Concat(currentDriver).Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        int historyAttention = historicalDriver.Append(setupHistory).Append(plugAndPlayHistory)
            .Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed {currentDevices.Count} current device results, {driverHistory.Count} retained driver-install results, device setup history and Plug and Play history. Current evidence needing attention: {currentAttention}; historical results needing review: {historyAttention}; incomplete results: {incomplete}. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete device or history evidence before drawing a combined conclusion.");
        if (currentAttention > 0)
            return new(name, CheckState.Attention, detail + "Review each exact present device code and its approved driver source before selecting a targeted action." + boundary);
        return new(name, CheckState.Information, detail + (historyAttention > 0
            ? "Historical failures direct narrower identity-based review but do not establish a current device fault."
            : "No collected source established a current device fault. Device operation remains unverified.") + boundary);
    }
}
