using System.Text.Json;

namespace KillerBench.Engine;

public sealed record VolumeCapacitySnapshot(string Volume, long TotalBytes, long AvailableBytes);

public static class VolumeCapacity
{
    public static WorkflowCheck Check(string volume)
        => Check(volume, selected =>
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Volume capacity requires Windows.");
            var disk = new DriveInfo(selected + "\\");
            return new(selected, disk.TotalSize, disk.AvailableFreeSpace);
        });

    public static WorkflowCheck Check(string volume, Func<string, VolumeCapacitySnapshot> collect)
    {
        volume = StorageDiagnostics.NormalizeVolume(volume);
        ArgumentNullException.ThrowIfNull(collect);
        try
        {
            var snapshot = collect(volume);
            if (snapshot is null || !string.Equals(snapshot.Volume?.TrimEnd('\\'), volume, StringComparison.OrdinalIgnoreCase))
                return new("Volume capacity", CheckState.Unknown, $"Capacity evidence for {volume} is missing or identifies another volume.",
                    Evidence: new(-1, JsonSerializer.Serialize(snapshot), "Volume identity was not verified.", false));
            return Assess(snapshot);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            return new("Volume capacity", CheckState.Unknown, $"Capacity for {volume} is unavailable. No files changed.",
                Evidence: new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Assess(VolumeCapacitySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        string volume = StorageDiagnostics.NormalizeVolume(snapshot.Volume);
        var evidence = new ToolResult(0, JsonSerializer.Serialize(snapshot), "", false);
        if (snapshot.TotalBytes <= 0 || snapshot.AvailableBytes < 0 || snapshot.AvailableBytes > snapshot.TotalBytes)
            return new("Volume capacity", CheckState.Unknown, $"Capacity values for {volume} are inconsistent.", Evidence: evidence);
        double percent = 100.0 * snapshot.AvailableBytes / snapshot.TotalBytes;
        bool low = percent < 10;
        return new("Volume capacity", low ? CheckState.Attention : CheckState.Information,
            $"Volume {volume} has {snapshot.AvailableBytes / 1073741824.0:F1} GiB available of {snapshot.TotalBytes / 1073741824.0:F1} GiB ({percent:F1}%). " +
            (low ? "Below the 10% review threshold. Review storage usage before selecting cleanup. " : "") +
            "Available space is for the running account and can change. This is not a filesystem or hardware health verdict. No files removed.", Evidence: evidence);
    }
}
