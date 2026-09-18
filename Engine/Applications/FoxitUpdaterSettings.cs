using Microsoft.Win32;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record FoxitUpdaterValue(string View, string Path, string Name, string Kind, string? Value);
public sealed record FoxitRegistryValue(string Kind, object? Value);
public sealed record FoxitRegistryNode(IReadOnlyList<string> Children, IReadOnlyDictionary<string, FoxitRegistryValue> Values);
public sealed record FoxitUpdaterSnapshot(IReadOnlyList<FoxitUpdaterValue> Values, IReadOnlyList<string> Issues)
{
    public bool Complete => Issues.Count == 0;
}

public static class FoxitUpdaterSettings
{
    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<CancellationToken, FoxitUpdaterSnapshot>? collector = null)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        try
        {
            var snapshot = await Task.Run(() => (collector ?? Read)(deadline.Token), deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false);
            return Describe(snapshot);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        { return new("Foxit updater settings", CheckState.Unknown, "Registry inspection ended before completion or reached its time limit. No settings changed."); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        { return new("Foxit updater settings", CheckState.Unknown, "Registry inspection could not complete. Updater configuration remains unknown; no settings changed."); }
    }

    public static WorkflowCheck Describe(FoxitUpdaterSnapshot snapshot)
    {
        string detail = $"{snapshot.Values.Count} updater values observed in machine Foxit hives. " +
            "Registry values do not prove effective policy, product applicability or ownership. No settings changed.\n" +
            string.Join("\n", snapshot.Values.Select(value => $"{value.View} | {value.Path} | {value.Name} ({value.Kind}): {value.Value ?? "unavailable"}"));
        if (!snapshot.Complete) detail += "\nIncomplete coverage: " + string.Join("; ", snapshot.Issues);
        return new("Foxit updater settings", snapshot.Complete ? CheckState.Information : CheckState.Unknown, detail,
            Evidence: new(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false));
    }

    private static FoxitUpdaterSnapshot Read(CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Foxit settings require Windows.");
        return Collect((view, path) =>
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Foxit settings require Windows.");
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, Enum.Parse<RegistryView>(view));
            using var key = root.OpenSubKey(path);
            if (key is null) return null;
            var entries = new Dictionary<string, FoxitRegistryValue>();
            if (path.EndsWith("\\Updater", StringComparison.OrdinalIgnoreCase))
                foreach (string name in new[] { "UpdateMode", "bNoUpdate" })
                {
                    object? value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                    if (value is not null) entries.Add(name, new(key.GetValueKind(name).ToString(), value));
                }
            return new FoxitRegistryNode(key.GetSubKeyNames(), entries);
        }, token);
    }

    /// <summary>Collects bounded machine-hive observations. The supplied read-only provider must return promptly.</summary>
    public static FoxitUpdaterSnapshot Collect(Func<string, string, FoxitRegistryNode?> read, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(read);
        var values = new List<FoxitUpdaterValue>();
        var issues = new List<string>();
        foreach (string view in new[] { "Registry64", "Registry32" })
        {
            var pending = new Queue<(string Path, int Depth)>();
            pending.Enqueue((@"SOFTWARE\Foxit Software", 0));
            int visited = 0;
            while (pending.TryDequeue(out var next))
            {
                token.ThrowIfCancellationRequested();
                if (++visited > 256) { issues.Add(view + ": 256-key limit reached"); break; }
                try
                {
                    var key = read(view, next.Path);
                    token.ThrowIfCancellationRequested();
                    if (key is null) { if (next.Depth > 0) issues.Add(view + ": key disappeared: " + next.Path); continue; }
                    if (next.Path.EndsWith("\\Updater", StringComparison.OrdinalIgnoreCase))
                        foreach (string name in new[] { "UpdateMode", "bNoUpdate" })
                        {
                            if (!key.Values.TryGetValue(name, out var entry)) continue;
                            object? value = entry.Value;
                            var kind = entry.Kind;
                            string? text = value is string s && s.Length <= 4096 ? s : value is int or long ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) : null;
                            values.Add(new(view.ToString(), next.Path, name, kind.ToString(), text));
                            if (text is null) issues.Add(view + ": unsupported or oversized value at " + next.Path + "\\" + name);
                        }
                    var children = key.Children;
                    if (next.Depth >= 8 && children.Count > 0) { issues.Add(view + ": depth limit at " + next.Path); continue; }
                    int remaining = 256 - visited - pending.Count;
                    if (children.Count > remaining) issues.Add(view + ": 256-key limit reached at " + next.Path);
                    foreach (string child in children.Take(256).Order(StringComparer.OrdinalIgnoreCase).Take(remaining))
                    {
                        if (string.IsNullOrWhiteSpace(child) || child.Length > 255 || child.Any(c => char.IsControl(c) || c == '\\' || c == '/'))
                        { issues.Add(view + ": invalid child at " + next.Path); continue; }
                        pending.Enqueue((next.Path + "\\" + child, next.Depth + 1));
                    }
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
                { issues.Add(view + ": unreadable key: " + next.Path); }
            }
        }
        return new(values.AsReadOnly(), issues.AsReadOnly());
    }
}
