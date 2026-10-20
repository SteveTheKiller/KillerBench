using System.Diagnostics;
using System.IO;
using System.Text;

namespace KillerBench.Services;

/// <summary>
/// Release checking and one-click self-update against the GitHub releases of
/// <see cref="AppInfo.GitHubRepo"/>, in the family's three steps so AboutController owns every piece
/// of UI and the shutdown: <see cref="CheckAsync"/> finds a newer tag, <see cref="DownloadAsync"/>
/// fetches and verifies the package, <see cref="StartSwap"/> launches the script that installs it.
///
/// KillerBench installs the way KillerPDF does rather than by copying an exe over itself. An
/// installed copy hands the verified installer the same /install-user or /silent command a manual
/// upgrade uses, into the folder it is already installed in. A portable copy replaces its launcher
/// once the launcher and this process have exited. A copy that is neither, such as a development
/// build, is not updated; the download step fails and the release page opens instead.
/// </summary>
internal static class UpdateService
{
    private readonly record struct UpdateTarget(string Asset, string Executable, string? Launcher, int LauncherPid,
        bool Machine, string? InstallDirectory);

    /// <summary>Latest release page, used as the fallback whenever an automatic step fails.</summary>
    internal static string ReleasesUrl => $"{SelfUpdate.Repo}/releases/latest";

    /// <summary>Release page for one version.</summary>
    internal static string ReleaseUrl(string version) => $"{SelfUpdate.Repo}/releases/tag/v{version}";

    /// <summary>The newer stable release's tag, or null when this copy is current, offline, or there
    /// is no release yet.</summary>
    internal static Task<string?> CheckAsync(System.Version? current) =>
        current is null ? Task.FromResult<string?>(null) : SelfUpdate.FindNewerReleaseAsync(current);

    /// <summary>Downloads the package that matches this copy, checks it against the published
    /// checksum and the publisher's signature, and returns the temporary file. Throws when this copy
    /// cannot be updated or anything at all went wrong: an update that cannot be verified must not be
    /// installed.</summary>
    internal static async Task<string> DownloadAsync(string tag)
    {
        UpdateTarget target = FindTarget()
            ?? throw new InvalidOperationException("This copy is neither installed nor portable.");
        string download = await SelfUpdate.DownloadVerifiedAsync(tag, target.Asset).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The package could not be downloaded or did not match its checksum.");
        // The checksum proves the file is the one on the release page; the signature proves the
        // release page carried a package Steve signed.
        var (valid, subject, _) = CodeSignature.GetSignerInfo(download);
        if (!valid || !subject.Contains(AppInfo.SignerName, StringComparison.OrdinalIgnoreCase))
        {
            DiscardDownload(download);
            throw new InvalidOperationException("The package is not signed by the publisher.");
        }
        return download;
    }

