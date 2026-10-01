using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class PrintExtensionFileChecks
{
    internal static async Task RunAsync()
    {
        object Row(string kind = "Processor", string name = "winprint", string environment = "Windows x64",
            string file = "winprint.dll", bool exists = true, string signature = "Valid", string error = "") =>
            new { kind, name, environment, file, exists, signature, error };
        ToolResult Evidence(object[] items, bool complete = true) => new(0, JsonSerializer.Serialize(new { complete, items }), "", false);

        var clearEvidence = Evidence([Row(), Row("Monitor", "Local Port", "Windows system directory", "localspl.dll")]);
        var clear = PrintExtensionFileDiagnostics.Interpret(clearEvidence);
        Require(clear.Count == 2 && clear.All(check => check.State == CheckState.Information && check.Repair is null && ReferenceEquals(check.Evidence, clearEvidence)) &&
            clear.All(check => !check.Detail.Contains(@"C:\", StringComparison.OrdinalIgnoreCase)),
            "Valid processor and monitor binaries remain informational without paths or repair selection");
        Require(PrintExtensionFileDiagnostics.Interpret(Evidence([Row(exists: false, signature: "Missing")])).Single().State == CheckState.Attention,
            "Missing registered extension binary requests package review");
        Require(PrintExtensionFileDiagnostics.Interpret(Evidence([Row(signature: "NotSigned")])).Single().State == CheckState.Attention,
            "Extension binary without a Valid Authenticode status requests package review");
        Require(PrintExtensionFileDiagnostics.Interpret(Evidence([Row(signature: "Unavailable", error: "System.UnauthorizedAccessException")], false)).Single().State == CheckState.Unknown,
            "Unavailable inspection and incomplete collection cannot pass");
        Require(PrintExtensionFileDiagnostics.Interpret(Evidence([])).Single().State == CheckState.Information,
            "Empty extension registration inventory remains scoped information");

        foreach (var invalid in new[]
        {
            Evidence([Row(kind: "Port")]), Evidence([Row(file: @"C:\bad.dll")]), Evidence([Row(exists: false)]),
            Evidence([Row(), Row(name: "WINPRINT")]), Evidence([Row(signature: "Unavailable")]),
            Evidence([.. Enumerable.Range(0, 385).Select(index => Row(name: "Extension " + index))]),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            clearEvidence with { ExitCode = 5 }, clearEvidence with { TimedOut = true }, clearEvidence with { Truncated = true }, clearEvidence with { Error = "denied" }
        })
            Require(PrintExtensionFileDiagnostics.Interpret(invalid).All(check => check.State == CheckState.Unknown && check.Repair is null),
                "Impossible, duplicate, excessive, malformed, failed and incomplete evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await PrintExtensionFileDiagnostics.CheckAsync(execute: (step, providerToken) =>
        {
            planned = step;
            bounded = providerToken.CanBeCanceled;
            return Task.FromResult(clearEvidence);
        });
        Require(collected.Count == 2 && bounded && planned is { Verification: true } && planned.Arguments.Contains("-NonInteractive") &&
            planned.Arguments.Last().Contains("Get-AuthenticodeSignature", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("Print Processors", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("Control\\Print\\Monitors", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Set-ItemProperty", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Remove-Item", StringComparison.Ordinal),
            "Native plan resolves registered processor and monitor binaries without loading or mutating extensions");
        var failure = await PrintExtensionFileDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"));
        Require(failure.Single() is { State: CheckState.Unknown, Evidence.ExitCode: -1 }, "Provider failure retains unknown evidence");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await PrintExtensionFileDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("Provider must not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents collection");
        Console.WriteLine("PASS: print processor and monitor binary presence, signature, privacy bounds and read-only collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
