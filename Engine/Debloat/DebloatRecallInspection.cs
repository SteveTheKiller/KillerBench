using System.Text.Json;

namespace KillerBench.Engine;

public sealed record DebloatRecallEvidence(string FeatureName, string State, bool Present,
    bool Complete, ToolResult Evidence, string Detail);

public interface IDebloatRecallReader
{
    Task<DebloatRecallEvidence> ReadAsync(CancellationToken token);
}

public sealed class WindowsDebloatRecallReader : IDebloatRecallReader
{
    public async Task<DebloatRecallEvidence> ReadAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Recall feature inspection requires Windows.");
        const string script = "$ErrorActionPreference='Stop';$f=@(Get-WindowsOptionalFeature -Online -ErrorAction Stop|Where-Object {$_.FeatureName -eq 'Recall'}|Select-Object -First 2);" +
            "[pscustomobject]@{present=($f.Count-eq 1);name=if($f.Count-eq 1){[string]$f[0].FeatureName}else{'Recall'};state=if($f.Count-eq 1){[string]$f[0].State}else{'NotPresent'}}|ConvertTo-Json -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static DebloatRecallEvidence Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        DebloatRecallEvidence Unknown() => new("Recall", "Unknown", false, false, evidence,
            "Recall optional-feature state is unavailable or incomplete. Absence was not established.");
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != 3 ||
                !root.TryGetProperty("present", out var presentValue) || presentValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty("state", out var stateValue) || stateValue.ValueKind != JsonValueKind.String) return Unknown();
            bool present = presentValue.GetBoolean();
            string name = nameValue.GetString()!, state = stateValue.GetString()!;
            if (!name.Equals("Recall", StringComparison.OrdinalIgnoreCase) ||
                state is not ("Enabled" or "Disabled" or "EnablePending" or "DisablePending" or "Removed" or "NotPresent") ||
                present == (state == "NotPresent")) return Unknown();
            return new(name, state, present, true, evidence,
                present ? "Exact Recall optional-feature state was read from the running Windows image." :
                    "The exact Recall optional feature was not returned by the provider.");
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException) { return Unknown(); }
    }
}
