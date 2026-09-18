using KillerBench.Engine;
using System.IO;

internal static class UpdateExecutionChecks
{
    internal static async Task RunAsync(string scratch)
    {
        await NativeUpdateChecks.RunAsync();
        var host = new Fixture();
        var selection = new UpdateSelection(Guid.Parse(host.Update.Id), 1);
        foreach (var returnedScope in new[] { new UpdateSearchOptions(true, true), new UpdateSearchOptions(false, true) })
        {
            var changedScope = new Fixture { ReturnedScope = returnedScope };
            string scopeFolder = Path.Combine(scratch, "update-scope-" + Guid.NewGuid().ToString("N"));
            var rejected = await UpdateExecution.RunAsync(new(), [selection], new(), scopeFolder, changedScope);
            Require(rejected.Status == "Needs review" && rejected.GetExitCode() == 3 && changedScope.Calls == 0 &&
                rejected.Outcomes.Count == 0 && rejected.Details.Any(detail => detail.Contains("different software or driver scope")) &&
                !File.Exists(Path.Combine(scopeFolder, "pending.json")) && File.ReadAllText(rejected.JournalPath).Contains("Finished"),
                "Changed provider scope cannot reach installation or leave a prepared operation");
        }
        foreach (string stage in new[] { "search", "readiness" })
        {
            string timeoutFolder = Path.Combine(scratch, "update-prerequisite-timeout-" + stage);
            var hung = new Fixture { StalledStage = stage };
            var timed = await UpdateExecution.RunBoundedAsync(new(), [selection], new(), timeoutFolder, hung,
                TimeSpan.FromSeconds(1), prerequisiteLimit: TimeSpan.FromMilliseconds(25)).WaitAsync(TimeSpan.FromSeconds(3));
            Require(timed.Status == "Needs review" && hung.Calls == 0 && timed.Details.Any(d => d.Contains("timed out")) &&
                !File.Exists(Path.Combine(timeoutFolder, "pending.json")) && File.ReadAllText(timed.JournalPath).Contains("Finished"),
                "Prerequisite deadline prevents dispatch and records an explicit outcome");
            using var stopped = new CancellationTokenSource();
            var stalled = new Fixture { StalledStage = stage, Stop = stopped };
            string stalledFolder = Path.Combine(scratch, "update-prerequisite-" + stage);
            var stoppedResult = await UpdateExecution.RunAsync(new(), [selection], new(), stalledFolder, stalled, token: stopped.Token).WaitAsync(TimeSpan.FromSeconds(3));
            Require(stoppedResult.Status == "Stopped" && stalled.Calls == 0 && stoppedResult.Outcomes.Count == 0 &&
                !File.Exists(Path.Combine(stalledFolder, "pending.json")), "Stop during update prerequisites cannot dispatch installation or create a prepared marker");
            Require(File.ReadAllText(stoppedResult.JournalPath).Contains("Stopped"), "Stopped update prerequisites retain a finished journal");
        }
        foreach (string scenario in new[] { "readiness", "missing", "scope" })
        {
            string finalFolder = Path.Combine(scratch, "update-final-" + scenario);
            var finalHost = new Fixture
            {
                FinalBlocker = scenario == "readiness" ? "A restart became pending." : null,
                FinalSearchMissing = scenario == "missing",
                FinalReturnedScope = scenario == "scope" ? new UpdateSearchOptions(false, true) : null
            };
            var blocked = await UpdateExecution.RunAsync(new(), [selection], new(), finalFolder, finalHost);
            Require(blocked.Status == "Blocked" && blocked.GetExitCode() == 3 && finalHost.Calls == 0 &&
                !File.Exists(Path.Combine(finalFolder, "pending.json")) &&
                Directory.GetFiles(finalFolder, "*.prepared.json").Length == 1,
                "Changed final update eligibility reached installation or lost prepared evidence: " + scenario);
            Require(File.ReadAllText(blocked.JournalPath).Contains(scenario == "missing" ? "Error" : "FinalEligibility"),
                "Changed final update eligibility was not journaled: " + scenario);
        }
        string payload = "{\"id\":\"" + selection.Id + "\",\"revision\":1,\"code\":2,\"itemCode\":2,\"hresult\":0,\"restart\":false,\"verified\":true}";
        var native = new ToolResult(0, payload, "", false);
        Require(WindowsUpdateExecutionHost.Interpret(native, selection).Installed, "Native result requires installed-state verification");
        foreach (string invalid in new[] { payload.Replace("\"verified\":true", "\"verified\":false"), payload.Replace("\"itemCode\":2", "\"itemCode\":3"), payload.Replace("\"revision\":1", "\"revision\":2"), "{}" })
            Require(!WindowsUpdateExecutionHost.Interpret(native with { Output = invalid }, selection).Installed, "Failed or mismatched native evidence cannot verify installation");
        Require(!WindowsUpdateExecutionHost.Interpret(native with { TimedOut = true }, selection).Installed, "Timeout overrides payload success");
        var step = WindowsUpdateExecutionHost.InstallationStep(new(), selection, new());
        string encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(step.Arguments[^1]));
        var syntax = new RepairStep(step.Tool, ["-NoProfile", "-NonInteractive", "-Command", "$text=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('" + encoded + "')); $tokens=$null; $errors=$null; [void][System.Management.Automation.Language.Parser]::ParseInput($text,[ref]$tokens,[ref]$errors); if($errors.Count){throw ($errors | Out-String)}"], true);
        var syntaxResult = await DefenderRecovery.ExecuteAsync(syntax);
        Require(syntaxResult.ExitCode == 0, "Installation collector parses without executing COM or mutations");
        string folder = Path.Combine(scratch, "update-execution");
        var result = await UpdateExecution.RunAsync(new(), [selection], new(), folder, host);
        Require(result.Status == "Completed" && result.GetExitCode() == 0 && result.Outcomes.Count == 1 && !File.Exists(Path.Combine(folder, "pending.json")), "Verified update clears pending record");
        Require(result.Format().Contains("NEXT: Selected updates verified"), "Complete verified update result offers success guidance");
        foreach (var incomplete in new[] { result with { SelectedCount = 2 }, result with { Outcomes = [] },
            result with { Outcomes = [result.Outcomes[0] with { Installed = false }] },
            result with { SelectedCount = 2, Outcomes = [result.Outcomes[0], result.Outcomes[0]] },
            result with { Outcomes = [result.Outcomes[0] with { RestartRequired = true }] } })
            Require(incomplete.Format().StartsWith("UPDATE INSTALLATION / Verification incomplete") &&
                !incomplete.Format().Contains("NEXT: Selected updates verified"),
                "Completed label cannot hide missing, duplicate, unverified or restart-pending update outcomes");
        Require((result with { SelectedCount = 2 }).GetExitCode() == 3 &&
            (result with { Outcomes = [] }).GetExitCode() == 3 &&
            (result with { Outcomes = [result.Outcomes[0] with { Installed = false }] }).GetExitCode() == 3 &&
            (result with { Status = "Stopped" }).GetExitCode() == 130 &&
            (result with { Status = "Unknown future status" }).GetExitCode() == 3,
            "CLI result mapping rejects incomplete, unverified and unknown outcomes");
        host.Restart = true;
        var microsoftSource = new UpdateSearchOptions(Source: UpdateSearchSource.MicrosoftUpdate);
        result = await UpdateExecution.RunAsync(microsoftSource, [selection], new(true), folder, host);
        Require(result.Status == "Restart required" && result.GetExitCode() == 4 && File.Exists(Path.Combine(folder, "pending.json")) &&
            host.InstallationSource == microsoftSource, "Restart retains source, reconciliation guard and distinct CLI outcome");
        int calls = host.Calls;
        try { await UpdateExecution.RunAsync(new(), [selection], new(true), folder, host); throw new Exception("Pending operation allowed retry"); }
        catch (InvalidOperationException) { }
        Require(host.Calls == calls, "Pending operation prevents another mutation");
        var unverifiedHost = new RecoveryFixture(false);
        var unverifiedRecovery = await UpdateReconciliation.ResolveAsync(folder, unverifiedHost);
        Require(!unverifiedRecovery.Resolved && unverifiedRecovery.Detail.Contains("not verified installed and ready") &&
            unverifiedHost.Source == microsoftSource, "Unverified recovery retains source and explains the retained guard");
        Require(File.Exists(Path.Combine(folder, "pending.json")), "Unverified recovery cannot clear pending record");
        string pendingPath = Path.Combine(folder, "pending.json");
        byte[] retainedPending = File.ReadAllBytes(pendingPath);
        var stalledRecovery = new StalledRecovery();
        var timedRecovery = await UpdateReconciliation.ResolveAsync(folder, stalledRecovery, verificationLimit: TimeSpan.FromMilliseconds(25))
            .WaitAsync(TimeSpan.FromSeconds(3));
        Require(!timedRecovery.Resolved && timedRecovery.Detail.Contains("timed out"), "Stalled recovery explains timeout without clearing the guard");
        stalledRecovery.Completion.SetResult(true);
        Require(File.ReadAllBytes(pendingPath).SequenceEqual(retainedPending), "Late recovery completion cannot clear pending evidence");
        using (var recoveryStop = new CancellationTokenSource(TimeSpan.FromMilliseconds(25)))
        {
            try
            {
                await UpdateReconciliation.TryResolveAsync(folder, new StalledRecovery(), recoveryStop.Token).WaitAsync(TimeSpan.FromSeconds(3));
                throw new Exception("Stalled recovery ignored cancellation");
            }
            catch (OperationCanceledException) { }
        }
        Require(File.ReadAllBytes(pendingPath).SequenceEqual(retainedPending), "Canceled recovery preserves pending evidence");
        var lockedRecovery = new StalledRecovery();
        var resolving = UpdateReconciliation.TryResolveAsync(folder, lockedRecovery);
        await lockedRecovery.Entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        try
        {
            using var competingWrite = new FileStream(pendingPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            throw new Exception("Pending recovery evidence allowed concurrent writing");
        }
        catch (IOException) { }
        try { File.Move(pendingPath, pendingPath + ".moved"); throw new Exception("Pending recovery evidence allowed concurrent replacement"); }
        catch (IOException) { }
        lockedRecovery.Completion.SetResult(true);
        Require(await resolving.WaitAsync(TimeSpan.FromSeconds(3)), "Verified installed state resolves guard");
        Require(!File.Exists(Path.Combine(folder, "pending.json")) && Directory.GetFiles(folder, "resolved-installed-*.json").Length == 1, "Recovery retains original prepared evidence");
        string recoveryFolder = Path.Combine(scratch, "recovery-invalid");
        Directory.CreateDirectory(recoveryFolder);
        string marker = Path.Combine(recoveryFolder, "pending.json");
        string target = "{\"Id\":\"" + selection.Id + "\",\"Revision\":1}";
        foreach (string corrupt in new[] { "{", "[]", "{}", "{\"Target\":null}",
            "{\"Target\":" + target + ",\"Target\":" + target + "}",
            "{\"Target\":{\"Id\":\"" + selection.Id + "\",\"Revision\":1,\"Revision\":2}}",
            new string(' ', 16385) })
        {
            File.WriteAllText(marker, corrupt);
            var recoveryHost = new RecoveryFixture(true);
            var invalidRecovery = await UpdateReconciliation.ResolveAsync(recoveryFolder, recoveryHost);
            Require(!invalidRecovery.Resolved && invalidRecovery.Detail.Contains("invalid, ambiguous or oversized"), "Corrupt recovery explains rejected evidence");
            Require(recoveryHost.Calls == 0 && File.ReadAllText(marker) == corrupt, "Rejected recovery does not query or alter evidence");
        }
        string foreignRecord = "{\"Machine\":\"OTHER-MACHINE\",\"Search\":{\"Software\":true,\"Drivers\":false,\"Source\":0},\"Target\":" + target + "}";
        File.WriteAllText(marker, foreignRecord);
        var foreignRecoveryHost = new RecoveryFixture(true);
        var foreignRecovery = await UpdateReconciliation.ResolveAsync(recoveryFolder, foreignRecoveryHost);
        Require(!foreignRecovery.Resolved && foreignRecoveryHost.Calls == 0 && File.ReadAllText(marker) == foreignRecord,
            "A foreign-machine update record remains guarded without inspecting or reinstalling the update");
        File.WriteAllText(marker, "{\"Search\":{\"Software\":true,\"Drivers\":false,\"Source\":0},\"Target\":" + target + "}");
        using (var canceled = new CancellationTokenSource())
        {
            canceled.Cancel();
            try { await UpdateReconciliation.TryResolveAsync(recoveryFolder, new RecoveryFixture(true), canceled.Token); throw new Exception("Canceled recovery completed"); }
            catch (OperationCanceledException) { }
            Require(File.Exists(marker), "Canceled recovery retains guard");
        }
        host.Restart = false; host.Blocker = "Pending machine restart";
        result = await UpdateExecution.RunAsync(new(), [selection], new(), Path.Combine(scratch, "update-blocked"), host);
        Require(result.Status == "Blocked" && host.Calls == calls, "Readiness failure prevents installation");
        Require(result.Format().Contains("Pending machine restart") && result.SelectedCount == 1 && result.Details.Count == 1, "Readiness reason reaches shared report and structured result");
        var displayHost = new Fixture();
        var displayResult = await UpdateExecution.RunAsync(new(), [selection], new(), Path.Combine(scratch, "update-display-failure"), displayHost, new BrokenDisplay());
        Require(displayResult.GetExitCode() == 0 && displayHost.Calls == 1 && File.ReadAllText(displayResult.JournalPath).Contains("Finished"),
            "Display exceptions cannot interrupt verification and final outcome recording");
        foreach (string failure in new[] { "timeout", "provider-canceled", "wrong-identity", "unverified", "user-canceled", "invalid-data", "unsupported", "security", "native-error" })
        {
            string failedFolder = Path.Combine(scratch, "update-" + failure);
            using var stop = new CancellationTokenSource();
            host = new Fixture { Failure = failure, Folder = failedFolder, Stop = stop };
            result = await UpdateExecution.RunAsync(new(), [selection], new(), failedFolder, host, token: stop.Token);
            Require(result.Status == (failure == "user-canceled" ? "Stopped" : "Needs review"), "Failure status is explicit: " + failure);
            Require(File.Exists(Path.Combine(failedFolder, "pending.json")), "Uncertain operation keeps pending marker: " + failure);
            Require(File.ReadAllText(result.JournalPath).Contains("Finished"), "Failure journals include a final outcome: " + failure);
            Require(result.Outcomes.All(o => !o.Installed), "Failure never becomes installed: " + failure);
        }
        string hungFolder = Path.Combine(scratch, "update-hung-provider");
        host = new Fixture { Failure = "never-completes" };
        result = await UpdateExecution.RunBoundedAsync(new(), [selection], new(), hungFolder, host, TimeSpan.FromMilliseconds(25));
        Require(result.Status == "Needs review" && result.Outcomes.Count == 0 && File.Exists(Path.Combine(hungFolder, "pending.json")),
            "Nonresponsive provider is bounded and preserves uncertain-operation guard");
        var whileRunning = new RecoveryFixture(true);
        try { await UpdateReconciliation.TryResolveAsync(hungFolder, whileRunning); throw new Exception("Active provider allowed recovery"); }
        catch (IOException error) { Require(error.Message.Contains("provider is still active") && error.InnerException is IOException, "Busy recovery provides actionable context and retains native error"); }
        Require(whileRunning.Calls == 0 && File.Exists(Path.Combine(hungFolder, "pending.json")), "Active provider blocks reconciliation before verification");
        host.Pending.SetResult(new(selection, true, false, "Late completion"));
        Require(await UpdateReconciliation.TryResolveAsync(hungFolder, new RecoveryFixture(true)), "Completed provider releases recovery lease");
        Console.WriteLine("PASS: update execution journals, verification outcomes, restart guard and readiness (substituted provider only).");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private sealed class RecoveryFixture(bool installed) : IUpdateReconciliationHost
    {
        internal int Calls;
        internal UpdateSearchOptions? Source;
        public Task<bool> IsInstalledAndReadyAsync(UpdateSearchOptions search, UpdateSelection selection, CancellationToken token) { Calls++; Source = search; return Task.FromResult(installed); }
    }
    private sealed class StalledRecovery : IUpdateReconciliationHost
    {
        internal readonly TaskCompletionSource<bool> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource<bool> Entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<bool> IsInstalledAndReadyAsync(UpdateSearchOptions search, UpdateSelection selection, CancellationToken token)
        { Entered.TrySetResult(true); return Completion.Task; }
    }
    private sealed class BrokenDisplay : IProgress<string>
    {
        public void Report(string value) => throw new InvalidOperationException("Fixture display failure");
    }
    private sealed class Fixture : IUpdateExecutionHost
    {
        internal AvailableUpdate Update = new("74ba239e-08d2-45bd-b285-b0d35610ca60", 1, "Fixture", "Software", true, true)
        { Behavior = new(false, 0, 0, false, [new("0fa1201d-4330-4fa8-8ae9-b877473b6441", "Fixture")]) };
        internal int Calls;
        internal bool Restart;
        internal TaskCompletionSource<UpdateExecutionOutcome> Pending = new();
        internal string? Blocker;
        internal string? FinalBlocker;
        internal string? StalledStage;
        internal UpdateSearchOptions? ReturnedScope;
        internal UpdateSearchOptions? FinalReturnedScope;
        internal bool FinalSearchMissing;
        internal string? Failure;
        internal string? Folder;
        internal CancellationTokenSource? Stop;
        internal UpdateSearchOptions? InstallationSource;
        private int readinessReads;
        private int searchReads;
        public Task<string?> ReadinessAsync(CancellationToken token)
        {
            readinessReads++;
            if (StalledStage == "readiness") { Stop?.Cancel(); return new TaskCompletionSource<string?>().Task; }
            return Task.FromResult(readinessReads > 1 ? FinalBlocker ?? Blocker : Blocker);
        }
        public Task<UpdateSearchResult> SearchAsync(UpdateSearchOptions options, CancellationToken token)
        {
            searchReads++;
            if (StalledStage == "search") { Stop?.Cancel(); return new TaskCompletionSource<UpdateSearchResult>().Task; }
            var returnedOptions = searchReads > 1 ? FinalReturnedScope ?? ReturnedScope ?? options : ReturnedScope ?? options;
            IReadOnlyList<AvailableUpdate> updates = FinalSearchMissing && searchReads > 1 ? [] : [Update];
            return Task.FromResult(new UpdateSearchResult(returnedOptions, updates, false, "Fixture"));
        }
        public Task<UpdateExecutionOutcome> InstallAndVerifyAsync(UpdateSearchOptions search, UpdateSelection selection, UpdateInstallOptions options, CancellationToken token)
        {
            Calls++;
            InstallationSource = search;
            if (Failure == "never-completes") return Pending.Task;
            if (Folder is not null)
            {
                Require(File.Exists(Path.Combine(Folder, "pending.json")), "Durable marker precedes provider execution");
                using var journal = new FileStream(Directory.GetFiles(Folder, "*.jsonl").Single(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(journal);
                Require(reader.ReadToEnd().Contains("Prepared"), "Prepared journal precedes provider execution");
            }
            if (Failure == "timeout") throw new TimeoutException("Fixture timeout");
            if (Failure == "invalid-data") throw new InvalidDataException("Fixture invalid data");
            if (Failure == "unsupported") throw new NotSupportedException("Fixture unsupported provider");
            if (Failure == "security") throw new System.Security.SecurityException("Fixture security failure");
            if (Failure == "native-error") throw new System.ComponentModel.Win32Exception("Fixture native failure");
            if (Failure == "provider-canceled") throw new OperationCanceledException("Fixture provider stopped");
            if (Failure == "user-canceled") { Stop!.Cancel(); throw new OperationCanceledException(token); }
            if (Failure == "wrong-identity") selection = selection with { Revision = selection.Revision + 1 };
            return Task.FromResult(new UpdateExecutionOutcome(selection, Failure != "unverified", Restart, "Fixture verification"));
        }
    }
}
