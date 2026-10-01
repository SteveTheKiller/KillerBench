using System.Text.Json;

namespace KillerBench.Engine.Profiles;

public sealed record ProfileInventoryResult(IReadOnlyList<ProfileSnapshot> Profiles, bool Complete);

public static class ProfileInventory
{
    public static RepairStep QueryStep() => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
        "$ErrorActionPreference='Stop'; $current=[Security.Principal.WindowsIdentity]::GetCurrent().User.Value; " +
        "$items=@(Get-CimInstance Win32_UserProfile -ErrorAction Stop | Select-Object -First 1025 | ForEach-Object { " +
        "$last=$null; if($_.LastUseTime){$last=$_.LastUseTime.ToUniversalTime().ToString('o')}; " +
        "[pscustomobject]@{sid=$_.SID; path=$_.LocalPath; loaded=$_.Loaded; special=$_.Special; last=$last; current=($_.SID -eq $current)} }); " +
        "[pscustomobject]@{profiles=$items} | ConvertTo-Json -Depth 3 -Compress"], true);

    public static async Task<ProfileInventoryResult> ReadAsync(CancellationToken token = default) =>
        Interpret(await BoundedTool.RunAsync(QueryStep(), TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false));

    public static ProfileInventoryResult Interpret(ToolResult result)
    {
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut) return new([], false);
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            if (!UniqueFields(json.RootElement)) return new([], false);
            var entries = json.RootElement.GetProperty("profiles");
            if (entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() > 1024) return new([], false);
            var profiles = new List<ProfileSnapshot>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries.EnumerateArray())
            {
                if (!UniqueFields(entry)) return new([], false);
                string? sid = entry.GetProperty("sid").GetString(), path = entry.GetProperty("path").GetString();
                if (!ProfilePlanning.IsSid(sid) || !seen.Add(sid!) ||
                    string.IsNullOrWhiteSpace(path) || path.Length > 32767 || path.Any(char.IsControl)) return new([], false);
                var last = entry.GetProperty("last");
                profiles.Add(new(sid!, path, OptionalBoolean(entry.GetProperty("loaded")), OptionalBoolean(entry.GetProperty("special")),
                    last.ValueKind == JsonValueKind.Null ? null : last.GetDateTimeOffset(), entry.GetProperty("current").GetBoolean()));
            }
            return new(profiles.AsReadOnly(), true);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { return new([], false); }
    }

    private static bool? OptionalBoolean(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : value.GetBoolean();

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) return false;
        var names = new HashSet<string>(StringComparer.Ordinal);
        return value.EnumerateObject().All(property => names.Add(property.Name));
    }
}
