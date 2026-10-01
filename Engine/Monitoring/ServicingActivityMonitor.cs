using System.Diagnostics;
using System.Text.Json;

namespace KillerBench.Engine.Monitoring;

public sealed record ServicingMonitorOptions(
    int RefreshSeconds = 3,
    int Samples = 1,
    bool IncludeEvents = true,
    bool IncludeHistory = true,
    bool IncludeServices = true,
    bool IncludeRestart = true,
    bool IncludeSetup = true,
    bool IncludeCompatibility = true,
    bool IncludeReadiness = true,
    bool IncludeTransfers = true,
    bool IncludeDeliveryOptimization = true,
    bool IncludeInstallerActivity = true,
    bool IncludeScheduledTask = true)
{
    public ServicingMonitorOptions Validate()
    {
        if (RefreshSeconds is < 1 or > 60) throw new ArgumentOutOfRangeException(nameof(RefreshSeconds), "Refresh seconds must be from 1 through 60.");
        if (Samples is < 1 or > 120) throw new ArgumentOutOfRangeException(nameof(Samples), "Samples must be from 1 through 120.");
        if (!IncludeEvents && !IncludeHistory && !IncludeServices && !IncludeRestart && !IncludeSetup && !IncludeCompatibility && !IncludeReadiness && !IncludeTransfers && !IncludeDeliveryOptimization && !IncludeInstallerActivity && !IncludeScheduledTask)
            throw new ArgumentException("Select at least one servicing evidence group.", nameof(ServicingMonitorOptions));
        return this;
    }
}

public sealed record ServicingActivitySnapshot(
    int Sequence,
    DateTimeOffset CapturedUtc,
    bool ServicingProcessActive,
    CheckState State,
    IReadOnlyList<WorkflowCheck> Checks);

public sealed record ServicingProcessGroup(
    string Name,
    int Count,
    long? WorkingSetBytes,
    long? OldestAgeSeconds,
    double? CpuPercent,
    bool MetricsComplete);

public sealed record ServicingProcessSnapshot(
    DateTimeOffset CapturedUtc,
    IReadOnlyList<ServicingProcessGroup> Groups);

public static class ServicingActivityText
{
    private const int LiveDetailLimit = 320;

    public static string FormatLiveSummary(ServicingActivitySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var lines = new List<string>
        {
            $"SNAPSHOT {snapshot.Sequence} / {snapshot.State.ToString().ToUpperInvariant()} / {snapshot.CapturedUtc:HH:mm:ss} UTC",
            "SERVICING PROCESS / " + (snapshot.ServicingProcessActive ? "ACTIVE" : "IDLE")
        };
        foreach (var check in snapshot.Checks)
            lines.Add($"{check.State.ToString().ToUpperInvariant()} / {check.Name}: {OneLine(check.Detail)}");
        return string.Join("\n", lines);
    }

    public static string FormatReport(IReadOnlyList<ServicingActivitySnapshot> snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        var lines = new List<string>
        {
            "SERVICING ACTIVITY MONITOR / READ ONLY",
            "Activity and resource use are point-in-time evidence. They do not prove progress, completion, a stall or a fault.",
            ""
        };
        foreach (var snapshot in snapshots)
        {
            lines.Add($"SNAPSHOT {snapshot.Sequence} / {snapshot.CapturedUtc:O} / {snapshot.State.ToString().ToUpperInvariant()}");
            lines.Add("Supported servicing process active: " + (snapshot.ServicingProcessActive ? "Yes" : "No"));
            foreach (var check in snapshot.Checks)
            {
                lines.Add(check.Name + " / " + check.State);
                lines.Add(check.Detail);
            }
            lines.Add("");
        }
        lines.Add("No update, setup, transfer, service, process, policy, restart or repair action was requested.");
        return string.Join("\n", lines);
    }

