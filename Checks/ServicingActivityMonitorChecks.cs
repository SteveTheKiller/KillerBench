using KillerBench.Engine;

internal static class ServicingActivityMonitorChecks
{
    public static async Task RunAsync()
    {
        RejectsInvalidOptions();
        ProcessEvidenceIsBounded();
        SetupEvidenceIsBounded();
        CompatibilityEvidenceIsBounded();
        PowerEvidenceIsFocused();
        InstallerActivityIsPrivacyBounded();
        TransferTrendIsConservative();
        LiveStateIsDeterministic();

        var provider = new FixtureProvider(active: true);
        var progressMessages = new List<string>();
        var result = await ServicingActivityMonitor.ObserveAsync(
            new ServicingMonitorOptions(1, 2, IncludeHistory: false), provider, new ProgressCapture(progressMessages));
        Require(result.Count == 2 && result.Select(item => item.Sequence).SequenceEqual([1, 2]), "Monitor retains ordered samples");
        Require(result.All(item => item.ServicingProcessActive && item.State == CheckState.Attention),
            "Active process and attention evidence remain distinct and visible");
        Require(result[0].Checks.All(check => check.Name != "Transfer interval") &&
            result[1].Checks.Any(check => check.Name == "Transfer interval"),
            "Transfer trend begins only after a baseline snapshot");
        Require(result.All(snapshot => snapshot.Checks.Any(check => check.Name == "Live servicing state" &&
            check.Detail.Contains("COMPONENT SERVICING / ACTIVE", StringComparison.Ordinal))),
            "Every snapshot includes a deterministic live servicing state");
        Require(provider.ProcessReads == 2 && provider.EventReads == 2 && provider.HistoryReads == 0 &&
            provider.ServiceReads == 2 && provider.RestartReads == 2 && provider.SetupReads == 2 &&
            provider.CompatibilityReads == 2 && provider.ReadinessReads == 2 && provider.TransferReads == 2 &&
            provider.DeliveryOptimizationReads == 2 && provider.InstallerReads == 2 && provider.ScheduledTaskReads == 2,
            "Monitor honors selected evidence groups per sample");
        Require(progressMessages.Any(message => message.Contains("servicing process activity", StringComparison.Ordinal)) &&
            progressMessages.Any(message => message.Contains("servicing readiness context", StringComparison.Ordinal)) &&
            progressMessages.Any(message => message.Contains("background transfers", StringComparison.Ordinal)) &&
            progressMessages.Any(message => message.Contains("Delivery Optimization activity", StringComparison.Ordinal)) &&
            progressMessages.Any(message => message.Contains("Windows Installer activity", StringComparison.Ordinal)) &&
            progressMessages.Any(message => message.Contains("Windows Update scheduled task", StringComparison.Ordinal)),
            "Monitor reports each long-running evidence stage");
        string firstLive = progressMessages.Single(message => message.StartsWith("SNAPSHOT 1 /", StringComparison.Ordinal));
        Require(firstLive.Contains("SERVICING PROCESS / ACTIVE", StringComparison.Ordinal) &&
            firstLive.Contains("COMPONENT SERVICING / ACTIVE", StringComparison.Ordinal) &&
            firstLive.Contains("ATTENTION / Services", StringComparison.Ordinal) && firstLive.Length < 3_000,
            "Each completed snapshot emits a bounded live summary");
        string retained = ServicingActivityText.FormatReport(result);
        Require(retained.Contains("SERVICING ACTIVITY MONITOR / READ ONLY", StringComparison.Ordinal) &&
            retained.Contains("No update, setup, transfer, service, process, policy, restart or repair action was requested.", StringComparison.Ordinal) &&
            retained.Contains("Services / Attention\nFixture attention.", StringComparison.Ordinal),
            "Retained report keeps full checks and the read-only boundary");

        using var canceled = new CancellationTokenSource();
        var canceling = new FixtureProvider(false, canceled);
        await RequireCanceled(() => ServicingActivityMonitor.ObserveAsync(
            new ServicingMonitorOptions(1, 2, IncludeEvents: false, IncludeHistory: false, IncludeServices: false,
                IncludeSetup: false, IncludeCompatibility: false, IncludeReadiness: false, IncludeTransfers: false,
                IncludeDeliveryOptimization: false, IncludeInstallerActivity: false, IncludeScheduledTask: false),
            canceling, token: canceled.Token));
        Require(canceling.ProcessReads == 1 && canceling.RestartReads == 1, "Cancellation stops before another snapshot");

        Console.WriteLine("PASS: Servicing monitor options, bounded process evidence, selected scopes, sample order and cancellation; substituted providers only.");
    }

