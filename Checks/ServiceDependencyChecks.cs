using KillerBench.Engine;

internal static class ServiceDependencyChecks
{
    internal static void Run()
    {
        int calls = 0;
        foreach (bool atStart in new[] { true, false })
        {
            using var stop = new CancellationTokenSource();
            int dispatched = 0;
            bool canceled = false;
            try
            {
                ServiceInspection.ReadAsync([RepairKind.WindowsTime], (_, _) =>
                {
                    dispatched++;
                    return Task.FromResult(new WorkflowCheck("Time", CheckState.Clear, "Running"));
                }, stop.Token, new CancelServiceProgress(stop, atStart)).GetAwaiter().GetResult();
            }
            catch (OperationCanceledException) { canceled = true; }
            Require(canceled && dispatched == (atStart ? 0 : 1), "Stop during progress prevents dispatch or completed publication");
        }
        var progress = new ServiceProgress();
        var streamed = ServiceInspection.ReadAsync([RepairKind.WindowsTime, RepairKind.WindowsAudio], (kind, _) =>
        {
            if (kind == RepairKind.WindowsAudio)
            {
                Require(progress.Messages.Any(message => message.Contains("W32Time: Clear")), "First result is published before the next service inspection");
                throw new UnauthorizedAccessException();
            }
            return Task.FromResult(new WorkflowCheck("Time", CheckState.Clear, "Running"));
        }, progress: progress).GetAwaiter().GetResult();
        Require(streamed.Count == 2 && progress.Messages.Any(message => message.Contains("Audiosrv: Unknown")), "Failed service result is streamed while retaining independent results");
        var brokenProgress = new ServiceProgress { Throw = true };
        Require(ServiceInspection.ReadAsync([RepairKind.WindowsTime], (_, _) => Task.FromResult(new WorkflowCheck("Time", CheckState.Clear, "Running")),
            progress: brokenProgress).GetAwaiter().GetResult().Single().State == CheckState.Clear, "Display failure cannot discard service evidence");
        foreach (uint code in new uint[] { 1, 2, 3, 5, 193, 1053, 1057, 1058, 1059, 1064, 1066, 1067, 1068, 1069, 1070, 1072, 1075, 1079, 123456, uint.MaxValue })
        {
            var reported = new ToolResult(0, $"SERVICE_NAME: Spooler\nSTATE : 1 STOPPED\nWIN32_EXIT_CODE : {code} (0x{code:x})", "", false);
            var blocked = ServiceRecovery.Interpret(RepairKind.PrintSpooler, reported, 2);
            Require(blocked.State == CheckState.Attention && blocked.Repair is null && blocked.Evidence == reported &&
                blocked.Detail.Contains(code.ToString()), "Reported configuration failure defers automatic service start");
            Require(ServiceRecovery.Interpret(RepairKind.PrintSpooler, reported with { Output = reported.Output.Replace("1 STOPPED", "4 RUNNING") }, 2).State == CheckState.Clear,
                "A running service is not blocked by a historical exit code");
        }
        foreach (string value in new[] { "-1", "4294967296", "1069 (0x0)", "bad", "0\nWIN32_EXIT_CODE : 1069" })
            Require(ServiceRecovery.Interpret(RepairKind.PrintSpooler, new(0, "SERVICE_NAME: Spooler\nSTATE : 1 STOPPED\nWIN32_EXIT_CODE : " + value, "", false), 2).State == CheckState.Unknown,
                "Malformed service exit evidence cannot authorize recovery");
        Require(ServiceRecovery.Interpret(RepairKind.PrintSpooler, new(0, "SERVICE_NAME: Spooler\nSTATE : 1 STOPPED\nWIN32_EXIT_CODE : 0 (0x0)", "", false), 2).State == CheckState.Repairable,
            "Normal stopped state retains existing start eligibility");
        foreach (int startup in new[] { 2, 3, 4 })
        {
            var neverStarted = new ToolResult(0, "SERVICE_NAME: dot3svc\nSTATE : 1 STOPPED\nWIN32_EXIT_CODE : 1077 (0x435)", "", false);
            var check = ServiceRecovery.Interpret(RepairKind.WiredAutoConfig, neverStarted, startup);
            Require((check.Repair == RepairKind.WiredAutoConfig) == (startup == 2) && check.Evidence == neverStarted &&
                !ServiceRecovery.CanStart(RepairKind.WiredAutoConfig, check),
                "Never-started code preserves startup guards and still requires dependency evidence");
        }
        var managementPlan = Repairs.Plan(RepairKind.WindowsManagement);
        Require(managementPlan.Steps.Count == 2 && managementPlan.Steps[0].Arguments.SequenceEqual(new[] { "start", "winmgmt" }) &&
            managementPlan.Steps[1].Verification && managementPlan.Steps[1].Arguments.SequenceEqual(new[] { "query", "winmgmt" }),
            "WMI availability uses start-only recovery and exact service verification");
        Require(ServiceRecovery.Interpret(RepairKind.WindowsManagement, new(0, "SERVICE_NAME: winmgmt\nSTATE : 1 STOPPED", "", false), 4).Repair is null,
            "Disabled WMI service cannot authorize a start or startup-type change");
        foreach (var rejectedScopes in new[] { Enumerable.Repeat(RepairKind.WindowsTime, 129), new[] { (RepairKind)int.MaxValue }.AsEnumerable() })
        {
            try
            {
                ServiceInspection.ReadAsync(rejectedScopes, (_, _) => throw new Exception("Invalid request reached provider")).GetAwaiter().GetResult();
                throw new Exception("Invalid scope request was accepted");
            }
            catch (ArgumentException) { }
        }
        int enumerated = 0;
        IEnumerable<RepairKind> RepeatingScopes()
        {
            while (true) { enumerated++; yield return RepairKind.SystemFiles; }
        }
        try { ServiceInspection.ReadAsync(RepeatingScopes(), (_, _) => throw new Exception("Unbounded request reached provider")).GetAwaiter().GetResult(); throw new Exception("Unbounded request accepted"); }
        catch (ArgumentException) { }
        Require(enumerated == 129, "Input enumeration is bounded before filtering unsupported service scopes");
        var observations = ServiceInspection.ReadAsync([RepairKind.WindowsAudio, RepairKind.AudioEndpoints, RepairKind.WindowsAudio, RepairKind.SystemFiles],
            (kind, token) =>
            {
                calls++;
                if (kind == RepairKind.WindowsAudio) throw new UnauthorizedAccessException();
                return Task.FromResult(new WorkflowCheck(kind.ToString(), CheckState.Repairable, "Stopped", kind,
                    new ToolResult(0, "retained", "", false)));
            }).GetAwaiter().GetResult();
        Require(calls == 2 && observations.Count == 2 && observations[0].State == CheckState.Unknown &&
            observations[1].State == CheckState.Attention && observations[1].Evidence?.Output == "retained" &&
            observations[1].Detail.Contains("read-only inspection did not start") && observations.All(check => check.Repair is null),
            "Read-only service inspection isolates access failures, deduplicates scopes and never authorizes repair");
        var automaticObservation = ServiceInspection.ReadAsync([RepairKind.WindowsImageAcquisitionService], (kind, _) =>
            Task.FromResult(new WorkflowCheck("Windows Image Acquisition", CheckState.Repairable, "Stopped", kind)
            { ServiceDependencies = new ServiceDependencyGraphResult([], true) { TargetStartup = 2 } })).GetAwaiter().GetResult().Single();
        Require(automaticObservation.State == CheckState.Attention && automaticObservation.Repair is null &&
            automaticObservation.Detail.Contains("Automatic service is stopped") && !automaticObservation.Detail.Contains("normal when idle"),
            "Read-only automatic-service findings explain focused guarded recovery without demand-start wording");
        var demandObservation = ServiceInspection.ReadAsync([RepairKind.WindowsTime], (kind, _) =>
            Task.FromResult(new WorkflowCheck("Windows Time", CheckState.Repairable, "Stopped", kind)
            { ServiceDependencies = new ServiceDependencyGraphResult([], true) { TargetStartup = 3 } })).GetAwaiter().GetResult().Single();
        Require(demandObservation.Detail.Contains("normal when idle"),
            "Read-only demand-start findings retain the idle-state explanation");
        try { ServiceInspection.ReadAsync([RepairKind.WindowsAudio], (_, _) => throw new Exception("Provider must not run"), new CancellationToken(true)).GetAwaiter().GetResult(); throw new Exception("Inspection cancellation ignored"); }
        catch (OperationCanceledException) { }
        var providerCanceled = ServiceInspection.ReadAsync([RepairKind.WindowsTime], (_, _) =>
            Task.FromCanceled<WorkflowCheck>(new CancellationToken(true))).GetAwaiter().GetResult();
        Require(providerCanceled.Single().State == CheckState.Unknown && providerCanceled.Single().Repair is null,
            "Provider cancellation remains incomplete observation without authorizing repair");
        foreach (var failure in new[] { "null-task", "null-result", "exception", "timeout", "truncated", "exit", "stderr" })
        {
            int inspected = 0;
            var results = ServiceInspection.ReadAsync([RepairKind.WindowsTime, RepairKind.WindowsAudio], (kind, _) =>
            {
                inspected++;
                if (kind == RepairKind.WindowsAudio) return Task.FromResult(new WorkflowCheck("Audio", CheckState.Clear, "Running"));
                if (failure == "null-task") return null!;
                if (failure == "null-result") return Task.FromResult<WorkflowCheck>(null!);
                if (failure == "exception") throw new InvalidOperationException();
                return Task.FromResult(new WorkflowCheck("Time", CheckState.Clear, "Running", RepairKind.WindowsTime,
                    new ToolResult(failure == "exit" ? 5 : 0, "partial observation", failure == "stderr" ? "Provider error" : "", failure == "truncated") { TimedOut = failure == "timeout" }));
            }).GetAwaiter().GetResult();
            Require(inspected == 2 && results[0].State == CheckState.Unknown && results[1].State == CheckState.Clear &&
                results.All(result => result.Repair is null), "Failed service observations preserve independent checks: " + failure);
            if (failure is "timeout" or "truncated" or "exit" or "stderr")
                Require(results[0].Evidence?.Output == "partial observation", "Incomplete service evidence remains available for review");
        }
        var nodes = new Dictionary<string, ServiceDependencyNode>(StringComparer.OrdinalIgnoreCase)
        {
            ["root"] = new(3, ["left", "right"], true), ["left"] = new(3, ["shared"], true),
            ["right"] = new(3, ["shared"], true), ["shared"] = new(4, [], true)
        };
        var graph = ServiceDependencyGraph.Read("root", name => nodes[name]);
        Require(graph.TargetStartup == 3, "Graph retains freshly collected target startup configuration");
        var delayedGraph = ServiceDependencyGraph.Read("root", _ => new(2, [], true, true));
        Require(delayedGraph.TargetStartup == 2 && delayedGraph.TargetDelayedAutoStart,
            "Graph retains the target delayed automatic startup setting");
        var disabledTarget = ServiceDependencyGraph.Read("root", _ => new(4, [], true));
        Require(disabledTarget.TargetStartup == 4, "Disabled target cannot be confused with an enabled dependency graph");
        using (var duringRead = new CancellationTokenSource())
        {
            try
            {
                ServiceDependencyGraph.Read("root", _ => { duringRead.Cancel(); return new(3, [], true); }, duringRead.Token);
                throw new Exception("Cancellation during provider read was lost.");
            }
            catch (OperationCanceledException) { }
        }
        Require(graph.Complete && graph.Dependencies.Count == 3 && graph.Dependencies.Single(d => d.Name == "shared").Startup == 4,
            "Dependency closure retains disabled transitive prerequisites and shared nodes once");
        nodes["shared"] = new(3, ["ROOT"], true);
        var cycle = ServiceDependencyGraph.Read("root", name => nodes[name]);
        Require(!cycle.Complete && cycle.Issues.Any(issue => issue.Contains("cycle")), "Transitive cycle blocks recovery with explanation");
        var inaccessible = ServiceDependencyGraph.Read("root", _ => throw new UnauthorizedAccessException());
        Require(!inaccessible.Complete && inaccessible.Issues.Single().Contains("root"), "Unreadable node is identified without exposing provider exception text");
        Require(!ServiceDependencyGraph.Read("0", name => new(3, [(int.Parse(name) + 1).ToString()], true)).Complete,
            "Dependency depth is bounded");
        try { ServiceDependencyGraph.Read("root", name => nodes[name], new CancellationToken(true)); throw new Exception("Graph cancellation ignored."); }
        catch (OperationCanceledException) { }
        var evidence = new ToolResult(0, "SERVICE_NAME: Audiosrv\nSTATE : 1 STOPPED", "", false);
        var identified = evidence with { Output = "SERVICE_NAME: Audiosrv\nSTATE : 4 RUNNING" };
        var nativeShape = evidence with { Output = "\r\nSERVICE_NAME: RpcSs \r\n        TYPE               : 20  WIN32_SHARE_PROCESS  \r\n        STATE              : 4  RUNNING \r\n                                (NOT_STOPPABLE, NOT_PAUSABLE, IGNORES_SHUTDOWN)\r\n        WIN32_EXIT_CODE    : 0  (0x0)\r\n        SERVICE_EXIT_CODE  : 0  (0x0)\r\n        CHECKPOINT         : 0x0\r\n        WAIT_HINT          : 0x0\r\n" };
        Require(ServiceRecovery.MatchesService(nativeShape, "RpcSs") && ServiceRecovery.State(nativeShape) == 4,
            "Native Windows query formatting preserves matching identity and state");
        Require(ServiceRecovery.MatchesService(identified, "audiosrv"), "Service identity comparison ignores casing");
        foreach (var wrong in new[] { evidence with { Output = "STATE : 1 STOPPED" }, identified with { Output = identified.Output.Replace("Audiosrv", "Spooler") },
            identified with { Output = identified.Output + "\nSERVICE_NAME: Audiosrv" }, identified with { TimedOut = true },
            identified with { Truncated = true }, identified with { ExitCode = 5 } })
            Require(!ServiceRecovery.MatchesService(wrong, "Audiosrv"), "Missing, wrong, duplicate and incomplete service identities cannot verify the target");
        var eligible = ServiceRecovery.Interpret(RepairKind.WindowsAudio, evidence, 2);
        var retained = ServiceRecovery.AssessGraph(eligible, inaccessible);
        Require(retained.State == CheckState.Unknown && retained.Repair is null && retained.Evidence == evidence &&
            retained.ServiceDependencies is { Complete: false } && retained.Detail.Contains("root"),
            "Unconfirmed target retains dependency coverage and collection issues alongside raw service output");
        var serialized = System.Text.Json.JsonSerializer.Serialize(retained);
        var restored = System.Text.Json.JsonSerializer.Deserialize<WorkflowCheck>(serialized)!;
        Require(restored.ServiceDependencies?.Issues.SequenceEqual(inaccessible.Issues) == true && restored.Evidence == evidence,
            "Workflow journal round trip preserves dependency issues and service evidence");
        var observed = new List<ServiceRecovery.Dependency> { new("RpcSs", 2) };
        var captured = ServiceRecovery.AssessGraph(eligible, new(observed, true) { TargetStartup = 2 });
        observed.Clear();
        Require(captured.State == CheckState.Repairable && captured.ServiceDependencies!.Dependencies.Count == 1,
            "Collected dependency evidence is detached from the provider collection");
        var delayedPending = ServiceRecovery.AssessGraph(eligible, new([], true)
            { TargetStartup = 2, TargetDelayedAutoStart = true, DelayedStartWindowElapsed = false });
        Require(delayedPending.State == CheckState.Attention && delayedPending.Repair is null &&
            delayedPending.Detail.Contains("startup window", StringComparison.Ordinal) &&
            !ServiceRecovery.CanStart(RepairKind.WindowsAudio, delayedPending),
            "Delayed automatic service remains untouched while Windows owns its startup window");
        var delayedElapsed = ServiceRecovery.AssessGraph(eligible, new([], true)
            { TargetStartup = 2, TargetDelayedAutoStart = true, DelayedStartWindowElapsed = true });
        Require(delayedElapsed.State == CheckState.Repairable && ServiceRecovery.CanStart(RepairKind.WindowsAudio, delayedElapsed),
            "Delayed automatic service can use the ordinary guarded recovery after its startup window");
        Require(ServiceRecovery.Interpret(RepairKind.PrintSpooler, evidence, 3).State == CheckState.Unknown,
            "Public interpreter rejects evidence for a different service");
        Require(ServiceRecovery.Interpret(RepairKind.WindowsAudio, evidence with { Output = "STATE : 1 STOPPED" }, 3).Repair is null,
            "Public interpreter requires service identity before selecting recovery");
        var stoppedTime = new ToolResult(0, "SERVICE_NAME: W32Time\nSTATE : 1 STOPPED\nWIN32_EXIT_CODE : 1077 (0x435)", "", false);
        var ordinaryTime = ServiceRecovery.Interpret(RepairKind.WindowsTime, stoppedTime, 3);
        var requiredTime = ServiceRecovery.Interpret(RepairKind.WindowsTime, stoppedTime, 3, true);
        Require(ordinaryTime.State == CheckState.Information && ordinaryTime.Repair is null && !ordinaryTime.DemandStartAuthorized &&
            requiredTime.State == CheckState.Repairable && requiredTime.Repair == RepairKind.WindowsTime && requiredTime.DemandStartAuthorized,
            "Only focused current Windows Time evidence can authorize its stopped demand-start service");
        requiredTime = ServiceRecovery.AssessGraph(requiredTime, new([], true) { TargetStartup = 3 });
        Require(ServiceRecovery.CanStart(RepairKind.WindowsTime, requiredTime) &&
            !ServiceRecovery.CanStart(RepairKind.WindowsTime, requiredTime with { DemandStartAuthorized = false }) &&
            ServiceRecovery.Interpret(RepairKind.WindowsAudio, evidence, 3, true).Repair is null,
            "Demand-start authorization is retained through dependency evidence and cannot be broadened to another service");
        var stoppedProxy = new ToolResult(0, "SERVICE_NAME: WinHttpAutoProxySvc\nSTATE : 1 STOPPED\nWIN32_EXIT_CODE : 1077 (0x435)", "", false);
        var ordinaryProxy = ServiceRecovery.Interpret(RepairKind.WinHttpAutoProxyService, stoppedProxy, 3);
        var focusedProxy = ServiceRecovery.Interpret(RepairKind.WinHttpAutoProxyService, stoppedProxy, 3, true);
        focusedProxy = ServiceRecovery.AssessGraph(focusedProxy, new([], true) { TargetStartup = 3 });
        Require(ordinaryProxy.State == CheckState.Information && ordinaryProxy.Repair is null &&
            focusedProxy.State == CheckState.Repairable && focusedProxy.Repair == RepairKind.WinHttpAutoProxyService &&
            focusedProxy.DemandStartAuthorized && ServiceRecovery.CanStart(RepairKind.WinHttpAutoProxyService, focusedProxy),
            "Only the focused WinHTTP Auto-Proxy workflow authorizes its stopped demand-start service");
        var stoppedMedic = new ToolResult(0, "SERVICE_NAME: WaaSMedicSvc\nSTATE : 1 STOPPED\nWIN32_EXIT_CODE : 1077 (0x435)", "", false);
        var ordinaryMedic = ServiceRecovery.Interpret(RepairKind.WindowsUpdateMedicService, stoppedMedic, 3);
        var focusedMedic = ServiceRecovery.Interpret(RepairKind.WindowsUpdateMedicService, stoppedMedic, 3, true);
        focusedMedic = ServiceRecovery.AssessGraph(focusedMedic, new([], true) { TargetStartup = 3 });
        Require(ordinaryMedic.State == CheckState.Information && ordinaryMedic.Repair is null &&
            focusedMedic.State == CheckState.Repairable && focusedMedic.Repair == RepairKind.WindowsUpdateMedicService &&
            focusedMedic.DemandStartAuthorized && ServiceRecovery.CanStart(RepairKind.WindowsUpdateMedicService, focusedMedic),
            "Only the focused Windows Update Medic workflow authorizes its stopped demand-start service");
        foreach (int startup in new[] { 0, 1, 2, 3 })
            Require(ServiceRecovery.AssessDependencies(eligible, [new("dependency", startup)], true) == eligible, "Enabled dependency does not invent a blocker");
        var disabled = ServiceRecovery.AssessDependencies(eligible, [new("dependency", 4)], true);
        Require(disabled.State == CheckState.Attention && disabled.Repair is null && disabled.Evidence == evidence && disabled.Detail.Contains("dependency"), "Disabled dependency blocks start with retained evidence");
        Require(ServiceRecovery.AssessDependencies(eligible, [new("missing", null)], true).State == CheckState.Unknown, "Missing dependency does not permit repair");
        Require(ServiceRecovery.AssessDependencies(eligible, [], false).Repair is null, "Incomplete dependency coverage blocks start");
        Require(ServiceRecovery.AssessDependencies(eligible, [new("AUDIOSRV", 3)], true).State == CheckState.Unknown,
            "Self dependency cannot authorize automatic service recovery");
        Require(ServiceRecovery.AssessDependencies(eligible, [new("RpcSs", 2), new("rpcss", 4)], true).State == CheckState.Unknown,
            "Conflicting duplicate dependency identities cannot authorize recovery");
        Require(ServiceRecovery.AssessDependencies(eligible with { Repair = RepairKind.SystemFiles }, [], true).Repair is null,
            "Dependency assessment cannot preserve an unsupported repair target");
        Require(ServiceRecovery.AssessDependencies(eligible, Enumerable.Repeat(new ServiceRecovery.Dependency("dependency", 3), 65).ToArray(), true).Repair is null, "Dependency bound blocks oversized input");
        var running = eligible with { State = CheckState.Clear, Repair = null };
        var incompleteEvidence = evidence with { TimedOut = true, Truncated = true, Error = "Fixture provider error" };
        var deferred = ServiceRecovery.DeferredStart(eligible with { State = CheckState.Unknown, Repair = null, Evidence = incompleteEvidence });
        Require(deferred.ExitCode != 0 && deferred.TimedOut && deferred.Truncated && deferred.Output == incompleteEvidence.Output && deferred.Error.Contains(incompleteEvidence.Error),
            "Deferred service start preserves timeout, truncation, output and provider error for the journal");
        foreach (string name in new[] { "", " dependency", "dependency ", "bad\\path", "bad/path", "bad\nname", new string('a', 257) })
            Require(ServiceRecovery.AssessDependencies(eligible, [new(name, 3)], true).Repair is null, "Invalid embedded dependency names block automatic repair");
        Require(ServiceRecovery.AssessDependencies(eligible, [null!], true).State == CheckState.Unknown, "Null dependency records do not authorize a start");
        Require(ServiceRecovery.AssessDependencies(running, [new("dependency", 4)], false) == running, "Running-state evidence is not replaced by a start prerequisite");
        Require(!ServiceRecovery.CanStart(RepairKind.WindowsAudio, eligible), "Stopped service alone does not prove dependency prerequisites");
        eligible = ServiceRecovery.AssessGraph(eligible, new([], true) { TargetStartup = 2 });
        Require(ServiceRecovery.CanStart(RepairKind.WindowsAudio, eligible), "Matching service with complete enabled prerequisites can proceed");
        foreach (string exitField in new[] { "1053 (0x41d)", "1064 (0x428)", "1067 (0x42b)", "1070 (0x42e)", "1057 (0x421)", "1058 (0x422)", "1059 (0x423)", "1068 (0x42c)",
            "1072 (0x430)", "1079 (0x437)", "1069 (0x42d)", "2 (0x2)", "1075 (0x433)", "1069 (0x0)", "4294967296" })
            Require(!ServiceRecovery.CanStart(RepairKind.WindowsAudio, eligible with
                { Evidence = eligible.Evidence! with { Output = eligible.Evidence.Output + "\nWIN32_EXIT_CODE : " + exitField } }),
                "Final start gate reassesses reported exit evidence even when a caller supplies Repairable state");
        foreach (var contradictory in new ServiceDependencyGraphResult[] {
            new([], false) { TargetStartup = 2 }, new([], true) { TargetStartup = 4 }, new([], true),
            new([new("RpcSs", 4)], true) { TargetStartup = 2 },
            new([new("AUDIOSRV", 3)], true) { TargetStartup = 2 },
            new([], true) { TargetStartup = 2, TargetDelayedAutoStart = true, DelayedStartWindowElapsed = false },
            new([], true) { TargetStartup = 2, Issues = ["Unresolved dependency"] } })
            Require(!ServiceRecovery.CanStart(RepairKind.WindowsAudio, eligible with { ServiceDependencies = contradictory }),
                "Repairable label cannot override missing, disabled, contradictory or unresolved dependency evidence");
        foreach (var inconsistent in new ToolResult?[] { null, identified,
            evidence with { Output = evidence.Output.Replace("Audiosrv", "Spooler") },
            evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { ExitCode = 5 },
            evidence with { Output = evidence.Output.Replace("1 STOPPED", "2 START_PENDING") } })
            Require(!ServiceRecovery.CanStart(RepairKind.WindowsAudio, eligible with { Evidence = inconsistent }),
                "A repairable label cannot override missing, wrong, incomplete or non-stopped service evidence");
        Require(!ServiceRecovery.CanStart(RepairKind.PrintSpooler, eligible), "A different repair identity cannot authorize a service start");
        foreach (var state in Enum.GetValues<CheckState>().Where(s => s != CheckState.Repairable))
            Require(!ServiceRecovery.CanStart(RepairKind.WindowsAudio, eligible with { State = state }), "Only repairable state authorizes start");
        Require(!ServiceRecovery.CanStart(RepairKind.SystemFiles, eligible with { Repair = RepairKind.SystemFiles }), "Non-service repairs cannot authorize service starts");
        Console.WriteLine("PASS: service dependency eligibility, missing/disabled configuration and bounded coverage (fixtures only).");
    }
    private sealed class ServiceProgress : IProgress<string>
    {
        internal readonly List<string> Messages = [];
        internal bool Throw;
        public void Report(string value) { if (Throw) throw new InvalidOperationException("Display failure"); Messages.Add(value); }
    }
    private sealed class CancelServiceProgress(CancellationTokenSource stop, bool atStart) : IProgress<string>
    {
        public void Report(string value) { if (value.StartsWith("Inspecting ", StringComparison.Ordinal) == atStart) stop.Cancel(); }
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
