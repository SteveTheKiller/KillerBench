using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace KillerBench.Engine;

public enum ServiceStartRegistrationStatus
{
    Ready,
    MissingExecutable,
    MissingServiceDll,
    UnsupportedLocation,
    InvalidRegistration,
    Unavailable
}

public sealed record ServiceStartRegistrationEvidence(
    string ServiceName,
    ServiceStartRegistrationStatus Status,
    bool Complete,
    string? ExecutableFile,
    string? ServiceDllFile);

public static class ServiceStartPrerequisite
{
    private sealed record PathResult(ServiceStartRegistrationStatus Status, string? FileName);

    public static WorkflowCheck Assess(RepairKind kind, WorkflowCheck serviceCheck, ServiceStartRegistrationEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(serviceCheck);
        ArgumentNullException.ThrowIfNull(evidence);
        string? expected = ServiceRecovery.ServiceName(kind);
        if (expected is null || serviceCheck.State != CheckState.Repairable || serviceCheck.Repair != kind)
            return serviceCheck with { State = CheckState.Unknown, Repair = null,
                Detail = "Service start eligibility changed before executable registration was checked. No automatic start selected." };
        if (!ValidServiceName(evidence.ServiceName) || !string.Equals(expected, evidence.ServiceName, StringComparison.OrdinalIgnoreCase))
            return serviceCheck with { State = CheckState.Unknown, Repair = null,
                Detail = "Executable registration evidence identifies a different or invalid service. No automatic start selected." };
        if (!evidence.Complete || evidence.Status == ServiceStartRegistrationStatus.Unavailable)
            return serviceCheck with { State = CheckState.Unknown, Repair = null,
                Detail = "The registered service executable could not be verified immediately before start. No automatic start selected." };
        string detail = evidence.Status switch
        {
            ServiceStartRegistrationStatus.MissingExecutable => "The registered service executable is missing. Repair the owning Windows component or approved application package before retrying; no file is downloaded or replaced.",
            ServiceStartRegistrationStatus.MissingServiceDll => "The registered service DLL is missing. Repair the owning Windows component or approved application package before retrying; no DLL is downloaded or replaced.",
            ServiceStartRegistrationStatus.UnsupportedLocation => "The service uses a remote, removable or otherwise unsupported executable location. Review the registration with its owner before starting it.",
            ServiceStartRegistrationStatus.InvalidRegistration => "The service executable registration is malformed or unsupported. Review the registration with its owner before starting it.",
            ServiceStartRegistrationStatus.Ready when ValidFileName(evidence.ExecutableFile, ".exe", ".sys") &&
                (evidence.ServiceDllFile is null || ValidFileName(evidence.ServiceDllFile, ".dll")) => "",
            _ => "The service executable evidence is internally inconsistent. No automatic start selected."
        };
        return detail.Length == 0 ? serviceCheck : serviceCheck with { State = CheckState.Attention, Repair = null, Detail = detail };
    }

    [SupportedOSPlatform("windows")]
    public static ServiceStartRegistrationEvidence Read(string serviceName)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        if (!ValidServiceName(serviceName)) throw new ArgumentException("Invalid service name.", nameof(serviceName));
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var service = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + serviceName);
            if (service is null) return new(serviceName, ServiceStartRegistrationStatus.Unavailable, false, null, null);
            object? typeValue = service.GetValue("Type", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            object? imageValue = service.GetValue("ImagePath", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (typeValue is not int type || (type & 0x33) == 0 || imageValue is not string imagePath || string.IsNullOrWhiteSpace(imagePath))
                return new(serviceName, ServiceStartRegistrationStatus.InvalidRegistration, true, null, null);
            var executable = Resolve(imagePath, ".exe", ".sys");
            if (executable.Status != ServiceStartRegistrationStatus.Ready)
                return new(serviceName, executable.Status, true, executable.FileName, null);

            using var parameters = service.OpenSubKey("Parameters");
            if (parameters is null || !parameters.GetValueNames().Contains("ServiceDll", StringComparer.OrdinalIgnoreCase))
                return new(serviceName, ServiceStartRegistrationStatus.Ready, true, executable.FileName, null);
            object? dllValue = parameters.GetValue("ServiceDll", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            if (dllValue is not string dllPath || string.IsNullOrWhiteSpace(dllPath))
                return new(serviceName, ServiceStartRegistrationStatus.InvalidRegistration, true, executable.FileName, null);
            var dll = Resolve(dllPath, ".dll");
            var status = dll.Status == ServiceStartRegistrationStatus.MissingExecutable
                ? ServiceStartRegistrationStatus.MissingServiceDll : dll.Status;
            return new(serviceName, status, true, executable.FileName, dll.FileName);
        }
        catch (Exception error) when (error is UnauthorizedAccessException or IOException or System.Security.SecurityException or InvalidOperationException or ArgumentException)
        {
            return new(serviceName, ServiceStartRegistrationStatus.Unavailable, false, null, null);
        }
    }

    private static PathResult Resolve(string registration, params string[] extensions)
    {
        try
        {
            string expanded = Environment.ExpandEnvironmentVariables(registration.Trim());
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            if (expanded.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase))
                expanded = Path.Combine(windows, expanded[12..]);
            else if (expanded.StartsWith(@"System32\", StringComparison.OrdinalIgnoreCase))
                expanded = Path.Combine(windows, expanded);
            string path;
            if (expanded.StartsWith('"'))
            {
                int close = expanded.IndexOf('"', 1);
                if (close < 2) return new(ServiceStartRegistrationStatus.InvalidRegistration, null);
                path = expanded[1..close];
            }
            else
            {
                string pattern = "^(.+?(?:" + string.Join('|', extensions.Select(Regex.Escape)) + "))(?=\\s|$)";
                var match = Regex.Match(expanded, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (!match.Success) return new(ServiceStartRegistrationStatus.InvalidRegistration, null);
                path = match.Groups[1].Value;
            }
            string file = Path.GetFileName(path);
            if (!ValidFileName(file, extensions) || !Path.IsPathRooted(path) || path.StartsWith(@"\\", StringComparison.Ordinal))
                return new(ServiceStartRegistrationStatus.UnsupportedLocation, ValidFileName(file, extensions) ? file : null);
            string fullPath = Path.GetFullPath(path);
            string? root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrEmpty(root) || new DriveInfo(root).DriveType != DriveType.Fixed)
                return new(ServiceStartRegistrationStatus.UnsupportedLocation, file);
            return File.Exists(fullPath)
                ? new(ServiceStartRegistrationStatus.Ready, file)
                : new(ServiceStartRegistrationStatus.MissingExecutable, file);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            return new(ServiceStartRegistrationStatus.Unavailable, null);
        }
    }

    private static bool ValidServiceName(string? value) => !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && value == value.Trim() &&
        !value.Any(character => char.IsControl(character) || character is '\\' or '/');

    private static bool ValidFileName(string? value, params string[] extensions) => !string.IsNullOrWhiteSpace(value) && value.Length <= 260 &&
        Path.GetFileName(value) == value && extensions.Any(extension => value.EndsWith(extension, StringComparison.OrdinalIgnoreCase));
}
