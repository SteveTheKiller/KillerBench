using Microsoft.Win32;
using System.Security;
using System.Text.Json;

namespace KillerBench.Engine.SystemHealth;

public sealed record PendingFileOperationSnapshot(
    bool Complete,
    bool ValuePresent,
    int EntryCount,
    int OperationCount,
    int DeleteCount,
    int ReplaceCount,
    int RenameCount,
    string? CollectionIssue = null);

public static class PendingFileOperationDiagnostics
{
    private const int MaximumEntries = 4096;
    private const int MaximumEntryLength = 32768;

    public static WorkflowCheck Check(Func<object?>? collect = null)
    {
        try
        {
            object? value = (collect ?? Collect)();
            return Assess(Summarize(value));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException or InvalidOperationException)
        {
            return Unknown("Pending file operation registration could not be read.", error.GetType().Name);
        }
    }

    public static PendingFileOperationSnapshot Summarize(object? value)
    {
        if (value is null)
            return new(true, false, 0, 0, 0, 0, 0);
        if (value is not string[] entries)
            return new(false, true, 0, 0, 0, 0, 0, "Unexpected registry value type");
        if (entries.Length > MaximumEntries)
            return new(false, true, entries.Length, 0, 0, 0, 0, "Entry limit exceeded");
        if (entries.Length % 2 != 0)
            return new(false, true, entries.Length, 0, 0, 0, 0, "Unpaired source and destination entries");

        int deletes = 0, replacements = 0, renames = 0;
        for (int index = 0; index < entries.Length; index += 2)
        {
            string? source = entries[index];
            string? destination = entries[index + 1];
            if (string.IsNullOrWhiteSpace(source) || source.Length > MaximumEntryLength ||
                destination is null || destination.Length > MaximumEntryLength)
                return new(false, true, entries.Length, index / 2, deletes, replacements, renames,
                    "Malformed source or destination entry");

            if (destination.Length == 0)
                deletes++;
            else if (destination[0] == '!')
            {
                if (destination.Length == 1)
                    return new(false, true, entries.Length, index / 2, deletes, replacements, renames,
                        "Malformed replacement destination");
                replacements++;
            }
            else
                renames++;
        }

        return new(true, true, entries.Length, entries.Length / 2, deletes, replacements, renames);
    }

    public static WorkflowCheck Assess(PendingFileOperationSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        bool countsValid = snapshot.EntryCount >= 0 && snapshot.OperationCount >= 0 && snapshot.DeleteCount >= 0 &&
            snapshot.ReplaceCount >= 0 && snapshot.RenameCount >= 0 &&
            snapshot.EntryCount == snapshot.OperationCount * 2 &&
            snapshot.DeleteCount + snapshot.ReplaceCount + snapshot.RenameCount == snapshot.OperationCount;
        bool presenceValid = snapshot.ValuePresent || snapshot.EntryCount == 0;
        bool valid = snapshot.Complete && string.IsNullOrWhiteSpace(snapshot.CollectionIssue) && countsValid && presenceValid;
        if (!valid)
            return Unknown("Pending file operation evidence was incomplete or malformed. Restart readiness remains unknown.",
                snapshot.CollectionIssue, snapshot);

        string preservation = " File paths and identities are excluded from this summary. The registration was not cleared or changed, and no restart was requested.";
        if (snapshot.OperationCount == 0)
            return new("Pending file operations", CheckState.Information,
                "No pending file rename, replacement or deletion operation was registered at capture time." + preservation,
                Evidence: Evidence(snapshot));

        string detail = $"Windows has {snapshot.OperationCount} pending file operation{(snapshot.OperationCount == 1 ? "" : "s")}: " +
            $"{snapshot.RenameCount} rename, {snapshot.ReplaceCount} replacement and {snapshot.DeleteCount} deletion request{(snapshot.DeleteCount == 1 ? "" : "s")}. " +
            "These operations can belong to Windows servicing, an installer, a driver or another application. Restart normally, then collect fresh evidence before retrying related work.";
        return new("Pending file operations", CheckState.Attention, detail + preservation, Evidence: Evidence(snapshot));
    }

    private static object? Collect()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Pending file operation collection requires Windows.");
        using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var session = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager");
        return session?.GetValue("PendingFileRenameOperations", null, RegistryValueOptions.DoNotExpandEnvironmentNames);
    }

    private static ToolResult Evidence(PendingFileOperationSnapshot snapshot) =>
        new(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false);

    private static WorkflowCheck Unknown(string detail, string? issue = null, PendingFileOperationSnapshot? snapshot = null) =>
        new("Pending file operations", CheckState.Unknown,
            detail + " No registration was cleared or changed, and no restart was requested.",
            Evidence: new(-1, snapshot is null ? "" : JsonSerializer.Serialize(snapshot, Capture.JsonOptions), issue ?? "", false));
}
