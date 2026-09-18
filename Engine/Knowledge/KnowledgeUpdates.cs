namespace KillerBench.Engine;

public enum KnowledgeUpdateState { Activated, Current, Failed }

public sealed record KnowledgeUpdateRequest(string PackId, long MinimumRevision, string StorageDirectory,
    ReadOnlyMemory<byte> TrustedPublicKey);

public sealed record KnowledgeUpdateProgress(string PackId, string Stage, string Detail);

public sealed record KnowledgeUpdateResult(string PackId, KnowledgeUpdateState State, long? InstalledRevision,
    int? EntryCount, DateTimeOffset? ActivatedUtc, string Detail);

public interface IKnowledgeCandidateSource
{
    Task<SignedKnowledgePack> FetchAsync(string packId, CancellationToken token);
}

public static class KnowledgeUpdates
{
    public static async Task<KnowledgeUpdateResult> UpdateAsync(KnowledgeUpdateRequest request,
        IKnowledgeCandidateSource source, IProgress<KnowledgeUpdateProgress>? progress = null,
        CancellationToken token = default)
    {
        Validate(request);
        ArgumentNullException.ThrowIfNull(source);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromMinutes(2));
        var reporter = new SafeProgress(progress);
        KnowledgeCatalog? current = null;
        DateTimeOffset? activatedUtc = null;
        try
        {
            reporter.Report(new(request.PackId, "Inspecting", "Checking the installed database."));
            try
            {
                var active = await KnowledgeActivation.LoadActiveAsync(request.StorageDirectory, request.TrustedPublicKey,
                    request.PackId, request.MinimumRevision, deadline.Token).ConfigureAwait(false);
                current = active.Catalog;
                activatedUtc = active.Pointer.ActivatedUtc;
            }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
                System.Security.Cryptography.CryptographicException or System.Text.Json.JsonException)
            {
                reporter.Report(new(request.PackId, "Recovering", "The active database is damaged. Checking retained verified versions."));
                var recovered = await KnowledgeActivation.RecoverAsync(request.StorageDirectory, request.TrustedPublicKey,
                    request.PackId, request.MinimumRevision, deadline.Token).ConfigureAwait(false);
                current = recovered.Catalog;
                activatedUtc = (await KnowledgeActivation.LoadActiveAsync(request.StorageDirectory, request.TrustedPublicKey,
                    request.PackId, request.MinimumRevision, deadline.Token).ConfigureAwait(false)).Pointer.ActivatedUtc;
            }

            reporter.Report(new(request.PackId, "Fetching", "Retrieving the signed candidate."));
            var candidate = await (source.FetchAsync(request.PackId, deadline.Token)
                ?? throw new InvalidOperationException("The database source returned no operation."))
                .WaitAsync(deadline.Token).ConfigureAwait(false);
            if (candidate is null) throw new InvalidDataException("The database source returned no candidate.");

            reporter.Report(new(request.PackId, "Verifying", "Verifying the candidate and revision."));
            var activation = await KnowledgeActivation.ActivateAsync(request.StorageDirectory, candidate,
                request.TrustedPublicKey, request.PackId, request.MinimumRevision, deadline.Token).ConfigureAwait(false);
            var selected = await KnowledgeActivation.LoadActiveAsync(request.StorageDirectory, request.TrustedPublicKey,
                request.PackId, request.MinimumRevision, deadline.Token).ConfigureAwait(false);
            var state = activation.Changed ? KnowledgeUpdateState.Activated : KnowledgeUpdateState.Current;
            string detail = activation.Changed
                ? $"Database revision {selected.Catalog.Revision} activated and verified."
                : $"Database revision {selected.Catalog.Revision} is already current and verified.";
            reporter.Report(new(request.PackId, state.ToString(), detail));
            return new(request.PackId, state, selected.Catalog.Revision, selected.Catalog.Count,
                selected.Pointer.ActivatedUtc, detail);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (OperationCanceledException)
        {
            return Failed("Database update timed out before activation completed.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or
            InvalidOperationException or System.Security.Cryptography.CryptographicException or System.Security.SecurityException)
        {
            return Failed("Database update failed. The prior active database was retained. " + error.Message);
        }

        KnowledgeUpdateResult Failed(string detail)
        {
            reporter.Report(new(request.PackId, "Failed", detail));
            return new(request.PackId, KnowledgeUpdateState.Failed, current?.Revision, current?.Count, activatedUtc, detail);
        }
    }

    public static async Task<IReadOnlyList<KnowledgeUpdateResult>> UpdateAllAsync(
        IReadOnlyList<KnowledgeUpdateRequest> requests, IKnowledgeCandidateSource source,
        IProgress<KnowledgeUpdateProgress>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(source);
        if (requests.Count is 0 or > 32 || requests.Any(request => request is null) ||
            requests.Select(request => request.PackId).Distinct(StringComparer.Ordinal).Count() != requests.Count)
            throw new ArgumentException("Supply 1 to 32 databases with unique identities.", nameof(requests));
        foreach (var request in requests) Validate(request);
        var results = new List<KnowledgeUpdateResult>(requests.Count);
        foreach (var request in requests)
        {
            token.ThrowIfCancellationRequested();
            results.Add(await UpdateAsync(request, source, progress, token).ConfigureAwait(false));
        }
        return results.AsReadOnly();
    }

    private static void Validate(KnowledgeUpdateRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.PackId) || request.PackId.Length > 128 ||
            request.PackId.Any(char.IsControl) || request.MinimumRevision < 1 ||
            string.IsNullOrWhiteSpace(request.StorageDirectory) || request.TrustedPublicKey.Length is 0 or > 1024)
            throw new ArgumentException("Database identity, revision, storage and trusted key are required.", nameof(request));
    }

    private sealed class SafeProgress(IProgress<KnowledgeUpdateProgress>? target)
    {
        public void Report(KnowledgeUpdateProgress value)
        {
            try { target?.Report(value); }
            catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
        }
    }
}
