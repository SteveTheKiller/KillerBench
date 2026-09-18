using System.IO;
using KillerBench.Engine;

internal static class PeripheralOperationalCorrelationChecks
{
    public static void Run(string scratch)
    {
        foreach (var kind in new[] { WorkflowKind.Bluetooth, WorkflowKind.Camera, WorkflowKind.SmartCard, WorkflowKind.Biometric,
            WorkflowKind.WindowsImageAcquisitionService, WorkflowKind.HumanInterfaceDeviceService,
            WorkflowKind.PortableDeviceEnumeratorService, WorkflowKind.TextInputManagementService,
            WorkflowKind.DisplayEnhancementService, WorkflowKind.ShellHardwareDetectionService })
            Require(PeripheralOperationalCorrelation.Supports(kind), "Every supported peripheral workflow is discoverable");
        Require(!PeripheralOperationalCorrelation.Supports(WorkflowKind.Audio), "Specialized audio correlation remains separate");

        var clear = PeripheralOperationalCorrelation.Check(WorkflowKind.Bluetooth,
        [
            new("Bluetooth device: Fixture", CheckState.Clear, "fixture"),
            new("Bluetooth driver: Fixture", CheckState.Information, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(clear is { Name: "Bluetooth readiness", State: CheckState.Information, Repair: null } &&
            clear.Detail.Contains("2 device and driver results") && clear.Detail.Contains("No current configuration"),
            "Complete clear device and service evidence produces one bounded readiness conclusion");

        var attention = PeripheralOperationalCorrelation.Check(WorkflowKind.Camera,
        [
            new("Camera devices", CheckState.Attention, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(attention is { Name: "Camera readiness", State: CheckState.Attention, Repair: null } &&
            attention.Detail.Contains("Results needing attention: 1") && attention.Detail.Contains("selects no repair"),
            "A current device concern remains attention without widening repair authority");

        var incomplete = PeripheralOperationalCorrelation.Check(WorkflowKind.SmartCard,
        [
            new("SmartCard driver collection", CheckState.Unknown, "fixture"),
            new("Focused service readiness", CheckState.Attention, "fixture")
        ]);
        Require(incomplete is { Name: "Smart card readiness", State: CheckState.Unknown, Repair: null } &&
            incomplete.Detail.Contains("incomplete results: 1"), "Incomplete evidence takes precedence over attention");

        var textInput = PeripheralOperationalCorrelation.Check(WorkflowKind.TextInputManagementService,
        [
            new("Input device: Fixture", CheckState.Clear, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(textInput is { Name: "Text input readiness", State: CheckState.Information, Repair: null } &&
            textInput.Detail.Contains("handwriting") && textInput.Detail.Contains("changes no device"),
            "Shared input evidence produces a text-input-specific conclusion and boundary");

        var shellHardware = PeripheralOperationalCorrelation.Check(WorkflowKind.ShellHardwareDetectionService,
        [
            new("Storage device: Fixture disk", CheckState.Clear, "fixture"),
            new("Storage driver: Fixture disk", CheckState.Information, "fixture"),
            new("Portable devices", CheckState.Information, "fixture"),
            new("Focused service readiness", CheckState.Information, "fixture")
        ]);
        Require(shellHardware is { Name: "Shell hardware detection readiness", State: CheckState.Information, Repair: null } &&
            shellHardware.Detail.Contains("3 device and driver results") && shellHardware.Detail.Contains("AutoPlay policy"),
            "Storage and portable-device evidence produce one bounded Shell Hardware Detection conclusion");

        Require(PeripheralOperationalCorrelation.Check(WorkflowKind.Biometric,
            [new("Biometric devices", CheckState.Clear, "fixture")]).State == CheckState.Unknown &&
            PeripheralOperationalCorrelation.Check(WorkflowKind.Biometric,
            [new("Focused service readiness", CheckState.Information, "fixture")]).State == CheckState.Unknown,
            "Missing device or service evidence fails closed");
        try
        {
            PeripheralOperationalCorrelation.Check(WorkflowKind.Audio, []);
            throw new Exception("Unsupported peripheral workflow accepted");
        }
        catch (ArgumentOutOfRangeException) { }
        try
        {
            PeripheralOperationalCorrelation.Check(WorkflowKind.Bluetooth,
                [new("Bluetooth devices", CheckState.Clear, "fixture")], new CancellationToken(true));
            throw new Exception("Peripheral correlation cancellation ignored");
        }
        catch (OperationCanceledException) { }

        string folder = Path.Combine(scratch, "peripheral-correlation");
        var workflow = Workflows.RunAsync(WorkflowKind.Bluetooth, false, folder,
            host: new WorkflowHost()).GetAwaiter().GetResult();
        Require(workflow.Checks.Single(check => check.Name == "Bluetooth readiness") is
            { State: CheckState.Information, Repair: null } && workflow.Repairs.Count == 0 &&
            workflow.Checks.Count(check => check.Name == "Bluetooth readiness") == 1,
            "The Bluetooth workflow appends exactly one peripheral conclusion without executing repair");

        Console.WriteLine("PASS: peripheral device, driver, service and exact failure-history evidence produce conservative workflow conclusions.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private sealed class WorkflowHost : IWorkflowHost
    {
        public Task<IReadOnlyList<WorkflowCheck>> InspectAsync(WorkflowKind kind, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<WorkflowCheck>>([
                new("Bluetooth devices", CheckState.Clear, "fixture"),
                new("Bluetooth Support Service failure history", CheckState.Information, "fixture")
            ]);

        public Task<WorkflowCheck> CheckIntegrityAsync(RepairKind kind, IProgress<string>? progress, CancellationToken token) =>
            Task.FromResult(new WorkflowCheck(kind.ToString(), CheckState.Clear, "fixture"));

        public Task<RepairResult> RepairAsync(RepairKind kind, string folder, IProgress<string>? progress, CancellationToken token) =>
            throw new Exception("Read-only peripheral workflow dispatched a repair");
    }
}
