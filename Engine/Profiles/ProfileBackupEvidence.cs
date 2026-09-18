namespace KillerBench.Engine;

public sealed record ProfileBackupReceipt(
    string SourceSid,
    string SourcePath,
    string DestinationPath,
    DateTimeOffset CompletedUtc,
    long FileCount,
    long TotalBytes,
    string ManifestSha256,
    bool Verified);

public static class ProfileBackupEvidence
{
    public static bool Matches(ProfileSnapshot target, ProfileBackupReceipt? receipt, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (receipt is null || !receipt.Verified || receipt.FileCount < 0 || receipt.TotalBytes < 0 ||
            receipt.CompletedUtc == default || receipt.CompletedUtc > now || now - receipt.CompletedUtc > TimeSpan.FromHours(24) ||
            !target.Sid.Equals(receipt.SourceSid, StringComparison.OrdinalIgnoreCase) ||
            !ExactPath(target.Path, receipt.SourcePath) ||
            !ProfilePlanning.IsLocalProfilePath(receipt.DestinationPath) ||
            !IsSha256(receipt.ManifestSha256)) return false;

        string source = Normalize(target.Path);
        string destination = Normalize(receipt.DestinationPath);
        return !source.Equals(destination, StringComparison.OrdinalIgnoreCase) &&
            !IsWithin(destination, source) && !IsWithin(source, destination);
    }

    private static bool ExactPath(string left, string right) =>
        ProfilePlanning.IsLocalProfilePath(left) && ProfilePlanning.IsLocalProfilePath(right) &&
        Normalize(left).Equals(Normalize(right), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string path) => Path.GetFullPath(path).TrimEnd('\\');

    private static bool IsWithin(string candidate, string parent) =>
        candidate.StartsWith(parent + "\\", StringComparison.OrdinalIgnoreCase);

    private static bool IsSha256(string value) =>
        value is { Length: 64 } && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');
}
