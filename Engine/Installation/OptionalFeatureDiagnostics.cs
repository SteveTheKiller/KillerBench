using System.Text.Json;

namespace KillerBench.Engine.Installation;

public static class OptionalFeatureDiagnostics
{
    private const int MaximumFeatures = 1024;
    private static readonly HashSet<string> States = new(StringComparer.Ordinal)
    {
        "Enabled", "Disabled", "EnablePending", "DisablePending", "Removed"
    };

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$rows=[Collections.Generic.List[object]]::new();" +
            "foreach($feature in @(Get-WindowsOptionalFeature -Online -ErrorAction Stop|Sort-Object FeatureName)){if($rows.Count-ge 1025){break};" +
            "$rows.Add([pscustomobject]@{name=[string]$feature.FeatureName;state=[string]$feature.State})};" +
            "[pscustomobject]@{complete=($rows.Count-le 1024);features=$rows}|ConvertTo-Json -Depth 4 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromMinutes(2), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        const string title = "Optional Windows features";
        WorkflowCheck Unknown(string detail = "Optional Windows feature state is unavailable or incomplete. No feature or servicing setting changed.") =>
            new(title, CheckState.Unknown, detail, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("complete", out var completeValue) ||
                completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("features", out var featuresValue) || featuresValue.ValueKind != JsonValueKind.Array) return Unknown();
            if (!completeValue.GetBoolean() || featuresValue.GetArrayLength() > MaximumFeatures)
                return Unknown("The optional Windows feature inventory exceeded its evidence bound. Coverage is incomplete and no change was selected.");
            var features = new List<(string Name, string State)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var feature in featuresValue.EnumerateArray())
            {
                if (feature.ValueKind != JsonValueKind.Object ||
                    !feature.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    !feature.TryGetProperty("state", out var stateValue) || stateValue.ValueKind != JsonValueKind.String) return Unknown();
                string name = nameValue.GetString()!, state = stateValue.GetString()!;
                if (!DriverReference.ValidText(name, 256) || name != name.Trim() || !States.Contains(state) || !seen.Add(name)) return Unknown();
                features.Add((name, state));
            }
            var pending = features.Where(feature => feature.State is "EnablePending" or "DisablePending").ToArray();
            string counts = string.Join(", ", features.GroupBy(feature => feature.State, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal).Select(group => group.Key + ": " + group.Count()));
            string detail = $"Reviewed {features.Count} optional Windows feature{(features.Count == 1 ? "" : "s")}. " +
                (counts.Length == 0 ? "No features were returned." : counts + ".");
            if (pending.Length > 0)
                detail += "\nPending feature changes: " + string.Join("; ", pending.Take(20).Select(feature => feature.Name + " (" + feature.State + ")")) +
                    (pending.Length > 20 ? "; additional features remain in evidence." : ".");
            detail += "\nFeature state is configuration evidence, not proof of a fault or authorization to enable, disable, remove or restore a feature. No feature, payload, source, policy or servicing setting changed.";
            return new(title, pending.Length > 0 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return Unknown(); }
    }
}
