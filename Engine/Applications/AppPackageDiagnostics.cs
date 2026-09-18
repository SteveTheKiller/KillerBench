namespace KillerBench.Engine;

public static class AppPackageDiagnostics
{
    public static WorkflowCheck Summarize(IReadOnlyList<WorkflowCheck> checks)
    {
        ArgumentNullException.ThrowIfNull(checks);
        const string name = "Registered app package summary";
        WorkflowCheck Unknown(string detail, ToolResult? evidence = null) => new(name, CheckState.Unknown,
            detail + " No package was launched, installed, repaired, reset or removed.", Evidence: evidence);
        if (checks.Count is < 1 or > 4097 || checks.Any(check => check is null))
            return Unknown("Packaged-app status evidence is unavailable or exceeds its bound.");
        var coverage = checks[0];
        if (!string.Equals(coverage.Name, "Registered app packages", StringComparison.Ordinal) || coverage.Evidence is null)
            return Unknown("Packaged-app coverage evidence is missing or has an unexpected identity.", coverage.Evidence);
        var states = checks.Select(check => check.EffectiveState).ToArray();
        int incomplete = states.Count(state => state == CheckState.Unknown);
        int attention = states.Count(state => state is CheckState.Attention or CheckState.Repairable);
        CheckState state = incomplete > 0 ? CheckState.Unknown : attention > 0 ? CheckState.Attention : CheckState.Information;
        return new(name, state,
            $"Reviewed {checks.Count - 1} registered packaged-app status rows: {attention} need attention and {incomplete} are incomplete. " +
            "The detailed App packages diagnostic retains individual identities. Provider status does not prove application operation or authorize recovery. " +
            "No package was launched, installed, repaired, reset or removed.", Evidence: coverage.Evidence);
    }

    public static IReadOnlyList<WorkflowCheck> Describe(AppPackageInventoryResult inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        var observed = AppPackageInventory.Interpret(inventory.Scope, inventory.Evidence);
        var checks = new List<WorkflowCheck>
        {
            new("Registered app packages", observed.Complete ? CheckState.Information : CheckState.Unknown,
                $"{observed.Packages.Count} packages; scope: {observed.Scope}. " +
                "CurrentAccount means the account running this process. Provisioned packages, Win32 applications, managed ownership and user data are not evaluated. No packages removed.",
                Evidence: observed.Evidence)
        };
        foreach (var package in observed.Packages)
            checks.Add(new(package.FullName, StatusState(package.Status),
                $"Family: {package.FamilyName}. Framework: {package.Framework}. Non-removable: {package.NonRemovable}. " +
                $"Windows package status: {package.Status ?? "unavailable"}. " +
                $"Reported dependencies: {(package.Dependencies is null ? "unknown" : package.Dependencies.Count.ToString(System.Globalization.CultureInfo.InvariantCulture))}. " +
                "Status is provider-reported, not a launch or integrity test. " + StatusGuidance(package.Status) +
                " These flags do not establish removal eligibility."));
        return checks.AsReadOnly();
    }

    private static CheckState StatusState(string? status)
    {
        if (status == "Ok" || status == "OK") return CheckState.Information;
        if (status is null) return CheckState.Unknown;
        var flags = status.Split(',', StringSplitOptions.TrimEntries);
        return flags.Length > 0 && flags.All(flag => flag is "LicenseIssue" or "Modified" or "Tampered" or "Disabled")
            ? CheckState.Attention : CheckState.Unknown;
    }

    private static string StatusGuidance(string? status)
    {
        if (StatusState(status) == CheckState.Information)
            return "No package status fault was reported. Application launch and user data remain untested.";
        if (StatusState(status) == CheckState.Unknown)
            return "The reported status is missing or not fully recognized. Recollect and review package deployment events before choosing a repair.";
        var steps = new List<string>();
        foreach (string flag in status!.Split(',', StringSplitOptions.TrimEntries).Distinct(StringComparer.Ordinal))
            steps.Add(flag switch
            {
                "LicenseIssue" => "Review the affected account's package entitlement and licensing source before reinstalling.",
                "Modified" => "Review deployment history and the trusted package source for unexpected payload changes.",
                "Tampered" => "Preserve deployment evidence and review package integrity with the security administrator before attempting recovery.",
                "Disabled" => "Review deployment state and administrator restrictions before attempting to enable or repair the package.",
                _ => throw new InvalidOperationException("Unrecognized package status.")
            });
        return string.Join(" ", steps);
    }
}
