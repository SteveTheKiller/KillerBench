using KillerBench.Engine;

internal static class WindowsLicensingChecks
{
    private const string Id = "11111111-2222-3333-4444-555555555555";

    internal static void Run()
    {
        var licensed = WindowsLicensingDiagnostics.Interpret(Result("{\"complete\":true,\"products\":[{" +
            "\"id\":\"" + Id + "\",\"name\":\"Windows 11 Pro\",\"description\":\"Windows Operating System\",\"status\":1,\"grace\":0}]}"));
        Require(licensed is { State: CheckState.Clear, Repair: null, Evidence: not null } && licensed.Detail.Contains("Licensed") &&
            licensed.Detail.Contains("No product key was collected"), "Licensed Windows evidence remains read-only and explains key exclusion");
        foreach (int status in new[] { 0, 2, 3, 4, 5, 6 })
        {
            var review = WindowsLicensingDiagnostics.Interpret(Result("{\"complete\":true,\"products\":[{" +
                "\"id\":\"" + Id + "\",\"name\":\"Windows 11 Pro\",\"description\":\"Windows Operating System\",\"status\":" + status + ",\"grace\":120}]}"));
            Require(review is { State: CheckState.Attention, Repair: null }, "Non-licensed Windows state requests review without activation");
        }
        foreach (string invalid in new[]
        {
            "{}", "{\"complete\":false,\"products\":[]}", "{\"complete\":true,\"products\":[]}",
            "{\"complete\":true,\"products\":[{\"id\":\"bad\",\"name\":\"Windows\",\"description\":\"OS\",\"status\":1,\"grace\":0}]}",
            "{\"complete\":true,\"products\":[{\"id\":\"" + Id + "\",\"name\":\"bad\\nname\",\"description\":\"OS\",\"status\":1,\"grace\":0}]}",
            "{\"complete\":true,\"complete\":true,\"products\":[]}"
        })
            Require(WindowsLicensingDiagnostics.Interpret(Result(invalid)) is { State: CheckState.Unknown, Repair: null },
                "Incomplete, duplicate or malformed licensing evidence remains unknown");
        foreach (var failure in new[] { Result("{}").WithExit(5), Result("{}") with { TimedOut = true }, new ToolResult(0, "{}", "provider error", false), new ToolResult(0, "{}", "", true) })
            Require(WindowsLicensingDiagnostics.Interpret(failure).State == CheckState.Unknown, "Failed licensing collection cannot pass");
        Require(!WindowsLicensingDiagnostics.Step().Arguments.Any(argument => argument.Contains("productKey=", StringComparison.OrdinalIgnoreCase)),
            "Licensing query does not select or emit a product key");
        Console.WriteLine("PASS: Windows licensing states, incomplete evidence and key-free read-only collection.");
    }

    private static ToolResult Result(string json) => new(0, json, "", false);
    private static ToolResult WithExit(this ToolResult result, int exitCode) => result with { ExitCode = exitCode };
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
