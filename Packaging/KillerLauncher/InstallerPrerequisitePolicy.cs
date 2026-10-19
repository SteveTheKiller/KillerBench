using System.IO;

namespace KillerLauncher
{
    internal static class InstallerPrerequisitePolicy
    {
        internal const int MissingDesktopRuntimeExitCode = 10;

        internal static int? SilentInstallRejection(bool hasDesktopRuntime10) =>
            hasDesktopRuntime10 ? (int?)null : MissingDesktopRuntimeExitCode;

        /// <summary>
        /// True when the x64 .NET 10 Desktop Runtime that KillerBench needs is installed. On Arm64
        /// Windows the x64 runtime lives in dotnet\x64, and dotnet\shared holds the Arm64 runtime,
        /// which an x64 app cannot use.
        /// </summary>
        internal static bool HasDesktopRuntime10(string programFiles, bool arm64Host)
        {
            string dotnet = Path.Combine(programFiles, "dotnet");
            if (arm64Host) dotnet = Path.Combine(dotnet, "x64");
            string root = Path.Combine(dotnet, "shared", "Microsoft.WindowsDesktop.App");
            try { return Directory.Exists(root) && Directory.GetDirectories(root, "10.*").Length > 0; }
            catch { return false; }
        }
    }
}
