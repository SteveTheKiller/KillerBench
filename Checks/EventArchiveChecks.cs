using KillerBench.Engine;
using System.IO;

internal static class EventArchiveChecks
{
    internal static void Run()
    {
        string path = Path.Combine(Path.GetTempPath(), "fixture archive.evtx");
        var plan = EventArchive.PlanRead(path);
        if (!plan.Verification || plan.Arguments[0] != "qe" || plan.Arguments[1] != path ||
            !plan.Arguments.Contains("/lf:true") || !plan.Arguments.Contains("/c:201") || plan.Arguments.Any(a => a.StartsWith("/q:")))
            throw new Exception("Archive plan must query the supplied file without a live date filter");
        const string entry = "<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='MsiInstaller'/><EventID>11708</EventID><EventRecordID>1</EventRecordID><Level>2</Level><TimeCreated SystemTime='2020-01-01T00:00:00Z'/></System></Event>";
        var source = EventArchive.FromResult(path, new(0, "<Events>" + entry + "</Events>", "", false));
        var report = EventArchive.Analyze(source);
        if (report.Source != path || report.ParsedLines != 1 || !report.Limitation.Contains("no date filter"))
            throw new Exception("Archive preserves source and historical records with explicit coverage");
        var capped = EventArchive.FromResult(path, new(0, "<Events>" + string.Concat(Enumerable.Repeat(entry, 201)) + "</Events>", "", false));
        if (!capped.Truncated || capped.Lines.Length != 200) throw new Exception("Archive event limit must remain explicit");
        foreach (var bad in new[] { new ToolResult(1, "<Events/>", "failure", false), new(0, "<Events/>", "warning", false),
            new(0, "<Events/>", "", true), new(0, "<Events/>", "", false) { TimedOut = true }, new(0, "not xml", "", false) })
        {
            try { EventArchive.FromResult(path, bad); throw new Exception("Incomplete archive accepted"); }
            catch (InvalidDataException) { }
        }
        Console.WriteLine("PASS: EVTX query plan, historical evidence, source identity, limits and failed collection (synthetic XML only).");
    }
}
