using System.Text.Json;

namespace KillerBench.Engine;

public static class AdapterStatisticsDiagnostics
{
    private const int MaximumAdapters = 256;
    private const string Scope = " Adapter counters are cumulative since the adapter or computer was restarted. A nonzero value identifies evidence to review, not a current outage or faulty adapter. No adapter, counter, protocol, route, service or policy setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$a=@(Get-NetAdapter -Name '*' -ErrorAction Stop|Sort-Object ifIndex|Select-Object -First 257);" +
        "$s=@(Get-NetAdapterStatistics -Name '*' -ErrorAction Stop|Select-Object -First 257);$complete=($a.Count-le 256-and $s.Count-le 256);$rows=@();" +
        "foreach($v in @($s|Select-Object -First 256)){$m=@($a|Where-Object{[string]$_.Name-ceq[string]$v.Name});if($m.Count-ne 1){$complete=$false;continue};$x=$m[0];" +
        "$rows+=[pscustomobject]@{name=[string]$x.Name;interfaceIndex=[int64]$x.ifIndex;status=[string]$x.Status;hardware=[bool]$x.HardwareInterface;receivedBytes=[uint64]$v.ReceivedBytes;sentBytes=[uint64]$v.SentBytes;receivedErrors=[uint64]$v.ReceivedPacketErrors;outboundErrors=[uint64]$v.OutboundPacketErrors;receivedDiscards=[uint64]$v.ReceivedDiscardedPackets;outboundDiscards=[uint64]$v.OutboundDiscardedPackets}};" +
        "[pscustomobject]@{complete=$complete;adapters=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Network adapter statistics require Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var evidence = await (execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Adapter-statistics provider returned no task.")).WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Adapter-statistics provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Adapter-statistics inspection exceeded its overall deadline.", new(-1, "", "Adapter-statistics inspection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException or
            System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Adapter-statistics inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Adapter-statistics collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) || !complete ||
                !root.TryGetProperty("adapters", out var adapters) || adapters.ValueKind != JsonValueKind.Array || adapters.GetArrayLength() > MaximumAdapters)
                return Unknown("Adapter-statistics evidence was malformed, incomplete or exceeded its bound.", evidence);

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var indexes = new HashSet<int>();
            int active = 0, reviewed = 0;
            ulong receivedErrors = 0, outboundErrors = 0, receivedDiscards = 0, outboundDiscards = 0;
            foreach (var adapter in adapters.EnumerateArray())
            {
                if (!Unique(adapter, 10) || !Text(adapter, "name", 256, out string name) ||
                    !Integer(adapter, "interfaceIndex", 1, int.MaxValue, out int index) || !Text(adapter, "status", 64, out string status) ||
                    !Boolean(adapter, "hardware", out bool hardware) || !Unsigned(adapter, "receivedBytes", out _) ||
                    !Unsigned(adapter, "sentBytes", out _) || !Unsigned(adapter, "receivedErrors", out ulong rxErrors) ||
                    !Unsigned(adapter, "outboundErrors", out ulong txErrors) || !Unsigned(adapter, "receivedDiscards", out ulong rxDiscards) ||
                    !Unsigned(adapter, "outboundDiscards", out ulong txDiscards) || !names.Add(name) || !indexes.Add(index))
                    return Unknown("Adapter-statistics evidence contained an invalid or duplicate adapter.", evidence);
                if (hardware && status.Equals("Up", StringComparison.OrdinalIgnoreCase)) active++;
                if (rxErrors > 0 || txErrors > 0 || rxDiscards > 0 || txDiscards > 0) reviewed++;
                receivedErrors = Add(receivedErrors, rxErrors);
                outboundErrors = Add(outboundErrors, txErrors);
                receivedDiscards = Add(receivedDiscards, rxDiscards);
                outboundDiscards = Add(outboundDiscards, txDiscards);
            }
            if (adapters.GetArrayLength() == 0)
                return new("Network adapter statistics", CheckState.Information, "No visible network adapters returned statistics, so no counters were available." + Scope, Evidence: evidence);
            string detail = $"Reviewed {adapters.GetArrayLength()} visible adapter{(adapters.GetArrayLength() == 1 ? "" : "s")} that returned statistics, including {active} active hardware adapter{(active == 1 ? "" : "s")}. " +
                $"{reviewed} adapters reported at least one cumulative error or discarded packet. Totals: {receivedErrors} received errors, {outboundErrors} outbound errors, {receivedDiscards} received discards and {outboundDiscards} outbound discards.";
            if (reviewed > 0) detail += " Compare repeated snapshots and the affected adapter's link, driver and switch-port evidence before selecting a repair.";
            else detail += " No reported adapter had a nonzero error or discard counter in this snapshot.";
            return new("Network adapter statistics", reviewed > 0 ? CheckState.Attention : CheckState.Information, detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("Adapter-statistics evidence could not be interpreted.", evidence);
        }
    }

    private static ulong Add(ulong left, ulong right) => ulong.MaxValue - left < right ? ulong.MaxValue : left + right;
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new("Network adapter statistics", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value) { value = false; if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false; value = item.GetBoolean(); return true; }
    private static bool Integer(JsonElement owner, string name, int minimum, int maximum, out int value) { value = 0; return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out value) && value >= minimum && value <= maximum; }
    private static bool Unsigned(JsonElement owner, string name, out ulong value) { value = 0; return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetUInt64(out value); }
    private static bool Text(JsonElement owner, string name, int maximum, out string value) { value = ""; if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text || text.Length is < 1 || text.Length > maximum || text != text.Trim() || text.Any(char.IsControl)) return false; value = text; return true; }
}
