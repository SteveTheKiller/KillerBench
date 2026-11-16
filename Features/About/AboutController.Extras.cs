namespace KillerBench.Features.About
{
    /// <summary>A host that can hold an update back while a repair is running. Closing the app
    /// would interrupt the repair, and a repair must finish its current command.</summary>
    internal interface IAboutRepairHold
    {
        /// <summary>True while a repair is running, after reporting why the update has to wait.</summary>
        bool HoldUpdateForRepair();
    }

    // KillerBench's side of the shared About controller (AboutController.cs is the KillerUI copy).
    internal sealed partial class AboutController
    {
        // Checked before the download and again once it is verified; a held download is discarded.
        partial void HoldUpdate(ref bool hold)
        {
            if (_host is IAboutRepairHold repair && repair.HoldUpdateForRepair()) hold = true;
        }

        partial void UpdateNotVerified(System.Exception failure)
        {
            string message = _host.Loc("Str_About_UpdateRejected");
            _host.UpdateText = message;
            _host.SetStatus(message);
        }

        partial void UpdateHandoffFailed(System.Exception failure)
        {
            string message = _host.Loc(failure is System.ComponentModel.Win32Exception { NativeErrorCode: 1223 }
                ? "Str_About_UpdateElevationCanceled" : "Str_About_UpdateHandoffFailed");
            _host.UpdateText = message;
            _host.SetStatus(message);
        }

    }
}
