using System.Net;
using System.Text.Json;

namespace KillerBench.Engine;

public static class HostsFileDiagnostics
{
    private const int MaximumBytes = 1_048_576;
    private const int MaximumLines = 4096;
    private const int MaximumNames = 8192;

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Hosts file inspection requires Windows.");
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists) return Unknown(new(-1, "", "Hosts file is missing.", false));
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length > MaximumBytes)
                return Unknown(new(-1, "", "Hosts file is redirected or exceeds the inspection bound.", false));
            string text = await File.ReadAllTextAsync(path, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return Analyze(text);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Unknown(new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Analyze(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var mappings = new List<(int Line, string Address, string Name)>();
        var invalidLines = new List<int>();
        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
        if (lines.Length > MaximumLines) return Unknown(new(-1, "", "Hosts file exceeds the line bound.", false));
        for (int index = 0; index < lines.Length; index++)
        {
            string content = lines[index].Split('#', 2)[0].Trim();
            if (content.Length == 0) continue;
            string[] fields = content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (fields.Length < 2 || !IPAddress.TryParse(fields[0], out var address)) { invalidLines.Add(index + 1); continue; }
            foreach (string name in fields.Skip(1))
            {
                if (!ValidName(name)) { invalidLines.Add(index + 1); continue; }
                if (mappings.Count >= MaximumNames) return Unknown(new(-1, "", "Hosts file exceeds the mapping bound.", false));
                mappings.Add((index + 1, address.ToString(), name));
            }
        }
        var conflicts = mappings.GroupBy(mapping => mapping.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Select(mapping => mapping.Address).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1).ToArray();
        var repeats = mappings.GroupBy(mapping => mapping.Address + "\0" + mapping.Name, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1).ToArray();
        var evidence = new ToolResult(0, JsonSerializer.Serialize(new
        {
            entries = mappings.Select(mapping => new { line = mapping.Line, address = mapping.Address, name = mapping.Name }),
            invalidLines,
            conflictingNames = conflicts.Select(group => group.Key),
            repeatedMappings = repeats.Select(group => group.First().Name)
        }, Capture.JsonOptions), "", false);
        string detail = $"Reviewed {mappings.Count} Hosts file mapping{(mappings.Count == 1 ? "" : "s")}. Invalid lines: {invalidLines.Count}; conflicting names: {conflicts.Length}; repeated mappings: {repeats.Length}.";
        if (invalidLines.Count > 0) detail += "\nReview lines: " + string.Join(", ", invalidLines.Distinct().Take(32)) + (invalidLines.Distinct().Count() > 32 ? ", additional lines remain in evidence." : ".");
        if (conflicts.Length > 0) detail += "\nNames mapped to different addresses: " + string.Join("; ", conflicts.Take(20).Select(group => group.Key)) + (conflicts.Length > 20 ? "; additional names remain in evidence." : ".");
        detail += "\nComments were omitted from evidence. Mappings may be intentional and do not prove the active resolver used them. No file, DNS, adapter, cache or policy setting changed.";
        return new("Hosts file", invalidLines.Count > 0 || conflicts.Length > 0 || repeats.Length > 0 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
    }

    private static WorkflowCheck Unknown(ToolResult evidence) => new("Hosts file", CheckState.Unknown,
        "The Hosts file is unavailable or outside inspection bounds. No file, DNS, adapter, cache or policy setting changed.", Evidence: evidence);

    private static bool ValidName(string value) => value.Length is > 0 and <= 253 && value == value.Trim() &&
        !value.Any(char.IsControl) && !value.Any(character => char.IsWhiteSpace(character) || character is '/' or '\\' or '#');
}