    private static void RejectsInvalidOptions()
    {
        RequireThrows(() => new ServicingMonitorOptions(0).Validate(), "Refresh lower bound");
        RequireThrows(() => new ServicingMonitorOptions(61).Validate(), "Refresh upper bound");
        RequireThrows(() => new ServicingMonitorOptions(Samples: 0).Validate(), "Sample lower bound");
        RequireThrows(() => new ServicingMonitorOptions(Samples: 121).Validate(), "Sample upper bound");
        RequireThrows(() => new ServicingMonitorOptions(IncludeEvents: false, IncludeHistory: false,
            IncludeServices: false, IncludeRestart: false, IncludeSetup: false, IncludeCompatibility: false,
            IncludeReadiness: false, IncludeTransfers: false, IncludeDeliveryOptimization: false,
            IncludeInstallerActivity: false, IncludeScheduledTask: false).Validate(), "At least one evidence group");
    }

    private static void SetupEvidenceIsBounded()
    {
        var now = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        var recent = SetupActivityDiagnostics.Assess(new SetupActivitySnapshot(
            [new("Upgrade activity", now.AddSeconds(-10), 4096, Mode: "In-Place Upgrade")], true), now);
        Require(recent.State == CheckState.Information && recent.Detail.Contains("recent write: yes", StringComparison.Ordinal) &&
            recent.Detail.Contains("In-Place Upgrade", StringComparison.Ordinal),
            "Recent setup writes remain activity evidence");
        var olderMode = SetupActivityDiagnostics.Assess(new SetupActivitySnapshot(
            [new("Windows activity", now.AddSeconds(-5), 4096), new("Windows error", now.AddMinutes(-2), 1024, Mode: "Windows Setup")], true), now);
        Require(olderMode.Detail.Contains("Latest classified Setup mode: Windows Setup", StringComparison.Ordinal),
            "Latest classified mode remains visible when the newest log has no mode marker");
        Require(SetupActivityDiagnostics.ClassifyMode("SetupHost::Initialize: CmdLine = [/Compat ScanOnly /quiet]") == "Compatibility Scan" &&
            SetupActivityDiagnostics.ClassifyMode("SetupHost::Initialize: CmdLine = [/PreDownload]") == "PreDownload / Package" &&
            SetupActivityDiagnostics.ClassifyMode("SetupHost::Initialize: CmdLine = [/auto upgrade]") == "In-Place Upgrade" &&
            SetupActivityDiagnostics.ClassifyMode("SetupHost::Initialize: CmdLine = [/Finalize]") == "Finalize" &&
            SetupActivityDiagnostics.ClassifyMode("SetupHost::Initialize: CmdLine = [/quiet]") == "Windows Setup" &&
            SetupActivityDiagnostics.ClassifyMode("ordinary log line") == "", "Setup mode classification excludes command-line content");
        var code = SetupActivityDiagnostics.Assess(new SetupActivitySnapshot(
            [new("Upgrade error", now.AddMinutes(-1), 1024, "0XC1900208")], true), now);
        Require(code.State == CheckState.Attention && code.Detail.Contains("0XC1900208", StringComparison.Ordinal) &&
            code.Detail.Contains("MOSETUP_E_COMPAT_INSTALLREQ_BLOCK", StringComparison.Ordinal) &&
            code.Detail.Contains("CompatData", StringComparison.Ordinal),
            "Bounded feature-upgrade codes request a catalog-backed review");
        var scanOnly = SetupActivityDiagnostics.Assess(new SetupActivitySnapshot(
            [new("Upgrade activity", now.AddMinutes(-1), 1024, "0XC1900210", "Compatibility Scan")], true), now);
        Require(scanOnly.State == CheckState.Information && scanOnly.Detail.Contains("MOSETUP_E_COMPAT_SCANONLY", StringComparison.Ordinal),
            "Compatibility-only completion remains informational");
        var unknownCode = SetupActivityDiagnostics.Assess(new SetupActivitySnapshot(
            [new("Upgrade error", now.AddMinutes(-1), 1024, "0XC190FFFF")], true), now);
        Require(unknownCode.State == CheckState.Attention && unknownCode.Detail.Contains("no installed reference is available", StringComparison.Ordinal),
            "Unknown current Setup codes remain visible and request evidence review");
        var oldCode = SetupActivityDiagnostics.Assess(new SetupActivitySnapshot(
            [new("Upgrade error", now.AddDays(-1), 1024, "0XC1900208")], true), now);
        Require(oldCode.State == CheckState.Information && oldCode.Detail.Contains("outside the 12-hour", StringComparison.Ordinal) &&
            oldCode.Detail.Contains("MOSETUP_E_COMPAT_INSTALLREQ_BLOCK", StringComparison.Ordinal),
            "Old setup codes remain historical context");
        var invalid = SetupActivityDiagnostics.Assess(new SetupActivitySnapshot(
            [new("Unexpected path", now, 1)], true), now);
        Require(invalid.State == CheckState.Unknown, "Unknown setup source remains unknown");
        Require(SetupActivityDiagnostics.Assess(new SetupActivitySnapshot(
            [new("Upgrade activity", now, 1, Mode: "Invented")], true), now).State == CheckState.Unknown,
            "Unknown setup mode remains unknown");
    }

