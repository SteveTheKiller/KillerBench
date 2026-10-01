using System.Text.Json;

namespace KillerBench.Engine.Security;

public enum DefenderScanType { Quick, Full }
public enum DefenderScanOutcome { Preview, Started, Verified, Blocked, Inconclusive }
public sealed record DefenderScanResult(DefenderScanType Type, DefenderScanOutcome Outcome, string Detail,
    string? PendingPath = null, ToolResult? DispatchEvidence = null);
public sealed record DefenderScanPending(int Schema, string Machine, DefenderScanType Type, DateTimeOffset PreparedUtc,
    DateTimeOffset? PreviousCompletionUtc, bool CommandDispatched);

public interface IDefenderScanHost
{
    bool IsAdministrator();
    bool RebootPending();
    Task<WorkflowCheck> ReadProtectionAsync(CancellationToken token);
    Task<WorkflowCheck> ReadHistoryAsync(CancellationToken token);
    Task<ToolResult> StartAsync(DefenderScanType type, CancellationToken token);
}

public static class DefenderScanExecution
{
    private static readonly JsonSerializerOptions Json = new(Capture.JsonOptions) { RespectRequiredConstructorParameters = true };

    public static async Task<DefenderScanResult> PreviewAsync(DefenderScanType type, IDefenderScanHost host,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        var (eligible, detail, _) = await InspectAsync(type, host, token).ConfigureAwait(false);
        return new(type, eligible ? DefenderScanOutcome.Preview : DefenderScanOutcome.Blocked, detail);
    }

