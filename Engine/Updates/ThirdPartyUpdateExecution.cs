using System.Text;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record ThirdPartyUpdateSelection(string Id, string Source, string TargetVersion);
public sealed record ThirdPartyUpdatePlan(ThirdPartyUpdateSelection Selection, bool Eligible, IReadOnlyList<string> Blockers)
{
    public string Format() => "THIRD-PARTY APPLICATION UPDATE / PREVIEW ONLY\n" +
        $"Package: {Selection.Id}\nSource: {Selection.Source}\nTarget version: {Selection.TargetVersion}\nEligible: {Eligible}\n" +
        (Blockers.Count == 0 ? "Fresh discovery found the exact package and target version." : string.Join("\n", Blockers.Select(value => "BLOCKED: " + value))) +
        "\n\nExecution updates only this exact package and version. It uses silent mode, disables interactive prompts, does not allow an automatic reboot, and verifies the installed version afterward.";
}

public sealed record ThirdPartyUpdateOutcome(ThirdPartyUpdateSelection Selection, bool Verified, string Detail);
public sealed record ThirdPartyUpdateExecutionResult(string Status, string JournalPath, ThirdPartyUpdateOutcome? Outcome, IReadOnlyList<string> Details)
{
    public int GetExitCode() => Status == "Stopped" ? 130 : Status == "Completed" && Outcome?.Verified == true ? 0 : 3;
    public string Format() => $"THIRD-PARTY APPLICATION UPDATE / {Status}\nJournal: {JournalPath}\n\n" +
        string.Join("\n", Details) + (Outcome is null ? "" : $"\n\n{Outcome.Selection.Id} {Outcome.Selection.TargetVersion}\nVerified: {Outcome.Verified}\n{Outcome.Detail}") +
        "\n\nKillerBench did not request an automatic reboot. Review the application and restart Windows only if the vendor requires it.";
}

public enum ThirdPartyUpdateRecoveryOutcome { NoPendingRecord, Verified, StillPending, InvalidRecord }
public sealed record ThirdPartyUpdateRecoveryResult(ThirdPartyUpdateRecoveryOutcome Outcome, string Detail, string PendingPath);

public interface IThirdPartyUpdateExecutionHost
{
    Task<ThirdPartyUpdateResult> DiscoverAsync(CancellationToken token);
    Task<ToolResult> UpgradeAsync(ThirdPartyUpdateSelection selection, CancellationToken token);
    Task<string?> ReadInstalledVersionAsync(string id, string source, CancellationToken token);
}

