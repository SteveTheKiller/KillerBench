using KillerBench.Engine;

internal static class DefenderProtectionChecks
{
    internal static void Run()
    {
        const string enabled = "{\"complete\":true,\"mode\":\"Normal\",\"service\":true,\"antivirus\":true,\"realtime\":true,\"behavior\":true,\"ioav\":true,\"nis\":true,\"tamper\":true,\"pua\":1,\"network\":1,\"maps\":2,\"samples\":1,\"cloud\":2,\"cfa\":1,\"managed\":[]}";
        var evidence = new ToolResult(0, enabled, "", false);
        var checks = DefenderProtectionDiagnostics.Interpret(evidence);
        Require(checks.Count == 10 && checks.Take(7).All(check => check.State == CheckState.Clear), "Enabled protection layers are classified with bounded scope");
        Require(checks.All(check => check.Evidence == evidence && check.Repair is null), "Protection settings retain evidence and never select repair");
        Require(checks.All(check => check.Detail.Contains("No Defender preference, exclusion or policy was changed")), "Every setting states the mutation boundary");

        var reduced = DefenderProtectionDiagnostics.Interpret(evidence with
        {
            Output = enabled.Replace("\"realtime\":true", "\"realtime\":false")
                .Replace("\"behavior\":true", "\"behavior\":false")
                .Replace("\"ioav\":true", "\"ioav\":false")
                .Replace("\"nis\":true", "\"nis\":false")
                .Replace("\"tamper\":true", "\"tamper\":false")
                .Replace("\"pua\":1", "\"pua\":0")
                .Replace("\"network\":1", "\"network\":2")
                .Replace("\"maps\":2", "\"maps\":0")
        });
        Require(reduced.Take(6).All(check => check.State == CheckState.Attention) &&
            reduced[6].State == CheckState.Information && reduced[7].State == CheckState.Attention,
            "Inactive, disabled and audit settings remain distinct");

        var managed = DefenderProtectionDiagnostics.Interpret(evidence with
        {
            Output = enabled.Replace("\"managed\":[]", "\"managed\":[\"PUAProtection\",\"EnableNetworkProtection\"]")
        });
        Require(managed.All(check => check.Detail.Contains("Recognized machine policy value names observed")) &&
            managed[0].Detail.Contains("Effective policy ownership requires policy tooling"),
            "Recognized policy names remain evidence without an invented authority claim");

        foreach (string output in new[]
        {
            "{}", enabled.Replace("\"pua\":1", "\"pua\":9"),
            enabled.Replace("\"behavior\":true,", ""),
            enabled.Replace("\"managed\":[]", "\"managed\":[\"UnknownPolicy\"]"),
            enabled.Replace("\"managed\":[]", "\"managed\":[\"PUAProtection\",\"puaprotection\"]")
        }) Require(DefenderProtectionDiagnostics.Interpret(evidence with { Output = output }).All(check => check.State == CheckState.Unknown),
            "Malformed or unsupported Defender evidence fails closed");

        foreach (ToolResult incomplete in new[] { evidence with { Error = "provider failure" }, evidence with { Truncated = true }, evidence with { TimedOut = true } })
            Require(DefenderProtectionDiagnostics.Interpret(incomplete).All(check => check.State == CheckState.Unknown),
                "Incomplete Defender collection cannot establish settings");

        string command = DefenderProtectionDiagnostics.QueryStep().Arguments.Last();
        Require(command.Contains("Get-MpComputerStatus") && command.Contains("Get-MpPreference") &&
            !command.Contains("ExclusionPath") && !command.Contains("Set-MpPreference"),
            "Native collection is read-only and excludes Defender exclusion contents");
        Console.WriteLine("PASS: Defender active, behavior, downloaded-file, network-inspection, tamper, PUA, network, cloud, sample and controlled-folder settings remain bounded and read only.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
