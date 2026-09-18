using KillerBench.Engine;
using System.IO;

internal static class EventChannelEnableChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "event-channel-enable-" + Guid.NewGuid().ToString("N"));
        var disabled = State(false);
        var enabled = State(true);
        var selectedReview = await EventChannelEnable.ReviewAsync("System", new Host([disabled], false, true));
        Require(selectedReview is { State: CheckState.Attention, Repair: null } &&
            selectedReview.Detail.Contains("separate guarded enable action", StringComparison.Ordinal),
            "Selected channel review exposes disabled local state without granting repair authority");
        var managedReview = await EventChannelEnable.ReviewAsync("System", new Host([disabled], true, true));
        Require(managedReview is { State: CheckState.Attention, Repair: null } &&
            managedReview.Detail.Contains("policy owner", StringComparison.Ordinal),
            "Selected managed channel remains a policy-owner review");
        var incompleteReview = await EventChannelEnable.ReviewAsync("System", new Host([], false, true));
        Require(incompleteReview is { State: CheckState.Unknown, Repair: null },
            "Unavailable selected-channel evidence remains unknown");
        var previewHost = new Host([disabled], false, true);
        var preview = await EventChannelEnable.RunAsync("System", false, Path.Combine(root, "preview"), previewHost);
        var previewCheck = EventChannelEnable.ToWorkflowCheck(preview);
        Require(preview.Outcome == EventChannelEnableOutcome.Preview && previewHost.Enables == 0 &&
            previewCheck is { State: CheckState.Repairable, Repair: RepairKind.EventChannelEnable },
            "Event channel enablement defaults to a repairable preview");

        var verifiedHost = new Host([disabled, disabled, disabled, enabled], false, true);
        var verified = await EventChannelEnable.RunAsync("System", true, Path.Combine(root, "verified"), verifiedHost);
        Require(verified.Outcome == EventChannelEnableOutcome.Verified && verified.GetExitCode() == 0 && verifiedHost.Enables == 1 &&
            File.Exists(verified.RecordPath!), "Disabled unmanaged channel is enabled once and verified with durable evidence");

        var managed = await EventChannelEnable.RunAsync("System", true, Path.Combine(root, "managed"), new Host([disabled], true, true));
        var unknown = await EventChannelEnable.RunAsync("System", true, Path.Combine(root, "unknown"), new Host([disabled], null, true));
        var denied = await EventChannelEnable.RunAsync("System", true, Path.Combine(root, "denied"), new Host([disabled], false, false));
        Require(managed.Outcome == EventChannelEnableOutcome.Blocked && unknown.Outcome == EventChannelEnableOutcome.Blocked &&
            denied.Outcome == EventChannelEnableOutcome.Blocked, "Managed, unknown-policy and non-administrator states block before mutation");

        string becameEnabledRoot = Path.Combine(root, "became-enabled");
        var becameEnabledHost = new Host([disabled, disabled, enabled], false, true);
        var becameEnabled = await EventChannelEnable.RunAsync("System", true, becameEnabledRoot, becameEnabledHost);
        Require(becameEnabled.Outcome == EventChannelEnableOutcome.Blocked && becameEnabledHost.Enables == 0 &&
            becameEnabled.After?.Enabled == true && !File.Exists(Path.Combine(becameEnabledRoot, "event-channel-enable.pending.json")) &&
            Directory.GetFiles(becameEnabledRoot, "*.prepared.json").Length == 1,
            "A channel enabled after preparation records final evidence without another enable command");

        string finalFailureRoot = Path.Combine(root, "final-failure");
        var finalFailureHost = new Host([disabled, disabled, disabled], false, true) { FailRead = 3 };
        var finalFailure = await EventChannelEnable.RunAsync("System", true, finalFailureRoot, finalFailureHost);
        Require(finalFailure.Outcome == EventChannelEnableOutcome.Blocked && finalFailureHost.Enables == 0 &&
            !File.Exists(Path.Combine(finalFailureRoot, "event-channel-enable.pending.json")) &&
            Directory.GetFiles(finalFailureRoot, "*.prepared.json").Length == 1,
            "Unavailable final channel evidence closes the prepared no-command guard");

        string finalCanceledRoot = Path.Combine(root, "final-canceled");
        var finalCanceledHost = new Host([disabled, disabled, disabled], false, true) { CancelRead = 3 };
        var finalCanceled = await EventChannelEnable.RunAsync("System", true, finalCanceledRoot, finalCanceledHost);
        Require(finalCanceled.Outcome == EventChannelEnableOutcome.Stopped && finalCanceledHost.Enables == 0 &&
            !File.Exists(Path.Combine(finalCanceledRoot, "event-channel-enable.pending.json")) &&
            Directory.GetFiles(finalCanceledRoot, "*.prepared.json").Length == 1,
            "Cancellation before enable dispatch closes the prepared no-command guard");

        var changed = enabled with { MaximumSize = enabled.MaximumSize + 1 };
        var uncertainRoot = Path.Combine(root, "changed");
        var uncertain = await EventChannelEnable.RunAsync("System", true, uncertainRoot, new Host([disabled, disabled, disabled, changed], false, true));
        Require(uncertain.Outcome == EventChannelEnableOutcome.Uncertain && File.Exists(Path.Combine(uncertainRoot, "event-channel-enable.pending.json")),
            "Collateral channel-setting changes remain unresolved");

        var interruptedRoot = Path.Combine(root, "interrupted");
        var interruptedHost = new Host([disabled, disabled, disabled], false, true) { FailEnable = true };
        var interrupted = await EventChannelEnable.RunAsync("System", true, interruptedRoot, interruptedHost);
        var recoveryHost = new Host([enabled], false, true);
        var recovered = await EventChannelEnable.ReconcileAsync(interruptedRoot, recoveryHost);
        Require(interrupted.Outcome == EventChannelEnableOutcome.Uncertain && recovered.Outcome == EventChannelEnableOutcome.Verified &&
            recoveryHost.Enables == 0, "Interrupted channel enablement reconciles without repeating the command");

        string duplicateRoot = Path.Combine(root, "duplicate-record");
        Directory.CreateDirectory(duplicateRoot);
        string duplicate = System.Text.Json.JsonSerializer.Serialize(new EventChannelPendingRecord(1, Environment.MachineName,
            DateTimeOffset.UtcNow, "System", disabled), Capture.JsonOptions).Insert(1, "\"schema\":1,");
        File.WriteAllText(Path.Combine(duplicateRoot, "event-channel-enable.pending.json"), duplicate);
        var duplicateHost = new Host([disabled], false, true);
        var duplicateResult = await EventChannelEnable.ReconcileAsync(duplicateRoot, duplicateHost);
        Require(duplicateResult.Outcome == EventChannelEnableOutcome.Uncertain && duplicateHost.Enables == 0,
            "Duplicate recovery fields are rejected without reading state or repeating a command");

        const string xml = "<channel xmlns='http://schemas.microsoft.com/win/2004/08/events' name='System' enabled='false'><logging><retention>false</retention><autoBackup>false</autoBackup><maxSize>20971520</maxSize></logging></channel>";
        var parsedBefore = EventChannelEnable.Parse("System", new(0, xml, "", false));
        var parsedAfter = EventChannelEnable.Parse("System", new(0, xml.Replace("enabled='false'", "enabled='true'"), "", false));
        Require(!parsedBefore.Enabled && parsedAfter.Enabled && parsedBefore.ConfigurationHash == parsedAfter.ConfigurationHash,
            "Configuration fingerprint excludes only enabled state");
        foreach (string invalid in new[] { "Security", "system", "System\n" })
        {
            try { EventChannelEnable.ValidateChannel(invalid); throw new Exception("Unsupported channel accepted"); }
            catch (ArgumentException) { }
        }
        Console.WriteLine("PASS: exact event channel enablement uses repeated policy and administrator gates, durable preparation, collateral-setting verification and no-repeat reconciliation; simulated provider only.");
    }

    private static EventChannelState State(bool enabled) => new("System", enabled, false, false, 20_971_520,
        new string('A', 64));
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }

    private sealed class Host(IEnumerable<EventChannelState> states, bool? managed, bool administrator) : IEventChannelEnableHost
    {
        private readonly Queue<EventChannelState> states = new(states);
        public bool IsAdministrator { get; } = administrator;
        internal int Enables;
        internal bool FailEnable;
        internal int FailRead;
        internal int CancelRead;
        private int reads;
        public Task<bool?> IsManagedAsync(string channel, CancellationToken token) => Task.FromResult(managed);
        public Task<EventChannelState> ReadAsync(string channel, CancellationToken token)
        {
            reads++;
            if (reads == FailRead) throw new InvalidDataException("fixture final inspection failure");
            if (reads == CancelRead) throw new OperationCanceledException("fixture final inspection cancellation");
            return Task.FromResult(states.Dequeue());
        }
        public Task<ToolResult> EnableAsync(string channel, CancellationToken token)
        {
            Enables++;
            if (FailEnable) throw new IOException("fixture interruption");
            return Task.FromResult(new ToolResult(0, "enabled", "", false));
        }
    }
}
