namespace KillerBench.Engine.Services;

public enum InfrastructureServiceFailureArea
{
    Bluetooth,
    Camera,
    SmartCard,
    Biometric,
    FileSharing,
    TaskScheduler,
    DiagnosticPolicy,
    WindowsErrorReporting,
    WindowsEventCollector,
    RemoteAccessConnectionManager,
    RemoteDesktop,
    WindowsRemoteManagement,
    SecurityCenter,
    NetworkConnections,
    WinHttpAutoProxy,
    WindowsConnectionManager,
    NetworkSetup,
    ComPlusEventSystem,
    SystemEventNotification,
    Power,
    PlugAndPlay,
    UserManager,
    GroupPolicyClient,
    NetworkStoreInterface,
    RpcEndpointMapper,
    RemoteProcedureCall,
    DcomServerProcessLauncher,
    BackgroundTasksInfrastructure,
    CoreMessaging
}

public static class InfrastructureServiceFailureEvents
{
    private static readonly Dictionary<InfrastructureServiceFailureArea, ExactServiceFailureEventDefinition> Definitions =
        new()
        {
            [InfrastructureServiceFailureArea.Bluetooth] = Definition("bthserv", "Bluetooth Support Service", "Bluetooth service failure history", "Bluetooth discovery, pairing or device operation"),
            [InfrastructureServiceFailureArea.Camera] = Definition("FrameServer", "Windows Camera Frame Server", "Camera Frame Server failure history", "camera permission, stream availability or device operation"),
            [InfrastructureServiceFailureArea.SmartCard] = Definition("SCardSvr", "Smart Card", "Smart Card service failure history", "reader availability, certificate usability or authentication success"),
            [InfrastructureServiceFailureArea.Biometric] = Definition("WbioSrvc", "Windows Biometric Service", "Windows Biometric service failure history", "sensor availability, enrollment or authentication success"),
            [InfrastructureServiceFailureArea.FileSharing] = Definition("LanmanServer", "Server", "Server service failure history", "share availability, session success or remote reachability"),
            [InfrastructureServiceFailureArea.TaskScheduler] = Definition("Schedule", "Task Scheduler", "Task Scheduler service failure history", "task validity, launch success or schedule completion"),
            [InfrastructureServiceFailureArea.DiagnosticPolicy] = Definition("DPS", "Diagnostic Policy Service", "Diagnostic Policy service failure history", "troubleshooter availability, diagnosis completion or repair outcome"),
            [InfrastructureServiceFailureArea.WindowsErrorReporting] = Definition("WerSvc", "Windows Error Reporting Service", "Windows Error Reporting service failure history", "report creation, upload or problem resolution"),
            [InfrastructureServiceFailureArea.WindowsEventCollector] = Definition("Wecsvc", "Windows Event Collector", "Windows Event Collector service failure history", "subscription validity, source reachability or event delivery"),
            [InfrastructureServiceFailureArea.RemoteAccessConnectionManager] = Definition("RasMan", "Remote Access Connection Manager", "Remote Access Connection Manager failure history", "VPN configuration, authentication or connection success"),
            [InfrastructureServiceFailureArea.RemoteDesktop] = Definition("TermService", "Remote Desktop Services", "Remote Desktop Services failure history", "listener availability, authorization or connection success"),
            [InfrastructureServiceFailureArea.WindowsRemoteManagement] = Definition("WinRM", "Windows Remote Management", "Windows Remote Management service failure history", "listener validity, authentication or remote command success"),
            [InfrastructureServiceFailureArea.SecurityCenter] = Definition("wscsvc", "Security Center", "Security Center service failure history", "protection status, provider registration or system security"),
            [InfrastructureServiceFailureArea.NetworkConnections] = Definition("Netman", "Network Connections", "Network Connections service failure history", "adapter configuration, connection state or network reachability"),
            [InfrastructureServiceFailureArea.WinHttpAutoProxy] = Definition("WinHttpAutoProxySvc", "WinHTTP Web Proxy Auto-Discovery Service", "WinHTTP Auto-Proxy service failure history", "WPAD or PAC discovery, proxy reachability, authentication or application proxy use"),
            [InfrastructureServiceFailureArea.WindowsConnectionManager] = Definition("Wcmsvc", "Windows Connection Manager", "Windows Connection Manager failure history", "connection selection, profile state or network reachability"),
            [InfrastructureServiceFailureArea.NetworkSetup] = Definition("NetSetupSvc", "Network Setup Service", "Network Setup service failure history", "adapter setup, binding state or network reachability"),
            [InfrastructureServiceFailureArea.ComPlusEventSystem] = Definition("EventSystem", "COM+ Event System", "COM+ Event System failure history", "event delivery, subscriber operation or dependent feature health"),
            [InfrastructureServiceFailureArea.SystemEventNotification] = Definition("SENS", "System Event Notification Service", "System Event Notification service failure history", "system notification delivery or subscriber operation"),
            [InfrastructureServiceFailureArea.Power] = Definition("Power", "Power", "Power service failure history", "power-plan behavior, battery state or sleep operation"),
            [InfrastructureServiceFailureArea.PlugAndPlay] = Definition("PlugPlay", "Plug and Play", "Plug and Play service failure history", "device discovery, configuration or operation"),
            [InfrastructureServiceFailureArea.UserManager] = Definition("UserManager", "User Manager", "User Manager service failure history", "account validity, sign-in success or profile availability"),
            [InfrastructureServiceFailureArea.GroupPolicyClient] = Definition("gpsvc", "Group Policy Client", "Group Policy Client service failure history", "policy freshness, authority ownership or policy application"),
            [InfrastructureServiceFailureArea.NetworkStoreInterface] = Definition("nsi", "Network Store Interface Service", "Network Store Interface service failure history", "network state, interface configuration or reachability"),
            [InfrastructureServiceFailureArea.RpcEndpointMapper] = Definition("RpcEptMapper", "RPC Endpoint Mapper", "RPC Endpoint Mapper failure history", "endpoint registration, dependency availability or remote procedure success"),
            [InfrastructureServiceFailureArea.RemoteProcedureCall] = Definition("RpcSs", "Remote Procedure Call", "Remote Procedure Call service failure history", "dependency availability, component activation or remote procedure success"),
            [InfrastructureServiceFailureArea.DcomServerProcessLauncher] = Definition("DcomLaunch", "DCOM Server Process Launcher", "DCOM Server Process Launcher failure history", "component activation, dependency availability or application operation"),
            [InfrastructureServiceFailureArea.BackgroundTasksInfrastructure] = Definition("BrokerInfrastructure", "Background Tasks Infrastructure Service", "Background Tasks Infrastructure service failure history", "background task launch, completion or application operation"),
            [InfrastructureServiceFailureArea.CoreMessaging] = Definition("CoreMessagingRegistrar", "CoreMessaging", "CoreMessaging service failure history", "system message delivery, shell operation or application behavior")
        };

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(InfrastructureServiceFailureArea area,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null, CancellationToken token = default)
    {
        if (!Enum.IsDefined(area)) throw new ArgumentOutOfRangeException(nameof(area));
        token.ThrowIfCancellationRequested();
        return await ExactServiceFailureEvents.CheckManyAsync([Definitions[area]], execute, token: token).ConfigureAwait(false);
    }

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckManyAsync(IEnumerable<InfrastructureServiceFailureArea> areas,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(areas);
        token.ThrowIfCancellationRequested();
        var selected = areas.ToArray();
        if (selected.Length == 0 || selected.Distinct().Count() != selected.Length || selected.Any(area => !Enum.IsDefined(area)))
            throw new ArgumentException("Use one or more distinct infrastructure service areas.", nameof(areas));
        return await ExactServiceFailureEvents.CheckManyAsync([.. selected.Select(area => Definitions[area])], execute, token: token)
            .ConfigureAwait(false);
    }

    public static Task<IReadOnlyList<WorkflowCheck>> CheckAllAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default) =>
        CheckManyAsync(Enum.GetValues<InfrastructureServiceFailureArea>(), execute, token: token);

    private static ExactServiceFailureEventDefinition Definition(string service, string display, string check, string excludedClaim) =>
        new(service, display, check,
            $" Records are historical observations tied to the exact local {display} identity. They do not establish a current outage, prove {excludedClaim} or authorize a service start, restart, startup change, configuration reset, policy change or repair.");
}