public static class ThirdPartyUpdatePlanning
{
    public static ThirdPartyUpdatePlan Create(ThirdPartyUpdateResult discovery, ThirdPartyUpdateSelection selection)
    {
        ArgumentNullException.ThrowIfNull(discovery);
        Validate(selection);
        var blockers = new List<string>();
        if (!discovery.ProviderAvailable) blockers.Add("Microsoft.WinGet.Client is unavailable.");
        if (!discovery.Complete) blockers.Add("Discovery is incomplete.");
        var matches = discovery.Candidates.Where(candidate =>
            string.Equals(candidate.Id, selection.Id, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(candidate.Source, selection.Source, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1) blockers.Add(matches.Length == 0 ? "The exact package and source were not found as an update candidate." : "The exact package and source were ambiguous.");
        else
        {
            if (string.Equals(matches[0].InstalledVersion, selection.TargetVersion, StringComparison.OrdinalIgnoreCase))
                blockers.Add("The target version is already installed.");
            if (!matches[0].AvailableVersions.Contains(selection.TargetVersion, StringComparer.OrdinalIgnoreCase))
                blockers.Add("The exact target version was not offered by fresh discovery.");
        }
        return new(selection, blockers.Count == 0, blockers.AsReadOnly());
    }

    internal static void Validate(ThirdPartyUpdateSelection selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (!Valid(selection.Id, 256) || !Valid(selection.Source, 128) || !Valid(selection.TargetVersion, 256))
            throw new ArgumentException("Package ID, source and target version must be nonempty trimmed text without control characters.");
    }

    private static bool Valid(string value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum && value == value.Trim() && !value.Any(char.IsControl);
}

public static class ThirdPartyUpdateExecution
{
    private sealed record PendingRecord(int Schema, string Machine, string Journal, ThirdPartyUpdateSelection Selection, DateTimeOffset PreparedUtc);

    public static async Task<ThirdPartyUpdateExecutionResult> RunAsync(ThirdPartyUpdateSelection selection, bool acceptAgreements,
        string journalFolder, IThirdPartyUpdateExecutionHost host, IProgress<string>? progress = null, CancellationToken token = default)
        => await RunBoundedAsync(selection, acceptAgreements, journalFolder, host, TimeSpan.FromHours(1), progress, token).ConfigureAwait(false);

    public static async Task<ThirdPartyUpdateExecutionResult> RunBoundedAsync(ThirdPartyUpdateSelection selection, bool acceptAgreements,
        string journalFolder, IThirdPartyUpdateExecutionHost host, TimeSpan executionLimit, IProgress<string>? progress = null,
        CancellationToken token = default)
    {
        ThirdPartyUpdatePlanning.Validate(selection);
        ArgumentNullException.ThrowIfNull(host);
        if (!acceptAgreements) throw new ArgumentException("Execution requires explicit package and source agreement acceptance.");
        if (executionLimit <= TimeSpan.Zero || executionLimit > TimeSpan.FromHours(1)) throw new ArgumentOutOfRangeException(nameof(executionLimit));
        token.ThrowIfCancellationRequested();
        Directory.CreateDirectory(journalFolder);
        using var gate = new FileStream(Path.Combine(journalFolder, "third-party-update.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(journalFolder, "third-party-update.pending.json");
        if (File.Exists(pendingPath)) throw new InvalidOperationException("A prior third-party update needs reconciliation before another update can start.");
        string journalPath = Path.Combine(journalFolder, "third-party-update-" + Guid.NewGuid().ToString("N") + ".jsonl");
        using var journal = new FileStream(journalPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
        var details = new List<string>();
        ThirdPartyUpdateOutcome? outcome = null;
        bool dispatched = false;
        void ArchivePrepared()
        {
            if (dispatched || !File.Exists(pendingPath)) return;
            using var prepared = RecoveryRecordFiles.OpenLocked(pendingPath);
            RecoveryRecordFiles.Archive(prepared, journalPath + ".prepared.json");
        }
        void Record(object value)
        {
            byte[] data = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Capture.JsonOptions) + "\n");
            journal.Write(data); journal.Flush(true);
        }
        Record(new { Stage = "Started", Selection = selection });
        try
        {
            Report(progress, "Checking the exact application update against fresh discovery...");
            var initial = await ReadBounded(host.DiscoverAsync, TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
            var plan = ThirdPartyUpdatePlanning.Create(initial, selection);
            Record(new { Stage = "Plan", Plan = plan });
            if (!plan.Eligible) return Finish("Blocked", plan.Blockers);
            using (var pending = new FileStream(pendingPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
            {
                JsonSerializer.Serialize(pending, new PendingRecord(1, Environment.MachineName, journalPath, selection, DateTimeOffset.UtcNow), Capture.JsonOptions);
                pending.Flush(true);
            }
            using (var locked = RecoveryRecordFiles.OpenLocked(pendingPath))
            {
                var final = await ReadBounded(host.DiscoverAsync, TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
                var finalPlan = ThirdPartyUpdatePlanning.Create(final, selection);
                Record(new { Stage = "FinalEligibility", Plan = finalPlan });
                if (!finalPlan.Eligible)
                {
                    RecoveryRecordFiles.Archive(locked, journalPath + ".prepared.json");
                    return Finish("Blocked", finalPlan.Blockers);
                }
            }
            Report(progress, $"Updating {selection.Id} to exact version {selection.TargetVersion}...");
            dispatched = true;
            var tool = await ReadBounded(t => host.UpgradeAsync(selection, t), executionLimit, token, selection).ConfigureAwait(false);
            Record(new { Stage = "ProviderResult", Result = tool });
            string? installed = await ReadBounded(t => host.ReadInstalledVersionAsync(selection.Id, selection.Source, t), TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
            bool verified = tool.ExitCode == 0 && !tool.TimedOut && !tool.Truncated && string.IsNullOrWhiteSpace(tool.Error) &&
                string.Equals(installed, selection.TargetVersion, StringComparison.OrdinalIgnoreCase);
            string detail = verified ? "The provider returned success and fresh exact-package inventory reports the requested version." :
                "The requested version was not verified. The pending record remains and must be reconciled before another third-party update.";
            outcome = new(selection, verified, detail);
            Record(new { Stage = "Verification", InstalledVersion = installed, Outcome = outcome });
            if (verified) File.Delete(pendingPath);
            return Finish(verified ? "Completed" : "Needs review", []);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            ArchivePrepared();
            details.Add(dispatched ? "Operation stopped after provider dispatch. Verification is required before retrying." : "Operation stopped before provider dispatch.");
            return Finish("Stopped", []);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException or InvalidOperationException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException)
        {
            ArchivePrepared();
            details.Add(error.Message);
            return Finish(dispatched ? "Needs review" : "Blocked", []);
        }

        ThirdPartyUpdateExecutionResult Finish(string status, IEnumerable<string> more)
        {
            details.AddRange(more);
            var result = new ThirdPartyUpdateExecutionResult(status, journalPath, outcome, details.AsReadOnly());
            Record(new { Stage = "Finished", Result = result });
            return result;
        }
    }

    public static async Task<ThirdPartyUpdateRecoveryResult> ReconcileAsync(string journalFolder, IThirdPartyUpdateExecutionHost host, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        string pendingPath = Path.Combine(journalFolder, "third-party-update.pending.json");
        Directory.CreateDirectory(journalFolder);
        using var gate = new FileStream(Path.Combine(journalFolder, "third-party-update.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (!File.Exists(pendingPath)) return new(ThirdPartyUpdateRecoveryOutcome.NoPendingRecord, "No pending third-party update was found.", pendingPath);
        try
        {
            using var recordFile = RecoveryRecordFiles.OpenLocked(pendingPath);
            using var reader = new StreamReader(recordFile, Encoding.UTF8, true, 4096, true);
            string json = await reader.ReadToEndAsync(token).ConfigureAwait(false);
            if (json.Length > 65_536) throw new InvalidDataException("Pending record exceeds its size bound.");
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object || document.RootElement.EnumerateObject().Count() != 5 ||
                document.RootElement.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != 5)
                throw new InvalidDataException("Pending record fields are invalid or duplicated.");
            var record = JsonSerializer.Deserialize<PendingRecord>(json, Capture.JsonOptions) ?? throw new InvalidDataException("Pending record is empty.");
            if (record.Schema != 1 || !string.Equals(record.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(record.Journal) || !Path.IsPathFullyQualified(record.Journal)) throw new InvalidDataException("Pending record identity is invalid.");
            ThirdPartyUpdatePlanning.Validate(record.Selection);
            string? installed = await ReadBounded(t => host.ReadInstalledVersionAsync(record.Selection.Id, record.Selection.Source, t), TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
            if (!string.Equals(installed, record.Selection.TargetVersion, StringComparison.OrdinalIgnoreCase))
                return new(ThirdPartyUpdateRecoveryOutcome.StillPending, "The exact target version is not verified. No update was retried.", pendingPath);
            RecoveryRecordFiles.Archive(recordFile, record.Journal + ".recovered.json");
            return new(ThirdPartyUpdateRecoveryOutcome.Verified, "Fresh exact-package inventory reports the requested version. The pending record was archived without retrying the update.", pendingPath);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is JsonException or IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or NotSupportedException)
        { return new(ThirdPartyUpdateRecoveryOutcome.InvalidRecord, "Pending evidence could not be safely reconciled: " + error.Message, pendingPath); }
    }

    private static async Task<T> ReadBounded<T>(Func<CancellationToken, Task<T>> read, TimeSpan limit, CancellationToken token, ThirdPartyUpdateSelection? selection = null)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(limit);
        try
        {
            var task = read(deadline.Token) ?? throw new InvalidOperationException("Provider returned no task.");
            return await task.WaitAsync(deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        { throw new TimeoutException((selection is null ? "Prerequisite" : "Application update") + " timed out."); }
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); } catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsThirdPartyUpdateExecutionHost : IThirdPartyUpdateExecutionHost
{
    public Task<ThirdPartyUpdateResult> DiscoverAsync(CancellationToken token) => ThirdPartyUpdateDiscovery.ReadAsync(token: token);

    public Task<ToolResult> UpgradeAsync(ThirdPartyUpdateSelection selection, CancellationToken token)
        => BoundedTool.RunAsync(UpgradeStep(selection), TimeSpan.FromHours(1), token: token);

    public async Task<string?> ReadInstalledVersionAsync(string id, string source, CancellationToken token)
    {
        ThirdPartyUpdatePlanning.Validate(new(id, source, "fixture"));
        string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';Import-Module Microsoft.WinGet.Client -ErrorAction Stop;" +
            "$rows=@(Get-WinGetPackage -Count 4097 -ErrorAction Stop|Where-Object{[string]$_.Id -ieq '" + Ps(id) + "' -and [string]$_.Source -ieq '" + Ps(source) + "'});" +
            "if($rows.Count-ne 1){[pscustomobject]@{count=$rows.Count;version=''}|ConvertTo-Json -Compress;exit 0};" +
            "[pscustomobject]@{count=1;version=[string]$rows[0].InstalledVersion}|ConvertTo-Json -Compress";
        var result = await BoundedTool.RunAsync(new("pwsh.exe", ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true), TimeSpan.FromMinutes(5), token: token).ConfigureAwait(false);
        return InterpretInstalledVersion(result);
    }

    public static RepairStep UpgradeStep(ThirdPartyUpdateSelection selection)
    {
        ThirdPartyUpdatePlanning.Validate(selection);
        return new("winget.exe", ["upgrade", "--id", selection.Id, "--exact", "--source", selection.Source, "--version", selection.TargetVersion,
            "--silent", "--disable-interactivity", "--accept-source-agreements", "--accept-package-agreements"], false);
    }

    public static string? InterpretInstalledVersion(ToolResult result)
    {
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error) || result.Output.Length > 4096) return null;
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !root.TryGetProperty("count", out var count) || !count.TryGetInt32(out int value) || value != 1 ||
                !root.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.String || version.GetString() is not { } text ||
                string.IsNullOrWhiteSpace(text) || text.Length > 256 || text != text.Trim() || text.Any(char.IsControl)) return null;
            return text;
        }
        catch (JsonException) { return null; }
    }

    private static string Ps(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
