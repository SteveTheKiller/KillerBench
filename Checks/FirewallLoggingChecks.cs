using KillerBench.Engine;

internal static class FirewallLoggingChecks
{
    internal static void Run()
    {
        const string json = "{\"profiles\":[" +
            "{\"Name\":\"Public\",\"Allowed\":\"True\",\"Blocked\":\"False\",\"Ignored\":\"NotConfigured\",\"MaximumKilobytes\":\"4096\",\"HasLogFilePath\":false}," +
            "{\"Name\":\"Domain\",\"Allowed\":\"False\",\"Blocked\":\"False\",\"Ignored\":\"False\",\"MaximumKilobytes\":null,\"HasLogFilePath\":false}," +
            "{\"Name\":\"Private\",\"Allowed\":\"False\",\"Blocked\":\"True\",\"Ignored\":\"False\",\"MaximumKilobytes\":\"8192\",\"HasLogFilePath\":true}]}";
        var evidence = new ToolResult(0, json, "", false);
        var checks = FirewallLoggingDiagnostics.Interpret(evidence);
        if (checks.Count != 3 || checks[0].Name != "Firewall logging: Domain" || checks[1].Name != "Firewall logging: Private" ||
            checks[2].Name != "Firewall logging: Public" || checks[0].State != CheckState.Information ||
            checks[1].State != CheckState.Information || checks[2].State != CheckState.Attention ||
            !checks[0].Detail.Contains("No traffic category", StringComparison.Ordinal) ||
            !checks[1].Detail.Contains("8192 KiB", StringComparison.Ordinal) ||
            !checks[2].Detail.Contains("without a complete destination", StringComparison.Ordinal) ||
            checks.Any(check => check.Repair is not null || !ReferenceEquals(check.Evidence, evidence) || check.Detail.Contains("pfirewall.log", StringComparison.OrdinalIgnoreCase)))
            throw new Exception("Firewall logging configuration lost identity, coherence or repair isolation.");

        foreach (var invalid in new[]
        {
            evidence with { Error = "denied" }, evidence with { Truncated = true }, evidence with { TimedOut = true },
            evidence with { Output = "{\"profiles\":[]}" }, evidence with { Output = json.Replace("\"Private\"", "\"Domain\"", StringComparison.Ordinal) },
            evidence with { Output = json.Replace("\"Allowed\":\"True\"", "\"Allowed\":\"Yes\"", StringComparison.Ordinal) },
            evidence with { Output = json.Replace("\"8192\"", "\"0\"", StringComparison.Ordinal) },
            evidence with { Output = json.Replace("\"HasLogFilePath\":false", "\"HasLogFilePath\":\"False\"", StringComparison.Ordinal) },
            evidence with { Output = json.Replace("\"Blocked\":\"False\"", "\"Blocked\":\"False\",\"Blocked\":\"True\"", StringComparison.Ordinal) }
        })
            if (FirewallLoggingDiagnostics.Interpret(invalid).Single().State != CheckState.Unknown)
                throw new Exception("Invalid firewall logging evidence was accepted.");
        Console.WriteLine("PASS: Windows Firewall logging modes, destination presence, size coherence and incomplete evidence remain read only.");
    }
}
