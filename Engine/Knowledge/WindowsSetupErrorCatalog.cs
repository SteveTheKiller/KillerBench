namespace KillerBench.Engine.Knowledge;

public sealed record WindowsSetupCodeReference(uint Code, string Symbol, string Meaning, string DiagnosticScope, bool RequiresReview = true);

public static class WindowsSetupErrorCatalog
{
    public const string SourceUrl = "https://learn.microsoft.com/en-us/troubleshoot/windows-client/setup-upgrade-and-drivers/windows-10-upgrade-resolution-procedures";

    public static IReadOnlyList<WindowsSetupCodeReference> All { get; } = Array.AsReadOnly<WindowsSetupCodeReference>(
    [
        new WindowsSetupCodeReference(0xC1900100, "MOSETUP_E_VERSION_MISMATCH", "Setup Platform binaries have an unexpected version.", "Review installation media identity and Setup Platform package contents."),
        new WindowsSetupCodeReference(0xC1900101, "MOSETUP_E_SETUP_PLATFORM", "Setup Platform reported an unspecified error. This result is also used for generic rollback failures.", "Review the extend code, rollback logs, setup memory dump and exact driver evidence."),
        new WindowsSetupCodeReference(0xC1900102, "MOSETUP_E_SHUTDOWN_BLOCK", "Setup could not create or destroy its shutdown block message.", "Review Setup phase logs and shutdown history."),
        new WindowsSetupCodeReference(0xC1900103, "MOSETUP_E_COMPAT_TIMEOUT", "Compatibility issues were not resolved within the required time.", "Review current CompatData blocks and Setup activity."),
        new WindowsSetupCodeReference(0xC1900104, "MOSETUP_E_PROCESS_TIMEOUT", "The installation process did not finish within the required time.", "Review the extend code, Setup phase and active servicing processes."),
        new WindowsSetupCodeReference(0xC1900105, "MOSETUP_E_TEST_MODE", "The installation process is running in a test environment.", "Review the Setup invocation and deployment context."),
        new WindowsSetupCodeReference(0xC1900106, "MOSETUP_E_TERMINATE_PROCESS", "The installation process was terminated.", "Review Setup logs, shutdown history and external process termination evidence."),
        new WindowsSetupCodeReference(0xC1900107, "MOSETUP_E_CLEANUP_PENDING", "Cleanup from a previous installation attempt is still pending.", "Review restart evidence and retained Setup files before another attempt."),
        new WindowsSetupCodeReference(0xC1900128, "MOSETUP_E_MISSING_TASK", "A required Setup task is missing.", "Review exact scheduled-task definitions and Setup Platform integrity."),
        new WindowsSetupCodeReference(0xC1900129, "MOSETUP_E_UPDATEMEDIA_REQUESTED", "Setup requested newer installation media to continue.", "Review installation media identity and Dynamic Update evidence."),
        new WindowsSetupCodeReference(0xC190012F, "MOSETUP_E_FINALIZE_ALREADY_REQUESTED", "A finalize operation was already requested.", "Review current Setup activity and pending restart evidence before another request."),
        new WindowsSetupCodeReference(0xC1900130, "MOSETUP_E_INSTALL_HASH_MISSING", "The installation instance hash is missing.", "Review Setup Platform state and the exact installation instance evidence."),
        new WindowsSetupCodeReference(0xC1900131, "MOSETUP_E_INSTALL_HASH_MISMATCH", "The installation instance hash does not match.", "Review Setup Platform state and avoid combining evidence from different attempts."),
        new WindowsSetupCodeReference(0xC19001DF, "MOSETUP_E_DISK_FULL", "Setup cannot continue because the system is out of disk space.", "Review exact system-volume capacity and Setup storage requirements."),
        new WindowsSetupCodeReference(0xC19001E0, "MOSETUP_E_GATHER_OS_STATE_FAILED", "GatherOsState failed to run.", "Review Setup phase logs, executable presence and application-control evidence."),
        new WindowsSetupCodeReference(0xC19001E1, "MOSETUP_E_PROCESS_SUSPENDED", "The installation process was suspended.", "Review current Setup process activity and the deployment controller."),
        new WindowsSetupCodeReference(0xC19001E2, "MOSETUP_E_PREINSTALL_SCRIPT_FAILED", "A preinstall script failed or returned an error.", "Review the exact deployment script result and Setup phase logs."),
        new WindowsSetupCodeReference(0xC19001E3, "MOSETUP_E_PRECOMMIT_SCRIPT_FAILED", "A precommit script failed or returned an error.", "Review the exact deployment script result and Setup phase logs."),
        new WindowsSetupCodeReference(0xC19001E4, "MOSETUP_E_FAILURE_SCRIPT_FAILED", "A failure script failed or returned an error.", "Review the exact deployment script result and the earlier Setup failure."),
        new WindowsSetupCodeReference(0xC19001E5, "MOSETUP_E_SCRIPT_TIMEOUT", "A Setup script exceeded its time limit.", "Review the exact deployment script, its dependencies and Setup phase logs."),
        new WindowsSetupCodeReference(0xC1900200, "MOSETUP_E_COMPAT_SYSREQ_BLOCK", "The system does not meet the minimum requirements to install the update.", "Review hardware readiness, edition, architecture and current compatibility evidence."),
        new WindowsSetupCodeReference(0xC1900201, "MOSETUP_E_COMPAT_SYSREQ_CANCEL", "Setup was canceled after the system requirements check failed.", "Review hardware readiness and current compatibility evidence."),
        new WindowsSetupCodeReference(0xC1900202, "MOSETUP_E_COMPAT_DOWNLOADREQ_BLOCK", "The system does not meet the minimum requirements to download the update.", "Review download prerequisites, architecture and current compatibility evidence."),
        new WindowsSetupCodeReference(0xC1900203, "MOSETUP_E_COMPAT_DOWNLOADREQ_CANCEL", "Setup download was canceled after its requirements check failed.", "Review download prerequisites and current compatibility evidence."),
        new WindowsSetupCodeReference(0xC1900204, "MOSETUP_E_COMPAT_MIGCHOICE_BLOCK", "The requested migration choice is not available.", "Review source and target edition, language, architecture and retention choices."),
        new WindowsSetupCodeReference(0xC1900205, "MOSETUP_E_COMPAT_MIGCHOICE_CANCEL", "Setup was canceled because the requested migration choice is unavailable.", "Review source and target edition, language, architecture and retention choices."),
        new WindowsSetupCodeReference(0xC1900206, "MOSETUP_E_COMPAT_DEVICEREQ_BLOCK", "The system did not pass the device requirements scan.", "Review exact Device Manager, driver and compatibility-report evidence."),
        new WindowsSetupCodeReference(0xC1900207, "MOSETUP_E_COMPAT_DEVICEREQ_CANCEL", "Setup was canceled after the device requirements scan failed.", "Review exact Device Manager, driver and compatibility-report evidence."),
        new WindowsSetupCodeReference(0xC1900208, "MOSETUP_E_COMPAT_INSTALLREQ_BLOCK", "An actionable compatibility issue blocked installation.", "Review bounded CompatData blocks and current application and driver evidence."),
        new WindowsSetupCodeReference(0xC1900209, "MOSETUP_E_COMPAT_INSTALLREQ_CANCEL", "Setup was canceled because installed applications cannot be migrated.", "Review bounded CompatData blocks and exact installed-application evidence."),
        new WindowsSetupCodeReference(0xC190020A, "MOSETUP_E_COMPAT_RECOVERYREQ_BLOCK", "The system does not meet the minimum requirements to recover Windows.", "Review Windows Recovery Environment and recovery-partition evidence."),
        new WindowsSetupCodeReference(0xC190020B, "MOSETUP_E_COMPAT_RECOVERYREQ_CANCEL", "Recovery was canceled after its requirements check failed.", "Review Windows Recovery Environment and recovery-partition evidence."),
        new WindowsSetupCodeReference(0xC190020C, "MOSETUP_E_DOWNLOADDISKSPACE_BLOCK", "There is not enough disk space to download the payload.", "Review exact system-volume capacity and download-cache evidence."),
        new WindowsSetupCodeReference(0xC190020D, "MOSETUP_E_DOWNLOADDISKSPACE_CANCEL", "Payload download was canceled because disk space is insufficient.", "Review exact system-volume capacity and download-cache evidence."),
        new WindowsSetupCodeReference(0xC190020E, "MOSETUP_E_INSTALLDISKSPACE_BLOCK", "There is not enough disk space to install the payload.", "Review exact system-volume capacity, reserved storage and Setup storage evidence."),
        new WindowsSetupCodeReference(0xC190020F, "MOSETUP_E_INSTALLDISKSPACE_CANCEL", "Installation was canceled because disk space is insufficient.", "Review exact system-volume capacity, reserved storage and Setup storage evidence."),
        new WindowsSetupCodeReference(0xC1900210, "MOSETUP_E_COMPAT_SCANONLY", "Setup completed a compatibility-only scan instead of installing Windows.", "Review the generated compatibility report before planning an upgrade.", false),
        new WindowsSetupCodeReference(0xC1900211, "MOSETUP_E_DOWNLOAD_UNPACK_DISKSPACE_BLOCK", "There is not enough disk space to download and unpack the media.", "Review exact system-volume capacity and download-cache evidence."),
        new WindowsSetupCodeReference(0xC1900212, "MOSETUP_E_DOWNLOAD_UNPACK_DISKSPACE_MULTIARCH_BLOCK", "There is not enough disk space to download and unpack multi-architecture media.", "Review exact system-volume capacity, media architecture and download-cache evidence."),
        new WindowsSetupCodeReference(0xC1900213, "MOSETUP_E_NO_OFFER_FOUND", "No upgrade offer matched the required criteria.", "Review current edition, release targeting, safeguard and update-source evidence."),
        new WindowsSetupCodeReference(0xC1900214, "MOSETUP_E_UNSUPPORTED_VERSION", "This version of the upgrade tool is not supported.", "Review tool version, target release and installation media identity."),
        new WindowsSetupCodeReference(0xC1900215, "MOSETUP_E_NO_MATCHING_INSTALL_IMAGE", "Setup could not find an install image for this system.", "Review media edition, language, architecture and image inventory."),
        new WindowsSetupCodeReference(0xC1900216, "MOSETUP_E_ROLLBACK_PENDING", "An operating-system rollback is pending.", "Review Setup rollback activity and restart evidence before another attempt."),
        new WindowsSetupCodeReference(0xC1900220, "MOSETUP_E_COMPAT_REPORT_NOT_DISPLAYED", "A missing system component prevented the compatibility report from being displayed.", "Review component servicing health and the retained compatibility report."),
        new WindowsSetupCodeReference(0xC1900400, "MOSETUP_E_UA_VERSION_MISMATCH", "The Update Agent client version is unexpected.", "Review Update Agent servicing and installation-media identity."),
        new WindowsSetupCodeReference(0xC1900401, "MOSETUP_E_UA_NO_PACKAGES_TO_DOWNLOAD", "Update Agent found no packages to download.", "Review the exact offer, source, targeting and applicability evidence."),
        new WindowsSetupCodeReference(0xC1900402, "MOSETUP_E_UA_UPDATE_CANNOT_BE_MERGED", "Update Agent could not merge the requested update.", "Review exact package identity, revision and update-source metadata."),
        new WindowsSetupCodeReference(0xC1900403, "MOSETUP_E_UA_CORRUPT_PAYLOAD_FILES", "Update Agent found corrupt payload files.", "Review transfer integrity, download cache and source metadata evidence."),
        new WindowsSetupCodeReference(0xC1900404, "MOSETUP_E_UA_BOX_NOT_FOUND", "The installation executable was not found.", "Review installation-media contents and source identity."),
        new WindowsSetupCodeReference(0xC1900405, "MOSETUP_E_UA_BOX_CRASHED", "The installation process terminated unexpectedly.", "Review Setup crash, dump, application and rollback evidence.")
    ]);

    public static WindowsSetupCodeReference? Find(uint code) => All.FirstOrDefault(entry => entry.Code == code);
}
