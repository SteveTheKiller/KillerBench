using KillerBench.Engine;

internal static class DiagnosticPolicyEventChecks
{
    internal static void Run()
    {
        static string Entry(int id, int level = 2, string provider = DiagnosticPolicyEvents.Provider, ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-17T00:00:00Z'/></System><EventData><Data Name='User'>PRIVATE\\User</Data><Data Name='Process'>private.exe</Data><Data Name='Scenario'>private-scenario</Data><Data Name='Session'>42</Data></EventData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(100, record: 1) + Entry(100, record: 2) + Entry(101, 4, record: 3) + "</Events>", "", false);
        var result = DiagnosticPolicyEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && result.Evidence == evidence && result.Detail.Contains("3 records retained; 2 warning") &&
            result.Detail.Contains("Event 100, level 2: 2 records.") && result.Detail.Contains("account, process, scenario") &&
            !result.Detail.Contains("PRIVATE") && !result.Detail.Contains("private.exe") && !result.Detail.Contains("private-scenario") &&
            result.Detail.Contains("No troubleshooter, suggested fix"),
            "Diagnostic Policy history groups failures without copying private scenario payload into the summary");
        Require(DiagnosticPolicyEvents.Interpret(evidence with { Output = "<Events/>" }).State == CheckState.Information,
            "Empty complete Diagnostic Policy history remains informational");
        foreach (var bad in new[] { evidence with { Output = "<Events>" }, evidence with { ExitCode = 5 }, evidence with { Error = "denied" }, evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { Output = "<Events>" + Entry(100, provider: "Other") + "</Events>" }, evidence with { Output = "<Events>" + Entry(100, level: 9) + "</Events>" }, evidence with { Output = "<Events>" + Entry(100) + Entry(100) + "</Events>" }, evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(100, record: (ulong)index))) + "</Events>" } })
            Require(DiagnosticPolicyEvents.Interpret(bad).State == CheckState.Unknown, "Incomplete Diagnostic Policy history remains unknown");
        var many = DiagnosticPolicyEvents.Interpret(evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 10).Select(id => Entry(id, record: (ulong)id))) + "</Events>" });
        Require(many.Detail.Split('\n').Count(line => line.StartsWith("Event ")) == 8 && many.Detail.Contains("2 additional event/level patterns"),
            "Diagnostic Policy summaries remain bounded");
        try { DiagnosticPolicyEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("Diagnostic Policy history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Diagnostic Policy event grouping, payload privacy, incomplete evidence and cancellation; no troubleshooting changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
