using System.Text.Json;

namespace KillerBench.Engine;

public enum OrcaCacheResetOutcome { Preview, Blocked, Verified, Uncertain, Stopped }
public enum OrcaCacheReconciliationOutcome { NoPendingRecord, Verified, NotChanged, Inconclusive }

public sealed record OrcaCacheTarget(string Label, string SourcePath, bool Exists, bool ReparsePoint, bool Complete);
public sealed record OrcaCacheTargetPlan(string Label, string SourcePath, string RetainedPath);

public sealed record OrcaCacheResetResult(OrcaCacheResetOutcome Outcome, string Detail,
    string? JournalPath = null, IReadOnlyList<OrcaCacheTargetPlan>? Targets = null)
{
    public int GetExitCode() => Outcome switch
    {
        OrcaCacheResetOutcome.Preview or OrcaCacheResetOutcome.Verified => 0,
        OrcaCacheResetOutcome.Stopped => 130,
        _ => 3
    };
}

public sealed record OrcaCachePendingRecord(int Schema, string OperationId, DateTimeOffset StartedUtc,
    string Machine, OrcaOutlookMode Mode, string JournalPath, IReadOnlyList<OrcaCacheTargetPlan> Targets);

public sealed record OrcaCacheReconciliationResult(OrcaCacheReconciliationOutcome Outcome, string Detail,
    string PendingPath, OrcaCachePendingRecord? Pending = null, string? ArchivePath = null)
{
    public int GetExitCode() => Outcome is OrcaCacheReconciliationOutcome.NoPendingRecord or
        OrcaCacheReconciliationOutcome.Verified or OrcaCacheReconciliationOutcome.NotChanged ? 0 : 3;
}

public interface IOrcaCacheResetHost
{
    Task<OrcaProcessEvidence> ReadProcessesAsync(OrcaOutlookMode mode, CancellationToken token);
    Task<IReadOnlyList<OrcaCacheTarget>> ReadTargetsAsync(OrcaOutlookMode mode, CancellationToken token);
    Task<bool> DirectoryExistsAsync(string path, CancellationToken token);
    Task<ToolResult> RetainAsync(string sourcePath, string retainedPath, CancellationToken token);
}

