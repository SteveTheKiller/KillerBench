using System.Text.RegularExpressions;

namespace KillerBench.Engine.Evidence;

public static partial class Investigation
{
    private const string MsiReference = "https://learn.microsoft.com/en-us/windows/win32/msi/error-codes";

    [GeneratedRegex(@"^MSI \([cs]\) \([0-9A-F]+:[0-9A-F]+\)(?: \[(?<time>(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d:\d{3})\])?: MainEngineThread is returning (?<code>1601|1603|1604|1605|1606|1607|1610|1612|1613|1618|1619|1620|1621|1622|1623|1624|1625|1631|1632|1633|1635|1636|1637|1638|1639|1642|1643|1644|1646|1647|1648|1649|1652|1653)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MsiOutcomePattern();

    [GeneratedRegex(@"^MSI \([cs]\) \([0-9A-F]+:[0-9A-F]+\)(?: \[(?<time>(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d:\d{3})\])?: MainEngineThread is returning (?<code>0|1641|3010)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MsiSuccessPattern();

    [GeneratedRegex(@"^MSI \([cs]\) \([0-9A-F]+:[0-9A-F]+\)(?: \[(?:[01]\d|2[0-3]):[0-5]\d:[0-5]\d:\d{3}\])?: ", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MsiPrefixPattern();

    [GeneratedRegex(@"^Action ended (?<time>(?:[01]?\d|2[0-3]):[0-5]\d:[0-5]\d): (?<action>[A-Za-z_][A-Za-z0-9_.]{0,127})\. Return value 3\.\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex MsiActionFailurePattern();

    private static (string Id, string Title, string Explanation, string Next, string Url) MsiDescription(string code)
    {
        if (int.TryParse(code, System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out int numeric) &&
            WindowsInstallerErrorCatalog.Find(numeric) is { } reference)
            return ("KB-MSI-" + code, reference.Symbol, reference.Summary,
                reference.DiagnosticScope + " A later attempt may have a different outcome.",
                WindowsInstallerErrorCatalog.SourceUrl);
        var (title, explanation, next) = code switch
        {
            "ACTION" => ("Installer action failure recorded", "An MSI action ended with logged return value 3. The action name and nearby lines are review leads, not proof of the underlying cause.", "Review the failed action and its preceding messages in the full package log. Nested actions can repeat a failure; nearby text is context only and may belong to concurrent activity."),
            "1603" => ("Installer failure recorded", "The installer returned 1603, a general installation failure.", "Inspect preceding action failures and package-specific messages. This code alone does not identify the cause; do not reset permissions or delete installer caches based on it."),
            "1604" => ("Suspended installation recorded", "This installation stopped before completion.", "Check deployment status and its supported resume procedure. Preserve transaction records."),
            "1605" => ("Installed product not recognized", "The requested operation did not identify an installed product.", "Compare product identity, account and installation context. Do not recreate registrations manually."),
            "1606" => ("Unregistered feature reported", "The requested feature identity was not registered.", "Compare the feature with the exact vendor package and installed version."),
            "1607" => ("Unregistered component reported", "The requested component identity was not registered.", "Identify the owning package before considering supported product recovery."),
            "1613" => ("Installer engine version mismatch", "The package requires an unsupported Installer version.", "Compare vendor prerequisites with the operating system. Use supported servicing channels."),
            "1621" => ("Installer interface startup failed", "The Installer interface could not start.", "Review deployment session and package UI requirements. Do not force a silent retry."),
            "1623" => ("Unsupported installer language", "The system did not support the package language.", "Obtain the correct language package from the deployment owner."),
            "1631" => ("Installer service startup failed", "Windows Installer did not start.", "Run Installer readiness and service checks to collect current evidence before recovery."),
            "1612" => ("Installer source unavailable", "The installer returned 1612 because its product source was unavailable.", "Confirm the exact installed product and required source with the vendor or deployment owner. Preserve the installer cache and product registration."),
            "1618" => ("Concurrent installation recorded", "The installer returned 1618 while another installation was in progress.", "Check whether deployment is still active before retrying. This historical entry does not establish a currently stuck process; do not terminate installers or remove transaction markers."),
            "1619" => ("Installer package could not be opened", "The installer returned 1619 while opening the package.", "Check the selected package location and access under the deployment identity. Verify package provenance with its vendor before retrying."),
            "1625" => ("Installation blocked by policy", "The installer returned 1625 for a policy restriction.", "Review the deployment context with the policy owner. Do not disable organizational installation restrictions."),
            "1622" => ("Installer log could not be opened", "The installer returned 1622 while opening its log file.", "Check the configured log folder and write access for the deployment account. Preserve existing logs."),
            "1632" => ("Installer temporary folder unavailable", "The installer returned 1632 for a full or inaccessible temporary folder.", "Check available space and the deployment account's temporary-folder access. Do not delete active installation files or reset permissions from this record."),
            "1633" => ("Installer platform mismatch", "The installer returned 1633 for an unsupported platform.", "Compare package architecture and supported systems with this PC. Obtain the matching vendor package; do not bypass compatibility checks."),
            "1642" => ("Patch target mismatch", "The installer returned 1642 because the patch target was missing or did not match.", "Confirm installed product identity and version against the patch requirements. Preserve product registration and cached packages."),
            "1635" => ("Patch could not be opened", "Windows Installer could not open the selected patch.", "Check the patch path and access under the deployment identity. Preserve cached packages."),
            "1636" => ("Invalid patch package", "Windows Installer rejected the patch package.", "Confirm patch provenance and integrity with the vendor. Do not replace cached packages manually."),
            "1637" => ("Unsupported patch format", "The installed Windows Installer service cannot process this patch.", "Compare vendor prerequisites with the installed operating system and installer version."),
            "1643" => ("Patch blocked by policy", "System policy rejected the patch.", "Review the deployment with the policy owner. Do not disable managed restrictions."),
            "1644" => ("Customization blocked by policy", "System policy rejected an installer customization.", "Review the selected transforms with the deployment owner. Preserve organizational policy."),
            "1638" => ("Another product version recorded", "The installer returned 1638 because another version was already installed.", "Inventory the installed version and confirm the vendor-supported upgrade path. Do not uninstall a product automatically based on this log."),
            "1601" => ("Installer service inaccessible", "Windows Installer reported unavailable service access.", "Run Installer readiness checks; do not infer registration damage from this code alone."),
            "1610" => ("Product configuration damaged", "Windows Installer reported corrupt product configuration.", "Identify the product and consult its supported recovery procedure. Preserve registration and cached packages."),
            "1620" => ("Invalid installer package", "Windows Installer rejected the package format.", "Obtain a verified package from the deployment owner; preserve the failed package for comparison."),
            "1624" => ("Installer transform failed", "Windows Installer could not apply a transform.", "Check transform paths and package compatibility with the deployment owner."),
            "1639" => ("Invalid installer arguments", "Windows Installer rejected its command line.", "Review quoting and supported options. Do not retry an edited command automatically."),
            "1652" => ("Installer unavailable in Safe Mode", "The recorded installation ran in Safe Mode.", "Review the boot context before scheduling another installation."),
            "1646" => ("Patch removal unsupported", "The patch does not support removal.", "Use the vendor-supported recovery path; preserve cached packages and registration."),
            "1647" => ("Patch not applied to product", "The selected patch is not applied to this product.", "Compare patch and product identities before retrying. Do not remove unrelated patches."),
            "1648" => ("Patch sequence unavailable", "Windows Installer could not order the selected patch set.", "Check vendor sequencing and prerequisite versions. Preserve the installer cache."),
            "1649" => ("Patch removal blocked by policy", "Policy prevented patch removal.", "Review the removal request with the policy owner. Do not disable managed restrictions."),
            "1653" => ("Rollback restriction blocked transaction", "Disabled rollback prevented a multiple-package transaction.", "Review rollback policy with its owner; preserve managed settings."),
            _ => throw new ArgumentOutOfRangeException(nameof(code))
        };
        return ("KB-MSI-" + code, title, explanation, next + " A later attempt may have a different outcome.",
            code == "ACTION" ? "https://learn.microsoft.com/en-us/windows/win32/msi/logging-of-action-return-values" : MsiReference);
    }
}
