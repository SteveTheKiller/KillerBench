namespace KillerBench.Engine;

public enum WindowsRecoveryState { Enabled, Disabled, Unknown }

public sealed record WindowsRecoveryStatus(WindowsRecoveryState State, string? Location, string? BootConfigurationId);

public static class WindowsRecoveryDiagnostics
{
    public static RepairStep Step() => new("reagentc.exe", ["/info"], true);
    public static RepairStep EnableStep() => new("reagentc.exe", ["/enable"], false);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Recovery Environment inspection requires Windows.");
        return Interpret(await BoundedTool.RunAsync(Step(), TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        WorkflowCheck Unknown(string detail = "Windows Recovery Environment status was unavailable, incomplete or not recognized. No recovery configuration was changed.") =>
            new("Windows Recovery Environment", CheckState.Unknown, detail, Evidence: result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error) || result.Output.Length > 32768)
            return Unknown();

        string[] lines = result.Output.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (string raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("REAGENTC.EXE:", StringComparison.OrdinalIgnoreCase)) continue;
            int separator = line.IndexOf(':');
            if (separator <= 0) continue;
            string name = line[..separator].Trim();
            string value = line[(separator + 1)..].Trim();
            if (name.Length > 128 || value.Length > 4096 || !fields.TryAdd(name, value)) return Unknown();
        }

        if (!fields.TryGetValue("Windows RE status", out string? status))
            return Unknown("Windows reported recovery information in an unrecognized or localized format. The raw bounded output was retained and no recovery configuration was changed.");
        WindowsRecoveryState state = status.Equals("Enabled", StringComparison.OrdinalIgnoreCase) ? WindowsRecoveryState.Enabled :
            status.Equals("Disabled", StringComparison.OrdinalIgnoreCase) ? WindowsRecoveryState.Disabled : WindowsRecoveryState.Unknown;
        if (state == WindowsRecoveryState.Unknown) return Unknown();

        fields.TryGetValue("Windows RE location", out string? location);
        fields.TryGetValue("Boot Configuration Data (BCD) identifier", out string? identifier);
        location = string.IsNullOrWhiteSpace(location) ? null : location;
        identifier = string.IsNullOrWhiteSpace(identifier) ? null : identifier;
        if (location?.Any(char.IsControl) == true || identifier?.Any(char.IsControl) == true) return Unknown();

        var evidence = new WindowsRecoveryStatus(state, location, identifier);
        string detail = state == WindowsRecoveryState.Enabled
            ? "Windows reports that Windows Recovery Environment is enabled."
            : "Windows reports that Windows Recovery Environment is disabled. Recovery and advanced startup options may be unavailable until an administrator reviews the configuration.";
        if (location is not null) detail += "\nConfigured location: " + location;
        if (identifier is not null) detail += "\nBCD identifier: " + identifier;
        detail += "\nThis status does not verify that the recovery image can boot or repair this PC. No enable, disable, boot or image-location command was run.";
        return new("Windows Recovery Environment", state == WindowsRecoveryState.Enabled ? CheckState.Clear : CheckState.Repairable,
            detail, state == WindowsRecoveryState.Disabled ? RepairKind.WindowsRecoveryEnvironment : null,
            result with { Output = System.Text.Json.JsonSerializer.Serialize(evidence) + "\n\n" + result.Output });
    }
}
