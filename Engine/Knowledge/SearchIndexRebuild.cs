using System.Security.Principal;
using System.Text.Json;

namespace KillerBench.Engine;

public enum SearchIndexRebuildOutcome { Preview, Blocked, Verified, Uncertain, Stopped }
public enum SearchIndexReconciliationOutcome { NoPendingRecord, Verified, NotChanged, Inconclusive }

public sealed record SearchIndexServiceSnapshot(bool Complete, bool Exists, bool Running, string StartMode);
public sealed record SearchIndexDirectorySnapshot(bool Complete, string Path, bool Exists, bool ReparsePoint);
public sealed record SearchIndexRebuildPendingRecord(int Schema, string OperationId, DateTimeOffset StartedUtc,
    string MachineName, string OriginalPath, string RetainedPath, bool ServiceOriginallyRunning, string OriginalStartMode,
    string Stage);
public sealed record SearchIndexRebuildResult(SearchIndexRebuildOutcome Outcome, string Detail,
    string? JournalPath = null, string? RetainedPath = null)
{
    public int GetExitCode() => Outcome is SearchIndexRebuildOutcome.Preview or SearchIndexRebuildOutcome.Verified ? 0 :
        Outcome == SearchIndexRebuildOutcome.Blocked ? 2 : 3;
}
public sealed record SearchIndexReconciliationResult(SearchIndexReconciliationOutcome Outcome, string Detail,
    string? JournalPath = null)
{
    public int GetExitCode() => Outcome is SearchIndexReconciliationOutcome.NoPendingRecord or
        SearchIndexReconciliationOutcome.Verified or SearchIndexReconciliationOutcome.NotChanged ? 0 : 3;
}

public interface ISearchIndexRebuildHost
{
    bool IsAdministrator { get; }
    Task<WorkflowCheck> ReadInstallerActivityAsync(CancellationToken token);
    Task<WorkflowCheck> ReadRestartAsync(CancellationToken token);
    Task<SearchIndexServiceSnapshot> ReadServiceAsync(CancellationToken token);
    Task<SearchIndexDirectorySnapshot> ReadIndexAsync(CancellationToken token);
    Task<bool> DirectoryExistsAsync(string path, CancellationToken token);
    Task<ToolResult> StopServiceAsync(CancellationToken token);
    Task<ToolResult> StartServiceAsync(CancellationToken token);
    Task<ToolResult> RetainIndexAsync(string sourcePath, string retainedPath, CancellationToken token);
}

public static class SearchIndexRebuild
{
    public const string IndexPath = @"C:\ProgramData\Microsoft\Search\Data\Applications\Windows";
    private const string PendingName = "search-index-rebuild.pending.json";

    public static async Task<SearchIndexRebuildResult> ReviewAsync(ISearchIndexRebuildHost? host = null,
        CancellationToken token = default)
    {
        host ??= new WindowsSearchIndexRebuildHost();
        if (!host.IsAdministrator) return new(SearchIndexRebuildOutcome.Blocked,
            "Administrator rights are required before an index rebuild can be offered.");
        try
        {
            var activity = await host.ReadInstallerActivityAsync(token).ConfigureAwait(false);
            if (!Usable(activity, CheckState.Information)) return new(SearchIndexRebuildOutcome.Blocked,
                "Installer activity is active or unavailable. Wait for deployment work and collect fresh evidence.");
            var restart = await host.ReadRestartAsync(token).ConfigureAwait(false);
            if (!Usable(restart, CheckState.Clear)) return new(SearchIndexRebuildOutcome.Blocked,
                "Restart readiness is not clear. Restart if required and collect fresh evidence.");
            var service = await host.ReadServiceAsync(token).ConfigureAwait(false);
            if (!ValidService(service)) return new(SearchIndexRebuildOutcome.Blocked,
                "Windows Search must exist, be running, and remain enabled before its index can be rebuilt.");
            var index = await host.ReadIndexAsync(token).ConfigureAwait(false);
            if (!ValidIndex(index)) return new(SearchIndexRebuildOutcome.Blocked,
                "The exact Windows Search index directory could not be safely identified, is absent, or is a reparse point.");
            return new(SearchIndexRebuildOutcome.Preview,
                "Fresh evidence permits an explicitly confirmed Search index rebuild. The previous index will be retained for manual review.");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or TimeoutException or InvalidOperationException)
        {
            return new(SearchIndexRebuildOutcome.Blocked,
                "Search index rebuild eligibility could not be established: " + error.Message);
        }
    }

