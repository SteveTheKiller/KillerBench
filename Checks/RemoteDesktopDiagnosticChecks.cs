using KillerBench.Engine;

internal static class RemoteDesktopDiagnosticChecks
{
    internal static void Run()
    {
        var raw = new ToolResult(0, "{\"deny\":0,\"nla\":1,\"port\":3389,\"policyDeny\":null,\"policyNla\":null}", "", false);
        var checks = RemoteDesktopDiagnostics.Interpret(raw);
        Require(checks.Count == 3 && checks.All(check => check.Repair is null && check.Evidence == raw) &&
            checks[0].Detail.Contains("enabled") && checks[1].State == CheckState.Information && checks[2].Detail.Contains("3389"),
            "Complete Remote Desktop configuration remains read only and retains evidence");
        var managed = RemoteDesktopDiagnostics.Interpret(raw with { Output = "{\"deny\":1,\"nla\":0,\"port\":3390,\"policyDeny\":1,\"policyNla\":1}" });
        Require(managed[0].Detail.Contains("Machine policy values observed") && managed[1].State == CheckState.Attention && managed[1].Detail.Contains("policy"),
            "Remote Desktop local and policy observations remain distinct");
        foreach (var bad in new[]
        {
            raw with { ExitCode = 1 }, raw with { Error = "denied" }, raw with { TimedOut = true }, raw with { Truncated = true },
            raw with { Output = "{}" }, raw with { Output = "{\"deny\":2,\"nla\":1,\"port\":3389,\"policyDeny\":null,\"policyNla\":null}" },
            raw with { Output = "{\"deny\":0,\"nla\":1,\"port\":70000,\"policyDeny\":null,\"policyNla\":null}" },
            raw with { Output = "{\"deny\":0,\"nla\":1,\"port\":3389,\"policyDeny\":null,\"policyNla\":null,\"extra\":1}" }
        }) Require(RemoteDesktopDiagnostics.Interpret(bad).Single().State == CheckState.Unknown,
            "Incomplete Remote Desktop configuration remains unknown");
        Console.WriteLine("PASS: Remote Desktop enablement, NLA, port and policy evidence remain read only and bounded.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
