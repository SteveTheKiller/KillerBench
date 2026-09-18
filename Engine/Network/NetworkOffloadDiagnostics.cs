using System.Text.Json;

namespace KillerBench.Engine;

public static class NetworkOffloadDiagnostics
{
    private const string Scope = " Global offload configuration is context, not proof of a network fault or performance problem. Hardware, drivers, virtualization, specialized workloads and managed policy can make different values appropriate. No offload, adapter, TCP template, route, firewall, service or policy setting changed.";
    private const string Source = "https://learn.microsoft.com/powershell/module/nettcpip/get-netoffloadglobalsetting";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-NetOffloadGlobalSetting -ErrorAction Stop);" +
        "$rows=@($all|Select-Object -First 2|ForEach-Object{[pscustomobject]@{" +
        "receiveSideScaling=[uint32]$_.ReceiveSideScaling;receiveSegmentCoalescing=[uint32]$_.ReceiveSegmentCoalescing;" +
        "chimney=[uint32]$_.Chimney;taskOffload=[uint32]$_.TaskOffload;networkDirect=[uint32]$_.NetworkDirect;" +
        "packetCoalescingFilter=[uint32]$_.PacketCoalescingFilter;networkDirectAcrossIpSubnets=[uint32]$_.NetworkDirectAcrossIPSubnets}});" +
        "[pscustomobject]@{complete=($all.Count-eq 1);settings=$rows}|ConvertTo-Json -Depth 3 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Network offload inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        try
        {
            execute ??= (step, cancellationToken) =>
                BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: cancellationToken);
            var task = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Network offload provider returned no task.");
            var evidence = await task.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Network offload provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Network offload inspection exceeded its overall deadline.",
                new(-1, "", "Network offload deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Network offload inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated ||
            !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 65_536)
            return Unknown("Network offload collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) || !complete ||
                !root.TryGetProperty("settings", out var settings) || settings.ValueKind != JsonValueKind.Array || settings.GetArrayLength() != 1)
                return Unknown("Network offload evidence was malformed, missing, ambiguous or incomplete.", evidence);
            var item = settings[0];
            if (!Unique(item, 7) ||
                !Byte(item, "receiveSideScaling", out int rss) ||
                !Byte(item, "receiveSegmentCoalescing", out int rsc) ||
                !Byte(item, "chimney", out int chimney) ||
                !Byte(item, "taskOffload", out int taskOffload) ||
                !Byte(item, "networkDirect", out int networkDirect) ||
                !Byte(item, "packetCoalescingFilter", out int packetCoalescing) ||
                !Byte(item, "networkDirectAcrossIpSubnets", out int networkDirectSubnets))
                return Unknown("Network offload evidence contained an unsupported value.", evidence);

            string detail = $"Global numeric values: RSS {rss}; RSC {rsc}; Chimney {chimney}; task offload {taskOffload}; NetworkDirect {networkDirect}; packet coalescing {packetCoalescing}; NetworkDirect across IP subnets {networkDirectSubnets}. Numeric values are retained exactly so future Windows members are not mislabeled. Microsoft Get-NetOffloadGlobalSetting reference: {Source}.";
            return new("Network offload settings", CheckState.Information, detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        {
            return Unknown("Network offload evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Network offload settings", CheckState.Unknown, detail + Scope, Evidence: evidence);
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
    private static bool Byte(JsonElement owner, string name, out int value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number &&
            item.TryGetInt32(out value) && value is >= byte.MinValue and <= byte.MaxValue;
    }
}
