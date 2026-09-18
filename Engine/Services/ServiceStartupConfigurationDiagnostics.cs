namespace KillerBench.Engine;

public static class ServiceStartupConfigurationDiagnostics
{
    private const string Scope = " Disabled and demand-start services can be intentional. This check does not infer desired startup behavior from service state, event history or product role. No service, driver, startup setting, dependency, trigger, recovery action, policy or registry value changed.";

    public static WorkflowCheck Check(WorkflowCheck inventory, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        token.ThrowIfCancellationRequested();
        WorkflowCheck Unknown(string detail) => new("Service startup configuration", CheckState.Unknown,
            detail + Scope, Evidence: inventory.Evidence);
        if (inventory.EffectiveState == CheckState.Unknown || inventory.Evidence is null)
            return Unknown("A complete installed-service inventory is required before startup configuration can be classified.");
        try
        {
            var snapshot = ServiceInventoryDiagnostics.Parse(inventory.Evidence.Output);
            if (!snapshot.Complete) return Unknown("The installed-service inventory exceeded its bound or was incomplete.");
            int automatic = snapshot.Services.Count(service => service.StartMode == "Auto");
            int manual = snapshot.Services.Count(service => service.StartMode == "Manual");
            int disabled = snapshot.Services.Count(service => service.StartMode == "Disabled");
            int boot = snapshot.Services.Count(service => service.StartMode == "Boot");
            int system = snapshot.Services.Count(service => service.StartMode == "System");
            int delayed = snapshot.Services.Count(service => service.DelayedAutoStart);
            int unknown = snapshot.Services.Count(service => service.StartMode == "Unknown" || service.ServiceType == "Unknown");
            int bootOrSystemNonDriver = snapshot.Services.Count(service =>
                (service.StartMode is "Boot" or "System") &&
                !IsDriver(service.ServiceType) && service.ServiceType != "Unknown");
            int invalidDelayed = snapshot.Services.Count(service => service.DelayedAutoStart && (service.StartMode != "Auto" || IsDriver(service.ServiceType)));
            string summary = $"Reviewed {snapshot.Services.Count} installed service startup configurations: {automatic} automatic, {manual} manual, {disabled} disabled, {boot} boot, {system} system, and {delayed} delayed automatic. {unknown} records have an unknown startup mode or service type.";
            if (unknown > 0) return Unknown(summary + " Complete startup configuration classification could not be established.");
            if (bootOrSystemNonDriver > 0 || invalidDelayed > 0)
                return new("Service startup configuration", CheckState.Attention,
                    summary + $" {bootOrSystemNonDriver} non-driver service{(bootOrSystemNonDriver == 1 ? " uses" : "s use")} a driver-only boot or system mode, and {invalidDelayed} record{(invalidDelayed == 1 ? " has" : "s have")} a delayed-start flag outside an automatic user-mode service. Review the owning package and effective policy before changing startup configuration." + Scope,
                    Evidence: inventory.Evidence);
            return new("Service startup configuration", CheckState.Information,
                summary + " No internally inconsistent startup-mode combination was established within this bounded inventory." + Scope,
                Evidence: inventory.Evidence);
        }
        catch (Exception error) when (error is System.Text.Json.JsonException or InvalidDataException or InvalidOperationException)
        {
            return Unknown("Installed-service startup configuration evidence was malformed (" + error.GetType().Name + ").");
        }
    }

    private static bool IsDriver(string serviceType) => serviceType.Contains("Driver", StringComparison.OrdinalIgnoreCase);
}
