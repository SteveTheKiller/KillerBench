using System.Globalization;
using System.Text.RegularExpressions;
using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>Shared helpers for network adapter primitives. Adapter names come from Get-NetAdapter and are validated before use.</summary>
public static class WindowsNetAdapterPrimitives
{
    public const string AdapterNamePattern = @"[A-Za-z0-9][A-Za-z0-9 ()#._-]{0,127}";
    public static readonly PrimitiveParameter NameParameter = new("name", true, "Exact adapter name as Get-NetAdapter reports it") { Pattern = AdapterNamePattern };

    internal static Task<ToolResult> RunAsync(string script, CancellationToken token, params string[] arguments) =>
        NativeTool.RunAsync(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script, .. arguments], token: token);

    internal static PrimitiveResult Lines(ToolResult result, string tool, Func<string[], PrimitiveResult> interpret)
    {
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new PrimitiveResult(false, null, raw, tool + " timed out.") { TimedOut = true };
        if (result.Truncated) return new PrimitiveResult(false, null, raw, tool + " output was truncated.") { Truncated = true };
        if (result.ExitCode != 0) return PrimitiveResult.Unavailable(tool + " exited with " + result.ExitCode + ": " + result.Error.Trim());
        return interpret(result.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}

/// <summary>Counts physical adapters that are administratively disabled. Value is the count; Raw lists each physical adapter with its status.</summary>
public sealed class WindowsDisabledAdaptersProbe : IProbePrimitive
{
    public string Name => "netadapter.disabled-physical";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "Get-NetAdapter -Physical -IncludeHidden:$false|ForEach-Object{$_.Name+'|'+$_.Status+'|'+$_.AdminStatus}";

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Adapter inspection requires Windows.");
        return Interpret(await WindowsNetAdapterPrimitives.RunAsync(Script, token).ConfigureAwait(false));
    }

    public static PrimitiveResult Interpret(ToolResult result) => WindowsNetAdapterPrimitives.Lines(result, "Get-NetAdapter", lines =>
    {
        int disabled = 0;
        foreach (string line in lines)
        {
            var parts = line.Split('|');
            if (parts.Length != 3 || !Regex.IsMatch(parts[0], "^" + WindowsNetAdapterPrimitives.AdapterNamePattern + "$")) return PrimitiveResult.Unavailable("Adapter inventory line was not recognized.");
            if (parts[1].Equals("Disabled", StringComparison.OrdinalIgnoreCase) || parts[2].Equals("Down", StringComparison.OrdinalIgnoreCase)) disabled++;
        }
        return new PrimitiveResult(true, disabled.ToString(CultureInfo.InvariantCulture), string.Join("\n", lines))
        { Detail = lines.Length == 0 ? "No physical adapters were reported." : disabled == 0 ? "All " + lines.Length + " physical adapter(s) are enabled." : disabled + " of " + lines.Length + " physical adapter(s) are disabled." };
    });
}

/// <summary>Reports one physical adapter's administrative status. Value is Up or Down.</summary>
public sealed class WindowsAdapterAdminStatusProbe : IProbePrimitive
{
    public string Name => "netadapter.admin-status";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsNetAdapterPrimitives.NameParameter];
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "& { param([string]$name) $a=@(Get-NetAdapter -Name $name -Physical -ErrorAction Stop); if($a.Count -ne 1){throw 'Exact physical adapter not found.'}; [string]$a[0].AdminStatus }";

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Adapter inspection requires Windows.");
        return Interpret(await WindowsNetAdapterPrimitives.RunAsync(Script, token, parameters["name"]).ConfigureAwait(false));
    }

    public static PrimitiveResult Interpret(ToolResult result) => WindowsNetAdapterPrimitives.Lines(result, "Get-NetAdapter", lines =>
        lines.Length == 1 && lines[0] is "Up" or "Down"
            ? new PrimitiveResult(true, lines[0], lines[0]) { Detail = lines[0] == "Up" ? "The adapter is enabled." : "The adapter is administratively disabled." }
            : PrimitiveResult.Unavailable("Adapter status output was not recognized."));
}

