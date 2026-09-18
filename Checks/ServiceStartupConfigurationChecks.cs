using KillerBench.Engine;

internal static class ServiceStartupConfigurationChecks
{
    internal static void Run()
    {
        WorkflowCheck Inventory(string services, bool complete = true) => new("Installed services", CheckState.Information, "fixture",
            Evidence: new ToolResult(0, "{\"complete\":" + complete.ToString().ToLowerInvariant() + ",\"services\":[" + services + "]}", "", false));
        const string automatic = "{\"name\":\"AutoSvc\",\"displayName\":\"Automatic Service\",\"state\":\"Running\",\"startMode\":\"Auto\",\"delayedAutoStart\":true,\"serviceType\":\"Own Process\"}";
        const string manual = "{\"name\":\"ManualSvc\",\"displayName\":\"Manual Service\",\"state\":\"Stopped\",\"startMode\":\"Manual\",\"delayedAutoStart\":false,\"serviceType\":\"Own Process\"}";
        const string driver = "{\"name\":\"BootDriver\",\"displayName\":\"Boot Driver\",\"state\":\"Running\",\"startMode\":\"Boot\",\"delayedAutoStart\":false,\"serviceType\":\"Kernel Driver\"}";
        var clear = ServiceStartupConfigurationDiagnostics.Check(Inventory(automatic + "," + manual + "," + driver));
        Require(clear.State == CheckState.Information && clear.Repair is null && clear.Detail.Contains("No internally inconsistent", StringComparison.Ordinal),
            "Coherent automatic, demand-start and boot-driver modes remain informational");
        var nonDriverBoot = ServiceStartupConfigurationDiagnostics.Check(Inventory(automatic.Replace("\"Auto\"", "\"Boot\"").Replace("\"delayedAutoStart\":true", "\"delayedAutoStart\":false")));
        Require(nonDriverBoot.State == CheckState.Attention && nonDriverBoot.Repair is null && nonDriverBoot.Detail.Contains("driver-only", StringComparison.Ordinal),
            "Boot or system start on a non-driver requests owner review without selecting a startup change");
        var staleDelayed = ServiceStartupConfigurationDiagnostics.Check(Inventory(manual.Replace("\"delayedAutoStart\":false", "\"delayedAutoStart\":true")));
        Require(staleDelayed.State == CheckState.Attention && staleDelayed.Repair is null && staleDelayed.Detail.Contains("delayed-start flag", StringComparison.Ordinal),
            "Delayed-start outside an automatic user-mode service requests review");
        var unknown = ServiceStartupConfigurationDiagnostics.Check(Inventory(manual.Replace("\"Own Process\"", "\"Unknown\"")));
        Require(unknown.State == CheckState.Unknown && unknown.Repair is null, "Unknown service type keeps aggregate startup classification unknown");
        foreach (var invalid in new[]
        {
            ServiceStartupConfigurationDiagnostics.Check(Inventory(automatic, false)),
            ServiceStartupConfigurationDiagnostics.Check(Inventory(automatic + "," + automatic)),
            ServiceStartupConfigurationDiagnostics.Check(new("Installed services", CheckState.Unknown, "fixture")),
            ServiceStartupConfigurationDiagnostics.Check(new("Installed services", CheckState.Information, "fixture", Evidence: new(1, "", "failed", false)))
        }) Require(invalid.State == CheckState.Unknown && invalid.Repair is null,
            "Incomplete, duplicate, missing or failed service inventory keeps startup classification unknown");
        Console.WriteLine("PASS: service startup-mode coherence, driver-only modes, delayed-start semantics and no automatic configuration changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
