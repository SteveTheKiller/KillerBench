using KillerBench.Engine;

internal static class DefaultPrinterChecks
{
    public static void Run()
    {
        static ToolResult Evidence(string complete = "true", int total = 0, string defaults = "[]", string kind = "Absent", string mode = "null") =>
            new(0, $"{{\"complete\":{complete},\"total\":{total},\"defaults\":{defaults},\"modeKind\":\"{kind}\",\"mode\":{mode}}}", "", false);
        var selected = DefaultPrinterDiagnostics.Interpret(Evidence(total: 3, defaults: "[\"Office Printer\"]", kind: "DWord", mode: "1"));
        Require(selected.State == CheckState.Information && selected.Repair is null && selected.Detail.Contains("one default: Office Printer", StringComparison.Ordinal) &&
            selected.Detail.Contains("legacy explicit", StringComparison.Ordinal), "One default queue remains informational and read only");
        Require(DefaultPrinterDiagnostics.Interpret(Evidence()).State == CheckState.Information,
            "No queues and no default is a valid informational state");
        var missing = DefaultPrinterDiagnostics.Interpret(Evidence(total: 2));
        Require(missing.State == CheckState.Attention && missing.Repair is null && missing.Detail.Contains("no default selection", StringComparison.Ordinal),
            "Queues without a default request review without selecting a change");
        var conflicting = DefaultPrinterDiagnostics.Interpret(Evidence(total: 2, defaults: "[\"A\",\"B\"]", kind: "DWord", mode: "0"));
        Require(conflicting.State == CheckState.Attention && conflicting.Detail.Contains("Conflicting", StringComparison.Ordinal),
            "Multiple defaults remain conflicting evidence");
        foreach (var invalid in new[]
        {
            Evidence() with { ExitCode = 1 }, Evidence() with { Error = "denied" }, Evidence() with { TimedOut = true }, Evidence() with { Truncated = true },
            Evidence("false"), Evidence(total: -1), Evidence(total: 1, defaults: "[\"A\",\"B\"]"),
            Evidence(total: 2, defaults: "[\"A\",\"a\"]"), Evidence(total: 1, defaults: "[\" A\"]"),
            Evidence(kind: "String", mode: "1"), Evidence(kind: "DWord", mode: "null"), Evidence(kind: "DWord", mode: "2"),
            new ToolResult(0, "{\"complete\":true,\"complete\":true,\"total\":0,\"defaults\":[],\"modeKind\":\"Absent\",\"mode\":null}", "", false)
        }) Require(DefaultPrinterDiagnostics.Interpret(invalid).State == CheckState.Unknown,
            "Failed, incomplete, malformed, duplicate and unsupported default-printer evidence remains unknown");
        Require(DefaultPrinterDiagnostics.Interpret(Evidence(total: 513)).State == CheckState.Unknown,
            "Oversized printer inventory cannot claim complete coverage");
        Console.WriteLine("PASS: bounded default-printer and automatic-management evidence remains read only.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
