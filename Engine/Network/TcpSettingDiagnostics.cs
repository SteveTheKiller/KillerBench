using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class TcpSettingDiagnostics
{
    private const int MaximumSettings = 64;
    private const string Scope = " TCP templates are configuration context, not proof of a network fault or performance problem. Specialized workloads and managed systems can intentionally use nondefault values. No TCP template, connection, port range, adapter, route, firewall, service or policy setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-NetTCPSetting -ErrorAction Stop|Sort-Object SettingName|Select-Object -First 65);" +
        "$rows=@($all|Select-Object -First 64|ForEach-Object{[pscustomobject]@{settingName=[string]$_.SettingName;minRto=[uint64]$_.MinRto;initialCongestionWindow=[uint64]$_.InitialCongestionWindow;congestionProvider=[uint64]$_.CongestionProvider;autoTuningLocal=[uint64]$_.AutoTuningLevelLocal;autoTuningEffective=[uint64]$_.AutoTuningLevelEffective;ecnCapability=[uint64]$_.EcnCapability;timestamps=[uint64]$_.Timestamps;initialRto=[uint64]$_.InitialRto;scalingHeuristics=[uint64]$_.ScalingHeuristics;maxSynRetransmissions=[uint64]$_.MaxSynRetransmissions}});" +
        "[pscustomobject]@{complete=($all.Count-le 64);settings=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("TCP setting inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) =>
                BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var task = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("TCP setting provider returned no task.");
            var evidence = await task.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("TCP setting provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("TCP setting inspection exceeded its overall deadline.",
                new(-1, "", "TCP setting deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("TCP setting inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated ||
            !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 262_144)
            return Unknown("TCP setting collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) || !complete ||
                !root.TryGetProperty("settings", out var settings) || settings.ValueKind != JsonValueKind.Array ||
                settings.GetArrayLength() is 0 or > MaximumSettings)
                return Unknown("TCP setting evidence was malformed, empty, incomplete or exceeded its bound.", evidence);

            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int localEffectiveDifferences = 0;
            int explicitCongestionProviders = 0;
            int ecnEnabled = 0;
            int timestampsEnabled = 0;
            var summaries = new List<string>();
            foreach (var item in settings.EnumerateArray())
            {
                if (!Unique(item, 11) || !Text(item, "settingName", 128, out string name) || !names.Add(name) ||
                    !Integer(item, "minRto", uint.MaxValue, out long minRto) ||
                    !Integer(item, "initialCongestionWindow", uint.MaxValue, out long initialWindow) ||
                    !Byte(item, "congestionProvider", out int congestionProvider) ||
                    !Byte(item, "autoTuningLocal", out int autoTuningLocal) ||
                    !Byte(item, "autoTuningEffective", out int autoTuningEffective) ||
                    !Byte(item, "ecnCapability", out int ecn) || !Byte(item, "timestamps", out int timestamps) ||
                    !Integer(item, "initialRto", uint.MaxValue, out long initialRto) ||
                    !Byte(item, "scalingHeuristics", out int scalingHeuristics) ||
                    !Byte(item, "maxSynRetransmissions", out int maxSynRetransmissions))
                    return Unknown("TCP setting evidence contained an invalid or duplicate template.", evidence);
                if (autoTuningLocal != autoTuningEffective) localEffectiveDifferences++;
                if (congestionProvider != 0) explicitCongestionProviders++;
                if (ecn != 0) ecnEnabled++;
                if (timestamps != 0) timestampsEnabled++;
                summaries.Add($"{name}: local/effective auto-tuning {autoTuningLocal}/{autoTuningEffective}, congestion provider {congestionProvider}, ECN {ecn}, timestamps {timestamps}, initial RTO {initialRto} ms, minimum RTO {minRto} ms, initial window {initialWindow}, scaling heuristics {scalingHeuristics}, maximum SYN retransmissions {maxSynRetransmissions}.");
            }

            string detail = $"Reviewed {summaries.Count} TCP templates. {localEffectiveDifferences} have different local and effective auto-tuning values, {explicitCongestionProviders} report a nonzero congestion-provider value, {ecnEnabled} report nonzero ECN capability and {timestampsEnabled} report nonzero timestamp configuration. Numeric enum values are retained exactly so newer Windows values are not mislabeled.";
            detail += "\n" + string.Join("\n", summaries.Take(12));
            if (summaries.Count > 12)
                detail += $"\n{summaries.Count - 12} additional templates remain in raw evidence.";
            return new("TCP setting templates", CheckState.Information, detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("TCP setting evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("TCP setting templates", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected &&
        value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Byte(JsonElement owner, string name, out int value) => Integer(owner, name, byte.MaxValue, out value);
    private static bool Integer(JsonElement owner, string name, long maximum, out long value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number &&
            item.TryGetInt64(out value) && value >= 0 && value <= maximum;
    }
    private static bool Integer(JsonElement owner, string name, int maximum, out int value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number &&
            item.TryGetInt32(out value) && value >= 0 && value <= maximum;
    }
    private static bool Text(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String ||
            item.GetString() is not { } text || text.Length is < 1 || text.Length > maximum ||
            text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
}
