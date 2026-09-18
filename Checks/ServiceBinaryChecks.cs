using KillerBench.Engine;

internal static class ServiceBinaryChecks
{
    public static void Run()
    {
        ToolResult Evidence(string services, bool complete = true) => new(0,
            "{\"complete\":" + complete.ToString().ToLowerInvariant() + ",\"services\":[" + services + "]}", "", false);
        const string present = "{\"name\":\"ExampleSvc\",\"displayName\":\"Example Service\",\"serviceType\":\"Own Process\",\"binaryFile\":\"example.exe\",\"status\":\"Present\",\"signature\":\"Valid\",\"signatureIssue\":null,\"complete\":true,\"issue\":null}";
        var clear = ServiceBinaryDiagnostics.Interpret(Evidence(present));
        Require(clear.State == CheckState.Information && clear.Repair is null && clear.Detail.Contains("1 local binaries are present", StringComparison.Ordinal) &&
            clear.Detail.Contains("Authenticode reported 1 Valid", StringComparison.Ordinal), "Present Valid local service binaries remain informational");
        var missing = ServiceBinaryDiagnostics.Interpret(Evidence(present.Replace("\"status\":\"Present\",\"signature\":\"Valid\"", "\"status\":\"Missing\",\"signature\":null")));
        Require(missing.State == CheckState.Attention && missing.Repair is null && missing.Detail.Contains("owning Windows component", StringComparison.Ordinal),
            "Missing service binaries request owner review without selecting replacement");
        var unsigned = ServiceBinaryDiagnostics.Interpret(Evidence(present.Replace("\"Valid\"", "\"NotSigned\"")));
        Require(unsigned.State == CheckState.Attention && unsigned.Repair is null && unsigned.Detail.Contains("approved application package", StringComparison.Ordinal),
            "Non-Valid service binary signatures request package review");
        var signatureUnavailable = ServiceBinaryDiagnostics.Interpret(Evidence(present.Replace("\"signature\":\"Valid\",\"signatureIssue\":null", "\"signature\":\"Unavailable\",\"signatureIssue\":\"SecurityException\"")));
        Require(signatureUnavailable.State == CheckState.Unknown && signatureUnavailable.Repair is null,
            "Unavailable service binary signature remains unknown");
        var unsupported = ServiceBinaryDiagnostics.Interpret(Evidence(present.Replace("\"binaryFile\":\"example.exe\",\"status\":\"Present\",\"signature\":\"Valid\"", "\"binaryFile\":null,\"status\":\"Unsupported\",\"signature\":null")));
        Require(unsupported.State == CheckState.Unknown && unsupported.Detail.Contains("were not contacted", StringComparison.Ordinal),
            "Unsupported service paths remain unknown and are not contacted");
        var unavailable = ServiceBinaryDiagnostics.Interpret(Evidence(present.Replace("\"displayName\":\"Example Service\",\"serviceType\":\"Own Process\",\"binaryFile\":\"example.exe\",\"status\":\"Present\",\"signature\":\"Valid\",\"signatureIssue\":null,\"complete\":true,\"issue\":null",
            "\"displayName\":\"\",\"serviceType\":\"\",\"binaryFile\":null,\"status\":\"Unavailable\",\"signature\":null,\"signatureIssue\":null,\"complete\":false,\"issue\":\"SecurityException\"")));
        Require(unavailable.State == CheckState.Unknown && unavailable.Repair is null, "Unavailable service registration remains unknown");
        foreach (var invalid in new[]
        {
            ServiceBinaryDiagnostics.Interpret(Evidence(present, false)),
            ServiceBinaryDiagnostics.Interpret(Evidence(present + "," + present)),
            ServiceBinaryDiagnostics.Interpret(Evidence(present.Replace("example.exe", "C:\\\\service\\\\example.exe"))),
            ServiceBinaryDiagnostics.Interpret(Evidence(present.Replace("\"Present\"", "\"Future\""))),
            ServiceBinaryDiagnostics.Interpret(Evidence(present.Replace("\"Valid\"", "\"Future\""))),
            ServiceBinaryDiagnostics.Interpret(Evidence(present.Replace("\"ExampleSvc\"", "\"Bad/Name\""))),
            ServiceBinaryDiagnostics.Interpret(new ToolResult(1, "", "failed", false))
        }) Require(invalid.State == CheckState.Unknown && invalid.Repair is null,
            "Malformed, duplicate, excessive or failed service binary evidence remains unknown");
        Require(!clear.Detail.Contains("C:\\", StringComparison.OrdinalIgnoreCase), "Service binary summary excludes full paths");
        Console.WriteLine("PASS: bounded service binary presence, Authenticode status, path privacy, missing files and unsupported locations; no service changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
