using KillerBench.Engine;

internal static class HardwareErrorEventChecks
{
    internal static void Run()
    {
        string record = Event(17, 3, 42, "2026-09-14T10:00:00Z", "<EventData><Data Name='Component'>PCI Express Root Port</Data><Data Name='ErrorSource'>4</Data></EventData>");
        var evidence = new ToolResult(0, "<Events>" + record + "</Events>", "", false);
        var finding = HardwareErrorEvents.Interpret(evidence);
        Require(finding is { State: CheckState.Attention, Repair: null } && finding.Evidence == evidence &&
            finding.Detail.Contains("Event 17 | level 3 | 1 records") && finding.Detail.Contains("Component=PCI Express Root Port"),
            "WHEA history groups exact event identities and retains payload evidence without selecting repair");
        Require(HardwareErrorEvents.Interpret(new(0, "<Events/>", "", false)) is { State: CheckState.Information, Repair: null },
            "Empty complete WHEA history is not a hardware-health verdict");
        Require(!Workflows.SupportsRepair(WorkflowKind.HardwareErrors) && Workflows.Scopes(WorkflowKind.HardwareErrors).Length == 0 &&
            RecipeCatalog.ForWorkflow(WorkflowKind.HardwareErrors).Count == 0,
            "Hardware error history remains read-only through workflow and recipe APIs");
        foreach (var invalid in new[]
        {
            evidence with { ExitCode = 5 }, evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { Error = "fixture failure" },
            evidence with { Output = "broken" }, evidence with { Output = evidence.Output.Replace(HardwareErrorEvents.Provider, "Other") },
            evidence with { Output = evidence.Output.Replace("<EventID>17", "<EventID>0") },
            evidence with { Output = evidence.Output.Replace("<Level>3", "<Level>5") },
            evidence with { Output = "<Events>" + string.Concat(Enumerable.Repeat(record, 201)) + "</Events>" }
        })
            Require(HardwareErrorEvents.Interpret(invalid) is { State: CheckState.Unknown, Repair: null },
                "Failed, malformed, unexpected or capped WHEA evidence remains unknown");
        try { HardwareErrorEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("Hardware history cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: bounded WHEA history, payload evidence, incomplete coverage and read-only workflow behavior.");
    }

    private static string Event(int id, int level, long recordId, string time, string payload) =>
        "<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='" + HardwareErrorEvents.Provider + "'/><EventID>" + id +
        "</EventID><Level>" + level + "</Level><TimeCreated SystemTime='" + time + "'/><EventRecordID>" + recordId + "</EventRecordID></System>" + payload + "</Event>";

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
