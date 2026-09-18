using KillerBench.Engine;

internal static class SmbServerConfigurationChecks
{
    internal static void Run()
    {
        static ToolResult Result(string output, int exit = 0, string error = "", bool timedOut = false, bool truncated = false) =>
            new(exit, output, error, truncated) { TimedOut = timedOut };
        const string current = """
            {"EnableSMB1Protocol":false,"EnableSMB2Protocol":true,"EnableSecuritySignature":true,"RequireSecuritySignature":true,"EncryptData":false,"RejectUnencryptedAccess":true,"EnableMultiChannel":true,"EnableLeasing":true}
            """;
        var healthy = SmbServerConfigurationDiagnostics.Interpret(Result(current));
        Require(healthy.State == CheckState.Information && healthy.Detail.Contains("SMB 1 is disabled", StringComparison.Ordinal) &&
            healthy.Detail.Contains("signing required: yes", StringComparison.Ordinal) && healthy.Detail.Contains("default share encryption: no", StringComparison.Ordinal) &&
            healthy.Repair is null, "SMB configuration lost protocol, signing, encryption or non-mutating evidence");
        var legacy = SmbServerConfigurationDiagnostics.Interpret(Result(current.Replace("\"EnableSMB1Protocol\":false", "\"EnableSMB1Protocol\":true")));
        var disabled = SmbServerConfigurationDiagnostics.Interpret(Result(current.Replace("\"EnableSMB2Protocol\":true", "\"EnableSMB2Protocol\":false")));
        Require(legacy.State == CheckState.Attention && legacy.Detail.Contains("SMB 1 is enabled", StringComparison.Ordinal) &&
            disabled.State == CheckState.Attention && disabled.Detail.Contains("SMB 2 and newer protocols are disabled", StringComparison.Ordinal),
            "SMB protocol review did not retain explicit legacy or disabled state");
        foreach (var invalid in new[]
        {
            Result("{}"), Result(current.Replace("true", "null")), Result(current + "{}"), Result("", exit: 1),
            Result("{}", error: "denied"), Result("{}", timedOut: true), Result("{}", truncated: true)
        })
            Require(SmbServerConfigurationDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Incomplete or malformed SMB configuration evidence must remain unknown");
        Console.WriteLine("PASS: SMB protocol, signing, encryption, multichannel and leasing configuration remains read only with incomplete evidence classified unknown.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
