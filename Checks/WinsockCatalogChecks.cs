using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class WinsockCatalogChecks
{
    internal static async Task RunAsync()
    {
        object Row(uint catalogId, string provider, int chainLength = 1, int addressFamily = 2, int socketType = 1, int protocol = 6) =>
            new { catalogId, provider, chainLength, addressFamily, socketType, protocol };
        ToolResult Result(object items, bool complete = true, int error = 0) =>
            new(0, JsonSerializer.Serialize(new { complete, error, items }), "", false);
        const string provider1 = "11111111-2222-3333-4444-555555555555";
        const string provider2 = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

        var evidence = Result(new[] { Row(1001, provider1), Row(1002, provider1, 2), Row(1003, provider2, 0, 23, 2, 17) });
        var inventory = WinsockCatalogDiagnostics.Interpret(evidence);
        Require(inventory is { State: CheckState.Information, Repair: null } && ReferenceEquals(inventory.Evidence, evidence) &&
            inventory.Detail.Contains("3 protocol catalog entries", StringComparison.Ordinal) &&
            inventory.Detail.Contains("2 provider identifiers", StringComparison.Ordinal) &&
            inventory.Detail.Contains("1 base, 1 layered and 1 dummy-chain", StringComparison.Ordinal),
            "Complete unique catalog evidence produces a bounded structural inventory");

        foreach (var invalid in new[]
        {
            Result(Array.Empty<object>()), Result(new[] { Row(0, provider1) }),
            Result(new[] { Row(1, provider1), Row(1, provider2) }), Result(new[] { Row(1, Guid.Empty.ToString("D")) }),
            Result(new[] { Row(1, provider1, -1) }), Result(new[] { Row(1, provider1, 8) }),
            Result(new[] { Row(1, provider1, 1, -1) }), Result(new[] { Row(1, provider1, 1, 2, -1) }),
            Result(Enumerable.Range(1, 513).Select(index => Row((uint)index, provider1)).ToArray()),
            Result(new[] { Row(1, provider1) }, false), Result(new[] { Row(1, provider1) }, error: 10022),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            evidence with { ExitCode = 1 }, evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { Error = "fixture" }
        })
            Require(WinsockCatalogDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Empty, duplicate, invalid, excessive, incomplete, failed and malformed catalog evidence remains unknown");

        var providerFailure = await WinsockCatalogDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("Fixture failure"));
        Require(providerFailure is { State: CheckState.Unknown, Evidence.ExitCode: -1 },
            "Provider failures remain retained unknown evidence");
        var missing = await WinsockCatalogDiagnostics.CheckAsync(execute: (_, _) => Task.FromResult<ToolResult>(null!));
        Require(missing.State == CheckState.Unknown, "Missing provider evidence remains unknown");
        RepairStep? planned = null;
        bool bounded = false;
        var collected = await WinsockCatalogDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(evidence);
        });
        string script = planned?.Arguments.LastOrDefault() ?? "";
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && script.Contains("WSAEnumProtocolsW", StringComparison.Ordinal) &&
            !script.Contains("netsh", StringComparison.OrdinalIgnoreCase) && !script.Contains("WSAInstallProvider", StringComparison.Ordinal) &&
            !script.Contains("WSCDeinstallProvider", StringComparison.Ordinal),
            "Native plan uses bounded noninteractive Winsock enumeration without catalog mutation");

        using var stop = new CancellationTokenSource();
        try
        {
            await WinsockCatalogDiagnostics.CheckAsync(stop.Token, (_, _) =>
            {
                stop.Cancel();
                return new TaskCompletionSource<ToolResult>().Task;
            }).WaitAsync(TimeSpan.FromSeconds(2));
            throw new Exception("Winsock catalog collection ignored Stop");
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        Console.WriteLine("PASS: bounded Winsock protocol catalog structure, native planning, incomplete evidence and cancellation; fixtures only.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
