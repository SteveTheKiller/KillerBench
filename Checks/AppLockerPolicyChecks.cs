using KillerBench.Engine;

internal static class AppLockerPolicyChecks
{
    public static void Run()
    {
        const string complete = "{\"complete\":true,\"collections\":[{\"collection\":\"Exe\",\"mode\":\"Enabled\",\"rules\":12},{\"collection\":\"Msi\",\"mode\":\"AuditOnly\",\"rules\":3},{\"collection\":\"Script\",\"mode\":\"NotConfigured\",\"rules\":0},{\"collection\":\"Dll\",\"mode\":\"Enabled\",\"rules\":2},{\"collection\":\"Appx\",\"mode\":\"Enabled\",\"rules\":5}]}";
        var evidence = new ToolResult(0, complete, "", false);
        var checks = AppLockerPolicyDiagnostics.Interpret(evidence);
        Require(checks.Count == 5 && checks.All(check => check.State == CheckState.Information && check.Repair is null && check.Evidence == evidence),
            "Complete AppLocker collections remain informational and read only");
        Require(checks[0].Detail.Contains("enabled with 12 aggregate rules") && checks[1].Detail.Contains("audit-only") &&
            checks[2].Detail.Contains("No effective AppLocker collection is configured"),
            "Enforced, audit-only and unconfigured collections remain distinct");
        Require(checks.All(check => check.Detail.Contains("Rule identities, paths, publishers, hashes, users and SIDs were not retained")),
            "Every collection states the privacy boundary");

        foreach (string output in new[]
        {
            "{}",
            complete.Replace("\"rules\":12", "\"rules\":-1"),
            complete.Replace("\"mode\":\"Enabled\"", "\"mode\":\"FutureMode\""),
            complete.Replace("{\"collection\":\"Appx\",\"mode\":\"Enabled\",\"rules\":5}", "{\"collection\":\"Exe\",\"mode\":\"Enabled\",\"rules\":5}"),
            complete.Replace("\"rules\":12", "\"rules\":12,\"names\":[\"private rule\"]")
        })
            Require(AppLockerPolicyDiagnostics.Interpret(evidence with { Output = output }).All(check => check.State == CheckState.Unknown),
                "Malformed, future, duplicate or privacy-expanded AppLocker evidence fails closed");

        foreach (ToolResult incomplete in new[]
        {
            evidence with { Error = "provider failure" }, evidence with { Truncated = true }, evidence with { TimedOut = true }
        })
            Require(AppLockerPolicyDiagnostics.Interpret(incomplete).All(check => check.State == CheckState.Unknown),
                "Incomplete AppLocker collection cannot establish effective policy");

        string command = AppLockerPolicyDiagnostics.QueryStep().Arguments.Last();
        Require(command.Contains("Get-AppLockerPolicy -Effective -Xml", StringComparison.Ordinal) &&
            !command.Contains("Set-AppLockerPolicy", StringComparison.OrdinalIgnoreCase) &&
            !command.Contains("RuleName", StringComparison.OrdinalIgnoreCase) &&
            !command.Contains("UserOrGroupSid", StringComparison.OrdinalIgnoreCase),
            "Native AppLocker collection is effective, aggregate and read only");
        Console.WriteLine("PASS: effective AppLocker collection modes and rule counts remain bounded, privacy safe and read only.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
