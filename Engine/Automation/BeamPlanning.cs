namespace KillerBench.Engine;

public enum BeamSourceKind { LocalFile, HttpsUrl }
public enum BeamInstallerType { Exe, Msi }

public sealed record BeamOptions(BeamSourceKind SourceKind, string Source, BeamInstallerType InstallerType,
    string AppName, string Publisher, string InstallSwitches, IReadOnlyList<string> TargetHostnames,
    string TenantDomain)
{
    public void Validate()
    {
        if (!Enum.IsDefined(SourceKind)) throw new ArgumentOutOfRangeException(nameof(SourceKind));
        if (!Enum.IsDefined(InstallerType)) throw new ArgumentOutOfRangeException(nameof(InstallerType));
        ValidateText(Source, nameof(Source), 32768, false);
        ValidateText(AppName, nameof(AppName), 256, false);
        ValidateText(Publisher, nameof(Publisher), 256, false);
        ValidateText(InstallSwitches, nameof(InstallSwitches), 2048, true);
        ValidateText(TenantDomain, nameof(TenantDomain), 253, false);
        if (TargetHostnames is null || TargetHostnames.Count == 0)
            throw new ArgumentException("Enter at least one target hostname.", nameof(TargetHostnames));
        if (TargetHostnames.Count > 1000)
            throw new ArgumentException("A BEAM plan is limited to 1,000 target entries.", nameof(TargetHostnames));
        if (SourceKind == BeamSourceKind.HttpsUrl &&
            (!Uri.TryCreate(Source, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
             !string.IsNullOrEmpty(uri.UserInfo) || string.IsNullOrWhiteSpace(uri.Host)))
            throw new ArgumentException("Use an absolute HTTPS installer URL without embedded credentials.", nameof(Source));
        if (SourceKind == BeamSourceKind.HttpsUrl)
        {
            string extension = Path.GetExtension(new Uri(Source).AbsolutePath);
            string expected = InstallerType == BeamInstallerType.Msi ? ".msi" : ".exe";
            if ((string.Equals(extension, ".exe", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(extension, ".msi", StringComparison.OrdinalIgnoreCase)) &&
                !string.Equals(extension, expected, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The installer URL extension conflicts with the selected installer type.", nameof(Source));
        }
        if (SourceKind == BeamSourceKind.LocalFile)
        {
            if (!Path.IsPathFullyQualified(Source))
                throw new ArgumentException("Use a fully qualified local installer path.", nameof(Source));
            string expected = InstallerType == BeamInstallerType.Msi ? ".msi" : ".exe";
            if (!string.Equals(Path.GetExtension(Source), expected, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The local installer extension must match the selected installer type.", nameof(Source));
        }
        ValidateDomain(TenantDomain);
        foreach (string target in TargetHostnames) ValidateTargetText(target);
    }

    private static void ValidateText(string? value, string name, int maximum, bool allowEmpty)
    {
        if (value is null || (!allowEmpty && string.IsNullOrWhiteSpace(value)) || value.Length > maximum ||
            value.Any(char.IsControl))
            throw new ArgumentException((allowEmpty ? "Use plain text within " : "Enter plain text within ") + maximum + " characters.", name);
    }

    private static void ValidateDomain(string value)
    {
        string domain = value.Trim().TrimEnd('.');
        if (domain.Length == 0 || domain.Length > 253 || !domain.Contains('.') ||
            domain.Split('.').Any(label => label.Length is < 1 or > 63 || label[0] == '-' || label[^1] == '-' ||
                label.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')))
            throw new ArgumentException("Use a DNS tenant domain such as contoso.onmicrosoft.com.", nameof(TenantDomain));
    }

    private static void ValidateTargetText(string? value)
    {
        if (value is null || value.Length > 253 || value.Any(char.IsControl))
            throw new ArgumentException("Target entries must be plain hostnames within 253 characters.", nameof(TargetHostnames));
    }
}

public sealed record BeamPlan(string SourceDisplay, BeamInstallerType InstallerType, string SetupFile,
    string AppDisplayName, string Publisher, string InstallCommand, string UninstallCommand,
    IReadOnlyList<string> TargetHostnames, IReadOnlyList<string> IgnoredIpTargets, string TenantDomain,
    string SecurityGroupName, string InstallContext, string AssignmentIntent, string DetectionMethod,
    string RestartBehavior);

public sealed record BeamReview(BeamPlan Plan, bool SourceVerified, bool Ready, bool ExecutionSupported,
    IReadOnlyList<WorkflowCheck> Checks)
{
    public string Format() => "BEAM / INTUNE DEPLOYMENT REVIEW\n" +
        "Source: " + Plan.SourceDisplay + "\nInstaller: " + Plan.InstallerType + " / " + Plan.SetupFile +
        "\nApp: " + Plan.AppDisplayName + "\nPublisher: " + Plan.Publisher +
        "\nTenant domain: " + Plan.TenantDomain + "\nTargets: " + string.Join(", ", Plan.TargetHostnames) +
        (Plan.IgnoredIpTargets.Count == 0 ? "" : "\nIgnored IP targets: " + string.Join(", ", Plan.IgnoredIpTargets)) +
        "\nInstall context: " + Plan.InstallContext + "\nAssignment: " + Plan.AssignmentIntent +
        "\nDetection: " + Plan.DetectionMethod + "\nRestart: " + Plan.RestartBehavior +
        "\nInstall command: " + Plan.InstallCommand + "\nUninstall command: " + Plan.UninstallCommand +
        "\nSource verified: " + (SourceVerified ? "Yes" : "No") +
        "\nReady for execution review: " + (Ready ? "Yes" : "No") +
        "\nExecution available: No\n\nNo authentication, download, packaging, upload, application change, group change, assignment, device sync or installer execution occurred.";
}

public static class BeamPlanning
{
    public static async Task<BeamReview> ReviewAsync(BeamOptions options,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        token.ThrowIfCancellationRequested();
        progress?.Report("Validating BEAM installer source and target hostnames...");
        var normalized = NormalizeTargets(options.TargetHostnames);
        if (normalized.Hostnames.Count == 0)
            throw new ArgumentException("Enter at least one hostname. Intune device matching does not accept IP targets.", nameof(options));
        string setupFile = SetupFile(options);
        bool sourceVerified = false;
        string sourceDetail;
        if (options.SourceKind == BeamSourceKind.LocalFile)
        {
            (sourceVerified, sourceDetail) = await VerifyLocalSourceAsync(options.Source, options.InstallerType, token).ConfigureAwait(false);
        }
        else
        {
            sourceDetail = "HTTPS URL syntax accepted. The remote response, redirects, binary header, publisher signature, size and content are not fetched or trusted by this review.";
        }
        string switches = options.InstallSwitches.Trim();
        if (options.InstallerType == BeamInstallerType.Msi && switches.Length == 0) switches = "/qn";
        string install = options.InstallerType == BeamInstallerType.Msi
            ? "msiexec.exe /i \"" + setupFile + "\"" + (switches.Length == 0 ? "" : " " + switches)
            : ".\\" + setupFile + (switches.Length == 0 ? "" : " " + switches);
        string uninstall = options.InstallerType == BeamInstallerType.Msi
            ? "msiexec.exe /x \"" + setupFile + "\" /qn"
            : "No automatic uninstall command supplied";
        string safeName = SafeName(options.AppName.Trim());
        var plan = new BeamPlan(SourceDisplay(options), options.InstallerType, setupFile,
            options.AppName.Trim() + " (BEAM)", options.Publisher.Trim(), install, uninstall,
            normalized.Hostnames, normalized.IpAddresses, options.TenantDomain.Trim().TrimEnd('.'),
            "BEAM-" + safeName, "SYSTEM", "Required for the generated device group",
            "Per-run marker at C:\\ProgramData\\BEAM\\BEAM.marker", "Suppress restart");
        bool ready = options.SourceKind == BeamSourceKind.HttpsUrl || sourceVerified;
        var checks = new List<WorkflowCheck>
        {
            new("Installer source", sourceVerified ? CheckState.Clear : options.SourceKind == BeamSourceKind.HttpsUrl ? CheckState.Unknown : CheckState.Attention, sourceDetail),
            new("Target hostnames", CheckState.Information,
                normalized.Hostnames.Count + " unique hostname target(s) retained. " +
                (normalized.IpAddresses.Count == 0 ? "No IP targets were supplied. " : normalized.IpAddresses.Count + " IP target(s) were ignored because Intune matches these devices by hostname. ") +
                "Entra and Intune membership, duplicate display names, license state and device check-in are not verified."),
            new("Package behavior", CheckState.Information,
                "The plan uses SYSTEM context, x64 and x86 applicability, Windows 10 1607 minimum, a required group assignment, visible notifications, suppressed restart behavior and a new content version when the BEAM app already exists."),
            new("Detection and uninstall", CheckState.Information,
                "The source workflow uses a unique per-run marker, so a later BEAM run intentionally installs again. Marker detection does not prove application health. " +
                (options.InstallerType == BeamInstallerType.Msi ? "The generated MSI uninstall command still requires product-specific validation." : "The source workflow provides no real EXE uninstall command.")),
            new("Tenant execution", CheckState.Information,
                "Execution is unavailable. Global or Intune administrator sign-in, Graph consent, tenant licensing, IntuneWinAppUtil trust, download, packaging, upload, group membership, assignment and device sync require separate guarded implementation and verification.")
        };
        return new(plan, sourceVerified, ready, false, checks.AsReadOnly());
    }

    public static (IReadOnlyList<string> Hostnames, IReadOnlyList<string> IpAddresses) NormalizeTargets(
        IEnumerable<string> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        var hosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var ips = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in targets)
        {
            string value = (raw ?? "").Trim();
            if (value.Length == 0) continue;
            if (System.Net.IPAddress.TryParse(value, out _)) { ips.Add(value); continue; }
            if (value.Length > 253 || value.TrimEnd('.').Split('.').Any(label => label.Length is < 1 or > 63 ||
                label[0] == '-' || label[^1] == '-' || label.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-')))
                throw new ArgumentException("Target hostnames must use DNS labels with letters, digits and internal hyphens.", nameof(targets));
            hosts.Add(value.TrimEnd('.'));
        }
        return (Array.AsReadOnly(hosts.Order(StringComparer.OrdinalIgnoreCase).ToArray()),
            Array.AsReadOnly(ips.Order(StringComparer.OrdinalIgnoreCase).ToArray()));
    }

    private static async Task<(bool Verified, string Detail)> VerifyLocalSourceAsync(string path,
        BeamInstallerType type, CancellationToken token)
    {
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            byte[] header = new byte[8];
            int read = await stream.ReadAsync(header, token).ConfigureAwait(false);
            bool valid = type == BeamInstallerType.Exe
                ? read >= 2 && header[0] == 0x4D && header[1] == 0x5A
                : read >= 4 && header[0] == 0xD0 && header[1] == 0xCF && header[2] == 0x11 && header[3] == 0xE0;
            return valid
                ? (true, "The local file exists and its header matches the selected installer type. Signature, publisher, installer behavior and compatibility remain unverified.")
                : (false, "The local file header does not match the selected installer type. No package should be created from this source.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return (false, "The local installer could not be read: " + error.Message + ". No package should be created from this source.");
        }
    }

    private static string SetupFile(BeamOptions options)
    {
        string leaf;
        if (options.SourceKind == BeamSourceKind.LocalFile) leaf = Path.GetFileName(options.Source);
        else
        {
            var uri = new Uri(options.Source);
            leaf = Path.GetFileName(Uri.UnescapeDataString(uri.AbsolutePath));
        }
        string expected = options.InstallerType == BeamInstallerType.Msi ? ".msi" : ".exe";
        if (!leaf.EndsWith(expected, StringComparison.OrdinalIgnoreCase)) leaf = "installer" + expected;
        if (leaf.Length > 255 || leaf.Any(c => char.IsControl(c) || Path.GetInvalidFileNameChars().Contains(c)))
            throw new ArgumentException("The installer file name contains unsupported characters.", nameof(options));
        return leaf;
    }

    private static string SourceDisplay(BeamOptions options)
    {
        if (options.SourceKind == BeamSourceKind.LocalFile) return options.Source;
        var uri = new Uri(options.Source);
        string display = uri.GetLeftPart(UriPartial.Path);
        return string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment)
            ? display : display + " (query and fragment omitted)";
    }

    private static string SafeName(string value)
    {
        string safe = new(value.Select(c => "\\/:*?\"<>|".Contains(c) ? '_' : c).ToArray());
        return safe.Length <= 128 ? safe : safe[..128];
    }
}
