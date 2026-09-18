using KillerBench.Engine;

internal static class ServiceDllChecks
{
    public static void Run()
    {
        ToolResult Evidence(string services, bool complete = true) => new(0,
            "{\"complete\":" + complete.ToString().ToLowerInvariant() + ",\"services\":[" + services + "]}", "", false);
        const string present = "{\"name\":\"ExampleSvc\",\"dllFile\":\"example.dll\",\"status\":\"Present\",\"signature\":\"Valid\",\"signatureIssue\":null,\"complete\":true,\"issue\":null}";
        var clear = ServiceDllDiagnostics.Interpret(Evidence(present));
        Require(clear.State == CheckState.Information && clear.Repair is null && clear.Detail.Contains("1 local DLLs are present", StringComparison.Ordinal) &&
            clear.Detail.Contains("Authenticode reported 1 Valid", StringComparison.Ordinal), "Present Valid registered service DLLs remain informational");
        var missing = ServiceDllDiagnostics.Interpret(Evidence(present.Replace("\"status\":\"Present\",\"signature\":\"Valid\"", "\"status\":\"Missing\",\"signature\":null")));
        Require(missing.State == CheckState.Attention && missing.Repair is null && missing.Detail.Contains("owning Windows component", StringComparison.Ordinal),
            "Missing registered service DLLs request owner review without selecting replacement");
        var unsigned = ServiceDllDiagnostics.Interpret(Evidence(present.Replace("\"Valid\"", "\"NotSigned\"")));
        Require(unsigned.State == CheckState.Attention && unsigned.Repair is null && unsigned.Detail.Contains("approved application package", StringComparison.Ordinal),
            "Non-Valid registered service DLL signatures request package review");
        var signatureUnavailable = ServiceDllDiagnostics.Interpret(Evidence(present.Replace("\"signature\":\"Valid\",\"signatureIssue\":null", "\"signature\":\"Unavailable\",\"signatureIssue\":\"SecurityException\"")));
        Require(signatureUnavailable.State == CheckState.Unknown && signatureUnavailable.Repair is null,
            "Unavailable registered service DLL signature remains unknown");
        var unsupported = ServiceDllDiagnostics.Interpret(Evidence(present.Replace("\"status\":\"Present\",\"signature\":\"Valid\"", "\"status\":\"Unsupported\",\"signature\":null")));
        Require(unsupported.State == CheckState.Unknown && unsupported.Detail.Contains("were not contacted", StringComparison.Ordinal),
            "Unsupported registered service DLL locations remain unknown and are not contacted");
        var invalid = ServiceDllDiagnostics.Interpret(Evidence(present.Replace("\"dllFile\":\"example.dll\",\"status\":\"Present\",\"signature\":\"Valid\"", "\"dllFile\":null,\"status\":\"Invalid\",\"signature\":null")));
        Require(invalid.State == CheckState.Unknown && invalid.Repair is null, "Invalid registered service DLL remains unknown");
        var unavailable = ServiceDllDiagnostics.Interpret(Evidence(present.Replace("\"dllFile\":\"example.dll\",\"status\":\"Present\",\"signature\":\"Valid\",\"signatureIssue\":null,\"complete\":true,\"issue\":null",
            "\"dllFile\":null,\"status\":\"Unavailable\",\"signature\":null,\"signatureIssue\":null,\"complete\":false,\"issue\":\"SecurityException\"")));
        Require(unavailable.State == CheckState.Unknown && unavailable.Repair is null, "Unavailable registered service DLL remains unknown");
        foreach (var result in new[]
        {
            ServiceDllDiagnostics.Interpret(Evidence(present, false)),
            ServiceDllDiagnostics.Interpret(Evidence(present + "," + present)),
            ServiceDllDiagnostics.Interpret(Evidence(present.Replace("example.dll", "C:\\\\service\\\\example.dll"))),
            ServiceDllDiagnostics.Interpret(Evidence(present.Replace("\"Present\"", "\"Future\""))),
            ServiceDllDiagnostics.Interpret(Evidence(present.Replace("\"Valid\"", "\"Future\""))),
            ServiceDllDiagnostics.Interpret(Evidence(present.Replace("\"ExampleSvc\"", "\"Bad/Name\""))),
            ServiceDllDiagnostics.Interpret(new ToolResult(1, "", "failed", false))
        }) Require(result.State == CheckState.Unknown && result.Repair is null,
            "Malformed, duplicate, excessive or failed registered service DLL evidence remains unknown");
        Require(!clear.Detail.Contains("C:\\", StringComparison.OrdinalIgnoreCase), "Registered service DLL summary excludes full paths");
        var selected = ServiceDllDiagnostics.CreateReadStep(["wuauserv", "BITS", "CryptSvc", "TrustedInstaller", "WaaSMedicSvc", "UsoSvc"]);
        string selectedScript = selected.Arguments[^1];
        Require(selected.Verification && selected.Tool == PrintRecovery.Tool &&
            new[] { "wuauserv", "BITS", "CryptSvc", "TrustedInstaller", "WaaSMedicSvc", "UsoSvc" }.All(selectedScript.Contains) &&
            !selectedScript.Contains("GetSubKeyNames", StringComparison.Ordinal) && !selectedScript.Contains("SetValue", StringComparison.Ordinal) &&
            !selectedScript.Contains("Start-Service", StringComparison.Ordinal),
            "Selected service DLL plan reads only the exact requested registrations without changing files, services or registry values");
        foreach (IReadOnlyCollection<string> invalidSelection in new IReadOnlyCollection<string>[]
        {
            Array.Empty<string>(), new[] { "wuauserv", "WUAUSERV" }, new[] { "bad/name" }
        })
        {
            try { ServiceDllDiagnostics.CreateReadStep(invalidSelection); throw new Exception("Invalid selected service DLL scope was accepted"); }
            catch (ArgumentException) { }
        }
        Console.WriteLine("PASS: bounded registered service DLL presence, selected scopes, Authenticode status, path privacy, missing files and unsupported locations; no service changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
