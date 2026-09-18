using System.Text.Json;

namespace KillerBench.Engine;

public static class StartupApprovalDiagnostics
{
    private const int MaximumEntries = 512;
    private static readonly HashSet<string> Sources = new(StringComparer.Ordinal)
    {
        "Machine64Run", "Machine64Run32", "Machine64StartupFolder",
        "UserRun", "UserRun32", "UserStartupFolder"
    };

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$rows=[Collections.Generic.List[object]]::new();$issues=[Collections.Generic.List[string]]::new();$capped=$false;" +
            "$specs=@(@('LocalMachine','Registry64','Machine64Run','SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run'),@('LocalMachine','Registry64','Machine64Run32','SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run32'),@('LocalMachine','Registry64','Machine64StartupFolder','SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\StartupFolder'),@('CurrentUser','Default','UserRun','SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run'),@('CurrentUser','Default','UserRun32','SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\Run32'),@('CurrentUser','Default','UserStartupFolder','SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\StartupApproved\\StartupFolder'));" +
            "foreach($s in $specs){try{$h=[Microsoft.Win32.RegistryHive]::$($s[0]);$v=[Microsoft.Win32.RegistryView]::$($s[1]);$b=[Microsoft.Win32.RegistryKey]::OpenBaseKey($h,$v);try{$k=$b.OpenSubKey($s[3]);if($null-ne$k){try{foreach($n in @($k.GetValueNames()|Sort-Object)){if(($rows.Count+$issues.Count)-ge 512){$capped=$true;break};$value=$k.GetValue($n,$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames);if($value-isnot[byte[]]-or$value.Length-lt 1-or$value.Length-gt 256){$issues.Add($s[2]+':'+$n);continue};$rows.Add([pscustomobject]@{source=$s[2];name=$n;state=[int]$value[0];bytes=$value.Length})}}finally{$k.Dispose()}}}finally{$b.Dispose()}}catch{if(($rows.Count+$issues.Count)-lt 512){$issues.Add($s[2])}else{$capped=$true}}};" +
            "[pscustomobject]@{complete=($issues.Count-eq 0-and-not$capped);issues=$issues;entries=$rows}|ConvertTo-Json -Depth 4 -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        const string title = "Startup approval state";
        WorkflowCheck Unknown(string detail = "Windows startup approval evidence is unavailable or incomplete. No startup setting changed.") =>
            new(title, CheckState.Unknown, detail, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 3 ||
                !Unique(root, "complete", out var completeValue) || completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !Unique(root, "issues", out var issuesValue) || issuesValue.ValueKind != JsonValueKind.Array ||
                !Unique(root, "entries", out var entriesValue) || entriesValue.ValueKind != JsonValueKind.Array ||
                issuesValue.GetArrayLength() + entriesValue.GetArrayLength() > MaximumEntries) return Unknown();

            var issues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var issue in issuesValue.EnumerateArray())
                if (issue.ValueKind != JsonValueKind.String || !ValidIssue(issue.GetString()) || !issues.Add(issue.GetString()!)) return Unknown();

            var entries = new List<(string Source, string Name, int State)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entriesValue.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object || entry.EnumerateObject().Count() != 4 ||
                    !Unique(entry, "source", out var sourceValue) || sourceValue.ValueKind != JsonValueKind.String ||
                    !Unique(entry, "name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    !Unique(entry, "state", out var stateValue) || !stateValue.TryGetInt32(out int state) || state is < 0 or > 255 ||
                    !Unique(entry, "bytes", out var bytesValue) || !bytesValue.TryGetInt32(out int bytes) || bytes is < 1 or > 256) return Unknown();
                string source = sourceValue.GetString()!, name = nameValue.GetString()!;
                if (!Sources.Contains(source) || !DriverReference.ValidText(name, 512) || name != name.Trim() || !seen.Add(source + "\0" + name)) return Unknown();
                entries.Add((source, name, state));
            }

            bool recognized = entries.All(entry => entry.State is 2 or 3);
            bool complete = completeValue.GetBoolean() && issues.Count == 0 && recognized;
            int enabled = entries.Count(entry => entry.State == 2), disabled = entries.Count(entry => entry.State == 3);
            string detail = $"Reviewed {entries.Count} Windows startup approval entr{(entries.Count == 1 ? "y" : "ies")}: {enabled} enabled and {disabled} disabled.";
            if (entries.Count > 0)
                detail += "\nApproval entries: " + string.Join("; ", entries.Take(24).Select(entry => entry.Source + ": " + entry.Name + " (" + State(entry.State) + ")")) +
                    (entries.Count > 24 ? "; additional entries remain in evidence." : ".");
            detail += "\nApproval state is retained separately from startup registration and does not prove that an entry exists, ran, delayed startup or is safe to change. Binary timestamps and command lines were not collected. No startup setting changed.";
            if (!complete)
                detail += "\nCoverage is incomplete because a source, value or state marker could not be interpreted. No enabled or disabled state was inferred for that evidence.";
            return new(title, complete ? CheckState.Information : CheckState.Unknown, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return Unknown(); }
    }

    private static string State(int value) => value switch { 2 => "Enabled", 3 => "Disabled", _ => "Unknown marker " + value };

    private static bool ValidIssue(string? issue)
    {
        if (issue is null || !DriverReference.ValidText(issue, 800) || issue != issue.Trim()) return false;
        int separator = issue.IndexOf(':');
        string source = separator < 0 ? issue : issue[..separator];
        if (!Sources.Contains(source)) return false;
        return separator < 0 || DriverReference.ValidText(issue[(separator + 1)..], 512);
    }

    private static bool Unique(JsonElement owner, string name, out JsonElement value)
    {
        var matches = owner.EnumerateObject().Where(property => property.NameEquals(name)).ToArray();
        value = matches.Length == 1 ? matches[0].Value : default;
        return matches.Length == 1;
    }
}
