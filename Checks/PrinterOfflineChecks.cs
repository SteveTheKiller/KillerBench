using System.Reflection;
using KillerBench.Engine;

internal static class PrinterOfflineChecks
{
    internal static async Task RunAsync()
    {
        var success = RecipeChecks.Success(RepairKind.PrinterOffline);
        var scopedHost = new SelectedHost();
        string scopedFolder = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "KillerBench-printer-run-" + Guid.NewGuid().ToString("N"));
        var scopedResult = await Repairs.RunAsync(RepairKind.PrinterOffline, scopedFolder, host: scopedHost, selectedPrinterQueues: ["A"]);
        Require(scopedHost.Calls == 3 && scopedHost.Prerequisites == 2 && Repairs.IsVerified(scopedResult) &&
            scopedResult.SelectedPrinterQueues!.SequenceEqual(new[] { "A" }), "Shared runner diagnoses, checks prerequisites and verifies selected queues");
        Require(System.IO.File.ReadAllText(scopedResult.JournalPath).Contains("selectedPrinterQueues"), "Selected queue scope is journaled");
        for (int index = 0; index < scopedResult.Steps.Count; index++)
        {
            var steps = scopedResult.Steps.ToArray();
            steps[index] = steps[index] with { Result = steps[index].Result with { Error = "Fixture provider failure" } };
            Require(!Repairs.IsVerified(scopedResult with { Steps = steps }) && !PrinterOfflineRecovery.VerifySelected(["A"], steps),
                "Errors in selected-printer mutation or verification prevent a successful result");
            Require(PrinterOfflineRecovery.Read(steps[index].Result) is null,
                "Error-bearing printer snapshots cannot establish eligibility or final state");
        }
        foreach (string[] targets in new[] { Array.Empty<string>(), new[] { "A", "a" }, new[] { "" }, new[] { "A\nB" } })
            Require(!Repairs.IsVerified(scopedResult with { SelectedPrinterQueues = targets }),
                "Malformed saved selections remain unverified without throwing");
        Require(!Repairs.IsVerified(scopedResult with { Steps = null! }) &&
            !Repairs.IsVerified(scopedResult with { Steps = new RepairStepResult[] { null! } }),
            "Missing saved repair evidence remains unverified");
        foreach (string evidence in new[] {
            "{\"targets\":[],\"queues\":[{\"id\":\"A\",\"offline\":false}]}",
            "{\"targets\":[],\"queues\":[]}", "unavailable" })
        {
            var blockedHost = new SelectedHost { InitialEvidence = evidence };
            try
            {
                await Repairs.RunAsync(RepairKind.PrinterOffline, scopedFolder, host: blockedHost, selectedPrinterQueues: ["A"]);
                throw new Exception("Unconfirmed selected-printer repair started");
            }
            catch (InvalidOperationException) { }
            Require(blockedHost.Calls == 1, "Healthy, missing or unknown selected queue cannot reach mutation");
        }
        foreach (bool discovery in new[] { true, false })
        {
            using var stop = new CancellationTokenSource();
            var provider = new TaskCompletionSource<ToolResult>();
            Task operation = discovery
                ? PrinterDiagnostics.DiscoverAsync(stop.Token, execute: (_, _) => provider.Task)
                : PrinterDiagnostics.CheckOfflineAsync(["A"], stop.Token, execute: (_, _) => provider.Task);
            stop.Cancel();
            try { await operation.WaitAsync(TimeSpan.FromSeconds(2)); throw new Exception("Printer query ignored cancellation"); }
            catch (OperationCanceledException) { }
            provider.SetResult(new(0, "{\"targets\":[],\"queues\":[]}", "", false));
            Require(operation.IsCanceled, "Late printer evidence cannot complete a canceled query");
        }
        var selectedPlan = PrinterOfflineRecovery.Plan(["A"]);
        var selectedSteps = new[] {
            new RepairStepResult(selectedPlan.Steps[0], new(0, "{\"targets\":[\"A\"],\"queues\":[{\"id\":\"A\",\"offline\":false}]}", "", false)),
            new RepairStepResult(selectedPlan.Steps[1], new(0, "{\"targets\":[],\"queues\":[{\"id\":\"A\",\"offline\":false}]}", "", false)) };
        Require(PrinterOfflineRecovery.VerifySelected(["A"], selectedSteps), "Selected plan verifies matching executed commands and queues");
        Require(!PrinterOfflineRecovery.VerifySelected(["B"], selectedSteps), "Results cannot verify another selected plan");
        var wrongCommands = selectedSteps.Select(s => s with { Step = PrinterOfflineRecovery.Step(s.Step.Verification) }).ToArray();
        Require(!PrinterOfflineRecovery.VerifySelected(["A"], wrongCommands), "Broad commands cannot prove selected-only execution");
        foreach (var failedEvidence in new[] {
            selectedSteps[1].Result with { ExitCode = 5 },
            selectedSteps[1].Result with { TimedOut = true },
            selectedSteps[1].Result with { Truncated = true },
            selectedSteps[1].Result with { Output = "{\"targets\":[],\"queues\":[]}" },
            selectedSteps[1].Result with { Output = "{\"targets\":[],\"queues\":[{\"id\":\"A\",\"offline\":true}]}" },
            selectedSteps[1].Result with { Output = "{\"targets\":[],\"queues\":[{\"id\":\"A\",\"offline\":false},{\"id\":\"Extra\",\"offline\":false}]}" } })
            Require(!PrinterOfflineRecovery.VerifySelected(["A"], [selectedSteps[0], selectedSteps[1] with { Result = failedEvidence }]),
                "Failed, incomplete or changed-scope evidence cannot verify selected repair");
        Require(!PrinterOfflineRecovery.VerifySelected(["A"], [selectedSteps[0]]), "Canceled repair without verification remains unresolved");
        foreach (Exception failure in new Exception[] { new System.IO.IOException("Fixture unavailable"), new UnauthorizedAccessException("Fixture denied"),
            new InvalidOperationException("Fixture changed"), new TimeoutException("Fixture timeout"), new NotSupportedException("Fixture unsupported") })
        {
            var providerFailure = await PrinterDiagnostics.CheckOfflineAsync(["A"], execute: (_, _) => Task.FromException<ToolResult>(failure));
            Require(providerFailure is { State: CheckState.Unknown, Repair: null, Evidence.ExitCode: -1 } && providerFailure.Evidence.Error == failure.Message,
                "Printer provider failures retain unknown scope without repair eligibility");
        }
        var missing = await PrinterDiagnostics.CheckOfflineAsync(["A"], execute: (_, _) => Task.FromResult<ToolResult>(null!));
        Require(missing is { State: CheckState.Unknown, Repair: null }, "Missing printer evidence cannot authorize a repair");
        var discovered = await PrinterDiagnostics.DiscoverAsync(execute: (step, _) =>
        {
            Require(!step.Arguments[^1].Contains("$p.Put()"), "Discovery cannot change printer settings");
            return Task.FromResult(new ToolResult(0, "{\"targets\":[],\"queues\":[{\"id\":\"A\",\"offline\":true}]}", "", false));
        });
        Require(discovered?.Queues["A"] == true, "Discovery preserves queue identity and offline state");
        var partialDiscovery = await PrinterDiagnostics.DiscoverAsync(execute: (_, _) =>
            Task.FromResult(new ToolResult(0, "{\"targets\":[],\"queues\":[]}", "", true)));
        Require(partialDiscovery is null, "Truncated discovery is unavailable rather than an empty queue list");
        foreach (string queue in new[] { "A", "Different" })
        {
            var check = await PrinterDiagnostics.CheckOfflineAsync(["A"], execute: (step, _) =>
            {
                Require(!step.Arguments[^1].Contains("$p.Put()"), "Selected diagnosis must not mutate queues");
                return Task.FromResult(new ToolResult(0, "{\"targets\":[],\"queues\":[{\"id\":\"" + queue + "\",\"offline\":false}]}", "", false));
            });
            Require(check.State == (queue == "A" ? CheckState.Clear : CheckState.Unknown), "Selected diagnosis rejects a different returned queue");
        }
        Require(Repairs.IsVerified(success), "Changed queue verifies offline setting");
        foreach (string operation in new[] {
            "{\"targets\":[\"A\"],\"queues\":[]}",
            "{\"targets\":[\"A\"],\"queues\":[{\"id\":\"B\",\"offline\":false}]}",
            "{\"targets\":[\"A\"],\"queues\":[{\"id\":\"A\",\"offline\":true}]}" })
        {
            var inconsistentOperation = success.Steps.ToArray();
            inconsistentOperation[0] = inconsistentOperation[0] with { Result = new(0, operation, "", false) };
            Require(!Repairs.IsVerified(success with { Steps = inconsistentOperation }), "Printer repair requires a matching changed-operation snapshot");
        }
        var gone = success with { Steps = [success.Steps[0], success.Steps[1] with { Result = new(0, "{\"targets\":[],\"queues\":[]}", "", false) }] };
        Require(!Repairs.IsVerified(gone), "Disappearing queue cannot verify");
        foreach (string ambiguous in new[] {
            "{\"targets\":[],\"queues\":[],\"queues\":[{\"id\":\"A\",\"offline\":false}]}",
            "{\"targets\":[],\"queues\":[{\"id\":\"A\",\"offline\":true,\"offline\":false}]}",
            "{\"targets\":[],\"queues\":[{\"id\":\"A\",\"id\":\"B\",\"offline\":false}]}" })
        {
            var invalid = new ToolResult(0, ambiguous, "", false);
            Require(PrinterOfflineRecovery.Read(invalid) is null && PrinterOfflineRecovery.Interpret(invalid).State == CheckState.Unknown,
                "Duplicate evidence fields remain unknown");
            var ambiguousFinal = success with { Steps = [success.Steps[0], success.Steps[1] with { Result = invalid }] };
            Require(!Repairs.IsVerified(ambiguousFinal), "Ambiguous final evidence cannot verify repair");
        }
        Require(PrinterOfflineRecovery.Read(new(0, "{\"targets\":[],\"queues\":[{\"id\":\"A\",\"offline\":null}]}", "", false)) is null, "Missing offline state cannot imply online");
        const string fixture = """
            $global:queues=@(
                [pscustomobject]@{DeviceID='A';Local=$true;Network=$false;WorkOffline=$true;KeepPrintedJobs=$true},
                [pscustomobject]@{DeviceID='Remote';Local=$true;Network=$true;WorkOffline=$true;KeepPrintedJobs=$true}
            );
            foreach($p in $global:queues) { $p | Add-Member -MemberType ScriptMethod -Name Put -Value { if($this.DeviceID -ne 'A' -or -not $this.KeepPrintedJobs) { throw 'Wrong queue or altered job settings' }; return $null } }
            function Get-WmiObject($Class,[switch]$EnableAllPrivileges,$ErrorAction) {
                if($Class -ne 'Win32_Printer') { throw 'Unexpected query' }; return $global:queues
            }
            """;
        var method = typeof(ToolResult).Assembly.GetType("KillerBench.Engine.NativeTool")!.GetMethod("RunAsync", BindingFlags.Static | BindingFlags.NonPublic)!;
        async Task<ToolResult> Run(string providers, string[]? selected = null)
        {
            var args = (selected is null ? PrinterOfflineRecovery.Step(false) : PrinterOfflineRecovery.Step(false, selected)).Arguments.ToArray();
            args[^1] = providers + "\n" + args[^1];
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            return await ((Task<ToolResult>)method.Invoke(null, new object?[] { PrintRecovery.Tool, args, false, null, timeout.Token })!);
        }
        var output = await Run(fixture);
        var selectedOutput = await Run(fixture, ["A"]);
        Require(PrinterOfflineRecovery.Read(selectedOutput)?.Targets.SequenceEqual(new[] { "A" }) == true, "Explicit local queue selection executes");
        var anotherLocal = fixture.Replace("Network=$true", "Network=$false");
        var isolated = await Run(anotherLocal, ["A"]);
        Require(PrinterOfflineRecovery.Read(isolated)?.Targets.SequenceEqual(new[] { "A" }) == true,
            "An unselected offline local queue must not receive a provider write");
        const string punctuatedName = "O'Brien; $(throw 'unexpected')";
        string quotedName = "'" + punctuatedName.Replace("'", "''") + "'";
        var punctuated = await Run(fixture.Replace("'A'", quotedName), [punctuatedName]);
        Require(PrinterOfflineRecovery.Read(punctuated)?.Targets.SequenceEqual(new[] { punctuatedName }) == true,
            "Queue punctuation stays literal and cannot become a command");
        var missingOutput = await Run(fixture, ["Missing"]);
        Require(missingOutput.ExitCode != 0, "Missing selected queue blocks mutation");
        var remoteOutput = await Run(fixture, ["Remote"]);
        Require(remoteOutput.ExitCode != 0, "Remote queue cannot be selected as local");
        string onlineFixture = fixture.Replace("WorkOffline=$true", "WorkOffline=$false")
            .Replace("return $null", "throw 'MUTATION_CALLED'");
        var onlineStep = PrinterOfflineRecovery.Step(false, ["A"]);
        var onlineArguments = onlineStep.Arguments.ToArray();
        onlineArguments[^1] = onlineFixture + "\n" + onlineArguments[^1];
        try
        {
            await PrintRecovery.ExecuteAsync(onlineStep with { Arguments = onlineArguments });
            throw new Exception("An online queue returned fabricated printer-change evidence");
        }
        catch (RepairStepSkippedException skipped)
        {
            Require(PrinterOfflineRecovery.Read(skipped.Evidence) is { Targets.Length: 0 } &&
                !skipped.Evidence.Error.Contains("MUTATION_CALLED", StringComparison.Ordinal),
                "A queue that became online records an exact no-change result without calling the provider");
        }
        foreach (var invalid in new[] { Array.Empty<string>(), new[] { "A", "a" }, new[] { "bad\nqueue" } })
        {
            try { PrinterOfflineRecovery.Step(false, invalid); throw new Exception("Invalid queue selection accepted"); }
            catch (ArgumentException) { }
        }
        var state = PrinterOfflineRecovery.Read(output);
        Require(state is not null && state.Targets.SequenceEqual(new[] { "A" }) && state.Queues.Count == 1 && !state.Queues["A"], "Script changes only the affected local queue: " + output.Error);
        var failed = await Run(fixture.Replace("return $null", "throw 'Synthetic provider denied update'"));
        Require(failed.ExitCode != 0 && PrinterOfflineRecovery.Read(failed) is null, "Provider write failures cannot verify");
        var unavailable = await Run(fixture.Replace("WorkOffline=$true", "WorkOffline=$null"));
        Require(unavailable.ExitCode != 0 && PrinterOfflineRecovery.Read(unavailable) is null, "Unknown provider state cannot trigger update");
        Console.WriteLine("PASS: printer offline selection, changed-queue verification and failure handling with substituted WMI providers.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    private sealed class SelectedHost : IRepairHost
    {
        internal int Calls;
        internal int Prerequisites;
        internal string? InitialEvidence;
        public void CheckPrerequisites() => Prerequisites++;
        public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress)
        {
            Calls++;
            if (Calls == 1 && InitialEvidence is not null) return Task.FromResult(new ToolResult(0, InitialEvidence, "", false));
            var expected = PrinterOfflineRecovery.Step(Calls != 2, ["A"]);
            Require(step.Arguments.SequenceEqual(expected.Arguments), "Runner executes the selected plan only");
            return Task.FromResult(new ToolResult(0,
                "{\"targets\":" + (Calls == 2 ? "[\"A\"]" : "[]") + ",\"queues\":[{\"id\":\"A\",\"offline\":" + (Calls == 1 ? "true" : "false") + "}]}", "", false));
        }
    }
}