/// <summary>Reports whether Windows may turn off one adapter to save power. Value is Enabled or Disabled.</summary>
public sealed class WindowsAdapterPowerSavingProbe : IProbePrimitive
{
    public string Name => "netadapter.power-saving";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsNetAdapterPrimitives.NameParameter];
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "& { param([string]$name) $p=Get-NetAdapterPowerManagement -Name $name -ErrorAction Stop; [string]$p.AllowComputerToTurnOffDevice }";

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Adapter inspection requires Windows.");
        return Interpret(await WindowsNetAdapterPrimitives.RunAsync(Script, token, parameters["name"]).ConfigureAwait(false));
    }

    public static PrimitiveResult Interpret(ToolResult result) => WindowsNetAdapterPrimitives.Lines(result, "Get-NetAdapterPowerManagement", lines =>
        lines.Length == 1 && lines[0] is "Enabled" or "Disabled" or "Unsupported"
            ? new PrimitiveResult(true, lines[0], lines[0]) { Detail = lines[0] switch
                {
                    "Enabled" => "Windows may turn the adapter off to save power, which can drop connections after idle or sleep.",
                    "Disabled" => "Windows keeps the adapter powered.",
                    _ => "The adapter does not support power management."
                } }
            : PrimitiveResult.Unavailable("Power management output was not recognized."));
}

/// <summary>Enables one physical adapter that is administratively disabled.</summary>
public sealed class WindowsAdapterEnableAction : IActionPrimitive
{
    public string Name => "netadapter.enable";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsNetAdapterPrimitives.NameParameter];
    public RecipeRisk Risk => RecipeRisk.Reversible;
    public bool RequiresElevation => true;
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "& { param([string]$name) $a=Get-NetAdapter -Name $name -Physical -ErrorAction Stop; if($a.Count -ne 1){throw 'Exact adapter not found.'}; " +
        "if($a.AdminStatus -ne 'Down'){'AlreadyEnabled';exit 0}; Enable-NetAdapter -Name $name -Confirm:$false -ErrorAction Stop; 'Enabled' }";

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "Adapter changes require Windows.");
        var result = await WindowsNetAdapterPrimitives.RunAsync(Script, token, parameters["name"]).ConfigureAwait(false);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new ActionResult(true, false, raw, "Enable-NetAdapter timed out.") { TimedOut = true };
        if (result.ExitCode != 0) return new ActionResult(false, false, raw, "Enable-NetAdapter failed: " + result.Error.Trim());
        string text = result.Output.Trim();
        return text == "AlreadyEnabled" ? new ActionResult(false, true, raw, "The adapter was already enabled; nothing changed.")
            : new ActionResult(true, text == "Enabled", raw, text == "Enabled" ? null : "Unexpected output.");
    }
}

/// <summary>Stops Windows from turning one adapter off to save power.</summary>
public sealed class WindowsAdapterPowerSavingDisableAction : IActionPrimitive
{
    public string Name => "netadapter.power-saving-disable";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsNetAdapterPrimitives.NameParameter];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => true;
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "& { param([string]$name) $p=Get-NetAdapterPowerManagement -Name $name -ErrorAction Stop; if($p.AllowComputerToTurnOffDevice -ne 'Enabled'){'AlreadyDisabled';exit 0}; " +
        "Set-NetAdapterPowerManagement -Name $name -AllowComputerToTurnOffDevice Disabled -ErrorAction Stop; 'Disabled' }";

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "Adapter changes require Windows.");
        var result = await WindowsNetAdapterPrimitives.RunAsync(Script, token, parameters["name"]).ConfigureAwait(false);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new ActionResult(true, false, raw, "Set-NetAdapterPowerManagement timed out.") { TimedOut = true };
        if (result.ExitCode != 0) return new ActionResult(false, false, raw, "Set-NetAdapterPowerManagement failed: " + result.Error.Trim());
        string text = result.Output.Trim();
        return text == "AlreadyDisabled" ? new ActionResult(false, true, raw, "Power saving was already disabled; nothing changed.")
            : new ActionResult(true, text == "Disabled", raw, text == "Disabled" ? null : "Unexpected output.");
    }
}
