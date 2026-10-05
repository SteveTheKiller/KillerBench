namespace KillerBench.Engine.Updates;

public enum UpdateDiagnosticArea
{
    ExecutionContext,
    ApplicabilityContext,
    UpdateServices,
    BackgroundTransfers,
    DeliveryOptimization,
    NetworkConfiguration,
    DnsServerConfiguration,
    RoutingTable,
    HostsFile,
    ProxyConfiguration,
    RestartState,
    InstallerActivity,
    ManagedUpdatePolicy,
    UpdateDataStore,
    ComponentServicing
}

public static class UpdateFailureFollowup
{
    public static IReadOnlyList<UpdateDiagnosticArea> Plan(IEnumerable<WindowsUpdateCodeReference> references)
    {
        ArgumentNullException.ThrowIfNull(references);
        var selected = new HashSet<UpdateDiagnosticArea>();
        foreach (var reference in references)
        {
            ArgumentNullException.ThrowIfNull(reference);
            foreach (var area in Areas(reference.Code)) selected.Add(area);
        }
        return Array.AsReadOnly(Enum.GetValues<UpdateDiagnosticArea>().Where(selected.Contains).ToArray());
    }

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(IEnumerable<WindowsUpdateCodeReference> references,
        IProgress<string>? progress = null, Func<UpdateDiagnosticArea, CancellationToken, Task<IReadOnlyList<WorkflowCheck>>>? inspect = null,
        CancellationToken token = default)
    {
        var plan = Plan(references);
        if (plan.Count == 0) return [];
        inspect ??= InspectAsync;
        var results = new List<WorkflowCheck>();
        foreach (var area in plan)
        {
            token.ThrowIfCancellationRequested();
            progress?.Report("Following update failure evidence: " + Label(area) + "...");
            IReadOnlyList<WorkflowCheck>? findings;
            try { findings = await inspect(area, token).ConfigureAwait(false); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception)
            {
                findings = [new("Update follow-up: " + Label(area), CheckState.Unknown,
                    "Automatic follow-up collection was unavailable: " + error.Message + ". Other evidence remains usable; no repair was selected.")];
            }
            if (findings is null || findings.Count is < 1 or > 512)
                results.Add(new("Update follow-up: " + Label(area), CheckState.Unknown,
                    "Automatic follow-up returned incomplete coverage. Other evidence remains usable; no repair was selected."));
            else results.AddRange(findings.Select(finding => finding with { Name = "Update follow-up: " + finding.Name, Repair = null }));
        }
        return results.AsReadOnly();
    }

    private static IReadOnlyList<UpdateDiagnosticArea> Areas(uint code) => code switch
    {
        0x80070005 or 0x80240018 or 0x80240020 => [UpdateDiagnosticArea.ExecutionContext],
        0x80244017 => [UpdateDiagnosticArea.ExecutionContext, UpdateDiagnosticArea.ProxyConfiguration],
        0x80244018 => [UpdateDiagnosticArea.ExecutionContext, UpdateDiagnosticArea.ManagedUpdatePolicy],
        0x80240017 => [UpdateDiagnosticArea.ApplicabilityContext],
        0x80070422 or 0x80240001 or 0x8024001E => [UpdateDiagnosticArea.UpdateServices],
        0x80240021 => [UpdateDiagnosticArea.UpdateServices, UpdateDiagnosticArea.NetworkConfiguration, UpdateDiagnosticArea.DnsServerConfiguration, UpdateDiagnosticArea.RoutingTable, UpdateDiagnosticArea.HostsFile, UpdateDiagnosticArea.ProxyConfiguration],
        0x80072EE2 or 0x8024401C or 0x80244021 or 0x80244022 or 0x80244023 or 0x8024402C => [UpdateDiagnosticArea.NetworkConfiguration, UpdateDiagnosticArea.DnsServerConfiguration, UpdateDiagnosticArea.RoutingTable, UpdateDiagnosticArea.HostsFile, UpdateDiagnosticArea.ProxyConfiguration],
        0x80240034 or 0x80246005 or 0x80246009 => [UpdateDiagnosticArea.BackgroundTransfers, UpdateDiagnosticArea.DeliveryOptimization, UpdateDiagnosticArea.NetworkConfiguration, UpdateDiagnosticArea.DnsServerConfiguration, UpdateDiagnosticArea.RoutingTable, UpdateDiagnosticArea.HostsFile, UpdateDiagnosticArea.ProxyConfiguration],
        0x80246008 => [UpdateDiagnosticArea.UpdateServices, UpdateDiagnosticArea.BackgroundTransfers],
        0x80246002 => [UpdateDiagnosticArea.BackgroundTransfers, UpdateDiagnosticArea.DeliveryOptimization, UpdateDiagnosticArea.ComponentServicing],
        0x8024001F => [UpdateDiagnosticArea.NetworkConfiguration, UpdateDiagnosticArea.DnsServerConfiguration, UpdateDiagnosticArea.RoutingTable, UpdateDiagnosticArea.HostsFile, UpdateDiagnosticArea.ProxyConfiguration],
        0x80240030 or 0x8024401B => [UpdateDiagnosticArea.ProxyConfiguration],
        0x00240005 => [UpdateDiagnosticArea.RestartState],
        0x80070020 => [UpdateDiagnosticArea.InstallerActivity],
        0x80240009 or 0x80240016 => [UpdateDiagnosticArea.RestartState, UpdateDiagnosticArea.InstallerActivity],
        0x80241001 or 0x80241002 or 0x80241004 or 0x80241FFF => [UpdateDiagnosticArea.InstallerActivity],
        0x80241003 => [UpdateDiagnosticArea.InstallerActivity, UpdateDiagnosticArea.ManagedUpdatePolicy],
        0x80240025 or 0x8024002E or 0x8024002F or 0x80244011 => [UpdateDiagnosticArea.ManagedUpdatePolicy],
        0x8024D009 => [UpdateDiagnosticArea.UpdateServices, UpdateDiagnosticArea.ManagedUpdatePolicy],
        >= 0x80248000 and <= 0x8024801D or 0x80248FFF => [UpdateDiagnosticArea.UpdateDataStore],
        0x8024002A or 0x8024002C or 0x8024002D or 0x800F081F or 0x800F0831 => [UpdateDiagnosticArea.ComponentServicing],
        _ => []
    };

