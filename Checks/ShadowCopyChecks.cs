using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class ShadowCopyChecks
{
    internal static async Task RunAsync()
    {
        DateTimeOffset now = new(2026, 9, 16, 12, 0, 0, TimeSpan.Zero);
        object Row(string id, string createdUtc, uint state = 9, string status = "OK", bool persistent = true,
            bool clientAccessible = true, bool noAutoRelease = true, bool noWriters = false, bool hardwareAssisted = false,
            bool differential = true, bool imported = false, bool exposedLocally = false, bool exposedRemotely = false) =>
            new { id, createdUtc, state, status, persistent, clientAccessible, noAutoRelease, noWriters, hardwareAssisted, differential, imported, exposedLocally, exposedRemotely };
        ToolResult Result(object items, bool complete = true) => new(0, JsonSerializer.Serialize(new { complete, items }), "", false);
        string id1 = "11111111-2222-3333-4444-555555555555";
        string id2 = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";
        var ordinaryEvidence = Result(new[] { Row(id1, now.AddHours(-2).ToString("o")), Row(id2, now.AddHours(-1).ToString("o"), hardwareAssisted: true) });
        var ordinary = ShadowCopyDiagnostics.Interpret(ordinaryEvidence, now);
        Require(ordinary is { State: CheckState.Information, Repair: null } && ordinary.Detail.Contains("2 created", StringComparison.Ordinal) &&
            ordinary.Detail.Contains("1 hardware-assisted", StringComparison.Ordinal) && !ordinary.Detail.Contains(id1, StringComparison.OrdinalIgnoreCase),
            "Created shadow copies are summarized without snapshot identities");
        var transitional = ShadowCopyDiagnostics.Interpret(Result(new[] { Row(id1, now.AddMinutes(-1).ToString("o"), state: 6) }), now);
        Require(transitional.State == CheckState.Information && transitional.Detail.Contains("can be normal", StringComparison.Ordinal),
            "Transitional state remains informational with later-correlation guidance");
        var adverse = ShadowCopyDiagnostics.Interpret(Result(new[] { Row(id1, now.AddHours(-1).ToString("o"), state: 10, status: "Error") }), now);
        Require(adverse.State == CheckState.Attention && adverse.Detail.Contains("1 aborted", StringComparison.Ordinal) && adverse.Repair is null,
            "Aborted or adverse status requests review without deletion or recreation");
        Require(ShadowCopyDiagnostics.Interpret(Result(Array.Empty<object>()), now).State == CheckState.Information,
            "Empty inventory does not claim missing protection or health");
        foreach (var invalid in new[]
        {
            Result(new[] { Row("not-guid", now.ToString("o")) }), Result(new[] { Row(id1, now.AddMinutes(10).ToString("o")) }),
            Result(new[] { Row(id1, now.ToString("o"), state: 12) }), Result(new[] { Row(id1, now.ToString("o")), Row(id1, now.ToString("o")) }),
            Result(Enumerable.Range(0, 513).Select(_ => Row(Guid.NewGuid().ToString(), now.ToString("o"))).ToArray()),
            Result(Array.Empty<object>(), false), new ToolResult(0, "{}", "", false), ordinaryEvidence with { ExitCode = 5 },
            ordinaryEvidence with { TimedOut = true }, ordinaryEvidence with { Truncated = true }, ordinaryEvidence with { Error = "denied" }
        }) Require(ShadowCopyDiagnostics.Interpret(invalid, now).State == CheckState.Unknown, "Malformed, duplicate, excessive, incomplete and failed evidence remains unknown");
        var failure = await ShadowCopyDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("Fixture failure"));
        Require(failure is { State: CheckState.Unknown, Evidence.ExitCode: -1 }, "Provider failure remains bounded evidence");
        RepairStep? planned = null;
        await ShadowCopyDiagnostics.CheckAsync(execute: (step, _) => { planned = step; return Task.FromResult(ordinaryEvidence); });
        Require(planned is { Verification: true } && planned.Arguments.Last().Contains("Win32_ShadowCopy", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains(".Delete", StringComparison.OrdinalIgnoreCase) && !planned.Arguments.Last().Contains("::Create", StringComparison.OrdinalIgnoreCase),
            "Native plan is a read-only bounded shadow copy query");
        Console.WriteLine("PASS: current shadow copy states, privacy, bounds and incomplete evidence; fixtures only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
