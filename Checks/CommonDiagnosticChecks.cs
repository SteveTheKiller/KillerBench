using KillerBench.Engine;
using System.IO;
using System.Text.Json;

internal static class CommonDiagnosticChecks
{
    internal static async Task RunAsync(string folder)
    {
        Require(CommonDiagnosticCatalog.All.Select(area => area.Scope).SequenceEqual(Enum.GetValues<CommonDiagnosticScope>()) &&
            CommonDiagnosticCatalog.All.All(area => !string.IsNullOrWhiteSpace(area.Title) && !string.IsNullOrWhiteSpace(area.Description) &&
                CommonDiagnosticCatalog.Get(area.Scope) == area), "Every selectable diagnostic area has one stable catalog entry");
        Require(CommonDiagnosticCatalog.SummaryAreas.Count == 11 &&
            CommonDiagnosticCatalog.SummaryAreas.Select(area => area.Scope).SequenceEqual(CommonDiagnosticCatalog.All.TakeLast(11).Select(area => area.Scope)),
            "The simple focused selector exposes only stable aggregate outcome areas in canonical order");
        CommonDiagnosticScope[] expectedTailScopes =
            [CommonDiagnosticScope.DriverInstallHistory,
                CommonDiagnosticScope.CrashEvidence, CommonDiagnosticScope.FirewallProfiles, CommonDiagnosticScope.WindowsUpdateContext,
                CommonDiagnosticScope.SystemVolumeDirtyBit, CommonDiagnosticScope.InstallerActivity, CommonDiagnosticScope.SearchHealth,
                CommonDiagnosticScope.SystemRestore, CommonDiagnosticScope.WindowsRecovery, CommonDiagnosticScope.Certificates,
                CommonDiagnosticScope.RestartState, CommonDiagnosticScope.ServicingTransitions, CommonDiagnosticScope.NetworkPathReadiness,
                CommonDiagnosticScope.StorageReadiness, CommonDiagnosticScope.PrintingReadiness, CommonDiagnosticScope.InstallationReadiness,
                CommonDiagnosticScope.ServiceReadiness, CommonDiagnosticScope.WindowsUpdateReadiness, CommonDiagnosticScope.BackupReadiness,
                CommonDiagnosticScope.SystemStabilityReadiness, CommonDiagnosticScope.SecurityReadiness,
                CommonDiagnosticScope.ApplicationDeploymentReadiness];
        Require(CommonDiagnosticCatalog.All.Count == 49 && CommonDiagnosticCatalog.All.TakeLast(22).Select(area => area.Scope).SequenceEqual(expectedTailScopes),
            "Common problems includes bounded driver, crash, firewall, update, filesystem, installer, Search, recovery, certificate, restart, servicing, network, storage, printing, installation, service, Windows Update, backup, system stability, security and application deployment readiness areas");
        CommonDiagnosticScope[] requested = [CommonDiagnosticScope.EventChannels, CommonDiagnosticScope.PrinterPorts, CommonDiagnosticScope.Vitals,
            CommonDiagnosticScope.WindowsLicensing, CommonDiagnosticScope.PrinterPorts];
        var focusedVisits = new List<CommonDiagnosticScope>();
        var focused = await CommonDiagnostics.CheckAsync(selectedScopes: requested, collect: (scope, _) =>
        {
            focusedVisits.Add(scope);
            requested[0] = CommonDiagnosticScope.Security;
            return Task.FromResult<IReadOnlyList<WorkflowCheck>>([new(scope.ToString(), CheckState.Information, "fixture")]);
        });
        CommonDiagnosticScope[] expectedVisits = [CommonDiagnosticScope.Vitals, CommonDiagnosticScope.PrinterPorts,
            CommonDiagnosticScope.WindowsLicensing, CommonDiagnosticScope.EventChannels];
        Require(focusedVisits.SequenceEqual(expectedVisits) && focused.Count == 4,
            "Focused diagnostics snapshot selections, deduplicate and retain canonical order without collecting other scopes");
        var repeatedFinding = new WorkflowCheck("Shared retained evidence", CheckState.Information, "One exact observation");
        var deduplicatedFindings = await CommonDiagnostics.CheckAsync(
            selectedScopes: [CommonDiagnosticScope.Vitals, CommonDiagnosticScope.SystemStabilityReadiness],
            collect: (_, _) => Task.FromResult<IReadOnlyList<WorkflowCheck>>([repeatedFinding]));
        Require(deduplicatedFindings.SequenceEqual([repeatedFinding]),
            "Aggregate scopes do not repeat an exact finding already retained by an earlier scope");
        CommonDiagnosticScope[][] invalidSelections = [[], [(CommonDiagnosticScope)999],
            [.. Enumerable.Repeat(CommonDiagnosticScope.Vitals, 65)]];
        foreach (var invalid in invalidSelections)
        {
            try
            {
                await CommonDiagnostics.CheckAsync(selectedScopes: invalid, collect: (_, _) => throw new Exception("Invalid selection reached collector"));
                throw new Exception("Invalid diagnostic selection was accepted");
            }
            catch (ArgumentException) { }
        }
        CancellationToken supplied = default;
        int persistenceReads = 0;
        var saveFailure = new IOException("Fixture checkpoint unavailable");
        try
        {
            await CommonDiagnostics.CheckAsync(
                collect: (scope, _) =>
                {
                    persistenceReads++;
                    return Task.FromResult<IReadOnlyList<WorkflowCheck>>([new(scope.ToString(), CheckState.Information, "fixture")]);
                }, checkpoint: _ => throw saveFailure);
            throw new Exception("Failed diagnostic checkpoint was reported as complete.");
        }
        catch (IOException error) when (ReferenceEquals(error, saveFailure)) { }
        Require(persistenceReads == 1, "Checkpoint failure stops later collection without claiming a complete saved scan");
        foreach (string failure in new[] { "missing", "stalled", "canceled" })
        {
            int reads = 0;
            using var checkpointStop = new CancellationTokenSource();
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            var pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            try
            {
                await CommonDiagnostics.CheckAsync(checkpointTimeout: TimeSpan.FromMilliseconds(20), token: checkpointStop.Token,
                    collect: (scope, _) => { reads++; return Task.FromResult<IReadOnlyList<WorkflowCheck>>([new(scope.ToString(), CheckState.Information, "fixture")]); },
                    checkpoint: _ => {
                        if (failure == "canceled") checkpointStop.Cancel();
                        return failure == "missing" ? null! : pending.Task;
                    }).WaitAsync(TimeSpan.FromSeconds(3));
                throw new Exception("Unfinished diagnostic checkpoint accepted");
            }
            catch (InvalidOperationException) when (failure == "missing") { }
            catch (TimeoutException) when (failure == "stalled") { }
            catch (OperationCanceledException) when (failure == "canceled") { }
            Require(reads == 1 && elapsed.Elapsed < TimeSpan.FromSeconds(2), "Unfinished checkpoint stops promptly before later scope collection");
            pending.TrySetResult();
        }
        int savedScopes = 0;
        var observerFailure = await CommonDiagnostics.CheckAsync(
            progress: new InlineProgress(_ => throw new InvalidOperationException("Fixture display unavailable")),
            collect: (scope, _) => Task.FromResult<IReadOnlyList<WorkflowCheck>>([new(scope.ToString(), CheckState.Information, "retained")]),
            checkpoint: checks => { savedScopes = checks.Count; return Task.CompletedTask; });
        Require(savedScopes == Enum.GetValues<CommonDiagnosticScope>().Length && observerFailure.Count == savedScopes,
            "Presentation failures cannot interrupt collection or checkpoint persistence");
        var contradicted = await CommonDiagnostics.CheckAsync(collect: (scope, _) =>
            Task.FromResult<IReadOnlyList<WorkflowCheck>>([new(scope.ToString(), CheckState.Clear, "Fixture claimed clear",
                Evidence: new(5, "Retained command failure", "", false))]));
        Require(contradicted.Count == Enum.GetValues<CommonDiagnosticScope>().Length &&
            contradicted.All(check => check.State == CheckState.Unknown && check.Evidence?.ExitCode == 5 &&
                check.Evidence.Output == "Retained command failure" && check.Repair is null),
            "Failed command evidence cannot pass a combined diagnostic scope as clear");
        var bounded = await CommonDiagnostics.CheckAsync(scopeTimeout: TimeSpan.FromMilliseconds(20), collect: (scope, token) =>
        {
            if (scope == CommonDiagnosticScope.DeviceProblems)
            {
                supplied = token;
                return new TaskCompletionSource<IReadOnlyList<WorkflowCheck>>().Task;
            }
            return Task.FromResult<IReadOnlyList<WorkflowCheck>>([new(scope.ToString(), CheckState.Information, "fixture")]);
        }).WaitAsync(TimeSpan.FromSeconds(3));
        Require(supplied.IsCancellationRequested && bounded.Count == Enum.GetValues<CommonDiagnosticScope>().Length && bounded[0].State == CheckState.Unknown &&
            bounded[0].Evidence?.TimedOut == true && bounded.All(check => check.Repair is null),
            "A stalled read-only collector receives cancellation, retains timeout evidence and allows other scopes to finish");
        foreach (var invalidLimit in new[] { TimeSpan.Zero, TimeSpan.FromMinutes(6) })
        {
            bool rejected = false;
            try { await CommonDiagnostics.CheckAsync(scopeTimeout: invalidLimit, collect: (_, _) => throw new Exception("Invalid limit reached provider")); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Require(rejected, "Invalid scope deadlines are rejected before collection");
        }
        using var linkStop = new CancellationTokenSource();
        linkStop.Cancel();
        bool linkCanceled = false;
        try { NetworkSnapshot.ReadLink("Fixture stopped", () => throw new OperationCanceledException(linkStop.Token)); }
        catch (OperationCanceledException error) when (error.CancellationToken == linkStop.Token) { linkCanceled = true; }
        Require(linkCanceled, "Stopping link collection propagates cancellation instead of reporting a network fault");
        foreach (Exception failure in new Exception[] { new System.Net.NetworkInformation.NetworkInformationException(2),
            new InvalidOperationException("Adapter changed"), new NotSupportedException("Unavailable"), new UnauthorizedAccessException("Denied") })
        {
            var failedLink = NetworkSnapshot.ReadLink("Fixture disappeared", () => throw failure);
            Require(failedLink.State == CheckState.Unknown && failedLink.Evidence?.ExitCode == -1 && failedLink.Repair is null,
                "Per-adapter link collection failure retains incomplete evidence");
            Require(NetworkSnapshot.ReadLink("Fixture retained", () => System.Net.NetworkInformation.OperationalStatus.Up).State == CheckState.Clear,
                "Other adapter evidence remains collectible after link failure");
        }
        foreach (var linkState in Enum.GetValues<System.Net.NetworkInformation.OperationalStatus>().Append((System.Net.NetworkInformation.OperationalStatus)999))
        {
            var link = NetworkSnapshot.AssessLink("Fixture link", linkState);
            Require(link.Evidence is not null && link.Repair is null, "Link state retains evidence without proposing adapter changes");
            Require(link.State == (linkState == System.Net.NetworkInformation.OperationalStatus.Up ? CheckState.Clear :
                linkState == System.Net.NetworkInformation.OperationalStatus.Unknown || !Enum.IsDefined(linkState) ? CheckState.Unknown : CheckState.Information),
                "Inactive links are informational; unknown states cannot pass");
        }
        var adapter = NetworkSnapshot.Assess(new("Fixture Ethernet", ["192.0.2.5"], ["192.0.2.1"], ["192.0.2.53"]));
        using var adapterEvidence = JsonDocument.Parse(adapter.Evidence!.Output);
        Require(adapter.State == CheckState.Clear && adapter.Repair is null &&
            adapterEvidence.RootElement.GetProperty("dns")[0].GetString() == "192.0.2.53",
            "Network snapshot retains adapter configuration without claiming connectivity or selecting repairs");
        var isolated = NetworkSnapshot.Assess(new("Fixture isolated", ["169.254.1.2"], [], []));
        Require(isolated.State == CheckState.Attention && isolated.Repair is null && isolated.Evidence is not null,
            "Link-local-only configuration needs review without forcing network changes");
        Require(NetworkSnapshot.Assess(new("Fixture invalid", ["bad-address"], [], [])).State == CheckState.Unknown,
            "Malformed adapter evidence stays unknown");
        var visited = new List<CommonDiagnosticScope>();
        var raw = new ToolResult(0, "fixture", "", false);
        var messages = new List<string>();
        var findings = await CommonDiagnostics.CheckAsync(progress: new InlineProgress(messages.Add), collect: (scope, _) =>
        {
            visited.Add(scope);
            if (scope == CommonDiagnosticScope.Vitals) throw new UnauthorizedAccessException("fixture access");
            if (scope == CommonDiagnosticScope.Security) return Task.FromResult<IReadOnlyList<WorkflowCheck>>([]);
            return Task.FromResult<IReadOnlyList<WorkflowCheck>>([new(scope.ToString(), CheckState.Repairable, "fixture",
                RepairKind.SystemFiles, raw)]);
        });
        Require(visited.SequenceEqual(Enum.GetValues<CommonDiagnosticScope>()), "Each common diagnostic scope runs despite earlier collector failure");
        Require(findings.Count == Enum.GetValues<CommonDiagnosticScope>().Length && findings.Count(f => f.State == CheckState.Unknown) == 2 && findings.All(f => f.Repair is null),
            "Missing coverage stays unknown and supplemental evidence never authorizes repair");
        Require(findings.Where(f => f.State == CheckState.Attention).All(f => ReferenceEquals(f.Evidence, raw)), "Snapshots retain raw evidence");
        Require(messages.First() == "Diagnostic area 1 of 49: Device faults..." &&
            messages.Any(message => message == "Diagnostic area 49 of 49: Application deployment readiness...") &&
            messages.Any(message => message.Contains("Device faults: 1 findings; 1 require review")) &&
            messages.Any(message => message.Contains("Vitals: Unknown")) && messages.Any(message => message.Contains("TimeConfiguration: Attention")),
            "Live progress shows overall position, completed findings and collection failures before the final report");
        var historyMessages = new List<string>();
        var history = await CommonDiagnostics.CheckAsync(progress: new InlineProgress(historyMessages.Add), collect: (scope, _) =>
            Task.FromResult<IReadOnlyList<WorkflowCheck>>([scope switch
            {
                CommonDiagnosticScope.StorageHistory => StorageEvents.Interpret(new(0, "<Events/>", "fixture query error", false)),
                CommonDiagnosticScope.InstallerHistory => InstallerEvents.Interpret(new(0, "<Events/>", "", false)),
                CommonDiagnosticScope.PrintingHistory => new("Print history failure", CheckState.Unknown, "Unavailable", Evidence: new(-1, "", "fixture access", false)),
                CommonDiagnosticScope.ApplicationHistory => ApplicationEvents.Interpret(new(0, "<Events/>", "", false)),
                CommonDiagnosticScope.ServiceHistory => ServiceEvents.Interpret(new(0, "<Events/>", "", false)),
                CommonDiagnosticScope.TimeHistory => TimeServiceEvents.Interpret(new(5, "", "fixture access denied", false)),
                CommonDiagnosticScope.SystemVolumeCapacity => VolumeCapacity.Assess(new("C:", 100L * 1073741824, 5L * 1073741824)),
                _ => new(scope.ToString(), CheckState.Information, "fixture")
            }]));
        Require(history.Single(check => check.Name == "Recent storage events").State == CheckState.Unknown &&
            history.Single(check => check.Name == "Recent installer events").State == CheckState.Information &&
            history.All(check => check.Repair is null) && historyMessages.Any(message => message.StartsWith("Application history: 1 findings; 0 require review")) &&
            historyMessages.Any(message => message.StartsWith("Printing history: 1 findings; 1 require review")),
            "History scopes retain collection errors, continue independently and cannot authorize repairs");
        Require(history.Single(check => check.Name == "Recent service failures").State == CheckState.Information &&
            history.Single(check => check.Name == "Recent time-service events").State == CheckState.Unknown &&
            history.Single(check => check.Name == "Volume capacity").State == CheckState.Attention &&
            historyMessages.Any(message => message.StartsWith("System volume capacity: 1 findings; 1 require review")),
            "Service history and unavailable clock history do not hide low space or authorize mutations");
        using var stop = new CancellationTokenSource();
        var largeMessages = new List<string>();
        var large = Enumerable.Range(0, 25).Select(index => new WorkflowCheck("clear-" + index, CheckState.Clear, "fixture")).ToList();
        large.Add(new("late-fault", CheckState.Attention, new string('x', 3000)));
        large.Add(new("late-unknown", CheckState.Unknown, "incomplete fixture"));
        var retainedLarge = await CommonDiagnostics.CheckAsync(progress: new InlineProgress(largeMessages.Add),
            collect: (scope, _) => Task.FromResult<IReadOnlyList<WorkflowCheck>>(scope == CommonDiagnosticScope.DeviceProblems ? large : [new(scope.ToString(), CheckState.Information, "fixture")]));
        var rendered = largeMessages.Where(message => message.StartsWith("late-") || message.StartsWith("clear-")).ToArray();
        Require(rendered.Length == 20 && rendered[0].StartsWith("late-fault: Attention") && rendered[1].StartsWith("late-unknown: Unknown"),
            "Large inventories surface review findings before clear checks within the display limit");
        Require(rendered[0].Contains("shortened") && retainedLarge.Single(check => check.Name == "late-fault").Detail.Length == 3000 && retainedLarge.Count == large.Count + Enum.GetValues<CommonDiagnosticScope>().Length - 1,
            "Bounded live previews preserve full findings and inventory coverage");
        int calls = 0;
        bool canceled = false;
        try
        {
            await CommonDiagnostics.CheckAsync(collect: (_, _) =>
            {
                calls++;
                stop.Cancel();
                return new TaskCompletionSource<IReadOnlyList<WorkflowCheck>>().Task;
            }, token: stop.Token).WaitAsync(TimeSpan.FromSeconds(3));
        }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled && calls == 1, "Stop releases a stalled read-only collector and prevents later scopes");
        using var journalStop = new CancellationTokenSource();
        string journalFolder = Path.Combine(folder, "partial-common");
        var host = new PartialHost(journalFolder, journalStop);
        var stopped = await Workflows.RunAsync(WorkflowKind.CommonProblems, true, journalFolder, token: journalStop.Token, host: host);
        Require(stopped.Status.StartsWith("Stopped") && stopped.Checks.Count == 1 && stopped.Checks[0].Name == "Retained fixture" && stopped.Repairs.Count == 0,
            "Stopped inspection retains completed evidence without running recipes");
        string finalJournal = await File.ReadAllTextAsync(stopped.JournalPath);
        await host.LateCheckpoint!([new("Late fixture", CheckState.Clear, "Must be ignored")]);
        Require(await File.ReadAllTextAsync(stopped.JournalPath) == finalJournal, "Late checkpoints cannot overwrite final stopped journal");
        foreach (Exception failure in new Exception[] { new IOException("Fixture save failed"), new TimeoutException("Fixture save timed out"), new InvalidOperationException("Fixture save returned no task") })
        {
            string failedFolder = Path.Combine(folder, "partial-" + failure.GetType().Name);
            using var failureStop = new CancellationTokenSource();
            var failedHost = new PartialHost(failedFolder, failureStop) { Failure = failure };
            var failed = await Workflows.RunAsync(WorkflowKind.CommonProblems, true, failedFolder, host: failedHost);
            Require(failed.GetExitCode() != 0 && failed.Checks.Count == 2 &&
                failed.Checks.Single(check => check.Name == "Retained fixture").Evidence?.Output == "raw fixture" &&
                failed.Checks.Single(check => check.Name == "Workflow") is { State: CheckState.Unknown } errorCheck && errorCheck.Detail == failure.Message &&
                failed.Repairs.Count == 0,
                "Inspection failure retains completed raw evidence and prevents automatic recipes");
            string retainedJournal = await File.ReadAllTextAsync(failed.JournalPath);
            await failedHost.LateCheckpoint!([new("Late fixture", CheckState.Clear, "Must not replace failed result")]);
            Require(await File.ReadAllTextAsync(failed.JournalPath) == retainedJournal && retainedJournal.Contains("raw fixture"),
                "Late failed-inspection checkpoints cannot overwrite final evidence");
        }
        Console.WriteLine("PASS: combined diagnostic coverage, independent failures, retained evidence and cancellation (substituted collectors only).");
    }

