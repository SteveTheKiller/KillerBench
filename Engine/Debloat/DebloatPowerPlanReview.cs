using System.Text.Json;

namespace KillerBench.Engine.Debloat;

public sealed record DebloatPowerPlan(string Id, string Name, bool Active);
public sealed record DebloatPowerPlanEvidence(IReadOnlyList<DebloatPowerPlan> Plans,
    DebloatPowerPlan? HighPerformance, bool Complete, ToolResult Evidence, string Detail);

public interface IDebloatPowerPlanReader
{
    Task<DebloatPowerPlanEvidence> ReadAsync(CancellationToken token);
}

public sealed class WindowsDebloatPowerPlanReader : IDebloatPowerPlanReader
{
    public const string HighPerformanceId = "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c";

    public async Task<DebloatPowerPlanEvidence> ReadAsync(CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Power plan inventory requires Windows.");
        const string script = "$ErrorActionPreference='Stop';$items=@(Get-CimInstance -Namespace root/cimv2/power -ClassName Win32_PowerPlan -ErrorAction Stop|Sort-Object InstanceId|Select-Object -First 65);" +
            "$rows=@($items|Select-Object -First 64|ForEach-Object{[pscustomobject]@{id=([regex]::Match([string]$_.InstanceId,'\\{([0-9a-fA-F-]{36})\\}').Groups[1].Value.ToLowerInvariant());name=[string]$_.ElementName;active=[bool]$_.IsActive}});" +
            "[pscustomobject]@{complete=($items.Count-le 64);plans=$rows}|ConvertTo-Json -Depth 3 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static DebloatPowerPlanEvidence Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        DebloatPowerPlanEvidence Unknown() => new([], null, false, evidence,
            "Power plan inventory is unavailable or incomplete. High performance availability was not inferred.");
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 2 ||
                !root.TryGetProperty("complete", out var completeValue) || completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("plans", out var rows) || rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() > 64) return Unknown();
            bool complete = completeValue.GetBoolean();
            var plans = new List<DebloatPowerPlan>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in rows.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object || row.EnumerateObject().Count() != 3 ||
                    !row.TryGetProperty("id", out var idValue) || idValue.ValueKind != JsonValueKind.String ||
                    !Guid.TryParseExact(idValue.GetString(), "D", out Guid id) || !ids.Add(id.ToString("D")) ||
                    !row.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
                    !DriverReference.ValidText(nameValue.GetString(), 256) ||
                    !row.TryGetProperty("active", out var activeValue) || activeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Unknown();
                plans.Add(new(id.ToString("D"), nameValue.GetString()!, activeValue.GetBoolean()));
            }
            if (!complete || plans.Count(plan => plan.Active) != 1) return Unknown();
            var high = plans.Where(plan => plan.Id.Equals(HighPerformanceId, StringComparison.OrdinalIgnoreCase) ||
                    plan.Name.Equals("High performance", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (high.Length > 1) return Unknown();
            return new(plans.AsReadOnly(), high.SingleOrDefault(), true, evidence,
                high.Length == 0 ? "No exact High performance plan identity was found in the complete inventory." :
                high[0].Active ? "The exact High performance plan is already active." : "The exact High performance plan is available but inactive.");
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException) { return Unknown(); }
    }
}
