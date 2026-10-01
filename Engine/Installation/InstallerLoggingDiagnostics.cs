using Microsoft.Win32;
using System.Text.Json;

namespace KillerBench.Engine.Installation;

public sealed record InstallerLoggingEnvironment(object? Logging, object? DisableLoggingFromPackage, object? Debug);

public static class InstallerLoggingDiagnostics
{
    private static readonly HashSet<char> LoggingModes = [.. "iweaprcvmuox!"];

    public static WorkflowCheck Check(Func<InstallerLoggingEnvironment>? collect = null)
    {
        try
        {
            var state = (collect ?? Collect)();
            return state is null ? Unknown("Installer logging collector returned no observation.") : Assess(state);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            return Unknown("Windows Installer logging policy could not be read. Managed policy remains preserved.",
                new(-1, "", error.GetType().Name + ": " + error.Message, false));
        }
    }

    public static WorkflowCheck Assess(InstallerLoggingEnvironment state)
    {
        ArgumentNullException.ThrowIfNull(state);
        bool attention = false, unknown = false;
        var details = new List<string>();
        switch (state.Logging)
        {
            case null:
                details.Add("Machine Installer logging policy is not explicitly configured. Individual packages, commands or applications can still enable logging.");
                break;
            case string mode when ValidMode(mode):
                details.Add("Machine Installer logging policy is configured with mode letters: " + mode + ". Policy-created logs use the process temporary directory and random MSI log names.");
                if (mode.Contains('!'))
                    details.Add("Per-line log flushing is enabled and can slow an installation.");
                if (mode.Contains('x') || mode.Contains('X'))
                    details.Add("Extra debugging output is requested where the installed Windows Installer version supports it.");
                break;
            default:
                unknown = true;
                details.Add("Machine Installer Logging has an unrecognized value, type or mode character. Logging behavior is unconfirmed.");
                break;
        }
        switch (state.DisableLoggingFromPackage)
        {
            case null:
            case int value when value == 0:
                details.Add("Logging requested by an MSI package is not explicitly disabled by machine policy.");
                break;
            case int value when value == 1:
                attention = true;
                details.Add("Machine policy disables logging requested through an MSI package's MsiLogging property. Command-line and machine-policy logging can still apply.");
                break;
            default:
                unknown = true;
                details.Add("DisableLoggingFromPackage has an unrecognized value or type. Package-requested logging behavior is unconfirmed.");
                break;
        }
        switch (state.Debug)
        {
            case null:
            case int value when value == 0:
                details.Add("Installer debug policy is not enabled.");
                break;
            case int value when value is >= 1 and <= 7:
                attention = true;
                details.Add("Installer debug policy is enabled with value " + value + ". Debug output can expose detailed installation data.");
                if ((value & 4) != 0)
                    details.Add("The command-line logging bit is enabled. Values supplied on an Installer command line can appear in logs, including properties normally listed as hidden.");
                break;
            default:
                unknown = true;
                details.Add("Installer Debug has an unrecognized value or type. Debug logging behavior is unconfirmed.");
                break;
        }
        details.Add("Logging configuration does not prove that a log exists, identify a package failure or authorize policy changes. No policy, log, package, cache or Installer setting changed.");
        return new("Installer logging policy", unknown ? CheckState.Unknown : attention ? CheckState.Attention : CheckState.Information,
            string.Join("\n", details), Evidence: new(0, JsonSerializer.Serialize(state, Capture.JsonOptions), "", false));
    }

    private static bool ValidMode(string mode)
    {
        if (mode.Length is < 1 or > 16 || mode != mode.Trim()) return false;
        var seen = new HashSet<char>();
        foreach (char value in mode)
        {
            char normalized = char.ToLowerInvariant(value);
            if (!LoggingModes.Contains(normalized) || !seen.Add(normalized)) return false;
        }
        return true;
    }

    private static InstallerLoggingEnvironment Collect()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Installer logging checks require Windows.");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var policy = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\Installer");
        return new(policy?.GetValue("Logging"), policy?.GetValue("DisableLoggingFromPackage"), policy?.GetValue("Debug"));
    }

    private static WorkflowCheck Unknown(string detail, ToolResult? evidence = null) => new("Installer logging policy", CheckState.Unknown,
        detail + " No policy, log, package, cache or Installer setting changed.", Evidence: evidence);
}
