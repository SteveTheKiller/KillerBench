using KillerBench.Engine;

internal static class SmbConnectionChecks
{
    internal static void Run()
    {
        static ToolResult Result(string output, int exit = 0, string error = "", bool timedOut = false, bool truncated = false) =>
            new(exit, output, error, truncated) { TimedOut = timedOut };
        const string modern = """
            {"Connections":[{"Dialect":"3.1.1","Signed":true,"Encrypted":false,"ContinuouslyAvailable":false},{"Dialect":"3.1.1","Signed":true,"Encrypted":false,"ContinuouslyAvailable":false},{"Dialect":"3.0.2","Signed":true,"Encrypted":true,"ContinuouslyAvailable":true}]}
            """;
        var standard = SmbConnectionDiagnostics.Interpret(Result(modern));
        Require(standard.State == CheckState.Information && standard.Detail.Contains("2 using dialect 3.1.1", StringComparison.Ordinal) &&
            standard.Detail.Contains("1 using dialect 3.0.2", StringComparison.Ordinal) && standard.Repair is null,
            "Modern SMB connections lost grouped protocol evidence or became repairable");
        var unsigned = SmbConnectionDiagnostics.Interpret(Result(modern.Replace("\"Signed\":true", "\"Signed\":false", StringComparison.Ordinal)));
        Require(unsigned.State == CheckState.Attention && unsigned.Detail.Contains("without signing", StringComparison.Ordinal),
            "Unsigned SMB connections did not request review");
        var legacy = SmbConnectionDiagnostics.Interpret(Result("""
            {"Connections":[{"Dialect":"1.5","Signed":true,"Encrypted":false,"ContinuouslyAvailable":false}]}
            """));
        Require(legacy.State == CheckState.Attention && legacy.Detail.Contains("SMB 1", StringComparison.Ordinal),
            "Legacy SMB negotiation did not request review");
        var empty = SmbConnectionDiagnostics.Interpret(Result("{\"Connections\":[]}"));
        Require(empty.State == CheckState.Information && empty.Detail.Contains("No active SMB client connections", StringComparison.Ordinal),
            "Empty SMB connection evidence was not retained honestly");
        foreach (var invalid in new[]
        {
            Result("{}"), Result("{\"Connections\":null}"), Result("{\"Connections\":[{}]}"),
            Result(modern.Replace("\"Dialect\":\"3.1.1\"", "\"Dialect\":\"server/share\"", StringComparison.Ordinal)),
            Result(modern + "{}"), Result("", exit: 1), Result("{}", error: "denied"),
            Result("{}", timedOut: true), Result("{}", truncated: true)
        })
            Require(SmbConnectionDiagnostics.Interpret(invalid).State == CheckState.Unknown,
                "Incomplete or malformed SMB connection evidence must remain unknown");
        Require(!standard.Evidence!.Output.Contains("ServerName", StringComparison.Ordinal) &&
            !standard.Evidence.Output.Contains("ShareName", StringComparison.Ordinal) &&
            !standard.Evidence.Output.Contains("UserName", StringComparison.Ordinal),
            "SMB connection evidence retained a prohibited identity field");
        Console.WriteLine("PASS: active SMB protocol negotiation is grouped without server, share or account identities and remains read only.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
