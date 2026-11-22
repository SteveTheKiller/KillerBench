namespace KillerBench.Features.About
{
    /// <summary>A host that can hold an update back while a repair is running. Closing the app
    /// would interrupt the repair, and a repair must finish its current command.</summary>
    internal interface IAboutRepairHold
    {
        /// <summary>True while a repair is running, after reporting why the update has to wait.</summary>
        bool HoldUpdateForRepair();

        /// <summary>True from the moment the update commits to closing the app, so no new run starts;
        /// false again if the handoff fails or is canceled.</summary>
        void HoldRunsForUpdate(bool hold);
    }

    // KillerBench's side of the shared About controller (AboutController.cs is the KillerUI copy).
    internal sealed partial class AboutController
    {
        // Checked before the download and again once it is verified; a held download is discarded.
        partial void HoldUpdate(ref bool hold)
        {
            if (_host is IAboutRepairHold repair && repair.HoldUpdateForRepair()) hold = true;
        }

        // HoldUpdate ran on this same UI turn, so no repair can slip in between it and this block.
        partial void BeforeUpdateHandoff(ref bool proceed)
        {
            if (_host is IAboutRepairHold repair) repair.HoldRunsForUpdate(true);
        }

        partial void AfterUpdateHandoff(bool started)
        {
            if (!started && _host is IAboutRepairHold repair) repair.HoldRunsForUpdate(false);
        }

        partial void UpdateShutdownCanceled()
        {
            if (_host is IAboutRepairHold repair) repair.HoldRunsForUpdate(false);
        }

        partial void UpdateNotVerified(System.Exception failure)
        {
            string message = _host.Loc("Str_About_UpdateRejected");
            _host.UpdateText = message;
            _host.SetStatus(message);
        }

        partial void UpdateHandoffFailed(System.Exception failure)
        {
            string message = _host.Loc(failure switch
            {
                System.ComponentModel.Win32Exception { NativeErrorCode: 1223 } => "Str_About_UpdateElevationCanceled",
                KillerBench.Services.UpdateHelperException { ExitCode: KillerFamily.Deployment.InstallerExitCodes.Busy } => "Str_About_UpdateBusy",
                KillerBench.Services.UpdateHelperException { ExitCode: KillerFamily.Deployment.InstallerExitCodes.DiskFull } => "Str_About_UpdateDiskFull",
                KillerBench.Services.UpdateHelperException { ExitCode: KillerFamily.Deployment.InstallerExitCodes.SecurityRejected } => "Str_About_UpdateSecurityRejected",
                _ => "Str_About_UpdateHandoffFailed"
            });
            _host.UpdateText = message;
            _host.SetStatus(message);
        }

    }
}
