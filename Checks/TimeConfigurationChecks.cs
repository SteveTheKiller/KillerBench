using KillerBench.Engine;
using System.IO;
using System.Text.Json;

internal static class TimeConfigurationChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string recoveryFolder = Path.Combine(scratch, "time-invalid-records-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(recoveryFolder);
        string recoveryPending = Path.Combine(recoveryFolder, "pending-time.json");
        var noPending = await TimeConfigurationRecovery.ReconcileAsync(Path.Combine(scratch, "time-no-pending-" + Guid.NewGuid().ToString("N")));
        if (noPending.Outcome != TimeConfigurationRecoveryOutcome.NoPendingRecord || !noPending.Resolved || noPending.GetExitCode() != 0)
            throw new Exception("Absent time recovery record did not report an exact no-pending outcome");
        string ValidRecord(string folder, TimeOptions options, string? machine = null)
        {
            var before = options.Source == TimeSourceMode.DomainHierarchy
                ? new TimeEnvironment(true, false, false, true) { LocalMode = "NT5DS", LocalPeers = "" }
                : new TimeEnvironment(false, false, false, true) { LocalMode = "NTP", LocalPeers = string.Join(" ", options.Peers!.Select(peer => peer + ",0x8")) };
            return JsonSerializer.Serialize(new
            {
                Schema = 1,
                Machine = machine ?? Environment.MachineName,
                StartedUtc = DateTimeOffset.UtcNow,
                Options = options,
                Before = before,
                Journal = Path.Combine(folder, "time-fixture.jsonl")
            }, Capture.JsonOptions);
        }
        foreach (string invalid in new[] { "{", "null", "[]", "{}",
            "{\"Options\":null}", "{\"Options\":{},\"Options\":{}}",
            "{\"Options\":{\"Source\":2,\"Source\":2,\"Peers\":[\"time.example.test\"]}}",
            "{\"Options\":{\"Source\":99,\"Peers\":[\"time.example.test\"]}}",
            "{\"Options\":{\"Source\":2,\"Peers\":[\"bad peer\"]}}", new string(' ', 65537) })
        {
            File.WriteAllText(recoveryPending, invalid);
            bool called = false;
            var invalidResult = await TimeConfigurationRecovery.ReconcileAsync(recoveryFolder, verify: (_, _) => { called = true; return Task.FromResult(true); });
            if (invalidResult.Outcome != TimeConfigurationRecoveryOutcome.InvalidRecord || invalidResult.Resolved || invalidResult.GetExitCode() != 3 || called ||
                File.ReadAllText(recoveryPending) != invalid || Directory.GetFiles(recoveryFolder, "resolved-time-*.json").Length != 0)
                throw new Exception("Malformed time recovery record was verified, archived or changed");
        }
        string validRecord = ValidRecord(recoveryFolder, new(TimeSourceMode.CustomPeers, ["time.example.test"]));
        File.WriteAllText(recoveryPending, validRecord);
        using (var ignoredStop = new CancellationTokenSource())
        {
            var neverCompletes = new TaskCompletionSource<bool>();
            var pendingCheck = TimeConfigurationRecovery.TryResolveAsync(recoveryFolder, ignoredStop.Token,
                (_, _) => neverCompletes.Task);
            ignoredStop.Cancel();
            try { await pendingCheck.WaitAsync(TimeSpan.FromSeconds(2)); throw new Exception("Uncooperative recovery ignored cancellation"); }
            catch (OperationCanceledException) { }
            if (File.ReadAllText(recoveryPending) != validRecord) throw new Exception("Uncooperative recovery lost evidence");
            neverCompletes.SetResult(true);
            if (!File.Exists(recoveryPending)) throw new Exception("Late verification archived canceled evidence");
        }
        string foreignRecord = ValidRecord(recoveryFolder, new(TimeSourceMode.CustomPeers, ["time.example.test"]), "OTHER-MACHINE");
        File.WriteAllText(recoveryPending, foreignRecord);
        bool foreignCalled = false;
        if (await TimeConfigurationRecovery.TryResolveAsync(recoveryFolder,
                verify: (_, _) => { foreignCalled = true; return Task.FromResult(true); }) || foreignCalled ||
            File.ReadAllText(recoveryPending) != foreignRecord)
            throw new Exception("Foreign-machine time recovery record was verified, archived or changed");
        File.WriteAllText(recoveryPending, validRecord);
        using (var recoveryStop = new CancellationTokenSource())
        {
            try
            {
                await TimeConfigurationRecovery.TryResolveAsync(recoveryFolder, recoveryStop.Token,
                    (_, _) => { recoveryStop.Cancel(); return Task.FromResult(true); });
                throw new Exception("Canceled recovery archived evidence");
            }
            catch (OperationCanceledException) { }
            if (File.ReadAllText(recoveryPending) != validRecord) throw new Exception("Canceled recovery changed pending evidence");
        }
        foreach (string outcome in new[] { "success", "failure", "timeout", "truncated", "stderr", "resync-failure" })
        {
            bool fail = outcome != "success";
            int expectedCommands = outcome is "success" or "resync-failure" ? 2 : 1;
            var commands = new List<RepairStep>();
            var commandHost = NativeTimeConfiguration.CreateHost(inspect: _ => Task.FromResult(new TimeEnvironment(false, false, false, true) { LocalMode = "NTP" }),
                execute: (step, _) =>
                {
                    commands.Add(step);
                    int code = outcome == "failure" || (outcome == "resync-failure" && commands.Count == 2) ? 5 : 0;
                    return Task.FromResult(new ToolResult(code, "Command completed", outcome == "stderr" ? "Configuration was not applied" : "", outcome == "truncated") { TimedOut = outcome == "timeout" });
                });
            try
            {
                await commandHost.ApplyAsync(new(TimeSourceMode.CustomPeers, ["time.example.test"]), default);
                if (fail) throw new Exception("Failed configuration continued");
            }
            catch (InvalidOperationException) when (fail) { }
            if (commands.Count != expectedCommands || commands[0].Arguments[0] != "/config" ||
                (expectedCommands == 2 && !commands[1].Arguments.SequenceEqual(new[] { "/resync" }))) throw new Exception("Native time command sequence mismatch: " + outcome);
        }
        try { await NativeTimeConfiguration.RunCustomAsync(new(TimeSourceMode.DomainHierarchy), scratch); throw new Exception("Unsupported native domain change accepted"); }
        catch (ArgumentException) { }
        var domainCommands = new List<RepairStep>();
        var domainCommandHost = NativeTimeConfiguration.CreateHost(
            inspect: _ => Task.FromResult(new TimeEnvironment(true, false, false, true) { LocalMode = "NTP", LocalPeers = "old.example.test,0x8" }),
            execute: (step, _) =>
            {
                domainCommands.Add(step);
                return Task.FromResult(new ToolResult(0, "Command completed", "", false));
            });
        await domainCommandHost.ApplyAsync(new(TimeSourceMode.DomainHierarchy), default);
        if (domainCommands.Count != 2 ||
            !domainCommands[0].Arguments.SequenceEqual(new[] { "/config", "/syncfromflags:domhier", "/update" }) ||
            !domainCommands[1].Arguments.SequenceEqual(new[] { "/resync", "/rediscover" }))
            throw new Exception("Native domain time command sequence changed scope");
        var selected = new TimeOptions(TimeSourceMode.CustomPeers, ["time.example.test", "192.0.2.1"]);
        var stored = new TimeEnvironment(false, false, false, true) { LocalMode = "NTP", LocalPeers = "192.0.2.1,0x8 TIME.EXAMPLE.TEST,0x8" };
        var selectedPlan = TimePlanning.Create(selected, stored);
        var selectedCheck = TimeConfigurationDiagnostics.AssessSelection(selected, stored, selectedPlan);
        if (selectedCheck.State != CheckState.Repairable || selectedCheck.Repair != RepairKind.TimeSourceConfiguration)
            throw new Exception("Eligible explicit time source did not select guarded configuration");
        var selectedReview = TimeConfigurationDiagnostics.ReviewSelection(selected, stored, selectedPlan);
        if (selectedReview.State != CheckState.Attention || selectedReview.Repair is not null ||
            !selectedReview.Detail.Contains("does not authorize", StringComparison.Ordinal))
            throw new Exception("Read-only time source review exposed repair authority or lost its execution boundary");
        var existingOptions = new TimeOptions();
        var existingPlan = TimePlanning.Create(existingOptions, stored);
        if (TimeConfigurationDiagnostics.ReviewSelection(existingOptions, stored, existingPlan) is not { State: CheckState.Information, Repair: null })
            throw new Exception("Existing time source selected a configuration change");
        var managed = stored with { PolicyManaged = true };
        if (TimeConfigurationDiagnostics.ReviewSelection(selected, managed, TimePlanning.Create(selected, managed)) is not { State: CheckState.Attention, Repair: null })
            throw new Exception("Managed time policy did not block source configuration");
        var unknownPolicy = stored with { PolicyManaged = null };
        if (TimeConfigurationDiagnostics.ReviewSelection(selected, unknownPolicy, TimePlanning.Create(selected, unknownPolicy)) is not { State: CheckState.Unknown, Repair: null })
            throw new Exception("Unknown time policy ownership did not keep source eligibility unknown");
        try
        {
            TimeConfigurationDiagnostics.AssessSelection(selected, stored, TimePlanning.Create(new(TimeSourceMode.CustomPeers, ["different.example.test"]), stored));
            throw new Exception("Mismatched time-source plan was accepted");
        }
        catch (ArgumentException) { }
        if (!TimeConfiguration.MatchesStoredConfiguration(selected, stored)) throw new Exception("Matching stored time peers rejected");
        var synchronized = new ToolResult(0, "Leap Indicator: 0\nStratum: 3\nSource: time.example.test\nLast Sync Error: 0", "", false);
        if (!await TimeConfiguration.VerifyCustomAsync(selected, inspect: _ => Task.FromResult(stored), queryStatus: _ => Task.FromResult(synchronized)))
            throw new Exception("Fresh custom verification failed");
        int inspection = 0;
        if (await TimeConfiguration.VerifyCustomAsync(selected, inspect: _ => Task.FromResult(++inspection == 1 ? stored : stored with { PolicyManaged = true }), queryStatus: _ => Task.FromResult(synchronized)))
            throw new Exception("Changed policy verified after status collection");
        if (!TimeConfiguration.MatchesCustomSynchronization(selected, stored, synchronized)) throw new Exception("Matching custom synchronization rejected");
        var domainStored = new TimeEnvironment(true, false, false, true) { LocalMode = "NT5DS", LocalPeers = "" };
        var domainOptions = new TimeOptions(TimeSourceMode.DomainHierarchy);
        if (!TimeConfiguration.MatchesDomainSynchronization(domainOptions, domainStored, synchronized) ||
            !await TimeConfiguration.VerifyAsync(domainOptions, inspect: _ => Task.FromResult(domainStored), queryStatus: _ => Task.FromResult(synchronized)))
            throw new Exception("Matching domain hierarchy synchronization rejected");
        foreach (var mismatch in new[] { domainStored with { DomainJoined = false }, domainStored with { DomainController = true },
            domainStored with { PolicyManaged = true }, domainStored with { PolicyManaged = null }, domainStored with { LocalMode = "NTP" } })
            if (TimeConfiguration.MatchesDomainSynchronization(domainOptions, mismatch, synchronized)) throw new Exception("Unsafe domain hierarchy state verified");
        if (TimeConfiguration.MatchesDomainSynchronization(domainOptions, domainStored, synchronized with { Output = synchronized.Output.Replace("Leap Indicator: 0", "Leap Indicator: 3") }))
            throw new Exception("Unsynchronized domain hierarchy state verified");
        foreach (var unknown in new[] { synchronized with { TimedOut = true }, synchronized with { Truncated = true },
            synchronized with { Output = synchronized.Output.Replace("time.example.test", "unselected.example.test") },
            synchronized with { Output = synchronized.Output.Replace("Leap Indicator: 0", "Leap Indicator: 3") } })
            if (TimeConfiguration.MatchesCustomSynchronization(selected, stored, unknown)) throw new Exception("Unknown or unselected time source verified");
        foreach (var mismatch in new[] { stored with { LocalPeers = "time.example.test,0x8" }, stored with { LocalMode = "NT5DS" },
            stored with { LocalPeers = "192.0.2.1,0x9 time.example.test,0x8" }, stored with { PolicyManaged = null },
            stored with { LocalPeers = "time.example.test,0x8 time.example.test,0x8" } })
            if (TimeConfiguration.MatchesStoredConfiguration(selected, mismatch)) throw new Exception("Different or unknown time configuration accepted");
        var custom = TimeConfiguration.ConfigurationStep(new(TimeSourceMode.CustomPeers, ["time.example.test", "192.0.2.1"]));
        if (custom.Tool != "w32tm.exe" || !custom.Arguments.SequenceEqual(new[] { "/config", "/manualpeerlist:time.example.test,0x8 192.0.2.1,0x8", "/syncfromflags:manual", "/update" }))
            throw new Exception("Custom time command changed selected peers or configuration scope");
        var domain = TimeConfiguration.ConfigurationStep(new(TimeSourceMode.DomainHierarchy));
        if (!domain.Arguments.SequenceEqual(new[] { "/config", "/syncfromflags:domhier", "/update" }))
            throw new Exception("Domain time command changed unrelated settings");
        foreach (string scenario in new[] { "changed", "canceled" })
        {
            using var stop = new CancellationTokenSource();
            string folder = Path.Combine(scratch, "time-" + scenario + "-" + Guid.NewGuid().ToString("N"));
            var host = new Fixture(folder, true) { ChangeOnSecondRead = scenario == "changed", AfterApply = scenario == "canceled" ? stop.Cancel : null };
            var result = await TimeConfiguration.RunAsync(new(TimeSourceMode.CustomPeers, ["time.example.test"]), folder, host, stop.Token);
            if (result.Status != (scenario == "changed" ? "Blocked" : "Stopped") || host.Applied != (scenario == "changed" ? 0 : 1) ||
                File.Exists(Path.Combine(folder, "pending-time.json")) != (scenario == "canceled"))
                throw new Exception("Time interruption or changed prerequisites lost guard state");
        }
        foreach (string scenario in new[] { "final-changed", "final-unavailable" })
        {
            string folder = Path.Combine(scratch, "time-" + scenario + "-" + Guid.NewGuid().ToString("N"));
            var host = new Fixture(folder, true)
            {
                ChangeOnThirdRead = scenario == "final-changed",
                FailOnThirdRead = scenario == "final-unavailable"
            };
            var result = await TimeConfiguration.RunAsync(new(TimeSourceMode.CustomPeers, ["time.example.test"]), folder, host);
            if (result.Status != "Blocked" || host.Applied != 0 || File.Exists(Path.Combine(folder, "pending-time.json")) ||
                Directory.GetFiles(folder, "*.prepared.json").Length != 1)
                throw new Exception("Final time eligibility reached configuration or lost prepared evidence: " + scenario);
            string journal = File.ReadAllText(result.Journal);
            if (!journal.Contains(scenario == "final-changed" ? "Final eligibility blocked" : "Final eligibility unavailable; no change requested"))
                throw new Exception("Final time eligibility outcome was not journaled: " + scenario);
        }
        foreach (bool verify in new[] { true, false })
        {
            string folder = Path.Combine(scratch, "time-config-" + Guid.NewGuid().ToString("N"));
            var host = new Fixture(folder, verify);
            var options = new TimeOptions(TimeSourceMode.CustomPeers, ["time.example.test"]);
            var result = await TimeConfiguration.RunAsync(options, folder, host);
            if (result.Before?.LocalPeers != "old.example.test,0x8" || result.After?.LocalPeers != "time.example.test,0x8" || string.IsNullOrWhiteSpace(result.Detail))
                throw new Exception("Time result lost before/after evidence or outcome explanation");
            string journalText = File.ReadAllText(result.Journal);
            if (!journalText.Contains("old.example.test,0x8") || !journalText.Contains("time.example.test,0x8"))
                throw new Exception("Time journal lost configuration snapshots");
            if (result.Status != (verify ? "Verified" : "Needs review") || host.Applied != 1 ||
                File.Exists(Path.Combine(folder, "pending-time.json")) == verify) throw new Exception("Time configuration outcome mismatch");
            if (!verify)
            {
                try { await TimeConfiguration.RunAsync(options, folder, host); throw new Exception("Unresolved time change retried"); }
                catch (InvalidOperationException) { }
                if (host.Applied != 1) throw new Exception("Time guard allowed another change");
                string pending = Path.Combine(folder, "pending-time.json");
                string original = File.ReadAllText(pending);
                var unresolved = await TimeConfigurationRecovery.ReconcileAsync(folder, verify: (_, _) => Task.FromResult(false));
                if (unresolved.Outcome != TimeConfigurationRecoveryOutcome.StillUnverified || unresolved.Resolved ||
                    unresolved.Options?.Source != TimeSourceMode.CustomPeers || File.ReadAllText(pending) != original)
                    throw new Exception("Unverified time recovery changed evidence");
                var resolved = await TimeConfigurationRecovery.ReconcileAsync(folder,
                    verify: (recorded, _) => Task.FromResult(recorded.Peers!.SequenceEqual(options.Peers!)));
                if (resolved.Outcome != TimeConfigurationRecoveryOutcome.Verified || !resolved.Resolved ||
                    resolved.ArchivedPath is null || host.Applied != 1)
                    throw new Exception("Time recovery failed or repeated mutation");
                if (File.Exists(pending) || File.ReadAllText(Directory.GetFiles(folder, "resolved-time-*.json").Single()) != original)
                    throw new Exception("Time recovery lost its original evidence");
            }
        }
        string domainRecoveryFolder = Path.Combine(scratch, "time-domain-recovery-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(domainRecoveryFolder);
        string domainPending = Path.Combine(domainRecoveryFolder, "pending-time.json");
        File.WriteAllText(domainPending, ValidRecord(domainRecoveryFolder, new(TimeSourceMode.DomainHierarchy)));
        if (!await TimeConfigurationRecovery.TryResolveAsync(domainRecoveryFolder,
                verify: (recorded, _) => Task.FromResult(recorded.Source == TimeSourceMode.DomainHierarchy)) ||
            File.Exists(domainPending) || Directory.GetFiles(domainRecoveryFolder, "resolved-time-*.json").Length != 1)
            throw new Exception("Domain hierarchy recovery was rejected or repeated a mutation");
        string uncertainFolder = Path.Combine(scratch, "time-command-error-" + Guid.NewGuid().ToString("N"));
        int uncertainCommands = 0;
        var uncertainHost = NativeTimeConfiguration.CreateHost(
            inspect: _ => Task.FromResult(new TimeEnvironment(false, false, false, true) { LocalMode = "NTP", LocalPeers = "old.example.test,0x8" }),
            execute: (_, _) =>
            {
                if (!File.Exists(Path.Combine(uncertainFolder, "pending-time.json"))) throw new Exception("Command ran before durable guard");
                uncertainCommands++;
                return Task.FromResult(new ToolResult(0, "", "Configuration incomplete", false));
            });
        var uncertainOptions = new TimeOptions(TimeSourceMode.CustomPeers, ["time.example.test"]);
        var uncertain = await TimeConfiguration.RunAsync(uncertainOptions, uncertainFolder, uncertainHost);
        if (uncertain.Status == "Verified" || uncertainCommands != 1 ||
            !File.Exists(Path.Combine(uncertainFolder, "pending-time.json")))
            throw new Exception("Uncertain command lost its pending guard or continued to resync");
        try { await TimeConfiguration.RunAsync(uncertainOptions, uncertainFolder, uncertainHost); throw new Exception("Uncertain command retried"); }
        catch (InvalidOperationException) { }
        if (uncertainCommands != 1) throw new Exception("Retry guard allowed another time command");
        string changedFolder = Path.Combine(scratch, "time-final-change-" + Guid.NewGuid().ToString("N"));
        var changedHost = new Fixture(changedFolder, true) { FinalPeers = "different.example.test,0x8" };
        var changedResult = await TimeConfiguration.RunAsync(new(TimeSourceMode.CustomPeers, ["time.example.test"]), changedFolder, changedHost);
        if (changedResult.Status != "Needs review" || changedResult.After?.LocalPeers != changedHost.FinalPeers ||
            !File.Exists(Path.Combine(changedFolder, "pending-time.json")) || Directory.GetFiles(changedFolder, "verified-time-*.json").Length != 0)
            throw new Exception("Final changed configuration was reported verified or lost its guard");
    }
    private sealed class Fixture(string folder, bool verify) : ITimeConfigurationHost
    {
        internal int Applied;
        internal bool ChangeOnSecondRead;
        internal bool ChangeOnThirdRead;
        internal bool FailOnThirdRead;
        internal Action? AfterApply;
        internal string FinalPeers = "time.example.test,0x8";
        private int reads;
        public Task<TimeEnvironment> InspectAsync(CancellationToken token)
        {
            reads++;
            if (FailOnThirdRead && reads == 3) return Task.FromException<TimeEnvironment>(new InvalidOperationException("Final time evidence unavailable."));
            bool managed = ChangeOnSecondRead && reads > 1 || ChangeOnThirdRead && reads == 3;
            return Task.FromResult(new TimeEnvironment(false, false, managed, true)
                { LocalMode = "NTP", LocalPeers = Applied > 0 ? FinalPeers : "old.example.test,0x8" });
        }
        public Task ApplyAsync(TimeOptions options, CancellationToken token)
        {
            using var record = new FileStream(Path.Combine(folder, "pending-time.json"), FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
            using var reader = new StreamReader(record);
            if (!reader.ReadToEnd().Contains("old.example.test")) throw new Exception("Time change lost its prior configuration");
            Applied++;
            AfterApply?.Invoke();
            return Task.CompletedTask;
        }
        public Task<bool> VerifyAsync(TimeOptions options, CancellationToken token) => Task.FromResult(verify);
    }
}
