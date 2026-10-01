using KillerBench.Engine;

internal static class PrintExtensionChecks
{
    public static void Run()
    {
        static ToolResult Evidence(string complete = "true", string queues = "[]", string processors = "[]", string monitors = "[]") =>
            new(0, $"{{\"complete\":{complete},\"queues\":{queues},\"processors\":{processors},\"monitors\":{monitors}}}", "", false);
        const string processor = "[{\"name\":\"winprint\",\"environment\":\"Windows x64\",\"driver\":\"winprint.dll\"}]";
        const string monitor = "[{\"name\":\"Standard TCP/IP Port\",\"driver\":\"tcpmon.dll\"}]";
        var matched = Evidence(queues: "[{\"Name\":\"Office\",\"Type\":0,\"PrintProcessor\":\"winprint\"}]", processors: processor, monitors: monitor);
        var matchedResult = PrintExtensionDiagnostics.Interpret(matched);
        Require(matchedResult.State == CheckState.Information && matchedResult.Repair is null && matchedResult.Evidence == matched &&
            matchedResult.Detail.Contains("1 print processor registration", StringComparison.Ordinal),
            "Matched print extensions remain informational and non-mutating");
        var missing = PrintExtensionDiagnostics.Interpret(Evidence(queues: "[{\"Name\":\"Broken queue\",\"Type\":0,\"PrintProcessor\":\"missing\"}]", processors: processor, monitors: monitor));
        Require(missing.State == CheckState.Attention && missing.Repair is null && missing.Detail.Contains("Broken queue -> missing", StringComparison.Ordinal),
            "A local queue with an absent processor registration requests review without selecting repair");
        var remote = PrintExtensionDiagnostics.Interpret(Evidence(queues: "[{\"Name\":\"Shared queue\",\"Type\":1,\"PrintProcessor\":\"remoteprint\"}]", processors: processor, monitors: monitor));
        Require(remote.State == CheckState.Information && remote.Detail.Contains("not verified against the remote server", StringComparison.Ordinal),
            "Remote processor references remain explicitly unverified");
        foreach (var invalid in new[]
        {
            matched with { ExitCode = 1 }, matched with { Error = "denied" }, matched with { TimedOut = true }, matched with { Truncated = true }, Evidence("false"),
            Evidence(queues: "[{}]", processors: processor, monitors: monitor),
            Evidence(processors: "[{\"name\":\" winprint\",\"environment\":\"Windows x64\",\"driver\":\"winprint.dll\"}]"),
            Evidence(processors: "[{\"name\":\"winprint\",\"environment\":\"Windows x64\",\"driver\":\"C:\\\\bad.dll\"}]"),
            Evidence(processors: "[{\"name\":\"winprint\",\"environment\":\"Windows x64\",\"driver\":\"a.dll\"},{\"name\":\"WINPRINT\",\"environment\":\"windows x64\",\"driver\":\"b.dll\"}]"),
            Evidence(monitors: "[{\"name\":\"Monitor\",\"driver\":\"a.dll\"},{\"name\":\"monitor\",\"driver\":\"b.dll\"}]")
        }) Require(PrintExtensionDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Failed, incomplete, malformed, path-bearing and duplicate print extension evidence remains unknown");
        var capped = Evidence(processors: "[" + string.Join(",", Enumerable.Range(0, 257).Select(index =>
            $"{{\"name\":\"Processor{index}\",\"environment\":\"Windows x64\",\"driver\":\"driver{index}.dll\"}}")) + "]");
        Require(PrintExtensionDiagnostics.Interpret(capped).State == CheckState.Unknown,
            "Oversized print extension evidence cannot claim complete coverage");
        Console.WriteLine("PASS: bounded print processor and monitor registration, local matching and incomplete evidence; no print changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
