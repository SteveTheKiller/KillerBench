using KillerBench.Engine;

internal static class WinRmEventChecks
{
    internal static void Run()
    {
        string Entry(int id, int level = 2, string provider = "Microsoft-Windows-WinRM", ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><EventData><Data Name='user'>PRIVATE\\User</Data><Data Name='endpoint'>server.private</Data><Data Name='operationName'>Get</Data></EventData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(142, record: 1) + Entry(142, record: 2) + Entry(145, level: 4, record: 3) + "</Events>", "", false);
        var result = WinRmEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && result.Evidence == evidence && result.Detail.Contains("3 records retained; 2 warning") &&
            result.Detail.Contains("Event 142, level 2: 2 records.") && result.Detail.Contains("account, process, endpoint") &&
            !result.Detail.Contains("PRIVATE") && !result.Detail.Contains("server.private") && result.Detail.Contains("No listener, firewall, trust, certificate"),
            "WinRM history groups failures without copying account or endpoint payload into the summary");
        Require(WinRmEvents.Interpret(evidence with { Output = "<Events/>" }).State == CheckState.Information, "Empty complete WinRM history remains informational");
        foreach (var bad in new[] { evidence with { Output = "<Events>" }, evidence with { ExitCode = 5 }, evidence with { Error = "denied" }, evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { Output = "<Events>" + Entry(142, provider: "Other") + "</Events>" }, evidence with { Output = "<Events>" + Entry(142, level: 9) + "</Events>" }, evidence with { Output = "<Events>" + Entry(142) + Entry(142) + "</Events>" }, evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(142, record: (ulong)index))) + "</Events>" } })
            Require(WinRmEvents.Interpret(bad).State == CheckState.Unknown, "Incomplete WinRM history remains unknown");
        var many = WinRmEvents.Interpret(evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 10).Select(id => Entry(id, record: (ulong)id))) + "</Events>" });
        Require(many.Detail.Split('\n').Count(line => line.StartsWith("Event ")) == 8 && many.Detail.Contains("2 additional event/level patterns"), "WinRM summaries remain bounded");
        try { WinRmEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("WinRM history ignored cancellation"); } catch (OperationCanceledException) { }
        Console.WriteLine("PASS: WinRM event grouping, endpoint privacy, incomplete evidence and cancellation; no remote-management changes.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
