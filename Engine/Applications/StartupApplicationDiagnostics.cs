using System.Text.Json;

namespace KillerBench.Engine;

public static class StartupApplicationDiagnostics
{
    private const int MaximumEntries = 512;
    private static readonly HashSet<string> Sources = new(StringComparer.Ordinal)
    {
        "Machine64Run", "Machine64RunOnce", "Machine32Run", "Machine32RunOnce",
        "UserRun", "UserRunOnce", "CommonStartup", "UserStartup"
    };

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$rows=[Collections.Generic.List[object]]::new();$issues=[Collections.Generic.List[string]]::new();" +
            "$specs=@(@('LocalMachine','Registry64','Machine64Run','SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run'),@('LocalMachine','Registry64','Machine64RunOnce','SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce'),@('LocalMachine','Registry32','Machine32Run','SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run'),@('LocalMachine','Registry32','Machine32RunOnce','SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce'),@('CurrentUser','Default','UserRun','SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run'),@('CurrentUser','Default','UserRunOnce','SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce'));" +
            "foreach($s in $specs){try{$h=[Microsoft.Win32.RegistryHive]::$($s[0]);$v=[Microsoft.Win32.RegistryView]::$($s[1]);$b=[Microsoft.Win32.RegistryKey]::OpenBaseKey($h,$v);try{$k=$b.OpenSubKey($s[3]);if($null-ne$k){try{foreach($n in @($k.GetValueNames()|Sort-Object)){if($rows.Count-ge 513){break};$rows.Add([pscustomobject]@{source=$s[2];name=$n;kind='Registry'})}}finally{$k.Dispose()}}}finally{$b.Dispose()}}catch{$issues.Add($s[2])}};" +
            "$folders=@(@('CommonStartup',[Environment]::GetFolderPath('CommonStartup')),@('UserStartup',[Environment]::GetFolderPath('Startup')));foreach($f in $folders){try{if(-not[string]::IsNullOrWhiteSpace($f[1])-and[IO.Directory]::Exists($f[1])){foreach($p in @([IO.Directory]::EnumerateFiles($f[1])|Sort-Object)){if($rows.Count-ge 513){break};$file=[IO.Path]::GetFileName($p);if($file-eq'desktop.ini'){continue};$rows.Add([pscustomobject]@{source=$f[0];name=$file;kind='File'})}}}catch{$issues.Add($f[0])}};" +
            "[pscustomobject]@{complete=($issues.Count-eq 0-and$rows.Count-le 512);issues=$issues;entries=$rows}|ConvertTo-Json -Depth 4 -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        const string name = "Startup applications";
        WorkflowCheck Unknown(string detail = "Startup registration inventory is unavailable or incomplete. No registration, file, application or setting changed.") =>
            new(name, CheckState.Unknown, detail, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("complete", out var completeValue) ||
                completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("issues", out var issuesValue) || issuesValue.ValueKind != JsonValueKind.Array ||
                !root.TryGetProperty("entries", out var entriesValue) || entriesValue.ValueKind != JsonValueKind.Array) return Unknown();
            if (issuesValue.GetArrayLength() > Sources.Count || entriesValue.GetArrayLength() > MaximumEntries) return Unknown("The startup inventory exceeded its evidence bounds. Coverage is incomplete and no change was selected.");
            var issues = new HashSet<string>(StringComparer.Ordinal);
            foreach (var issue in issuesValue.EnumerateArray())
                if (issue.ValueKind != JsonValueKind.String || !Sources.Contains(issue.GetString()!) || !issues.Add(issue.GetString()!)) return Unknown();
            var entries = new List<(string Source, string Name, string Kind)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entriesValue.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object || !entry.TryGetProperty("source", out var sourceValue) || sourceValue.ValueKind != JsonValueKind.String ||
                    !entry.TryGetProperty("name", out var entryNameValue) || entryNameValue.ValueKind != JsonValueKind.String ||
                    !entry.TryGetProperty("kind", out var kindValue) || kindValue.ValueKind != JsonValueKind.String) return Unknown();
                string source = sourceValue.GetString()!, entryName = entryNameValue.GetString()!, kind = kindValue.GetString()!;
                if (!Sources.Contains(source) || kind is not ("Registry" or "File") || !DriverReference.ValidText(entryName, 512) || entryName != entryName.Trim() ||
                    !seen.Add(source + "\0" + entryName)) return Unknown();
                entries.Add((source, entryName, kind));
            }
            bool complete = completeValue.GetBoolean() && issues.Count == 0;
            string counts = string.Join(", ", Sources.Select(source => source + ": " + entries.Count(entry => entry.Source == source)));
            string detail = $"Reviewed {entries.Count} startup registration{(entries.Count == 1 ? "" : "s")}. {counts}. Command lines and target paths were not collected.";
            if (entries.Count > 0) detail += "\nRegistered names: " + string.Join("; ", entries.Take(20).Select(entry => entry.Source + ": " + entry.Name)) + (entries.Count > 20 ? "; additional names remain in evidence." : ".");
            detail += "\nRegistration does not prove that an item ran, caused a delay, is unwanted or is safe to disable. No registration, file, application, task, service or policy change is selected.";
            if (!complete) detail += "\nUnavailable sources: " + (issues.Count == 0 ? "inventory limit reached" : string.Join(", ", issues)) + ". Coverage is incomplete.";
            return new(name, complete ? CheckState.Information : CheckState.Unknown, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return Unknown();
        }
    }
}

