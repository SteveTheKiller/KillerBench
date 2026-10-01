using Microsoft.Win32;
using System.Security;
using System.Text.Json;

namespace KillerBench.Engine.SystemHealth;

public sealed record RestartSnapshot(bool Complete, object? ComponentServicing, object? WindowsUpdate,
    object? PendingFileRenames, object? ComputerRename, object? UpdateExecutable,
    string? ActiveComputerName = null, string? ConfiguredComputerName = null, string? CollectionIssue = null);

public static class RestartDiagnostics
{
    public static WorkflowCheck Check() => Assess(Read());

    public static WorkflowCheck Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return Assess(new(false, null, null, null, null, null, CollectionIssue: "IncompleteEvidence"));
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 3 });
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 9 ||
                !Boolean(root, "complete", out bool complete) ||
                !Boolean(root, "componentServicing", out bool componentServicing) ||
                !Boolean(root, "windowsUpdate", out bool windowsUpdate) ||
                !Boolean(root, "pendingFileRenames", out bool pendingFileRenames) ||
                !Boolean(root, "computerRename", out bool computerRename) ||
                !Boolean(root, "updateExecutable", out bool updateExecutable) ||
                !String(root, "activeComputerName", out string? activeComputerName) ||
                !String(root, "configuredComputerName", out string? configuredComputerName) ||
                !String(root, "collectionIssue", out string? collectionIssue))
                return Assess(new(false, null, null, null, null, null, CollectionIssue: "MalformedEvidence"));
            return Assess(new(complete, componentServicing, windowsUpdate, pendingFileRenames, computerRename,
                updateExecutable, activeComputerName, configuredComputerName, collectionIssue));
        }
        catch (JsonException)
        {
            return Assess(new(false, null, null, null, null, null, CollectionIssue: "MalformedEvidence"));
        }
    }

    public static RestartSnapshot Read()
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Restart indicator collection requires Windows.");
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var cbs = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending");
            using var update = root.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
            using var session = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager");
            using var activeName = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ComputerName\ActiveComputerName");
            using var configuredName = root.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ComputerName\ComputerName");
            using var updates = root.OpenSubKey(@"SOFTWARE\Microsoft\Updates");
            object? pendingFiles = session?.GetValue("PendingFileRenameOperations");
            object? volatileUpdate = updates?.GetValue("UpdateExeVolatile");
            string? active = activeName?.GetValue("ComputerName") as string;
            string? configured = configuredName?.GetValue("ComputerName") as string;
            object filePending = pendingFiles switch
            {
                null => false,
                string[] values when values.Length <= 4096 && values.All(value => value is not null && value.Length <= 32768) => values.Any(value => !string.IsNullOrWhiteSpace(value)),
                _ => pendingFiles
            };
            object executablePending = volatileUpdate switch { null => false, int value => value != 0, _ => volatileUpdate };
            object renamePending = ValidName(active) && ValidName(configured)
                ? !active!.Equals(configured, StringComparison.OrdinalIgnoreCase)
                : "Unavailable";
            return new(true, cbs is not null, update is not null, filePending, renamePending, executablePending, active, configured);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or SecurityException)
        {
            return new(false, null, null, null, null, null, CollectionIssue: error.GetType().Name);
        }
    }

    public static WorkflowCheck Assess(RestartSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        object?[] indicators = [snapshot.ComponentServicing, snapshot.WindowsUpdate, snapshot.PendingFileRenames,
            snapshot.ComputerRename, snapshot.UpdateExecutable];
        bool valid = snapshot.Complete && string.IsNullOrWhiteSpace(snapshot.CollectionIssue) && indicators.All(value => value is bool) &&
            ValidName(snapshot.ActiveComputerName) && ValidName(snapshot.ConfiguredComputerName);
        CheckState state = !valid ? CheckState.Unknown : indicators.Any(value => value is true) ? CheckState.Attention : CheckState.Clear;
        string detail;
        if (!valid)
            detail = "Restart indicators were unavailable or malformed. Treat restart readiness as unknown before servicing or repair.";
        else
        {
            string[] pending =
            [
                .. snapshot.ComponentServicing is true ? ["component servicing"] : Array.Empty<string>(),
                .. snapshot.WindowsUpdate is true ? ["Windows Update"] : Array.Empty<string>(),
                .. snapshot.PendingFileRenames is true ? ["pending file rename operations"] : Array.Empty<string>(),
                .. snapshot.ComputerRename is true ? ["computer rename"] : Array.Empty<string>(),
                .. snapshot.UpdateExecutable is true ? ["update executable"] : Array.Empty<string>()
            ];
            detail = pending.Length == 0
                ? "No supported pending restart indicator was found. Applications and installers can maintain additional restart state."
                : "Pending restart sources: " + string.Join(", ", pending) + ". Restart, then collect fresh evidence before servicing or repair.";
        }
        detail += " No restart was requested or scheduled, and no indicator was cleared.";
        return new("Restart", state, detail,
            Evidence: new(valid ? 0 : -1, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), snapshot.CollectionIssue ?? "", false));
    }

    private static bool ValidName(string? value) => value is { Length: > 0 and <= 63 } &&
        value.All(character => char.IsLetterOrDigit(character) || character == '-');

    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        var matches = owner.EnumerateObject().Where(property => property.NameEquals(name)).ToArray();
        value = matches.Length == 1 && matches[0].Value.ValueKind is JsonValueKind.True or JsonValueKind.False &&
            matches[0].Value.GetBoolean();
        return matches.Length == 1 && matches[0].Value.ValueKind is JsonValueKind.True or JsonValueKind.False;
    }

    private static bool String(JsonElement owner, string name, out string? value)
    {
        var matches = owner.EnumerateObject().Where(property => property.NameEquals(name)).ToArray();
        value = matches.Length == 1 && matches[0].Value.ValueKind == JsonValueKind.String
            ? matches[0].Value.GetString()
            : null;
        return matches.Length == 1 && matches[0].Value.ValueKind is JsonValueKind.String or JsonValueKind.Null;
    }
}
