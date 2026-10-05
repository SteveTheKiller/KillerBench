using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class WinsockNamespaceChecks
{
    internal static async Task RunAsync()
    {
        object Row(string provider, uint nameSpace = 12, bool active = true, uint version = 1) =>
            new { provider, nameSpace, active, version };
        ToolResult Result(object items, bool complete = true, int error = 0) =>
            new(0, JsonSerializer.Serialize(new { complete, error, items }), "", false);
        const string provider1 = "11111111-2222-3333-4444-555555555555";
        const string provider2 = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

        var evidence = Result(new[] { Row(provider1, version: 0), Row(provider2, 15, false, 2) });
        var inventory = WinsockNamespaceDiagnostics.Interpret(evidence);
        Require(inventory is { State: CheckState.Information, Repair: null } && ReferenceEquals(inventory.Evidence, evidence) &&
            inventory.Detail.Contains("2 unique namespace-provider registrations", StringComparison.Ordinal) &&
            inventory.Detail.Contains("2 namespace types", StringComparison.Ordinal) &&
            inventory.Detail.Contains("1 active and 1 inactive", StringComparison.Ordinal),
            "Complete unique namespace evidence produces a bounded structural inventory");

        foreach (var invalid in new[]
        {
            Result(Array.Empty<object>()), Result(new[] { Row(Guid.Empty.ToString("D")) }),
            Result(new[] { Row(provider1), Row(provider1, 15) }),
            Result(Enumerable.Range(1, 129).Select(index => Row(GuidFrom(index))).ToArray()),
            Result(new[] { Row(provider1) }, false), Result(new[] { Row(provider1) }, error: 10022),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            evidence with { ExitCode = 1 }, evidence with { TimedOut = true }, evidence with { Truncated = true }, evidence with { Error = "fixture" }
        })
            Require(WinsockNamespaceDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Empty, duplicate, invalid, excessive, incomplete, failed and malformed namespace evidence remains unknown");

        var providerFailure = await WinsockNamespaceDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("Fixture failure"));
        Require(providerFailure is { State: CheckState.Unknown, Evidence.ExitCode: -1 },
            "Provider failures remain retained unknown evidence");
        var missing = await WinsockNamespaceDiagnostics.CheckAsync(execute: (_, _) => Task.FromResult<ToolResult>(null!));
        Require(missing.State == CheckState.Unknown, "Missing provider evidence remains unknown");
        RepairStep? planned = null;
        bool bounded = false;
        var collected = await WinsockNamespaceDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(evidence);
        });
        string script = planned?.Arguments.LastOrDefault() ?? "";
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && script.Contains("WSAEnumNameSpaceProvidersW", StringComparison.Ordinal) &&
            !script.Contains("netsh", StringComparison.OrdinalIgnoreCase) && !script.Contains("WSCInstallNameSpace", StringComparison.Ordinal) &&
            !script.Contains("WSCUnInstallNameSpace", StringComparison.Ordinal),
            "Native plan uses bounded noninteractive namespace enumeration without provider mutation");

        using var stop = new CancellationTokenSource();
        try
        {
            await WinsockNamespaceDiagnostics.CheckAsync((_, _) =>
            {
                stop.Cancel();
                return new TaskCompletionSource<ToolResult>().Task;
            }, token: stop.Token).WaitAsync(TimeSpan.FromSeconds(2));
            throw new Exception("Winsock namespace collection ignored Stop");
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        Console.WriteLine("PASS: bounded Winsock namespace-provider structure, native planning, incomplete evidence and cancellation; fixtures only.");
    }

    private static string GuidFrom(int value) => new Guid(value, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1).ToString("D");
    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