    public static WorkflowCheck ToWorkflowCheck(SearchIndexRebuildResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        bool available = result.Outcome == SearchIndexRebuildOutcome.Preview;
        return new("Windows Search index rebuild", available ? CheckState.Repairable : CheckState.Attention,
            result.Detail, available ? RepairKind.SearchIndexRebuild : null);
    }

    public static async Task<SearchIndexRebuildResult> RunAsync(bool execute, string journalDirectory,
        ISearchIndexRebuildHost? host = null, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalDirectory);
        if (!execute) return new(SearchIndexRebuildOutcome.Preview,
            "Preview only. KillerBench will require fresh idle servicing and restart evidence, stop Windows Search, retain the existing SystemIndex directory, restart the service and verify a new index directory. The retained index will not be deleted automatically.");

        host ??= new WindowsSearchIndexRebuildHost();
        string directory = Path.GetFullPath(journalDirectory);
        string pendingPath = Path.Combine(directory, PendingName);
        if (File.Exists(pendingPath)) return new(SearchIndexRebuildOutcome.Blocked,
            "A prepared Search index rebuild requires read-only reconciliation before another rebuild can run.", pendingPath);
        if (!host.IsAdministrator) return new(SearchIndexRebuildOutcome.Blocked,
            "Administrator rights are required. No Search service or index change was attempted.");

        progress?.Report("Checking servicing activity and restart state");
        var activity = await host.ReadInstallerActivityAsync(token).ConfigureAwait(false);
        if (!Usable(activity, CheckState.Information)) return Blocked("Installer activity is active or unavailable. Wait for deployment work and collect fresh evidence.");
        var restart = await host.ReadRestartAsync(token).ConfigureAwait(false);
        if (!Usable(restart, CheckState.Clear)) return Blocked("Restart readiness is not clear. Restart if required and collect fresh evidence.");
        var service = await host.ReadServiceAsync(token).ConfigureAwait(false);
        if (!ValidService(service)) return Blocked("Windows Search must exist, be running, and remain enabled before its index can be rebuilt.");
        var index = await host.ReadIndexAsync(token).ConfigureAwait(false);
        if (!ValidIndex(index)) return Blocked("The exact Windows Search index directory could not be safely identified, is absent, or is a reparse point.");

