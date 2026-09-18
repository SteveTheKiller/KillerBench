namespace KillerBench.Engine;

public static class SystemStabilityOperationalCorrelation
{
    private static readonly string[] VitalNames =
        ["Memory snapshot", "Physical disk health", "System uptime", "Windows Memory Diagnostic results"];

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> vitals, IReadOnlyList<WorkflowCheck> devices,
        IReadOnlyList<WorkflowCheck> reliability, IReadOnlyList<WorkflowCheck> hardwareErrors,
        IReadOnlyList<WorkflowCheck> crashes, IReadOnlyList<WorkflowCheck> shutdowns,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(vitals);
        ArgumentNullException.ThrowIfNull(devices);
        ArgumentNullException.ThrowIfNull(reliability);
        ArgumentNullException.ThrowIfNull(hardwareErrors);
        ArgumentNullException.ThrowIfNull(crashes);
        ArgumentNullException.ThrowIfNull(shutdowns);
        token.ThrowIfCancellationRequested();
        const string name = "System stability operational correlation";
        const string boundary = " This is a bounded correlation of current snapshots and retained history, not proof of stable hardware, software, drivers, firmware or future operation. No test was scheduled, no application was launched and no hardware, driver, service, file, dump, firmware, power or restart state changed. No repair is selected.";
        WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        bool invalidSize = new[] { vitals, devices, reliability, hardwareErrors, crashes, shutdowns }
            .Any(group => group.Count is 0 or > 256);
        bool invalidVitals = VitalNames.Any(required => vitals.Count(item => item.Name == required) != 1);
        bool invalidHardware = hardwareErrors.Count(item => item.Name == "Recent Windows hardware error records") != 1;
        bool invalidShutdown = shutdowns.Count(item => item.Name == "Recent unexpected shutdowns") != 1;
        bool invalidCrash = crashes.Count(item => item.Name == "Crash dump configuration") != 1 ||
            crashes.Count(item => item.Name == "Crash dump files") != 1 ||
            crashes.Count(item => item.Name == "Recent system crash reports") != 1;
        if (invalidSize || invalidVitals || invalidHardware || invalidShutdown || invalidCrash)
            return Unknown("System stability correlation inputs were missing, duplicated, oversized or carried an unexpected identity.");

        var all = vitals.Concat(devices).Concat(reliability).Concat(hardwareErrors).Concat(crashes).Concat(shutdowns).ToArray();
        int incomplete = all.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = all.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed {all.Length} current vital, device, reliability, hardware-error, crash and shutdown results. Correlated evidence: {attention} need attention and {incomplete} incomplete. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete evidence before drawing a combined stability conclusion.");
        if (attention > 0)
            return new(name, CheckState.Attention, detail + "Review timestamps and exact retained evidence together before selecting targeted hardware, driver, application or operating-system diagnostics." + boundary);
        return new(name, CheckState.Information, detail + "No collected source requested review. Future stability and fault-free operation remain unverified." + boundary);
    }
}
