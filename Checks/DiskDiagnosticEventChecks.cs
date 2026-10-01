using KillerBench.Engine;

internal static class DiskDiagnosticEventChecks
{
    private const string Channel = "Microsoft-Windows-DiskDiagnostic/Operational";
    private const string Provider = "Microsoft-Windows-DiskDiagnostic";

    internal static void Run()
    {
        string configuration = $"<channel xmlns='http://schemas.microsoft.com/win/2004/08/events' name='{Channel}' owningPublisher='{Provider}' enabled='true' />";
        string item = $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{Provider}'/><EventID>100</EventID><Level>2</Level><TimeCreated SystemTime='2026-09-15T12:00:00Z'/><EventRecordID>42</EventRecordID></System><EventData><Data Name='Disk'>private-disk-id</Data></EventData></Event>";
        var config = new ToolResult(0, configuration, "", false);
        var history = new ToolResult(0, "<Events>" + item + "</Events>", "", false);
        var check = DiskDiagnosticEvents.Interpret(Channel, config, history);
        Require(check.State == CheckState.Attention && check.Repair is null && check.Evidence == history &&
            check.Detail.Contains("Event 100, level 2: 1 record") && !check.Detail.Contains("private-disk-id") &&
            check.Detail.Contains("Ordinary summaries exclude those values"),
            "Disk diagnostic history groups warning records without exposing payload identity or selecting repair");
        Require(DiskDiagnosticEvents.Interpret(Channel, config, history with { Output = "<Events/>" }).State == CheckState.Information,
            "Empty complete disk diagnostic history remains informational");
        string disabledXml = configuration.Replace("enabled='true'", "enabled='false'");
        Require(DiskDiagnosticEvents.Interpret(Channel, config with { Output = disabledXml }, null).State == CheckState.Information,
            "Disabled disk diagnostic logging remains descriptive");
        foreach (var invalid in new[]
        {
            config with { ExitCode = 5 }, config with { TimedOut = true }, config with { Truncated = true }, config with { Error = "failed" },
            config with { Output = configuration.Replace(Channel, "Other") }, config with { Output = configuration.Replace(Provider, "Other") },
            config with { Output = "<broken" }
        })
            Require(DiskDiagnosticEvents.Interpret(Channel, invalid, null).State == CheckState.Unknown,
                "Unavailable or malformed disk diagnostic channel configuration remains unknown");
        foreach (var invalid in new[]
        {
            history with { ExitCode = 5 }, history with { TimedOut = true }, history with { Truncated = true }, history with { Error = "failed" },
            history with { Output = "<broken" }, history with { Output = history.Output.Replace(Provider, "Other") },
            history with { Output = "<Events>" + item + item + "</Events>" }
        })
            Require(DiskDiagnosticEvents.Interpret(Channel, config, invalid).State == CheckState.Unknown,
                "Unavailable, malformed, foreign or duplicated disk diagnostic history remains unknown");
        var configurationStep = DiskDiagnosticEvents.CreateConfigurationStep(Channel);
        var historyStep = DiskDiagnosticEvents.CreateHistoryStep(Channel);
        string[] expectedConfigurationArguments = ["gl", Channel, "/f:xml"];
        Require(configurationStep.Verification && historyStep.Verification && configurationStep.Tool == "wevtutil.exe" && historyStep.Tool == "wevtutil.exe" &&
            configurationStep.Arguments.SequenceEqual(expectedConfigurationArguments) && historyStep.Arguments.Contains("/c:201") &&
            !historyStep.Arguments.Any(argument => argument.Contains("sl", StringComparison.OrdinalIgnoreCase)),
            "Disk diagnostic event plans read exact channel configuration and bounded history without changing logging");

        var tasks = new[]
        {
            new WorkflowCheck("Disk diagnostic data collector task", CheckState.Information, "fixture"),
            new WorkflowCheck("Disk diagnostic resolver task", CheckState.Information, "fixture")
        };
        var histories = new[]
        {
            new WorkflowCheck("Disk diagnostic operational history", CheckState.Information, "fixture"),
            new WorkflowCheck("Disk diagnostic collector history", CheckState.Information, "fixture"),
            new WorkflowCheck("Disk diagnostic resolver history", CheckState.Information, "fixture")
        };
        Require(DiskDiagnosticCorrelation.Check(tasks, histories).State == CheckState.Information,
            "Complete quiet disk diagnostic evidence remains informational without claiming health");
        Require(DiskDiagnosticCorrelation.Check(tasks, [.. histories.Select((item, index) => index == 1 ? item with { State = CheckState.Attention } : item)]).State == CheckState.Attention,
            "Disk diagnostic correlation carries a review result from exact history");
        Require(DiskDiagnosticCorrelation.Check(tasks, [.. histories.Select((item, index) => index == 2 ? item with { State = CheckState.Unknown } : item)]).State == CheckState.Unknown,
            "Incomplete disk diagnostic evidence takes precedence over review findings");
        Require(DiskDiagnosticCorrelation.Check([.. tasks.Take(1)], histories).State == CheckState.Unknown,
            "Missing exact disk diagnostic source prevents a combined conclusion");
        Console.WriteLine("PASS: exact disk diagnostic channels and task correlation remain bounded, private and read only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
