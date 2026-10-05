using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class PrinterDriverFileChecks
{
    internal static async Task RunAsync()
    {
        object Row(string name = "Fixture Driver", string environment = "Windows x64", int declared = 4,
            int missing = 0, int signed = 2, int signatureReview = 0, int nonExecutable = 2, int unknown = 0,
            string[]? errors = null) =>
            new { name, environment, declared, missing, signed, signatureReview, nonExecutable, unknown, errors = errors ?? [] };
        ToolResult Evidence(object[] items, bool complete = true) => new(0, JsonSerializer.Serialize(new { complete, items }), "", false);

        var clearEvidence = Evidence([Row()]);
        var clear = PrinterDriverFileDiagnostics.Interpret(clearEvidence).Single();
        Require(clear is { State: CheckState.Information, Repair: null } && ReferenceEquals(clear.Evidence, clearEvidence) &&
            !clear.Detail.Contains(@"C:\", StringComparison.OrdinalIgnoreCase), "Complete present driver files retain counts without paths or repair selection");
        Require(PrinterDriverFileDiagnostics.Interpret(Evidence([Row(missing: 1, declared: 4, signed: 1)])).Single().State == CheckState.Attention,
            "Missing registered files request package review");
        Require(PrinterDriverFileDiagnostics.Interpret(Evidence([Row(signatureReview: 1, signed: 1)])).Single().State == CheckState.Attention,
            "Executable files without Valid Authenticode status request package review");
        Require(PrinterDriverFileDiagnostics.Interpret(Evidence([Row(unknown: 1, signed: 1, nonExecutable: 1,
            errors: ["System.UnauthorizedAccessException"])], complete: false)).Single().State == CheckState.Unknown,
            "Unavailable file evidence and incomplete collection cannot pass");
        Require(PrinterDriverFileDiagnostics.Interpret(Evidence([])).Single().State == CheckState.Information,
            "Empty local registration inventory remains scoped information");

        foreach (var invalid in new[]
        {
            Evidence([Row(declared: 5)]), Evidence([Row(), Row(name: "fixture driver")]),
            Evidence([.. Enumerable.Range(0, 129).Select(index => Row(name: "Driver " + index))]),
            new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            clearEvidence with { ExitCode = 5 }, clearEvidence with { TimedOut = true }, clearEvidence with { Truncated = true }, clearEvidence with { Error = "denied" }
        })
            Require(PrinterDriverFileDiagnostics.Interpret(invalid).All(check => check.State == CheckState.Unknown && check.Repair is null),
                "Impossible, duplicate, excessive, malformed, failed and incomplete evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await PrinterDriverFileDiagnostics.CheckAsync(execute: (step, providerToken) =>
        {
            planned = step;
            bounded = providerToken.CanBeCanceled;
            return Task.FromResult(clearEvidence);
        });
        Require(collected.Single().State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && planned.Arguments.Last().Contains("Get-PrinterDriver", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("Get-AuthenticodeSignature", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Add-PrinterDriver", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Remove-PrinterDriver", StringComparison.Ordinal),
            "Native plan inventories exact registrations, presence and signatures without driver mutation");
        var failure = await PrinterDriverFileDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"));
        Require(failure.Single() is { State: CheckState.Unknown, Evidence.ExitCode: -1 }, "Provider failure retains unknown evidence");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await PrinterDriverFileDiagnostics.CheckAsync((_, _) => throw new Exception("Provider must not run"), token: stop.Token); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents collection");
        Console.WriteLine("PASS: printer driver file presence, signature summary, privacy bounds and read-only collection; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
