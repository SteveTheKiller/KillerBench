using KillerBench.Engine;

internal static class StoreInstallEventChecks
{
    internal static void Run()
    {
        string Entry(int id, int level = 2, string provider = "Microsoft-Windows-Install-Agent", ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><EventData><Data Name='Package'>PRIVATE.PACKAGE</Data><Data Name='Uri'>https://private.invalid/content</Data><Data Name='Account'>PRIVATE\\User</Data></EventData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(2001, record: 1) + Entry(2001, record: 2) + Entry(2006, 4, record: 3) + "</Events>", "", false);
        var result = StoreInstallEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && ReferenceEquals(result.Evidence, evidence) &&
            result.Detail.Contains("3 records retained; 2 warning") && result.Detail.Contains("Event 2001, level 2: 2 records") &&
            result.Detail.Contains("package, account, URI") && !result.Detail.Contains("PRIVATE") && !result.Detail.Contains("private.invalid"),
            "Store history groups failures without copying package or account payload into summaries");
        Require(StoreInstallEvents.Interpret(evidence with { Output = "<Events/>" }).State == CheckState.Information,
            "Empty complete Store history remains informational");
        foreach (var invalid in new[] { evidence with { ExitCode = 5 }, evidence with { Error = "denied" }, evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { Output = "<Events>" }, evidence with { Output = "<Events>" + Entry(2001, provider: "Other") + "</Events>" }, evidence with { Output = "<Events>" + Entry(2001) + Entry(2001) + "</Events>" }, evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(2001, record: (ulong)index))) + "</Events>" } })
            Require(StoreInstallEvents.Interpret(invalid).State == CheckState.Unknown, "Incomplete Store installation history remains unknown");
        try { StoreInstallEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("Store history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Store installation event grouping, payload privacy, incomplete evidence and cancellation; no package changes.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
