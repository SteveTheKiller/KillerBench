using KillerBench.Engine;

internal static class DefenderEventChecks
{
    internal static void Run()
    {
        string Entry(int id, int level = 2, string provider = "Microsoft-Windows-Windows Defender", ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><EventData><Data Name='Threat Name'>Private.Sample</Data><Data Name='Path'>C:\\Private\\sample.exe</Data><Data Name='User'>PRIVATE\\User</Data></EventData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(1116, record: 1) + Entry(1116, record: 2) + Entry(1151, level: 4, record: 3) + "</Events>", "", false);
        var result = DefenderEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && result.Evidence == evidence &&
            result.Detail.Contains("3 matching records retained; 2 warning") && result.Detail.Contains("native query excludes informational events") && result.Detail.Contains("Event 1116, level 2: 2 records.") &&
            result.Detail.Contains("threat names") && !result.Detail.Contains("Private.Sample") && !result.Detail.Contains("sample.exe") &&
            result.Detail.Contains("No scan, quarantine action, exclusion change, protection change or signature refresh is selected"),
            "Defender history groups events without copying threat, path or account payload into the summary");
        var empty = DefenderEvents.Interpret(evidence with { Output = "<Events/>" });
        Require(empty.State == CheckState.Information && empty.Repair is null && empty.Detail.Contains("0 matching records retained"),
            "Empty complete Defender history remains informational");
        foreach (var bad in new[]
        {
            evidence with { Output = "<Events>" }, evidence with { ExitCode = 5 }, evidence with { Error = "Access denied" },
            evidence with { TimedOut = true }, evidence with { Truncated = true },
            evidence with { Output = "<Events>" + Entry(1116, provider: "Other") + "</Events>" },
            evidence with { Output = "<Events>" + Entry(1116, level: 9) + "</Events>" },
            evidence with { Output = "<Events>" + Entry(1116) + Entry(1116) + "</Events>" },
            evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(1116, record: (ulong)index))) + "</Events>" }
        })
        {
            var unknown = DefenderEvents.Interpret(bad);
            Require(unknown.State == CheckState.Unknown && unknown.Repair is null && unknown.Evidence == bad,
                "Incomplete Defender history cannot establish protection, threat or repair state");
        }
        var many = DefenderEvents.Interpret(evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 10).Select(id => Entry(id, record: (ulong)id))) + "</Events>" });
        Require(many.Detail.Split('\n').Count(line => line.StartsWith("Event ")) == 8 && many.Detail.Contains("2 additional event/level patterns"),
            "Defender summaries retain bounded pattern output");
        try { DefenderEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("Defender history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Defender event grouping, sensitive payload privacy, incomplete evidence and cancellation; no security changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
