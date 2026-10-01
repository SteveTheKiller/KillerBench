using System.Collections.Concurrent;
using KillerBench.Engine;

internal static class PrintServiceOperationalEventChecks
{
    internal static async Task RunAsync()
    {
        const string disabledXml = "<channel xmlns='http://schemas.microsoft.com/win/2004/08/events' name='Microsoft-Windows-PrintService/Operational' enabled='false' type='Operational' owningPublisher='Microsoft-Windows-PrintService'><logging/></channel>";
        const string enabledXml = "<channel xmlns='http://schemas.microsoft.com/win/2004/08/events' name='Microsoft-Windows-PrintService/Operational' enabled='true' type='Operational' owningPublisher='Microsoft-Windows-PrintService'><logging/></channel>";
        string Event(int id, int level = 2, ulong record = 1, string provider = "Microsoft-Windows-PrintService") =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-15T00:00:00Z'/></System><EventData><Data Name='DocumentName'>PRIVATE-DOCUMENT</Data><Data Name='User'>PRIVATE-USER</Data><Data Name='PrinterName'>PRIVATE-PRINTER</Data><Data Name='Port'>PRIVATE-PORT</Data></EventData></Event>";
        ToolResult Configuration(string xml) => new(0, xml, "", false);

        var disabled = PrintServiceOperationalEvents.Interpret(Configuration(disabledXml), null);
        Require(disabled is { State: CheckState.Information, Repair: null } && disabled.Detail.Contains("logging is disabled") &&
            disabled.Detail.Contains("not itself a printing fault"), "Disabled Operational logging is explicit without becoming a fault or repair");

        var evidence = new ToolResult(0, "<Events>" + Event(372, record: 1) + Event(372, record: 2) + Event(307, 4, record: 3) + "</Events>", "", false);
        var problems = PrintServiceOperationalEvents.Interpret(Configuration(enabledXml), evidence);
        Require(problems.State == CheckState.Attention && problems.Repair is null && ReferenceEquals(problems.Evidence, evidence) &&
            problems.Detail.Contains("3 records retained; 2 warning") && problems.Detail.Contains("Event 372, level 2: 2 records") &&
            problems.Detail.Contains("Ordinary summaries exclude those values") && !problems.Detail.Contains("PRIVATE-"),
            "Operational failures are grouped without copying job or printer identities into summaries");
        Require(PrintServiceOperationalEvents.Interpret(Configuration(enabledXml), evidence with { Output = "<Events/>" }).State == CheckState.Information,
            "Empty enabled Operational history remains informational");

        foreach (var invalid in new[]
        {
            Configuration(enabledXml) with { ExitCode = 5 }, Configuration(enabledXml) with { Error = "denied" },
            Configuration(enabledXml) with { TimedOut = true }, Configuration(enabledXml) with { Truncated = true },
            Configuration(enabledXml.Replace("Microsoft-Windows-PrintService", "Other")), Configuration("<channel>"),
            Configuration(enabledXml.Replace("enabled='true'", "enabled='maybe'"))
        })
            Require(PrintServiceOperationalEvents.Interpret(invalid, evidence).State == CheckState.Unknown,
                "Incomplete or unexpected channel configuration remains unknown");
        foreach (var invalid in new[]
        {
            evidence with { ExitCode = 5 }, evidence with { Error = "denied" }, evidence with { TimedOut = true },
            evidence with { Truncated = true }, evidence with { Output = "<Events>" },
            evidence with { Output = "<Events>" + Event(372, provider: "Other") + "</Events>" },
            evidence with { Output = "<Events>" + Event(372) + Event(372) + "</Events>" },
            evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Event(372, record: (ulong)index))) + "</Events>" }
        })
            Require(PrintServiceOperationalEvents.Interpret(Configuration(enabledXml), invalid).State == CheckState.Unknown,
                "Incomplete Operational history remains unknown");

        var disabledPlans = new ConcurrentBag<RepairStep>();
        var disabledCollected = await PrintServiceOperationalEvents.CheckAsync(execute: (step, token) =>
        {
            Require(token.CanBeCanceled, "Operational provider receives a cancelable token");
            disabledPlans.Add(step);
            return Task.FromResult(Configuration(disabledXml));
        });
        Require(disabledCollected.State == CheckState.Information && disabledPlans.Single() is { Tool: "wevtutil.exe", Verification: true } configurationStep &&
            configurationStep.Arguments.SequenceEqual(["gl", PrintServiceOperationalEvents.Channel, "/f:xml"]),
            "Disabled channel collection stops after one read-only configuration query");

        var enabledPlans = new ConcurrentBag<RepairStep>();
        var enabledCollected = await PrintServiceOperationalEvents.CheckAsync(execute: (step, token) =>
        {
            enabledPlans.Add(step);
            return Task.FromResult(step.Arguments[0] == "gl" ? Configuration(enabledXml) : evidence with { Output = "<Events/>" });
        });
        Require(enabledCollected.State == CheckState.Information && enabledPlans.Count == 2 && enabledPlans.All(step => step.Verification) &&
            enabledPlans.Single(step => step.Arguments[0] == "qe").Arguments.Contains("/c:201") &&
            enabledPlans.Single(step => step.Arguments[0] == "qe").Arguments.Contains("/rd:true"),
            "Enabled channel collection uses one bounded newest-first history query");

        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await PrintServiceOperationalEvents.CheckAsync(stop.Token, (_, _) => throw new Exception("Provider must not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents Operational collection");
        Console.WriteLine("PASS: PrintService Operational channel state, event grouping, summary privacy, isolation and cancellation; no print changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
