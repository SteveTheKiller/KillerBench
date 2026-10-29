using System.IO;
using Path = System.IO.Path;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;

namespace KillerBench;

// The installed half of the family setup. The verified launcher (Packaging\KillerLauncher) stages
// the payload and then hands off here: /register-user and /register-machine write shortcuts and the
// Installed Apps entry from the final installed path, /remove-user-install clears a per-user registration
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
            Shutdown(RemoveInstalledCopy(machine: false, UserInstallDir)
                ? KillerFamily.Deployment.InstallerExitCodes.Success : KillerFamily.Deployment.InstallerExitCodes.Failure);
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
        Shutdown(Uninstall(silent));
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
        WriteRegistrationShortcuts(Path.Combine(StartMenuDir(machine), AppName + ".lnk"),
            desktop ? DesktopShortcut(machine) : null, exePath, CreateShortcut);

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

    private static int Uninstall(bool silent)
    {
        string exePath = Path.GetFullPath(Environment.ProcessPath ?? string.Empty);
        string installDirectory = Path.GetDirectoryName(exePath) ?? string.Empty;
        Console.WriteLine("KillerBench application files will be kept. Location: " + installDirectory);
        bool machine = InstalledCopy()?.Machine ?? false;
        int? elevationResult = machine ? RelaunchElevated(exePath, silent) : null;
        if (elevationResult.HasValue) return elevationResult.Value;

        if (!silent)
        {
            var confirm = new Controls.ConfirmDialog(
                "Uninstall KillerBench?",
                "Registration and shortcuts will be removed. Application files, reports, and settings will be kept.",
                "Uninstall",
                "Cancel")
            { WindowStartupLocation = WindowStartupLocation.CenterScreen, ShowInTaskbar = true };
            confirm.ShowDialog();
            if (!confirm.Confirmed) return KillerFamily.Deployment.InstallerExitCodes.Cancelled;
        }

        if (!CloseOtherCopies(installDirectory))
        {
            Console.Error.WriteLine("KillerBench is still running. Registration and shortcut removal did not proceed.");
            if (!silent) ShowNotice("KillerBench is still running.\nClose it, then try uninstalling again.");
            return KillerFamily.Deployment.InstallerExitCodes.Busy;
        }

        bool removed = RemoveInstalledCopy(machine, installDirectory);
        if (!silent) ShowNotice((removed ? "KillerBench registration and shortcuts were removed." : "KillerBench registration and shortcuts were not fully removed.") +
            "\nApplication files were not removed. Location: " + installDirectory);
        return removed ? KillerFamily.Deployment.InstallerExitCodes.Success : KillerFamily.Deployment.InstallerExitCodes.Failure;
    }

    // Only the matching copy's registration and shortcuts are removed. Application files are retained.
    private static bool RemoveInstalledCopy(bool machine, string installDirectory)
    {
        RegistryKey root = machine ? Registry.LocalMachine : Registry.CurrentUser;
        string? registered = null;
        try { using var key = root.OpenSubKey(RegistryKeyPath); registered = key?.GetValue("InstallPath") as string; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { Console.Error.WriteLine("KillerBench registration could not be read: " + error.Message); }
        bool complete = true;
        bool removed = Services.InstallationOwnership.UnregisterPreservingFiles(installDirectory, registered, () =>
        {
            try { Services.CommandLinePath.UnregisterRequired(installDirectory, machine); } catch { complete = false; }
            try { File.Delete(Path.Combine(StartMenuDir(machine), AppName + ".lnk")); } catch { complete = false; }
            try { Directory.Delete(StartMenuDir(machine), recursive: false); } catch { }
            try { File.Delete(DesktopShortcut(machine)); } catch { complete = false; }
            try { root.DeleteSubKeyTree(UninstallKeyPath, throwOnMissingSubKey: false); } catch { complete = false; }
            try
            {
                using var key = root.OpenSubKey(RegistryKeyPath, writable: true);
                key?.DeleteValue("Installed", throwOnMissingValue: false);
                key?.DeleteValue("InstallPath", throwOnMissingValue: false);
                key?.DeleteValue("Version", throwOnMissingValue: false);
            }
            catch { complete = false; }
        });
        Console.WriteLine(removed && complete ? "KillerBench registration and shortcuts were removed." : "KillerBench registration and shortcuts were not fully removed.");
        Console.WriteLine("KillerBench application files were not removed. Location: " + installDirectory);
        return removed && complete;
    }

    // Asks every other KillerBench running from this installation to close normally, so a
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
            catch
            {
                // An elevated copy seen from an unelevated uninstaller hides its module path. A process
                // with this app's name is treated as running rather than skipped, so it is never removed under.
                if (process.Id != self && string.Equals(process.ProcessName + ".exe", Services.AppInfo.ExeName, StringComparison.OrdinalIgnoreCase))
                {
                    running.Add(process);
                    continue;
                }
            }
            process.Dispose();
        }
        try
        {
            foreach (var process in running)
                try { if (!process.HasExited) process.CloseMainWindow(); }
                catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception) { }
            return running.All(process =>
            {
                try { return process.HasExited || process.WaitForExit(30000); }
                catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception) { return false; }
            });
        }
        finally
        {
            foreach (var process in running) process.Dispose();
        }
    }

    private static int? RelaunchElevated(string exePath, bool silent)
    {
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        if (new System.Security.Principal.WindowsPrincipal(identity)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            return null;
        try
        {
            using var child = Process.Start(new ProcessStartInfo(exePath, silent ? "/uninstall-silent" : "/uninstall")
            {
                UseShellExecute = true,
                Verb = "runas"
            });
            if (child is null) return KillerFamily.Deployment.InstallerExitCodes.Failure;
            child.WaitForExit();
            return child.ExitCode;
        }
        catch (System.ComponentModel.Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // UAC was declined. Leave the installation untouched.
            return KillerFamily.Deployment.InstallerExitCodes.Cancelled;
        }
        catch (Exception ex)
        {
            if (!silent) ShowNotice("Uninstall could not request administrator access.\n" + ex.Message);
            Console.Error.WriteLine("Uninstall could not request administrator access: " + ex.Message);
            return KillerFamily.Deployment.InstallerExitCodes.FromException(ex);
        }
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

    /// <summary>Writes the Start Menu shortcut and, when requested, the desktop one. A shortcut that cannot be
    /// written fails registration, so setup reports a nonzero result instead of success without shortcuts.</summary>
    public static void WriteRegistrationShortcuts(string startMenuShortcut, string? desktopShortcut, string targetPath,
        Func<string, string, bool> writeShortcut)
    {
        foreach (string? shortcut in new[] { startMenuShortcut, desktopShortcut })
            if (shortcut is not null && !writeShortcut(shortcut, targetPath))
                throw new IOException("The shortcut could not be created: " + shortcut);
    }

    private static bool CreateShortcut(string shortcutPath, string targetPath)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null) return false;
            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = targetPath;
            shortcut.WorkingDirectory = Path.GetDirectoryName(targetPath);
            shortcut.Save();
            return true;
        }
        catch { return false; }
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