        string operationId = Guid.NewGuid().ToString("N");
        string retainedPath = IndexPath + ".KillerBench." + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss") + "." + operationId[..8];
        var record = new SearchIndexRebuildPendingRecord(1, operationId, DateTimeOffset.UtcNow, Environment.MachineName,
            IndexPath, retainedPath, service.Running, service.StartMode, "Prepared");
        Directory.CreateDirectory(directory);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(record, Capture.JsonOptions), token).ConfigureAwait(false);
        bool mutationDispatched = false;

        try
        {
            progress?.Report("Repeating Search index rebuild eligibility immediately before stopping Windows Search");
            var immediateActivity = await host.ReadInstallerActivityAsync(token).ConfigureAwait(false);
            var immediateRestart = await host.ReadRestartAsync(token).ConfigureAwait(false);
            var immediateService = await host.ReadServiceAsync(token).ConfigureAwait(false);
            var immediateIndex = await host.ReadIndexAsync(token).ConfigureAwait(false);
            if (!Usable(immediateActivity, CheckState.Information) || !Usable(immediateRestart, CheckState.Clear) ||
                !ValidService(immediateService) || immediateService.StartMode != service.StartMode || !ValidIndex(immediateIndex))
            {
                string blockedPath = Path.Combine(directory, $"search-index-rebuild.{operationId}.blocked.json");
                await JournalPersistence.WriteAsync(blockedPath, JsonSerializer.Serialize(new
                {
                    Stage = "FinalEligibility",
                    Initial = new { Activity = activity, Restart = restart, Service = service, Index = index },
                    Current = new { Activity = immediateActivity, Restart = immediateRestart, Service = immediateService, Index = immediateIndex },
                    Detail = "Final servicing, restart, service or index evidence no longer authorizes a rebuild. No Search service or index change was attempted."
                }, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, blockedPath + ".prepared.json");
                return new(SearchIndexRebuildOutcome.Blocked,
                    "Final servicing, restart, service or index evidence no longer authorizes a rebuild. No Search service or index change was attempted.",
                    blockedPath, retainedPath);
            }

            progress?.Report("Stopping Windows Search");
            mutationDispatched = true;
            if (!Complete(await host.StopServiceAsync(token).ConfigureAwait(false))) return Uncertain("Windows Search did not stop cleanly. The preparation record was retained.");
            var stopped = await host.ReadServiceAsync(token).ConfigureAwait(false);
            if (!stopped.Complete || !stopped.Exists || stopped.Running || stopped.StartMode != service.StartMode)
                return Uncertain("The stopped service state could not be verified exactly. The preparation record was retained.");

            record = record with { Stage = "ServiceStopped" };
            await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(record, Capture.JsonOptions), token).ConfigureAwait(false);
            progress?.Report("Retaining the existing Search index");
            if (!Complete(await host.RetainIndexAsync(IndexPath, retainedPath, token).ConfigureAwait(false)))
            {
                await host.StartServiceAsync(CancellationToken.None).ConfigureAwait(false);
                return Uncertain("The existing index could not be retained. Windows Search restart was attempted and the preparation record was retained.");
            }

            record = record with { Stage = "IndexRetained" };
            await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(record, Capture.JsonOptions), token).ConfigureAwait(false);
            progress?.Report("Restarting Windows Search");
            if (!Complete(await host.StartServiceAsync(token).ConfigureAwait(false))) return Uncertain("Windows Search did not restart cleanly. The retained index and preparation record were preserved.");
            var afterService = await host.ReadServiceAsync(token).ConfigureAwait(false);
            var afterIndex = await host.ReadIndexAsync(token).ConfigureAwait(false);
            bool retainedExists = await host.DirectoryExistsAsync(retainedPath, token).ConfigureAwait(false);
            if (!ValidService(afterService) || afterService.StartMode != service.StartMode || !ValidIndex(afterIndex) || !retainedExists)
                return Uncertain("The rebuilt Search index could not be verified completely. The retained index and preparation record were preserved.");

            string verifiedPath = Path.Combine(directory, $"search-index-rebuild.{operationId}.verified.json");
            record = record with { Stage = "Verified" };
            await JournalPersistence.WriteAsync(verifiedPath, JsonSerializer.Serialize(record, Capture.JsonOptions), token).ConfigureAwait(false);
            File.Delete(pendingPath);
            return new(SearchIndexRebuildOutcome.Verified,
                "Windows Search restarted with a new exact index directory. The previous index remains retained for manual review and was not deleted.", verifiedPath, retainedPath);
        }
        catch (OperationCanceledException)
        {
            if (!mutationDispatched)
            {
                string stoppedPath = Path.Combine(directory, $"search-index-rebuild.{operationId}.stopped.json");
                await JournalPersistence.WriteAsync(stoppedPath, JsonSerializer.Serialize(new
                {
                    Stage = "FinalEligibility",
                    Detail = "The final Search index rebuild check was canceled before any service or index change was requested."
                }, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, stoppedPath + ".prepared.json");
                return new(SearchIndexRebuildOutcome.Stopped,
                    "The final check was canceled before any Search service or index change was requested.", stoppedPath, retainedPath);
            }
            return new(SearchIndexRebuildOutcome.Stopped,
                "The operation was canceled. Current state is uncertain, so the retained data and preparation record were preserved for read-only reconciliation.", pendingPath, retainedPath);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or TimeoutException or InvalidOperationException)
        {
            if (!mutationDispatched)
            {
                string blockedPath = Path.Combine(directory, $"search-index-rebuild.{operationId}.blocked.json");
                await JournalPersistence.WriteAsync(blockedPath, JsonSerializer.Serialize(new
                {
                    Stage = "FinalEligibility",
                    Error = error.GetType().Name,
                    Detail = "Final Search index rebuild evidence was unavailable before any service or index change was requested."
                }, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, blockedPath + ".prepared.json");
                return new(SearchIndexRebuildOutcome.Blocked,
                    "Final Search index rebuild evidence was unavailable before any service or index change was requested.", blockedPath, retainedPath);
            }
            return Uncertain("The rebuild stopped with " + error.GetType().Name + ". Current state is uncertain, so the preparation record was preserved.");
        }

        SearchIndexRebuildResult Blocked(string detail) => new(SearchIndexRebuildOutcome.Blocked, detail);
        SearchIndexRebuildResult Uncertain(string detail) => new(SearchIndexRebuildOutcome.Uncertain, detail, pendingPath, retainedPath);
    }

    internal static bool ValidService(SearchIndexServiceSnapshot value) => value.Complete && value.Exists && value.Running &&
        value.StartMode is "Automatic" or "AutomaticDelayedStart" or "Manual";
    internal static bool ValidIndex(SearchIndexDirectorySnapshot value) => value.Complete && value.Exists && !value.ReparsePoint &&
        string.Equals(value.Path, IndexPath, StringComparison.OrdinalIgnoreCase);
    private static bool Usable(WorkflowCheck check, CheckState expected) => check.State == expected && check.Evidence is { ExitCode: 0, TimedOut: false, Truncated: false } evidence &&
        string.IsNullOrWhiteSpace(evidence.Error);
    private static bool Complete(ToolResult result) => result.ExitCode == 0 && !result.TimedOut && !result.Truncated && string.IsNullOrWhiteSpace(result.Error);
}