    private static void CompatibilityEvidenceIsBounded()
    {
        var now = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        var absent = SetupCompatibilityDiagnostics.Assess(new(false, DateTimeOffset.MinValue, 0, 0, true), now);
        Require(absent.State == CheckState.Information && absent.Detail.Contains("does not establish compatibility", StringComparison.Ordinal),
            "Absent compatibility report stays inconclusive");
        var blocked = SetupCompatibilityDiagnostics.Assess(new(true, now.AddMinutes(-1), 2, 1, true), now);
        Require(blocked.State == CheckState.Attention && blocked.Detail.Contains("2 hard block", StringComparison.Ordinal) &&
            !blocked.Detail.Contains(".inf", StringComparison.OrdinalIgnoreCase), "Compatibility block counts exclude identities");
        var historical = SetupCompatibilityDiagnostics.Assess(new(true, now.AddDays(-1), 2, 1, true), now);
        Require(historical.State == CheckState.Information && historical.Detail.Contains("historical evidence", StringComparison.Ordinal),
            "Old compatibility reports do not claim a current block");
        Require(SetupCompatibilityDiagnostics.Assess(new(true, now, 4097, 0, true), now).State == CheckState.Unknown,
            "Excessive compatibility evidence remains unknown");
    }

    private static void ProcessEvidenceIsBounded()
    {
        var none = ProcessActivityDiagnostics.Names.ToDictionary(name => name, _ => 0, StringComparer.OrdinalIgnoreCase);
        var idle = ProcessActivityDiagnostics.Interpret(none);
        Require(idle.State == CheckState.Information && !ProcessActivityDiagnostics.Active(idle), "Idle process snapshot remains informational");
        none["TiWorker"] = 2;
        var active = ProcessActivityDiagnostics.Interpret(none);
        Require(ProcessActivityDiagnostics.Active(active) && active.Detail.Contains("TiWorker=2", StringComparison.Ordinal), "Exact active counts are retained");
        none["TiWorker"] = 0;
        none["SetupPrep"] = 1;
        active = ProcessActivityDiagnostics.Interpret(none);
        Require(ProcessActivityDiagnostics.Active(active) && active.Detail.Contains("SetupPrep=1", StringComparison.Ordinal), "Feature upgrade process counts are retained");
        var captured = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        var groups = ProcessActivityDiagnostics.Names.Select(name => name == "SetupHost"
            ? new ServicingProcessGroup(name, 1, 157_286_400, 90, 12.5, true)
            : new ServicingProcessGroup(name, 0, 0, null, null, true)).ToArray();
        var measured = ProcessActivityDiagnostics.Interpret(new ServicingProcessSnapshot(captured, groups));
        Require(ProcessActivityDiagnostics.Active(measured) && measured.Detail.Contains("SetupHost=1 (150.0 MiB, oldest 90 sec, CPU 12.5%)", StringComparison.Ordinal) &&
            measured.Evidence is { Output: var output } && !output.Contains("pid", StringComparison.OrdinalIgnoreCase) &&
            !output.Contains("command", StringComparison.OrdinalIgnoreCase) && !output.Contains("path", StringComparison.OrdinalIgnoreCase),
            "Bounded aggregate CPU, memory and age remain visible without per-process identity");
        groups[0] = groups[0] with { Count = 1, WorkingSetBytes = null, OldestAgeSeconds = 2, CpuPercent = null, MetricsComplete = false };
        Require(ProcessActivityDiagnostics.Interpret(new ServicingProcessSnapshot(captured, groups)).State == CheckState.Unknown,
            "Incomplete metrics cannot retain partial resource values");
        groups[0] = groups[0] with { WorkingSetBytes = 1, OldestAgeSeconds = 2, CpuPercent = 100.1, MetricsComplete = true };
        Require(ProcessActivityDiagnostics.Interpret(new ServicingProcessSnapshot(captured, groups)).State == CheckState.Unknown,
            "Out-of-range aggregate CPU remains unknown");
        none.Remove("dism");
        Require(ProcessActivityDiagnostics.Interpret(none).State == CheckState.Unknown, "Missing process identity remains unknown");
        none["dism"] = 257;
        Require(ProcessActivityDiagnostics.Interpret(none).State == CheckState.Unknown, "Excessive process count remains unknown");
    }

