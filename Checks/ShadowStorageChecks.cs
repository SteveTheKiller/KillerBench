using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class ShadowStorageChecks
{
    internal static async Task RunAsync()
    {
        ToolResult Result(object items, bool complete = true) => new(0,
            JsonSerializer.Serialize(new { complete, items }), "", false);
        object Row(string source, string storage, ulong used, ulong allocated, ulong maximum) =>
            new { source, storage, used, allocated, maximum };

        var clearEvidence = Result(new[] { Row("C:", "D:", 40, 50, 100) });
        var clear = ShadowStorageDiagnostics.Interpret(clearEvidence);
        Require(clear is { State: CheckState.Clear, Repair: null } && ReferenceEquals(clear.Evidence, clearEvidence) &&
            clear.Detail.Contains("0 at or above 90%", StringComparison.Ordinal), "Ordinary shadow storage allocation is clear within collected scope");
        var pressure = ShadowStorageDiagnostics.Interpret(Result(new[] { Row("C:", "C:", 90, 95, 100) }));
        Require(pressure.State == CheckState.Attention && pressure.Detail.Contains("C: uses 90%", StringComparison.Ordinal) && pressure.Repair is null,
            "Ninety-percent used allocation requests review without selecting resize or deletion");
        var below = ShadowStorageDiagnostics.Interpret(Result(new[] { Row("C:", "C:", 899, 900, 1000) }));
        Require(below.State == CheckState.Clear, "Value below exact ninety-percent threshold remains clear");
        var unbounded = ShadowStorageDiagnostics.Interpret(Result(new[] { Row("C:", "C:", 900, 1000, ulong.MaxValue) }));
        Require(unbounded.State == CheckState.Clear && unbounded.Detail.Contains("1 report no finite maximum", StringComparison.Ordinal),
            "Unbounded maximum is distinguished from finite pressure");
        var empty = ShadowStorageDiagnostics.Interpret(Result(Array.Empty<object>()));
        Require(empty.State == CheckState.Information && empty.Detail.Contains("does not establish", StringComparison.Ordinal),
            "Empty association inventory does not claim VSS is healthy or unnecessary");

        foreach (var invalid in new[]
        {
            Result(new[] { Row("C:", "C:", 101, 100, 100) }), Result(new[] { Row("C:", "C:", 50, 40, 100) }),
            Result(new[] { Row("C:", "C:", 0, 0, 0) }), Result(new[] { Row("C:", "C:", 1, 1, 100), Row("c:", "c:", 1, 1, 100) }),
            Result(Enumerable.Range(0, 65).Select(index => Row("V" + index, "S" + index, 1, 1, 10)).ToArray()),
            Result(Array.Empty<object>(), false), new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            clearEvidence with { ExitCode = 5 }, clearEvidence with { TimedOut = true }, clearEvidence with { Truncated = true }, clearEvidence with { Error = "denied" }
        })
            Require(ShadowStorageDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Impossible, duplicate, excessive, incomplete, failed and malformed evidence remains unknown");

        var providerFailure = await ShadowStorageDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("Fixture provider failure"));
        Require(providerFailure is { State: CheckState.Unknown, Evidence.ExitCode: -1 } && providerFailure.Evidence!.Error == "Fixture provider failure",
            "Provider failures retain evidence without interrupting the caller");
        var missing = await ShadowStorageDiagnostics.CheckAsync(execute: (_, _) => Task.FromResult<ToolResult>(null!));
        Require(missing.State == CheckState.Unknown, "Missing provider evidence remains unknown");
        RepairStep? planned = null;
        var plannedResult = await ShadowStorageDiagnostics.CheckAsync(execute: (step, _) =>
        {
            planned = step;
            return Task.FromResult(clearEvidence);
        });
        Require(plannedResult.State == CheckState.Clear && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && planned.Arguments.Last().Contains("Win32_ShadowStorage", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("resize", StringComparison.OrdinalIgnoreCase) &&
            !planned.Arguments.Last().Contains("delete", StringComparison.OrdinalIgnoreCase),
            "Native plan is noninteractive read-only Win32_ShadowStorage collection");
        using var stop = new CancellationTokenSource();
        try
        {
            await ShadowStorageDiagnostics.CheckAsync((_, _) =>
            {
                stop.Cancel();
                return new TaskCompletionSource<ToolResult>().Task;
            }, token: stop.Token).WaitAsync(TimeSpan.FromSeconds(2));
            throw new Exception("Shadow storage collection ignored Stop");
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        Console.WriteLine("PASS: VSS shadow storage allocation, pressure, unbounded limits, incomplete evidence and cancellation; fixtures only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
