using System.Text.Json;

namespace KillerBench.Engine;

public sealed record MacePackageRemovalEligibility(bool Eligible, string Detail, MaceCandidate? Candidate = null);

public enum MacePackageRemovalOutcome { Preview, Blocked, Verified, NotApplied, Uncertain, Stopped }
public enum MacePackageRemovalReconciliationOutcome { NoPendingRecord, Verified, NotApplied, Inconclusive }

public sealed record MacePackageRemovalResult(MacePackageRemovalOutcome Outcome, string Detail,
    string? JournalPath = null, string? PendingPath = null)
{
    public int GetExitCode() => Outcome is MacePackageRemovalOutcome.Preview or
        MacePackageRemovalOutcome.Verified or MacePackageRemovalOutcome.NotApplied ? 0 :
        Outcome == MacePackageRemovalOutcome.Stopped ? 130 : 3;
}

public sealed record MacePackageRemovalPendingRecord(int Schema, string Machine, DateTimeOffset StartedUtc,
    MaceReviewOptions Options, MaceTargets Target, string Identity);

public sealed record MacePackageRemovalReconciliationResult(MacePackageRemovalReconciliationOutcome Outcome,
    string Detail, string PendingPath, string? ArchivePath = null)
{
    public int GetExitCode() => Outcome is MacePackageRemovalReconciliationOutcome.NoPendingRecord or
        MacePackageRemovalReconciliationOutcome.Verified or MacePackageRemovalReconciliationOutcome.NotApplied ? 0 : 3;
}

public interface IMacePackageRemovalHost
{
    Task<MaceRemovalReview> ReviewAsync(MaceReviewOptions options, CancellationToken token);
    Task<ToolResult> RemoveCurrentAccountPackageAsync(string packageFullName, CancellationToken token);
}

