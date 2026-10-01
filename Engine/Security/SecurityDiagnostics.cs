using System.Text.Json;

namespace KillerBench.Engine.Security;

public sealed record SecurityOptions(bool Tpm = true, bool SecureBoot = true, bool Virtualization = true);

public static class SecurityDiagnostics
{
    public static RepairStep QueryStep(SecurityOptions? options = null)
    {
        options ??= new();
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
        "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; $t=$null; $b=$null; $g=$null; " +
        (options.Tpm ? "try { $x=Get-Tpm; $t=[pscustomobject]@{present=$x.TpmPresent; ready=$x.TpmReady} } catch {} " : "") +
        (options.SecureBoot ? "try { $b=Confirm-SecureBootUEFI } catch {} " : "") +
        (options.Virtualization ? "try { $x=Get-CimInstance -Namespace root/Microsoft/Windows/DeviceGuard -ClassName Win32_DeviceGuard -OperationTimeoutSec 15; $g=[pscustomobject]@{vbs=$x.VirtualizationBasedSecurityStatus; configured=@($x.SecurityServicesConfigured); running=@($x.SecurityServicesRunning)} } catch {} " : "") +
        "[pscustomobject]@{tpm=$t; secureBoot=$b; guard=$g} | ConvertTo-Json -Depth 4 -Compress"], true);
    }

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default, SecurityOptions? options = null)
    {
        options ??= new();
        token.ThrowIfCancellationRequested();
        var result = options.Tpm || options.SecureBoot || options.Virtualization
            ? await BoundedTool.RunAsync(QueryStep(options), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false)
            : new ToolResult(0, "{}", "", false);
        return ApplySelection(Interpret(result), options);
    }

    public static IReadOnlyList<WorkflowCheck> ApplySelection(IReadOnlyList<WorkflowCheck> checks, SecurityOptions options) =>
        [.. checks.Select((check, index) => (index == 0 ? options.Tpm : index == 1 ? options.SecureBoot : options.Virtualization)
            ? check : new WorkflowCheck(check.Name, CheckState.Information, "Not selected. This protection was not checked."))];

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult result)
    {
        string[] names = ["TPM readiness", "Secure Boot", "Virtualization-based security", "Memory integrity", "Credential Guard"];
        var checks = names.Select(n => new WorkflowCheck(n, CheckState.Unknown, "State unavailable, unsupported or access denied. No setting changed.", Evidence: result)).ToArray();
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return checks;
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return checks;
            if (root.TryGetProperty("tpm", out var tpm) && tpm.ValueKind == JsonValueKind.Object &&
                Boolean(tpm, "present") is bool present && Boolean(tpm, "ready") is bool ready && (present || !ready))
                checks[0] = new(names[0], present && ready ? CheckState.Clear : CheckState.Attention,
                    present && ready ? "Windows reports TPM present and ready. TPM version and attestation are not assessed."
                    : "Windows does not report a present, ready TPM. Review hardware and provisioning requirements. No TPM clear or initialization attempted.");
            if (Boolean(root, "secureBoot") is bool boot)
                checks[1] = new(names[1], boot ? CheckState.Clear : CheckState.Attention,
                    boot ? "Firmware reports Secure Boot enabled. Boot-chain attestation is not assessed."
                    : "Secure Boot is disabled. Review the required baseline and boot compatibility before firmware changes.");
            if (root.TryGetProperty("guard", out var guard) && guard.ValueKind == JsonValueKind.Object &&
                guard.TryGetProperty("vbs", out var state) && state.ValueKind == JsonValueKind.Number && state.TryGetInt32(out int vbs) && vbs is >= 0 and <= 2)
            {
                checks[2] = new(names[2], vbs == 2 ? CheckState.Clear : vbs == 1 ? CheckState.Attention : CheckState.Information,
                    vbs == 2 ? "Windows reports VBS running." : vbs == 1 ? "VBS is enabled but not running. Review prerequisites and startup events." : "VBS is not enabled. Compare with the required security baseline.");
                int[]? configured = Services(guard, "configured"), running = Services(guard, "running");
                if (configured is not null && running is not null)
                {
                    for (int index = 3; index <= 4; index++)
                    {
                        int code = index == 3 ? 2 : 1;
                        bool active = running.Contains(code), enabled = configured.Contains(code);
                        if (active && vbs != 2) continue;
                        checks[index] = new(names[index], active ? CheckState.Clear : enabled ? CheckState.Attention : CheckState.Information,
                            active ? "Windows reports this protection running." : enabled ? "Configured but not running. Review compatible drivers, prerequisites and startup events. No policy changed." : "Not reported as configured or running. Compare with the required security baseline. No policy changed.");
                    }
                }
            }
            return [.. checks.Select(check => check with { Evidence = result })];
        }
        catch (JsonException) { return checks; }
    }

    private static bool? Boolean(JsonElement obj, string name) => obj.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;
    private static int[]? Services(JsonElement obj, string name)
    {
        if (!obj.TryGetProperty(name, out var list) || list.ValueKind != JsonValueKind.Array || list.GetArrayLength() > 16) return null;
        var values = new List<int>();
        foreach (var value in list.EnumerateArray())
        {
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int number) || number is < 0 or > 7 || values.Contains(number)) return null;
            values.Add(number);
        }
        return values.Contains(0) && values.Count > 1 ? null : [.. values];
    }
}
