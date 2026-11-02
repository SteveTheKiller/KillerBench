namespace KillerBench.Engine.Profiles;

public static class ProfileBackupCopy
{
    private static readonly string[] FixedOptions =
    [
        "/E", "/COPYALL", "/DCOPY:DAT", "/B", "/EFSRAW", "/XJ", "/SL",
        "/R:1", "/W:1", "/NFL", "/NDL", "/NJH", "/NJS", "/NP"
    ];

    public static RepairStep CreateStep(ProfileSnapshot target, string destinationPath, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        token.ThrowIfCancellationRequested();
        string destination = ProfileBackupExecution.ValidateDestination(target, destinationPath);
        string source = Path.GetFullPath(target.Path).TrimEnd('\\');
        var arguments = new List<string> { source, destination };
        arguments.AddRange(FixedOptions);
        // Exclude only supported links. Ordinary data beside an app alias still belongs in the backup.
        if (Directory.Exists(source))
        {
            var links = ProfileBackupPathGuard.OmittedLinks(source, token, rejectEncrypted: true);
            foreach (bool directories in new[] { true, false })
            {
                string[] excluded = links.Where(link => ((File.GetAttributes(link) & FileAttributes.Directory) != 0) == directories).ToArray();
                if (excluded.Length == 0) continue;
                arguments.Add(directories ? "/XD" : "/XF");
                arguments.AddRange(excluded);
            }
        }
        return new("robocopy.exe", arguments.ToArray(), false);
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
    private (ProfileSnapshot Target, string Destination, ProfileBackupVerification Verification)? completed;
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

    public Task<string?> BoundaryProblemAsync(ProfileSnapshot target, string destinationPath, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            using var held = ProfileBackupPathGuard.Open(target.Path, destinationPath);
            return Task.FromResult(ProfileBackupPathGuard.UnsupportedEntry(Path.GetFullPath(target.Path).TrimEnd('\\'), token));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or
            NotSupportedException or System.ComponentModel.Win32Exception)
        {
            return Task.FromResult<string?>("The profile or backup path could not be checked: " + error.Message);
        }
    }

    public async Task CopyAsync(ProfileSnapshot target, string destinationPath, IProgress<string>? progress,
        CancellationToken token)
    {
        completed = null;
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Profile copying requires Windows.");
        token.ThrowIfCancellationRequested();
        string source = Path.GetFullPath(target.Path).TrimEnd('\\');
        string destination = ProfileBackupExecution.ValidateDestination(target, destinationPath);
        // Held through the copy: the final check and the native dispatch see the same folders.
        using var held = ProfileBackupPathGuard.Open(source, destination);
        if (ProfileBackupPathGuard.UnsupportedEntry(source, token) is { } unsupported)
            throw new InvalidOperationException(unsupported);
        if (!Directory.Exists(source) || (File.GetAttributes(source) & FileAttributes.ReparsePoint) != 0 ||
            Directory.Exists(destination) || File.Exists(destination))
            throw new InvalidOperationException("The exact source and absent destination are required before profile backup copying.");

        var verified = await ProfileBackupTransaction.CopyAsync(target, destination, progress, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        completed = (target, destination, verified);
    }

    public Task<ProfileBackupVerification> VerifyAsync(ProfileSnapshot target, string destinationPath, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var proof = completed;
        completed = null;
        return Task.FromResult(proof is { } value && value.Target == target &&
            value.Destination.Equals(Path.GetFullPath(destinationPath).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)
            ? value.Verification : new ProfileBackupVerification(null, false, "No completed protected copy is available for this exact request."));
    }
}
