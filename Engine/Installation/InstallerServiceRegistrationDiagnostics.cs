using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Text.Json;

namespace KillerBench.Engine.Installation;

public sealed record InstallerServiceRegistrationSnapshot(bool Complete, bool ServiceKeyPresent,
    bool ImagePathPresent, bool ImageTargetExpected, bool VerificationSwitchExpected,
    bool AccountPresent, bool AccountExpected, int? StartMode, int? ServiceType, int? ErrorControl);

public static class InstallerServiceRegistrationDiagnostics
{
    private const string Scope = " Raw executable paths and account values are excluded. This check does not establish service availability, Installer registration, package integrity or installation success. No service, file, registry value, package, policy or Installer setting changed.";

    public static WorkflowCheck Check(Func<InstallerServiceRegistrationSnapshot>? collect = null)
    {
        try
        {
            if (collect is null && !OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Windows Installer service registration inspection requires Windows.");
            return Assess((collect ?? Collect)() ?? throw new InvalidOperationException("Installer service registration collector returned no observation."));
        }
        catch (Exception error) when (error is IOException or InvalidDataException or InvalidOperationException or
            UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Unknown("Windows Installer service registration could not be read completely.",
                new(-1, "", error.GetType().Name + ": " + error.Message, false));
        }
    }

    public static WorkflowCheck Assess(InstallerServiceRegistrationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var evidence = new ToolResult(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false);
        if (!snapshot.Complete || !ValidOptional(snapshot.StartMode, 0, 4) || !ValidOptional(snapshot.ServiceType, 1, 511) ||
            !ValidOptional(snapshot.ErrorControl, 0, 3))
            return Unknown("Windows Installer service registration evidence was incomplete or invalid.", evidence);
        if (!snapshot.ServiceKeyPresent)
            return Attention("The Windows Installer service registration is missing. Use the System files workflow before considering component recovery.", evidence);

        var review = new List<string>();
        if (!snapshot.ImagePathPresent) review.Add("The service image command is missing");
        else
        {
            if (!snapshot.ImageTargetExpected) review.Add("the service image does not target the protected native msiexec.exe");
            if (!snapshot.VerificationSwitchExpected) review.Add("the service image does not contain only the expected /V service switch");
        }
        if (!snapshot.AccountPresent) review.Add("the service account registration is missing");
        else if (!snapshot.AccountExpected) review.Add("the service account differs from LocalSystem");
        if (snapshot.StartMode is null) review.Add("the service start mode is missing");
        else if (snapshot.StartMode != 3) review.Add("the service start mode differs from the Windows demand-start default");
        if (snapshot.ServiceType is null) review.Add("the service type is missing");
        else if (snapshot.ServiceType != 16) review.Add("the service type differs from an own-process service");
        if (snapshot.ErrorControl is null) review.Add("the service error-control value is missing");
        else if (snapshot.ErrorControl != 1) review.Add("the service error-control value differs from the normal Windows setting");
        if (review.Count > 0)
            return Attention("Review the Windows Installer service registration: " + string.Join("; ", review) +
                ". Use protected-file and policy evidence before deciding how to recover it.", evidence);
        return new("Windows Installer service registration", CheckState.Information,
            "The service image target, service switch, LocalSystem account, demand-start mode, own-process type and normal error-control value match the expected Windows registration." + Scope,
            Evidence: evidence);
    }

    [SupportedOSPlatform("windows")]
    private static InstallerServiceRegistrationSnapshot Collect()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Installer service registration inspection requires Windows.");
        using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var service = machine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\msiserver");
        if (service is null) return new(true, false, false, false, false, false, false, null, null, null);
        string? image = ReadString(service, "ImagePath", out bool imagePresent);
        string? account = ReadString(service, "ObjectName", out bool accountPresent);
        (bool target, bool serviceSwitch) = AnalyzeImageCommand(image);
        return new(true, true, imagePresent, target, serviceSwitch, accountPresent,
            account is not null && (account.Equals("LocalSystem", StringComparison.OrdinalIgnoreCase) ||
                account.Equals(@"NT AUTHORITY\SYSTEM", StringComparison.OrdinalIgnoreCase)),
            ReadDword(service, "Start"), ReadDword(service, "Type"), ReadDword(service, "ErrorControl"));
    }

    internal static (bool Target, bool ServiceSwitch) AnalyzeImageCommand(string? command)
    {
        if (string.IsNullOrWhiteSpace(command)) return (false, false);
        string expanded = Environment.ExpandEnvironmentVariables(command).Trim();
        string expected = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "msiexec.exe");
        string target;
        string remainder;
        if (expanded.StartsWith('"'))
        {
            int close = expanded.IndexOf('"', 1);
            if (close < 0) return (false, false);
            target = expanded[1..close];
            remainder = expanded[(close + 1)..].Trim();
        }
        else if (expanded.StartsWith(expected, StringComparison.OrdinalIgnoreCase) &&
            (expanded.Length == expected.Length || char.IsWhiteSpace(expanded[expected.Length])))
        {
            target = expanded[..expected.Length];
            remainder = expanded[expected.Length..].Trim();
        }
        else return (false, false);
        return (Path.GetFullPath(target).Equals(Path.GetFullPath(expected), StringComparison.OrdinalIgnoreCase),
            remainder.Equals("/V", StringComparison.OrdinalIgnoreCase));
    }

    [SupportedOSPlatform("windows")]
    private static string? ReadString(RegistryKey key, string name, out bool present)
    {
        present = key.GetValueNames().Contains(name, StringComparer.OrdinalIgnoreCase);
        if (!present) return null;
        if (key.GetValueKind(name) is not (RegistryValueKind.String or RegistryValueKind.ExpandString))
            throw new InvalidDataException(name + " has an unexpected registry type.");
        return key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string
            ?? throw new InvalidDataException(name + " could not be read as text.");
    }

    [SupportedOSPlatform("windows")]
    private static int? ReadDword(RegistryKey key, string name)
    {
        if (!key.GetValueNames().Contains(name, StringComparer.OrdinalIgnoreCase)) return null;
        if (key.GetValueKind(name) != RegistryValueKind.DWord || key.GetValue(name) is not int value)
            throw new InvalidDataException(name + " has an unexpected registry type.");
        return value;
    }

    private static bool ValidOptional(int? value, int minimum, int maximum) => value is null || value is >= 0 && value >= minimum && value <= maximum;
    private static WorkflowCheck Attention(string detail, ToolResult evidence) =>
        new("Windows Installer service registration", CheckState.Attention, detail + Scope, Evidence: evidence);
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Windows Installer service registration", CheckState.Unknown, detail + Scope, Evidence: evidence);
}