    private static string OneLine(string value)
    {
        string normalized = string.Join(" ", (value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= LiveDetailLimit ? normalized : normalized[..(LiveDetailLimit - 3)] + "...";
    }
}

public interface IServicingActivityProvider
{
    Task<WorkflowCheck> ReadProcessesAsync(CancellationToken token);
    Task<WorkflowCheck> ReadEventsAsync(CancellationToken token);
    Task<WorkflowCheck> ReadHistoryAsync(CancellationToken token);
    Task<WorkflowCheck> ReadServicesAsync(CancellationToken token);
    Task<WorkflowCheck> ReadRestartAsync(CancellationToken token);
    Task<WorkflowCheck> ReadSetupAsync(CancellationToken token);
    Task<WorkflowCheck> ReadCompatibilityAsync(CancellationToken token);
    Task<WorkflowCheck> ReadReadinessAsync(CancellationToken token);
    Task<WorkflowCheck> ReadTransfersAsync(CancellationToken token);
    Task<WorkflowCheck> ReadDeliveryOptimizationAsync(CancellationToken token);
    Task<WorkflowCheck> ReadInstallerActivityAsync(CancellationToken token);
    Task<WorkflowCheck> ReadScheduledTaskAsync(CancellationToken token);
}

public static class ServicingActivityMonitor
{
    public static async Task<IReadOnlyList<ServicingActivitySnapshot>> ObserveAsync(
        ServicingMonitorOptions options,
        IServicingActivityProvider? provider = null,
        IProgress<string>? progress = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        provider ??= new WindowsServicingActivityProvider();
        var snapshots = new List<ServicingActivitySnapshot>(options.Samples);
        for (int sequence = 1; sequence <= options.Samples; sequence++)
        {
            token.ThrowIfCancellationRequested();
            progress?.Report($"Collecting servicing activity snapshot {sequence} of {options.Samples}.");
            progress?.Report("Reading bounded servicing process activity...");
            var checks = new List<WorkflowCheck> { await provider.ReadProcessesAsync(token).ConfigureAwait(false) };
            if (options.IncludeEvents) { progress?.Report("Reading recent Windows Update events..."); checks.Add(await provider.ReadEventsAsync(token).ConfigureAwait(false)); }
            if (options.IncludeHistory) { progress?.Report("Reading Windows Update history..."); checks.Add(await provider.ReadHistoryAsync(token).ConfigureAwait(false)); }
            if (options.IncludeServices) { progress?.Report("Reading servicing service states..."); checks.Add(await provider.ReadServicesAsync(token).ConfigureAwait(false)); }
            if (options.IncludeRestart) { progress?.Report("Reading restart indicators..."); checks.Add(await provider.ReadRestartAsync(token).ConfigureAwait(false)); }
            if (options.IncludeSetup) { progress?.Report("Reading bounded Panther setup context..."); checks.Add(await provider.ReadSetupAsync(token).ConfigureAwait(false)); }
            if (options.IncludeCompatibility) { progress?.Report("Reading bounded setup compatibility counts..."); checks.Add(await provider.ReadCompatibilityAsync(token).ConfigureAwait(false)); }
            if (options.IncludeReadiness) { progress?.Report("Reading servicing readiness context..."); checks.Add(await provider.ReadReadinessAsync(token).ConfigureAwait(false)); }
            if (options.IncludeTransfers) { progress?.Report("Reading current-account background transfers..."); checks.Add(await provider.ReadTransfersAsync(token).ConfigureAwait(false)); }
            if (options.IncludeDeliveryOptimization) { progress?.Report("Reading Delivery Optimization activity..."); checks.Add(await provider.ReadDeliveryOptimizationAsync(token).ConfigureAwait(false)); }
            if (options.IncludeInstallerActivity) { progress?.Report("Reading privacy-bounded Windows Installer activity..."); checks.Add(await provider.ReadInstallerActivityAsync(token).ConfigureAwait(false)); }
            if (options.IncludeScheduledTask) { progress?.Report("Reading the exact Windows Update scheduled task..."); checks.Add(await provider.ReadScheduledTaskAsync(token).ConfigureAwait(false)); }
            DateTimeOffset capturedUtc = DateTimeOffset.UtcNow;
            checks.Insert(1, ServicingPhaseDiagnostics.Summarize(checks, capturedUtc));
            if (sequence > 1 && (options.IncludeTransfers || options.IncludeDeliveryOptimization))
                checks.Add(ServicingTransferTrendDiagnostics.Compare(snapshots[^1], checks, capturedUtc));
            bool active = ProcessActivityDiagnostics.Active(checks[0]);
            CheckState state = checks.Any(check => check.State == CheckState.Unknown) ? CheckState.Unknown :
                checks.Any(check => check.State == CheckState.Attention) ? CheckState.Attention : CheckState.Information;
            var snapshot = new ServicingActivitySnapshot(sequence, capturedUtc, active, state, checks.AsReadOnly());
            snapshots.Add(snapshot);
            progress?.Report(ServicingActivityText.FormatLiveSummary(snapshot));
            if (sequence < options.Samples)
                await Task.Delay(TimeSpan.FromSeconds(options.RefreshSeconds), token).ConfigureAwait(false);
        }
        return snapshots.AsReadOnly();
    }
}

public static class ProcessActivityDiagnostics
{
    public static readonly string[] Names =
    [
        "Windows11InstallationAssistant",
        "setup",
        "SetupHost",
        "SetupPrep",
        "wuaulcore",
        "TiWorker",
        "TrustedInstaller",
        "MoUsoCoreWorker",
        "UsoClient",
        "WaaSMedicAgent",
        "dism"
    ];

    public static WorkflowCheck Check() => new ServicingProcessSampler().Check();

    public static WorkflowCheck Interpret(IReadOnlyDictionary<string, int> counts)
    {
        ArgumentNullException.ThrowIfNull(counts);
        if (counts.Count != Names.Length || Names.Any(name => !counts.TryGetValue(name, out int count) || count is < 0 or > 256) ||
            counts.Keys.Any(name => !Names.Contains(name, StringComparer.OrdinalIgnoreCase)))
            return new("Active servicing processes", CheckState.Unknown,
                "Servicing process evidence is incomplete or outside its bounds. No process action was selected.");
        var groups = Names.Select(name => counts[name] == 0
            ? new ServicingProcessGroup(name, 0, 0, null, null, true)
            : new ServicingProcessGroup(name, counts[name], null, null, null, false)).ToArray();
        return Interpret(new ServicingProcessSnapshot(DateTimeOffset.UtcNow, groups));
    }

    public static WorkflowCheck Interpret(ServicingProcessSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.CapturedUtc == default || snapshot.CapturedUtc > DateTimeOffset.UtcNow.AddMinutes(5) ||
            snapshot.Groups is null || snapshot.Groups.Count != Names.Length ||
            snapshot.Groups.Select(group => group.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Names.Length ||
            Names.Any(name => !snapshot.Groups.Any(group => group.Name.Equals(name, StringComparison.OrdinalIgnoreCase))) ||
            snapshot.Groups.Any(group => !Names.Contains(group.Name, StringComparer.OrdinalIgnoreCase) || group.Count is < 0 or > 256 ||
                (group.CpuPercent is < 0 or > 100) ||
                (group.Count == 0 && (group.WorkingSetBytes != 0 || group.OldestAgeSeconds is not null || group.CpuPercent is not null and not 0 || !group.MetricsComplete)) ||
                (group.Count > 0 && group.MetricsComplete && (group.WorkingSetBytes is null or < 0 or > 4_398_046_511_104L || group.OldestAgeSeconds is null or < 0 or > 315_360_000L)) ||
                (group.Count > 0 && !group.MetricsComplete && (group.WorkingSetBytes is not null || group.OldestAgeSeconds is not null || group.CpuPercent is not null))))
            return new("Active servicing processes", CheckState.Unknown,
                "Servicing process evidence is incomplete or outside its bounds. No process action was selected.");

        int total = snapshot.Groups.Sum(group => group.Count);
        string detail = total == 0
            ? "No supported Windows Update or Setup servicing process is active in this point-in-time snapshot. This does not prove that servicing is idle between samples."
            : "Active process groups: " + string.Join(", ", Names.Select(name => snapshot.Groups.Single(group => group.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                .Where(group => group.Count > 0)
                .Select(group => group.Name + "=" + group.Count + (group.MetricsComplete
                    ? $" ({group.WorkingSetBytes!.Value / 1048576d:F1} MiB, oldest {group.OldestAgeSeconds} sec" +
                        (group.CpuPercent is null ? ")" : $", CPU {group.CpuPercent:F1}%)")
                    : " (resource totals unavailable)"))) + ". Activity and resource use do not prove progress or a fault.";
        detail += " Process IDs, command lines, users and executable paths are excluded. No process was started, stopped or opened.";
        return new("Active servicing processes", CheckState.Information, detail,
            Evidence: new(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false));
    }

    public static bool Active(WorkflowCheck check)
    {
        if (check.Name != "Active servicing processes" || check.State == CheckState.Unknown || check.Evidence is not { ExitCode: 0 } evidence) return false;
        try
        {
            var snapshot = JsonSerializer.Deserialize<ServicingProcessSnapshot>(evidence.Output, Capture.JsonOptions);
            if (snapshot?.Groups is not null) return snapshot.Groups.Any(group => group.Count > 0);
            var counts = JsonSerializer.Deserialize<Dictionary<string, int>>(evidence.Output, Capture.JsonOptions);
            return counts is not null && counts.Values.Any(value => value > 0);
        }
        catch (JsonException) { return false; }
    }

}

public sealed class ServicingProcessSampler
{
    private readonly Lock _gate = new();
    private readonly Dictionary<int, double> _previousCpuSeconds = [];
    private DateTimeOffset? _previousCapturedUtc;

    public WorkflowCheck Check()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Servicing process inspection requires Windows.");
        lock (_gate)
        {
            try
            {
                DateTimeOffset capturedUtc = DateTimeOffset.UtcNow;
                double elapsedSeconds = _previousCapturedUtc is null ? 0 : (capturedUtc - _previousCapturedUtc.Value).TotalSeconds;
                bool intervalAvailable = elapsedSeconds is >= 0.25 and <= 120;
                int processorCount = Math.Max(1, Environment.ProcessorCount);
                var nextCpuSeconds = new Dictionary<int, double>();
                var groups = new List<ServicingProcessGroup>(ProcessActivityDiagnostics.Names.Length);

                foreach (string name in ProcessActivityDiagnostics.Names)
                {
                    using var processes = new ProcessCollection(Process.GetProcessesByName(name));
                    int count = processes.Items.Length;
                    if (count == 0)
                    {
                        groups.Add(new(name, 0, 0, null, intervalAvailable ? 0 : null, true));
                        continue;
                    }

                    long workingSetBytes = 0;
                    long oldestAgeSeconds = 0;
                    double cpuDeltaSeconds = 0;
                    bool metricsComplete = count <= 256;
                    bool cpuComplete = intervalAvailable && metricsComplete;
                    if (metricsComplete)
                    {
                        foreach (var process in processes.Items)
                        {
                            try
                            {
                                workingSetBytes = checked(workingSetBytes + process.WorkingSet64);
                                var age = capturedUtc - new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
                                if (age < TimeSpan.Zero || age > TimeSpan.FromDays(3650)) { metricsComplete = false; cpuComplete = false; break; }
                                oldestAgeSeconds = Math.Max(oldestAgeSeconds, (long)age.TotalSeconds);

                                double totalCpuSeconds = process.TotalProcessorTime.TotalSeconds;
                                nextCpuSeconds[process.Id] = totalCpuSeconds;
                                if (!_previousCpuSeconds.TryGetValue(process.Id, out double previousCpu) || totalCpuSeconds < previousCpu)
                                    cpuComplete = false;
                                else
                                    cpuDeltaSeconds += totalCpuSeconds - previousCpu;
                            }
                            catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException or OverflowException)
                            {
                                metricsComplete = false;
                                cpuComplete = false;
                                break;
                            }
                        }
                    }

                    double? cpuPercent = cpuComplete
                        ? Math.Round(Math.Clamp(cpuDeltaSeconds / elapsedSeconds / processorCount * 100, 0, 100), 1)
                        : null;
                    groups.Add(new(name, count, metricsComplete ? workingSetBytes : null,
                        metricsComplete ? oldestAgeSeconds : null, metricsComplete ? cpuPercent : null, metricsComplete));
                }

                _previousCpuSeconds.Clear();
                foreach (var pair in nextCpuSeconds) _previousCpuSeconds[pair.Key] = pair.Value;
                _previousCapturedUtc = capturedUtc;
                return ProcessActivityDiagnostics.Interpret(new ServicingProcessSnapshot(capturedUtc, groups.AsReadOnly()));
            }
            catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
            {
                return new("Active servicing processes", CheckState.Unknown,
                    "The bounded servicing process snapshot is unavailable. No process was started, stopped or opened.",
                    Evidence: new(-1, "", error.GetType().Name, false));
            }
        }
    }

    private sealed class ProcessCollection(Process[] items) : IDisposable
    {
        public Process[] Items { get; } = items;
        public void Dispose()
        {
            foreach (var process in Items) process.Dispose();
        }
    }
}

public sealed class WindowsServicingActivityProvider : IServicingActivityProvider
{
    private readonly ServicingProcessSampler _processSampler = new();

    public Task<WorkflowCheck> ReadProcessesAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(_processSampler.Check());
    }

    public Task<WorkflowCheck> ReadEventsAsync(CancellationToken token) =>
        WindowsUpdateActivityDiagnostics.CheckAsync(token);
    public Task<WorkflowCheck> ReadHistoryAsync(CancellationToken token) => UpdateHistoryDiagnostics.CheckAsync(token);
    public Task<WorkflowCheck> ReadTransfersAsync(CancellationToken token) => BitsDiagnostics.CheckAsync(token: token);
    public Task<WorkflowCheck> ReadDeliveryOptimizationAsync(CancellationToken token) => DeliveryOptimizationActivityDiagnostics.CheckAsync(token);
    public Task<WorkflowCheck> ReadInstallerActivityAsync(CancellationToken token) => ServicingInstallerActivityDiagnostics.CheckAsync(token);
    public Task<WorkflowCheck> ReadScheduledTaskAsync(CancellationToken token) => WindowsUpdateScheduledTaskDiagnostics.CheckAsync(token);
    public Task<WorkflowCheck> ReadSetupAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(SetupActivityDiagnostics.Check());
    }
    public Task<WorkflowCheck> ReadCompatibilityAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(SetupCompatibilityDiagnostics.Check());
    }
    public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(RestartDiagnostics.Check());
    }

    public async Task<WorkflowCheck> ReadServicesAsync(CancellationToken token)
    {
        var inventory = await ServiceInventoryDiagnostics.CheckAsync(token).ConfigureAwait(false);
        if (inventory.State == CheckState.Unknown || inventory.Evidence is not { ExitCode: 0 } evidence)
            return new("Servicing services", CheckState.Unknown,
                "The exact servicing service snapshot is unavailable. No service action was selected.", Evidence: inventory.Evidence);
        try
        {
            var snapshot = ServiceInventoryDiagnostics.Parse(evidence.Output);
            string[] names = ["wuauserv", "BITS", "CryptSvc", "TrustedInstaller", "WaaSMedicSvc", "UsoSvc"];
            var selected = snapshot.Services.Where(service => names.Contains(service.Name, StringComparer.OrdinalIgnoreCase)).ToArray();
            if (!snapshot.Complete || selected.Select(item => item.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Length)
                return new("Servicing services", CheckState.Unknown,
                    "One or more exact servicing services were unavailable in the bounded inventory. Missing evidence does not establish a missing service.", Evidence: evidence);
            string detail = string.Join("; ", names.Select(name =>
            {
                var service = selected.Single(item => item.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                return service.Name + "=" + service.State + "/" + service.StartMode;
            }));
            return new("Servicing services", CheckState.Information,
                detail + ". Service state is point-in-time context and does not authorize a start, stop or configuration change.", Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return new("Servicing services", CheckState.Unknown,
                "Servicing service evidence could not be interpreted. No service action was selected.", Evidence: evidence);
        }
    }

    public async Task<WorkflowCheck> ReadReadinessAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        string volume = Path.GetPathRoot(Environment.SystemDirectory)!.TrimEnd('\\');
        WorkflowCheck capacity = VolumeCapacity.Check(volume);
        WorkflowCheck power = await ServicingPowerDiagnostics.CheckAsync(token).ConfigureAwait(false);
        var bitLockerChecks = await BitLockerDiagnostics.CheckVolumeAsync(volume, token).ConfigureAwait(false);
        WorkflowCheck bitLocker = bitLockerChecks.Count == 1 ? bitLockerChecks[0] :
            new("BitLocker " + volume, CheckState.Unknown, "The operating-system volume was not uniquely identified.");
        CheckState state = new[] { capacity, power, bitLocker }.Any(check => check.State == CheckState.Unknown) ? CheckState.Unknown :
            new[] { capacity, power, bitLocker }.Any(check => check.State == CheckState.Attention) ? CheckState.Attention : CheckState.Information;
        string detail = "Storage: " + capacity.Detail + " Power: " + power.Detail + " Encryption: " + bitLocker.Detail +
            " These are point-in-time readiness signals. They do not establish upgrade eligibility or authorize a servicing or encryption change.";
        return new("Servicing readiness", state, detail);
    }
}
