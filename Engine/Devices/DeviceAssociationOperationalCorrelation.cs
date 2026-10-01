namespace KillerBench.Engine.Devices;

public static class DeviceAssociationOperationalCorrelation
{
    private const string SetupName = "Recent device setup events";
    private const string PnpName = "Recent Plug and Play configuration events";
    private const string ServiceName = "Focused service readiness";

    public static WorkflowCheck Check(IReadOnlyList<WorkflowCheck> checks, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        const string boundary = " This conclusion combines bounded Device Setup Manager and Plug and Play configuration history with the exact Device Association Service conclusion. Historical events do not prove a current device fault. It does not prove discovery, pairing, association, driver installation or physical device operation. It identifies no root cause, selects no repair and changes no device, driver, service configuration, permission, policy, credential or user data.";
        static WorkflowCheck Unknown(string detail) => new("Device association readiness", CheckState.Unknown, detail + boundary);

        if (checks.Count == 0 || checks.Any(check => check is null))
            return Unknown("Device association correlation evidence is missing or invalid.");

        var setup = checks.Where(check => check.Name == SetupName).ToArray();
        var pnp = checks.Where(check => check.Name == PnpName).ToArray();
        var service = checks.Where(check => check.Name == ServiceName).ToArray();
        if (setup.Length != 1 || pnp.Length != 1 || service.Length != 1)
            return Unknown("The expected device setup, Plug and Play or focused-service conclusion was missing or duplicated.");

        var evidence = new[] { setup[0], pnp[0], service[0] };
        int incomplete = evidence.Count(check => check.EffectiveState == CheckState.Unknown);
        int historical = setup.Concat(pnp).Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable);
        bool serviceAttention = service[0].EffectiveState is CheckState.Attention or CheckState.Repairable;
        string counts = $"Reviewed both retained event histories and the exact service conclusion. Historical results needing review: {historical}; incomplete results: {incomplete}. ";

        if (incomplete > 0)
            return Unknown(counts + "Recollect the incomplete event or service evidence before drawing a device association conclusion.");
        if (serviceAttention)
            return new("Device association readiness", CheckState.Attention,
                counts + "The current exact service conclusion needs attention. Review it together with matching event and device identities." + boundary);
        return new("Device association readiness", CheckState.Information,
            counts + (historical > 0
                ? "The service is currently available within the checked scope. Review retained warning and error records against the exact affected device before taking action."
                : "No current service-availability issue or retained setup warning or error was established within the checked scope.") + boundary);
    }
}
