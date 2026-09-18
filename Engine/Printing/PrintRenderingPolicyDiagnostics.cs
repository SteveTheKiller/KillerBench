using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record PrintRenderingPolicySnapshot(
    object? ApplicationDriverIsolation,
    object? PrintDriverIsolationExecutionPolicy,
    object? PrintDriverIsolationOverrideCompat,
    object? ForceServerRendering,
    object? ForceSoftwareRasterization);

public static class PrintRenderingPolicyDiagnostics
{
    private const string Name = "Print rendering and isolation policy";
    private const string Scope = " These machine policies are configuration evidence, not proof of a spooler, application, driver, queue, document or physical-output fault. Other driver and system policy can alter effective behavior. No policy, registry value, process, driver, queue, job or print setting changed.";

    public static WorkflowCheck Check(Func<PrintRenderingPolicySnapshot>? collect = null)
    {
        try
        {
            if (collect is null && !OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Print rendering policy inspection requires Windows.");
            var snapshot = (collect ?? Collect)();
            return snapshot is null ? Unknown("Print rendering policy collection returned no observation.") : Assess(snapshot);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            return Unknown("Print rendering and isolation policy could not be read.",
                new(-1, "", error.GetType().Name + ": " + error.Message, false));
        }
    }

    public static WorkflowCheck Assess(PrintRenderingPolicySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        bool attention = false, unknown = false;
        var details = new List<string>();

        if (snapshot is { ApplicationDriverIsolation: null, PrintDriverIsolationExecutionPolicy: null,
            PrintDriverIsolationOverrideCompat: null, ForceServerRendering: null, ForceSoftwareRasterization: null })
            details.Add("No recognized print rendering or isolation policy is explicitly configured. Windows defaults and other policy still apply.");
        else
        {
            DescribeIsolation("Application driver isolation", snapshot.ApplicationDriverIsolation,
                "Eligible applications isolate print driver components from their own processes.",
                "Print driver components load in associated application processes. A driver failure can therefore affect those applications.");
            DescribeIsolation("Spooler driver isolation", snapshot.PrintDriverIsolationExecutionPolicy,
                "The spooler executes compatible print drivers in isolated processes by default.",
                "The spooler executes print drivers in its own process. A driver failure can therefore stop the Print Spooler service.");
            DescribeFlag("Isolation compatibility override", snapshot.PrintDriverIsolationOverrideCompat,
                "The spooler may isolate drivers that do not explicitly report compatibility, unless they opt out.",
                "The spooler uses each driver's reported isolation compatibility.");
            DescribeFlag("Always render on the print server", snapshot.ForceServerRendering,
                "The machine policy requests server-side rendering for applicable shared-printer jobs.",
                "The machine policy does not force server-side rendering. Driver and connection conditions can still determine where rendering occurs.");
            DescribeFlag("Force software rasterization", snapshot.ForceSoftwareRasterization,
                "XPS rasterization and XPS-to-GDI conversion are forced to use software rasterization.",
                "Software rasterization is not forced by this policy.");
        }

        return new(Name, unknown ? CheckState.Unknown : attention ? CheckState.Attention : CheckState.Information,
            string.Join(" ", details) + Scope,
            Evidence: new(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false));

        void DescribeIsolation(string label, object? value, string enabled, string disabled)
        {
            switch (ReadFlag(value))
            {
                case true: details.Add(label + ": Enabled. " + enabled); break;
                case false: attention = true; details.Add(label + ": Disabled. " + disabled + " Review this reliability exposure with the policy owner."); break;
                default:
                    if (value is null) details.Add(label + ": Not explicitly configured. Windows enables isolation by default where the application or driver supports it.");
                    else { unknown = true; details.Add(label + " has an unsupported value or registry type."); }
                    break;
            }
        }

        void DescribeFlag(string label, object? value, string enabled, string disabled)
        {
            switch (ReadFlag(value))
            {
                case true: details.Add(label + ": Enabled. " + enabled); break;
                case false: details.Add(label + ": Disabled. " + disabled); break;
                default:
                    if (value is null) details.Add(label + ": Not explicitly configured.");
                    else { unknown = true; details.Add(label + " has an unsupported value or registry type."); }
                    break;
            }
        }
    }

    [SupportedOSPlatform("windows")]
    private static PrintRenderingPolicySnapshot Collect()
    {
        using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = machine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\Printers");
        return new(
            key?.GetValue("ApplicationDriverIsolation", null, RegistryValueOptions.DoNotExpandEnvironmentNames),
            key?.GetValue("PrintDriverIsolationExecutionPolicy", null, RegistryValueOptions.DoNotExpandEnvironmentNames),
            key?.GetValue("PrintDriverIsolationOverrideCompat", null, RegistryValueOptions.DoNotExpandEnvironmentNames),
            key?.GetValue("ForceCSREMFDespooling", null, RegistryValueOptions.DoNotExpandEnvironmentNames),
            key?.GetValue("ForceSoftwareRasterization", null, RegistryValueOptions.DoNotExpandEnvironmentNames));
    }

    private static bool? ReadFlag(object? value) => value switch
    {
        int number when number == 0 => false,
        int number when number == 1 => true,
        _ => null
    };

    private static WorkflowCheck Unknown(string detail, ToolResult? evidence = null) =>
        new(Name, CheckState.Unknown, detail + Scope, Evidence: evidence);
}
