using KillerBench.Engine;

internal static class ServiceLogonChecks
{
    public static void Run()
    {
        ToolResult Evidence(string services, bool complete = true) => new(0,
            "{\"complete\":" + complete.ToString().ToLowerInvariant() + ",\"services\":[" + services + "]}", "", false);
        const string system = "{\"name\":\"SystemSvc\",\"displayName\":\"System Service\",\"serviceType\":\"Own Process\",\"accountClass\":\"LocalSystem\",\"interactive\":false,\"complete\":true,\"issue\":null}";
        const string virtualAccount = "{\"name\":\"VirtualSvc\",\"displayName\":\"Virtual Service\",\"serviceType\":\"Own Process\",\"accountClass\":\"VirtualAccount\",\"interactive\":false,\"complete\":true,\"issue\":null}";
        const string named = "{\"name\":\"NamedSvc\",\"displayName\":\"Named Service\",\"serviceType\":\"Own Process\",\"accountClass\":\"DomainOrLocalAccount\",\"interactive\":false,\"complete\":true,\"issue\":null}";
        var clear = ServiceLogonDiagnostics.Interpret(Evidence(system + "," + virtualAccount + "," + named));
        Require(clear.State == CheckState.Information && clear.Repair is null && clear.Detail.Contains("configuration context", StringComparison.Ordinal),
            "Supported service account classes remain informational without a least-privilege claim");
        var interactive = ServiceLogonDiagnostics.Interpret(Evidence(named.Replace("\"interactive\":false", "\"interactive\":true")));
        Require(interactive.State == CheckState.Attention && interactive.Repair is null && interactive.Detail.Contains("not configured for LocalSystem", StringComparison.Ordinal),
            "Interactive non-LocalSystem service configuration requests package review without selecting an account change");
        var unavailable = ServiceLogonDiagnostics.Interpret(Evidence(system.Replace("\"displayName\":\"System Service\",\"serviceType\":\"Own Process\",\"accountClass\":\"LocalSystem\",\"interactive\":false,\"complete\":true,\"issue\":null",
            "\"displayName\":\"\",\"serviceType\":\"\",\"accountClass\":\"Unavailable\",\"interactive\":false,\"complete\":false,\"issue\":\"SecurityException\"")));
        Require(unavailable.State == CheckState.Unknown && unavailable.Repair is null, "Unavailable service logon evidence remains unknown");
        foreach (var invalid in new[]
        {
            ServiceLogonDiagnostics.Interpret(Evidence(system, false)),
            ServiceLogonDiagnostics.Interpret(Evidence(system + "," + system)),
            ServiceLogonDiagnostics.Interpret(Evidence(system.Replace("\"LocalSystem\"", "\"Future\""))),
            ServiceLogonDiagnostics.Interpret(Evidence(system.Replace("SystemSvc", "Bad/Name"))),
            ServiceLogonDiagnostics.Interpret(new ToolResult(1, "", "failed", false))
        }) Require(invalid.State == CheckState.Unknown && invalid.Repair is null,
            "Malformed, duplicate, excessive or failed service logon evidence remains unknown");
        Require(!clear.Detail.Contains("\\", StringComparison.Ordinal) && !clear.Detail.Contains("@", StringComparison.Ordinal),
            "Service logon summary excludes account and domain identities");
        Console.WriteLine("PASS: bounded service logon account classes, identity privacy and interactive-service coherence; no account or service changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