    private static void PowerEvidenceIsFocused()
    {
        string command = string.Join(" ", ServicingPowerDiagnostics.QueryStep().Arguments);
        Require(command.Contains("Win32_Battery", StringComparison.Ordinal) && !command.Contains("DeviceID", StringComparison.Ordinal) &&
            !command.Contains("FriendlyName", StringComparison.Ordinal), "Power query excludes battery identities");
        var none = ServicingPowerDiagnostics.Interpret(new(0, "{\"batteries\":[]}", "", false));
        Require(none.State == CheckState.Information && none.Detail.Contains("no Win32_Battery", StringComparison.Ordinal),
            "No battery remains normal desktop context");
        var charging = ServicingPowerDiagnostics.Interpret(new(0, "{\"batteries\":[{\"charge\":80,\"status\":6}]}", "", false));
        Require(charging.State == CheckState.Information && charging.Detail.Contains("minimum charge 80%", StringComparison.Ordinal) &&
            charging.Detail.Contains("charging-status records 1", StringComparison.Ordinal), "Charging evidence remains privacy bounded");
        var low = ServicingPowerDiagnostics.Assess([new(18, 4)]);
        Require(low.State == CheckState.Attention && low.Detail.Contains("minimum charge 18%", StringComparison.Ordinal),
            "Low battery requests readiness review");
        Require(ServicingPowerDiagnostics.Interpret(new(0, "{\"batteries\":[{\"charge\":101,\"status\":6}]}", "", false)).State == CheckState.Unknown,
            "Invalid battery evidence remains unknown");
    }

