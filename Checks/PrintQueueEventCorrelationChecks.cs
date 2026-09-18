using KillerBench.Engine;

internal static class PrintQueueEventCorrelationChecks
{
    internal static void Run()
    {
        const string queueJson = "{\"queues\":[{\"Name\":\"Office Queue\",\"DriverName\":\"Fixture Driver\",\"PortName\":\"IP_1\"}],\"drivers\":[{\"Name\":\"Fixture Driver\",\"PrinterEnvironment\":\"Windows x64\"}]}";
        var queueEvidence = new ToolResult(0, queueJson, "", false);
        var queues = PrinterDriverDiagnostics.Interpret(queueEvidence);
        string Event(string level, ushort id, ulong record, params string[] values) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='Microsoft-Windows-PrintService'/><EventID>{id}</EventID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T12:00:00Z'/><EventRecordID>{record}</EventRecordID></System><EventData>{string.Join("", values.Select(value => "<Data>" + value + "</Data>"))}</EventData></Event>";
        WorkflowCheck History(string body, CheckState state = CheckState.Information) => new("Recent print-service events", state, "fixture",
            Evidence: new(0, "<Events>" + body + "</Events>", "", false));

        var matched = PrintQueueEventCorrelation.Check(queues, History(Event("2", 372, 10, "Office Queue", "fixture error")));
        Require(matched.State == CheckState.Attention && matched.Detail.Contains("1 matched one exact current queue") &&
            matched.Detail.Contains("Office Queue") && matched.Repair is null, "Exact current queue was not correlated");
        var unresolved = PrintQueueEventCorrelation.Check(queues, History(Event("3", 808, 11, "Removed Queue")));
        Require(unresolved.State == CheckState.Attention && unresolved.Detail.Contains("1 remained unresolved"),
            "Unknown queue identity disappeared from retained history");
        const string twoQueueJson = "{\"queues\":[{\"Name\":\"Office Queue\",\"DriverName\":\"Fixture Driver\",\"PortName\":\"IP_1\"},{\"Name\":\"Second Queue\",\"DriverName\":\"Fixture Driver\",\"PortName\":\"IP_2\"}],\"drivers\":[{\"Name\":\"Fixture Driver\",\"PrinterEnvironment\":\"Windows x64\"}]}";
        var ambiguous = PrintQueueEventCorrelation.Check(PrinterDriverDiagnostics.Interpret(new(0, twoQueueJson, "", false)),
            History(Event("2", 372, 12, "Office Queue", "Second Queue")));
        Require(ambiguous.State == CheckState.Attention && ambiguous.Detail.Contains("remained unresolved"),
            "Event naming multiple queues was guessed");
        Require(PrintQueueEventCorrelation.Check(queues, History("", CheckState.Unknown)).State == CheckState.Unknown,
            "Incomplete history established correlation");
        Require(PrintQueueEventCorrelation.Check([queues[0] with { State = CheckState.Unknown }], History("")).State == CheckState.Unknown,
            "Incomplete queue inventory established correlation");
        Require(PrintQueueEventCorrelation.Check(queues, History("<broken")).State == CheckState.Unknown,
            "Malformed XML established correlation");
        var empty = PrintQueueEventCorrelation.Check(queues, History(Event("4", 307, 13, "Office Queue")));
        Require(empty.State == CheckState.Information && empty.Detail.Contains("No warning, error or critical"),
            "Informational print history invented a problem");
        try { PrintQueueEventCorrelation.Check(queues, History(""), new CancellationToken(true)); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: exact print-queue event correlation, unresolved identities and incomplete evidence; no print changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
