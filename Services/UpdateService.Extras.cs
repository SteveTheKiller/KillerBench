using System;
using System.IO;
using System.Text;

namespace KillerBench.Services
{
    /// <summary>
    /// APP-OWNED: how KillerBench updates on the shared update stack (Services/UpdateService.cs).
    /// KillerBench installs the way KillerPDF does rather than by copying an exe over itself. An
    /// installed copy hands the verified installer the same /install-user or /silent command a manual
    /// upgrade uses, into the folder it is already installed in. A portable copy replaces its launcher
    /// once the launcher and this process have exited. A copy that is neither, such as a development
    /// build, is not updated; the download step fails and the release page opens instead.
    /// </summary>
    internal static partial class UpdateService
    {
        static partial void ChooseUpdateTarget(UpdateTarget target)
        {
            string? launcher = Environment.GetEnvironmentVariable("KILLERBENCH_LAUNCHER_PATH");
            string? launcherPidText = Environment.GetEnvironmentVariable("KILLERBENCH_LAUNCHER_PID");
            if (PortableUpdateTarget.IsTrustedLauncher(launcher, launcherPidText, AppContext.BaseDirectory, Environment.ProcessPath))
            {
                _ = int.TryParse(launcherPidText, out int launcherPid);
                target.Asset = SelfUpdate.PortableAsset;
                target.Replace = launcher;
                target.WaitForProcess = launcherPid;
                return;
            }
            var installed = App.InstalledCopy();
            if (installed is null)
            {
                target.Unsupported = true;
                return;
            }
            target.Asset = SelfUpdate.InstallerAsset;
            target.Machine = installed.Value.Machine;
            target.InstallerArguments = (installed.Value.Machine ? "/silent" : "/install-user")
                + " /install-dir64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(installed.Value.Directory));
        }
    }

    /// <summary>
    /// APP-OWNED: decides whether the launcher named by KILLERBENCH_LAUNCHER_PATH is the one to replace. The
    /// variable is inherited, and the replacement can run elevated, so the path alone is not trusted. It must be a
    /// KillerBench executable, the portable marker beside this copy must name the launcher process that started it,
    /// and the launcher must be one this copy may replace under its signature.
    /// </summary>
    public static class PortableUpdateTarget
    {
        private const string MarkerName = ".killerbench-portable";

        public static bool IsTrustedLauncher(string? launcher, string? launcherPid, string appDirectory, string? currentExecutable)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(launcher) || string.IsNullOrWhiteSpace(launcherPid) ||
                    string.IsNullOrWhiteSpace(currentExecutable)) return false;
                string path = Path.GetFullPath(launcher);
                string name = Path.GetFileName(path);
                if (!name.StartsWith("KillerBench", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(Path.GetExtension(name), ".exe", StringComparison.OrdinalIgnoreCase) ||
                    !File.Exists(path)) return false;
                string marker = Path.Combine(appDirectory, MarkerName);
                if (!File.Exists(marker)) return false;
                string[] lines = File.ReadAllLines(marker);
                if (lines.Length < 3 || !string.Equals(lines[0], "KillerBench", StringComparison.Ordinal) ||
                    !string.Equals(lines[2], launcherPid.Trim(), StringComparison.Ordinal)) return false;
                return CodeSignature.MayReplace(currentExecutable, path);
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException ||
                error is ArgumentException || error is NotSupportedException)
            {
                return false;
            }
        }
    }
}
