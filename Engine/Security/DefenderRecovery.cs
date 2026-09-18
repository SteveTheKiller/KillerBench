using System.Text.Json;

namespace KillerBench.Engine;

public interface IDefenderRecoveryHost
{
    Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token);
}

public static class DefenderRecovery
{
    private const string Query = "$s=Get-MpComputerStatus -ErrorAction Stop; ";
    private const string Snapshot = "[pscustomobject]@{mode=$s.AMRunningMode; service=$s.AMServiceEnabled; antivirus=$s.AntivirusEnabled; realtime=$s.RealTimeProtectionEnabled; stale=$s.DefenderSignaturesOutOfDate; age=$s.AntivirusSignatureAge; version=$s.AntivirusSignatureVersion} | ConvertTo-Json -Compress";
    private const string Refresh = "if($s.AMRunningMode -ne 'Normal' -or $s.AMServiceEnabled -ne $true -or $s.AntivirusEnabled -ne $true -or $s.RealTimeProtectionEnabled -ne $true -or $null -eq $s.DefenderSignaturesOutOfDate) { throw 'Defender is not eligible for signature recovery' }; $updated=$false; if($s.DefenderSignaturesOutOfDate -eq $true) { Update-MpSignature -ErrorAction Stop; $updated=$true }; " + Query;
    private const string RefreshSnapshot = "[pscustomobject]@{commandDispatched=$updated; mode=$s.AMRunningMode; service=$s.AMServiceEnabled; antivirus=$s.AntivirusEnabled; realtime=$s.RealTimeProtectionEnabled; stale=$s.DefenderSignaturesOutOfDate; age=$s.AntivirusSignatureAge; version=$s.AntivirusSignatureVersion} | ConvertTo-Json -Compress";

