using KillerBench.Engine;

internal static class ServicingServiceStateChecks
{
    internal static async Task RunAsync()
    {
        static ToolResult State(string name, int state, string label) => new(0,
            $"SERVICE_NAME: {name}\r\n        STATE              : {state}  {label}\r\n", "", false);
        var services = ServicingServiceStateDiagnostics.Services;
        Require(services.Count == 6 && services.Select(service => service.Name).SequenceEqual(
            ["wuauserv", "BITS", "CryptSvc", "TrustedInstaller", "WaaSMedicSvc", "UsoSvc"], StringComparer.Ordinal),
            "Servicing state catalog retains the exact six-service chain");
        Require(ServicingServiceStateDiagnostics.Interpret(services[4], State("WaaSMedicSvc", 1, "STOPPED"), 3) is
            { State: CheckState.Information, Repair: null } idle && idle.Detail.Contains("can be normal while idle"),
            "Stopped demand-start Medic state remains informational and cannot select repair");
        Require(ServicingServiceStateDiagnostics.Interpret(services[5], State("UsoSvc", 1, "STOPPED"), 2) is
            { State: CheckState.Attention, Repair: null },
            "Stopped automatic Orchestrator state requests review without selecting repair");
        Require(ServicingServiceStateDiagnostics.Interpret(services[0], State("wuauserv", 4, "RUNNING"), 2) is
            { State: CheckState.Information, Repair: null } running && running.Detail.Contains("does not prove"),
            "Running service state remains bounded evidence rather than a success claim");
        Require(ServicingServiceStateDiagnostics.Interpret(services[0], State("BITS", 4, "RUNNING"), 2).State == CheckState.Unknown &&
            ServicingServiceStateDiagnostics.Interpret(services[0], State("wuauserv", 4, "RUNNING") with { Truncated = true }, 2).State == CheckState.Unknown &&
            ServicingServiceStateDiagnostics.Interpret(services[0], State("wuauserv", 4, "RUNNING"), null).State == CheckState.Unknown,
            "Mismatched, truncated and incomplete service evidence remains unknown");
        var called = new List<string>();
        var checks = await ServicingServiceStateDiagnostics.CheckAsync(inspect: (service, token) =>
        {
            token.ThrowIfCancellationRequested();
            called.Add(service.Name);
            return Task.FromResult((State(service.Name, 4, "RUNNING"), (int?)2));
        });
        Require(checks.Count == 6 && called.SequenceEqual(services.Select(service => service.Name)) &&
            checks.All(check => check is { State: CheckState.Information, Repair: null, Evidence: not null }),
            "Bounded servicing state collection inspects each exact identity once and never selects repair");
        called.Clear();
        var selected = await ServicingServiceStateDiagnostics.CheckSelectedAsync(["WaaSMedicSvc"], inspect: (service, token) =>
        {
            token.ThrowIfCancellationRequested();
            called.Add(service.Name);
            return Task.FromResult((State(service.Name, 1, "STOPPED"), (int?)3));
        });
        Require(selected is [{ Name: "Windows Update Medic Service state", State: CheckState.Information, Repair: null }] &&
            called.SequenceEqual(["WaaSMedicSvc"]),
            "Focused collection reads only the selected exact servicing service");
        try
        {
            await ServicingServiceStateDiagnostics.CheckSelectedAsync(["UnknownSvc"], inspect: (_, _) =>
                Task.FromResult((State("UnknownSvc", 4, "RUNNING"), (int?)2)));
            throw new InvalidOperationException("Unsupported servicing state selection was accepted");
        }
        catch (ArgumentException) { }
        Console.WriteLine("PASS: servicing-chain current states are exact, bounded and read-only.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