    private static string Label(UpdateDiagnosticArea area) => area switch
    {
        UpdateDiagnosticArea.ExecutionContext => "execution context",
        UpdateDiagnosticArea.ApplicabilityContext => "update applicability context",
        UpdateDiagnosticArea.UpdateServices => "Windows Update services",
        UpdateDiagnosticArea.BackgroundTransfers => "BITS transfer state",
        UpdateDiagnosticArea.DeliveryOptimization => "Delivery Optimization",
        UpdateDiagnosticArea.NetworkConfiguration => "network configuration",
        UpdateDiagnosticArea.DnsServerConfiguration => "DNS server configuration",
        UpdateDiagnosticArea.RoutingTable => "IP routing table",
        UpdateDiagnosticArea.HostsFile => "Hosts file mappings",
        UpdateDiagnosticArea.ProxyConfiguration => "proxy configuration",
        UpdateDiagnosticArea.RestartState => "pending restart state",
        UpdateDiagnosticArea.InstallerActivity => "Installer activity",
        UpdateDiagnosticArea.ManagedUpdatePolicy => "managed update policy",
        UpdateDiagnosticArea.UpdateDataStore => "Windows Update data store",
        UpdateDiagnosticArea.ComponentServicing => "component servicing health",
        _ => throw new ArgumentOutOfRangeException(nameof(area))
    };

    private static async Task<IReadOnlyList<WorkflowCheck>> InspectAsync(UpdateDiagnosticArea area, CancellationToken token) => area switch
    {
        UpdateDiagnosticArea.ExecutionContext => [UpdateExecutionContextDiagnostics.Check()],
        UpdateDiagnosticArea.ApplicabilityContext => [UpdateApplicabilityDiagnostics.Check()],
        UpdateDiagnosticArea.UpdateServices => await InspectUpdateServicesAsync(token).ConfigureAwait(false),
        UpdateDiagnosticArea.BackgroundTransfers => [await BitsDiagnostics.CheckAsync(false, token).ConfigureAwait(false)],
        UpdateDiagnosticArea.DeliveryOptimization => [await DeliveryOptimizationDiagnostics.CheckAsync(token).ConfigureAwait(false)],
        UpdateDiagnosticArea.NetworkConfiguration => NetworkSnapshot.Check(token),
        UpdateDiagnosticArea.DnsServerConfiguration => [await DnsServerDiagnostics.CheckAsync(token).ConfigureAwait(false)],
        UpdateDiagnosticArea.RoutingTable => [await RouteDiagnostics.CheckAsync(token).ConfigureAwait(false)],
        UpdateDiagnosticArea.HostsFile => [await HostsFileDiagnostics.CheckAsync(token).ConfigureAwait(false)],
        UpdateDiagnosticArea.ProxyConfiguration => ProxyDiagnostics.Check(),
        UpdateDiagnosticArea.RestartState => [RestartDiagnostics.Check()],
        UpdateDiagnosticArea.InstallerActivity => [await InstallerActivityDiagnostics.CheckAsync(token).ConfigureAwait(false)],
        UpdateDiagnosticArea.ManagedUpdatePolicy => [UpdatePolicyDiagnostics.Check()],
        UpdateDiagnosticArea.UpdateDataStore => [await UpdateServiceDiagnostics.CheckAsync(token).ConfigureAwait(false),
            await UpdateHistoryDiagnostics.CheckAsync(token).ConfigureAwait(false)],
        UpdateDiagnosticArea.ComponentServicing => [await PlatformProviders.CreateWorkflowHost().CheckIntegrityAsync(RepairKind.ComponentStore, null, token).ConfigureAwait(false)],
        _ => throw new ArgumentOutOfRangeException(nameof(area))
    };

    private static async Task<IReadOnlyList<WorkflowCheck>> InspectUpdateServicesAsync(CancellationToken token)
    {
        var states = await ServicingServiceStateDiagnostics.CheckAsync(token: token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        var histories = await ServicingServiceFailureEvents.CheckAsync(token: token).ConfigureAwait(false);
        return Array.AsReadOnly(states.Concat(histories).ToArray());
    }
}
