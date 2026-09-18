using KillerBench.Engine;

internal static class PrinterEndpointChecks
{
    public static void Run()
    {
        ToolResult Evidence(string queues, string ports, string complete = "true") => new(0,
            $"{{\"complete\":{complete},\"queues\":{queues},\"ports\":{ports}}}", "", false);
        const string endpoint = "[{\"name\":\"IP_Office\",\"host\":\"192.0.2.10\",\"port\":9100,\"protocol\":1,\"snmpEnabled\":true}]";
        var valid = Evidence("[{\"name\":\"Office\",\"portName\":\"IP_Office\",\"type\":0}]", endpoint);
        var check = PrinterEndpointDiagnostics.Interpret(valid).Single();
        Require(check.State == CheckState.Information && check.Repair is null && check.Evidence == valid &&
            check.Detail.Contains("192.0.2.10:9100, protocol RAW", StringComparison.Ordinal) && check.Detail.Contains("without resolving names or connecting", StringComparison.Ordinal),
            "Local queue maps to syntactically valid TCP/IP endpoint without network contact");
        var lpr = valid with { Output = valid.Output.Replace("192.0.2.10", "printer.example.test").Replace("9100", "515").Replace("\"protocol\":1", "\"protocol\":2") };
        Require(PrinterEndpointDiagnostics.Interpret(lpr).Single().Detail.Contains("protocol LPR", StringComparison.Ordinal),
            "LPR endpoint and DNS hostname remain distinguishable");
        foreach (var invalidEndpoint in new[]
        {
            endpoint.Replace("192.0.2.10", "https://printer"), endpoint.Replace("9100", "0"), endpoint.Replace("\"protocol\":1", "\"protocol\":9")
        }) Require(PrinterEndpointDiagnostics.Interpret(Evidence("[{\"name\":\"Office\",\"portName\":\"IP_Office\",\"type\":0}]", invalidEndpoint)).Single().State == CheckState.Attention,
            "Assigned endpoint with invalid host, port or protocol requests review without repair");
        var nonTcp = PrinterEndpointDiagnostics.Interpret(Evidence("[{\"name\":\"USB printer\",\"portName\":\"USB001\",\"type\":0}]", endpoint)).Single();
        Require(nonTcp.State == CheckState.Information && nonTcp.Detail.Contains("USB, WSD, file and vendor", StringComparison.Ordinal),
            "Non-TCP local port remains explicit rather than being called missing");
        var remote = PrinterEndpointDiagnostics.Interpret(Evidence("[{\"name\":\"Shared\",\"portName\":\"server\",\"type\":1}]", endpoint)).Single();
        Require(remote.State == CheckState.Information && remote.Detail.Contains("belongs to its print server", StringComparison.Ordinal),
            "Remote queue endpoint remains outside local configuration claims");
        var pooled = PrinterEndpointDiagnostics.Interpret(Evidence("[{\"name\":\"Pool\",\"portName\":\"IP_Office,IP_Backup\",\"type\":0}]", endpoint)).Single();
        Require(pooled.State == CheckState.Unknown && pooled.Repair is null, "Pooled endpoint selection is not guessed");
        foreach (var invalid in new[]
        {
            valid with { ExitCode = 5 }, valid with { Error = "denied" }, valid with { TimedOut = true }, valid with { Truncated = true },
            Evidence("[]", "[]", "false"), Evidence("[{}]", endpoint),
            Evidence("[]", endpoint.Replace("}]", "},{\"name\":\"ip_office\",\"host\":\"192.0.2.11\",\"port\":9100,\"protocol\":1,\"snmpEnabled\":false}]")),
            valid with { Output = valid.Output.Replace("\"snmpEnabled\":true", "\"snmpEnabled\":true,\"community\":\"private\"") }
        }) Require(PrinterEndpointDiagnostics.Interpret(invalid).Single().State == CheckState.Unknown,
            "Failed, incomplete, malformed, duplicate and privacy-expanding endpoint evidence remains unknown");
        var empty = PrinterEndpointDiagnostics.Interpret(Evidence("[]", endpoint)).Single();
        Require(empty.State == CheckState.Information && empty.Detail.Contains("1 registered TCP/IP port remains", StringComparison.Ordinal),
            "Unassigned endpoint inventory does not become a queue fault");
        Console.WriteLine("PASS: queue-to-TCP/IP endpoint mapping, syntax checks, privacy exclusions and incomplete evidence; no network or printer changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
