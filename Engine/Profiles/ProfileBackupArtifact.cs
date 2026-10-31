using System.Security.Cryptography;
using System.Text.Json;

namespace KillerBench.Engine.Profiles;

public sealed record ProfileBackupArtifact(
    int Schema,
    string SourceSid,
    string SourcePath,
    string DestinationPath,
    DateTimeOffset CompletedUtc,
    long FileCount,
    long TotalBytes,
    string ManifestSha256);

public sealed record ProfileBackupManifestEntry(string RelativePath, long Length, string Sha256);

public sealed record ProfileBackupVerification(ProfileBackupReceipt? Receipt, bool Verified, string Detail);

public static class ProfileBackupArtifactWriter
{
    private const int MaximumFiles = 1_000_000;
    private const long MaximumManifestBytes = 268_435_456;
    private const int MaximumManifestLineCharacters = 65_536;
    private static readonly JsonSerializerOptions CompactJson = new(Capture.JsonOptions) { WriteIndented = false };

    public static async Task<ProfileBackupVerification> CreateForExistingCopyAsync(ProfileSnapshot target, string destinationPath,
        DateTimeOffset? completedUtc = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        token.ThrowIfCancellationRequested();
        if (!ProfilePlanning.IsSid(target.Sid) || !ProfilePlanning.IsLocalProfilePath(target.Path) ||
            string.IsNullOrWhiteSpace(destinationPath) || !Path.IsPathFullyQualified(destinationPath))
            return Failed("The selected profile or backup destination identity is invalid.");

        string destination;
        try { destination = Path.GetFullPath(destinationPath).TrimEnd('\\'); }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException)
        { return Failed("The backup destination identity is invalid."); }
        if (!ProfilePlanning.IsLocalProfilePath(destination) || !Directory.Exists(destination) || IsReparsePoint(destination) ||
            destination.Equals(target.Path.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase) ||
            destination.StartsWith(target.Path.TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase) ||
            target.Path.StartsWith(destination + "\\", StringComparison.OrdinalIgnoreCase))
            return Failed("The backup destination must be an existing separate non-redirected local directory.");

        string manifestPath = Path.Combine(destination, ProfileBackupArtifactVerifier.ManifestFileName);
        string receiptPath = Path.Combine(destination, ProfileBackupArtifactVerifier.ReceiptFileName);
        if (File.Exists(manifestPath) || File.Exists(receiptPath))
            return Failed("The fixed backup artifact files already exist and were not overwritten.");