    /// <summary>Writes the update script and starts it. Throws if it cannot be started, including
    /// when the user declines the UAC prompt, so the caller only shuts down once it is running. The
    /// installer records the new version itself, so unlike an exe swap there is no registry to fix.</summary>
    internal static void StartSwap(string download)
    {
        UpdateTarget target = FindTarget()
            ?? throw new InvalidOperationException("This copy is neither installed nor portable.");
        int pid = Environment.ProcessId;
        string replaced = target.Launcher ?? target.Executable;
        // A machine-wide install or a portable copy in a protected folder is not writable by a normal
        // user, so the script runs elevated there. It relaunches through explorer.exe so the app comes
        // back at the user's normal integrity level.
        bool elevate = target.Machine || !CanWriteTo(Path.GetDirectoryName(replaced)!);
        string explorer = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
        string relaunch = elevate ? $"start \"\" \"{Bat(explorer)}\" \"{Bat(replaced)}\"" : $"start \"\" \"{Bat(replaced)}\"";
        string system = Environment.GetFolderPath(Environment.SpecialFolder.System);
        string tasklist = Bat(Path.Combine(system, "tasklist.exe"));
        string find = Bat(Path.Combine(system, "find.exe"));
        string ping = Bat(Path.Combine(system, "ping.exe"));

        var script = new StringBuilder()
            .AppendLine("@echo off")
            .AppendLine("setlocal DisableDelayedExpansion")
            .AppendLine(":waitapp")
            .AppendLine($"\"{tasklist}\" /fi \"PID eq {pid}\" 2>nul | \"{find}\" \"{pid}\" >nul")
            .AppendLine($"if not errorlevel 1 ( \"{ping}\" -n 2 127.0.0.1 >nul & goto waitapp )");
        if (target.Launcher is not null)
        {
            if (target.LauncherPid > 0)
                script.AppendLine(":waitlauncher")
                      .AppendLine($"\"{tasklist}\" /fi \"PID eq {target.LauncherPid}\" 2>nul | \"{find}\" \"{target.LauncherPid}\" >nul")
                      .AppendLine($"if not errorlevel 1 ( \"{ping}\" -n 2 127.0.0.1 >nul & goto waitlauncher )");
            script.AppendLine(UpdateImagePublication.Command(download, target.Launcher, elevate,
                      ReleasesUrl, AppInfo.DisplayName) + " >\"%~dpn0.helper.log\" 2>&1")
                  .AppendLine("if errorlevel 1 goto failed")
                  .AppendLine("goto cleanup");
        }
        else
        {
            // Base64 keeps the folder intact through the batch file whatever characters it holds.
            string folder = Convert.ToBase64String(Encoding.UTF8.GetBytes(target.InstallDirectory!));
            script.AppendLine($"start /wait \"\" \"{Bat(download)}\" {(target.Machine ? "/silent" : "/install-user")} /install-dir64:{folder}")
                  .AppendLine("if errorlevel 1 goto failed");
        }
        script.AppendLine(relaunch)
              .AppendLine("goto cleanup")
              .AppendLine(":failed")
              .AppendLine($"start \"\" \"{ReleasesUrl}\"")
              .AppendLine(":cleanup");
        if (target.Launcher is null) script.AppendLine($"del \"{Bat(download)}\" >nul 2>&1");
        script.AppendLine("del \"%~f0\" >nul 2>&1");
        string batch = Path.Combine(Path.GetTempPath(), $"killerbench_update_{Guid.NewGuid():N}.bat");
        File.WriteAllText(batch, script.ToString());

        var start = new ProcessStartInfo(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"),
            $"/d /v:off /c \"{Path.GetFileName(batch)}\"")
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            WorkingDirectory = Path.GetTempPath(),
            UseShellExecute = true
        };
        if (elevate) start.Verb = "runas";
        if (Process.Start(start) is null)
            throw new IOException("The update helper did not start. Download retained at: " + download);
    }

    /// <summary>Deletes a downloaded update that will not be installed.</summary>
    internal static void DiscardDownload(string? path)
    {
        try { if (path is not null && File.Exists(path)) File.Delete(path); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    private static UpdateTarget? FindTarget()
    {
        string? executable = Environment.ProcessPath;
        if (string.IsNullOrEmpty(executable)) return null;
        string? launcher = Environment.GetEnvironmentVariable("KILLERBENCH_LAUNCHER_PATH");
        if (!string.IsNullOrWhiteSpace(launcher) && File.Exists(launcher))
        {
            _ = int.TryParse(Environment.GetEnvironmentVariable("KILLERBENCH_LAUNCHER_PID"), out int launcherPid);
            return new UpdateTarget(SelfUpdate.PortableAsset, executable, launcher, launcherPid, false, null);
        }
        var installed = App.InstalledCopy();
        return installed is null
            ? null
            : new UpdateTarget(SelfUpdate.InstallerAsset, executable, null, 0, installed.Value.Machine, installed.Value.Directory);
    }

    /// <summary>A literal percent sign in a batch file must be doubled.</summary>
    private static string Bat(string value) => value.Replace("%", "%%", StringComparison.Ordinal);

    private static bool CanWriteTo(string folder)
    {
        try
        {
            string probe = Path.Combine(folder, $".kb_write_{Guid.NewGuid():N}.tmp");
            using (new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose)) { }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { return false; }
    }
}
