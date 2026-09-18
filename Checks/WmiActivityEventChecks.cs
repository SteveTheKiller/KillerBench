using KillerBench.Engine;

internal static class WmiActivityEventChecks
{
    internal static void Run()
    {
        string Entry(int id, int level = 2, string provider = "Microsoft-Windows-WMI-Activity", ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><UserData><Failure><User>PRIVATE\\User</User><ClientProcessId>4321</ClientProcessId><Operation>SELECT Secret FROM PrivateClass</Operation></Failure></UserData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(5858, record: 1) + Entry(5858, record: 2) + Entry(5857, level: 4, record: 3) + "</Events>", "", false);
        var result = WmiActivityEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && result.Evidence == evidence &&
            result.Detail.Contains("3 records retained; 2 warning") && result.Detail.Contains("Event 5858, level 2: 2 records.") &&
            result.Detail.Contains("account names") && !result.Detail.Contains("PRIVATE") && !result.Detail.Contains("Secret") &&
            result.Detail.Contains("No repository salvage, service start, provider registration or client change is selected"),
            "WMI history groups failures without copying client identities or queries into the summary");
        var empty = WmiActivityEvents.Interpret(evidence with { Output = "<Events/>" });
        Require(empty.State == CheckState.Information && empty.Repair is null && empty.Detail.Contains("0 records retained"),
            "Empty complete WMI activity history remains informational");
        foreach (var bad in new[]
        {
            evidence with { Output = "<Events>" }, evidence with { ExitCode = 5 }, evidence with { Error = "Access denied" },
            evidence with { TimedOut = true }, evidence with { Truncated = true },
            evidence with { Output = "<Events>" + Entry(5858, provider: "Other") + "</Events>" },
            evidence with { Output = "<Events>" + Entry(5858, level: 9) + "</Events>" },
            evidence with { Output = "<Events>" + Entry(5858) + Entry(5858) + "</Events>" },
            evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(5858, record: (ulong)index))) + "</Events>" }
        })
        {
            var unknown = WmiActivityEvents.Interpret(bad);
            Require(unknown.State == CheckState.Unknown && unknown.Repair is null && unknown.Evidence == bad,
                "Incomplete WMI activity history cannot establish current state or repair eligibility");
        }
        var many = WmiActivityEvents.Interpret(evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 10).Select(id => Entry(id, record: (ulong)id))) + "</Events>" });
        Require(many.Detail.Split('\n').Count(line => line.StartsWith("Event ")) == 8 && many.Detail.Contains("2 additional event/level patterns"),
            "WMI activity summaries retain bounded pattern output");
        try { WmiActivityEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("WMI activity history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: WMI activity grouping, client privacy, incomplete evidence and cancellation; no WMI changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
