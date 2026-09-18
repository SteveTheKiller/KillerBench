namespace KillerBench.Engine;

public sealed record WindowsInstallerCodeReference(int Code, string Symbol, string Summary,
    string DiagnosticScope, bool Success, bool RestartRequired = false);

public static class WindowsInstallerErrorCatalog
{
    private const string Source = "https://learn.microsoft.com/en-us/windows/win32/msi/error-codes";

    public static string SourceUrl => Source;

    public static IReadOnlyList<WindowsInstallerCodeReference> All { get; } = new[]
    {
        Entry(0, "ERROR_SUCCESS", "The action completed successfully.", "Review the requested product outcome only if application validation is still required.", true),
        Entry(13, "ERROR_INVALID_DATA", "The data is invalid.", "Review the package, command input and detailed Installer log."),
        Entry(87, "ERROR_INVALID_PARAMETER", "A parameter was invalid.", "Review the exact command line and package documentation."),
        Entry(120, "ERROR_CALL_NOT_IMPLEMENTED", "A custom action called an unavailable function.", "Review the package custom action and supported Windows version."),
        Entry(1259, "ERROR_APPHELP_BLOCK", "Windows compatibility guidance blocked the application after the user declined to continue.", "Review application compatibility and the user's choice; do not bypass the block automatically."),
        Entry(1601, "ERROR_INSTALL_SERVICE_FAILURE", "The Windows Installer service could not be accessed.", "Review Installer service state, exact service registration and trusted engine files."),
        Entry(1602, "ERROR_INSTALL_USEREXIT", "The user canceled the installation.", "Confirm whether cancellation was intentional before retrying."),
        Entry(1603, "ERROR_INSTALL_FAILURE", "A fatal installation error occurred.", "Find the first actionable failure in the verbose Installer log; this code alone does not identify the cause."),
        Entry(1604, "ERROR_INSTALL_SUSPEND", "The installation is suspended or incomplete.", "Review Installer activity, restart state and the owning deployment before another attempt."),
        Entry(1605, "ERROR_UNKNOWN_PRODUCT", "The requested action applies only to an installed product.", "Check the exact product code and current registration state."),
        Entry(1606, "ERROR_UNKNOWN_FEATURE", "The requested feature is not registered.", "Check the exact product and feature identities against the package."),
        Entry(1607, "ERROR_UNKNOWN_COMPONENT", "The requested component is not registered.", "Review the package component identity and current product registration."),
        Entry(1608, "ERROR_UNKNOWN_PROPERTY", "The requested Installer property is unknown.", "Review the command line and package property documentation."),
        Entry(1609, "ERROR_INVALID_HANDLE_STATE", "An Installer handle is in an invalid state.", "Review the calling application and detailed Installer log."),
        Entry(1610, "ERROR_BAD_CONFIGURATION", "The product configuration data is corrupt.", "Inspect exact product registration and source availability before considering a supported product repair."),
        Entry(1611, "ERROR_INDEX_ABSENT", "A component qualifier is not present.", "Review the package component and qualifier data."),
        Entry(1612, "ERROR_INSTALL_SOURCE_ABSENT", "The installation source is unavailable.", "Review the exact registered source, network or media access and Installer source policy."),
        Entry(1613, "ERROR_INSTALL_PACKAGE_VERSION", "The package requires a newer Windows Installer service.", "Confirm package requirements and the supported Windows version."),
        Entry(1614, "ERROR_PRODUCT_UNINSTALLED", "The product is uninstalled.", "Recheck the exact product registration before another maintenance action."),
        Entry(1615, "ERROR_BAD_QUERY_SYNTAX", "The SQL query syntax is invalid or unsupported.", "Review the package database query or calling tool."),
        Entry(1616, "ERROR_INVALID_FIELD", "A record field does not exist.", "Review the package database and custom action that accessed the field."),
        Entry(1618, "ERROR_INSTALL_ALREADY_RUNNING", "Another installation is already in progress.", "Wait for current Installer activity to finish, then collect fresh activity and restart evidence."),
        Entry(1619, "ERROR_INSTALL_PACKAGE_OPEN_FAILED", "The installation package could not be opened.", "Verify the exact package path, access, download integrity and source availability."),
        Entry(1620, "ERROR_INSTALL_PACKAGE_INVALID", "The installation package is invalid or unsupported.", "Verify the package signature, integrity, architecture and vendor source."),
        Entry(1621, "ERROR_INSTALL_UI_FAILURE", "The Windows Installer user interface could not start.", "Review execution context, session interactivity and the detailed Installer log."),
        Entry(1622, "ERROR_INSTALL_LOG_FAILURE", "The Installer log could not be opened.", "Review the selected log path, parent folder, permissions and free space."),
        Entry(1623, "ERROR_INSTALL_LANGUAGE_UNSUPPORTED", "The package language is unsupported by this system.", "Confirm package language and Windows language compatibility."),
        Entry(1624, "ERROR_INSTALL_TRANSFORM_FAILURE", "A transform could not be applied.", "Verify the exact transform path, integrity and applicability to this package version."),
        Entry(1625, "ERROR_INSTALL_PACKAGE_REJECTED", "System policy forbids this installation.", "Review effective Installer, AppLocker and software restriction policy with the policy owner."),
        Entry(1626, "ERROR_FUNCTION_NOT_CALLED", "An Installer function could not run.", "Review the detailed Installer log and package action sequencing."),
        Entry(1627, "ERROR_FUNCTION_FAILED", "An Installer function failed.", "Review the first detailed log failure before this final code."),
        Entry(1628, "ERROR_INVALID_TABLE", "The package contains an invalid or unknown table.", "Verify package integrity and vendor compatibility."),
        Entry(1629, "ERROR_DATATYPE_MISMATCH", "Package data has the wrong type.", "Review package database integrity and custom transforms."),
        Entry(1630, "ERROR_UNSUPPORTED_TYPE", "Package data uses an unsupported type.", "Confirm package format and Windows Installer version compatibility."),
        Entry(1631, "ERROR_CREATE_FAILED", "The Windows Installer service could not start.", "Review Installer service state, service failure history and exact registration."),
        Entry(1632, "ERROR_INSTALL_TEMP_UNWRITABLE", "The temporary folder is full or inaccessible.", "Review free space and access to the executing account's temporary folder."),
        Entry(1633, "ERROR_INSTALL_PLATFORM_UNSUPPORTED", "This package is not supported on this platform.", "Confirm Windows architecture, edition, build and package requirements."),
        Entry(1634, "ERROR_INSTALL_NOTUSED", "A component is not used on this machine.", "Review package feature selection and component conditions."),
        Entry(1635, "ERROR_PATCH_PACKAGE_OPEN_FAILED", "The patch package could not be opened.", "Verify the exact patch path, access, integrity and vendor source."),
        Entry(1636, "ERROR_PATCH_PACKAGE_INVALID", "The patch package is invalid.", "Verify patch integrity, signature and applicability."),
        Entry(1637, "ERROR_PATCH_PACKAGE_UNSUPPORTED", "This Windows Installer version cannot process the patch.", "Confirm patch and Windows Installer version requirements."),
        Entry(1638, "ERROR_PRODUCT_VERSION", "Another version of this product is already installed.", "Inventory exact installed product and upgrade identities before changing either version."),
        Entry(1639, "ERROR_INVALID_COMMAND_LINE", "The command line is invalid.", "Review the exact msiexec syntax, quoting and package parameters."),
        Entry(1640, "ERROR_INSTALL_REMOTE_DISALLOWED", "The current server client session cannot perform this installation.", "Review the deployment context and supported server installation method."),
        Entry(1641, "ERROR_SUCCESS_REBOOT_INITIATED", "The operation succeeded and initiated a restart.", "After Windows restarts, verify the exact product state before another action.", true, true),
        Entry(1642, "ERROR_PATCH_TARGET_NOT_FOUND", "The patch target is missing or has a different version.", "Inventory the exact installed product code and version, then confirm patch applicability."),
        Entry(1643, "ERROR_PATCH_PACKAGE_REJECTED", "System policy rejected the patch package.", "Review effective software restriction and Installer policy with the policy owner."),
        Entry(1644, "ERROR_INSTALL_TRANSFORM_REJECTED", "System policy rejected one or more transforms.", "Review effective Installer transform policy with the policy owner."),
        Entry(1645, "ERROR_INSTALL_REMOTE_PROHIBITED", "Windows Installer prohibits this installation from Remote Desktop.", "Use an approved deployment context; do not bypass session restrictions automatically."),
        Entry(1646, "ERROR_PATCH_REMOVAL_UNSUPPORTED", "The patch package cannot be removed.", "Use the vendor-supported recovery path and preserve cached packages and registration."),
        Entry(1647, "ERROR_UNKNOWN_PATCH", "The patch is not applied to this product.", "Compare exact patch and product identities before another action."),
        Entry(1648, "ERROR_PATCH_NO_SEQUENCE", "No valid sequence could be found for the patch set.", "Review vendor patch sequencing and prerequisite product versions."),
        Entry(1649, "ERROR_PATCH_REMOVAL_DISALLOWED", "System policy forbids removing the patch.", "Review the request with the policy owner; preserve managed restrictions."),
        Entry(1650, "ERROR_INVALID_PATCH_XML", "The patch XML data is invalid.", "Verify patch integrity and obtain a supported package from the vendor."),
        Entry(1651, "ERROR_PATCH_MANAGED_ADVERTISED_PRODUCT", "An administrative user could not apply a per-user managed or advertised patch.", "Review deployment scope, assignment and the target user's product registration."),
        Entry(1652, "ERROR_INSTALL_SERVICE_SAFEBOOT", "Windows Installer is unavailable in Safe Mode.", "Review the boot context before scheduling another installation."),
        Entry(1653, "ERROR_ROLLBACK_DISABLED", "Disabled rollback blocked a multiple-package transaction.", "Review rollback policy with its owner and preserve managed settings."),
        Entry(1654, "ERROR_INSTALL_REJECTED", "This application is not supported on this Windows version.", "Confirm application compatibility and obtain a supported vendor release."),
        Entry(3010, "ERROR_SUCCESS_REBOOT_REQUIRED", "The operation succeeded and a restart is required.", "Restart at an approved time, then verify the exact product state before another action.", true, true)
    };

    private static readonly IReadOnlyDictionary<int, WindowsInstallerCodeReference> ByCode =
        All.ToDictionary(entry => entry.Code);

    public static WindowsInstallerCodeReference? Find(int code) => ByCode.TryGetValue(code, out var entry) ? entry : null;

    private static WindowsInstallerCodeReference Entry(int code, string symbol, string summary,
        string diagnosticScope, bool success = false, bool restartRequired = false) =>
        new(code, symbol, summary, diagnosticScope, success, restartRequired);
}
