using Microsoft.Win32;
using System.Security;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record PrintSpoolDirectorySnapshot(bool Complete, string? ConfiguredPath, string? ResolvedPath,
    bool? Exists, bool? ReparsePoint, DriveType? DriveType, long? AvailableBytes, bool UsedWindowsDefault, string? CollectionIssue = null);

public static class PrintSpoolDirectoryDiagnostics
{
    private const long LowCapacityBytes = 1024L * 1024 * 1024;

    public static WorkflowCheck Check() => Interpret(Read());

    public static PrintSpoolDirectorySnapshot Read()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Print spool directory inspection requires Windows.");
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Print\Printers");
            object? configuredValue = key?.GetValue("DefaultSpoolDirectory", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            bool usedDefault = configuredValue is null;
            if (configuredValue is not null and not string)
                return new(false, null, null, null, null, null, null, false, "DefaultSpoolDirectory has an unsupported registry type.");
            string configured = (string?)configuredValue ?? Path.Combine(Environment.SystemDirectory, "spool", "PRINTERS");
            if (!ValidPathText(configured)) return new(false, configured, null, null, null, null, null, usedDefault, "Spool directory configuration is malformed.");
            bool expandable = !usedDefault && key!.GetValueKind("DefaultSpoolDirectory") == RegistryValueKind.ExpandString;
            string expanded = expandable ? Environment.ExpandEnvironmentVariables(configured) : configured;
            if (expandable && expanded.Contains('%'))
                return new(false, configured, expanded, null, null, null, null, usedDefault, "Spool directory contains an unresolved environment variable.");
            string resolved = Path.GetFullPath(expanded);
            if (!ValidPathText(resolved) || !Path.IsPathFullyQualified(resolved))
                return new(false, configured, resolved, null, null, null, null, usedDefault, "Spool directory does not resolve to a fully qualified path.");
            var directory = new DirectoryInfo(resolved);
            bool exists = directory.Exists;
            bool? reparse = exists ? (directory.Attributes & FileAttributes.ReparsePoint) != 0 : null;
            string? driveRoot = Path.GetPathRoot(resolved);
            DriveType? driveType = null;
            long? available = null;
            if (!string.IsNullOrWhiteSpace(driveRoot))
            {
                var drive = new DriveInfo(driveRoot);
                driveType = drive.DriveType;
                if (drive.IsReady) available = drive.AvailableFreeSpace;
            }
            return new(true, configured, resolved, exists, reparse, driveType, available, usedDefault);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException or ArgumentException or NotSupportedException)
        {
            return new(false, null, null, null, null, null, null, false, error.GetType().Name);
        }
    }

    public static WorkflowCheck Interpret(PrintSpoolDirectorySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        const string name = "Print spool directory";
        const string scope = " The directory was not enumerated. No spool file, queued document, printer, port, driver, processor, monitor, registry value, permission or print setting changed.";
        WorkflowCheck Result(CheckState state, string detail, RepairKind? repair = null) => new(name, state, detail + scope, repair,
            Evidence: new(state == CheckState.Unknown ? -1 : 0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), snapshot.CollectionIssue ?? "", false));
        string? resolvedPath = snapshot.ResolvedPath;
        if (!snapshot.Complete || !string.IsNullOrWhiteSpace(snapshot.CollectionIssue) || !ValidPathText(snapshot.ConfiguredPath) ||
            !ValidPathText(resolvedPath) || resolvedPath is null || !Path.IsPathFullyQualified(resolvedPath) || snapshot.Exists is null ||
            snapshot.DriveType is null || snapshot.AvailableBytes is < 0)
            return Result(CheckState.Unknown, "Print spool directory configuration or filesystem evidence is unavailable or incomplete.");
        string configured = snapshot.UsedWindowsDefault ? "Windows default" : "Configured override";
        string detail = $"{configured}: {resolvedPath}.";
        var findings = new List<string>();
        if (snapshot.Exists == false) findings.Add("The directory does not exist");
        if (snapshot.Exists == true && snapshot.ReparsePoint is null) return Result(CheckState.Unknown, "Print spool directory reparse status is unavailable.");
        if (snapshot.ReparsePoint == true) findings.Add("The directory is a reparse point and its target was not followed");
        if (snapshot.DriveType != System.IO.DriveType.Fixed) findings.Add("The resolved root is not reported as a fixed drive");
        if (snapshot.AvailableBytes is null) findings.Add("Available capacity is unavailable");
        else if (snapshot.AvailableBytes < LowCapacityBytes) findings.Add($"Only {snapshot.AvailableBytes / (1024d * 1024):F0} MiB is available on the containing drive");
        if (PrintSpoolDirectoryRecovery.IsEligible(snapshot))
            return Result(CheckState.Repairable, detail + " The Windows default directory is missing, while its fixed drive has at least 1 GiB available. KillerBench can create only this exact directory and then verify its identity and state.", RepairKind.PrintSpoolDirectory);
        if (findings.Count > 0)
            return Result(CheckState.Attention, detail + " " + string.Join(". ", findings) + ". Review spooler configuration, storage availability and organizational policy before changing anything.");
        return Result(CheckState.Information, detail + $" The directory exists on a fixed drive with {snapshot.AvailableBytes!.Value / (1024d * 1024 * 1024):F1} GiB available. This does not test write access, queued document integrity, spooler operation or physical printing.");
    }

    private static bool ValidPathText(string? value) => value is { Length: > 0 and <= 32768 } && value == value.Trim() && !value.Any(char.IsControl);
}
