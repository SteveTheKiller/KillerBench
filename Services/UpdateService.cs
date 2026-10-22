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

    /// <summary>Starts the update helper. Throws if it cannot be started, including when the user
    /// declines the UAC prompt, so the caller only shuts down once it is running. The installer
    /// records the new version itself, so unlike an exe swap there is no registry to fix.</summary>
    internal static void StartSwap(string download)
    {
        UpdateTarget target = FindTarget()
            ?? throw new InvalidOperationException("This copy is neither installed nor portable.");
        int pid = Environment.ProcessId;
        string replaced = target.Launcher ?? target.Executable;
        // A machine-wide install or a portable copy in a protected folder is not writable by a normal
        // user, so the helper runs elevated there. It relaunches through explorer.exe so the app comes
        // back at the user's normal integrity level.
        bool elevate = target.Machine || !CanWriteTo(Path.GetDirectoryName(replaced)!);
        // The whole helper travels in the process command line, so nothing the user account can
        // write is read after a consented elevated helper starts.
        string script;
        if (target.Launcher is not null)
            script = UpdateHelperLaunch.WaitFor(pid, target.LauncherPid) +
                UpdateImagePublication.Script(download, target.Launcher, elevate, ReleasesUrl, AppInfo.DisplayName);
        else
        {
            string folder = Convert.ToBase64String(Encoding.UTF8.GetBytes(target.InstallDirectory!));
            // An elevated helper stages under Program Files, where only administrators can write.
            string stageRoot = elevate
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppInfo.DisplayName + " Update")
                : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppInfo.DisplayName, "UpdateStage");
            script = UpdateHelperLaunch.WaitFor(pid) + UpdateHelperLaunch.Installer(download,
                UpdateImagePublication.VerifiedHash(download), AppInfo.SignerName,
                (target.Machine ? "/silent" : "/install-user") + " /install-dir64:" + folder, elevate, replaced, stageRoot, ReleasesUrl);
        }
        if (Process.Start(UpdateHelperLaunch.StartInfo(script, elevate)) is null)
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
