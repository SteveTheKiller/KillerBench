using System.IO;
using Path = System.IO.Path;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;

namespace KillerBench;

// The installed half of the family setup. The verified launcher (Packaging\KillerLauncher) stages
// the payload and then hands off here: /register-user and /register-machine write shortcuts and the
// Installed Apps entry from the final installed path, /remove-user-install clears a per-user copy
// that a machine-wide install supersedes, and /uninstall and /uninstall-silent are what Installed
// Apps and package managers call.
public partial class App
{
    private const string AppName = "KillerBench";
    private const string RegistryKeyPath = @"Software\KillerBench";
    private const string UninstallKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\KillerBench";

    private static readonly string UserInstallDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", AppName);
    private static readonly string MachineInstallDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppName);

    private static string StartMenuDir(bool machine) => Path.Combine(Environment.GetFolderPath(machine
        ? Environment.SpecialFolder.CommonPrograms : Environment.SpecialFolder.Programs), AppName);

    private static string DesktopShortcut(bool machine) => Path.Combine(Environment.GetFolderPath(machine
        ? Environment.SpecialFolder.CommonDesktopDirectory : Environment.SpecialFolder.DesktopDirectory), AppName + ".lnk");

    private static bool HasArgument(StartupEventArgs e, string name) =>
        e.Args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>Handles the headless launcher hand-offs. Runs before any window, theme or engine
    /// setup and never touches the main window.</summary>
    private bool TryRunInstallerCommand(StartupEventArgs e)
    {
        if (HasArgument(e, "/register-user") || HasArgument(e, "/register-machine"))
        {
            try
            {
                RegisterInstalledCopy(HasArgument(e, "/register-machine"), HasArgument(e, "/desktop"));
                Shutdown(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Install registration failed: " + ex.Message);
                Shutdown(KillerFamily.Deployment.InstallerExitCodes.FromException(ex));
            }
            return true;
        }
        if (HasArgument(e, "/remove-user-install"))
        {
            RemoveInstalledCopy(machine: false, UserInstallDir);
            Shutdown(0);
            return true;
        }
        return false;
    }

    /// <summary>Handles /uninstall and /uninstall-silent. Runs after the theme and language are
    /// loaded so the confirmation uses the family dialog.</summary>
    private bool TryRunUninstall(StartupEventArgs e)
    {
        bool silent = HasArgument(e, "/uninstall-silent");
        if (!silent && !HasArgument(e, "/uninstall")) return false;
        // The confirmation closes before the result notice opens, so closing the last window must
        // not end the app in between.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        Uninstall(silent);
        Shutdown(0);
        return true;
    }

    private static void RegisterInstalledCopy(bool machine, bool desktop)
    {
        string exePath = Path.GetFullPath(Environment.ProcessPath
            ?? throw new InvalidOperationException("The installed application path is unavailable."));
        string installDirectory = Path.GetDirectoryName(exePath)
            ?? throw new InvalidOperationException("The installed application folder is unavailable.");
        RegistryKey root = machine ? Registry.LocalMachine : Registry.CurrentUser;
        string version = AppVersion();

        Directory.CreateDirectory(StartMenuDir(machine));
        CreateShortcut(Path.Combine(StartMenuDir(machine), AppName + ".lnk"), exePath);
        if (desktop) CreateShortcut(DesktopShortcut(machine), exePath);

        using (var key = root.CreateSubKey(RegistryKeyPath))
        {
            key.SetValue("Installed", 1);
            key.SetValue("InstallPath", exePath);
            key.SetValue("Version", version);
        }
        using (var key = root.CreateSubKey(UninstallKeyPath))
        {
            key.SetValue("DisplayName", AppName);
            key.SetValue("DisplayVersion", version);
            key.SetValue("Publisher", "Steve the Killer");
            key.SetValue("EstimatedSize", InstalledSizeKilobytes(installDirectory), RegistryValueKind.DWord);
            key.SetValue("InstallLocation", installDirectory);
            key.SetValue("DisplayIcon", $"{exePath},0");
            key.SetValue("UninstallString", $"\"{exePath}\" /uninstall");
            key.SetValue("QuietUninstallString", $"\"{exePath}\" /uninstall-silent");
            key.SetValue("NoModify", 1);
            key.SetValue("NoRepair", 1);
        }
        Services.CommandLinePath.Register(installDirectory, machine);
    }

    private static void Uninstall(bool silent)
    {
        string exePath = Path.GetFullPath(Environment.ProcessPath ?? string.Empty);
        string installDirectory = Path.GetDirectoryName(exePath) ?? string.Empty;
        bool machine = installDirectory.StartsWith(MachineInstallDir, StringComparison.OrdinalIgnoreCase)
            || string.Equals(Registry.LocalMachine.OpenSubKey(RegistryKeyPath)?.GetValue("InstallPath") as string,
                exePath, StringComparison.OrdinalIgnoreCase);
        if (machine && RelaunchElevated(exePath, silent)) return;

        if (!silent)
        {
            var confirm = new Controls.ConfirmDialog(
                "Uninstall KillerBench?",
                "Your reports and settings will be kept.",
                "Uninstall",
                "Cancel")
            { WindowStartupLocation = WindowStartupLocation.CenterScreen, ShowInTaskbar = true };
            confirm.ShowDialog();
            if (!confirm.Confirmed) return;
        }

        if (!CloseOtherCopies(installDirectory))
        {
            if (!silent) ShowNotice("KillerBench is still running.\nClose it, then try uninstalling again.");
            return;
        }

        RemoveInstalledCopy(machine, installDirectory, deferDirectory: true);
        if (!silent) ShowNotice("KillerBench has been uninstalled.");
    }

    // Shortcuts, registry and files for one installed copy. The running uninstaller cannot delete
    // its own folder, so that removal is deferred to a short batch file that runs after it exits.
    private static void RemoveInstalledCopy(bool machine, string installDirectory, bool deferDirectory = false)
    {
        Services.CommandLinePath.Unregister(installDirectory, machine);
        try { File.Delete(Path.Combine(StartMenuDir(machine), AppName + ".lnk")); } catch { }
        try { Directory.Delete(StartMenuDir(machine), recursive: false); } catch { }
        try { File.Delete(DesktopShortcut(machine)); } catch { }
        RegistryKey root = machine ? Registry.LocalMachine : Registry.CurrentUser;
        try { root.DeleteSubKeyTree(UninstallKeyPath, throwOnMissingSubKey: false); } catch { }
        try
        {
            using var key = root.OpenSubKey(RegistryKeyPath, writable: true);
            key?.DeleteValue("Installed", throwOnMissingValue: false);
            key?.DeleteValue("InstallPath", throwOnMissingValue: false);
            key?.DeleteValue("Version", throwOnMissingValue: false);
        }
        catch { }

        if (string.IsNullOrEmpty(installDirectory) || !Directory.Exists(installDirectory)) return;
        if (!deferDirectory)
        {
            try { Directory.Delete(installDirectory, recursive: true); } catch { }
            return;
        }
        // The batch waits for this process to exit (up to about two minutes), since the uninstaller
        // may still be showing its result notice.
        int pid = Environment.ProcessId;
        string batch = Path.Combine(Path.GetTempPath(), "killerbench_uninstall.bat");
        File.WriteAllText(batch,
            "@echo off\r\n" +
            "set n=0\r\n" +
            ":wait\r\n" +
            $"tasklist /fi \"PID eq {pid}\" | find \"{pid}\" >nul\r\n" +
            "if errorlevel 1 goto gone\r\n" +
            "set /a n+=1\r\n" +
            "if %n% geq 120 goto gone\r\n" +
            "ping -n 2 127.0.0.1 >nul\r\n" +
            "goto wait\r\n" +
            ":gone\r\n" +
            "ping -n 2 127.0.0.1 >nul\r\n" +
            $"rmdir /s /q \"{installDirectory}\"\r\n" +
            "del \"%~f0\"\r\n");
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{batch}\"")
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            UseShellExecute = true
        });
    }

    // Asks every other KillerBench running from the folder being removed to close normally, so a
    // repair in progress keeps its own prompts. Returns false when one is still open.
    private static bool CloseOtherCopies(string installDirectory)
    {
        if (string.IsNullOrEmpty(installDirectory)) return true;
        string root = installDirectory.TrimEnd('\\') + "\\";
        int self = Environment.ProcessId;
        var running = new List<Process>();
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.Id != self && (process.MainModule?.FileName ?? string.Empty)
                        .StartsWith(root, StringComparison.OrdinalIgnoreCase))
                {
                    running.Add(process);
                    continue;
                }
            }
            catch { }
            process.Dispose();
        }
        try
        {
            foreach (var process in running) if (!process.HasExited) process.CloseMainWindow();
            return running.All(process => process.HasExited || process.WaitForExit(30000));
        }
        finally
        {
            foreach (var process in running) process.Dispose();
        }
    }

    private static bool RelaunchElevated(string exePath, bool silent)
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        if (new System.Security.Principal.WindowsPrincipal(identity)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            return false;
        try
        {
            Process.Start(new ProcessStartInfo(exePath, silent ? "/uninstall-silent" : "/uninstall")
            {
                UseShellExecute = true,
                Verb = "runas"
            });
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // UAC was declined. Leave the installation untouched.
        }
        catch (Exception ex)
        {
            if (!silent) ShowNotice("Uninstall could not request administrator access.\n" + ex.Message);
        }
        return true;
    }

    private static void ShowNotice(string message)
    {
        string heading = message, detail = string.Empty;
        int lineBreak = message.IndexOf('\n');
        if (lineBreak >= 0) { heading = message[..lineBreak]; detail = message[(lineBreak + 1)..]; }
        var notice = new Controls.ConfirmDialog(heading, detail, "OK")
        { WindowStartupLocation = WindowStartupLocation.CenterScreen, ShowInTaskbar = true };
        notice.CancelButton.Visibility = Visibility.Collapsed;
        notice.ShowDialog();
    }

    private static void CreateShortcut(string shortcutPath, string targetPath)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null) return;
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.Save();
        }
        catch { }
    }

    /// <summary>The registered installed copy this process is running from, or null for a portable or
    /// development copy. The self-update hands only a registered copy to the installer, into the folder
    /// it is already installed in.</summary>
    internal static (bool Machine, string Directory)? InstalledCopy()
    {
        try
        {
            string exe = Path.GetFullPath(Environment.ProcessPath ?? "");
            foreach (var (root, machine) in new[] { (Registry.LocalMachine, true), (Registry.CurrentUser, false) })
            {
                using var key = root.OpenSubKey(RegistryKeyPath);
                if (key?.GetValue("InstallPath") is string registered
                    && string.Equals(Path.GetFullPath(registered), exe, StringComparison.OrdinalIgnoreCase))
                    return (machine, Path.GetDirectoryName(exe)!);
            }
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException
            or System.Security.SecurityException or NotSupportedException) { }
        return null;
    }

    private static string AppVersion() =>
        typeof(App).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

    private static int InstalledSizeKilobytes(string installDirectory)
    {
        long bytes = 0;
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };
        foreach (var file in new DirectoryInfo(installDirectory).EnumerateFiles("*", options))
            bytes += file.Length;
        return (int)Math.Min(int.MaxValue, (bytes + 1023) / 1024);
    }
}