    private sealed class PartialHost(string folder, CancellationTokenSource stop) : IWorkflowHost
    {
        internal Func<IReadOnlyList<WorkflowCheck>, Task>? LateCheckpoint;
        internal Exception? Failure { get; init; }
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) => throw new Exception("Checkpoint overload expected");
        public async Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, Func<IReadOnlyList<WorkflowCheck>, Task> checkpoint, CancellationToken token)
        {
            LateCheckpoint = checkpoint;
            await checkpoint([new("Retained fixture", CheckState.Attention, "Observed before Stop", Evidence: new(0, "raw fixture", "", false))]);
            using var journal = JsonDocument.Parse(await File.ReadAllTextAsync(Directory.GetFiles(folder, "workflow-*.json").Single()));
            Require(journal.RootElement.GetProperty("status").GetString() == "Running" && journal.RootElement.GetProperty("checks").GetArrayLength() == 1,
                "Completed evidence is durable while inspection still runs");
            if (Failure is not null) throw Failure;
            stop.Cancel();
            return await new TaskCompletionSource<IReadOnlyList<WorkflowCheck>>().Task;
        }
        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token) => throw new Exception("No recipes after Stop");
        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token) => throw new Exception("No repairs after Stop");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private sealed class InlineProgress(Action<string> report) : IProgress<string>
    {
        public void Report(string value) => report(value);
    }
}