        string manifestTemporary = manifestPath + ".creating-" + Guid.NewGuid().ToString("N");
        string receiptTemporary = receiptPath + ".creating-" + Guid.NewGuid().ToString("N");
        try
        {
            long fileCount = 0;
            long totalBytes = 0;
            await using (var stream = new FileStream(manifestTemporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65_536,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            await using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false), 65_536, leaveOpen: true))
            {
                foreach (string filePath in EnumerateFiles(destination, manifestTemporary, receiptTemporary, token))
                {
                    token.ThrowIfCancellationRequested();
                    if (++fileCount > MaximumFiles) throw new InvalidDataException("The backup contains too many files.");
                    var info = new FileInfo(filePath);
                    long length = info.Length;
                    await using var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65_536,
                        FileOptions.Asynchronous | FileOptions.SequentialScan);
                    string digest = Convert.ToHexString(await SHA256.HashDataAsync(file, token).ConfigureAwait(false));
                    if (file.Length != length || IsReparsePoint(filePath))
                        throw new IOException("A backed-up file changed while the manifest was created.");
                    totalBytes = checked(totalBytes + length);
                    string relative = Path.GetRelativePath(destination, filePath).Replace('/', '\\');
                    string line = JsonSerializer.Serialize(new ProfileBackupManifestEntry(relative, length, digest), CompactJson);
                    if (line.Length > MaximumManifestLineCharacters) throw new InvalidDataException("A manifest entry is too large.");
                    await writer.WriteLineAsync(line.AsMemory(), token).ConfigureAwait(false);
                    await writer.FlushAsync(token).ConfigureAwait(false);
                    if (stream.Length > MaximumManifestBytes) throw new InvalidDataException("The backup manifest is too large.");
                }
            }

            string manifestDigest;
            await using (var manifest = new FileStream(manifestTemporary, FileMode.Open, FileAccess.Read, FileShare.Read, 65_536,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
                manifestDigest = Convert.ToHexString(await SHA256.HashDataAsync(manifest, token).ConfigureAwait(false));
            var artifact = new ProfileBackupArtifact(1, target.Sid, target.Path, destination,
                completedUtc ?? DateTimeOffset.UtcNow, fileCount, totalBytes, manifestDigest);
            await File.WriteAllTextAsync(receiptTemporary, JsonSerializer.Serialize(artifact, Capture.JsonOptions),
                new System.Text.UTF8Encoding(false), token).ConfigureAwait(false);
            File.Move(manifestTemporary, manifestPath);
            File.Move(receiptTemporary, receiptPath);
            return await ProfileBackupArtifactVerifier.VerifyAsync(target, receiptPath, token: token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or OverflowException)
        {
            return Failed("The backup artifact could not be created: " + error.GetType().Name + ".");
        }
        finally
        {
            TryDeleteOwnTemporary(manifestTemporary);
            TryDeleteOwnTemporary(receiptTemporary);
        }
    }

    private static IEnumerable<string> EnumerateFiles(string destination, string manifestTemporary, string receiptTemporary,
        CancellationToken token)
    {
        var folders = new Stack<string>();
        folders.Push(destination);
        while (folders.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            string folder = folders.Pop();
            if (IsReparsePoint(folder)) throw new IOException("A redirected backup directory was found.");
            foreach (string entry in Directory.EnumerateFileSystemEntries(folder).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
            {
                token.ThrowIfCancellationRequested();
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("A redirected backup entry was found.");
                if ((attributes & FileAttributes.Directory) != 0) folders.Push(entry);
                else if (!entry.Equals(manifestTemporary, StringComparison.OrdinalIgnoreCase) &&
                         !entry.Equals(receiptTemporary, StringComparison.OrdinalIgnoreCase)) yield return entry;
            }
        }
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static void TryDeleteOwnTemporary(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
    }

    private static ProfileBackupVerification Failed(string detail) => new(null, false, detail);
}

public static class ProfileBackupArtifactVerifier
{
    public const string ReceiptFileName = "profile-receipt.json";
    public const string ManifestFileName = "profile-manifest.jsonl";
    private const long MaximumReceiptBytes = 65_536;
    private const long MaximumManifestBytes = 268_435_456;
    private const int MaximumManifestLineCharacters = 65_536;
    private const int MaximumFiles = 1_000_000;
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static async Task<ProfileBackupVerification> VerifyAsync(ProfileSnapshot target, string receiptPath,
        DateTimeOffset? now = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        token.ThrowIfCancellationRequested();
        if (!ProfilePlanning.IsSid(target.Sid) || !ProfilePlanning.IsLocalProfilePath(target.Path) ||
            !TryNormalizeLocalFile(receiptPath, out string normalizedReceipt) ||
            !Path.GetFileName(normalizedReceipt).Equals(ReceiptFileName, StringComparison.OrdinalIgnoreCase))
            return Failed("The selected backup receipt identity is invalid.");

        try
        {
            using var receiptStream = OpenBoundedFile(normalizedReceipt, MaximumReceiptBytes);
            using var json = await JsonDocument.ParseAsync(receiptStream, cancellationToken: token).ConfigureAwait(false);
            if (!UniqueFields(json.RootElement)) return Failed("The backup receipt contains ambiguous fields.");
            var artifact = json.RootElement.Deserialize<ProfileBackupArtifact>(StrictJson);
            if (artifact is null || artifact.Schema != 1 || artifact.FileCount is < 0 or > MaximumFiles || artifact.TotalBytes < 0 ||
                !ProfileBackupEvidence.Matches(target, new(artifact.SourceSid, artifact.SourcePath, artifact.DestinationPath,
                    artifact.CompletedUtc, artifact.FileCount, artifact.TotalBytes, artifact.ManifestSha256, true), now ?? DateTimeOffset.UtcNow))
                return Failed("The backup receipt does not match the selected profile or supported bounds.");

            string destination = Path.GetFullPath(artifact.DestinationPath).TrimEnd('\\');
            string source = Path.GetFullPath(target.Path).TrimEnd('\\');
            if (!Directory.Exists(source) || IsReparsePoint(source) || !Directory.Exists(destination) || IsReparsePoint(destination) ||
                !normalizedReceipt.Equals(Path.Combine(destination, ReceiptFileName), StringComparison.OrdinalIgnoreCase))
                return Failed("The backup source, destination or fixed receipt location is unavailable.");

            string manifestPath = Path.Combine(destination, ManifestFileName);
            using var manifest = OpenBoundedFile(manifestPath, MaximumManifestBytes);
            string manifestDigest = Convert.ToHexString(await SHA256.HashDataAsync(manifest, token).ConfigureAwait(false));
            if (!manifestDigest.Equals(artifact.ManifestSha256, StringComparison.OrdinalIgnoreCase))
                return Failed("The backup manifest digest does not match the receipt.");
            manifest.Position = 0;

            long fileCount = 0;
            long totalBytes = 0;
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var relativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var reader = new StreamReader(manifest, System.Text.Encoding.UTF8, true, 65_536, leaveOpen: true);
            while (await reader.ReadLineAsync(token).ConfigureAwait(false) is { } line)
            {
                token.ThrowIfCancellationRequested();
                if (line.Length is 0 or > MaximumManifestLineCharacters || ++fileCount > MaximumFiles)
                    return Failed("The backup manifest is empty, oversized or outside its entry bounds.");
                ProfileBackupManifestEntry? entry;
                try
                {
                    using var lineJson = JsonDocument.Parse(line);
                    if (!UniqueFields(lineJson.RootElement)) return Failed("The backup manifest contains ambiguous fields.");
                    entry = lineJson.RootElement.Deserialize<ProfileBackupManifestEntry>(StrictJson);
                }
                catch (JsonException) { return Failed("The backup manifest contains malformed entries."); }
                if (entry is null || entry.Length < 0 || !IsSha256(entry.Sha256) ||
                    !TryResolveEntry(destination, entry.RelativePath, out string filePath) || !paths.Add(filePath) ||
                    !relativePaths.Add(entry.RelativePath.Replace('/', '\\')) ||
                    filePath.Equals(normalizedReceipt, StringComparison.OrdinalIgnoreCase) ||
                    filePath.Equals(manifestPath, StringComparison.OrdinalIgnoreCase) || HasReparsePoint(destination, filePath))
                    return Failed("The backup manifest contains an invalid, duplicate or redirected file identity.");

                if (!TryResolveEntry(source, entry.RelativePath, out string sourceFile) || HasReparsePoint(source, sourceFile))
                    return Failed("The source profile no longer contains the exact safe manifest identity.");

                using var file = OpenBoundedFile(filePath, long.MaxValue);
                if (file.Length != entry.Length) return Failed("A backed-up file length does not match the manifest.");
                string digest = Convert.ToHexString(await SHA256.HashDataAsync(file, token).ConfigureAwait(false));
                if (!digest.Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase) || file.Length != entry.Length ||
                    HasReparsePoint(destination, filePath))
                    return Failed("A backed-up file digest does not match the manifest.");

                using var sourceStream = OpenBoundedFile(sourceFile, long.MaxValue);
                if (sourceStream.Length != entry.Length) return Failed("A source profile file length does not match the backup manifest.");
                string sourceDigest = Convert.ToHexString(await SHA256.HashDataAsync(sourceStream, token).ConfigureAwait(false));
                if (!sourceDigest.Equals(entry.Sha256, StringComparison.OrdinalIgnoreCase) || sourceStream.Length != entry.Length ||
                    HasReparsePoint(source, sourceFile))
                    return Failed("A source profile file digest does not match the backup manifest.");
                totalBytes = checked(totalBytes + entry.Length);
            }

            var omitted = new List<string>();
            var sourcePaths = EnumerateRelativeFiles(source, null, null, token, omitted);
            var destinationPaths = EnumerateRelativeFiles(destination, normalizedReceipt, manifestPath, token);
            if (fileCount != artifact.FileCount || totalBytes != artifact.TotalBytes ||
                !sourcePaths.SetEquals(relativePaths) || !destinationPaths.SetEquals(relativePaths) ||
                IsReparsePoint(source) || IsReparsePoint(destination) || IsReparsePoint(normalizedReceipt) || IsReparsePoint(manifestPath))
                return Failed("The source, destination, manifest and receipt file sets or totals do not match.");
            var receipt = new ProfileBackupReceipt(artifact.SourceSid, artifact.SourcePath, destination, artifact.CompletedUtc,
                fileCount, totalBytes, manifestDigest, true);
            return new(receipt, true,
                $"Verified {fileCount} source and backed-up files and {totalBytes} bytes against the fixed manifest. " +
                (omitted.Count == 0 ? "" : $"Omitted {omitted.Count} {(omitted.Count == 1 ? "junction" : "junctions")}; a junction is a link, and anything it points to was not copied. ") +
                "This does not prove ACL, alternate-stream, encrypted-file, application-state or restore behavior.");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException or JsonException or OverflowException)
        {
            return Failed("The backup artifact could not be verified: " + error.GetType().Name + ".");
        }
    }

    private static FileStream OpenBoundedFile(string path, long maximumBytes)
    {
        if (!File.Exists(path) || IsReparsePoint(path)) throw new IOException("Required artifact is unavailable or redirected.");
        var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65_536,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        if (stream.Length < 0 || stream.Length > maximumBytes) { stream.Dispose(); throw new InvalidDataException("Artifact size is outside its bound."); }
        return stream;
    }

    private static bool TryNormalizeLocalFile(string? path, out string normalized)
    {
        normalized = "";
        if (string.IsNullOrWhiteSpace(path) || path.Any(char.IsControl) || !Path.IsPathFullyQualified(path)) return false;
        try
        {
            normalized = Path.GetFullPath(path);
            return ProfilePlanning.IsLocalProfilePath(normalized);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException) { return false; }
    }

    private static bool TryResolveEntry(string destination, string? relativePath, out string filePath)
    {
        filePath = "";
        if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Length > 32_767 || Path.IsPathRooted(relativePath) ||
            relativePath.Any(char.IsControl) || relativePath.Contains(':')) return false;
        string[] parts = relativePath.Replace('/', '\\').Split('\\');
        if (parts.Any(part => part.Length == 0 || part is "." or ".." || part.EndsWith(' ') || part.EndsWith('.') ||
            part.Any(c => c is '*' or '?' or '"' or '<' or '>' or '|'))) return false;
        try
        {
            filePath = Path.GetFullPath(Path.Combine(destination, Path.Combine(parts)));
            return filePath.StartsWith(destination + "\\", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception error) when (error is ArgumentException or NotSupportedException or PathTooLongException) { return false; }
    }

    private static bool HasReparsePoint(string destination, string filePath)
    {
        string current = filePath;
        while (!current.Equals(destination, StringComparison.OrdinalIgnoreCase))
        {
            if (IsReparsePoint(current)) return true;
            string? parent = Path.GetDirectoryName(current);
            if (parent is null || !parent.StartsWith(destination, StringComparison.OrdinalIgnoreCase)) return true;
            current = parent;
        }
        return false;
    }

    private static HashSet<string> EnumerateRelativeFiles(string root, string? excludedFile1, string? excludedFile2,
        CancellationToken token, List<string>? omittedJunctions = null)
    {
        var relativePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var folders = new Stack<string>();
        folders.Push(root);
        while (folders.Count > 0)
        {
            token.ThrowIfCancellationRequested();
            string folder = folders.Pop();
            if (IsReparsePoint(folder)) throw new IOException("A redirected directory was found while comparing the backup.");
            foreach (string entry in Directory.EnumerateFileSystemEntries(folder))
            {
                token.ThrowIfCancellationRequested();
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    if (omittedJunctions is null || !ProfileBackupPathGuard.IsJunction(entry))
                        throw new IOException("A redirected entry was found while comparing the backup.");
                    omittedJunctions.Add(entry);
                    continue;
                }
                if ((attributes & FileAttributes.Directory) != 0) folders.Push(entry);
                else if (!entry.Equals(excludedFile1, StringComparison.OrdinalIgnoreCase) &&
                         !entry.Equals(excludedFile2, StringComparison.OrdinalIgnoreCase))
                {
                    if (relativePaths.Count >= MaximumFiles) throw new InvalidDataException("The compared file set is too large.");
                    string relative = Path.GetRelativePath(root, entry).Replace('/', '\\');
                    if (!relativePaths.Add(relative)) throw new InvalidDataException("The compared file set contains ambiguous identities.");
                }
            }
        }
        return relativePaths;
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;

    private static bool IsSha256(string value) =>
        value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    private static ProfileBackupVerification Failed(string detail) => new(null, false, detail);
}
