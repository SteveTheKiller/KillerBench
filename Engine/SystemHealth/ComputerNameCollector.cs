using System.Text.Json;

namespace KillerBench.Engine.SystemHealth;

public static class ComputerNameCollector
{
    public static async Task<ComputerNameContext> ReadAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Computer name collection requires Windows.");
        var step = new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
            "$ErrorActionPreference='Stop'; $cs=Get-CimInstance Win32_ComputerSystem -OperationTimeoutSec 15; $pending=$null; $configured=$null; " +
            "try { $configured=(Get-ItemProperty -LiteralPath 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\ComputerName\\ComputerName' -Name ComputerName -ErrorAction Stop).ComputerName; " +
            "if(-not [string]::IsNullOrWhiteSpace($configured)){$pending=($configured -ne $cs.Name)} } catch {} " +
            "[pscustomobject]@{Name=$cs.Name; ConfiguredName=$configured; DomainRole=$cs.DomainRole; PartOfDomain=$cs.PartOfDomain; RenamePending=$pending} | ConvertTo-Json -Compress"], true);
        var result = await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static ComputerNameContext Interpret(ToolResult result)
    {
        var unknown = new ComputerNameContext(null, null, null);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated) return unknown;
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().GroupBy(p => p.Name, StringComparer.Ordinal).Any(g => g.Count() != 1)) return unknown;
            string? name = root.GetProperty("Name").GetString();
            int role = root.GetProperty("DomainRole").GetInt32();
            bool joined = root.GetProperty("PartOfDomain").GetBoolean();
            if (!DriverReference.ValidText(name, 63) || name != name!.Trim() || role is < 0 or > 5 || joined != (role is 1 or 3 or 4 or 5)) return unknown;
            string? configured = root.TryGetProperty("ConfiguredName", out var configuredValue) && configuredValue.ValueKind == JsonValueKind.String
                ? configuredValue.GetString() : null;
            if (configured is not null && (!DriverReference.ValidText(configured, 63) || configured != configured.Trim())) return unknown;
            bool? pending = root.TryGetProperty("RenamePending", out var rename) ? rename.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => null
            } : null;
            return new(name, role, pending) { ConfiguredName = configured };
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        { return unknown; }
    }
}
