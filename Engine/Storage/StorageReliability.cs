using System.Text.Json;

namespace KillerBench.Engine.Storage;

public static class StorageReliability
{
    private const string Scope = "Provider counters across this PC; not mapped to the selected volume. Missing counters are unavailable, not zero. Error counts do not establish when a fault occurred. No surface test or repair performed.";
    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        const string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$items=@(Get-CimInstance -Namespace root/Microsoft/Windows/Storage -ClassName MSFT_StorageReliabilityCounter -OperationTimeoutSec 15 | " +
            "Select-Object -First 65 DeviceId,Temperature,TemperatureMax,Wear,ReadErrorsUncorrected,WriteErrorsUncorrected); " +
            "[pscustomobject]@{counters=$items}|ConvertTo-Json -Depth 4 -Compress";
        return Interpret(await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true), TimeSpan.FromSeconds(25), token: token).ConfigureAwait(false));
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult result)
    {
        WorkflowCheck Unknown(string detail) => new("Storage reliability", CheckState.Unknown, detail + " " + Scope, Evidence: result);
        if (result is null || result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error))
            return [Unknown(result?.Error?.Contains("MI RESULT 2,Microsoft.Management.Infrastructure.CimCmdlets.GetCimInstanceCommand", StringComparison.Ordinal) == true
                ? "Windows denied access to reliability counters. Try an administrator diagnostic session; retained error output identifies the failed provider."
                : "Storage reliability counters could not be read completely. Review the retained provider error output.")];
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 1 ||
                !root.TryGetProperty("counters", out var counters) || counters.ValueKind != JsonValueKind.Array || counters.GetArrayLength() > 65)
                return [Unknown("Unrecognized counter envelope.")];
            if (counters.GetArrayLength() == 0) return [Unknown("No reliability counters were exposed by the storage provider.")];
            var checks = new List<WorkflowCheck>();
            var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var counter in counters.EnumerateArray().Take(64))
            {
                if (counter.ValueKind != JsonValueKind.Object || counter.EnumerateObject().Count() != 6 ||
                    counter.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != 6 ||
                    !counter.TryGetProperty("DeviceId", out var id) || id.ValueKind != JsonValueKind.String ||
                    !DriverReference.ValidText(id.GetString(), 256) || !identities.Add(id.GetString()!))
                    return [Unknown("Missing or duplicate device identity in counter evidence.")];
                ulong? Read(string key, ulong max = ulong.MaxValue)
                {
                    var value = counter.GetProperty(key);
                    if (value.ValueKind == JsonValueKind.Null) return null;
                    if (value.ValueKind != JsonValueKind.Number || !value.TryGetUInt64(out ulong number) || number > max)
                        throw new InvalidDataException("Invalid counter value.");
                    return number;
                }
                var temperature = Read("Temperature", 255);
                var maximum = Read("TemperatureMax", 255);
                var wear = Read("Wear", 255);
                var reads = Read("ReadErrorsUncorrected");
                var writes = Read("WriteErrorsUncorrected");
                var findings = new List<string>();
                if (temperature.HasValue && maximum > 0 && temperature > maximum) findings.Add("Temperature exceeds the provider's reported operating maximum.");
                if (wear >= 100) findings.Add("Provider reports the estimated wear limit has been reached.");
                if (reads > 0 || writes > 0) findings.Add("Provider reports uncorrected I/O errors. Confirm backups and investigate device history before repair writes.");
                string Show(ulong? value) => value?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "unavailable";
                bool any = temperature.HasValue || maximum.HasValue || wear.HasValue || reads.HasValue || writes.HasValue;
                checks.Add(new("Storage reliability: " + id.GetString(), findings.Count > 0 ? CheckState.Attention : any ? CheckState.Information : CheckState.Unknown,
                    $"Temperature: {Show(temperature)} C; operating maximum: {Show(maximum)} C; wear: {Show(wear)}%; uncorrected reads: {Show(reads)}; writes: {Show(writes)}. " +
                    string.Join(" ", findings) + " " + Scope, Evidence: result));
            }
            if (counters.GetArrayLength() > 64) checks.Add(Unknown("Only the first 64 counter records were evaluated; remaining devices are unassessed."));
            return checks;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or InvalidDataException or KeyNotFoundException)
        { return [Unknown("Malformed storage reliability evidence.")]; }
    }
}
