using Microsoft.Win32;
using System.Runtime.Versioning;
using System.Text.Json;

namespace KillerBench.Engine.Storage;

public sealed record StorageSensePolicyValue(string Name, bool Present, bool Valid, int? Value);
public sealed record StorageSensePolicySnapshot(bool Complete, IReadOnlyList<StorageSensePolicyValue> Values);

public static class StorageSensePolicyDiagnostics
{
    private static readonly string[] Names =
    [
        "AllowStorageSenseGlobal", "ConfigStorageSenseGlobalCadence", "AllowStorageSenseTemporaryFilesCleanup",
        "ConfigStorageSenseRecycleBinCleanupThreshold", "ConfigStorageSenseDownloadsCleanupThreshold",
        "ConfigStorageSenseCloudContentDehydrationThreshold"
    ];
    private const string Scope = " These are machine policy values, not proof that cleanup ran or that any file is eligible. User settings, file identities and contents are excluded. No file, folder, cloud placeholder, Recycle Bin item, policy or Storage Sense setting changed.";

    public static WorkflowCheck Check(Func<StorageSensePolicySnapshot>? collect = null)
    {
        try
        {
            if (collect is null && !OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Storage Sense policy inspection requires Windows.");
            return Assess((collect ?? Collect)() ?? throw new InvalidOperationException("Storage Sense policy collector returned no observation."));
        }
        catch (Exception error) when (error is IOException or InvalidDataException or InvalidOperationException or
            UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Unknown("Storage Sense machine policy could not be read completely.",
                new(-1, "", error.GetType().Name + ": " + error.Message, false));
        }
    }

    public static WorkflowCheck Assess(StorageSensePolicySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var evidence = new ToolResult(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false);
        if (!snapshot.Complete || snapshot.Values is null || snapshot.Values.Count != Names.Length)
            return Unknown("Storage Sense policy evidence was incomplete.", evidence);
        var values = new Dictionary<string, StorageSensePolicyValue>(StringComparer.Ordinal);
        foreach (var entry in snapshot.Values)
        {
            if (entry is null || !Names.Contains(entry.Name, StringComparer.Ordinal) || !values.TryAdd(entry.Name, entry) ||
                !entry.Valid || entry.Present != entry.Value.HasValue || entry.Value is < 0)
                return Unknown("Storage Sense policy evidence was malformed or had an unsupported registry type.", evidence);
        }
        if (!ValidToggle(values[Names[0]]) || !ValidCadence(values[Names[1]]) || !ValidToggle(values[Names[2]]) ||
            Names.Skip(3).Any(name => !ValidDays(values[name])))
            return Unknown("Storage Sense policy contains an unsupported value.", evidence);
        var configured = snapshot.Values.Where(value => value.Present).ToArray();
        if (configured.Length == 0)
            return Information("Storage Sense is not explicitly configured by the recognized machine policy values. Windows defaults and current-user settings can still apply.", evidence);

        string Setting(string name) => values[name].Present ? values[name].Value!.Value.ToString() : "not configured";
        string cadence = values[Names[1]].Value switch { 0 => "low disk space", 1 => "daily", 7 => "weekly", 30 => "monthly", _ => "not configured" };
        string detail = "Machine policy: global " + Toggle(values[Names[0]]) + "; cadence " + cadence +
            "; temporary files " + Toggle(values[Names[2]]) + "; Recycle Bin days " + Setting(Names[3]) +
            "; Downloads days " + Setting(Names[4]) + "; cloud dehydration days " + Setting(Names[5]) + ".";
        bool disabledWithChildren = values[Names[0]] is { Present: true, Value: 0 } && snapshot.Values.Skip(1).Any(value => value.Present);
        return disabledWithChildren
            ? new("Storage Sense machine policy", CheckState.Attention,
                detail + " Global policy blocks Storage Sense, so subordinate cleanup policy has no effect. Review the effective configuration with the policy owner." + Scope,
                Evidence: evidence)
            : Information(detail, evidence);
    }

    [SupportedOSPlatform("windows")]
    private static StorageSensePolicySnapshot Collect()
    {
        using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var policy = machine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\StorageSense");
        var entries = new List<StorageSensePolicyValue>(Names.Length);
        foreach (string name in Names)
        {
            bool present = policy?.GetValueNames().Contains(name, StringComparer.OrdinalIgnoreCase) == true;
            if (!present) { entries.Add(new(name, false, true, null)); continue; }
            bool valid = policy!.GetValueKind(name) == RegistryValueKind.DWord && policy.GetValue(name) is int;
            entries.Add(new(name, true, valid, valid ? (int?)policy.GetValue(name) : null));
        }
        return new(true, entries.AsReadOnly());
    }

    private static bool ValidToggle(StorageSensePolicyValue value) => !value.Present || value.Value is 0 or 1;
    private static bool ValidCadence(StorageSensePolicyValue value) => !value.Present || value.Value is 0 or 1 or 7 or 30;
    private static bool ValidDays(StorageSensePolicyValue value) => !value.Present || value.Value is >= 0 and <= 365;
    private static string Toggle(StorageSensePolicyValue value) => !value.Present ? "not configured" : value.Value == 1 ? "allowed" : "blocked";
    private static WorkflowCheck Information(string detail, ToolResult evidence) =>
        new("Storage Sense machine policy", CheckState.Information, detail + Scope, Evidence: evidence);
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Storage Sense machine policy", CheckState.Unknown, detail + Scope, Evidence: evidence);
}
