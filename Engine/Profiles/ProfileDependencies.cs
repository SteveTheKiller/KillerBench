using System.Text.Json;

namespace KillerBench.Engine.Profiles;

public sealed record ProfileDependency(string Kind, string Name, string? Sid);
public sealed record ProfileDependencyResult(IReadOnlyList<ProfileDependency> Items, bool Complete)
{
    public IReadOnlyList<ProfileDependency> ForSid(string sid) => [.. Items.Where(i => string.Equals(i.Sid, sid, StringComparison.OrdinalIgnoreCase))];
}

public static class ProfileDependencies
{
    public static RepairStep QueryStep() => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
        "$ErrorActionPreference='Stop'; function Resolve-Identity($name) { " +
        "if(!$name){return $null}; if($name -eq 'LocalSystem'){return 'S-1-5-18'}; " +
        "try { if($name -match '^S-1-'){return ([Security.Principal.SecurityIdentifier]::new($name)).Value}; " +
        "return ([Security.Principal.NTAccount]::new($name)).Translate([Security.Principal.SecurityIdentifier]).Value } catch { return $null } }; " +
        "$items=@(Get-CimInstance Win32_Service -ErrorAction Stop | Select-Object -First 2049 | ForEach-Object { " +
        "[pscustomobject]@{kind='Service'; name=$_.Name; sid=(Resolve-Identity $_.StartName)} }; " +
        "Get-ScheduledTask -ErrorAction Stop | Select-Object -First 2049 | ForEach-Object { " +
        "[pscustomobject]@{kind='Task'; name=($_.TaskPath+$_.TaskName); sid=(Resolve-Identity $_.Principal.UserId)} }); " +
        "[pscustomobject]@{items=$items} | ConvertTo-Json -Depth 3 -Compress"], true);

    public static async Task<ProfileDependencyResult> ReadAsync(CancellationToken token = default) =>
        Interpret(await BoundedTool.RunAsync(QueryStep(), TimeSpan.FromSeconds(60), token: token).ConfigureAwait(false));

    public static ProfileDependencyResult Interpret(ToolResult result)
    {
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut) return new([], false);
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            if (!UniqueFields(json.RootElement)) return new([], false);
            var entries = json.RootElement.GetProperty("items");
            if (entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() > 4096) return new([], false);
            var items = new List<ProfileDependency>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries.EnumerateArray())
            {
                if (!UniqueFields(entry)) return new([], false);
                string? kind = entry.GetProperty("kind").GetString(), name = entry.GetProperty("name").GetString(), sid = entry.GetProperty("sid").GetString();
                if (kind is not ("Service" or "Task") || string.IsNullOrWhiteSpace(name) || name.Length > 1024 || name.Any(char.IsControl) || !seen.Add(kind + ":" + name)) return new([], false);
                items.Add(new(kind, name, sid));
            }
            bool complete = items.All(i => ProfilePlanning.IsSid(i.Sid)) && items.Count(i => i.Kind == "Service") <= 2048 && items.Count(i => i.Kind == "Task") <= 2048;
            return new(items.AsReadOnly(), complete);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
        { return new([], false); }
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) return false;
        var names = new HashSet<string>(StringComparer.Ordinal);
        return value.EnumerateObject().All(property => names.Add(property.Name));
    }
}
