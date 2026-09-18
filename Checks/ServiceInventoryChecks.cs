using KillerBench.Engine;

internal static class ServiceInventoryChecks
{
    public static void Run()
    {
        const string json = "{\"complete\":true,\"services\":[{\"name\":\"Spooler\",\"displayName\":\"Print Spooler\",\"state\":\"Running\",\"startMode\":\"Auto\",\"delayedAutoStart\":false,\"serviceType\":\"Own Process\"},{\"name\":\"ExampleSvc\",\"displayName\":\"Example Service\",\"state\":\"Stopped\",\"startMode\":\"Disabled\",\"delayedAutoStart\":false,\"serviceType\":\"Share Process\"}]}";
        var evidence = new ToolResult(0, json, "", false);
        var check = ServiceInventoryDiagnostics.Interpret(evidence);
        var snapshot = ServiceInventoryDiagnostics.Parse(json);
        Require(check.State == CheckState.Information && check.Repair is null && check.Evidence == evidence && snapshot.Services.Count == 2 &&
            check.Detail.Contains("1 running, 1 stopped", StringComparison.Ordinal) && check.Detail.Contains("1 disabled", StringComparison.Ordinal),
            "Bounded service inventory reports current state and startup counts without repair");
        Require(!evidence.Output.Contains("StartName", StringComparison.Ordinal) && !evidence.Output.Contains("PathName", StringComparison.Ordinal),
            "Service inventory schema excludes accounts and executable paths");
        int invalidIndex = 0;
        foreach (string invalid in new[]
        {
            "{}", json.Replace("\"complete\":true", "\"complete\":false"), json.Replace("\"Running\"", "\"Started\""),
            json.Replace("\"Spooler\"", "\"Bad/Name\""), json.Replace("\"ExampleSvc\"", "\"Spooler\""),
            json.Replace("\"serviceType\":\"Own Process\"", "\"serviceType\":\"Own Process\",\"extra\":1")
        })
        {
            invalidIndex++;
            Require(ServiceInventoryDiagnostics.Interpret(evidence with { Output = invalid }).State == CheckState.Unknown,
                "Incomplete, malformed, duplicate and unsupported service inventory remains unknown at fixture " + invalidIndex);
        }
        foreach (var failed in new[] { evidence with { Error = "failed" }, evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { ExitCode = 5 } })
            Require(ServiceInventoryDiagnostics.Interpret(failed).State == CheckState.Unknown, "Failed service collection remains unknown");
        Console.WriteLine("PASS: bounded installed-service inventory, privacy exclusions and incomplete evidence; no service changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
