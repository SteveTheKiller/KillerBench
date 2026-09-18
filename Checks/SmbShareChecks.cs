using KillerBench.Engine;

internal static class SmbShareChecks
{
    internal static void Run()
    {
        static ToolResult Result(string output, int exit = 0, string error = "", bool timedOut = false, bool truncated = false) =>
            new(exit, output, error, truncated) { TimedOut = timedOut };
        const string shares = """
            {"shares":[{"Name":"ADMIN$","ScopeName":"*","ShareState":"Online","AvailabilityType":"NonClustered","CachingMode":"Manual","FolderEnumerationMode":"Unrestricted","EncryptData":false,"Special":true},{"Name":"Projects","ScopeName":"*","ShareState":"Offline","AvailabilityType":"NonClustered","CachingMode":"None","FolderEnumerationMode":"AccessBased","EncryptData":true,"Special":false}]}
            """;
        var checks = SmbShareDiagnostics.Interpret(Result(shares));
        Require(checks.Count == 2 && checks[0].State == CheckState.Information && checks[0].Detail.Contains("special or administrative", StringComparison.Ordinal) &&
            checks[1].State == CheckState.Attention && checks[1].Detail.Contains("encryption required", StringComparison.Ordinal) &&
            !checks.Any(check => check.Detail.Contains("C:\\", StringComparison.Ordinal) || check.Detail.Contains("permission", StringComparison.OrdinalIgnoreCase) && !check.Detail.Contains("permissions, sessions", StringComparison.Ordinal)),
            "SMB share inventory lost state, special-share, encryption or privacy boundaries");
        Require(SmbShareDiagnostics.Interpret(Result("{\"shares\":[]}"))[0].State == CheckState.Information,
            "An empty complete SMB share inventory must remain informational");
        foreach (var invalid in new[]
        {
            Result("{\"shares\":[{\"Name\":\"A\"}]}"),
            Result("{\"shares\":[{\"Name\":\"A\",\"ScopeName\":\"*\",\"ShareState\":\"Online\",\"AvailabilityType\":\"NonClustered\",\"CachingMode\":\"None\",\"FolderEnumerationMode\":\"Unrestricted\",\"EncryptData\":false,\"Special\":false},{\"Name\":\"a\",\"ScopeName\":\"*\",\"ShareState\":\"Online\",\"AvailabilityType\":\"NonClustered\",\"CachingMode\":\"None\",\"FolderEnumerationMode\":\"Unrestricted\",\"EncryptData\":false,\"Special\":false}]}"),
            Result("{}"), Result("", exit: 1), Result("{}", error: "denied"), Result("{}", timedOut: true), Result("{}", truncated: true)
        })
            Require(SmbShareDiagnostics.Interpret(invalid).Single().State == CheckState.Unknown,
                "Incomplete or malformed SMB share evidence must remain unknown");
        string oversized = "{\"shares\":[" + string.Join(',', Enumerable.Range(0, 129).Select(index =>
            $"{{\"Name\":\"S{index}\",\"ScopeName\":\"*\",\"ShareState\":\"Online\",\"AvailabilityType\":\"NonClustered\",\"CachingMode\":\"None\",\"FolderEnumerationMode\":\"Unrestricted\",\"EncryptData\":false,\"Special\":false}}")) + "]}";
        Require(SmbShareDiagnostics.Interpret(Result(oversized)).Single().State == CheckState.Unknown,
            "Capped SMB share evidence must not claim complete inventory");
        Console.WriteLine("PASS: bounded SMB share identity, state, special-share and encryption evidence with paths, permissions, sessions and accounts excluded; no settings changed.");
    }

    private static void Require(bool condition, string message)
    { if (!condition) throw new Exception(message); }
}
