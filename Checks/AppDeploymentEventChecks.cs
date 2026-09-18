using KillerBench.Engine;

internal static class AppDeploymentEventChecks
{
    internal static void Run()
    {
        string Entry(int id, int level = 2, string provider = "Microsoft-Windows-AppXDeployment-Server", ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><EventData><Data Name='PackageFullName'>Fixture.Package_1.0.0.0_x64__fixture</Data><Data Name='ActivityId'>fixture-activity</Data></EventData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(404, record: 1) + Entry(404, record: 2) + Entry(10002, level: 4, record: 3) + "</Events>", "", false);
        var result = AppDeploymentEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && result.Evidence == evidence &&
            result.Detail.Contains("3 records retained; 2 warning") && result.Detail.Contains("Event 404, level 2: 2 records.") &&
            result.Detail.Contains("Fixture.Package") && result.Detail.Contains("No package install, removal, reset, re-registration"),
            "AppX deployment history retains grouped errors and package context without repair eligibility");
        var empty = AppDeploymentEvents.Interpret(evidence with { Output = "<Events/>" });
        Require(empty.State == CheckState.Information && empty.Repair is null && empty.Detail.Contains("0 records retained"),
            "Empty complete AppX history remains informational rather than a health verdict");
        foreach (var bad in new[]
        {
            evidence with { Output = "<Events>" }, evidence with { ExitCode = 5 }, evidence with { Error = "Access denied" },
            evidence with { TimedOut = true }, evidence with { Truncated = true },
            evidence with { Output = "<Events>" + Entry(404, provider: "Other") + "</Events>" },
            evidence with { Output = "<Events>" + Entry(404, level: 9) + "</Events>" },
            evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(404, record: (ulong)index))) + "</Events>" }
        })
        {
            var unknown = AppDeploymentEvents.Interpret(bad);
            Require(unknown.State == CheckState.Unknown && unknown.Repair is null && unknown.Evidence == bad,
                "Incomplete AppX deployment history cannot establish package state or repair eligibility");
        }
        var many = AppDeploymentEvents.Interpret(evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 10).Select(id => Entry(id, record: (ulong)id))) + "</Events>" });
        Require(many.Detail.Split('\n').Count(line => line.StartsWith("Event ")) == 8 && many.Detail.Contains("2 additional event/level patterns"),
            "AppX deployment summaries retain bounded pattern output");
        try { AppDeploymentEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("AppX history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: AppX deployment event grouping, payload retention, incomplete evidence and cancellation; no package changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
