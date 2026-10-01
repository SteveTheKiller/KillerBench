using System.IO;
using KillerBench.Engine;

internal static class UpdateFailureFollowupChecks
{
    internal static async Task RunAsync()
    {
        var download = WindowsUpdateErrorCatalog.Find(unchecked((int)0x80240034))!;
        var proxy = WindowsUpdateErrorCatalog.Find(unchecked((int)0x8024401B))!;
        var install = WindowsUpdateErrorCatalog.Find(unchecked((int)0x80240016))!;
        var policy = WindowsUpdateErrorCatalog.Find(unchecked((int)0x80240025))!;
        var servicing = WindowsUpdateErrorCatalog.Find(unchecked((int)0x800F0831))!;
        var service = WindowsUpdateErrorCatalog.Find(unchecked((int)0x80070422))!;
        var timeout = WindowsUpdateErrorCatalog.Find(unchecked((int)0x80240021))!;
        var source = WindowsUpdateErrorCatalog.Find(unchecked((int)0x80244022))!;
        var access = WindowsUpdateErrorCatalog.Find(unchecked((int)0x80070005))!;
        var applicability = WindowsUpdateErrorCatalog.Find(unchecked((int)0x80240017))!;
        var bitsConnection = WindowsUpdateErrorCatalog.Find(unchecked((int)0x80246008))!;
        var dns = WindowsUpdateErrorCatalog.Find(unchecked((int)0x8024402C))!;
        var agentSetup = WindowsUpdateErrorCatalog.Find(unchecked((int)0x8024D009))!;
        var fileLocked = WindowsUpdateErrorCatalog.Find(unchecked((int)0x80070020))!;
        var dataStore = WindowsUpdateErrorCatalog.Find(unchecked((int)0x80248003))!;
        var installerPolicy = WindowsUpdateErrorCatalog.Find(unchecked((int)0x80241003))!;
        var plan = UpdateFailureFollowup.Plan([download, proxy, install, policy, download, servicing, service, timeout, source, access, applicability, bitsConnection, dns, agentSetup, fileLocked]);
        Require(plan.SequenceEqual(
        [
            UpdateDiagnosticArea.ExecutionContext,
            UpdateDiagnosticArea.ApplicabilityContext,
            UpdateDiagnosticArea.UpdateServices,
            UpdateDiagnosticArea.BackgroundTransfers,
            UpdateDiagnosticArea.DeliveryOptimization,
            UpdateDiagnosticArea.NetworkConfiguration,
            UpdateDiagnosticArea.DnsServerConfiguration,
            UpdateDiagnosticArea.RoutingTable,
            UpdateDiagnosticArea.HostsFile,
            UpdateDiagnosticArea.ProxyConfiguration,
            UpdateDiagnosticArea.RestartState,
            UpdateDiagnosticArea.InstallerActivity,
            UpdateDiagnosticArea.ManagedUpdatePolicy,
            UpdateDiagnosticArea.ComponentServicing
        ]), "Recognized failures produce a stable, deduplicated diagnostic plan");
        Require(UpdateFailureFollowup.Plan([bitsConnection]).SequenceEqual([UpdateDiagnosticArea.UpdateServices, UpdateDiagnosticArea.BackgroundTransfers]) &&
            UpdateFailureFollowup.Plan([dns]).SequenceEqual([UpdateDiagnosticArea.NetworkConfiguration, UpdateDiagnosticArea.DnsServerConfiguration, UpdateDiagnosticArea.RoutingTable, UpdateDiagnosticArea.HostsFile, UpdateDiagnosticArea.ProxyConfiguration]) &&
            UpdateFailureFollowup.Plan([agentSetup]).SequenceEqual([UpdateDiagnosticArea.UpdateServices, UpdateDiagnosticArea.ManagedUpdatePolicy]) &&
            UpdateFailureFollowup.Plan([fileLocked]).SequenceEqual([UpdateDiagnosticArea.InstallerActivity]) &&
            UpdateFailureFollowup.Plan([dataStore]).SequenceEqual([UpdateDiagnosticArea.UpdateDataStore]) &&
            UpdateFailureFollowup.Plan([installerPolicy]).SequenceEqual([UpdateDiagnosticArea.InstallerActivity, UpdateDiagnosticArea.ManagedUpdatePolicy]),
            "New update families select only their relevant read-only follow-up areas");

        var called = new List<UpdateDiagnosticArea>();
        var progress = new List<string>();
        var results = await UpdateFailureFollowup.CheckAsync([download, proxy], new ProgressCapture(progress), inspect: (area, token) =>
        {
            token.ThrowIfCancellationRequested();
            called.Add(area);
            IReadOnlyList<WorkflowCheck> findings = [new(area.ToString(), CheckState.Repairable, "Fixture evidence", RepairKind.BackgroundTransfer)];
            return Task.FromResult(findings);
        });
        Require(called.SequenceEqual([UpdateDiagnosticArea.BackgroundTransfers, UpdateDiagnosticArea.DeliveryOptimization, UpdateDiagnosticArea.NetworkConfiguration, UpdateDiagnosticArea.DnsServerConfiguration, UpdateDiagnosticArea.RoutingTable, UpdateDiagnosticArea.HostsFile, UpdateDiagnosticArea.ProxyConfiguration]) &&
            results.Count == 7 && results.All(result => result.Name.StartsWith("Update follow-up: ") && result.Repair is null) && progress.Count == 7,
            "Automatic follow-up runs each selected area once, reports progress and cannot authorize repair");

        var partial = await UpdateFailureFollowup.CheckAsync([proxy], inspect: (_, _) => Task.FromResult<IReadOnlyList<WorkflowCheck>>([]));
        Require(partial is [{ State: CheckState.Unknown, Repair: null }], "Empty provider coverage remains unknown");
        var isolated = await UpdateFailureFollowup.CheckAsync([download], inspect: (area, _) => area == UpdateDiagnosticArea.DeliveryOptimization
            ? throw new IOException("Synthetic provider failure")
            : Task.FromResult<IReadOnlyList<WorkflowCheck>>([new(area.ToString(), CheckState.Information, "Fixture evidence")]));
        Require(isolated.Count == 7 && isolated.Count(result => result.State == CheckState.Unknown) == 1,
            "One failed follow-up does not discard independent evidence");
        await RequireCanceled(UpdateFailureFollowup.CheckAsync([download], token: new CancellationToken(true), inspect: (_, _) =>
            Task.FromResult<IReadOnlyList<WorkflowCheck>>([new("Unexpected", CheckState.Information, "Unexpected")])),
            "Cancellation stops automatic follow-up before providers run");
        Console.WriteLine("PASS: recent update errors trigger stable read-only follow-up diagnostics with isolation and cancellation.");
    }

    private static async Task RequireCanceled(Task task, string message)
    {
        try { await task; throw new InvalidOperationException(message); }
        catch (OperationCanceledException) { }
    }

    private sealed class ProgressCapture(List<string> values) : IProgress<string>
    {
        public void Report(string value) => values.Add(value);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