public static class MacePackageRemoval
{
    public const string PendingFileName = "mace-package-removal.pending.json";
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static MacePackageRemovalEligibility Evaluate(MaceRemovalReview review, string identity)
    {
        ArgumentNullException.ThrowIfNull(review);
        ValidateIdentity(identity);
        if (!review.Complete) return Blocked("MACE review evidence is incomplete.");
        if (review.Options.Scope != MaceScope.RunningAccount)
            return Blocked("Automatic package removal is limited to the running account.");
        if (!review.Options.PreserveApplicationData || !review.Options.PreserveCachedCredentials)
            return Blocked("Automatic package removal requires preserving application data and cached credentials.");
        if (review.ProcessEvidence is not { Complete: true } processes || processes.RelevantCount(review.Options.Targets) != 0)
            return Blocked("Selected application process evidence must be complete and clear.");
        if (review.ManagedPolicyEvidence is not { Complete: true } policy || policy.ConfiguredCount(review.Options.Targets) != 0)
            return Blocked("Selected managed-configuration evidence must be complete and clear.");
        if (review.Options.Targets.HasFlag(MaceTargets.OneDrive) &&
            review.OneDriveFolderEvidence is not { Complete: true, RedirectedCount: 0 })
            return Blocked("OneDrive known-folder evidence must be complete and clear.");
        if (review.RemovalProviderEvidence is not { Complete: true } providers)
            return Blocked("Removal-provider evidence must be complete.");

        MaceCandidate[] matches = review.Candidates.Where(item =>
            item.Identity.Equals(identity, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1) return Blocked(matches.Length == 0
            ? "The exact selected package is not present in the current review."
            : "The exact selected package identity is ambiguous.");
        MaceCandidate candidate = matches[0];
        MaceRemovalProviderCandidate[] providerMatches = providers.Candidates.Where(item =>
            item.Identity.Equals(identity, StringComparison.OrdinalIgnoreCase) &&
            item.RegistrationType.Equals(candidate.RegistrationType, StringComparison.Ordinal) &&
            item.Scope.Equals(candidate.Scope, StringComparison.Ordinal)).ToArray();
        if (providerMatches.Length != 1 || providerMatches[0] is not
            { ProviderIdentified: true, ProviderKind: MaceRemovalProviderKind.CurrentAccountAppPackage })
            return Blocked("The exact selected package does not have a supported current-account removal provider.");
        if (candidate.Protected || candidate.RegistrationType != "Registered app package" || candidate.Scope != "CurrentAccount")
            return Blocked("The exact selected package is outside the supported current-account package boundary.");
        return new(true,
            "The exact unprotected current-account package has complete provider and safeguard evidence. A fresh identical review is required before any request.",
            candidate);
    }

    public static async Task<MacePackageRemovalResult> RunAsync(MaceReviewOptions options, string identity,
        bool execute, string journalFolder, IMacePackageRemovalHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        options.Validate();
        ValidateIdentity(identity);
        Report(progress, "Reviewing exact MACE package removal eligibility.");
        MaceRemovalReview first = await Required(ct => host.ReviewAsync(options, ct), "MACE review", token).ConfigureAwait(false);
        if (!SameOptions(options, first.Options))
            return new(MacePackageRemovalOutcome.Blocked,
                "The evidence provider returned a different MACE selection. No package removal was requested.");
        MacePackageRemovalEligibility firstEligibility = Evaluate(first, identity);
        if (!firstEligibility.Eligible)
            return new(MacePackageRemovalOutcome.Blocked, firstEligibility.Detail + " No package removal was requested.");
        if (!execute)
            return new(MacePackageRemovalOutcome.Preview,
                "The exact current-account package will be reviewed twice more, recorded before dispatch, requested once, and verified absent from a complete fresh inventory. Application data, credentials, policy and other packages remain outside the request.");

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        string pendingPath = Path.Combine(root, PendingFileName);
        await using var operationLock = new FileStream(Path.Combine(root, "mace-package-removal.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (File.Exists(pendingPath))
            return new(MacePackageRemovalOutcome.Blocked,
                "A prior package removal needs reconciliation before another can start.", PendingPath: pendingPath);

        Report(progress, "Repeating exact MACE package and safeguard evidence.");
        MaceRemovalReview repeated = await Required(ct => host.ReviewAsync(options, ct), "MACE review", token).ConfigureAwait(false);
        MacePackageRemovalEligibility repeatedEligibility = Evaluate(repeated, identity);
        if (!SameOptions(options, repeated.Options) || !repeatedEligibility.Eligible ||
            !SameCandidate(firstEligibility.Candidate!, repeatedEligibility.Candidate!))
            return new(MacePackageRemovalOutcome.Blocked,
                "Exact package or safeguard evidence changed before preparation. No package removal was requested.");

        var pending = new MacePackageRemovalPendingRecord(1, Environment.MachineName, DateTimeOffset.UtcNow,
            options, repeatedEligibility.Candidate!.Target, repeatedEligibility.Candidate.Identity);
        await JournalPersistence.WriteAsync(pendingPath,
            JsonSerializer.Serialize(pending, Capture.JsonOptions), token).ConfigureAwait(false);
        bool dispatched = false;
        try
        {
            Report(progress, "Performing the final exact MACE eligibility check.");
            MaceRemovalReview boundary = await Required(ct => host.ReviewAsync(options, ct), "MACE review", token).ConfigureAwait(false);
            MacePackageRemovalEligibility boundaryEligibility = Evaluate(boundary, identity);
            if (!SameOptions(options, boundary.Options) || !boundaryEligibility.Eligible ||
                !SameCandidate(repeatedEligibility.Candidate, boundaryEligibility.Candidate!))
                return await CloseAsync(root, pendingPath, pending, MacePackageRemovalOutcome.Blocked,
                    "Exact package or safeguard evidence changed at the final boundary. No package removal was requested.", token).ConfigureAwait(false);

            Report(progress, "Requesting removal of the exact current-account package.");
            dispatched = true;
            ToolResult command = await Required(ct => host.RemoveCurrentAccountPackageAsync(identity, ct),
                "MACE package removal", token).ConfigureAwait(false);
            Report(progress, "Verifying exact package absence.");
            MaceRemovalReview after = await Required(ct => host.ReviewAsync(options, ct), "MACE review", token).ConfigureAwait(false);
            if (!SameOptions(options, after.Options) || !after.Complete)
                return new(MacePackageRemovalOutcome.Uncertain,
                    "Package removal was requested, but fresh inventory is incomplete. The pending record remains and the request will not be repeated automatically.",
                    PendingPath: pendingPath);
            bool remains = after.Candidates.Any(item => item.Identity.Equals(identity, StringComparison.OrdinalIgnoreCase));
            if (!remains)
                return await CloseAsync(root, pendingPath, pending, MacePackageRemovalOutcome.Verified,
                    "The exact current-account package is absent from complete fresh inventory. No data, credential, policy or other package action was requested.", token).ConfigureAwait(false);
            string commandState = command.ExitCode == 0 && !command.TimedOut && !command.Truncated && string.IsNullOrWhiteSpace(command.Error)
                ? "The provider reported completion, but the package remains registered."
                : "The provider did not report clean completion and the package remains registered.";
            return await CloseAsync(root, pendingPath, pending, MacePackageRemovalOutcome.NotApplied,
                commandState + " The request will not be repeated automatically.", token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!dispatched)
                return await CloseAsync(root, pendingPath, pending, MacePackageRemovalOutcome.Stopped,
                    "Package removal stopped before dispatch. No package removal was requested.", CancellationToken.None).ConfigureAwait(false);
            return new(MacePackageRemovalOutcome.Stopped,
                "Package removal stopped after dispatch. The pending record remains and the request will not be repeated automatically.",
                PendingPath: pendingPath);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            if (!dispatched)
                return await CloseAsync(root, pendingPath, pending, MacePackageRemovalOutcome.Blocked,
                    "Final evidence was unavailable before dispatch. No package removal was requested. " + error.GetType().Name + ".",
                    CancellationToken.None).ConfigureAwait(false);
            return new(MacePackageRemovalOutcome.Uncertain,
                "Package removal was dispatched without complete verification. The pending record remains and the request will not be repeated automatically. " + error.GetType().Name + ".",
                PendingPath: pendingPath);
        }
    }

    public static async Task<MacePackageRemovalReconciliationResult> ReconcileAsync(string journalFolder,
        IMacePackageRemovalHost host, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(journalFolder);
        string pendingPath = Path.Combine(root, PendingFileName);
        if (!File.Exists(pendingPath))
            return new(MacePackageRemovalReconciliationOutcome.NoPendingRecord,
                "No interrupted MACE package removal record is present.", pendingPath);
        Directory.CreateDirectory(root);
        await using var operationLock = new FileStream(Path.Combine(root, "mace-package-removal.reconcile.lock"),
            FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
        try
        {
            MacePackageRemovalPendingRecord pending = await ReadPendingAsync(pendingPath, token).ConfigureAwait(false);
            MaceRemovalReview current = await Required(ct => host.ReviewAsync(pending.Options, ct), "MACE review", token).ConfigureAwait(false);
            if (!SameOptions(pending.Options, current.Options) || !current.Complete)
                return new(MacePackageRemovalReconciliationOutcome.Inconclusive,
                    "Fresh package inventory is incomplete. The pending record remains and no removal was repeated.", pendingPath);
            bool remains = current.Candidates.Any(item => item.Identity.Equals(pending.Identity, StringComparison.OrdinalIgnoreCase));
            MacePackageRemovalReconciliationOutcome outcome = remains
                ? MacePackageRemovalReconciliationOutcome.NotApplied
                : MacePackageRemovalReconciliationOutcome.Verified;
            string detail = remains
                ? "The exact package remains registered. The pending record was archived without repeating removal."
                : "The exact package is absent from complete fresh inventory. The pending record was archived without repeating removal.";
            string archive = Path.Combine(root, "mace-package-removal-reconciliation-" +
                DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N") + ".json");
            var result = new MacePackageRemovalReconciliationResult(outcome, detail, pendingPath, archive);
            await JournalPersistence.WriteAsync(archive, JsonSerializer.Serialize(result, Capture.JsonOptions), token).ConfigureAwait(false);
            File.Move(pendingPath, archive + ".prepared.json");
            return result;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            InvalidOperationException or NotSupportedException or TimeoutException or System.Security.SecurityException or
            System.ComponentModel.Win32Exception)
        {
            return new(MacePackageRemovalReconciliationOutcome.Inconclusive,
                "The pending record or fresh package evidence could not be verified. The record remains and no removal was repeated.", pendingPath);
        }
    }

    private static async Task<MacePackageRemovalResult> CloseAsync(string root, string pendingPath,
        MacePackageRemovalPendingRecord pending, MacePackageRemovalOutcome outcome, string detail,
        CancellationToken token)
    {
        string archive = Path.Combine(root, "mace-package-removal-" +
            DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N") + ".json");
        var result = new MacePackageRemovalResult(outcome, detail, archive);
        await JournalPersistence.WriteAsync(archive, JsonSerializer.Serialize(result, Capture.JsonOptions), token).ConfigureAwait(false);
        File.Move(pendingPath, archive + ".prepared.json");
        return result;
    }

    private static async Task<MacePackageRemovalPendingRecord> ReadPendingAsync(string path, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 131_072) throw new InvalidDataException("MACE package-removal record size is invalid.");
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        using var document = JsonDocument.Parse(bytes);
        if (!UniqueFields(document.RootElement)) throw new InvalidDataException("MACE package-removal record contains conflicting fields.");
        MacePackageRemovalPendingRecord pending = document.RootElement.Deserialize<MacePackageRemovalPendingRecord>(StrictJson) ??
            throw new InvalidDataException("MACE package-removal record is empty.");
        pending.Options.Validate();
        ValidateIdentity(pending.Identity);
        if (pending.Schema != 1 || pending.StartedUtc == default ||
            !pending.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
            !SingleTarget(pending.Target))
            throw new InvalidDataException("MACE package-removal record is invalid.");
        return pending;
    }

    private static bool SameCandidate(MaceCandidate left, MaceCandidate right) =>
        left.Target == right.Target && left.Protected == right.Protected &&
        left.RegistrationType.Equals(right.RegistrationType, StringComparison.Ordinal) &&
        left.Scope.Equals(right.Scope, StringComparison.Ordinal) &&
        left.Identity.Equals(right.Identity, StringComparison.OrdinalIgnoreCase);

    private static bool SameOptions(MaceReviewOptions left, MaceReviewOptions right) => left == right;

    private static bool SingleTarget(MaceTargets target) => target != MaceTargets.None &&
        ((int)target & ((int)target - 1)) == 0 && Enum.IsDefined(target);

    private static void ValidateIdentity(string identity)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identity);
        if (identity.Length > 512 || identity != identity.Trim() || identity.Any(character =>
            !(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')))
            throw new ArgumentException("Select one exact app package full name.", nameof(identity));
    }

    private static MacePackageRemovalEligibility Blocked(string detail) => new(false, detail);

    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation,
        string name, CancellationToken token)
    {
        Task<T> pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        return await pending.WaitAsync(token).ConfigureAwait(false) ??
            throw new InvalidDataException(name + " provider returned no evidence.");
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}
