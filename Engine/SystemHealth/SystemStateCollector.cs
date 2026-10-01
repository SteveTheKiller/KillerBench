using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace KillerBench.Engine.SystemHealth;

public static class WindowsState
{
    public static bool IsAdministrator()
    {
        if (!OperatingSystem.IsWindows()) return false;
        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }

    [SupportedOSPlatform("windows")]
    public static bool RebootPending() => RestartDiagnostics.Check().State != CheckState.Clear;
}

public sealed class SystemStateCollector : IEvidenceCollector
{
    public string Name => "System state";
    public Task<CaptureSource> CollectAsync(CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        cancellationToken.ThrowIfCancellationRequested();
        var lines = new List<string> { "SYSTEM STATE / READ ONLY", $"Captured UTC: {DateTimeOffset.UtcNow:O}",
            $"Uptime: {TimeSpan.FromMilliseconds(Environment.TickCount64)}", $"Logical processors: {Environment.ProcessorCount}",
            $"Restart readiness blocked: {WindowsState.RebootPending()} (CBS, Windows Update, pending file renames, computer rename, update executable)" };
        string drive = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.Windows))!;
        var disk = new DriveInfo(drive);
        lines.Add($"System disk {drive}: {disk.AvailableFreeSpace:N0} bytes available / {disk.TotalSize:N0} bytes total");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var update = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate");
        using var au = update?.OpenSubKey("AU");
        foreach (string name in new[] { "WUServer", "WUStatusServer", "DoNotConnectToWindowsUpdateInternetLocations", "DisableWindowsUpdateAccess" })
            lines.Add($"Policy {name}: {update?.GetValue(name) ?? "Not configured"}");
        foreach (string name in new[] { "UseWUServer", "NoAutoUpdate", "AUOptions" }) lines.Add($"Policy {name}: {au?.GetValue(name) ?? "Not configured"}");
        return Task.FromResult(new CaptureSource(Name, "Text", CollectionStatus.Collected, "Selected system state and update policies; no settings changed.", new("System state", [.. lines], false)));
    }
}

public sealed class UpdateServicesCollector : IEvidenceCollector
{
    public string Name => "Update services";
    public async Task<CaptureSource> CollectAsync(CancellationToken cancellationToken)
    {
        var lines = new List<string> { "UPDATE SERVICES / READ ONLY" };
        foreach (string service in new[] { "wuauserv", "BITS", "CryptSvc", "TrustedInstaller" })
        {
            var result = await NativeTool.RunAsync("sc.exe", ["query", service], token: cancellationToken);
            if (result.ExitCode != 0) throw new IOException($"Service query for {service} failed with exit {result.ExitCode}.");
            lines.AddRange(result.Output.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
        }
        return new(Name, "Text", CollectionStatus.Collected, "Service states only. A stopped demand-start service can be normal.", new(Name, [.. lines], false));
    }
}