    public static async Task<DefenderScanResult> ExecuteAsync(string folder, DefenderScanType type, bool confirmed,
        IDefenderScanHost host, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(host);
        if (!confirmed) return new(type, DefenderScanOutcome.Blocked, "Explicit scan confirmation is required.");
        string pendingPath = Path.Combine(folder, "defender-scan.pending.json");
        if (File.Exists(pendingPath)) return new(type, DefenderScanOutcome.Blocked,
            "A previous Defender scan request is unresolved. Reconcile it before starting another scan.", pendingPath);

        var (firstEligible, firstDetail, firstHistory) = await InspectAsync(type, host, token).ConfigureAwait(false);
        if (!firstEligible) return new(type, DefenderScanOutcome.Blocked, firstDetail);
        var pending = new DefenderScanPending(1, Environment.MachineName, type, DateTimeOffset.UtcNow,
            Completion(firstHistory!, type), false);
        Directory.CreateDirectory(folder);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending, Capture.JsonOptions), token).ConfigureAwait(false);

        (bool Eligible, string Detail, DefenderScanHistoryObservation? History) final;
        try { final = await InspectAsync(type, host, token).ConfigureAwait(false); }
        catch (OperationCanceledException)
        {
            await ResolveAsync(pendingPath, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        {
            await ResolveAsync(pendingPath, CancellationToken.None).ConfigureAwait(false);
            return new(type, DefenderScanOutcome.Blocked,
                "Final eligibility could not be confirmed (" + error.GetType().Name + "). No scan was started.", pendingPath);
        }
        if (!final.Eligible)
        {
            await ResolveAsync(pendingPath, token).ConfigureAwait(false);
            return new(type, DefenderScanOutcome.Blocked,
                "Final eligibility changed after preparation. No scan was started.", pendingPath);
        }
        pending = pending with { CommandDispatched = true };
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending, Capture.JsonOptions), token).ConfigureAwait(false);
        ToolResult dispatch;
        try
        {
            dispatch = await (host.StartAsync(type, token) ?? throw new InvalidOperationException("Defender scan provider returned no task."))
                .WaitAsync(TimeSpan.FromSeconds(45), token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
        {
            return new(type, DefenderScanOutcome.Inconclusive,
                "The scan request did not return complete dispatch evidence (" + error.GetType().Name + "). The pending record remains active; do not immediately retry.", pendingPath);
        }
        if (dispatch.ExitCode != 0 || dispatch.TimedOut || dispatch.Truncated || !string.IsNullOrWhiteSpace(dispatch.Error))
            return new(type, DefenderScanOutcome.Inconclusive,
                "Microsoft Defender did not return clean scan-dispatch evidence. The pending record remains active; do not immediately retry.", pendingPath, dispatch);

        var after = await host.ReadHistoryAsync(token).ConfigureAwait(false);
        if (TryHistory(after, out var observation) && Completion(observation, type) is { } completed && completed > pending.PreparedUtc)
        {
            await ResolveAsync(pendingPath, token).ConfigureAwait(false);
            return new(type, DefenderScanOutcome.Verified, "Microsoft Defender reports a newer completed " + Label(type) + " scan.", pendingPath, dispatch);
        }
        if (TryHistory(after, out observation) && observation.ScanInProgress)
            return new(type, DefenderScanOutcome.Started, "Microsoft Defender reports a scan in progress. The pending record remains until later evidence confirms completion.", pendingPath, dispatch);
        return new(type, DefenderScanOutcome.Inconclusive,
            "The scan request was dispatched, but current history does not yet prove that it started or completed. The pending record remains active; do not immediately retry.", pendingPath, dispatch);
    }

    public static async Task<DefenderScanResult> ReconcileAsync(string folder, IDefenderScanHost host,
        CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentNullException.ThrowIfNull(host);
        string path = Path.Combine(folder, "defender-scan.pending.json");
        DefenderScanPending pending;
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists) return new(DefenderScanType.Quick, DefenderScanOutcome.Preview, "No pending Defender scan request was found.");
            if (info.Length is <= 0 or > 16_384) throw new InvalidDataException();
            await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            pending = await JsonSerializer.DeserializeAsync<DefenderScanPending>(input, Json, token).ConfigureAwait(false) ?? throw new InvalidDataException();
            if (pending.Schema != 1 || !Enum.IsDefined(pending.Type) || !pending.CommandDispatched ||
                !string.Equals(pending.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or InvalidDataException)
        {
            return new(DefenderScanType.Quick, DefenderScanOutcome.Inconclusive,
                "Pending Defender scan evidence is unavailable or invalid (" + error.GetType().Name + "). No scan was started.", path);
        }
        var history = await host.ReadHistoryAsync(token).ConfigureAwait(false);
        if (!TryHistory(history, out var observation)) return new(pending.Type, DefenderScanOutcome.Inconclusive,
            "Current Defender scan history is incomplete. The pending record remains active, and no scan was started.", path);
        var completion = Completion(observation, pending.Type);
        if (completion is not null && completion > pending.PreparedUtc &&
            (pending.PreviousCompletionUtc is null || completion > pending.PreviousCompletionUtc))
        {
            await ResolveAsync(path, token).ConfigureAwait(false);
            return new(pending.Type, DefenderScanOutcome.Verified,
                "Later evidence confirms a newer completed " + Label(pending.Type) + " scan. No additional scan was started.", path);
        }
        if (observation.ScanInProgress) return new(pending.Type, DefenderScanOutcome.Started,
            "Microsoft Defender still reports a scan in progress. No additional scan was started.", path);
        return new(pending.Type, DefenderScanOutcome.Inconclusive,
            "Current history does not prove completion of the pending scan request. The guard remains active, and no additional scan was started.", path);
    }

    private static async Task<(bool Eligible, string Detail, DefenderScanHistoryObservation? History)> InspectAsync(
        DefenderScanType type, IDefenderScanHost host, CancellationToken token)
    {
        if (!Enum.IsDefined(type)) return (false, "Select Quick or Full scan.", null);
        if (!host.IsAdministrator()) return (false, "Administrator access is required to start a Microsoft Defender scan.", null);
        if (host.RebootPending()) return (false, "Restart Windows and reassess before starting a Microsoft Defender scan.", null);
        var protection = await host.ReadProtectionAsync(token).ConfigureAwait(false);
        var interpreted = protection.Evidence is null ? protection : DefenderRecovery.Interpret(protection.Evidence);
        if (interpreted.State is not (CheckState.Clear or CheckState.Repairable) ||
            interpreted.State == CheckState.Repairable && interpreted.Repair != RepairKind.DefenderSignatures)
            return (false, "Active Microsoft Defender protection could not be confirmed. " + protection.Detail, null);
        var history = await host.ReadHistoryAsync(token).ConfigureAwait(false);
        if (!TryHistory(history, out var observation)) return (false, "Complete Microsoft Defender scan-state evidence is required.", null);
        if (observation.ScanInProgress) return (false, "Microsoft Defender already reports a scan in progress.", observation);
        return (true, "Ready to start one explicit " + Label(type) + " scan. No exclusions, policy or remediation settings will change.", observation);
    }

    private static bool TryHistory(WorkflowCheck check, out DefenderScanHistoryObservation observation)
    {
        observation = default!;
        if (check.Evidence is not { ExitCode: 0, TimedOut: false, Truncated: false } evidence || !string.IsNullOrWhiteSpace(evidence.Error)) return false;
        if (DefenderScanHistory.Interpret(evidence).State == CheckState.Unknown) return false;
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (!root.GetProperty("complete").GetBoolean()) return false;
            DateTimeOffset? Read(string name) => root.GetProperty(name).ValueKind == JsonValueKind.Null ? null : root.GetProperty(name).GetDateTimeOffset().ToUniversalTime();
            observation = new(root.GetProperty("collectedUtc").GetDateTimeOffset().ToUniversalTime(), root.GetProperty("scanInProgress").GetBoolean(),
                Read("quickStartUtc"), Read("quickEndUtc"), null, root.GetProperty("quickOverdue").GetBoolean(),
                Read("fullStartUtc"), Read("fullEndUtc"), null, root.GetProperty("fullOverdue").GetBoolean());
            return observation.CollectedUtc <= DateTimeOffset.UtcNow.AddMinutes(5) &&
                (observation.QuickStartUtc is null) == (observation.QuickEndUtc is null) &&
                (observation.FullStartUtc is null) == (observation.FullEndUtc is null);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException) { return false; }
    }

    private static DateTimeOffset? Completion(DefenderScanHistoryObservation observation, DefenderScanType type) =>
        type == DefenderScanType.Quick ? observation.QuickEndUtc : observation.FullEndUtc;
    private static string Label(DefenderScanType type) => type == DefenderScanType.Quick ? "quick" : "full";
    private static async Task ResolveAsync(string path, CancellationToken token)
    {
        string resolved = path.Replace(".pending.json", ".resolved-" + DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss") + ".json", StringComparison.Ordinal);
        await Task.Run(() => File.Move(path, resolved, false), token).ConfigureAwait(false);
    }
}

public sealed class WindowsDefenderScanHost : IDefenderScanHost
{
    public bool IsAdministrator() => OperatingSystem.IsWindows() && WindowsState.IsAdministrator();
    public bool RebootPending() => OperatingSystem.IsWindows() && WindowsState.RebootPending();
    public Task<WorkflowCheck> ReadProtectionAsync(CancellationToken token) => DefenderRecovery.CheckAsync(token: token);
    public Task<WorkflowCheck> ReadHistoryAsync(CancellationToken token) => DefenderScanHistory.CheckAsync(token);
    public Task<ToolResult> StartAsync(DefenderScanType type, CancellationToken token)
    {
        string scanType = type == DefenderScanType.Quick ? "QuickScan" : "FullScan";
        var step = new RepairStep(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
            "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';Start-MpScan -ScanType " + scanType + " -ErrorAction Stop"], false);
        return BoundedTool.RunAsync(step, TimeSpan.FromSeconds(45), token: token);
    }
}
