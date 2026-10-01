namespace KillerBench.Engine.Profiles;

public static class ProfileBackupCopy
{
    private static readonly string[] FixedOptions =
    [
        "/E", "/COPYALL", "/DCOPY:DAT", "/B", "/EFSRAW", "/XJ", "/SL",
        "/R:1", "/W:1", "/NFL", "/NDL", "/NJH", "/NJS", "/NP"
    ];

    public static RepairStep CreateStep(ProfileSnapshot target, string destinationPath)
    {
        ArgumentNullException.ThrowIfNull(target);
        string destination = ProfileBackupExecution.ValidateDestination(target, destinationPath);
        string source = Path.GetFullPath(target.Path).TrimEnd('\\');
        return new("robocopy.exe", [source, destination, .. FixedOptions], false);
    }

    public static bool Accepted(ToolResult result) => result is
    {
        ExitCode: >= 0 and <= 7,
        TimedOut: false,
        Truncated: false,
        Error: ""
    };
}

public sealed class WindowsProfileBackupHost : IProfileBackupHost
{
    public Task<ProfileInventoryResult> InventoryAsync(CancellationToken token) => ProfileInventory.ReadAsync(token);

    public Task<ProfileDependencyResult> DependenciesAsync(CancellationToken token) => ProfileDependencies.ReadAsync(token);

    public Task<ProfileRegistrationResult> RegistrationsAsync(CancellationToken token) => ProfileRegistrations.ReadAsync(token);

    public Task<bool> ReadyAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult(OperatingSystem.IsWindows() && WindowsState.IsAdministrator());
    }

    public Task<bool> DestinationAbsentAsync(string destinationPath, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            string destination = Path.GetFullPath(destinationPath);
            string root = Path.GetPathRoot(destination)!;
            var drive = new DriveInfo(root);
            return Task.FromResult(drive.IsReady && drive.DriveFormat.Equals("NTFS", StringComparison.OrdinalIgnoreCase) &&
                !Directory.Exists(destination) && !File.Exists(destination));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Task.FromResult(false);
        }
    }

    public async Task CopyAsync(ProfileSnapshot target, string destinationPath, IProgress<string>? progress,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        RepairStep step = ProfileBackupCopy.CreateStep(target, destinationPath);
        string source = step.Arguments[0];
        string destination = step.Arguments[1];
        if (!Directory.Exists(source) || (File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0 ||
            Directory.Exists(destination) || File.Exists(destination))
            throw new InvalidOperationException("The exact source and absent destination are required before profile backup copying.");

        progress?.Report("Copying the selected unloaded profile into the new local backup folder...");
        ToolResult result = await BoundedTool.RunAsync(step, TimeSpan.FromHours(2), progress, token).ConfigureAwait(false);
        if (!ProfileBackupCopy.Accepted(result) || !Directory.Exists(destination))
            throw new IOException("The profile copy did not return a verified Robocopy completion. The destination was preserved for review.");
        progress?.Report("The copy provider completed. Verifying every regular source and destination file...");
    }
}
