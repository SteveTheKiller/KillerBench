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
            if (!string.IsNullOrWhiteSpace(launcher) && File.Exists(launcher))
            {
                _ = int.TryParse(Environment.GetEnvironmentVariable("KILLERBENCH_LAUNCHER_PID"), out int launcherPid);
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
}