    private static void InstallerActivityIsPrivacyBounded()
    {
        string command = string.Join(" ", ServicingInstallerActivityDiagnostics.QueryStep().Arguments);
        Require(command.Contains("Win32_Process", StringComparison.Ordinal) && command.Contains("processCount", StringComparison.Ordinal) &&
            !command.Contains("ProcessId", StringComparison.Ordinal) && !command.Contains("ParentProcessId", StringComparison.Ordinal) &&
            !command.Contains("CommandLine", StringComparison.Ordinal), "Servicing monitor Installer query retains counts without process identity");
        var idle = ServicingInstallerActivityDiagnostics.Interpret(new(0,
            "{\"complete\":true,\"processCount\":0,\"marker\":false}", "", false));
        Require(idle.State == CheckState.Information && idle.Detail.Contains("No msiexec process", StringComparison.Ordinal),
            "Idle Installer summary remains informational");
        var active = ServicingInstallerActivityDiagnostics.Assess(new(2, true));
        Require(active.State == CheckState.Attention && active.Detail.Contains("Observed 2 msiexec processes", StringComparison.Ordinal) &&
            active.Detail.Contains("Process IDs", StringComparison.Ordinal), "Active Installer summary requests review without identities");
        Require(ServicingInstallerActivityDiagnostics.Interpret(new(0,
            "{\"complete\":true,\"processCount\":129,\"marker\":false}", "", false)).State == CheckState.Unknown,
            "Out-of-range Installer activity remains unknown");
    }

    private static void TransferTrendIsConservative()
    {
        const string id = "11111111-1111-1111-1111-111111111111";
        static WorkflowCheck Bits(string id, ulong transferred) => BitsDiagnostics.Interpret(new(0,
            "{\"scope\":\"CurrentAccount\",\"complete\":true,\"jobs\":[{\"id\":\"" + id +
            "\",\"name\":\"Fixture\",\"state\":\"Transferring\",\"error\":\"\",\"owner\":\"fixture\",\"errorCode\":0,\"bytesTotal\":\"1000\",\"bytesTransferred\":\"" + transferred + "\"}]}", "", false));
        static WorkflowCheck Delivery(ulong transferred) => DeliveryOptimizationActivityDiagnostics.Interpret(new(0,
            "{\"complete\":true,\"entryCount\":1,\"groups\":[{\"status\":\"Downloading\",\"count\":1,\"fileBytes\":1000,\"downloadedBytes\":" + transferred +
            ",\"peerBytes\":0,\"httpBytes\":" + transferred + ",\"cacheServerBytes\":0}]}", "", false));

        var started = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        var previous = new ServicingActivitySnapshot(1, started, false, CheckState.Information,
            [Bits(id, 100), Delivery(200)]);
        var moving = ServicingTransferTrendDiagnostics.Compare(previous, [Bits(id, 150), Delivery(400)], started.AddSeconds(3));
        Require(moving.State == CheckState.Information && moving.Detail.Contains("BITS: 50 bytes moved", StringComparison.Ordinal) &&
            moving.Detail.Contains("Delivery Optimization: 200 aggregate downloaded bytes", StringComparison.Ordinal) &&
            moving.Detail.Contains("does not establish eventual completion", StringComparison.Ordinal),
            "Stable transfer counters produce bounded deltas without a completion claim");

        var changed = ServicingTransferTrendDiagnostics.Compare(previous, [Bits(Guid.NewGuid().ToString(), 10), Delivery(100)], started.AddSeconds(3));
        Require(changed.Detail.Contains("no stable job identity", StringComparison.Ordinal) &&
            changed.Detail.Contains("aggregate downloaded bytes decreased", StringComparison.Ordinal) &&
            changed.Detail.Contains("does not establish inactivity", StringComparison.Ordinal),
            "Changed transfer sets do not produce a false zero-movement claim");
        var invalidInterval = ServicingTransferTrendDiagnostics.Compare(previous, [Bits(id, 150)], started.AddMinutes(6));
        Require(invalidInterval.Detail.Contains("outside its five-minute bound", StringComparison.Ordinal),
            "Excessive transfer intervals are not compared");
    }

