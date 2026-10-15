using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using KillerBench.Services;

namespace KillerBench;

// The About card's update notice and the startup update check, following KillerPDF. An installed
// copy hands the verified installer the same /install-user or /silent command a manual upgrade uses,
// into the folder it is already installed in. A portable copy replaces its launcher once the
// launcher and this process have exited. A copy that is neither, such as a development build, opens
// the release page instead of installing anything.
public partial class MainWindow
{
    private string? updateTag;
    private bool updateInProgress;
    private static bool startupUpdateCheckStarted;

    private readonly record struct UpdateTarget(string Asset, string Executable, string? Launcher, int LauncherPid,
        bool Machine, string? InstallDirectory);

    private static bool StartupUpdateCheckEnabled
    {
        get => SelfUpdate.StartupCheckEnabled(Preferences.Get(SelfUpdate.StartupCheckSetting));
        set
        {
            Preferences.Set(SelfUpdate.StartupCheckSetting, value ? "1" : "0");
            Preferences.Save();
        }
    }

    private static Version RunningVersion => typeof(App).Assembly.GetName().Version ?? new Version(0, 0, 0);

    private string UpdateString(string key, string fallback) => TryFindResource(key) as string ?? fallback;

    /// <summary>Called as the About card opens: syncs the checkbox and looks for a newer release.</summary>
    private async void RefreshUpdateNotice()
    {
        AboutStartupUpdateCheck.IsChecked = StartupUpdateCheckEnabled;
        if (updateInProgress) return;
        string? tag = await SelfUpdate.FindNewerReleaseAsync(RunningVersion);
        if (tag is not null && !updateInProgress) ShowUpdateNotice(tag);
    }

