using KillerBench.Engine.Core;

namespace KillerBench.Engine.Windows.Primitives;

/// <summary>Reports TPM readiness from Get-Tpm. Value is Ready, NotReady or Absent; Raw keeps the present, ready, enabled and owned flags only.</summary>
public sealed class WindowsTpmStatusProbe : IProbePrimitive
{
    public string Name => "tpm.status";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$t=Get-Tpm -ErrorAction Stop;[string]$t.TpmPresent+'|'+[string]$t.TpmReady+'|'+[string]$t.TpmEnabled+'|'+[string]$t.TpmOwned";

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("TPM inspection requires Windows.");
        return Interpret(await WindowsNetAdapterPrimitives.RunAsync(Script, token).ConfigureAwait(false));
    }

    public static PrimitiveResult Interpret(ToolResult result) => WindowsNetAdapterPrimitives.Lines(result, "Get-Tpm", lines =>
    {
        if (lines.Length != 1) return PrimitiveResult.Unavailable("TPM status output was not recognized.");
        var parts = lines[0].Split('|');
        if (parts.Length != 4 || parts.Any(p => p is not ("True" or "False"))) return PrimitiveResult.Unavailable("TPM status output was not recognized.");
        bool present = parts[0] == "True", ready = parts[1] == "True";
        string raw = "present=" + parts[0] + ";ready=" + parts[1] + ";enabled=" + parts[2] + ";owned=" + parts[3];
        return !present ? new PrimitiveResult(true, "Absent", raw) { Detail = "No TPM is visible to Windows. It may be missing or turned off in the firmware settings." }
            : ready ? new PrimitiveResult(true, "Ready", raw) { Detail = "The TPM is present and ready for BitLocker and Windows Hello." }
            : new PrimitiveResult(true, "NotReady", raw) { Detail = "The TPM is present but not ready. Windows can usually provision it without clearing it." };
    });
}

/// <summary>Provisions the TPM with Initialize-Tpm without -AllowClear or -AllowPhysicalPresence, so no existing TPM keys are cleared and no firmware prompt is queued.
/// A TPM that needs clearing or a physical-presence confirmation is reported, never forced.</summary>
public sealed class WindowsTpmInitializeAction : IActionPrimitive
{
    public string Name => "tpm.initialize";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => true;
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$t=Get-Tpm -ErrorAction Stop;if(-not $t.TpmPresent){'Absent';exit 0};if($t.TpmReady){'AlreadyReady';exit 0};" +
        "$r=Initialize-Tpm -ErrorAction Stop;[string]$r.TpmReady+'|'+[string]$r.RestartRequired+'|'+[string]$r.ShutdownRequired+'|'+[string]$r.ClearRequired+'|'+[string]$r.PhysicalPresenceRequired";

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "TPM changes require Windows.");
        return Interpret(await WindowsNetAdapterPrimitives.RunAsync(Script, token).ConfigureAwait(false));
    }

    public static ActionResult Interpret(ToolResult result)
    {
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new ActionResult(true, false, raw, "Initialize-Tpm timed out.") { TimedOut = true };
        if (result.Truncated) return new ActionResult(true, false, raw, "Initialize-Tpm output was truncated.");
        if (result.ExitCode != 0) return new ActionResult(false, false, raw, "Initialize-Tpm failed: " + result.Error.Trim());
        string text = result.Output.Trim();
        if (text == "AlreadyReady") return new ActionResult(false, true, raw, "The TPM was already ready; nothing changed.");
        if (text == "Absent") return new ActionResult(false, false, raw, "No TPM is visible to Windows. Turn it on in the firmware settings.");
        var parts = text.Split('|');
        if (parts.Length != 5 || parts.Any(p => p is not ("True" or "False"))) return new ActionResult(true, false, raw, "Initialize-Tpm output was not recognized.");
        if (parts[3] == "True") return new ActionResult(true, false, raw, "The TPM needs to be cleared before it can be used. KillerBench does not clear a TPM because that destroys keys held by BitLocker, Windows Hello and other software.");
        if (parts[4] == "True") return new ActionResult(true, false, raw, "The TPM needs a physical-presence confirmation at boot. Confirm it in the firmware settings, then check again.");
        bool restart = parts[1] == "True" || parts[2] == "True";
        return new ActionResult(true, parts[0] == "True" || restart, raw, parts[0] == "True" || restart ? null : "Initialize-Tpm finished but the TPM is not ready.") { RestartRequired = restart };
    }
}