    private static void LiveStateIsDeterministic()
    {
        var now = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        WorkflowCheck Processes(params string[] active)
        {
            var groups = ProcessActivityDiagnostics.Names.Select(name => new ServicingProcessGroup(name,
                active.Contains(name, StringComparer.OrdinalIgnoreCase) ? 1 : 0,
                active.Contains(name, StringComparer.OrdinalIgnoreCase) ? 1 : 0,
                active.Contains(name, StringComparer.OrdinalIgnoreCase) ? 1 : null, null, true)).ToArray();
            return ProcessActivityDiagnostics.Interpret(new ServicingProcessSnapshot(now, groups));
        }

        var recentSetup = SetupActivityDiagnostics.Assess(new SetupActivitySnapshot(
            [new("Upgrade activity", now.AddSeconds(-8), 100, Mode: "In-Place Upgrade")], true), now);
        var setup = ServicingPhaseDiagnostics.Summarize([Processes("SetupHost", "TiWorker"), recentSetup], now);
        Require(setup.Detail.Contains("WINDOWS SETUP / IN-PLACE UPGRADE", StringComparison.Ordinal) &&
            !setup.Detail.Contains("COMPONENT SERVICING", StringComparison.Ordinal),
            "Recent classified Setup activity has deterministic priority");

        WorkflowCheck Update(int eventId, DateTimeOffset time, CheckState state = CheckState.Information)
        {
            string xml = $"<Events><Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{WindowsUpdateEvents.Provider}'/><EventID>{eventId}</EventID><TimeCreated SystemTime='{time:O}'/></System><EventData><Data Name='updateTitle'>Fixture update</Data></EventData></Event></Events>";
            return new("Windows Update live activity", state, "Fixture", Evidence: new(0, xml, "", false));
        }

        var updating = ServicingPhaseDiagnostics.Summarize([Processes(), Update(43, now.AddSeconds(-5))], now);
        Require(updating.State == CheckState.Information &&
            updating.Detail.Contains("WINDOWS UPDATE / INSTALLING", StringComparison.Ordinal) &&
            updating.Detail.Contains("5 seconds old", StringComparison.Ordinal),
            "Recent Windows Update transitions drive the live state before idle fallback");

        var failedUpdate = ServicingPhaseDiagnostics.Summarize([Processes(), Update(20, now.AddSeconds(-8), CheckState.Attention)], now);
        Require(failedUpdate.State == CheckState.Attention && failedUpdate.Detail.Contains("WINDOWS UPDATE / FAILED", StringComparison.Ordinal),
            "Recent Windows Update failures drive the attention state");

        var staleUpdate = ServicingPhaseDiagnostics.Summarize([Processes(), Update(43, now.AddMinutes(-3))], now);
        Require(staleUpdate.Detail.Contains("IDLE / NO SUPPORTED SERVICING PROCESS OBSERVED", StringComparison.Ordinal),
            "Old Windows Update transitions remain history instead of live state");

        var grouped = ServicingPhaseDiagnostics.Summarize([Processes("TiWorker", "MoUsoCoreWorker")], now);
        Require(grouped.Detail.Contains("COMPONENT SERVICING / ACTIVE (TiWorker)", StringComparison.Ordinal) &&
            grouped.Detail.Contains("UPDATE ORCHESTRATION / ACTIVE (MoUsoCoreWorker)", StringComparison.Ordinal) &&
            grouped.Detail.Contains("not a progress percentage", StringComparison.Ordinal),
            "Active process families are summarized without a progress claim");

        var installerEvidence = new ToolResult(0, "{\"complete\":true,\"processCount\":1,\"marker\":true}", "", false);
        var installer = ServicingPhaseDiagnostics.Summarize([Processes(),
            ServicingInstallerActivityDiagnostics.Interpret(installerEvidence)], now);
        Require(installer.Detail.Contains("WINDOWS INSTALLER / ACTIVE", StringComparison.Ordinal),
            "Installer activity remains distinct from Windows servicing processes");

        var restart = RestartDiagnostics.Assess(new(true, false, true, false, false, false, "FIXTURE", "FIXTURE"));
        var waiting = ServicingPhaseDiagnostics.Summarize([Processes(), restart], now);
        Require(waiting.Detail.Contains("RESTART / PENDING", StringComparison.Ordinal),
            "Pending restart is visible when supported processes are idle");
        var idle = ServicingPhaseDiagnostics.Summarize([Processes()], now);
        Require(idle.Detail.Contains("IDLE / NO SUPPORTED SERVICING PROCESS OBSERVED", StringComparison.Ordinal) &&
            idle.Detail.Contains("between-sample activity can still exist", StringComparison.Ordinal),
            "Idle state preserves the sampling limitation");
        Require(ServicingPhaseDiagnostics.Summarize([new("Active servicing processes", CheckState.Unknown, "Unavailable")], now).State == CheckState.Unknown,
            "Missing process evidence cannot produce a live state");
    }

