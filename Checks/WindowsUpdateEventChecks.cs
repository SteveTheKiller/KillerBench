using KillerBench.Engine;

internal static class WindowsUpdateEventChecks
{
    internal static void Run()
    {
        string Entry(int id, int level = 2, string provider = WindowsUpdateEvents.Provider, ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><EventData><Data Name='updateTitle'>Private fixture update</Data><Data Name='serviceUrl'>https://private.invalid/update</Data></EventData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(20, record: 1) + Entry(20, record: 2) + Entry(19, level: 4, record: 3) + "</Events>", "", false);
        var result = WindowsUpdateEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && result.Evidence == evidence &&
            result.Detail.Contains("3 records retained; 2 warning") && result.Detail.Contains("Event 20, level 2: 2 records.") &&
            result.Detail.Contains("No scan, download, installation, cache reset") &&
            !result.Detail.Contains("Private fixture update") && !result.Detail.Contains("private.invalid"),
            "Windows Update history groups problem records without exposing payload or selecting repair");
        var empty = WindowsUpdateEvents.Interpret(evidence with { Output = "<Events/>" });
        Require(empty.State == CheckState.Information && empty.Repair is null && empty.Detail.Contains("0 records retained"),
            "Empty complete Windows Update Client history remains informational rather than a health verdict");
        foreach (var bad in new[]
        {
            evidence with { Output = "<Events>" }, evidence with { ExitCode = 5 }, evidence with { Error = "Access denied" },
            evidence with { TimedOut = true }, evidence with { Truncated = true },
            evidence with { Output = "<Events>" + Entry(20, provider: "Other") + "</Events>" },
            evidence with { Output = "<Events>" + Entry(20, level: 9) + "</Events>" },
            evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(20, record: (ulong)index))) + "</Events>" }
        })
        {
            var unknown = WindowsUpdateEvents.Interpret(bad);
            Require(unknown.State == CheckState.Unknown && unknown.Repair is null && unknown.Evidence == bad,
                "Incomplete Windows Update Client history cannot establish update state or repair eligibility");
        }
        var many = WindowsUpdateEvents.Interpret(evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 10).Select(id => Entry(id, record: (ulong)id))) + "</Events>" });
        Require(many.Detail.Split('\n').Count(line => line.StartsWith("Event ")) == 8 && many.Detail.Contains("2 additional event/level patterns"),
            "Windows Update Client summaries retain bounded pattern output");
        try { WindowsUpdateEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("Windows Update Client history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Windows Update Client event grouping, payload privacy, incomplete evidence and cancellation; no update changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
