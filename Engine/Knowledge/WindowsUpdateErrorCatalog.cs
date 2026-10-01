namespace KillerBench.Engine.Knowledge;

public sealed record WindowsUpdateCodeReference(uint Code, string Symbol, string Meaning, string DiagnosticScope);

public static class WindowsUpdateErrorCatalog
{
    public const string SourceUrl = "https://learn.microsoft.com/en-us/windows/deployment/update/windows-update-error-reference";

    public static IReadOnlyList<WindowsUpdateCodeReference> All { get; } = Array.AsReadOnly<WindowsUpdateCodeReference>(
    [
        new WindowsUpdateCodeReference(0x00240003, "WU_S_UPDATE_ERROR", "The operation completed but one or more updates reported errors.", "Review the individual update outcomes and CBS evidence."),
        new WindowsUpdateCodeReference(0x00240005, "WU_S_REBOOT_REQUIRED", "A restart is required to complete the update.", "Review pending restart evidence before another repair."),
        new WindowsUpdateCodeReference(0x00240006, "WU_S_ALREADY_INSTALLED", "The update is already installed.", "Verify the exact installed revision if the caller expected a new change."),
        new WindowsUpdateCodeReference(0x00240007, "WU_S_ALREADY_UNINSTALLED", "The update is already absent.", "Verify the exact installed revision before retrying removal."),
        new WindowsUpdateCodeReference(0x80070020, "ERROR_SHARING_VIOLATION", "A required file was in use by another process.", "Review Installer activity and the exact locked file before retrying."),
        new WindowsUpdateCodeReference(0x80072EE2, "WININET_E_TIMEOUT", "The update source connection timed out.", "Review network, proxy and update-source reachability evidence."),
        new WindowsUpdateCodeReference(0x80240001, "WU_E_NO_SERVICE", "Windows Update Agent could not provide the requested service.", "Review Windows Update service availability and configuration."),
        new WindowsUpdateCodeReference(0x80240009, "WU_E_OPERATIONINPROGRESS", "Another conflicting update operation was in progress.", "Review Installer activity and pending restart evidence before another operation."),
        new WindowsUpdateCodeReference(0x80240016, "WU_E_INSTALL_NOT_ALLOWED", "Another installation or a required restart blocked installation.", "Review Installer activity and pending restart evidence."),
        new WindowsUpdateCodeReference(0x80240017, "WU_E_NOT_APPLICABLE", "The update is not applicable to this machine.", "Review edition, architecture, prerequisites and supersedence."),
        new WindowsUpdateCodeReference(0x80240018, "WU_E_NO_USERTOKEN", "A required user token was unavailable.", "Review execution identity and elevation."),
        new WindowsUpdateCodeReference(0x80240019, "WU_E_EXCLUSIVE_INSTALL_CONFLICT", "An exclusive update conflicted with another selected update.", "Plan the exclusive update by itself."),
        new WindowsUpdateCodeReference(0x8024001B, "WU_E_SELFUPDATE_IN_PROGRESS", "Windows Update Agent was updating itself.", "Wait for agent servicing to finish, then scan again."),
        new WindowsUpdateCodeReference(0x8024001D, "WU_E_INVALID_UPDATE", "The update contains invalid metadata.", "Review source metadata and the exact update identity."),
        new WindowsUpdateCodeReference(0x8024001E, "WU_E_SERVICE_STOP", "The service or system was shutting down.", "Review update service state and recent shutdown history."),
        new WindowsUpdateCodeReference(0x8024001F, "WU_E_NO_CONNECTION", "The network connection was unavailable.", "Run network, DNS and proxy diagnostics."),
        new WindowsUpdateCodeReference(0x80240020, "WU_E_NO_INTERACTIVE_USER", "No interactive user was signed in for the requested operation.", "Review the operation context and interaction requirements."),
        new WindowsUpdateCodeReference(0x80240021, "WU_E_TIME_OUT", "The Windows Update operation timed out.", "Review service, network and source response evidence before retrying."),
        new WindowsUpdateCodeReference(0x80240022, "WU_E_ALL_UPDATES_FAILED", "The operation failed for every selected update.", "Review individual outcomes and CBS evidence; this code has multiple causes."),
        new WindowsUpdateCodeReference(0x80240023, "WU_E_EULAS_DECLINED", "Required update license terms were not accepted.", "Review the exact update license requirement."),
        new WindowsUpdateCodeReference(0x80240025, "WU_E_USER_ACCESS_DISABLED", "Policy blocked user access to Windows Update.", "Review managed Windows Update policy ownership."),
        new WindowsUpdateCodeReference(0x8024002A, "WU_E_MISSING_HANDLER", "A required update handler is missing.", "Review servicing health and update metadata."),
        new WindowsUpdateCodeReference(0x8024002C, "WU_E_BIN_SOURCE_ABSENT", "A delta-compressed update source was unavailable.", "Review component source and servicing evidence."),
        new WindowsUpdateCodeReference(0x8024002D, "WU_E_SOURCE_ABSENT", "A full-file update source was unavailable.", "Review component source and servicing evidence."),
        new WindowsUpdateCodeReference(0x8024002E, "WU_E_WU_DISABLED", "Access to an unmanaged update server is not allowed.", "Review managed source and policy configuration."),
        new WindowsUpdateCodeReference(0x8024002F, "WU_E_CALL_CANCELLED_BY_POLICY", "Windows Update policy canceled the operation.", "Review DisableWindowsUpdateAccess and policy ownership."),
        new WindowsUpdateCodeReference(0x80240030, "WU_E_INVALID_PROXY_SERVER", "The configured proxy list is invalid.", "Run proxy diagnostics and review managed proxy ownership."),
        new WindowsUpdateCodeReference(0x80240034, "WU_E_DOWNLOAD_FAILED", "The update failed to download.", "Review BITS, Delivery Optimization, network, proxy and update-source evidence."),
        new WindowsUpdateCodeReference(0x8024401B, "WU_E_PT_HTTP_STATUS_PROXY_AUTH_REQ", "The update source required proxy authentication.", "Run WinHTTP and current-account proxy diagnostics."),
        new WindowsUpdateCodeReference(0x80244011, "WU_E_PT_SUS_SERVER_NOT_SET", "Managed update policy did not provide the expected WSUS server.", "Review managed Windows Update source configuration and policy ownership."),
        new WindowsUpdateCodeReference(0x80244017, "WU_E_PT_HTTP_STATUS_DENIED", "The update source required authentication.", "Review execution context, proxy authentication and update-source access."),
        new WindowsUpdateCodeReference(0x80244018, "WU_E_PT_HTTP_STATUS_FORBIDDEN", "The update source refused the request.", "Review execution context, managed policy and update-source access."),
        new WindowsUpdateCodeReference(0x8024401C, "WU_E_PT_HTTP_STATUS_REQUEST_TIMEOUT", "The update source timed out waiting for the request.", "Review network, proxy and update-source response evidence."),
        new WindowsUpdateCodeReference(0x80244021, "WU_E_PT_HTTP_STATUS_BAD_GATEWAY", "A gateway or proxy received an invalid upstream response.", "Review network, proxy and update-source reachability evidence."),
        new WindowsUpdateCodeReference(0x80244022, "WU_E_PT_HTTP_STATUS_SERVICE_UNAVAILABLE", "The configured update source reported service unavailable.", "Review source reachability and retry after the source recovers."),
        new WindowsUpdateCodeReference(0x80244023, "WU_E_PT_HTTP_STATUS_GATEWAY_TIMEOUT", "A gateway timed out waiting for the update source.", "Review network, proxy and update-source response evidence."),
        new WindowsUpdateCodeReference(0x8024402C, "WU_E_PT_WINHTTP_NAME_NOT_RESOLVED", "The proxy or update-source name could not be resolved.", "Review DNS, network and proxy configuration."),
        new WindowsUpdateCodeReference(0x80246002, "WU_E_DM_INCORRECTFILEHASH", "The downloaded file digest did not match the expected digest.", "Review transfer evidence, source metadata and component servicing health before retrying."),
        new WindowsUpdateCodeReference(0x80246005, "WU_E_DM_NONETWORK", "The download manager could not use a network connection.", "Review BITS, Delivery Optimization, network and proxy evidence."),
        new WindowsUpdateCodeReference(0x80246008, "WU_E_DM_FAILTOCONNECTTOBITS", "The download manager could not connect to BITS.", "Review BITS service and transfer state."),
        new WindowsUpdateCodeReference(0x80246009, "WU_E_DM_BITSTRANSFERERROR", "BITS reported an unspecified transfer error.", "Review exact BITS transfer, network and proxy evidence."),
        new WindowsUpdateCodeReference(0x80241001, "WU_E_MSI_WRONG_VERSION", "Windows Installer was too old for part of the update search.", "Review Windows Installer availability and the operating-system servicing level."),
        new WindowsUpdateCodeReference(0x80241002, "WU_E_MSI_NOT_CONFIGURED", "Windows Installer was not configured for part of the update search.", "Review Windows Installer service and policy evidence."),
        new WindowsUpdateCodeReference(0x80241003, "WU_E_MSP_DISABLED", "Policy disabled Windows Installer patching.", "Review managed Windows Installer policy ownership."),
        new WindowsUpdateCodeReference(0x80241004, "WU_E_MSI_WRONG_APP_CONTEXT", "An application update could not be applied in its current per-user context.", "Review the exact product registration and installation context."),
        new WindowsUpdateCodeReference(0x80241FFF, "WU_E_MSP_UNEXPECTED", "Windows Installer failed during part of the update search.", "Review Installer activity and exact product evidence."),
        new WindowsUpdateCodeReference(0x80248000, "WU_E_DS_SHUTDOWN", "The Windows Update Agent data store operation stopped because the Agent was shutting down.", "Review Agent service state and recent update history."),
        new WindowsUpdateCodeReference(0x80248001, "WU_E_DS_INUSE", "The Windows Update Agent data store was in use.", "Review current update and Installer activity before retrying."),
        new WindowsUpdateCodeReference(0x80248002, "WU_E_DS_INVALID", "The current and expected Windows Update data store states did not match.", "Review update service registration and history evidence."),
        new WindowsUpdateCodeReference(0x80248003, "WU_E_DS_TABLEMISSING", "A required Windows Update data store table was missing.", "Review update service registration and history evidence; do not reset the store from this code alone."),
        new WindowsUpdateCodeReference(0x80248004, "WU_E_DS_TABLEINCORRECT", "A Windows Update data store table had unexpected columns.", "Review update service registration and history evidence; do not reset the store from this code alone."),
        new WindowsUpdateCodeReference(0x80248005, "WU_E_DS_INVALIDTABLENAME", "A requested table was not present in the Windows Update data store.", "Review update service registration and history evidence."),
        new WindowsUpdateCodeReference(0x80248006, "WU_E_DS_BADVERSION", "The Windows Update data store version did not match the expected version.", "Review Agent servicing and update history evidence."),
        new WindowsUpdateCodeReference(0x80248007, "WU_E_DS_NODATA", "Requested information was not present in the Windows Update data store.", "Review exact update identity and history evidence."),
        new WindowsUpdateCodeReference(0x80248008, "WU_E_DS_MISSINGDATA", "Required information was missing from the Windows Update data store.", "Review update service registration and history evidence."),
        new WindowsUpdateCodeReference(0x80248009, "WU_E_DS_MISSINGREF", "The Windows Update data store contained a missing reference.", "Review exact update metadata and history evidence."),
        new WindowsUpdateCodeReference(0x8024800A, "WU_E_DS_UNKNOWNHANDLER", "The data store referenced an unrecognized update handler.", "Review Agent servicing, component servicing and exact update metadata."),
        new WindowsUpdateCodeReference(0x8024800B, "WU_E_DS_CANTDELETE", "An update remained referenced by one or more services and could not be deleted from the data store.", "Review registered update services and exact update identity."),
        new WindowsUpdateCodeReference(0x8024800C, "WU_E_DS_LOCKTIMEOUTEXPIRED", "The Windows Update data store could not be locked within the allotted time.", "Review current update and Installer activity before retrying."),
        new WindowsUpdateCodeReference(0x8024800D, "WU_E_DS_NOCATEGORIES", "An update category had no parent and was not a top-level category.", "Review update source metadata and service registration."),
        new WindowsUpdateCodeReference(0x8024800E, "WU_E_DS_ROWEXISTS", "The Windows Update data store already contained the requested primary key.", "Review duplicate update metadata and service registration."),
        new WindowsUpdateCodeReference(0x8024800F, "WU_E_DS_STOREFILELOCKED", "Another process locked the Windows Update data store file.", "Review current update and Installer activity before retrying."),
        new WindowsUpdateCodeReference(0x80248010, "WU_E_DS_CANNOTREGISTER", "The Windows Update data store could not register with COM in the current process.", "Review execution context and Agent service evidence."),
        new WindowsUpdateCodeReference(0x80248011, "WU_E_DS_UNABLETOSTART", "Windows Update Agent could not create a data store object in another process.", "Review Agent service availability and execution context."),
        new WindowsUpdateCodeReference(0x80248013, "WU_E_DS_DUPLICATEUPDATEID", "The server supplied one update with two different revision identifiers.", "Review update source metadata and the exact update revisions."),
        new WindowsUpdateCodeReference(0x80248014, "WU_E_DS_UNKNOWNSERVICE", "A required service was not registered in the Windows Update data store.", "Review configured update service registrations."),
        new WindowsUpdateCodeReference(0x80248015, "WU_E_DS_SERVICEEXPIRED", "An update service registration had expired.", "Review configured update service registrations and source policy."),
        new WindowsUpdateCodeReference(0x80248016, "WU_E_DS_DECLINENOTALLOWED", "A mandatory or deadline-controlled update could not be hidden.", "Review deployment policy and the exact update assignment."),
        new WindowsUpdateCodeReference(0x80248017, "WU_E_DS_TABLESESSIONMISMATCH", "A data store table was not associated with the current session.", "Review current Agent activity and retry only after the active operation completes."),
        new WindowsUpdateCodeReference(0x80248018, "WU_E_DS_SESSIONLOCKMISMATCH", "A data store lock was not associated with the current session.", "Review current Agent activity and retry only after the active operation completes."),
        new WindowsUpdateCodeReference(0x80248019, "WU_E_DS_NEEDWINDOWSSERVICE", "The built-in Windows Update service could not be removed or unregistered.", "Review update service registrations without removing the built-in service."),
        new WindowsUpdateCodeReference(0x8024801A, "WU_E_DS_INVALIDOPERATION", "The requested Windows Update data store operation was not allowed.", "Review operation context, service registration and policy evidence."),
        new WindowsUpdateCodeReference(0x8024801B, "WU_E_DS_SCHEMAMISMATCH", "The Windows Update data store schema did not match a backup document.", "Review Agent version and store history; preserve the existing store."),
        new WindowsUpdateCodeReference(0x8024801C, "WU_E_DS_RESETREQUIRED", "The Windows Update data store required a new session.", "Release the current Agent session and retry without deleting the data store."),
        new WindowsUpdateCodeReference(0x8024801D, "WU_E_DS_IMPERSONATED", "A Windows Update data store operation used an impersonated identity.", "Review execution identity and operation context."),
        new WindowsUpdateCodeReference(0x80248FFF, "WU_E_DS_UNEXPECTED", "The Windows Update data store reported an otherwise unclassified error.", "Review update service registration and history evidence before selecting a repair."),
        new WindowsUpdateCodeReference(0x8024D009, "WU_E_SETUP_SKIP_UPDATE", "Windows Update Agent self-update was skipped by source metadata.", "Review managed update source policy and Agent service availability."),
        new WindowsUpdateCodeReference(0x80070422, "ERROR_SERVICE_DISABLED", "A required service is disabled.", "Review Windows Update service startup policy before recovery."),
        new WindowsUpdateCodeReference(0x80070005, "E_ACCESSDENIED", "The operation was denied access.", "Review elevation, policy and target permissions."),
        new WindowsUpdateCodeReference(0x800F081F, "CBS_E_SOURCE_MISSING", "Required component source files were not found.", "Review CBS evidence and the approved component repair source."),
        new WindowsUpdateCodeReference(0x800F0831, "CBS_E_STORE_CORRUPTION", "A required package manifest is missing.", "Review CBS evidence and component-store health.")
    ]);

    public static WindowsUpdateCodeReference? Find(int hresult)
    {
        uint code = unchecked((uint)hresult);
        return All.FirstOrDefault(entry => entry.Code == code);
    }

    public static WindowsUpdateCodeReference? Find(uint code) => All.FirstOrDefault(entry => entry.Code == code);
}
