namespace KillerBench.Engine;

public sealed class WindowsMacePackageRemovalHost : IMacePackageRemovalHost
{
    public Task<MaceRemovalReview> ReviewAsync(MaceReviewOptions options, CancellationToken token) =>
        MacePlanning.ReviewCurrentAsync(options, token: token);

    public Task<ToolResult> RemoveCurrentAccountPackageAsync(string packageFullName, CancellationToken token)
    {
        ValidatePackageFullName(packageFullName);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("App package removal requires Windows.");

        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "& { param([string]$packageFullName) " +
            "$package=@(Get-AppxPackage -Package $packageFullName -ErrorAction Stop);" +
            "if($package.Count-ne 1){throw 'Exact current-account package registration is unavailable.'};" +
            "if([string]$package[0].PackageFullName -cne $packageFullName){throw 'Exact package identity changed.'};" +
            "Remove-AppxPackage -Package $packageFullName -ErrorAction Stop|Out-Null }";
        return BoundedTool.RunAsync(new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script, packageFullName], false),
            TimeSpan.FromMinutes(2), token: token);
    }

    private static void ValidatePackageFullName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 512 || value != value.Trim() || value.Any(character =>
            !(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')))
            throw new ArgumentException("Select one exact app package full name.", nameof(value));
    }
}
