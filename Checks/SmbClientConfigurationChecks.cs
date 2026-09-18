using KillerBench.Engine;

internal static class SmbClientConfigurationChecks
{
    internal static void Run()
    {
        static ToolResult Result(string output, int exit = 0, string error = "", bool timedOut = false, bool truncated = false) =>
            new(exit, output, error, truncated) { TimedOut = timedOut };
        const string current = """
            {"EnableSecuritySignature":true,"RequireSecuritySignature":false,"EnableInsecureGuestLogons":false,"EnableMultiChannel":true,"EnableLargeMtu":true,"ConnectionCountPerRssNetworkInterface":4,"MaximumConnectionCountPerServer":32}
            """;
        var standard = SmbClientConfigurationDiagnostics.Interpret(Result(current));
        Require(standard.State == CheckState.Information && standard.Detail.Contains("Insecure guest logons are disabled", StringComparison.Ordinal) &&
            standard.Detail.Contains("signing required: no", StringComparison.Ordinal) && standard.Detail.Contains("maximum connections per server: 32", StringComparison.Ordinal) &&
            standard.Repair is null, "SMB client configuration lost guest, signing, transport or non-mutating evidence");
        var insecure = SmbClientConfigurationDiagnostics.Interpret(Result(current.Replace("\"EnableInsecureGuestLogons\":false", "\"EnableInsecureGuestLogons\":true")));
        Require(insecure.State == CheckState.Attention && insecure.Detail.Contains("Insecure guest logons are enabled", StringComparison.Ordinal),
            "Insecure SMB guest access did not request policy review");
        foreach (var invalid in new[]
        {
            Result("{}"), Result(current.Replace("\"ConnectionCountPerRssNetworkInterface\":4", "\"ConnectionCountPerRssNetworkInterface\":0")),
            Result(current.Replace("true", "null")), Result(current + "{}"), Result("", exit: 1), Result("{}", error: "denied"),
            Result("{}", timedOut: true), Result("{}", truncated: true)
        })
            Require(SmbClientConfigurationDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Incomplete or malformed SMB client configuration evidence must remain unknown");
        Console.WriteLine("PASS: SMB client signing, guest-access, multichannel, large-MTU and connection-limit evidence remains read only with incomplete evidence classified unknown.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
