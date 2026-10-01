namespace KillerBench.Engine.Printing;

public static class PrintingEnvironmentCorrelation
{
    public static WorkflowCheck Check(WorkflowCheck pointAndPrint, WorkflowCheck rendering,
        IReadOnlyList<WorkflowCheck> drivers, IReadOnlyList<WorkflowCheck> driverFiles, WorkflowCheck driverModels,
        IReadOnlyList<WorkflowCheck> ports, IReadOnlyList<WorkflowCheck> endpoints,
        IReadOnlyList<WorkflowCheck> reachability, WorkflowCheck extensions,
        IReadOnlyList<WorkflowCheck> extensionFiles, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(pointAndPrint);
        ArgumentNullException.ThrowIfNull(rendering);
        ArgumentNullException.ThrowIfNull(drivers);
        ArgumentNullException.ThrowIfNull(driverFiles);
        ArgumentNullException.ThrowIfNull(driverModels);
        ArgumentNullException.ThrowIfNull(ports);
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(reachability);
        ArgumentNullException.ThrowIfNull(extensions);
        ArgumentNullException.ThrowIfNull(extensionFiles);
        token.ThrowIfCancellationRequested();

        const string name = "Printing environment correlation";
        const string boundary = " This is bounded registration, policy, file and configured-endpoint evidence. It does not render a document, submit a job, query supplies, identify a physical printer, prove queue acceptance or establish application output. No policy, driver, package, file, processor, monitor, port, queue, job or print setting changed, and no repair is selected.";
        static WorkflowCheck Unknown(string detail) => new(name, CheckState.Unknown, detail + boundary);

        if (pointAndPrint.Name != "Point and Print policy" || rendering.Name != "Print rendering and isolation policy" ||
            driverModels.Name != "Printer driver models" || extensions.Name != "Print processors and monitors" ||
            !ValidGroup(drivers, 256, item => item.Name is "Print queues" or "Print queue driver inventory" || item.Name.StartsWith("Print queue driver: ", StringComparison.Ordinal)) ||
            !ValidGroup(driverFiles, 256, item => item.Name == "Printer driver files" || item.Name.StartsWith("Printer driver files: ", StringComparison.Ordinal)) ||
            !ValidGroup(ports, 256, item => item.Name is "Printer ports" or "Printer port inventory" || item.Name.StartsWith("Printer port: ", StringComparison.Ordinal)) ||
            !ValidGroup(endpoints, 256, item => item.Name == "Printer TCP/IP endpoints" || item.Name.StartsWith("Printer endpoint: ", StringComparison.Ordinal)) ||
            !ValidGroup(reachability, 16, item => item.Name == "Printer endpoint reachability" || item.Name.StartsWith("Printer endpoint reachability: ", StringComparison.Ordinal)) ||
            !ValidGroup(extensionFiles, 384, item => item.Name == "Print extension files" || item.Name.StartsWith("Print processor file: ", StringComparison.Ordinal) || item.Name.StartsWith("Print monitor file: ", StringComparison.Ordinal)))
            return Unknown("Printing environment inputs were missing, duplicated, exceeded their bounds or carried an unexpected identity.");

        var inputs = new[] { pointAndPrint, rendering, driverModels, extensions }
            .Concat(drivers).Concat(driverFiles).Concat(ports).Concat(endpoints).Concat(reachability).Concat(extensionFiles).ToArray();
        int incomplete = inputs.Count(item => item.EffectiveState == CheckState.Unknown);
        int attention = inputs.Count(item => item.EffectiveState is CheckState.Attention or CheckState.Repairable);
        string detail = $"Reviewed print policy, rendering isolation, {drivers.Count} queue-driver result{(drivers.Count == 1 ? "" : "s")}, {driverFiles.Count} driver-file result{(driverFiles.Count == 1 ? "" : "s")}, driver models, {ports.Count} port result{(ports.Count == 1 ? "" : "s")}, {endpoints.Count} endpoint result{(endpoints.Count == 1 ? "" : "s")}, {reachability.Count} reachability result{(reachability.Count == 1 ? "" : "s")}, extension registration and {extensionFiles.Count} extension-file result{(extensionFiles.Count == 1 ? "" : "s")}. Correlated evidence: {attention} need attention and {incomplete} incomplete. ";
        if (incomplete > 0)
            return Unknown(detail + "Recollect incomplete evidence before choosing a printing action.");
        if (attention > 0)
            return new(name, CheckState.Attention, detail + "Review the exact policy, registration, file or endpoint results before changing the print environment." + boundary);
        return new(name, CheckState.Information, detail + "No environment contradiction was established within the collected scope." + boundary);
    }

    private static bool ValidGroup(IReadOnlyList<WorkflowCheck> checks, int maximum, Func<WorkflowCheck, bool> identity) =>
        checks.Count is > 0 && checks.Count <= maximum && checks.All(item => item is not null && identity(item));
}
