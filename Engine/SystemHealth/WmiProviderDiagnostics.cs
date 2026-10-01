using System.Text.Json;

namespace KillerBench.Engine.SystemHealth;

public static class WmiProviderDiagnostics
{
    private const string Script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
        "$rows=@(Get-CimInstance -Namespace root/cimv2 -ClassName Win32_OperatingSystem -Property Version,BuildNumber -OperationTimeoutSec 15); " +
        "[pscustomobject]@{count=$rows.Count;version=if($rows.Count -eq 1){[string]$rows[0].Version}else{$null};build=if($rows.Count -eq 1){[string]$rows[0].BuildNumber}else{$null}} | ConvertTo-Json -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("WMI provider checks require Windows.");
        var step = new RepairStep(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true);
        return Interpret(await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(25), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(ToolResult result)
    {
        WorkflowCheck Unknown() => new("WMI operating-system provider", CheckState.Unknown,
            "The root/cimv2 Win32_OperatingSystem query did not return one complete recognized result. Review access, namespace and provider errors in the evidence. This does not prove repository corruption; no salvage or provider registration selected.", Evidence: result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 3 ||
                root.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != 3 ||
                !root.TryGetProperty("count", out var count) || !count.TryGetInt32(out int number) || number != 1 ||
                !root.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.String ||
                !root.TryGetProperty("build", out var build) || build.ValueKind != JsonValueKind.String) return Unknown();
            string v = version.GetString()!, b = build.GetString()!;
            if (v.Length is < 3 or > 64 || !Version.TryParse(v, out _) || b.Length is < 1 or > 10 || !b.All(char.IsAsciiDigit)) return Unknown();
            return new("WMI operating-system provider", CheckState.Clear,
                $"root/cimv2 Win32_OperatingSystem returned one operating-system instance (version {v}, build {b}). This verifies this local query only; other namespaces, providers and remote management remain untested.", Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException) { return Unknown(); }
    }
}
