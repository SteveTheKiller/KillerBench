namespace KillerBench.Engine.Time;

public static class NativeTimeConfiguration
{
    public static Task<TimeConfigurationResult> RunAsync(TimeOptions options, string folder,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Source is not (TimeSourceMode.CustomPeers or TimeSourceMode.DomainHierarchy))
            throw new ArgumentException("Choose custom peers or the domain hierarchy before applying a time source.", nameof(options));
        return TimeConfiguration.RunAsync(options, folder, CreateHost(progress), token);
    }

    public static Task<TimeConfigurationResult> RunCustomAsync(TimeOptions options, string folder,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.Source != TimeSourceMode.CustomPeers) throw new ArgumentException("Choose custom peers for this operation.", nameof(options));
        return RunAsync(options, folder, progress, token);
    }

    public static ITimeConfigurationHost CreateHost(IProgress<string>? progress = null,
        Func<CancellationToken, Task<TimeEnvironment>>? inspect = null,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null) =>
        new Host(progress, inspect ?? TimeEnvironmentCollector.ReadAsync,
            execute ?? ((step, token) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(45), progress, token)));

    private sealed class Host(IProgress<string>? progress, Func<CancellationToken, Task<TimeEnvironment>> inspect,
        Func<RepairStep, CancellationToken, Task<ToolResult>> execute) : ITimeConfigurationHost
    {
        public Task<TimeEnvironment> InspectAsync(CancellationToken token) => inspect(token);

        public async Task ApplyAsync(TimeOptions options, CancellationToken token)
        {
            var fresh = await TimePlanning.PreviewAsync(options, token, inspect).ConfigureAwait(false);
            if (!fresh.Plan.ReviewPassed || options.Source is not (TimeSourceMode.CustomPeers or TimeSourceMode.DomainHierarchy))
                throw new InvalidOperationException("Time-source prerequisites no longer pass. Configuration was not started.");
            token.ThrowIfCancellationRequested();
            progress?.Report(options.Source == TimeSourceMode.DomainHierarchy
                ? "Applying the Active Directory domain hierarchy as the time source..."
                : "Applying the selected custom NTP peers...");
            var configured = await execute(TimeConfiguration.ConfigurationStep(options), token).ConfigureAwait(false);
            RequireSuccess(configured);
            token.ThrowIfCancellationRequested();
            progress?.Report("Requesting synchronization from the configured source...");
            var synchronization = options.Source == TimeSourceMode.DomainHierarchy
                ? new RepairStep(TimeRecovery.Tool, ["/resync", "/rediscover"], false)
                : TimeRecovery.Step(false);
            var synchronized = await execute(synchronization, token).ConfigureAwait(false);
            RequireSuccess(synchronized);
        }

        public Task<bool> VerifyAsync(TimeOptions options, CancellationToken token)
        {
            progress?.Report("Verifying the selected source and reported synchronization...");
            return TimeConfiguration.VerifyAsync(options, token, inspect, ct => execute(TimeRecovery.Step(true), ct));
        }

        private static void RequireSuccess(ToolResult result)
        {
            if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
                throw new InvalidOperationException("Time command did not complete with usable evidence. State remains unverified; no automatic retry.");
        }
    }
}
