namespace KillerBench.Engine.Windows.Automation;

public sealed class WindowsMacePackageRemovalHost : IMacePackageRemovalHost
{
    private readonly Func<string, CancellationToken, string[], Task<ToolResult>> runCommand;
    public WindowsMacePackageRemovalHost(Func<string, CancellationToken, string[], Task<ToolResult>>? runCommand = null) =>
        this.runCommand = runCommand ?? ((script, token, arguments) => BoundedTool.RunAsync(new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script, .. arguments], false), TimeSpan.FromMinutes(2), token: token));

    public Task<MaceRemovalReview> ReviewAsync(MaceReviewOptions options, CancellationToken token) =>
        MacePlanning.ReviewCurrentAsync(options, token: token);

    public Task<ToolResult> RemoveCurrentAccountPackageAsync(string packageFullName, CancellationToken token)
    {
        ValidatePackageFullName(packageFullName);
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("App package removal requires Windows.");

        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" + AppxSelection.Functions +
            "& { param([string]$packageFullName) " +
            "$package=ExactCurrentPackage $packageFullName;" +
            "$current=ExactCurrentPackage $packageFullName; if([string]$current.InstallLocation -cne [string]$package.InstallLocation){throw 'Selected package registration changed.'};" +
            "Remove-AppxPackage -Package $packageFullName -ErrorAction Stop|Out-Null }";
        return runCommand(script, token, [packageFullName]);
    }

    private static void ValidatePackageFullName(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (value.Length > 512 || value != value.Trim() || value.Any(character =>
            !(char.IsAsciiLetterOrDigit(character) || character is '.' or '_' or '-')))
            throw new ArgumentException("Select one exact app package full name.", nameof(value));
    }
}
