using System.IO;
using System.Net;
using System.Net.Sockets;
using KillerBench.Engine;

internal static class DnsCacheRecoveryChecks
{
    private static readonly string[] DnsClientDependency = ["network.dns-client"];

    private static DnsLookupResult Failed(string host = "fixture.example", SocketError error = SocketError.HostNotFound) =>
        new(host, DnsLookupState.Failed, [], "fixture") { ErrorCode = error };

    internal static async Task RunAsync(string scratch)
    {
        var selected = Failed();
        var previewHost = new Host();
        var previewJournal = new Journal();
        var preview = await DnsCacheRecovery.RunAsync(selected, false, previewHost, previewJournal);
        Require(preview.Outcome == DnsCacheRecoveryOutcome.Preview && preview.GetExitCode() == 0 && previewHost.Reads == 0 &&
            previewHost.Flushes == 0 && previewJournal.Begins == 0 && preview.Detail.Contains("exact hostname"),
            "Preview explains the bounded action without touching providers");

        foreach (var ineligible in new[]
        {
            new DnsLookupResult("fixture.example", DnsLookupState.TimedOut, [], "fixture"),
            Failed(error: SocketError.TryAgain), Failed(error: SocketError.NetworkUnreachable),
            new DnsLookupResult("fixture.example", DnsLookupState.NoAddresses, [], "fixture")
        })
        {
            var host = new Host();
            var journal = new Journal();
            var blocked = await DnsCacheRecovery.RunAsync(ineligible, true, host, journal);
            Require(blocked.Outcome == DnsCacheRecoveryOutcome.Blocked && host.Reads == 0 && host.Flushes == 0 && journal.Begins == 0,
                "Timeout, transient, routing and incomplete failures cannot authorize a flush");
        }

        var successJournal = new Journal();
        var successHost = new Host { After = new("fixture.example", DnsLookupState.Resolved, ["192.0.2.4"], "fixture") };
        var progress = new Observer();
        var success = await DnsCacheRecovery.RunAsync(selected, true, successHost, successJournal, progress: progress);
        Require(success.Outcome == DnsCacheRecoveryOutcome.Verified && success.GetExitCode() == 0 && success.FlushDispatched && successHost.Reads == 3 &&
            successHost.Flushes == 1 && successJournal.Begins == 1 && successJournal.Result == success &&
            success.Before?.Host == "fixture.example" && success.After?.Addresses.Single() == "192.0.2.4" && progress.Messages.Count == 6,
            "Execution rechecks, prepares, confirms eligibility again, flushes and verifies the exact hostname in order");

        var standardHost = new Host { IsAdministrator = false };
        var standardJournal = new Journal();
        var standard = await DnsCacheRecovery.RunAsync(selected, true, standardHost, standardJournal);
        Require(standard.Outcome == DnsCacheRecoveryOutcome.Blocked && standardHost.Reads == 1 && standardHost.Flushes == 0 && standardJournal.Begins == 0,
            "Fresh eligible evidence does not bypass administrator prerequisite");

        var changedHost = new Host { Before = new("fixture.example", DnsLookupState.Resolved, ["192.0.2.5"], "fixture") };
        var changedJournal = new Journal();
        var changed = await DnsCacheRecovery.RunAsync(selected, true, changedHost, changedJournal);
        Require(changed.Outcome == DnsCacheRecoveryOutcome.Blocked && changedHost.Flushes == 0 && changedJournal.Begins == 0,
            "Changed fresh evidence blocks mutation");

        var finalChangedHost = new Host
        {
            Immediate = new("fixture.example", DnsLookupState.Resolved, ["192.0.2.6"], "fixture")
        };
        var finalChangedJournal = new Journal();
        var finalChanged = await DnsCacheRecovery.RunAsync(selected, true, finalChangedHost, finalChangedJournal);
        Require(finalChanged.Outcome == DnsCacheRecoveryOutcome.Blocked && finalChangedHost.Reads == 2 &&
            finalChangedHost.Flushes == 0 && finalChangedJournal.Begins == 1 && finalChangedJournal.Result == finalChanged &&
            finalChanged.After?.Addresses.Single() == "192.0.2.6" && finalChanged.Detail.Contains("No cache flush"),
            "A resolved final eligibility check records an exact no-flush outcome after preparation");

        foreach (bool canceled in new[] { false, true })
        {
            var journal = new Journal();
            var host = new Host { ReadFailure = canceled ? new OperationCanceledException("fixture final cancellation") :
                new InvalidDataException("fixture final lookup failure"), FailRead = 2 };
            var result = await DnsCacheRecovery.RunAsync(selected, true, host, journal);
            Require(result.Outcome == (canceled ? DnsCacheRecoveryOutcome.Stopped : DnsCacheRecoveryOutcome.Blocked) &&
                !result.FlushDispatched && host.Flushes == 0 && journal.Result == result,
                "Unavailable final DNS evidence records an exact no-flush outcome");
        }

        foreach (var after in new[]
        {
            Failed(),
            new DnsLookupResult("fixture.example", DnsLookupState.TimedOut, [], "fixture")
        })
        {
            var journal = new Journal();
            var host = new Host { After = after };
            var result = await DnsCacheRecovery.RunAsync(selected, true, host, journal);
            Require(result.Outcome == (after.State == DnsLookupState.TimedOut ? DnsCacheRecoveryOutcome.Uncertain : DnsCacheRecoveryOutcome.Failed) &&
                result.GetExitCode() == 3 && host.Flushes == 1 && journal.Result == result,
                "Unresolved and incomplete verification cannot claim recovery");
        }

        var failedCommandJournal = new Journal();
        var failedCommandHost = new Host { Flush = new(1, "", "fixture failure", false) };
        var failedCommand = await DnsCacheRecovery.RunAsync(selected, true, failedCommandHost, failedCommandJournal);
        Require(failedCommand.Outcome == DnsCacheRecoveryOutcome.Uncertain && failedCommand.FlushDispatched && failedCommandHost.Reads == 2 && failedCommandHost.Flushes == 1 &&
            failedCommandJournal.Result == failedCommand, "Failed command evidence remains uncertain and skips verification");

        var deniedJournal = new Journal { Deny = true };
        var deniedHost = new Host();
        try { await DnsCacheRecovery.RunAsync(selected, true, deniedHost, deniedJournal); throw new Exception("Journal rejection was ignored."); }
        catch (IOException) { }
        Require(deniedHost.Flushes == 0, "Mutation cannot start without durable preparation");

        string folder = Path.Combine(scratch, "dns-cache-recovery");
        var durable = new DnsCacheRecoveryJournal(folder);
        string pending = await durable.BeginAsync(selected, CancellationToken.None);
        Require(File.Exists(pending), "Durable journal publishes pending evidence before mutation");
        try { await new DnsCacheRecoveryJournal(folder).BeginAsync(selected, CancellationToken.None); throw new Exception("Pending DNS cache operation was ignored."); }
        catch (IOException) { }
        var durableResult = preview with { Outcome = DnsCacheRecoveryOutcome.Uncertain, JournalPath = pending };
        await durable.CompleteAsync(durableResult, CancellationToken.None);
        Require(!File.Exists(pending) && Directory.GetFiles(folder, "dns-cache-*.json").Length == 2,
            "Completed journal archives both preparation and outcome evidence");

        string dispatchedFolder = Path.Combine(scratch, "dns-cache-dispatched-interruption");
        var dispatchedHost = new Host { FlushFailure = new IOException("fixture dispatched interruption") };
        var dispatched = await DnsCacheRecovery.RunAsync(selected, true, dispatchedHost,
            new DnsCacheRecoveryJournal(dispatchedFolder));
        string dispatchedPending = Path.Combine(dispatchedFolder, "dns-cache.pending.json");
        Require(dispatched.Outcome == DnsCacheRecoveryOutcome.Uncertain && dispatched.FlushDispatched &&
            dispatchedHost.Flushes == 1 && File.Exists(dispatchedPending) &&
            Directory.GetFiles(dispatchedFolder, "dns-cache-*.json").Length == 1,
            "A dispatched interrupted flush retains pending and attempt evidence");
        try
        {
            await new DnsCacheRecoveryJournal(dispatchedFolder).BeginAsync(selected, CancellationToken.None);
            throw new Exception("Dispatched interrupted DNS recovery was repeated.");
        }
        catch (IOException) { }

        string emptyFolder = Path.Combine(scratch, "dns-cache-empty");
        var none = await DnsCacheReconciliation.RunAsync(emptyFolder,
            (host, token) => Task.FromResult(new DnsLookupResult(host, DnsLookupState.Resolved, ["192.0.2.9"], "fixture")));
        Require(none.Outcome == DnsCacheReconciliationOutcome.NoPendingRecord && none.GetExitCode() == 0,
            "Recovery review reports an absent pending record without querying a resolver");

        string resolvedFolder = Path.Combine(scratch, "dns-cache-resolved");
        string resolvedPending = await new DnsCacheRecoveryJournal(resolvedFolder).BeginAsync(selected, CancellationToken.None);
        var resolvedProgress = new Observer();
        var reconciled = await DnsCacheReconciliation.RunAsync(resolvedFolder,
            (host, token) => Task.FromResult(new DnsLookupResult(host, DnsLookupState.Resolved, ["192.0.2.10"], "fixture")),
            progress: resolvedProgress);
        Require(reconciled.Outcome == DnsCacheReconciliationOutcome.Resolved && reconciled.GetExitCode() == 0 &&
            !File.Exists(resolvedPending) && File.Exists(reconciled.ArchivePath) && File.Exists(reconciled.ArchivePath + ".prepared.json") &&
            resolvedProgress.Messages.Count == 2, "Recovery review archives a resolved exact-host operation without another flush");

        string failedFolder = Path.Combine(scratch, "dns-cache-still-failing");
        await new DnsCacheRecoveryJournal(failedFolder).BeginAsync(selected, CancellationToken.None);
        var stillFailing = await DnsCacheReconciliation.RunAsync(failedFolder, (host, token) => Task.FromResult(Failed(host)));
        Require(stillFailing.Outcome == DnsCacheReconciliationOutcome.StillFailing && stillFailing.GetExitCode() == 3 &&
            File.Exists(stillFailing.ArchivePath), "Complete repeated failure is retained without claiming recovery or repeating mutation");

        string uncertainFolder = Path.Combine(scratch, "dns-cache-inconclusive");
        string uncertainPending = await new DnsCacheRecoveryJournal(uncertainFolder).BeginAsync(selected, CancellationToken.None);
        var inconclusive = await DnsCacheReconciliation.RunAsync(uncertainFolder,
            (host, token) => Task.FromResult(new DnsLookupResult(host, DnsLookupState.TimedOut, [], "fixture")));
        Require(inconclusive.Outcome == DnsCacheReconciliationOutcome.Inconclusive && File.Exists(uncertainPending) &&
            inconclusive.ArchivePath is null, "Incomplete reconciliation retains the pending guard");

        string corruptFolder = Path.Combine(scratch, "dns-cache-corrupt");
        Directory.CreateDirectory(corruptFolder);
        string corruptPending = Path.Combine(corruptFolder, "dns-cache.pending.json");
        await File.WriteAllTextAsync(corruptPending, "{\"schema\":1,\"schema\":2}");
        int corruptReads = 0;
        var corrupt = await DnsCacheReconciliation.RunAsync(corruptFolder, (host, token) =>
        { corruptReads++; return Task.FromResult(Failed(host)); });
        Require(corrupt.Outcome == DnsCacheReconciliationOutcome.Inconclusive && corruptReads == 0 && File.Exists(corruptPending),
            "Conflicting pending records remain guarded without resolving or flushing");

        string foreignFolder = Path.Combine(scratch, "dns-cache-foreign");
        string foreignPending = await new DnsCacheRecoveryJournal(foreignFolder).BeginAsync(selected, CancellationToken.None);
        File.WriteAllText(foreignPending, File.ReadAllText(foreignPending).Replace(Environment.MachineName, "OTHER-MACHINE"));
        int foreignReads = 0;
        var foreign = await DnsCacheReconciliation.RunAsync(foreignFolder, (host, token) =>
        { foreignReads++; return Task.FromResult(Failed(host)); });
        Require(foreign.Outcome == DnsCacheReconciliationOutcome.Inconclusive && foreignReads == 0 && File.Exists(foreignPending),
            "A foreign-machine DNS cache record remains guarded without resolving or flushing");

        var noTargetHost = new WindowsWorkflowHost { DnsRecoveryHost = new WorkflowHost() };
        var noTarget = await noTargetHost.CheckIntegrityAsync(RepairKind.DnsCache, null, CancellationToken.None);
        Require(noTarget.State == CheckState.Information && noTarget.Repair is null,
            "Network recipe remains read-only when no exact hostname was selected");

        var workflowDns = new WorkflowHost(Failed(), Failed(), Failed(),
            new("fixture.example", DnsLookupState.Resolved, ["192.0.2.20"], "fixture"));
        var workflowJournal = new Journal();
        var workflowHost = new WindowsWorkflowHost
        {
            NetworkLookupHost = "fixture.example",
            DnsRecoveryHost = workflowDns,
            DnsLookup = (host, timeout, token) => workflowDns.ResolveAsync(host, token),
            DnsRecoveryJournalFactory = _ => workflowJournal
        };
        var workflowCheck = await workflowHost.CheckIntegrityAsync(RepairKind.DnsCache, null, CancellationToken.None);
        Require(workflowCheck.State == CheckState.Repairable && workflowCheck.Repair == RepairKind.DnsCache,
            "Exact HostNotFound evidence selects the shared DNS cache recipe");
        var workflowRepair = await workflowHost.RepairAsync(RepairKind.DnsCache,
            Path.Combine(scratch, "dns-cache-workflow"), null, CancellationToken.None);
        Require(Repairs.IsVerified(workflowRepair) && workflowRepair.GetExitCode() == 0 &&
            workflowRepair.DnsCacheRecovery?.After?.Addresses.Single() == "192.0.2.20" && workflowDns.Reads == 4 && workflowDns.Flushes == 1,
            "Shared workflow rechecks, flushes and verifies the selected hostname with retained recovery evidence");
        Require(!Repairs.IsVerified(workflowRepair with { DnsCacheRecovery = null }) &&
            !Repairs.IsVerified(workflowRepair with { DnsCacheRecovery = workflowRepair.DnsCacheRecovery! with { Outcome = DnsCacheRecoveryOutcome.Failed } }) &&
            !Repairs.IsVerified(workflowRepair with { Steps = [workflowRepair.Steps[0] with { Step = new("ipconfig.exe", ["/all"], false) }] }),
            "Generic repair success requires the exact verified DNS recovery result and flush command evidence");

        string pendingWorkflowFolder = Path.Combine(scratch, "dns-cache-workflow-pending");
        string pendingDnsFolder = Path.Combine(pendingWorkflowFolder, "dns-cache");
        await new DnsCacheRecoveryJournal(pendingDnsFolder).BeginAsync(Failed(), CancellationToken.None);
        var guardedDns = new WorkflowHost(Failed(),
            new("fixture.example", DnsLookupState.Resolved, ["192.0.2.40"], "fixture"));
        var guardedWorkflow = new WindowsWorkflowHost
        {
            NetworkLookupHost = "fixture.example",
            DnsRecoveryHost = guardedDns,
            DnsLookup = (host, timeout, token) => guardedDns.ResolveAsync(host, token)
        };
        await guardedWorkflow.CheckIntegrityAsync(RepairKind.DnsCache, null, CancellationToken.None);
        var reconciledWorkflow = await guardedWorkflow.RepairAsync(RepairKind.DnsCache,
            pendingWorkflowFolder, null, CancellationToken.None);
        Require(Repairs.IsVerified(reconciledWorkflow) && reconciledWorkflow.Steps.Count == 0 &&
            reconciledWorkflow.DnsCacheRecovery is null &&
            reconciledWorkflow.DnsCacheReconciliation?.Outcome == DnsCacheReconciliationOutcome.Resolved &&
            guardedDns.Flushes == 0 && !File.Exists(Path.Combine(pendingDnsFolder, "dns-cache.pending.json")),
            "Pending shared workflow evidence is reconciled without repeating the DNS cache mutation");
        var reconciliationRecipe = RecipeCatalog.ForWorkflow(WorkflowKind.Network)
            .Single(recipe => recipe.Action == RepairKind.DnsCache);
        var reconciliationRecipes = await RecipeEngine.RunAsync([reconciliationRecipe with { Dependencies = [] }], true,
            Path.Combine(scratch, "dns-cache-recipe-reconciliation"), new ReconciliationHost(reconciledWorkflow));
        Require(reconciliationRecipes.Single().Outcome == RecipeOutcome.Repaired &&
            reconciliationRecipes.Single().Repair == reconciledWorkflow && !reconciliationRecipes.Single().ExecutionUncertain,
            "Recipe engine accepts a verified no-repeat reconciliation without fabricating command evidence");

        string unresolvedWorkflowFolder = Path.Combine(scratch, "dns-cache-workflow-unresolved");
        string unresolvedDnsFolder = Path.Combine(unresolvedWorkflowFolder, "dns-cache");
        await new DnsCacheRecoveryJournal(unresolvedDnsFolder).BeginAsync(Failed(), CancellationToken.None);
        var unresolvedDns = new WorkflowHost(Failed(), Failed());
        var unresolvedWorkflow = new WindowsWorkflowHost
        {
            NetworkLookupHost = "fixture.example",
            DnsRecoveryHost = unresolvedDns,
            DnsLookup = (host, timeout, token) => unresolvedDns.ResolveAsync(host, token)
        };
        await unresolvedWorkflow.CheckIntegrityAsync(RepairKind.DnsCache, null, CancellationToken.None);
        var unresolvedReview = await unresolvedWorkflow.RepairAsync(RepairKind.DnsCache,
            unresolvedWorkflowFolder, null, CancellationToken.None);
        Require(!Repairs.IsVerified(unresolvedReview) && unresolvedReview.GetExitCode() == 3 &&
            unresolvedReview.DnsCacheReconciliation?.Outcome == DnsCacheReconciliationOutcome.StillFailing &&
            unresolvedDns.Flushes == 0,
            "Conclusive repeated failure remains unresolved and never repeats the cache flush");
        Require(RecipeCatalog.ForWorkflow(WorkflowKind.Network).Single(recipe => recipe.Action == RepairKind.DnsCache)
            .Dependencies.SequenceEqual(DnsClientDependency),
            "Network workflow exposes DNS cache recovery after the DNS Client prerequisite");

        Console.WriteLine("PASS: hostname-bound DNS cache preview, repeated fresh eligibility, administrator gate, durable preparation, exact-target verification and interruption reconciliation; simulated flush only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }

    private sealed class Observer : IProgress<string>
    {
        public readonly List<string> Messages = [];
        public void Report(string value) => Messages.Add(value);
    }

    private sealed class Host : IDnsCacheRecoveryHost
    {
        public bool IsAdministrator { get; set; } = true;
        public DnsLookupResult Before { get; set; } = Failed();
        public DnsLookupResult Immediate { get; set; } = Failed();
        public DnsLookupResult After { get; set; } = Failed();
        public ToolResult Flush { get; set; } = new(0, "Successfully flushed the DNS Resolver Cache.", "", false);
        public int FailRead;
        public Exception? ReadFailure;
        public Exception? FlushFailure;
        public int Reads, Flushes;
        public Task<DnsLookupResult> ResolveAsync(string host, CancellationToken token)
        {
            Reads++;
            if (!string.Equals(host, "fixture.example", StringComparison.Ordinal)) throw new Exception("Wrong verification target.");
            if (Reads == FailRead && ReadFailure is not null) throw ReadFailure;
            return Task.FromResult(Reads switch { 1 => Before, 2 => Immediate, _ => After });
        }
        public Task<ToolResult> FlushAsync(CancellationToken token)
        {
            Flushes++;
            if (FlushFailure is not null) throw FlushFailure;
            return Task.FromResult(Flush);
        }
    }

    private sealed class Journal : IDnsCacheRecoveryJournal
    {
        public int Begins;
        public bool Deny;
        public DnsCacheRecoveryResult? Result;
        public Task<string> BeginAsync(DnsLookupResult before, CancellationToken token)
        {
            if (Deny) throw new IOException("fixture journal rejection");
            Begins++;
            return Task.FromResult("fixture.json");
        }
        public Task CompleteAsync(DnsCacheRecoveryResult result, CancellationToken token) { Result = result; return Task.CompletedTask; }
    }

    private sealed class WorkflowHost(params DnsLookupResult[] results) : IDnsCacheRecoveryHost
    {
        private readonly Queue<DnsLookupResult> results = new(results);
        public bool IsAdministrator => true;
        public int Reads, Flushes;
        public Task<DnsLookupResult> ResolveAsync(string host, CancellationToken token)
        {
            Reads++;
            if (results.Count == 0) throw new InvalidOperationException("No fixture lookup remains.");
            var result = results.Dequeue();
            if (!string.Equals(host, result.Host, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Wrong workflow verification target.");
            return Task.FromResult(result);
        }
        public Task<ToolResult> FlushAsync(CancellationToken token)
        {
            Flushes++;
            return Task.FromResult(new ToolResult(0, "Successfully flushed the DNS Resolver Cache.", "", false));
        }
    }

    private sealed class ReconciliationHost(RepairResult result) : IWorkflowHost
    {
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<WorkflowCheck>>([]);
        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token) =>
            Task.FromResult(new WorkflowCheck("DNS cache", CheckState.Repairable, "Fixture pending recovery", RepairKind.DnsCache));
        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token) =>
            Task.FromResult(result);
    }
}
