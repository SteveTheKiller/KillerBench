namespace KillerBench.Engine.Crash;

public static class StartupPerformanceOperationalCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck performanceHistory, WorkflowCheck restart,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(performanceHistory);
        ArgumentNullException.ThrowIfNull(restart);
        token.ThrowIfCancellationRequested();
        const string name = "Startup performance conclusion";
        const string boundary = " This bounded review does not identify a current bottleneck or prove startup speed, shutdown speed, application behavior, driver behavior, storage performance or power behavior. No startup item, service, application, driver, task, power setting, policy or restart state is changed, and no repair is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);
        if (performanceHistory.Name != "Startup and shutdown performance" || restart.Name != "Restart")
            return Unknown("Startup performance evidence carried an unexpected identity.");
        if (performanceHistory.EffectiveState == CheckState.Unknown || restart.EffectiveState == CheckState.Unknown)
            return Unknown("Performance history or restart evidence was incomplete. Recollect both sources before drawing a combined conclusion.");
        if (restart.EffectiveState is CheckState.Attention or CheckState.Repairable)
            return new(name, CheckState.Attention,
                "A supported pending restart indicator is present. Restart state can affect comparison of retained startup or shutdown history, but it does not explain a degradation record by itself." + boundary);
        if (performanceHistory.EffectiveState is CheckState.Attention or CheckState.Repairable)
            return new(name, CheckState.Information,
                "Retained startup or shutdown degradation records require narrower correlation by time with startup applications, services, drivers, scheduled tasks, storage and power evidence. No supported pending restart indicator was found." + boundary);
        return new(name, CheckState.Information,
            "No retained startup or shutdown performance record requested review, and no supported pending restart indicator was found. Empty or quiet retained history does not establish current performance." + boundary);
    }
}
