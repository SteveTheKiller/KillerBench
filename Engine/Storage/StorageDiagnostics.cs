namespace KillerBench.Engine.Storage;

public sealed record StorageVolumeState(bool Ready, bool Fixed, string? FileSystem);
public interface IStorageDiagnosticHost
{
    Task<StorageVolumeState> InspectAsync(string volume, CancellationToken token);
    Task<ToolResult> RunAsync(RepairStep step, TimeSpan timeout, IProgress<string>? progress, CancellationToken token);
}

public static class StorageDiagnostics
{
    public static string SystemVolume()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Storage diagnostics require Windows.");
        return Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows))!.TrimEnd('\\');
    }

    public static WorkflowCheck Interpret(ToolResult result)
    {
        if (result.Truncated || result.TimedOut) return new("Filesystem", CheckState.Unknown, "Filesystem output was truncated or the check timed out. No conclusion can be made.", Evidence: result);
        if (result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.Error))
            return new("Filesystem", CheckState.Unknown, "The filesystem check returned diagnostic error output despite exit zero. Review both output streams before concluding the check passed. No repair was attempted.", Evidence: result);
        string[] cleanVerdicts = [.. result.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.EndsWith('.') ? line[..^1].TrimEnd() : line)
            .Where(line => line.Equals("Windows has scanned the file system and found no problems", StringComparison.OrdinalIgnoreCase) ||
                line.Equals("Windows has checked the file system and found no problems", StringComparison.OrdinalIgnoreCase))];
        return result.ExitCode switch
        {
            0 when cleanVerdicts.Length == 1
                => new("Filesystem", CheckState.Clear, "The read-only filesystem check reported no problems. Physical disk health is not assessed.", Evidence: result),
            1 or 2 or 3 => new("Filesystem", CheckState.Attention, "The filesystem check reported errors or could not complete. A read-only check of an active volume can report transient inconsistencies. Back up important data and verify offline before any repair; no repair or restart was scheduled.", Evidence: result),
            _ => new("Filesystem", CheckState.Unknown, $"Filesystem check result was not recognized (exit {result.ExitCode}). No repair was attempted.", Evidence: result)
        };
    }

    public static string NormalizeVolume(string volume)
    {
        if (volume is null || volume.Length is < 2 or > 3 || !char.IsAsciiLetter(volume[0]) || volume[1] != ':' ||
            (volume.Length == 3 && volume[2] != '\\'))
            throw new ArgumentException("Choose a local drive letter such as C:. Paths, network shares and command switches are not accepted.", nameof(volume));
        return char.ToUpperInvariant(volume[0]) + ":";
    }

    internal static Task<WorkflowCheck> CheckAsync(IProgress<string>? progress, CancellationToken token)
        => CheckVolumeAsync(SystemVolume(), progress, token);

    public static RepairStep PlanCheck(string volume)
        => new("chkdsk.exe", [NormalizeVolume(volume)], true);

    public static Task<WorkflowCheck> CheckVolumeAsync(string volume, IProgress<string>? progress = null, CancellationToken token = default)
        => CheckVolumeAsync(volume, new NativeStorageHost(), progress, token);

    public static async Task<WorkflowCheck> CheckVolumeAsync(string volume, IStorageDiagnosticHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        volume = NormalizeVolume(volume);
        token.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(host);
        progress = progress is null ? null : new DiagnosticProgress(progress);
        using var inspection = CancellationTokenSource.CreateLinkedTokenSource(token);
        inspection.CancelAfter(TimeSpan.FromSeconds(20));
        StorageVolumeState disk;
        try
        {
            var pending = host.InspectAsync(volume, inspection.Token) ?? throw new InvalidOperationException("Volume provider returned no inspection task.");
            disk = await pending.WaitAsync(inspection.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && inspection.IsCancellationRequested)
        { return new("Filesystem", CheckState.Unknown, $"Volume {volume} inspection timed out. No filesystem command started."); }
        catch (Exception error) when (UnavailableProvider(error) || error is OperationCanceledException && !token.IsCancellationRequested)
        { return new("Filesystem", CheckState.Unknown, $"Volume {volume} readiness could not be read. No filesystem command started.", Evidence: new(-1, "", error.Message, false)); }
        token.ThrowIfCancellationRequested();
        if (disk is null)
            return new("Filesystem", CheckState.Unknown, $"Volume {volume} readiness evidence is missing. No filesystem command started.");
        if (!disk.Ready || !disk.Fixed || !string.Equals(disk.FileSystem, "NTFS", StringComparison.OrdinalIgnoreCase))
            return new("Filesystem", CheckState.Unknown,
                !disk.Ready ? $"Volume {volume} is not ready. Filesystem condition is unverified; no scan or repair was attempted." :
                $"Volume {volume} is outside this check's supported fixed-NTFS scope. Filesystem condition is unverified; no scan or repair was attempted.",
                Evidence: new(0, System.Text.Json.JsonSerializer.Serialize(new { Volume = volume, disk.Ready, disk.Fixed, disk.FileSystem }, Capture.JsonOptions), "", false));
        progress?.Report($"Checking {volume} without repair switches. No repair or restart will be scheduled.");
        token.ThrowIfCancellationRequested();
        using var scan = CancellationTokenSource.CreateLinkedTokenSource(token);
        scan.CancelAfter(TimeSpan.FromMinutes(10));
        ToolResult result;
        try
        {
            var pending = host.RunAsync(PlanCheck(volume), TimeSpan.FromMinutes(10), progress, scan.Token)
                ?? throw new InvalidOperationException("Filesystem provider returned no command task.");
            result = await pending.WaitAsync(scan.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && scan.IsCancellationRequested)
        { return new("Filesystem", CheckState.Unknown, $"Volume {volume} read-only check timed out. Filesystem state remains unverified."); }
        catch (Exception error) when (UnavailableProvider(error) || error is OperationCanceledException && !token.IsCancellationRequested)
        { return new("Filesystem", CheckState.Unknown, $"Volume {volume} read-only check could not produce a result. Filesystem state remains unverified; no repair was requested.", Evidence: new(-1, "", error.Message, false)); }
        token.ThrowIfCancellationRequested();
        if (result is null)
            return new("Filesystem", CheckState.Unknown, $"Volume {volume} command evidence is missing. Filesystem state remains unverified.");
        var check = Interpret(result);
        return check with { Detail = $"Volume: {volume}. " + check.Detail };
    }

    private static bool UnavailableProvider(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or System.Security.SecurityException;

    private sealed class NativeStorageHost : IStorageDiagnosticHost
    {
        public Task<StorageVolumeState> InspectAsync(string volume, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Storage diagnostics require Windows.");
            var disk = new DriveInfo(volume + "\\");
            bool ready = disk.IsReady;
            return Task.FromResult(new StorageVolumeState(ready, disk.DriveType == DriveType.Fixed, ready ? disk.DriveFormat : null));
        }
        public Task<ToolResult> RunAsync(RepairStep step, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
            => BoundedTool.RunAsync(step, timeout, progress, token);
    }
}