    private static async Task RequireCanceled(Func<Task> action)
    {
        try { await action(); throw new InvalidOperationException("Expected cancellation."); }
        catch (OperationCanceledException) { }
    }

    private static void RequireThrows(Action action, string label)
    {
        try { action(); throw new InvalidOperationException(label + " did not reject invalid options."); }
        catch (ArgumentException) { }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class ProgressCapture(List<string> values) : IProgress<string>
    {
        public void Report(string value) => values.Add(value);
    }

    private sealed class FixtureProvider(bool active, CancellationTokenSource? cancel = null) : IServicingActivityProvider
    {
        public int ProcessReads { get; private set; }
        public int EventReads { get; private set; }
        public int HistoryReads { get; private set; }
        public int ServiceReads { get; private set; }
        public int RestartReads { get; private set; }
        public int SetupReads { get; private set; }
        public int CompatibilityReads { get; private set; }
        public int ReadinessReads { get; private set; }
        public int TransferReads { get; private set; }
        public int DeliveryOptimizationReads { get; private set; }
        public int InstallerReads { get; private set; }
        public int ScheduledTaskReads { get; private set; }

        public Task<WorkflowCheck> ReadProcessesAsync(CancellationToken token)
        {
            ProcessReads++;
            var counts = ProcessActivityDiagnostics.Names.ToDictionary(name => name, name => active && name == "TiWorker" ? 1 : 0,
                StringComparer.OrdinalIgnoreCase);
            return Task.FromResult(ProcessActivityDiagnostics.Interpret(counts));
        }

        public Task<WorkflowCheck> ReadEventsAsync(CancellationToken token)
        { EventReads++; return Task.FromResult(Info("Events")); }

        public Task<WorkflowCheck> ReadHistoryAsync(CancellationToken token)
        { HistoryReads++; return Task.FromResult(Info("History")); }

        public Task<WorkflowCheck> ReadServicesAsync(CancellationToken token)
        { ServiceReads++; return Task.FromResult(new WorkflowCheck("Services", CheckState.Attention, "Fixture attention.")); }

        public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token)
        {
            RestartReads++;
            cancel?.Cancel();
            return Task.FromResult(Info("Restart"));
        }

        public Task<WorkflowCheck> ReadSetupAsync(CancellationToken token)
        { SetupReads++; return Task.FromResult(Info("Setup")); }

        public Task<WorkflowCheck> ReadCompatibilityAsync(CancellationToken token)
        { CompatibilityReads++; return Task.FromResult(Info("Compatibility")); }

        public Task<WorkflowCheck> ReadReadinessAsync(CancellationToken token)
        { ReadinessReads++; return Task.FromResult(Info("Readiness")); }

        public Task<WorkflowCheck> ReadTransfersAsync(CancellationToken token)
        { TransferReads++; return Task.FromResult(Info("Transfers")); }

        public Task<WorkflowCheck> ReadDeliveryOptimizationAsync(CancellationToken token)
        { DeliveryOptimizationReads++; return Task.FromResult(Info("Delivery Optimization")); }

        public Task<WorkflowCheck> ReadInstallerActivityAsync(CancellationToken token)
        { InstallerReads++; return Task.FromResult(Info("Installer")); }

        public Task<WorkflowCheck> ReadScheduledTaskAsync(CancellationToken token)
        { ScheduledTaskReads++; return Task.FromResult(Info("Windows Update scheduled task")); }

        private static WorkflowCheck Info(string name) => new(name, CheckState.Information, "Fixture evidence.");
    }
}
