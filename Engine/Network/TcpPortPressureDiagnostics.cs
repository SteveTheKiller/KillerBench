using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class TcpPortPressureDiagnostics
{
    private const int MaximumConnections = 200_000;
    private const string Scope = " Counts are a point-in-time view of distinct local dynamic ports. They do not identify a process, remote endpoint, leak or exhausted destination tuple. No connection, port range, adapter, firewall or service setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "function range($family){$text=(& netsh.exe interface $family show dynamicportrange tcp 2>&1|Out-String);if($LASTEXITCODE-ne 0){throw $text};" +
        "$s=[regex]::Match($text,'(?im)^\\s*Start Port\\s*:\\s*(\\d+)\\s*$');$n=[regex]::Match($text,'(?im)^\\s*Number of Ports\\s*:\\s*(\\d+)\\s*$');if(!$s.Success-or !$n.Success){throw 'Unrecognized dynamic port range'};@([int]$s.Groups[1].Value,[int]$n.Groups[1].Value)};" +
        "$r4=range 'ipv4';$r6=range 'ipv6';$c=@(Get-NetTCPConnection -ErrorAction Stop|Select-Object -First 200001);$complete=$c.Count-le 200000;$c=@($c|Select-Object -First 200000);" +
        "$p4=@($c|Where-Object{$_.LocalAddress-notmatch ':'-and $_.LocalPort-ge $r4[0]-and $_.LocalPort-lt ($r4[0]+$r4[1])}|Select-Object -ExpandProperty LocalPort -Unique);" +
        "$p6=@($c|Where-Object{$_.LocalAddress-match ':'-and $_.LocalPort-ge $r6[0]-and $_.LocalPort-lt ($r6[0]+$r6[1])}|Select-Object -ExpandProperty LocalPort -Unique);" +
        "$t4=@($c|Where-Object{$_.State-eq 'TimeWait'-and $_.LocalAddress-notmatch ':'-and $_.LocalPort-ge $r4[0]-and $_.LocalPort-lt ($r4[0]+$r4[1])}|Select-Object -ExpandProperty LocalPort -Unique);" +
        "$t6=@($c|Where-Object{$_.State-eq 'TimeWait'-and $_.LocalAddress-match ':'-and $_.LocalPort-ge $r6[0]-and $_.LocalPort-lt ($r6[0]+$r6[1])}|Select-Object -ExpandProperty LocalPort -Unique);" +
        "[pscustomobject]@{complete=$complete;ipv4Start=$r4[0];ipv4Count=$r4[1];ipv4InUse=$p4.Count;ipv4TimeWait=$t4.Count;ipv6Start=$r6[0];ipv6Count=$r6[1];ipv6InUse=$p6.Count;ipv6TimeWait=$t6.Count}|ConvertTo-Json -Compress";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("TCP port-pressure inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var evidence = await (execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("TCP port-pressure provider returned no task.")).WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("TCP port-pressure provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        { return [Unknown("TCP port-pressure inspection exceeded its overall deadline.", new(-1, "", "TCP port-pressure deadline expired.", false) { TimedOut = true })]; }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { return [Unknown("TCP port-pressure inspection was unavailable.", new(-1, "", error.Message, false))]; }
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 65_536)
            return [Unknown("TCP port-pressure collection failed or returned incomplete evidence.", evidence)];
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 3 });
            var root = document.RootElement;
            if (!Unique(root, 9) || !Boolean(root, "complete", out bool complete) || !complete ||
                !Range(root, "ipv4", out var ipv4) || !Range(root, "ipv6", out var ipv6))
                return [Unknown("TCP port-pressure evidence was malformed, incomplete or exceeded its connection bound.", evidence)];
            return [Assess("IPv4", ipv4, evidence), Assess("IPv6", ipv6, evidence)];
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        { return [Unknown("TCP port-pressure evidence could not be interpreted.", evidence)]; }
    }

    private static WorkflowCheck Assess(string family, (int Start, int Count, int Used, int TimeWait) value, ToolResult evidence)
    {
        double percent = value.Used * 100d / value.Count;
        bool pressure = percent >= 80d;
        string detail = $"Configured range: {value.Start} through {value.Start + value.Count - 1} ({value.Count} ports). Observed {value.Used} distinct local dynamic ports in use ({percent:F1}%), including {value.TimeWait} in TIME_WAIT.";
        detail += pressure
            ? " Usage is at or above the 80% review threshold. Capture repeated snapshots and correlate the affected workload before changing the range or TCP settings."
            : " Usage is below the 80% review threshold in this snapshot.";
        return new(family + " dynamic TCP ports", pressure ? CheckState.Attention : CheckState.Information, detail + Scope, Evidence: evidence);
    }

    private static bool Range(JsonElement root, string prefix, out (int Start, int Count, int Used, int TimeWait) value)
    {
        value = default;
        if (!Integer(root, prefix + "Start", 1, 65_535, out int start) || !Integer(root, prefix + "Count", 1, 65_535, out int count) ||
            start + count > 65_536 || !Integer(root, prefix + "InUse", 0, count, out int used) ||
            !Integer(root, prefix + "TimeWait", 0, used, out int timeWait)) return false;
        value = (start, count, used, timeWait);
        return true;
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new("Dynamic TCP port pressure", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value) { value = false; if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false; value = item.GetBoolean(); return true; }
    private static bool Integer(JsonElement owner, string name, int minimum, int maximum, out int value) { value = 0; return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out value) && value >= minimum && value <= maximum; }
}