public static class SearchIndexRebuildReconciliation
{
    public static async Task<SearchIndexReconciliationResult> RunAsync(string journalDirectory,
        ISearchIndexRebuildHost? host = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalDirectory);
        string directory = Path.GetFullPath(journalDirectory);
        string pendingPath = Path.Combine(directory, "search-index-rebuild.pending.json");
        if (!File.Exists(pendingPath)) return new(SearchIndexReconciliationOutcome.NoPendingRecord, "No prepared Search index rebuild exists.");
        SearchIndexRebuildPendingRecord? record;
        try { record = JsonSerializer.Deserialize<SearchIndexRebuildPendingRecord>(await File.ReadAllTextAsync(pendingPath, token).ConfigureAwait(false), Capture.JsonOptions); }
        catch (Exception error) when (error is IOException or JsonException or UnauthorizedAccessException)
        { return new(SearchIndexReconciliationOutcome.Inconclusive, "The preparation record could not be read safely.", pendingPath); }
        if (record is null || record.Schema != 1 || record.MachineName != Environment.MachineName ||
            !string.Equals(record.OriginalPath, SearchIndexRebuild.IndexPath, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(record.RetainedPath))
            return new(SearchIndexReconciliationOutcome.Inconclusive, "The preparation record is invalid or belongs to another machine.", pendingPath);

        host ??= new WindowsSearchIndexRebuildHost();
        try
        {
            var service = await host.ReadServiceAsync(token).ConfigureAwait(false);
            var index = await host.ReadIndexAsync(token).ConfigureAwait(false);
            bool retained = await host.DirectoryExistsAsync(record.RetainedPath, token).ConfigureAwait(false);
            SearchIndexReconciliationOutcome outcome;
            string detail;
            if (retained && SearchIndexRebuild.ValidService(service) && service.StartMode == record.OriginalStartMode && SearchIndexRebuild.ValidIndex(index))
            { outcome = SearchIndexReconciliationOutcome.Verified; detail = "Fresh evidence verifies the new index, restored service state and retained prior index. No repair command was repeated."; }
            else if (!retained && SearchIndexRebuild.ValidService(service) && service.StartMode == record.OriginalStartMode && SearchIndexRebuild.ValidIndex(index))
            { outcome = SearchIndexReconciliationOutcome.NotChanged; detail = "Fresh evidence shows the original index remained in place. No repair command was repeated."; }
            else return new(SearchIndexReconciliationOutcome.Inconclusive,
                "Fresh evidence cannot prove either a verified rebuild or an unchanged original state. No repair command was repeated.", pendingPath);
            string archive = Path.Combine(directory, $"search-index-rebuild.{record.OperationId}.reconciled.json");
            await JournalPersistence.WriteAsync(archive, JsonSerializer.Serialize(record with { Stage = outcome.ToString() }, Capture.JsonOptions), token).ConfigureAwait(false);
            File.Delete(pendingPath);
            return new(outcome, detail, archive);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or TimeoutException or InvalidOperationException or OperationCanceledException)
        { return new(SearchIndexReconciliationOutcome.Inconclusive, "Reconciliation was incomplete. The preparation record remains and no repair command was repeated.", pendingPath); }
    }
}

