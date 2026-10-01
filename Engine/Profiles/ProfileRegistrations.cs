using System.Text.Json;

namespace KillerBench.Engine.Profiles;

public sealed record ProfileRegistration(string Sid, string Path, string KeyName, bool BackupKey, uint? State, uint? RefCount);

public sealed record ProfileRegistrationResult(IReadOnlyList<ProfileRegistration> Items, bool Complete)
{
    public IReadOnlyList<ProfileRegistration> ForSid(string sid) =>
        [.. Items.Where(item => item.Sid.Equals(sid, StringComparison.OrdinalIgnoreCase))];
}

public static class ProfileRegistrations
{
    public static RepairStep QueryStep() => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
        "$ErrorActionPreference='Stop'; $root='Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\ProfileList'; " +
        "$items=@(Get-ChildItem -LiteralPath $root -ErrorAction Stop | Select-Object -First 1025 | ForEach-Object { " +
        "$key=$_.PSChildName; $backup=$key.EndsWith('.bak',[StringComparison]::OrdinalIgnoreCase); $sid=if($backup){$key.Substring(0,$key.Length-4)}else{$key}; " +
        "$value=Get-ItemProperty -LiteralPath $_.PSPath -ErrorAction Stop; $path=[Environment]::ExpandEnvironmentVariables([string]$value.ProfileImagePath); " +
        "$state=if($null -eq $value.State){$null}else{[uint32]$value.State}; $ref=if($null -eq $value.RefCount){$null}else{[uint32]$value.RefCount}; " +
        "[pscustomobject]@{sid=$sid; path=$path; key=$key; backup=$backup; state=$state; refCount=$ref} }); " +
        "[pscustomobject]@{items=$items} | ConvertTo-Json -Depth 3 -Compress"], true);

    public static async Task<ProfileRegistrationResult> ReadAsync(CancellationToken token = default) =>
        Interpret(await BoundedTool.RunAsync(QueryStep(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false));

    public static ProfileRegistrationResult Interpret(ToolResult result)
    {
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return new([], false);
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            if (!UniqueFields(json.RootElement) || !json.RootElement.TryGetProperty("items", out var entries) ||
                entries.ValueKind != JsonValueKind.Array || entries.GetArrayLength() > 1024) return new([], false);
            var items = new List<ProfileRegistration>();
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entries.EnumerateArray())
            {
                if (!UniqueFields(entry)) return new([], false);
                string? sid = entry.GetProperty("sid").GetString();
                string? path = entry.GetProperty("path").GetString();
                string? key = entry.GetProperty("key").GetString();
                bool backup = entry.GetProperty("backup").GetBoolean();
                if (!ProfilePlanning.IsSid(sid) || !ProfilePlanning.IsLocalProfilePath(path) ||
                    string.IsNullOrWhiteSpace(key) || key.Length > 188 || key.Any(char.IsControl) || !keys.Add(key) ||
                    backup != key.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) ||
                    !key.Equals(backup ? sid + ".bak" : sid, StringComparison.OrdinalIgnoreCase)) return new([], false);
                items.Add(new(sid!, path!, key, backup, OptionalUInt32(entry.GetProperty("state")), OptionalUInt32(entry.GetProperty("refCount"))));
            }
            return new(items.AsReadOnly(), true);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { return new([], false); }
    }

    public static WorkflowCheck Check(ProfileRegistrationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!result.Complete) return new("Profile registrations", CheckState.Unknown,
            "ProfileList registration evidence is incomplete. No removal decision is possible.");
        int backups = result.Items.Count(item => item.BackupKey);
        int referenced = result.Items.Count(item => item.RefCount is > 0);
        int activeState = result.Items.Count(item => item.State is > 0);
        int unresolved = result.Items.Count(item => item.State is null || item.RefCount is null);
        CheckState state = unresolved > 0 ? CheckState.Unknown :
            backups + referenced + activeState == 0 ? CheckState.Information : CheckState.Attention;
        return new("Profile registrations", state,
            $"{result.Items.Count} registrations; {backups} backup keys; {referenced} nonzero reference counts; {activeState} nonzero state values; {unresolved} unresolved state or reference values. " +
            "Registration state is evidence for review and does not authorize profile deletion or registry cleanup.");
    }

    private static uint? OptionalUInt32(JsonElement value) => value.ValueKind == JsonValueKind.Null ? null : value.GetUInt32();

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) return false;
        var names = new HashSet<string>(StringComparer.Ordinal);
        return value.EnumerateObject().All(property => names.Add(property.Name));
    }
}
