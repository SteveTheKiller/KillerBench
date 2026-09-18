using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class WindowsUpdateEngineFileChecks
{
    internal static async Task RunAsync()
    {
        object Row(string file, bool exists = true, string signature = "Valid", string version = "10.0.1.2") =>
            new { file, exists, signature, version = exists ? version : signature == "Unavailable" ? "Unavailable" : "Missing" };
        ToolResult Evidence(object[] files, bool complete = true) => new(0, JsonSerializer.Serialize(new { complete, files }), "", false);
        object[] validRows = [Row("wuapi.dll"), Row("wuaueng.dll"), Row("wups.dll"), Row("wups2.dll")];
        var valid = Evidence(validRows);
        var clear = WindowsUpdateEngineFileDiagnostics.Interpret(valid);
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("Valid Authenticode", StringComparison.Ordinal) &&
            clear.Detail.Contains("Paths and signer identities are excluded", StringComparison.Ordinal),
            "Complete trusted Windows Update engine evidence remains informational and private");
        var missing = WindowsUpdateEngineFileDiagnostics.Interpret(Evidence([
            Row("wuapi.dll"), Row("wuaueng.dll", false, "Missing"), Row("wups.dll"), Row("wups2.dll")
        ]));
        Require(missing is { State: CheckState.Attention, Repair: null } && missing.Detail.Contains("System files workflow", StringComparison.Ordinal),
            "Missing Windows Update engine file requests protected-file review without selecting repair");
        var mismatched = WindowsUpdateEngineFileDiagnostics.Interpret(Evidence([
            Row("wuapi.dll"), Row("wuaueng.dll", version: "10.0.1.3"), Row("wups.dll"), Row("wups2.dll")
        ]));
        Require(mismatched is { State: CheckState.Attention, Repair: null } && mismatched.Detail.Contains("version mismatch", StringComparison.OrdinalIgnoreCase),
            "Mismatched signed Windows Update engine versions request protected-file review");
        foreach (var invalid in new[]
        {
            Evidence(validRows[..3]), Evidence([.. validRows, Row("wuapi.dll")]),
            Evidence([Row("other.dll"), Row("wuaueng.dll"), Row("wups.dll"), Row("wups2.dll")]),
            Evidence([Row("wuapi.dll", false, "Valid"), Row("wuaueng.dll"), Row("wups.dll"), Row("wups2.dll")]),
            Evidence([Row("wuapi.dll", version: "bad version"), Row("wuaueng.dll"), Row("wups.dll"), Row("wups2.dll")]),
            Evidence(validRows, false), new ToolResult(0, "{}", "", false), valid with { ExitCode = 5 }, valid with { Truncated = true }
        }) Require(WindowsUpdateEngineFileDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Malformed, duplicate, incomplete, failed and inconsistent Windows Update file evidence remains unknown");
        RepairStep? planned = null;
        var collected = await WindowsUpdateEngineFileDiagnostics.CheckAsync(execute: (step, _) =>
        {
            planned = step;
            return Task.FromResult(valid);
        });
        Require(collected.State == CheckState.Information && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && planned.Arguments.Last().Contains("Get-AuthenticodeSignature", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("SignerCertificate", StringComparison.Ordinal) && !planned.Arguments.Last().Contains("Start-Process", StringComparison.Ordinal),
            "Native Windows Update file plan is bounded, read only and excludes signer identity");
        var failure = await WindowsUpdateEngineFileDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"));
        Require(failure.State == CheckState.Unknown, "Provider failure remains unknown");
        Console.WriteLine("PASS: Windows Update engine file presence, signatures, version coherence, privacy and bounds; fixtures only.");
    }

    private static void Require(bool value, string detail)
    { if (!value) throw new Exception(detail); }
}