    private void ShowUpdateNotice(string tag)
    {
        updateTag = tag;
        AboutUpdateText.Text = string.Format(UpdateString("Str_About_UpdateAvailable", "Update available: {0}"), tag);
        AboutUpdateButton.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Once per process, after the window has painted: when a newer release exists, offers it with an
    /// "Always check on startup" box so the check can be turned off right there. The About card's
    /// checkbox turns it back on. Skipped in demo and screenshot modes and under the checks, which
    /// host the window in their own Application, and never stacked on an open dialog.
    /// </summary>
    private async void CheckForUpdatesOnStartup()
    {
        if (startupUpdateCheckStarted || Application.Current is not App || App.DemoMode || App.ScreenshotMode
            || !StartupUpdateCheckEnabled) return;
        startupUpdateCheckStarted = true;
        string? tag = await SelfUpdate.FindNewerReleaseAsync(RunningVersion);
        if (tag is null || updateInProgress) return;
        ShowUpdateNotice(tag);

        if (!IsVisible || OwnedWindows.Count > 0) return;
        var dialog = new Controls.ConfirmDialog(
            string.Format(UpdateString("Str_About_StartupUpdateHeading", "{0} {1} is available"), "KillerBench", tag),
            UpdateString("Str_About_StartupUpdateDetail",
                "Would you like to download and install it now? The app will close and reopen automatically."),
            UpdateString("Str_About_UpdateNow", "Update"), UpdateString("Str_About_NotNow", "Not now"),
            UpdateString("Str_About_AlwaysCheckOnStartup", "Always check on startup"), StartupUpdateCheckEnabled)
        { Owner = this };
        dialog.ShowDialog();
        StartupUpdateCheckEnabled = dialog.Check1Checked;
        AboutStartupUpdateCheck.IsChecked = dialog.Check1Checked;
        if (dialog.Confirmed) await InstallUpdateAsync(tag);
    }

    // Click, not Checked/Unchecked, so only a person's click saves; the sync above never writes.
    private void OnAboutStartupUpdateCheck(object sender, RoutedEventArgs e)
        => StartupUpdateCheckEnabled = AboutStartupUpdateCheck.IsChecked == true;

    private async void OnAboutUpdate(object sender, RoutedEventArgs e)
    {
        string? tag = updateTag;
        if (string.IsNullOrEmpty(tag) || updateInProgress) return;
        var dialog = new Controls.ConfirmDialog(
            string.Format(UpdateString("Str_About_StartupUpdateHeading", "{0} {1} is available"), "KillerBench", tag),
            UpdateString("Str_About_StartupUpdateDetail",
                "Would you like to download and install it now? The app will close and reopen automatically."),
            UpdateString("Str_About_UpdateNow", "Update")) { Owner = this };
        dialog.ShowDialog();
        if (dialog.Confirmed) await InstallUpdateAsync(tag);
    }

    /// <summary>Downloads and verifies the release, then hands off to the script that installs it and
    /// reopens the app. Every failure leaves this copy untouched and opens the release page.</summary>
    private async Task InstallUpdateAsync(string tag)
    {
        if (updateInProgress) return;
        // Closing the app would interrupt the repair, and a repair must finish its current command.
        if (repairRunning)
        {
            Report("The update waits until the running repair finishes. Open About to update afterward.");
            return;
        }
        UpdateTarget? target = FindUpdateTarget();
        if (target is null)
        {
            OpenUrl($"{SelfUpdate.Repo}/releases/tag/{tag}");
            return;
        }

        updateInProgress = true;
        AboutUpdateButton.IsEnabled = false;
        AboutUpdateText.Text = UpdateString("Str_About_Downloading", "Downloading...");
        string? download = null;
        try
        {
            download = await SelfUpdate.DownloadVerifiedAsync(tag, target.Value.Asset);
            // The checksum proves the file is the one on the release page; the signature proves the
            // release page carried a package Steve signed.
            if (download is null || !IsSignedUpdate(download))
            {
                OpenUrl($"{SelfUpdate.Repo}/releases/latest");
                return;
            }
            if (repairRunning)
            {
                Report("The update waits until the running repair finishes. Open About to update afterward.");
                return;
            }
            if (LaunchUpdateAndExit(download, target.Value)) download = null;
            else OpenUrl($"{SelfUpdate.Repo}/releases/latest");
        }
        finally
        {
            if (download is not null)
                try { File.Delete(download); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            updateInProgress = false;
            AboutUpdateButton.IsEnabled = true;
            AboutUpdateText.Text = string.Format(UpdateString("Str_About_UpdateAvailable", "Update available: {0}"), tag);
        }
    }

    private static bool IsSignedUpdate(string path)
    {
        var (valid, subject, _) = GetSignerInfo(path);
        return valid && subject.Contains(SignerName, StringComparison.OrdinalIgnoreCase);
    }

    private static UpdateTarget? FindUpdateTarget()
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

    /// <summary>Writes the update script, starts it, and shuts the app down. Returns false if the
    /// script could not be started, in which case nothing has been changed.</summary>
    private static bool LaunchUpdateAndExit(string download, UpdateTarget target)
    {
        try
        {
            int pid = Environment.ProcessId;
            string replaced = target.Launcher ?? target.Executable;
            // A machine-wide install or a portable copy in a protected folder is not writable by a
            // normal user, so the script runs elevated there. It relaunches through explorer.exe so
            // the app comes back at the user's normal integrity level.
            bool elevate = target.Machine || !CanWriteTo(Path.GetDirectoryName(replaced)!);
            string relaunch = elevate ? $"start \"\" explorer.exe \"{Bat(replaced)}\"" : $"start \"\" \"{Bat(replaced)}\"";

            var script = new StringBuilder()
                .AppendLine("@echo off")
                .AppendLine(":waitapp")
                .AppendLine($"tasklist /fi \"PID eq {pid}\" 2>nul | find \"{pid}\" >nul")
                .AppendLine("if not errorlevel 1 ( ping -n 2 127.0.0.1 >nul & goto waitapp )");
            if (target.Launcher is not null)
            {
                if (target.LauncherPid > 0)
                    script.AppendLine(":waitlauncher")
                          .AppendLine($"tasklist /fi \"PID eq {target.LauncherPid}\" 2>nul | find \"{target.LauncherPid}\" >nul")
                          .AppendLine("if not errorlevel 1 ( ping -n 2 127.0.0.1 >nul & goto waitlauncher )");
                script.AppendLine($"attrib -r \"{Bat(target.Launcher)}\" >nul 2>&1")
                      .AppendLine($"copy /y \"{Bat(download)}\" \"{Bat(target.Launcher)}\" >nul 2>&1")
                      .AppendLine("if errorlevel 1 goto failed");
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
                  .AppendLine($"start \"\" \"{SelfUpdate.Repo}/releases/latest\"")
                  .AppendLine(":cleanup")
                  .AppendLine($"del \"{Bat(download)}\" >nul 2>&1")
                  .AppendLine("del \"%~f0\" >nul 2>&1");
            string batch = Path.Combine(Path.GetTempPath(), $"killerbench_update_{Guid.NewGuid():N}.bat");
            File.WriteAllText(batch, script.ToString());

            var start = new ProcessStartInfo("cmd.exe", $"/c \"{batch}\"")
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Path.GetTempPath(),
                UseShellExecute = true
            };
            if (elevate) start.Verb = "runas";
            // Declining UAC throws, so the app only closes once the script is actually running.
            Process.Start(start);
            Application.Current.Shutdown();
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException
            or System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
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
