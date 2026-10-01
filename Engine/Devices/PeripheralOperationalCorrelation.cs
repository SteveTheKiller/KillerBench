namespace KillerBench.Engine.Devices;

public static class PeripheralOperationalCorrelation
{
    public static bool Supports(WorkflowKind kind) => kind is WorkflowKind.Bluetooth or WorkflowKind.Camera or
        WorkflowKind.SmartCard or WorkflowKind.Biometric or WorkflowKind.WindowsImageAcquisitionService or
        WorkflowKind.HumanInterfaceDeviceService or WorkflowKind.PortableDeviceEnumeratorService or
        WorkflowKind.TextInputManagementService or WorkflowKind.DisplayEnhancementService or
        WorkflowKind.ShellHardwareDetectionService;

    public static WorkflowCheck Check(WorkflowKind kind, IReadOnlyList<WorkflowCheck> checks,
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(checks);
        token.ThrowIfCancellationRequested();
        var (title, evidencePrefixes, excludedBehavior) = Definition(kind);
        const string summaryName = "Focused service readiness";
        string boundary = " This conclusion combines current Plug and Play configuration, bounded driver inventory, exact service availability and retained Service Control Manager history. It does not prove " +
            excludedBehavior + ". It identifies no root cause, selects no repair and changes no device, driver, service configuration, permission, policy, credential or user data.";
        WorkflowCheck Unknown(string detail) => new(title + " readiness", CheckState.Unknown, detail + boundary);

        if (checks.Count == 0 || checks.Any(check => check is null))
            return Unknown("Peripheral correlation evidence is missing or invalid.");

        var service = checks.Where(check => check.Name == summaryName).ToArray();
        var devices = checks.Where(check => IsDeviceEvidence(check.Name, evidencePrefixes)).ToArray();
        if (service.Length != 1 || devices.Length == 0 ||
            devices.Select(check => check.Name).Any(string.IsNullOrWhiteSpace))
            return Unknown("The expected device or focused-service conclusion was missing or duplicated.");

        int incomplete = devices.Count(check => check.EffectiveState == CheckState.Unknown) +
            (service[0].EffectiveState == CheckState.Unknown ? 1 : 0);
        int attention = devices.Count(check => check.EffectiveState is CheckState.Attention or CheckState.Repairable) +
            (service[0].EffectiveState is CheckState.Attention or CheckState.Repairable ? 1 : 0);
        string detail = $"Reviewed {devices.Length} device and driver result{(devices.Length == 1 ? "" : "s")} plus the exact service conclusion. Results needing attention: {attention}; incomplete results: {incomplete}. ";

        if (incomplete > 0)
            return Unknown(detail + "Recollect the incomplete device, driver or service evidence before drawing a peripheral readiness conclusion.");
        if (attention > 0)
            return new(title + " readiness", CheckState.Attention,
                detail + "Review the current device and service findings. Historical service failures alone do not establish a current device outage." + boundary);
        return new(title + " readiness", CheckState.Information,
            detail + "No current configuration or service-availability issue was established within the checked scope." + boundary);
    }

    private static bool IsDeviceEvidence(string name, IReadOnlyList<string> prefixes) =>
        prefixes.Any(prefix => name.StartsWith(prefix + " ", StringComparison.Ordinal)) ||
        name == "Device driver references";

    private static (string Title, IReadOnlyList<string> EvidencePrefixes, string ExcludedBehavior) Definition(WorkflowKind kind) => kind switch
    {
        WorkflowKind.Bluetooth => ("Bluetooth", ["Bluetooth"], "radio state, discovery, pairing, audio, data transfer or physical device operation"),
        WorkflowKind.Camera => ("Camera", ["Camera"], "privacy permission, image capture, application access or physical camera operation"),
        WorkflowKind.SmartCard => ("Smart card", ["SmartCard"], "card presence, certificate validity, PIN validation, authentication or reader operation"),
        WorkflowKind.Biometric => ("Biometric", ["Biometric"], "sensor capture, enrollment, matching, sign-in or physical sensor operation"),
        WorkflowKind.WindowsImageAcquisitionService => ("Image acquisition", ["Camera"], "scanner or camera discovery, image capture, transfer, application access or physical device operation"),
        WorkflowKind.HumanInterfaceDeviceService => ("Human interface device", ["Input"], "key, button, pointer, accessibility feature or physical input operation"),
        WorkflowKind.PortableDeviceEnumeratorService => ("Portable device", ["Portable"], "device discovery, file access, synchronization, transfer or physical device operation"),
        WorkflowKind.TextInputManagementService => ("Text input", ["Input"], "text entry, touch keyboard, handwriting, recognition, language behavior or physical input operation"),
        WorkflowKind.DisplayEnhancementService => ("Display enhancement", ["Display"], "brightness, color, HDR, enhancement behavior, picture quality or physical display operation"),
        WorkflowKind.ShellHardwareDetectionService => ("Shell hardware detection", ["Storage", "Portable"], "AutoPlay policy, handler selection, notification delivery, media readiness, volume access or physical device operation"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
