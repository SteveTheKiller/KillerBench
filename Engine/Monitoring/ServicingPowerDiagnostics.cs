using System.Text.Json;

namespace KillerBench.Engine;

public sealed record ServicingBatterySnapshot(int ChargePercent, int StatusCode);

public static class ServicingPowerDiagnostics
{
    private const string Script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
        "$items=@(Get-CimInstance -ClassName Win32_Battery -OperationTimeoutSec 10 | Select-Object -First 17 | ForEach-Object { " +
        "[pscustomobject]@{charge=$_.EstimatedChargeRemaining; status=$_.BatteryStatus} }); " +
        "[pscustomobject]@{batteries=$items} | ConvertTo-Json -Depth 3 -Compress";

    public static RepairStep QueryStep() => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default) =>
        Interpret(await BoundedTool.RunAsync(QueryStep(), TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false));

    public static WorkflowCheck Interpret(ToolResult result)
    {
        WorkflowCheck Unknown(string detail) => new("Servicing power", CheckState.Unknown,
            detail + " No power setting was changed.", Evidence: result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return Unknown("The focused battery snapshot is unavailable or incomplete.");
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            if (!json.RootElement.TryGetProperty("batteries", out var batteries) || batteries.ValueKind != JsonValueKind.Array)
                return Unknown("Windows did not return a recognized battery array.");
            if (batteries.GetArrayLength() > 16)
                return Unknown("More than 16 battery records were returned, so coverage is incomplete.");
            if (batteries.GetArrayLength() == 0)
                return new("Servicing power", CheckState.Information,
                    "Windows reports no Win32_Battery device. This is normal for many desktop systems and does not exclude an external UPS. No power setting was changed.", Evidence: result);

            var snapshots = new List<ServicingBatterySnapshot>();
            foreach (var battery in batteries.EnumerateArray())
            {
                if (battery.ValueKind != JsonValueKind.Object ||
                    !battery.TryGetProperty("charge", out var chargeValue) || chargeValue.ValueKind != JsonValueKind.Number || !chargeValue.TryGetInt32(out int charge) || charge is < 0 or > 100 ||
                    !battery.TryGetProperty("status", out var statusValue) || statusValue.ValueKind != JsonValueKind.Number || !statusValue.TryGetInt32(out int status) || status is < 1 or > 11)
                    return Unknown("One or more battery records contain an unsupported charge or status value.");
                snapshots.Add(new(charge, status));
            }
            return Assess(snapshots);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            return Unknown("The focused battery snapshot could not be interpreted (" + error.GetType().Name + ").");
        }
    }

    public static WorkflowCheck Assess(IReadOnlyList<ServicingBatterySnapshot> batteries)
    {
        ArgumentNullException.ThrowIfNull(batteries);
        if (batteries.Count is < 1 or > 16 || batteries.Any(item => item.ChargePercent is < 0 or > 100 || item.StatusCode is < 1 or > 11))
            return new("Servicing power", CheckState.Unknown,
                "Battery evidence is incomplete or outside its bounds. No power setting was changed.");
        int minimum = batteries.Min(item => item.ChargePercent);
        int charging = batteries.Count(item => item.StatusCode is 6 or 7 or 8 or 9);
        int low = batteries.Count(item => item.ChargePercent < 30 || item.StatusCode is 4 or 5 or 9);
        string statuses = string.Join(", ", batteries.GroupBy(item => item.StatusCode).OrderBy(group => group.Key)
            .Select(group => group.Key + "=" + group.Count()));
        string detail = $"{batteries.Count} battery record{(batteries.Count == 1 ? "" : "s")}: minimum charge {minimum}%, charging-status records {charging}, status counts {statuses}. " +
            "Battery names, device identities, capacity and runtime are excluded. Status is a point-in-time provider report and does not prove stable external power or battery health. No power setting was changed.";
        return new("Servicing power", low > 0 ? CheckState.Attention : CheckState.Information, detail,
            Evidence: new(0, JsonSerializer.Serialize(batteries, Capture.JsonOptions), "", false));
    }
}
