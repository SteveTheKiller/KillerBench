using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class CaptureChecks
{
    internal static async Task<string> RunAsync(string scratch)
    {
        string cbsPath = Path.Combine(scratch, "CBS.log");
        await File.WriteAllTextAsync(cbsPath, "2026-09-12 12:00:01, Error CBS source missing 0x800f081f");
        string entry = "<Event xmlns=\"http://schemas.microsoft.com/win/2004/08/events/event\"><System><Provider Name=\"Microsoft-Windows-WindowsUpdateClient\"/><EventID>20</EventID><Level>2</Level><TimeCreated SystemTime=\"2026-09-12T12:00:00.123Z\"/><EventRecordID>42</EventRecordID></System><EventData><Data>test</Data></EventData></Event>";
        var events = EventEvidence.FromXml("synthetic", "<Events>" + entry + "</Events>");
        var report = Capture.Analyze(events);
        Require(report.Findings.Single().Evidence.Single().LineNumber == 1 && report.Timeline[0].Timestamp.EndsWith("Z"), "Event identity and UTC retained");
        Require(Capture.Analyze(EventEvidence.FromXml("synthetic", "<Events>" + entry.Replace("<Level>2", "<Level>4") + "</Events>")).Findings.Count == 0, "Informational event does not trigger findings");
        Require(Capture.Analyze(EventEvidence.FromXml("synthetic", "<Events>" + entry.Replace("Microsoft-Windows-WindowsUpdateClient", "Unrelated") + "</Events>")).Findings.Count == 0, "Unrelated provider excluded");
        Require(EventEvidence.FromXml("synthetic", "<Events/>").Evidence!.Lines.Length == 0, "Empty event window remains empty");
        var capped = EventEvidence.FromXml("synthetic", "<Events>" + string.Concat(Enumerable.Repeat(entry, 201)) + "</Events>");
        Require(capped.Status == CollectionStatus.Truncated && capped.Evidence!.Lines.Length == 200 && Capture.Analyze(capped).Findings.Single().Occurrences == 200, "Event cap preserves every complete retained event");
        try { EventEvidence.FromXml("synthetic", "<!DOCTYPE x [<!ENTITY a SYSTEM 'file:///C:/missing'>]><Events>&a;</Events>"); throw new Exception("DTD accepted"); }
        catch (System.Xml.XmlException) { }
        var session = await Capture.CollectAsync([new TextFileCollector(cbsPath), new FixtureCollector("events", _ => Task.FromResult(events)),
            new TextFileCollector(Path.Combine(scratch, "missing.log")), new FixtureCollector("denied", _ => throw new UnauthorizedAccessException())]);
        Require(session.Sources[2].Status == CollectionStatus.Unavailable && session.Sources[3].Status == CollectionStatus.AccessDenied && session.Partial, "Per-source failures preserve completed evidence");
        string path = Path.Combine(scratch, "fixture.kbench");
        await Capture.SaveAsync(session, path);
        var reopened = await Capture.OpenAsync(path);
        Require(Capture.ReportJson(session) == Capture.ReportJson(reopened), "Save/reopen preserves metadata, evidence, findings and references");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var stopped = await Capture.CollectAsync([new TextFileCollector(cbsPath)], cancellationToken: canceled.Token);
        Require(stopped.Sources.Single().Status == CollectionStatus.Canceled, "Pre-canceled scan records skipped source");
        using var middle = new CancellationTokenSource();
        var partial = await Capture.CollectAsync([new TextFileCollector(cbsPath), new FixtureCollector("stop", token => { middle.Cancel(); token.ThrowIfCancellationRequested(); throw new Exception(); }), new TextFileCollector(cbsPath)], cancellationToken: middle.Token);
        Require(partial.Sources[0].Evidence is not null && partial.Sources.Skip(1).All(s => s.Status == CollectionStatus.Canceled), "Stop preserves earlier evidence and marks remaining sources");
        await Capture.SaveAsync(partial, Path.Combine(scratch, "stopped.kbench"));
        var timed = await Capture.CollectAsync([new FixtureCollector("slow", async token => { await Task.Delay(Timeout.Infinite, token); throw new Exception(); })]);
        Require(timed.Sources[0].Status == CollectionStatus.Canceled && timed.Sources[0].Detail.Contains("15-second"), "Collector timeout is recorded");
        string huge = Path.Combine(scratch, "huge.log");
        await File.WriteAllTextAsync(huge, new string('x', TextEvidence.MaximumCharacters + 1));
        Require((await new TextFileCollector(huge).CollectAsync(default)).Status == CollectionStatus.Truncated, "Large source coverage is explicit");
        await Reject("{\"schemaVersion\":999}");
        await Reject(JsonSerializer.Serialize(session with { Sources = [session.Sources[0], session.Sources[0]] }, Capture.JsonOptions));
        await Reject(JsonSerializer.Serialize(session with { Sources = [new("bad", "Text", CollectionStatus.Collected, "", new("bad", [new string('x', TextEvidence.MaximumCharacters + 1)], false))] }, Capture.JsonOptions));
        string before = await File.ReadAllTextAsync(cbsPath);
        try { await Capture.SaveAsync(session, cbsPath); throw new Exception("Evidence overwrite accepted"); }
        catch (IOException) { }
        Require(before == await File.ReadAllTextAsync(cbsPath), "Save protects original source");
        string savedBefore = await File.ReadAllTextAsync(path);
        try { await Capture.SaveAsync(session, path, canceled.Token); throw new Exception("Canceled save accepted"); }
        catch (OperationCanceledException) { }
        Require(savedBefore == await File.ReadAllTextAsync(path), "Canceled save preserves existing capture");
        Console.WriteLine("PASS: collection coverage, cancellation, file bounds, event rules, safe XML, capture round trip, invalid captures and source protection.");
        return path;

        async Task Reject(string json)
        {
            string bad = Path.Combine(scratch, "bad.kbench");
            await File.WriteAllTextAsync(bad, json);
            try { await Capture.OpenAsync(bad); throw new Exception("Invalid capture accepted"); }
            catch (InvalidDataException) { }
        }
    }
    private sealed class FixtureCollector(string name, Func<CancellationToken, Task<CaptureSource>> collect) : IEvidenceCollector
    { public string Name => name; public Task<CaptureSource> CollectAsync(CancellationToken token) => collect(token); }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
