using KillerBench.Engine;

internal static class SearchEventChecks
{
    internal static void Run()
    {
        string Entry(int id, string provider = "Microsoft-Windows-Search", int level = 2) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>1</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-13T00:00:00Z'/></System><EventData><Data Name='Source'>fixture-source</Data></EventData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(3036) + Entry(3036) + Entry(1, level: 4) + "</Events>", "", false);
        var result = SearchEvents.Interpret(evidence);
        if (result.State != CheckState.Information || result.Repair is not null || result.Evidence != evidence ||
            !result.Detail.Contains("3 records retained; 2 warning") || !result.Detail.Contains("Event 3036, level 2: 2 records.") ||
            !result.Detail.Contains("fixture-source") || !result.Detail.Contains("does not establish current index corruption"))
            throw new Exception("Search history lost counts, payload or diagnostic limits");
        foreach (var bad in new[] { evidence with { Output = "<Events>" }, evidence with { ExitCode = 5 }, evidence with { TimedOut = true },
            evidence with { Truncated = true }, evidence with { Error = "Access denied" },
            evidence with { Output = "<Events>" + Entry(3036, "Other") + "</Events>" },
            evidence with { Output = "<Events>" + Entry(3036, level: 9) + "</Events>" },
            evidence with { Output = "<Events>" + string.Concat(Enumerable.Repeat(Entry(3036), 201)) + "</Events>" } })
        {
            var unknown = SearchEvents.Interpret(bad);
            if (unknown.State != CheckState.Unknown || unknown.Repair is not null || unknown.Evidence != bad)
                throw new Exception("Incomplete Search history claimed a diagnosis");
        }
        var many = SearchEvents.Interpret(evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 10).Select(id => Entry(id))) + "</Events>" });
        if (many.Detail.Split('\n').Count(line => line.StartsWith("Event ")) != 8 || !many.Detail.Contains("2 additional event/level patterns"))
            throw new Exception("Search history summary exceeded its pattern bound");
        var empty = SearchEvents.Interpret(evidence with { Output = "<Events/>" });
        if (empty.State != CheckState.Information || empty.Repair is not null || !empty.Detail.Contains("0 records retained"))
            throw new Exception("Empty Search history implied current health");
        try { SearchEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("Search history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Search event grouping, payload retention, incomplete evidence and cancellation; no index changes.");
    }
}
