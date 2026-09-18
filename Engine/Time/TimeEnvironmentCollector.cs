using System.Text.Json;

namespace KillerBench.Engine;

public static class TimeEnvironmentCollector
{
    public static RepairStep QueryStep() => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
        "$ErrorActionPreference='Stop'; $cs=Get-CimInstance Win32_ComputerSystem -ErrorAction Stop; " +
        "$base=[Microsoft.Win32.RegistryKey]::OpenBaseKey('LocalMachine','Registry64'); " +
        "try { $policy=$base.OpenSubKey('SOFTWARE\\Policies\\Microsoft\\W32Time'); " +
        "try { $managed=$null; if($null -ne $policy){$managed=$true}; " +
        "$identity=[Security.Principal.WindowsIdentity]::GetCurrent(); try { " +
        "$admin=([Security.Principal.WindowsPrincipal]::new($identity)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator); " +
        "$local=$base.OpenSubKey('SYSTEM\\CurrentControlSet\\Services\\W32Time\\Parameters'); try { " +
        "$mode=$null; $peers=$null; if($local){$mode=$local.GetValue('Type'); $peers=$local.GetValue('NtpServer')}; " +
        "[pscustomobject]@{joined=$cs.PartOfDomain; role=$cs.DomainRole; policy=$managed; elevated=$admin; localMode=$mode; localPeers=$peers} | ConvertTo-Json -Compress " +
        "} finally {if($local){$local.Dispose()}} " +
        "} finally {$identity.Dispose()} } finally {if($policy){$policy.Dispose()}} } finally {$base.Dispose()}"], true);

    public static async Task<TimeEnvironment> ReadAsync(CancellationToken token = default)
    {
        var environment = Interpret(await BoundedTool.RunAsync(QueryStep(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false));
        token.ThrowIfCancellationRequested();
        if (environment.DomainJoined is null || environment.PolicyManaged == true) return environment;
        var configuration = await BoundedTool.RunAsync(new(TimeRecovery.Tool, ["/query", "/configuration"], true), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return WithConfigurationOrigin(environment, configuration);
    }

    public static TimeEnvironment WithConfigurationOrigin(TimeEnvironment environment, ToolResult configuration)
    {
        if (environment.PolicyManaged == true) return environment;
        environment = environment with { ConfigurationEvidence = configuration };
        var unknown = environment with { PolicyManaged = null };
        if (environment.PolicyManaged is null && TimeRecovery.IsServiceNotStarted(configuration))
            return environment with { PolicyManaged = false };
        if (environment.DomainJoined is null || configuration.ExitCode != 0 || configuration.Truncated || configuration.TimedOut || !string.IsNullOrWhiteSpace(configuration.Error)) return unknown;
        if (configuration.Output.Contains("(Policy)", StringComparison.OrdinalIgnoreCase)) return environment with { PolicyManaged = true };
        string? LocalValue(string field)
        {
            var matches = System.Text.RegularExpressions.Regex.Matches(configuration.Output,
                @"(?m)^[ \t]*" + field + @":[ \t]*([^\r\n]*?)[ \t]+\(Local\)[ \t]*\r?$",
                System.Text.RegularExpressions.RegexOptions.CultureInvariant);
            return matches.Count == 1 ? matches[0].Groups[1].Value.Trim() : null;
        }
        string? mode = LocalValue("Type"), peers = LocalValue("NtpServer");
        if (mode is null || peers is null || !string.Equals(mode, environment.LocalMode, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(peers, environment.LocalPeers, StringComparison.OrdinalIgnoreCase)) return unknown;
        return environment with { PolicyManaged = false };
    }

    public static TimeEnvironment Interpret(ToolResult result)
    {
        var unknown = new TimeEnvironment(null, null, null, false);
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut) return unknown;
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var value = json.RootElement;
            if (value.ValueKind != JsonValueKind.Object || value.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != value.EnumerateObject().Count()) return unknown;
            bool joined = value.GetProperty("joined").GetBoolean();
            int role = value.GetProperty("role").GetInt32();
            if (role is < 0 or > 5 || joined != (role is 1 or 3 or 4 or 5)) return unknown;
            var policy = value.GetProperty("policy");
            bool? managed = policy.ValueKind == JsonValueKind.Null ? null : policy.GetBoolean();
            string? Text(string name, int limit)
            {
                if (!value.TryGetProperty(name, out var item) || item.ValueKind == JsonValueKind.Null) return null;
                string? text = item.GetString();
                return text is not null && text.Length <= limit && !text.Any(char.IsControl) ? text : null;
            }
            return new(joined, role >= 4, managed, value.GetProperty("elevated").GetBoolean())
            { LocalMode = Text("localMode", 32), LocalPeers = Text("localPeers", 4096) };
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { return unknown; }
    }
}