public static class StartupScheduledTaskDiagnostics
{
    private const int MaximumEntries = 512;
    private static readonly HashSet<string> TriggerKinds = new(StringComparer.Ordinal) { "Boot", "Logon" };
    private static readonly HashSet<string> States = new(StringComparer.Ordinal) { "Unknown", "Disabled", "Queued", "Ready", "Running" };

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$rows=[Collections.Generic.List[object]]::new();" +
            "foreach($task in @(Get-ScheduledTask -ErrorAction Stop|Sort-Object TaskPath,TaskName)){foreach($trigger in @($task.Triggers)){" +
            "$kind=switch($trigger.CimClass.CimClassName){'MSFT_TaskBootTrigger'{'Boot'}'MSFT_TaskLogonTrigger'{'Logon'}default{$null}};" +
            "if($null-eq$kind){continue};if($rows.Count-ge 513){break};$rows.Add([pscustomobject]@{path=$task.TaskPath;name=$task.TaskName;trigger=$kind;state=[string]$task.State})};if($rows.Count-ge 513){break}};" +
            "[pscustomobject]@{complete=($rows.Count-le 512);entries=$rows}|ConvertTo-Json -Depth 4 -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        const string title = "Startup scheduled tasks";
        WorkflowCheck Unknown(string detail = "Startup scheduled-task inventory is unavailable or incomplete. No task or setting changed.") =>
            new(title, CheckState.Unknown, detail, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("complete", out var completeValue) ||
                completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("entries", out var entriesValue) || entriesValue.ValueKind != JsonValueKind.Array) return Unknown();
            if (!completeValue.GetBoolean() || entriesValue.GetArrayLength() > MaximumEntries)
                return Unknown("The startup scheduled-task inventory exceeded its evidence bound. Coverage is incomplete and no change was selected.");
            var entries = new List<(string Path, string Name, string Trigger, string State)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in entriesValue.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object ||
                    !entry.TryGetProperty("path", out var pathValue) || pathValue.ValueKind != JsonValueKind.String ||
                    !entry.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    !entry.TryGetProperty("trigger", out var triggerValue) || triggerValue.ValueKind != JsonValueKind.String ||
                    !entry.TryGetProperty("state", out var stateValue) || stateValue.ValueKind != JsonValueKind.String) return Unknown();
                string path = pathValue.GetString()!, name = nameValue.GetString()!, trigger = triggerValue.GetString()!, state = stateValue.GetString()!;
                if (!DriverReference.ValidText(path, 512) || !DriverReference.ValidText(name, 256) || path != path.Trim() || name != name.Trim() ||
                    !path.StartsWith("\\", StringComparison.Ordinal) || !TriggerKinds.Contains(trigger) || !States.Contains(state) ||
                    !seen.Add(path + "\0" + name + "\0" + trigger)) return Unknown();
                entries.Add((path, name, trigger, state));
            }
            string detail = $"Reviewed {entries.Count} boot or logon task trigger{(entries.Count == 1 ? "" : "s")}.";
            if (entries.Count > 0) detail += "\nTasks: " + string.Join("; ", entries.Take(20).Select(entry =>
                entry.Path + entry.Name + " (" + entry.Trigger + ", " + entry.State + ")")) +
                (entries.Count > 20 ? "; additional tasks remain in evidence." : ".");
            detail += "\nActions, arguments, accounts and credentials were not collected. A registered trigger does not prove that a task ran, delayed startup or is safe to disable. No task, application, service or policy change is selected.";
            return new(title, CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return Unknown(); }
    }
}
