using Microsoft.Win32;

namespace KillerBench.Engine.Crash;

public sealed record CrashDumpSettings(int? Mode, int? FilterPages, string? DumpFile, string? MinidumpDirectory, string? Error = null);

public static class CrashDumpConfiguration
{
    public static WorkflowCheck Check(CancellationToken token = default) => Interpret(ReadSettings(token));

    public static CrashDumpSettings ReadSettings(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Crash configuration requires Windows.");
        try
        {
            using var machine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = machine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CrashControl", false);
            if (key is null) return new(null, null, null, null, "CrashControl key unavailable.");
            object? Read(string name)
            {
                if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Registry reading requires Windows.");
                return key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            }
            return ReadValues(Read);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { return new(null, null, null, null, error.Message); }
    }

    public static CrashDumpSettings ReadValues(Func<string, object?> read)
    {
        ArgumentNullException.ThrowIfNull(read);
        var errors = new List<string>();
        int? Number(string name)
        {
            var value = read(name);
            if (value is null or int) return value as int?;
            errors.Add(name + " has an unexpected value type.");
            return null;
        }
        string? Text(string name)
        {
            var value = read(name);
            if (value is null or string) return value as string;
            errors.Add(name + " has an unexpected value type.");
            return null;
        }
        int? mode = Number("CrashDumpEnabled"), filter = Number("FilterPages");
        string? dump = Text("DumpFile"), mini = Text("MinidumpDir");
        return new(mode, filter, dump, mini, errors.Count == 0 ? null : string.Join(" ", errors));
    }

    public static WorkflowCheck Interpret(CrashDumpSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        string? mode = settings.Mode switch { 0 => "Disabled", 1 when settings.FilterPages == 1 => "Active memory", 1 when settings.FilterPages is null or 0 => "Complete memory", 2 => "Kernel memory", 3 => "Small memory", 7 => "Automatic memory", _ => null };
        bool valid = settings.Error is null && mode is not null && settings.FilterPages is null or 0 or 1 &&
            ValidPathText(settings.DumpFile) && ValidPathText(settings.MinidumpDirectory);
        var state = !valid ? CheckState.Unknown : settings.Mode == 0 ? CheckState.Attention : CheckState.Information;
        string detail = "Configured dump mode: " + (mode ?? "Unrecognized or missing") +
            "\nConfigured dump file: " + (settings.DumpFile ?? "Not specified") +
            "\nConfigured minidump directory: " + (settings.MinidumpDirectory ?? "Not specified") +
            (settings.Error is null ? "" : "\nRead error: " + settings.Error) +
            "\nStored configuration only. Paths are not expanded or opened. Pagefile capacity, destination access, policy ownership and successful dump creation are unverified. No settings changed.";
        return new("Crash dump configuration", state, detail, Evidence: new ToolResult(0,
            System.Text.Json.JsonSerializer.Serialize(settings, Capture.JsonOptions), "", false));
    }

    private static bool ValidPathText(string? value) => value is null || value.Length is > 0 and <= 32767 && !value.Any(char.IsControl);
}
