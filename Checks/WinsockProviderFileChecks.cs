using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class WinsockProviderFileChecks
{
    internal static async Task RunAsync()
    {
        object Row(int id, bool resolved = true, string file = "mswsock.dll", bool exists = true,
            string signature = "Valid", int error = 0) => new
            {
                provider = new Guid(id, 0, 0, new byte[8]).ToString("D"), resolved, file, exists, signature, error
            };
        ToolResult Evidence(object[] items, bool complete = true) => new(0, JsonSerializer.Serialize(new { complete, items }), "", false);
        var valid = Evidence([Row(1), Row(2, file: "security-provider.dll")]);
        var clear = WinsockProviderFileDiagnostics.Interpret(valid);
        Require(clear is { State: CheckState.Information, Repair: null } && clear.Detail.Contains("Valid Authenticode", StringComparison.Ordinal) &&
            clear.Detail.Contains("Full paths, provider names, signer identities and file contents are excluded", StringComparison.Ordinal),
            "Complete trusted provider file evidence remains informational and private");
        var missing = WinsockProviderFileDiagnostics.Interpret(Evidence([Row(1), Row(2, file: "missing.dll", exists: false, signature: "Missing")]));
        Require(missing is { State: CheckState.Attention, Repair: null } && missing.Detail.Contains("1 missing", StringComparison.Ordinal) &&
            missing.Detail.Contains("before considering provider recovery", StringComparison.Ordinal),
            "Missing provider file requests review without selecting a reset");
        var unsigned = WinsockProviderFileDiagnostics.Interpret(Evidence([Row(1, signature: "NotSigned")]));
        Require(unsigned is { State: CheckState.Attention, Repair: null } && unsigned.Detail.Contains("non-Valid", StringComparison.Ordinal),
            "Non-Valid signature requests review without selecting repair");
        foreach (var invalid in new[]
        {
            Evidence([]), Evidence([Row(1), Row(1)]), Evidence([Row(1, file: "C:\\bad.dll")]),
            Evidence([Row(1, resolved: false, file: "", exists: false, signature: "Unavailable", error: 10022)]),
            Evidence([Row(1, resolved: true, file: "provider.dll", exists: false, signature: "Valid")]),
            Evidence([Row(1)], false), new ToolResult(0, "{}", "", false), valid with { ExitCode = 5 }, valid with { Truncated = true }
        }) Require(WinsockProviderFileDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Empty, duplicate, inconsistent, unresolved, incomplete and failed provider evidence remains unknown");

        RepairStep? planned = null;
        bool bounded = false;
        var collected = await WinsockProviderFileDiagnostics.CheckAsync(execute: (step, token) =>
        {
            planned = step;
            bounded = token.CanBeCanceled;
            return Task.FromResult(valid);
        });
        string script = planned?.Arguments.LastOrDefault() ?? "";
        Require(collected.State == CheckState.Information && bounded && planned is { Verification: true } &&
            planned.Arguments.Contains("-NonInteractive") && script.Contains("WSCGetProviderPath", StringComparison.Ordinal) &&
            script.Contains("Get-AuthenticodeSignature", StringComparison.Ordinal) && !script.Contains("SignerCertificate", StringComparison.Ordinal) &&
            !script.Contains("Start-Process", StringComparison.Ordinal) && !script.Contains("netsh", StringComparison.OrdinalIgnoreCase),
            "Native provider file plan is bounded, read only and excludes signer identity and reset commands");
        var failure = await WinsockProviderFileDiagnostics.CheckAsync(execute: (_, _) => throw new IOException("fixture failure"));
        Require(failure.State == CheckState.Unknown, "Provider failure remains unknown");
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        await RequireCanceled(() => WinsockProviderFileDiagnostics.CheckAsync(canceled.Token, (_, _) => Task.FromResult(valid)));
        Console.WriteLine("PASS: Winsock provider file presence, signatures, privacy, bounds and cancellation; fixtures only.");
    }

    private static async Task RequireCanceled(Func<Task> action)
    {
        try { await action(); }
        catch (OperationCanceledException) { return; }
        throw new Exception("Canceled Winsock provider file collection did not stop.");
    }
    private static void Require(bool value, string detail)
    { if (!value) throw new Exception(detail); }
}
