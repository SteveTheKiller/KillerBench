using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace KillerBench.Engine;

public static class ServiceRecovery
{
    public sealed record Dependency(string Name, int? Startup);
    private static bool ValidDependencyName(string? name) => !string.IsNullOrWhiteSpace(name) &&
        name.Length <= 256 && name == name.Trim() && !name.Any(c => char.IsControl(c) || c is '\\' or '/');

    [SupportedOSPlatform("windows")]
    private static (bool Delayed, bool Complete) DelayedAutoStart(RegistryKey? key, int? startup)
    {
        if (startup != 2) return (false, true);
        object? configured = key?.GetValue("DelayedAutoStart", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        return configured switch
        {
            null => (false, true),
            int value when value == 0 => (false, true),
            int value when value == 1 => (true, true),
            _ => (false, false)
        };
    }

    [SupportedOSPlatform("windows")]
    private static bool DelayedStartWindowElapsed(RegistryKey root)
    {
        using var control = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Control");
        object? configured = control?.GetValue("AutoStartDelay", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        long delayMilliseconds = configured switch
        {
            null => 120_000,
            int value when value >= 0 && value <= 86_400_000 => value,
            _ => -1
        };
        return delayMilliseconds >= 0 && Environment.TickCount64 >= delayMilliseconds + 30_000;
    }

    public static WorkflowCheck AssessDependencies(WorkflowCheck check, IReadOnlyList<Dependency> dependencies, bool complete)
    {
        ArgumentNullException.ThrowIfNull(check);
        ArgumentNullException.ThrowIfNull(dependencies);
        if (check.State != CheckState.Repairable) return check;
        if (!complete || dependencies.Count > 64 || dependencies.Any(d => d is null || !ValidDependencyName(d.Name) || d.Startup is not (0 or 1 or 2 or 3 or 4)))
            return check with { State = CheckState.Unknown, Repair = null, Detail = "Service dependency configuration is incomplete. No automatic start selected." };
        string? target = check.Repair is { } repair ? ServiceName(repair) : null;
        if (target is null || dependencies.Any(d => string.Equals(d.Name, target, StringComparison.OrdinalIgnoreCase)) ||
            dependencies.Select(d => d.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != dependencies.Count)
            return check with { State = CheckState.Unknown, Repair = null, Detail = "Service dependencies contain a self-reference, duplicate identity or unsupported target. No automatic start selected." };
        var disabled = dependencies.Where(d => d.Startup == 4).Select(d => d.Name).ToArray();
        return disabled.Length == 0 ? check : check with
        {
            State = CheckState.Attention, Repair = null,
            Detail = "Disabled service dependencies: " + string.Join(", ", disabled) + ". Startup settings are preserved; review the configuration before retrying."
        };
    }

    public static string? ServiceName(RepairKind kind) => kind switch
    {
        RepairKind.PrintSpooler => "Spooler",
        RepairKind.WindowsTime => "W32Time",
        RepairKind.WindowsInstaller => "msiserver",
        RepairKind.DhcpClient => "Dhcp",
        RepairKind.DnsClient => "Dnscache",
        RepairKind.Workstation => "LanmanWorkstation",
        RepairKind.UpdateService => "wuauserv",
        RepairKind.BackgroundTransfer => "BITS",
        RepairKind.CryptographicServices => "CryptSvc",
        RepairKind.ModulesInstaller => "TrustedInstaller",
        RepairKind.WindowsUpdateMedicService => "WaaSMedicSvc",
        RepairKind.UpdateOrchestrator => "UsoSvc",
        RepairKind.AudioEndpoints => "AudioEndpointBuilder",
        RepairKind.WindowsAudio => "Audiosrv",
        RepairKind.WindowsSearch => "WSearch",
        RepairKind.WindowsManagement => "winmgmt",
        RepairKind.WindowsEventLog => "EventLog",
        RepairKind.VolumeShadowCopy => "VSS",
        RepairKind.SoftwareShadowProvider => "swprv",
        RepairKind.WindowsBackupService => "SDRSVC",
        RepairKind.BlockLevelBackupEngineService => "wbengine",
        RepairKind.BaseFilteringEngine => "BFE",
        RepairKind.WindowsFirewall => "MpsSvc",
        RepairKind.WirelessAutoConfig => "WlanSvc",
        RepairKind.WiredAutoConfig => "dot3svc",
        RepairKind.NetworkLocationAwareness => "NlaSvc",
        RepairKind.NetworkListService => "netprofm",
        RepairKind.IpHelper => "iphlpsvc",
        RepairKind.BluetoothSupport => "bthserv",
        RepairKind.CameraFrameServer => "FrameServer",
        RepairKind.SmartCardService => "SCardSvr",
        RepairKind.BiometricService => "WbioSrvc",
        RepairKind.ServerService => "LanmanServer",
        RepairKind.TaskSchedulerService => "Schedule",
        RepairKind.DiagnosticPolicyService => "DPS",
        RepairKind.WindowsErrorReportingService => "WerSvc",
        RepairKind.WindowsEventCollectorService => "Wecsvc",
        RepairKind.RemoteAccessConnectionManagerService => "RasMan",
        RepairKind.RemoteDesktopServices => "TermService",
        RepairKind.WindowsRemoteManagement => "WinRM",
        RepairKind.SecurityCenterService => "wscsvc",
        RepairKind.NetworkConnectionsService => "Netman",
        RepairKind.WinHttpAutoProxyService => "WinHttpAutoProxySvc",
        RepairKind.WindowsConnectionManagerService => "Wcmsvc",
        RepairKind.NetworkSetupService => "NetSetupSvc",
        RepairKind.AppReadinessService => "AppReadiness",
        RepairKind.StateRepositoryService => "StateRepository",
        RepairKind.UserProfileService => "ProfSvc",
        RepairKind.CredentialManagerService => "VaultSvc",
        RepairKind.ShellHardwareDetectionService => "ShellHWDetection",
        RepairKind.DeviceInstallService => "DeviceInstall",
        RepairKind.AppxDeploymentService => "AppXSvc",
        RepairKind.ClientLicenseService => "ClipSVC",
        RepairKind.StoreInstallService => "InstallService",
        RepairKind.WindowsLicenseManagerService => "LicenseManager",
        RepairKind.PushToInstallService => "PushToInstall",
        RepairKind.WebAccountManagerService => "TokenBroker",
        RepairKind.MicrosoftAccountSignInService => "wlidsvc",
        RepairKind.StorageService => "StorSvc",
        RepairKind.OptimizeDrivesService => "defragsvc",
        RepairKind.WindowsImageAcquisitionService => "stisvc",
        RepairKind.FunctionDiscoveryProviderHostService => "fdPHost",
        RepairKind.FunctionDiscoveryResourcePublicationService => "FDResPub",
        RepairKind.HumanInterfaceDeviceService => "hidserv",
        RepairKind.PortableDeviceEnumeratorService => "WPDBusEnum",
        RepairKind.WindowsFontCacheService => "FontCache",
        RepairKind.DeviceAssociationService => "DeviceAssociationService",
        RepairKind.TextInputManagementService => "TextInputManagementService",
        RepairKind.DisplayEnhancementService => "DisplayEnhancementService",
        RepairKind.ComPlusEventSystem => "EventSystem",
        RepairKind.SystemEventNotificationService => "SENS",
        RepairKind.PowerService => "Power",
        RepairKind.PlugAndPlayService => "PlugPlay",
        RepairKind.UserManagerService => "UserManager",
        RepairKind.GroupPolicyClientService => "gpsvc",
        RepairKind.NetworkStoreInterfaceService => "nsi",
        RepairKind.RpcEndpointMapperService => "RpcEptMapper",
        RepairKind.RemoteProcedureCallService => "RpcSs",
        RepairKind.DcomServerProcessLauncherService => "DcomLaunch",
        RepairKind.BackgroundTasksInfrastructureService => "BrokerInfrastructure",
        RepairKind.CoreMessagingService => "CoreMessagingRegistrar",
        _ => null
    };

    public static int? State(ToolResult result)
    {
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return null;
        var lines = Regex.Matches(result.Output, @"(?m)^[\t ]*STATE[\t ]*:[^\r\n]*", RegexOptions.CultureInvariant);
        if (lines.Count != 1) return null;
        var match = Regex.Match(lines[0].Value, @":[\t ]*([1-7])(?:[\t ]+([A-Za-z_]+))?[\t ]*$", RegexOptions.CultureInvariant);
        if (!match.Success) return null;
        int state = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        string? label = match.Groups[2].Success ? match.Groups[2].Value : null;
        string expected = state switch
        {
            1 => "STOPPED", 2 => "START_PENDING", 3 => "STOP_PENDING", 4 => "RUNNING",
            5 => "CONTINUE_PENDING", 6 => "PAUSE_PENDING", 7 => "PAUSED", _ => ""
        };
        return label is null || string.Equals(label, expected, StringComparison.Ordinal) ? state : null;
    }

    public static bool MatchesService(ToolResult result, string expectedService)
    {
        if (!ValidDependencyName(expectedService)) throw new ArgumentException("Invalid service name.", nameof(expectedService));
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return false;
        var names = Regex.Matches(result.Output, @"(?m)^[\t ]*SERVICE_NAME[\t ]*:[\t ]*([^\r\n]*)", RegexOptions.CultureInvariant);
        return names.Count == 1 && string.Equals(names[0].Groups[1].Value.Trim(), expectedService, StringComparison.OrdinalIgnoreCase);
    }

    public static WorkflowCheck Interpret(RepairKind kind, ToolResult result, int? startup, bool demandStartAuthorized = false)
    {
        demandStartAuthorized &= kind is RepairKind.WindowsTime or RepairKind.WinHttpAutoProxyService or RepairKind.WindowsUpdateMedicService;
        string? expected = ServiceName(kind);
        if (expected is null || !MatchesService(result, expected))
            return new(kind.ToString(), CheckState.Unknown, "Service evidence does not identify the requested supported service. No repair selected.", Evidence: result);
        if (startup is not (2 or 3))
            return new(kind.ToString(), CheckState.Attention, "Service missing, disabled, or configuration unavailable. No startup setting will be changed.", Evidence: result);
        if (startup == 3 && State(result) == 1 && !demandStartAuthorized)
            return new(kind.ToString(), CheckState.Information,
                "Demand-start service is stopped. This can be normal when idle, so a stopped state alone does not authorize an automatic start. Startup settings are preserved.", Evidence: result);
        if (State(result) == 1)
        {
            var fields = Regex.Matches(result.Output, @"(?m)^[\t ]*WIN32_EXIT_CODE[\t ]*:[^\r\n]*", RegexOptions.CultureInvariant);
            if (fields.Count > 0)
            {
                var field = Regex.Match(fields[0].Value, @":\s*([0-9]+)(?:\s+\(0x([0-9a-fA-F]+)\))?\s*$", RegexOptions.CultureInvariant);
                if (fields.Count != 1 || !field.Success ||
                    !uint.TryParse(field.Groups[1].Value, out uint code) ||
                    (field.Groups[2].Success && (!uint.TryParse(field.Groups[2].Value, System.Globalization.NumberStyles.HexNumber,
                        System.Globalization.CultureInfo.InvariantCulture, out uint hex) || hex != code)))
                    return new(kind.ToString(), CheckState.Unknown, "Service exit-code evidence is ambiguous. No automatic start selected.", Evidence: result);
                if (code == 1077)
                    return new WorkflowCheck(kind.ToString(), CheckState.Repairable,
                        demandStartAuthorized ? "A focused current feature check requires this stopped demand-start service. The selected recovery flow can start it after dependency checks without changing startup settings." :
                        "Automatic service is stopped and reports no start attempt since this boot (1077). The selected recovery flow can start it after dependency checks without changing startup settings.", kind, result)
                        { DemandStartAuthorized = demandStartAuthorized };
                string? review = code switch
                {
                    2 or 3 => "Review the configured executable and required paths with the component owner. Do not download or replace files solely from this code.",
                    5 => "Review the service account and required resource permissions. No permissions or security controls are changed.",
                    193 => "Review the configured executable format and component installation. No executable is replaced.",
                    1053 or 1070 => "Review Service Control Manager events and the component's startup logs for a timeout or stalled start. Check dependency readiness and resource pressure before retrying. Service timeout settings are preserved.",
                    1064 or 1067 => "Review the component's application events and crash evidence for an exception or unexpected process exit. Preserve crash dumps and check recent component changes before retrying. No crash data or configuration is removed.",
                    1057 or 1069 => "Review the configured service account, logon rights and account status with its owner. No credentials are requested or changed.",
                    1058 => "Review disabled service or associated-device configuration with its owner. Startup types and device settings are preserved.",
                    1059 => "Review circular service dependencies with the component owner. Dependency registrations are preserved.",
                    1068 => "Review the dependency service or group that failed to start, including its own exit evidence. Dependency configuration is preserved.",
                    1072 => "Service removal is pending. Let the owning installer or administrator finish that operation before collecting fresh evidence. No service is recreated.",
                    1075 => "Review missing or deletion-pending dependencies. Dependency registrations are preserved.",
                    1079 => "Review the service account against other services sharing its process with the component owner. Service identities and credentials are preserved.",
                    1066 => "Review the service-specific exit code in the raw evidence and the component's own logs.",
                    0 => null,
                    _ => "Review Service Control Manager events and the component's logs for this reported failure. This code has no specific recovery mapping; service configuration is preserved."
                };
                if (review is not null)
                    return new(kind.ToString(), CheckState.Attention,
                        $"Stopped service reports Win32 exit code {code}. This describes its reported exit, not a confirmed current cause. " +
                        review + " Automatic start is deferred pending review of this evidence.", Evidence: result);
            }
        }
        return State(result) switch
        {
            4 => new(kind.ToString(), CheckState.Clear, "Service is running. This does not verify the complete feature.", Evidence: result),
            1 => new WorkflowCheck(kind.ToString(), CheckState.Repairable,
                demandStartAuthorized ? "A focused current feature check requires this stopped demand-start service. This recovery can start it without changing its startup type." :
                "Automatic service is stopped. This selected recovery flow can start it without changing its startup type.", kind, result)
                { DemandStartAuthorized = demandStartAuthorized },
            2 or 3 or 5 or 6 or 7 => new(kind.ToString(), CheckState.Attention, "Service is transitioning or paused. No automatic start selected; retry after it settles.", Evidence: result),
            _ => new(kind.ToString(), CheckState.Unknown, "Service state could not be recognized. No repair selected.", Evidence: result)
        };
    }

    internal static Task<WorkflowCheck> CheckAsync(RepairKind kind, CancellationToken token) =>
        CheckAsync(kind, token, false);

    internal static async Task<WorkflowCheck> CheckAsync(RepairKind kind, CancellationToken token, bool demandStartAuthorized)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        string service = ServiceName(kind) ?? throw new ArgumentException("Unsupported service.");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + service);
        var result = await BoundedTool.RunAsync(new("sc.exe", ["query", service], true), TimeSpan.FromSeconds(15), token: token).ConfigureAwait(false);
        if (!MatchesService(result, service))
            return new(kind.ToString(), CheckState.Unknown, "The service query did not identify the requested service. No repair selected.", Evidence: result);
        var check = Interpret(kind, result, key?.GetValue("Start") as int?, demandStartAuthorized);
        if (check.State != CheckState.Repairable || key is null) return check;
        var graph = ServiceDependencyGraph.Read(service, dependency =>
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
            using var dependencyKey = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + dependency);
            object? configured = dependencyKey?.GetValue("DependOnService");
            object? groups = dependencyKey?.GetValue("DependOnGroup");
            int? startup = dependencyKey?.GetValue("Start") as int?;
            var delayed = DelayedAutoStart(dependencyKey, startup);
            return new(startup, configured as string[] ?? [],
                delayed.Complete && dependencyKey is not null && configured is null or string[] && (groups is null || groups is string[] { Length: 0 }),
                delayed.Delayed);
        }, token);
        if (graph.TargetDelayedAutoStart)
            graph = graph with { DelayedStartWindowElapsed = DelayedStartWindowElapsed(root) };
        return AssessGraph(check, graph);
    }

    public static WorkflowCheck AssessGraph(WorkflowCheck check, ServiceDependencyGraphResult graph)
    {
        ArgumentNullException.ThrowIfNull(check);
        ArgumentNullException.ThrowIfNull(graph);
        var snapshot = graph with { Dependencies = graph.Dependencies.ToArray(), Issues = graph.Issues.ToArray() };
        check = check with { ServiceDependencies = snapshot };
        var assessed = check.State == CheckState.Repairable && graph.TargetStartup != 2 && !(graph.TargetStartup == 3 && check.DemandStartAuthorized)
            ? check with { State = CheckState.Unknown, Repair = null,
                Detail = "Target startup configuration changed or could not be confirmed during dependency inspection. No automatic start selected." }
            : check.State == CheckState.Repairable && graph.TargetDelayedAutoStart && !graph.DelayedStartWindowElapsed
            ? check with { State = CheckState.Attention, Repair = null,
                Detail = "Delayed automatic service is still inside the configured startup window. No automatic start selected; retry after Windows finishes delayed service startup." }
            : AssessDependencies(check, snapshot.Dependencies, snapshot.Complete);
        return graph.Issues.Count == 0 ? assessed : assessed with { Detail = assessed.Detail + "\n" + string.Join("\n", graph.Issues) };
    }

    public static bool CanStart(RepairKind kind, WorkflowCheck check) => ServiceName(kind) is { } service &&
        check.State == CheckState.Repairable && check.Repair == kind && check.Evidence is { } evidence &&
        MatchesService(evidence, service) && State(evidence) == 1 &&
        check.ServiceDependencies is { Complete: true, Dependencies: not null, Issues.Count: 0 } graph &&
        (graph.TargetStartup == 2 || graph.TargetStartup == 3 && check.DemandStartAuthorized) &&
        (!graph.TargetDelayedAutoStart || graph.DelayedStartWindowElapsed) &&
        Interpret(kind, evidence, graph.TargetStartup, check.DemandStartAuthorized).State == CheckState.Repairable &&
        AssessDependencies(check, graph.Dependencies, graph.Complete).State == CheckState.Repairable;

    public static bool CanStart(RepairKind kind, WorkflowCheck check, ServiceStartRegistrationEvidence registration) =>
        CanStart(kind, check) && ServiceStartPrerequisite.Assess(kind, check, registration).State == CheckState.Repairable;

    public static ToolResult DeferredStart(WorkflowCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        var evidence = check.Evidence ?? new ToolResult(-1, "", "", false);
        return evidence with { ExitCode = -1, Error = "Service start deferred: " + check.Detail +
            (string.IsNullOrEmpty(evidence.Error) ? "" : "\n" + evidence.Error) };
    }

    internal static async Task<ToolResult> ExecuteAsync(RepairStep step, IProgress<string>? progress, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            if (!step.Verification)
            {
                var kind = Enum.GetValues<RepairKind>().Where(k => ServiceName(k) is not null &&
                    step.Arguments.Length == 2 && step.Arguments[0] == "start" &&
                    string.Equals(ServiceName(k), step.Arguments[1], StringComparison.OrdinalIgnoreCase)).Cast<RepairKind?>().SingleOrDefault();
                if (kind is null) throw new ArgumentException("Unsupported service start command.", nameof(step));
                progress?.Report("Rechecking service state, dependencies and executable registration before start...");
                bool focusedDemandStart = kind.Value is RepairKind.WindowsTime or RepairKind.WinHttpAutoProxyService or RepairKind.WindowsUpdateMedicService;
                var check = await CheckAsync(kind.Value, timeout.Token, focusedDemandStart).ConfigureAwait(false);
                if (check.State == CheckState.Clear)
                    throw new RepairStepSkippedException("The selected service is already running. No start command was run.", check.Evidence!);
                if (!CanStart(kind.Value, check))
                    throw new RepairDispatchDeferredException("Service start was deferred because fresh exact eligibility was not available. " + check.Detail);
                if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
                var registration = ServiceStartPrerequisite.Read(step.Arguments[1]);
                var registered = ServiceStartPrerequisite.Assess(kind.Value, check, registration);
                if (!CanStart(kind.Value, check, registration))
                    throw new RepairDispatchDeferredException("Service start was deferred because fresh executable registration eligibility was not available. " + registered.Detail);
            }
            if (step.Verification)
            {
                if (step.Arguments.Length != 2 || step.Arguments[0] != "query")
                    throw new ArgumentException("Unsupported service verification command.", nameof(step));
                return await ObserveRunningAsync(step.Arguments[1],
                    token => NativeTool.RunAsync(step.Tool, step.Arguments, progress: progress, token: token), timeout.Token).ConfigureAwait(false);
            }
            var result = await NativeTool.RunAsync(step.Tool, step.Arguments, progress: progress, token: timeout.Token).ConfigureAwait(false);
            // A concurrent start is harmless; still require the separate verification query.
            return !step.Verification && result.ExitCode == 1056 ? result with { ExitCode = 0 } : result;
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested) { return new(-1, "", "Service command timed out. State is unverified.", false) { TimedOut = true }; }
    }

    public static async Task<ToolResult> ObserveRunningAsync(string service, Func<CancellationToken, Task<ToolResult>> query, CancellationToken token = default, TimeSpan? observationTimeout = null)
    {
        if (!ValidDependencyName(service)) throw new ArgumentException("Invalid service name.", nameof(service));
        ArgumentNullException.ThrowIfNull(query);
        var limit = observationTimeout ?? TimeSpan.FromSeconds(20);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromSeconds(20)) throw new ArgumentOutOfRangeException(nameof(observationTimeout));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(limit);
        bool previouslyRunning = false;
        ToolResult? lastObservation = null;
        try
        {
            while (true)
            {
                deadline.Token.ThrowIfCancellationRequested();
                Task<ToolResult>? pending = query(deadline.Token);
                if (pending is null) return ObservationProviderFailure(lastObservation);
                ToolResult? result = await pending.WaitAsync(deadline.Token).ConfigureAwait(false);
                if (result is null) return ObservationProviderFailure(lastObservation);
                lastObservation = result;
                if (!MatchesService(result, service))
                    return result with { ExitCode = -1, Error = result.Error + "\nService verification did not identify the requested service." };
                int? state = State(result);
                if (state == 4 && previouslyRunning) return result;
                if (previouslyRunning || state is not (2 or 4))
                    return result with { ExitCode = -1, Error = result.Error + "\nService did not remain running across verification observations. Idle shutdown can be normal; feature operation remains unverified." };
                previouslyRunning = state == 4;
                await Task.Delay(500, deadline.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            var retained = lastObservation ?? new ToolResult(-1, "", "", false);
            return retained with { ExitCode = -1, TimedOut = true,
                Error = retained.Error + "\nService observation timed out. Last observation retained; current state is unverified." };
        }
        catch (Exception error) when (error is not OperationCanceledException and not OutOfMemoryException)
        {
            return ObservationProviderFailure(lastObservation);
        }
    }

    private static ToolResult ObservationProviderFailure(ToolResult? lastObservation)
    {
        var retained = lastObservation ?? new ToolResult(-1, "", "", false);
        return retained with { ExitCode = -1,
            Error = retained.Error + "\nService observation provider failed. Last observation retained; current state is unverified." };
    }
}
