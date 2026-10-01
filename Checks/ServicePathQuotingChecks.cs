using KillerBench.Engine;

internal static class ServicePathQuotingChecks
{
    public static void Run()
    {
        static ToolResult Evidence(string services, bool complete = true) => new(0,
            "{\"complete\":" + complete.ToString().ToLowerInvariant() + ",\"services\":[" + services + "]}", "", false);
        const string quoted = "{\"name\":\"QuotedSvc\",\"displayName\":\"Quoted Service\",\"serviceType\":\"Own Process\",\"binaryFile\":\"service.exe\",\"status\":\"Quoted\",\"complete\":true,\"issue\":null}";
        const string noSpaces = "{\"name\":\"CompactSvc\",\"displayName\":\"Compact Service\",\"serviceType\":\"Share Process\",\"binaryFile\":\"svchost.exe\",\"status\":\"NoSpaces\",\"complete\":true,\"issue\":null}";
        const string exposed = "{\"name\":\"ExposedSvc\",\"displayName\":\"Exposed Service\",\"serviceType\":\"Own Process\",\"binaryFile\":\"service host.exe\",\"status\":\"UnquotedWithSpaces\",\"complete\":true,\"issue\":null}";
        const string driver = "{\"name\":\"ExampleDriver\",\"displayName\":\"Example Driver\",\"serviceType\":\"Kernel Driver\",\"binaryFile\":null,\"status\":\"NotApplicable\",\"complete\":true,\"issue\":null}";
        var clear = ServicePathQuotingDiagnostics.Interpret(Evidence(quoted + "," + noSpaces + "," + driver));
        Require(clear.State == CheckState.Information && clear.Repair is null && clear.Detail.Contains("No unquoted user-mode", StringComparison.Ordinal),
            "Quoted paths, paths without spaces and driver registrations remain informational");
        var attention = ServicePathQuotingDiagnostics.Interpret(Evidence(exposed));
        Require(attention.State == CheckState.Attention && attention.Repair is null &&
            attention.Detail.Contains("vendor command line", StringComparison.Ordinal) && attention.Detail.Contains("No automatic registry change", StringComparison.Ordinal),
            "Unquoted user-mode executable paths containing spaces request review without selecting a registry edit");
        var unsupported = ServicePathQuotingDiagnostics.Interpret(Evidence(quoted.Replace("\"binaryFile\":\"service.exe\",\"status\":\"Quoted\"", "\"binaryFile\":null,\"status\":\"Unsupported\"")));
        Require(unsupported.State == CheckState.Unknown && unsupported.Repair is null, "Unparseable user-mode paths keep coverage unknown");
        var unavailable = ServicePathQuotingDiagnostics.Interpret(Evidence(quoted.Replace("\"displayName\":\"Quoted Service\",\"serviceType\":\"Own Process\",\"binaryFile\":\"service.exe\",\"status\":\"Quoted\",\"complete\":true,\"issue\":null",
            "\"displayName\":\"\",\"serviceType\":\"\",\"binaryFile\":null,\"status\":\"Unavailable\",\"complete\":false,\"issue\":\"SecurityException\"")));
        Require(unavailable.State == CheckState.Unknown && unavailable.Repair is null, "Unavailable service path evidence remains unknown");
        foreach (var invalid in new[]
        {
            ServicePathQuotingDiagnostics.Interpret(Evidence(quoted, false)),
            ServicePathQuotingDiagnostics.Interpret(Evidence(quoted + "," + quoted)),
            ServicePathQuotingDiagnostics.Interpret(Evidence(quoted.Replace("service.exe", "C:\\\\service.exe"))),
            ServicePathQuotingDiagnostics.Interpret(Evidence(quoted.Replace("\"Quoted\"", "\"Future\""))),
            ServicePathQuotingDiagnostics.Interpret(Evidence(quoted.Replace("QuotedSvc", "Bad/Name"))),
            ServicePathQuotingDiagnostics.Interpret(new ToolResult(1, "", "failed", false))
        }) Require(invalid.State == CheckState.Unknown && invalid.Repair is null,
            "Malformed, duplicate, excessive or failed service path quoting evidence remains unknown");
        Require(!attention.Detail.Contains("C:\\", StringComparison.OrdinalIgnoreCase), "Service path quoting summary excludes full paths");
        Console.WriteLine("PASS: bounded service executable path quoting, driver exclusion, path privacy and review-only findings; no service changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
