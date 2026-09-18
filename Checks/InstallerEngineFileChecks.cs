using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class InstallerEngineFileChecks
{
    internal static async Task RunAsync()
    {
        object Row(string architecture, string file, bool exists = true, string signature = "Valid") =>
            new { architecture, file, exists, signature };
        ToolResult Evidence(object[] files, bool complete = true) =>
            new(0, JsonSerializer.Serialize(new { complete, files }), "", false);
        object[] native = [Row("Native", "msiexec.exe"), Row("Native", "msi.dll"), Row("Native", "msihnd.dll")];

        var valid = Evidence(native);
        var clear = InstallerEngineFileDiagnostics.Interpret(valid);
        Require(clear is { State: CheckState.Information, Repair: null } && ReferenceEquals(clear.Evidence, valid) &&
            clear.Detail.Contains("Valid Authenticode", StringComparison.Ordinal) && clear.Detail.Contains("Paths and signer identities are excluded", StringComparison.Ordinal),
            "Complete trusted native Installer file evidence remains informational and private");
        var wow = InstallerEngineFileDiagnostics.Interpret(Evidence([
            .. native, Row("WOW64", "msiexec.exe"), Row("WOW64", "msi.dll"), Row("WOW64", "msihnd.dll")
        ]));
        Require(wow.State == CheckState.Information && wow.Detail.Contains("6 required", StringComparison.Ordinal),
            "Complete native and WOW64 file sets are accepted");
        var missing = InstallerEngineFileDiagnostics.Interpret(Evidence([
            Row("Native", "msiexec.exe", false, "Missing"), Row("Native", "msi.dll"), Row("Native", "msihnd.dll")
        ]));
        Require(missing is { State: CheckState.Attention, Repair: null } && missing.Detail.Contains("System files workflow", StringComparison.Ordinal),
            "Missing Installer engine file requests protected-file review without repair selection");
        Require(InstallerEngineFileDiagnostics.Interpret(Evidence([
            Row("Native", "msiexec.exe", true, "HashMismatch"), Row("Native", "msi.dll"), Row("Native", "msihnd.dll")
        ])).State == CheckState.Attention, "Non-Valid Installer signature requests review");
        Require(InstallerEngineFileDiagnostics.Interpret(Evidence([
            Row("Native", "msiexec.exe", false, "Unavailable"), Row("Native", "msi.dll"), Row("Native", "msihnd.dll")
        ], false)).State == CheckState.Unknown, "Unavailable Installer file evidence remains unknown");

        foreach (var invalid in new[]
        {
            Evidence([Row("Native", "msiexec.exe"), Row("Native", "msi.dll")]),
            Evidence([.. native, Row("Native", "msiexec.exe")]),
            Evidence([Row("Future", "msiexec.exe"), Row("Future", "msi.dll"), Row("Future", "msihnd.dll")]),
            Evidence([Row("Native", "other.dll"), Row("Native", "msi.dll"), Row("Native", "msihnd.dll")]),
            Evidence([Row("Native", "msiexec.exe", false, "Valid"), Row("Native", "msi.dll"), Row("Native", "msihnd.dll")]),
            Evidence(native, false), new ToolResult(0, "{}", "", false), new ToolResult(0, "not json", "", false),
            valid with { ExitCode = 5 }, valid with { TimedOut = true }, valid with { Truncated = true }, valid with { Error = "denied" }
        })
            Require(InstallerEngineFileDiagnostics.Interpret(invalid) is { State: CheckState.Unknown, Repair: null },
                "Malformed, duplicate, incomplete, failed and inconsistent Installer file evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await InstallerEngineFileDiagnostics.CheckAsync(execute: (step, providerToken) =>
        {
            planned = step;
            bounded = providerToken.CanBeCanceled;
            return Task.FromResult(valid);
        });
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && planned.Arguments.Last().Contains("Get-AuthenticodeSignature", StringComparison.Ordinal) &&
            planned.Arguments.Last().Contains("SysWOW64", StringComparison.Ordinal) && !planned.Arguments.Last().Contains("SignerCertificate", StringComparison.Ordinal) &&
            !planned.Arguments.Last().Contains("Start-Process", StringComparison.Ordinal),
            "Native Installer file plan is bounded, covers available architectures and excludes signer identity and execution");
        var failure = await InstallerEngineFileDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"));
        Require(failure is { State: CheckState.Unknown, Evidence.ExitCode: -1 }, "Installer file provider failure retains unknown evidence");
        using var stop = new CancellationTokenSource();
        stop.Cancel();
        bool canceled = false;
        try { await InstallerEngineFileDiagnostics.CheckAsync(stop.Token, (_, _) => throw new Exception("Provider must not run")); }
        catch (OperationCanceledException) { canceled = true; }
        Require(canceled, "Caller cancellation prevents Installer file collection");
        Console.WriteLine("PASS: Windows Installer engine file presence, signature, architecture, privacy and bounds; fixtures only.");
    }

    private static void Require(bool value, string detail)
    {
        if (!value) throw new Exception(detail);
    }
}