    public static RepairStep Step(bool verification) => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " + Query +
            (verification ? Snapshot : Refresh + RefreshSnapshot)], verification);

    public static bool IsEligible(WorkflowCheck? check)
    {
        if (check is not { State: CheckState.Repairable, Repair: RepairKind.DefenderSignatures, Evidence: not null }) return false;
        var interpreted = Interpret(check.Evidence);
        return interpreted.State == CheckState.Repairable && interpreted.Repair == RepairKind.DefenderSignatures;
    }

    public static void RequireEligible(WorkflowCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        if (!IsEligible(check))
            throw new InvalidOperationException("Fresh exact stale-signature evidence from active Microsoft Defender is required before refresh. " + check.Detail);
    }

    public static Task<WorkflowCheck> CheckAsync(IProgress<string>? progress = null, CancellationToken token = default) =>
        CheckAsync(new WindowsDefenderRecoveryHost(), progress, token);

    public static async Task<WorkflowCheck> CheckAsync(IDefenderRecoveryHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        return Interpret(await RunBounded(Step(true), host, progress, TimeSpan.FromSeconds(45), token).ConfigureAwait(false));
    }

    public static Task<ToolResult> ExecuteAsync(RepairStep step, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        return Matches(step, Step(false)) || Matches(step, Step(true))
            ? ExecuteAsync(step, new WindowsDefenderRecoveryHost(), progress, token)
            : BoundedTool.RunAsync(step, step.Verification ? TimeSpan.FromSeconds(45) : TimeSpan.FromMinutes(5), progress, token);
    }

    public static async Task<ToolResult> ExecuteAsync(RepairStep step, IDefenderRecoveryHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(host);
        bool refresh = Matches(step, Step(false));
        bool verify = Matches(step, Step(true));
        if (!refresh && !verify)
            throw new ArgumentException("Command does not match the Defender signature refresh plan.", nameof(step));
        if (verify) return await RunBounded(step, host, progress, TimeSpan.FromSeconds(45), token).ConfigureAwait(false);

        progress?.Report("Rechecking Defender protection and signature freshness before refresh...");
        var current = await CheckAsync(host, progress, token).ConfigureAwait(false);
        if (current is { State: CheckState.Clear, Repair: null, Evidence: not null } && Interpret(current.Evidence).State == CheckState.Clear)
        {
            progress?.Report("Defender signatures are now current. No signature refresh was run.");
            throw new RepairStepSkippedException("Defender signatures are now current. No signature refresh was run.", current.Evidence);
        }
        if (!IsEligible(current))
            throw new RepairDispatchDeferredException(
                "Defender signature refresh was deferred because fresh exact stale-signature evidence from active protection was not available. " + current.Detail);

        progress?.Report("Fresh stale-signature evidence confirmed. Starting bounded Defender signature refresh...");
        var result = await RunBounded(step, host, progress, TimeSpan.FromMinutes(5), token).ConfigureAwait(false);
        if (NoRefreshDispatched(result))
            throw new RepairStepSkippedException(
                "Defender signatures became current at the final dispatch boundary. No signature refresh was run.", result);
        return result;
    }

    public static WorkflowCheck Interpret(ToolResult result)
    {
        WorkflowCheck Unknown() => new("Defender signatures", CheckState.Unknown, "Defender status is unavailable or incomplete. No automatic action selected.", Evidence: result);
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            string? mode = root.GetProperty("mode").GetString();
            bool service = root.GetProperty("service").GetBoolean(), antivirus = root.GetProperty("antivirus").GetBoolean(), realtime = root.GetProperty("realtime").GetBoolean();
            bool stale = root.GetProperty("stale").GetBoolean();
            uint age = root.GetProperty("age").GetUInt32();
            string? version = root.GetProperty("version").GetString();
            if (string.IsNullOrWhiteSpace(mode) || string.IsNullOrWhiteSpace(version) || version.Length > 64 || age == uint.MaxValue) return Unknown();
            if (mode != "Normal" || !service || !antivirus || !realtime)
                return new("Defender signatures", CheckState.Attention, $"Defender mode: {mode}. Service: {service}; antivirus: {antivirus}; real-time protection: {realtime}. Review the intended security product and policy. Protection settings are preserved.", Evidence: result);
            return new("Defender signatures", stale ? CheckState.Repairable : CheckState.Clear,
                $"Signature version {version}, reported age {age} days. " + (stale
                    ? "Defender reports signatures out of date. Refresh uses configured signature sources."
                    : "Defender does not report signatures out of date. This is not a malware scan or a complete security assessment."),
                stale ? RepairKind.DefenderSignatures : null, result);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException) { return Unknown(); }
    }

    private static bool NoRefreshDispatched(ToolResult result)
    {
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return false;
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            return json.RootElement.TryGetProperty("commandDispatched", out var dispatched) && dispatched.ValueKind == JsonValueKind.False;
        }
        catch (JsonException) { return false; }
    }

    private static bool Matches(RepairStep actual, RepairStep expected) =>
        actual.Tool == expected.Tool && actual.Verification == expected.Verification && actual.Arguments.SequenceEqual(expected.Arguments);

    private static async Task<ToolResult> RunBounded(RepairStep step, IDefenderRecoveryHost host,
        IProgress<string>? progress, TimeSpan limit, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(limit);
        try
        {
            var pending = host.RunAsync(step, progress, timeout.Token)
                ?? throw new InvalidOperationException("Defender provider returned no command task.");
            return await pending.WaitAsync(limit + TimeSpan.FromSeconds(1), token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Defender provider returned no command evidence.");
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return new(-1, "", "Defender operation timed out. Signature state is unverified; do not immediately repeat the refresh.", false) { TimedOut = true };
        }
        catch (TimeoutException)
        {
            timeout.Cancel();
            return new(-1, "", "Defender operation timed out. Signature state is unverified; do not immediately repeat the refresh.", false) { TimedOut = true };
        }
    }
}

public sealed class WindowsDefenderRecoveryHost : IDefenderRecoveryHost
{
    public Task<ToolResult> RunAsync(RepairStep step, IProgress<string>? progress, CancellationToken token) =>
        BoundedTool.RunAsync(step, step.Verification ? TimeSpan.FromSeconds(45) : TimeSpan.FromMinutes(5), progress, token);
}
