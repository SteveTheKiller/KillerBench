namespace KillerBench.Engine.Updates;

/// <summary>
/// Judges the installed Windows client feature release against Microsoft's published release table and suggests
/// the newest release when the installed one is behind or out of servicing. The table ships with KillerBench and
/// is dated; builds newer than it are reported without a verdict.
/// </summary>
public static class WindowsFeatureVersionDiagnostics
{
    /// <summary>Date of Microsoft's Windows 11 release information this table was taken from.</summary>
    public const string TableDate = "2026-10-21";
    private const string Newest = "26H2";

    private sealed record Release(int Build, string Version, DateOnly ConsumerEnd, DateOnly EnterpriseEnd, bool NewDevicesOnly = false);

    // End of updates: Home, Pro, Pro Education and Pro for Workstations, then Enterprise, Education and IoT Enterprise.
    private static readonly Release[] Releases =
    [
        new(22000, "21H2", new(2023, 10, 10), new(2024, 10, 8)),
        new(22621, "22H2", new(2024, 10, 8), new(2025, 10, 14)),
        new(22631, "23H2", new(2025, 11, 11), new(2026, 11, 10)),
        new(26100, "24H2", new(2026, 10, 13), new(2027, 10, 12)),
        new(26200, "25H2", new(2027, 10, 12), new(2028, 10, 10)),
        new(26300, "26H2", new(2028, 10, 10), new(2029, 10, 9)),
        new(28000, "26H1", new(2028, 3, 14), new(2029, 3, 13), NewDevicesOnly: true)
    ];

    private const string NoChange = " No update, upgrade or setting was changed.";

    public static WorkflowCheck Check() => Assess(UpdateApplicabilityDiagnostics.Read(), DateOnly.FromDateTime(DateTime.UtcNow));

    public static WorkflowCheck Assess(UpdateApplicabilitySnapshot snapshot, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        const string name = "Windows feature version";
        if (!snapshot.Complete || !int.TryParse(snapshot.Build, out int build) || build <= 0 || string.IsNullOrWhiteSpace(snapshot.Edition))
            return new(name, CheckState.Unknown, "The installed Windows build or edition could not be read, so feature version currency is unknown." + NoChange);
        string edition = snapshot.Edition;
        if (string.Equals(snapshot.InstallationType, "Server", StringComparison.OrdinalIgnoreCase))
            return new(name, CheckState.Information, $"This is Windows Server build {build}. Client feature release guidance does not apply." + NoChange);
        if (build < 22000)
            return new(name, CheckState.Attention,
                $"This is Windows 10 (build {build}). Windows 10 reached end of support on 2025-10-14; Extended Security Updates, where enrolled, are time-limited. " +
                $"Upgrade to Windows 11 {Newest} if the hardware meets Windows 11 requirements." + NoChange);
        if (edition is "EnterpriseS" or "EnterpriseSN" or "IoTEnterpriseS" or "IoTEnterpriseSK")
            return new(name, CheckState.Information,
                $"This is a Windows 11 LTSC edition (build {build}). LTSC does not receive feature updates; check its fixed servicing end date before planning a change." + NoChange);
        var release = Releases.LastOrDefault(candidate => candidate.Build == build);
        if (release is null)
            return new(name, CheckState.Information, build > Releases.Max(candidate => candidate.Build)
                ? $"Build {build} is newer than KillerBench's release table ({TableDate}). It may be a new or preview release; confirm its servicing in Windows Update." + NoChange
                : $"Build {build} is not a published Windows 11 feature release in KillerBench's table ({TableDate}). It may be a preview build; confirm its servicing in Windows Update." + NoChange);
        bool enterprise = edition.StartsWith("Enterprise", StringComparison.OrdinalIgnoreCase) ||
            edition.StartsWith("Education", StringComparison.OrdinalIgnoreCase) ||
            edition.StartsWith("IoTEnterprise", StringComparison.OrdinalIgnoreCase) ||
            edition.Equals("ServerRdsh", StringComparison.OrdinalIgnoreCase);
        DateOnly end = enterprise ? release.EnterpriseEnd : release.ConsumerEnd;
        string ends = end.ToString("yyyy-MM-dd");
        if (release.Version == Newest || release.NewDevicesOnly)
            return new(name, CheckState.Clear, release.NewDevicesOnly
                ? $"Windows 11 {release.Version} (build {build}) ships on new devices and is serviced until {ends} for this edition. It is not offered as an in-place update." + NoChange
                : $"Windows 11 {release.Version} (build {build}) is the newest feature release and is serviced until {ends} for this edition." + NoChange);
        if (today > end)
            return new(name, CheckState.Attention,
                $"Windows 11 {release.Version} (build {build}) stopped receiving security updates for this edition on {ends}. Upgrade to Windows 11 {Newest} through Windows Update or installation media." + NoChange);
        return new(name, CheckState.Information,
            $"Windows 11 {release.Version} (build {build}) is serviced until {ends} for this edition. Windows 11 {Newest} is the newest feature release; plan the upgrade before that date." + NoChange);
    }
}
