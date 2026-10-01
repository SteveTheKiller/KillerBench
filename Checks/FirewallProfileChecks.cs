using KillerBench.Engine;

internal static class FirewallProfileChecks
{
    internal static void Run()
    {
        const string json = "{\"profiles\":[{\"Name\":\"Domain\",\"Enabled\":\"True\"},{\"Name\":\"Private\",\"Enabled\":\"False\"},{\"Name\":\"Public\",\"Enabled\":\"NotConfigured\"}]}";
        var raw = new ToolResult(0, json, "", false);
        var checks = FirewallProfiles.Interpret(raw);
        if (!checks.Select(c => c.State).SequenceEqual([CheckState.Information, CheckState.Attention, CheckState.Unknown]) ||
            checks.Any(c => c.Repair is not null || !ReferenceEquals(c.Evidence, raw)))
            throw new Exception("Firewall settings lost scope, evidence or non-mutating classification.");
        foreach (var invalid in new[] { raw with { Error = "partial" }, raw with { Truncated = true }, raw with { TimedOut = true },
            raw with { Output = "{\"profiles\":[]}" }, raw with { Output = json.Replace("Private", "Domain") },
            raw with { Output = json.Replace("\"Enabled\":\"True\"", "\"Enabled\":\"True\",\"Enabled\":\"False\"") },
            raw with { Output = json.Replace("\"True\"", "true") }, raw with { Output = json.Replace("Public", "Other") } })
            if (FirewallProfiles.Interpret(invalid).Single().State != CheckState.Unknown)
                throw new Exception("Invalid firewall profiles were accepted.");
        Console.WriteLine("PASS: firewall profile identities, unresolved settings and incomplete evidence; no configuration changed.");
    }
}
