using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class VssProviderChecks
{
    internal static async Task RunAsync()
    {
        object Row(string id, string name, uint type, string version, string versionId) => new { id, name, type, version, versionId };
        ToolResult Result(object items, bool complete = true) => new(0, JsonSerializer.Serialize(new { complete, items }), "", false);
        string id1 = "b5946137-7b9f-4925-af80-51abd60b20d5";
        string id2 = "11111111-2222-3333-4444-555555555555";
        string version1 = "00000001-0000-0000-0007-000000000001";
        string version2 = "00000002-0000-0000-0001-000000000005";

        var ordinaryEvidence = Result(new[]
        {
            Row(id1, "Microsoft Software Shadow Copy provider 1.0", 1, "1.0.0.7", version1),
            Row(id2, "Storage vendor provider", 3, "2.1.5", version2)
        });
        var ordinary = VssProviderDiagnostics.Interpret(ordinaryEvidence);
        Require(ordinary is { State: CheckState.Information, Repair: null } && ReferenceEquals(ordinary.Evidence, ordinaryEvidence) &&
            ordinary.Detail.Contains("1 system, 0 software, 1 hardware", StringComparison.Ordinal) &&
            !ordinary.Detail.Contains(id1, StringComparison.OrdinalIgnoreCase) && !ordinary.Detail.Contains(version1, StringComparison.OrdinalIgnoreCase),
            "Recognized VSS providers are summarized without provider identifiers");
        var missingSystem = VssProviderDiagnostics.Interpret(Result(new[] { Row(id2, "Storage vendor provider", 2, "2.1.5", version2) }));
        Require(missingSystem.State == CheckState.Attention && missingSystem.Detail.Contains("system provider was not reported", StringComparison.OrdinalIgnoreCase),
            "Complete inventory without the Windows system provider requests review");
        var futureType = VssProviderDiagnostics.Interpret(Result(new[]
        {
            Row(id1, "Microsoft Software Shadow Copy provider 1.0", 1, "1.0.0.7", version1),
            Row(id2, "Future provider", 9, "1.0", version2)
        }));
        Require(futureType.State == CheckState.Attention && futureType.Detail.Contains("1 unknown type", StringComparison.Ordinal),
            "Unknown future provider types request review without selecting a repair");
        var empty = VssProviderDiagnostics.Interpret(Result(Array.Empty<object>()));
        Require(empty.State == CheckState.Attention && empty.Repair is null, "Empty complete provider inventory requests review without repair");

        foreach (var invalid in new[]
        {
            Result(new[] { Row("not-guid", "Provider", 1, "1.0", version1) }),
            Result(new[] { Row(id1, "Provider", 1, "1.0", "not-guid") }),
            Result(new[] { Row(id1, "Provider", 1, "1.0", version1), Row(id1, "Duplicate", 2, "2.0", version2) }),
            Result(Enumerable.Range(0, 65).Select(index => Row(Guid.NewGuid().ToString(), "Provider " + index, 2, "1.0", Guid.NewGuid().ToString())).ToArray()),
            Result(Array.Empty<object>(), false), new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            ordinaryEvidence with { ExitCode = 5 }, ordinaryEvidence with { TimedOut = true }, ordinaryEvidence with { Truncated = true }, ordinaryEvidence with { Error = "denied" }
        })
            Require(VssProviderDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Malformed, duplicate, excessive, incomplete and failed provider evidence remains unknown");

        var failure = await VssProviderDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("Fixture provider failure"));
        Require(failure is { State: CheckState.Unknown, Evidence.ExitCode: -1 } && failure.Evidence!.Error == "Fixture provider failure",
            "Provider failures remain bounded evidence");
        RepairStep? planned = null;
        var plannedResult = await VssProviderDiagnostics.CheckAsync(execute: (step, _) =>
        {
            planned = step;
            return Task.FromResult(ordinaryEvidence);
        });
        Require(plannedResult.State == CheckState.Information && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && planned.Arguments.Last().Contains("Win32_ShadowProvider", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Register", StringComparison.OrdinalIgnoreCase) &&
            !planned.Arguments.Last().Contains("Delete", StringComparison.OrdinalIgnoreCase),
            "Native plan is a noninteractive read-only VSS provider query");
        using var stop = new CancellationTokenSource();
        try
        {
            await VssProviderDiagnostics.CheckAsync(stop.Token, (_, _) =>
            {
                stop.Cancel();
                return new TaskCompletionSource<ToolResult>().Task;
            }).WaitAsync(TimeSpan.FromSeconds(2));
            throw new Exception("VSS provider collection ignored Stop");
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        Console.WriteLine("PASS: VSS provider types, identifiers, bounds, incomplete evidence and cancellation; fixtures only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
