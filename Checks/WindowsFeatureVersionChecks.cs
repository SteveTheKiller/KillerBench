using KillerBench.Engine;

/// <summary>Feature-version currency against the Windows 11 release table, judged on a fixed date.</summary>
internal static class WindowsFeatureVersionChecks
{
    internal static void Run()
    {
        var today = new DateOnly(2026, 10, 21);
        WorkflowCheck Assess(string edition, string build, string installation = "Client") =>
            WindowsFeatureVersionDiagnostics.Assess(new(true, "Windows", edition, "x", build, 1, installation, "X64"), today);

        var latest = Assess("Professional", "26300");
        Require(latest.State == CheckState.Clear && latest.Detail.Contains("26H2", StringComparison.Ordinal),
            "The newest feature release is current: " + latest.Detail);
        Require(Assess("Professional", "28000").State == CheckState.Clear,
            "26H1 ships only on new devices and is not offered an in-place update, so it is current");

        var serviced = Assess("Professional", "26200");
        Require(serviced.State == CheckState.Information && serviced.Detail.Contains("25H2", StringComparison.Ordinal) &&
            serviced.Detail.Contains("2027-10-12", StringComparison.Ordinal) && serviced.Detail.Contains("26H2", StringComparison.Ordinal),
            "A serviced older release names its end of updates and suggests the newest release: " + serviced.Detail);

        var expiredHome = Assess("Core", "26100");
        Require(expiredHome.State == CheckState.Attention && expiredHome.Detail.Contains("2026-10-13", StringComparison.Ordinal) &&
            expiredHome.Detail.Contains("26H2", StringComparison.Ordinal),
            "24H2 Home past its end of updates needs an upgrade: " + expiredHome.Detail);
        Require(Assess("Enterprise", "26100").State == CheckState.Information,
            "24H2 Enterprise is still serviced until 2027-10-12");
        Require(Assess("Education", "22631").State == CheckState.Information && Assess("Professional", "22631").State == CheckState.Attention,
            "23H2 dates follow the edition family");

        var windows10 = Assess("Professional", "19045");
        Require(windows10.State == CheckState.Attention && windows10.Detail.Contains("Windows 10", StringComparison.Ordinal) &&
            windows10.Detail.Contains("Windows 11", StringComparison.Ordinal),
            "Windows 10 is past end of support and is pointed at Windows 11: " + windows10.Detail);

        Require(Assess("EnterpriseS", "26100").State == CheckState.Information && Assess("EnterpriseS", "26100").Detail.Contains("LTSC", StringComparison.Ordinal),
            "LTSC editions do not take feature updates and are not told to upgrade");
        Require(Assess("Professional", "26500").State == CheckState.Information,
            "A build newer than the release table is reported, not judged");
        Require(Assess("ServerStandard", "26100", "Server").State == CheckState.Information,
            "Windows Server is outside the client release table");
        Require(WindowsFeatureVersionDiagnostics.Assess(new(false, null, null, null, null, null, null, null, "Unavailable"), today).State == CheckState.Unknown,
            "Missing version evidence stays unknown");
        foreach (var check in new[] { latest, serviced, expiredHome, windows10 })
            Require(check.Repair is null && check.Detail.Contains("No update", StringComparison.Ordinal), "The check only reports; it changes nothing");
        Console.WriteLine("PASS: Windows feature version is judged against the release table by edition, with upgrade suggestions and no changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