public sealed class WindowsSearchIndexRebuildHost : ISearchIndexRebuildHost
{
    public bool IsAdministrator => OperatingSystem.IsWindows() && new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    public Task<WorkflowCheck> ReadInstallerActivityAsync(CancellationToken token) => InstallerActivityDiagnostics.CheckAsync(token);
    public Task<WorkflowCheck> ReadRestartAsync(CancellationToken token) => Task.FromResult(RestartDiagnostics.Check());
    public async Task<SearchIndexServiceSnapshot> ReadServiceAsync(CancellationToken token)
    {
        const string script = "$ErrorActionPreference='Stop';$s=Get-CimInstance Win32_Service -Filter \"Name='WSearch'\" -OperationTimeoutSec 15; if($null -eq $s){[pscustomobject]@{complete=$true;exists=$false;running=$false;startMode=''}|ConvertTo-Json -Compress;exit};$mode=if($s.StartMode -eq 'Auto' -and $s.DelayedAutoStart){'AutomaticDelayedStart'}elseif($s.StartMode -eq 'Auto'){'Automatic'}elseif($s.StartMode -eq 'Manual'){'Manual'}elseif($s.StartMode -eq 'Disabled'){'Disabled'}else{[string]$s.StartMode};[pscustomobject]@{complete=$true;exists=$true;running=($s.State -eq 'Running');startMode=$mode}|ConvertTo-Json -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true), TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
        if (!Complete(result)) return new(false, false, false, "");
        try { return JsonSerializer.Deserialize<SearchIndexServiceSnapshot>(result.Output, Capture.JsonOptions) ?? new(false, false, false, ""); }
        catch (JsonException) { return new(false, false, false, ""); }
    }
    public Task<SearchIndexDirectorySnapshot> ReadIndexAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            bool exists = Directory.Exists(SearchIndexRebuild.IndexPath);
            bool reparse = exists && (File.GetAttributes(SearchIndexRebuild.IndexPath) & FileAttributes.ReparsePoint) != 0;
            return Task.FromResult(new SearchIndexDirectorySnapshot(true, SearchIndexRebuild.IndexPath, exists, reparse));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return Task.FromResult(new SearchIndexDirectorySnapshot(false, SearchIndexRebuild.IndexPath, false, false)); }
    }
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken token) { token.ThrowIfCancellationRequested(); return Task.FromResult(Directory.Exists(path)); }
    public Task<ToolResult> StopServiceAsync(CancellationToken token) => ServiceAsync("stop", token);
    public Task<ToolResult> StartServiceAsync(CancellationToken token) => ServiceAsync("start", token);
    public Task<ToolResult> RetainIndexAsync(string sourcePath, string retainedPath, CancellationToken token)
    {
        const string script = "& { param($source,$retained) $ErrorActionPreference='Stop';if(-not [IO.Path]::GetFullPath($source).Equals('C:\\ProgramData\\Microsoft\\Search\\Data\\Applications\\Windows',[StringComparison]::OrdinalIgnoreCase)){throw 'Unexpected source'};if(Test-Path -LiteralPath $retained){throw 'Retained path already exists'};Move-Item -LiteralPath $source -Destination $retained -ErrorAction Stop }";
        return BoundedTool.RunAsync(new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script, sourcePath, retainedPath], true), TimeSpan.FromSeconds(45), token: token);
    }
    private static Task<ToolResult> ServiceAsync(string action, CancellationToken token)
    {
        const string script = "& { param($action) $ErrorActionPreference='Stop';$s=Get-Service -Name WSearch -ErrorAction Stop;if($action -eq 'stop'){if($s.Status -ne 'Stopped'){Stop-Service -InputObject $s -ErrorAction Stop};$s.WaitForStatus([ServiceProcess.ServiceControllerStatus]::Stopped,[TimeSpan]::FromSeconds(30))}else{if($s.Status -ne 'Running'){Start-Service -InputObject $s -ErrorAction Stop};$s.WaitForStatus([ServiceProcess.ServiceControllerStatus]::Running,[TimeSpan]::FromSeconds(30))} }";
        return BoundedTool.RunAsync(new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script, action], true), TimeSpan.FromSeconds(45), token: token);
    }
    private static bool Complete(ToolResult result) => result.ExitCode == 0 && !result.TimedOut && !result.Truncated && string.IsNullOrWhiteSpace(result.Error);
}
