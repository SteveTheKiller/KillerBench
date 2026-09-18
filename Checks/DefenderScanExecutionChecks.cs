using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class DefenderScanExecutionChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "defender-scan");
        Directory.CreateDirectory(root);
        var before = DateTimeOffset.UtcNow.AddHours(-2);
        var eligible = new Host(History(false, before), History(false, before), History(true, before)) { Dispatch = new(0, "", "", false) };
        var started = await DefenderScanExecution.ExecuteAsync(Path.Combine(root, "started"), DefenderScanType.Quick, true, eligible);
        Require(started.Outcome == DefenderScanOutcome.Started && eligible.Starts == 1 && File.Exists(started.PendingPath),
            "Explicit eligible scan dispatch records pending state before one substituted request.");

        var denied = new Host(History(false, before), History(false, before)) { Administrator = false };
        var blocked = await DefenderScanExecution.ExecuteAsync(Path.Combine(root, "blocked"), DefenderScanType.Full, true, denied);
        Require(blocked.Outcome == DefenderScanOutcome.Blocked && denied.Starts == 0, "Missing administrator eligibility blocks before dispatch.");
        var unconfirmed = await DefenderScanExecution.ExecuteAsync(Path.Combine(root, "unconfirmed"), DefenderScanType.Full, false, eligible);
        Require(unconfirmed.Outcome == DefenderScanOutcome.Blocked && eligible.Starts == 1, "Missing explicit confirmation blocks another scan.");

        string changedFolder = Path.Combine(root, "changed");
        var changed = new Host(History(false, before), History(true, before));
        var changedResult = await DefenderScanExecution.ExecuteAsync(changedFolder, DefenderScanType.Quick, true, changed);
        Require(changedResult.Outcome == DefenderScanOutcome.Blocked && changed.Starts == 0 &&
            !File.Exists(Path.Combine(changedFolder, "defender-scan.pending.json")),
            "A scan that starts before the final boundary blocks dispatch and clears the unused guard.");

        string reconcileFolder = Path.Combine(root, "reconcile");
        var first = new Host(History(false, before), History(false, before)) { Dispatch = new(0, "", "", false) };
        var uncertain = await DefenderScanExecution.ExecuteAsync(reconcileFolder, DefenderScanType.Full, true, first);
        Require(uncertain.Outcome == DefenderScanOutcome.Inconclusive && first.Starts == 1, "Unproven dispatch retains the no-repeat guard.");
        var completed = new Host(History(false, DateTimeOffset.UtcNow.AddMinutes(1), full: true), History(false, before));
        var reconciled = await DefenderScanExecution.ReconcileAsync(reconcileFolder, completed);
        Require(reconciled.Outcome == DefenderScanOutcome.Verified && completed.Starts == 0 && !File.Exists(Path.Combine(reconcileFolder, "defender-scan.pending.json")),
            "Later completion resolves the guard without repeating a scan.");

        Require(typeof(WindowsDefenderScanHost).GetMethod(nameof(WindowsDefenderScanHost.StartAsync)) is not null,
            "Windows host exposes only the explicit typed scan boundary.");
        Console.WriteLine("PASS: Defender scan preview, explicit selection, final eligibility, durable no-repeat guard and read-only reconciliation; substituted providers only.");
    }

    private static WorkflowCheck Protection() => DefenderRecovery.Interpret(new(0, DefenderChecks.Fresh, "", false));
    private static WorkflowCheck History(bool active, DateTimeOffset end, bool full = false)
    {
        string? quickStart = full ? null : end.AddMinutes(-5).ToString("o");
        string? quickEnd = full ? null : end.ToString("o");
        string? fullStart = full ? end.AddMinutes(-30).ToString("o") : null;
        string? fullEnd = full ? end.ToString("o") : null;
        string json = JsonSerializer.Serialize(new { complete = true, collectedUtc = DateTimeOffset.UtcNow.ToString("o"), scanInProgress = active,
            quickStartUtc = quickStart, quickEndUtc = quickEnd, quickAgeDays = full ? (int?)null : 0, quickOverdue = false,
            fullStartUtc = fullStart, fullEndUtc = fullEnd, fullAgeDays = full ? 0 : (int?)null, fullOverdue = false });
        return new("Microsoft Defender scan history", CheckState.Information, "fixture", Evidence: new(0, json, "", false));
    }

    private sealed class Host(params WorkflowCheck[] histories) : IDefenderScanHost
    {
        private int historyReads;
        public bool Administrator { get; init; } = true;
        public bool PendingRestart { get; init; }
        public ToolResult Dispatch { get; init; } = new(0, "", "", false);
        public int Starts { get; private set; }
        public bool IsAdministrator() => Administrator;
        public bool RebootPending() => PendingRestart;
        public Task<WorkflowCheck> ReadProtectionAsync(CancellationToken token) { token.ThrowIfCancellationRequested(); return Task.FromResult(Protection()); }
        public Task<WorkflowCheck> ReadHistoryAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (histories.Length == 0) throw new InvalidOperationException("Fixture has no scan history.");
            return Task.FromResult(histories[Math.Min(historyReads++, histories.Length - 1)]);
        }
        public Task<ToolResult> StartAsync(DefenderScanType type, CancellationToken token) { token.ThrowIfCancellationRequested(); Starts++; return Task.FromResult(Dispatch); }
    }

    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