public static class OrcaCacheReset
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static async Task<WorkflowCheck> ReviewAsync(OrcaReviewOptions options,
        IOrcaCacheResetHost? host = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        if (options.Scope != OrcaScope.RunningAccount || options.Actions != OrcaActions.ClearCaches)
            return new("ORCA cache retention", CheckState.Information,
                "Cache retention requires ClearCaches as the sole running-account action. The selected ORCA areas remain review-only.");
        host ??= new WindowsOrcaCacheResetHost();
        var processes = await Required(ct => host.ReadProcessesAsync(options.Mode, ct), "Outlook process", token).ConfigureAwait(false);
        var targets = await Required(ct => host.ReadTargetsAsync(options.Mode, ct), "Outlook cache", token).ConfigureAwait(false);
        if (!processes.Complete || targets.Any(target => target is null || !target.Complete))
            return new("ORCA cache retention", CheckState.Unknown,
                "Outlook process or exact cache directory evidence is incomplete. No cache retention is available.");
        if (processes.RelevantCount(options.Mode) != 0)
            return new("ORCA cache retention", CheckState.Attention,
                "Close every selected Outlook edition and collect fresh evidence before retaining caches.");
        if (!TryValidate(options.Mode, processes, targets, out string reason))
            return new("ORCA cache retention", CheckState.Attention, reason + " No cache retention is available.");
        int present = targets.Count(target => target.Exists);
        return present == 0
            ? new("ORCA cache retention", CheckState.Clear,
                "No selected Outlook cache directory is present. No cache retention is needed.")
            : new("ORCA cache retention", CheckState.Repairable,
                present + " exact cache director" + (present == 1 ? "y is" : "ies are") +
                " eligible for preview-first retention after separate confirmation. Profiles, OST files, credentials, add-ins, packages and settings are excluded.",
                RepairKind.OrcaCacheReset);
    }

    public static async Task<OrcaCacheResetResult> RunAsync(OrcaReviewOptions options, bool execute,
        string journalFolder, IOrcaCacheResetHost? host = null, IProgress<string>? progress = null,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        options.Validate();
        if (options.Scope != OrcaScope.RunningAccount || options.Actions != OrcaActions.ClearCaches)
            return new(OrcaCacheResetOutcome.Blocked,
                "Cache retention supports only the running account with ClearCaches as the sole selected action. No Outlook data changed.");

        host ??= new WindowsOrcaCacheResetHost();
        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        string pendingPath = Path.Combine(root, "orca-cache-reset.pending.json");
        await using var operationLock = new FileStream(Path.Combine(root, "orca-cache-reset.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (File.Exists(pendingPath))
            return new(OrcaCacheResetOutcome.Blocked,
                "A prior ORCA cache reset needs reconciliation before another cache reset can start.", pendingPath);

        Report(progress, "Checking selected Outlook processes and cache locations.");
        var firstProcesses = await Required(ct => host.ReadProcessesAsync(options.Mode, ct), "Outlook process", token).ConfigureAwait(false);
        var firstTargets = await Required(ct => host.ReadTargetsAsync(options.Mode, ct), "Outlook cache", token).ConfigureAwait(false);
        if (!TryValidate(options.Mode, firstProcesses, firstTargets, out string reason))
            return new(OrcaCacheResetOutcome.Blocked, reason + " No Outlook cache changed.");

        var present = firstTargets.Where(target => target.Exists).ToArray();
        if (present.Length == 0)
            return new(OrcaCacheResetOutcome.Blocked,
                "No selected Outlook cache directory is present. No Outlook cache changed.");

        if (!execute)
            return new(OrcaCacheResetOutcome.Preview,
                "The reset will repeat process and directory checks, retain " + present.Length +
                " exact cache director" + (present.Length == 1 ? "y" : "ies") +
                " under unique recovery names, and verify every original is absent and every retained copy exists. No retained cache is deleted, and no profile, OST, credential, add-in, package or setting changes.");

        Report(progress, "Repeating Outlook process and cache checks before preparation.");
        var finalProcesses = await Required(ct => host.ReadProcessesAsync(options.Mode, ct), "Outlook process", token).ConfigureAwait(false);
        var finalTargets = await Required(ct => host.ReadTargetsAsync(options.Mode, ct), "Outlook cache", token).ConfigureAwait(false);
        if (!TryValidate(options.Mode, finalProcesses, finalTargets, out reason) || !Stable(firstTargets, finalTargets))
            return new(OrcaCacheResetOutcome.Blocked,
                "Outlook process or cache evidence changed before preparation. Collect a fresh review. No Outlook cache changed.");

        string operationId = Guid.NewGuid().ToString("N");
        var plans = finalTargets.Where(target => target.Exists).Select(target => new OrcaCacheTargetPlan(
            target.Label, Path.GetFullPath(target.SourcePath), Path.GetFullPath(target.SourcePath) + ".KillerBench." + operationId)).ToArray();
        foreach (var plan in plans)
            if (await Required(ct => host.DirectoryExistsAsync(plan.RetainedPath, ct), "Retained Outlook cache", token).ConfigureAwait(false))
                return new(OrcaCacheResetOutcome.Blocked,
                    "A retained cache destination already exists. No Outlook cache changed.");

        string journalPath = Path.Combine(root, "orca-cache-reset-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
            "-" + operationId + ".json");
        var pending = new OrcaCachePendingRecord(1, operationId, DateTimeOffset.UtcNow, Environment.MachineName,
            options.Mode, journalPath, plans);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending, Capture.JsonOptions), token).ConfigureAwait(false);

        try
        {
            Report(progress, "Performing the final Outlook process check.");
            var boundaryProcesses = await Required(ct => host.ReadProcessesAsync(options.Mode, ct), "Outlook process", token).ConfigureAwait(false);
            if (!boundaryProcesses.Complete || boundaryProcesses.RelevantCount(options.Mode) != 0)
                return await CloseWithoutMutationAsync(root, pendingPath, pending, OrcaCacheResetOutcome.Blocked,
                    "Outlook process evidence changed at the final boundary. No cache move was requested.", token).ConfigureAwait(false);

            foreach (var plan in plans)
            {
                bool sourceExists = await Required(ct => host.DirectoryExistsAsync(plan.SourcePath, ct), "Outlook cache source", token).ConfigureAwait(false);
                bool destinationExists = await Required(ct => host.DirectoryExistsAsync(plan.RetainedPath, ct), "Retained Outlook cache", token).ConfigureAwait(false);
                if (!sourceExists || destinationExists)
                    return await CloseWithoutMutationAsync(root, pendingPath, pending, OrcaCacheResetOutcome.Blocked,
                        "A cache directory changed at the final boundary. No cache move was requested.", token).ConfigureAwait(false);
            }

            foreach (var plan in plans)
            {
                token.ThrowIfCancellationRequested();
                Report(progress, "Retaining " + plan.Label + ".");
                ToolResult result = await Required(ct => host.RetainAsync(plan.SourcePath, plan.RetainedPath, ct),
                    "Outlook cache retention", token).ConfigureAwait(false);
                if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
                    return new(OrcaCacheResetOutcome.Uncertain,
                        "A cache retention request did not complete cleanly. The pending record was retained and no move will be repeated automatically.",
                        pendingPath, plans);
            }

            Report(progress, "Verifying retained Outlook caches.");
            if (!await VerifyMovedAsync(plans, host, token).ConfigureAwait(false))
                return new(OrcaCacheResetOutcome.Uncertain,
                    "Cache retention could not be verified exactly. The pending record was retained and no move will be repeated automatically.",
                    pendingPath, plans);

            var verified = new OrcaCacheResetResult(OrcaCacheResetOutcome.Verified,
                "Selected Outlook caches were retained under unique recovery names and every move was verified. Profiles, OST files, credentials, add-ins, packages and settings were unchanged.",
                journalPath, plans);
            await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(verified, Capture.JsonOptions), token).ConfigureAwait(false);
            File.Move(pendingPath, journalPath + ".prepared.json");
            return verified;
        }
        catch (OperationCanceledException)
        {
            return new(OrcaCacheResetOutcome.Stopped,
                "The cache reset stopped after durable preparation. The pending record was retained and no move will be repeated automatically.",
                pendingPath, plans);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            return new(OrcaCacheResetOutcome.Uncertain,
                "Cache retention ended without complete verification. The pending record was retained and no move will be repeated automatically. " + error.GetType().Name + ".",
                pendingPath, plans);
        }
    }

    public static async Task<OrcaCacheReconciliationResult> ReconcileAsync(string journalFolder,
        IOrcaCacheResetHost? host = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        host ??= new WindowsOrcaCacheResetHost();
        string root = Path.GetFullPath(journalFolder);
        string pendingPath = Path.Combine(root, "orca-cache-reset.pending.json");
        if (!File.Exists(pendingPath))
            return new(OrcaCacheReconciliationOutcome.NoPendingRecord,
                "No interrupted ORCA cache reset record is present.", pendingPath);
        Directory.CreateDirectory(root);
        await using var operationLock = new FileStream(Path.Combine(root, "orca-cache-reset.reconcile.lock"),
            FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
        try
        {
            OrcaCachePendingRecord pending = await ReadPendingAsync(pendingPath, token).ConfigureAwait(false);
            int moved = 0, unchanged = 0;
            foreach (var plan in pending.Targets)
            {
                bool source = await Required(ct => host.DirectoryExistsAsync(plan.SourcePath, ct), "Outlook cache source", token).ConfigureAwait(false);
                bool retained = await Required(ct => host.DirectoryExistsAsync(plan.RetainedPath, ct), "Retained Outlook cache", token).ConfigureAwait(false);
                if (!source && retained) moved++;
                else if (source && !retained) unchanged++;
                else return new(OrcaCacheReconciliationOutcome.Inconclusive,
                    "Current cache evidence cannot prove a complete move or an unchanged source. The pending record was retained and no move was repeated.",
                    pendingPath, pending);
            }

            OrcaCacheReconciliationOutcome outcome = moved == pending.Targets.Count
                ? OrcaCacheReconciliationOutcome.Verified
                : unchanged == pending.Targets.Count ? OrcaCacheReconciliationOutcome.NotChanged
                : OrcaCacheReconciliationOutcome.Inconclusive;
            if (outcome == OrcaCacheReconciliationOutcome.Inconclusive)
                return new(outcome,
                    "Only part of the cache set appears retained. The pending record was retained and no move was repeated.", pendingPath, pending);
            string archive = Path.Combine(root, "orca-cache-reconciliation-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
                "-" + Guid.NewGuid().ToString("N") + ".json");
            string detail = outcome == OrcaCacheReconciliationOutcome.Verified
                ? "Every selected cache source is absent and every exact retained directory exists. The pending record was archived without repeating a move."
                : "Every selected cache source remains present and every retained destination is absent. No move occurred, and the pending record was archived without retrying.";
            var result = new OrcaCacheReconciliationResult(outcome, detail, pendingPath, pending, archive);
            await JournalPersistence.WriteAsync(archive, JsonSerializer.Serialize(result, Capture.JsonOptions), token).ConfigureAwait(false);
            File.Move(pendingPath, archive + ".prepared.json");
            return result;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            return new(OrcaCacheReconciliationOutcome.Inconclusive,
                "The interrupted ORCA cache reset record or current directory evidence could not be verified. The pending record was retained and no move was repeated.",
                pendingPath);
        }
    }

    public static IReadOnlyList<string> ExpectedPaths(OrcaOutlookMode mode)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(local)) throw new InvalidOperationException("The local application-data directory is unavailable.");
        var paths = new List<string>();
        if (mode is OrcaOutlookMode.New or OrcaOutlookMode.Both)
            paths.Add(Path.Combine(local, "Microsoft", "Olk"));
        if (mode is OrcaOutlookMode.Classic or OrcaOutlookMode.Both)
        {
            paths.Add(Path.Combine(local, "Microsoft", "Outlook", "RoamCache"));
            paths.Add(Path.Combine(local, "Microsoft", "Outlook", "Offline Address Books"));
        }
        return paths.Select(Path.GetFullPath).ToArray();
    }

    private static bool TryValidate(OrcaOutlookMode mode, OrcaProcessEvidence processes,
        IReadOnlyList<OrcaCacheTarget> targets, out string reason)
    {
        if (!processes.Complete) { reason = "Outlook process evidence is incomplete."; return false; }
        if (processes.RelevantCount(mode) != 0) { reason = "Close every selected Outlook edition before retaining caches."; return false; }
        string[] expected = ExpectedPaths(mode).OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        if (targets is null || targets.Count != expected.Length || targets.Any(target => target is null || !target.Complete || target.ReparsePoint) ||
            !targets.Select(target => Path.GetFullPath(target.SourcePath)).OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .SequenceEqual(expected, StringComparer.OrdinalIgnoreCase))
        { reason = "Exact non-reparse cache directory evidence is required."; return false; }
        reason = "Selected Outlook editions are closed and exact cache evidence is complete.";
        return true;
    }

    private static bool Stable(IReadOnlyList<OrcaCacheTarget> first, IReadOnlyList<OrcaCacheTarget> second) =>
        first.Count == second.Count && first.OrderBy(item => item.SourcePath, StringComparer.OrdinalIgnoreCase)
            .SequenceEqual(second.OrderBy(item => item.SourcePath, StringComparer.OrdinalIgnoreCase));

    private static async Task<bool> VerifyMovedAsync(IReadOnlyList<OrcaCacheTargetPlan> plans,
        IOrcaCacheResetHost host, CancellationToken token)
    {
        foreach (var plan in plans)
            if (await Required(ct => host.DirectoryExistsAsync(plan.SourcePath, ct), "Outlook cache source", token).ConfigureAwait(false) ||
                !await Required(ct => host.DirectoryExistsAsync(plan.RetainedPath, ct), "Retained Outlook cache", token).ConfigureAwait(false))
                return false;
        return true;
    }

    private static async Task<OrcaCacheResetResult> CloseWithoutMutationAsync(string root, string pendingPath,
        OrcaCachePendingRecord pending, OrcaCacheResetOutcome outcome, string detail, CancellationToken token)
    {
        string archive = Path.Combine(root, "orca-cache-reset-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
            "-" + Guid.NewGuid().ToString("N") + ".blocked.json");
        var result = new OrcaCacheResetResult(outcome, detail, archive, pending.Targets);
        await JournalPersistence.WriteAsync(archive, JsonSerializer.Serialize(result, Capture.JsonOptions), token).ConfigureAwait(false);
        File.Move(pendingPath, archive + ".prepared.json");
        return result;
    }

    private static async Task<OrcaCachePendingRecord> ReadPendingAsync(string path, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 1_048_576) throw new InvalidDataException("ORCA cache reset record size is invalid.");
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        using var document = JsonDocument.Parse(bytes);
        if (!UniqueFields(document.RootElement)) throw new InvalidDataException("ORCA cache reset record contains conflicting fields.");
        var pending = document.RootElement.Deserialize<OrcaCachePendingRecord>(StrictJson);
        if (pending is null || pending.Schema != 1 || pending.StartedUtc == default || pending.OperationId.Length != 32 ||
            !pending.OperationId.All(Uri.IsHexDigit) || !pending.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
            !Enum.IsDefined(pending.Mode) || string.IsNullOrWhiteSpace(pending.JournalPath) || pending.Targets is null || pending.Targets.Count is < 1 or > 3)
            throw new InvalidDataException("ORCA cache reset record is invalid.");
        string[] expected = ExpectedPaths(pending.Mode).OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var target in pending.Targets)
        {
            string source = Path.GetFullPath(target.SourcePath), retained = Path.GetFullPath(target.RetainedPath);
            if (!expected.Contains(source, StringComparer.OrdinalIgnoreCase) ||
                !retained.Equals(source + ".KillerBench." + pending.OperationId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("ORCA cache reset record paths are invalid.");
        }
        if (pending.Targets.Select(item => item.SourcePath).Distinct(StringComparer.OrdinalIgnoreCase).Count() != pending.Targets.Count)
            throw new InvalidDataException("ORCA cache reset record contains duplicate targets.");
        return pending;
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name, CancellationToken token)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        return await pending.WaitAsync(token).ConfigureAwait(false) ?? throw new InvalidDataException(name + " provider returned no evidence.");
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsOrcaCacheResetHost : IOrcaCacheResetHost
{
    public Task<OrcaProcessEvidence> ReadProcessesAsync(OrcaOutlookMode mode, CancellationToken token) =>
        Task.FromResult(OrcaProcessInventory.Read(mode, token: token));

    public Task<IReadOnlyList<OrcaCacheTarget>> ReadTargetsAsync(OrcaOutlookMode mode, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var targets = new List<OrcaCacheTarget>();
        foreach (string path in OrcaCacheReset.ExpectedPaths(mode))
        {
            token.ThrowIfCancellationRequested();
            string label = Path.GetFileName(path);
            try
            {
                var directory = new DirectoryInfo(path);
                bool exists = directory.Exists;
                bool reparse = exists && directory.Attributes.HasFlag(FileAttributes.ReparsePoint);
                targets.Add(new(label, path, exists, reparse, true));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
            { targets.Add(new(label, path, false, false, false)); }
        }
        return Task.FromResult<IReadOnlyList<OrcaCacheTarget>>(targets.AsReadOnly());
    }

    public Task<bool> DirectoryExistsAsync(string path, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(Directory.Exists(Path.GetFullPath(path)));
    }

    public Task<ToolResult> RetainAsync(string sourcePath, string retainedPath, CancellationToken token)
    {
        string source = Path.GetFullPath(sourcePath), retained = Path.GetFullPath(retainedPath);
        if (!OrcaCacheReset.ExpectedPaths(OrcaOutlookMode.Both).Contains(source, StringComparer.OrdinalIgnoreCase) ||
            !retained.StartsWith(source + ".KillerBench.", StringComparison.OrdinalIgnoreCase) ||
            !Path.GetDirectoryName(source)!.Equals(Path.GetDirectoryName(retained), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("ORCA cache retention paths are outside the expected boundary.");
        var step = new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
            "& { param($source,$retained) if(Test-Path -LiteralPath $retained){throw 'Retention destination already exists.'}; Move-Item -LiteralPath $source -Destination $retained -ErrorAction Stop }",
            source, retained], true);
        return BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: token);
    }
}
