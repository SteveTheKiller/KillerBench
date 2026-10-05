using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class DhcpLeaseDiagnostics
{
    private const int MaximumAdapters = 256;
    private const string Scope = " Lease timestamps and DHCP server presence are point-in-time local evidence. A valid lease does not prove DNS, gateway or Internet connectivity. No lease, address, adapter, route, resolver, service, policy or network setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$physical=@{};foreach($item in @(Get-CimInstance Win32_NetworkAdapter -OperationTimeoutSec 15|Where-Object{$_.PhysicalAdapter-eq $true-and $_.NetConnectionStatus-eq 2})){[int]$index=0;if(-not [int]::TryParse([string]$item.Index,[ref]$index)-or $index-lt 0-or $physical.ContainsKey($index)){throw 'Ambiguous physical adapter identity'};$physical[$index]=$true};" +
        "function Has-UsableIPv4($addresses){foreach($text in @($addresses)){$ip=$null;if($null-ne $text-and-not [Net.IPAddress]::TryParse([string]$text,[ref]$ip)){throw 'Malformed DHCP address evidence'};if($null-ne $ip-and $ip.AddressFamily-eq [Net.Sockets.AddressFamily]::InterNetwork){$b=$ip.GetAddressBytes();if($b[0]-gt 0-and $b[0]-lt 224-and $b[0]-ne 127-and-not($b[0]-eq 169-and $b[1]-eq 254)){return $true}}};return $false};" +
        "function To-Utc($value){if($null-eq $value){return $null};try{return ([DateTimeOffset]([datetime]$value)).ToUniversalTime().ToString('o')}catch{throw 'Malformed DHCP lease timestamp'}};" +
        "$all=@(Get-CimInstance Win32_NetworkAdapterConfiguration -OperationTimeoutSec 15|Where-Object{$_.DHCPEnabled-eq $true-and $_.IPEnabled-eq $true-and $physical.ContainsKey([int]$_.Index)}|Sort-Object Index|Select-Object -First 257);" +
        "$seen=@{};$settings=@{};$rows=@($all|Select-Object -First 256|ForEach-Object{[int]$index=$_.Index;$setting=[guid]::Empty;if($seen.ContainsKey($index)-or-not [guid]::TryParse([string]$_.SettingID,[ref]$setting)-or $setting-eq [guid]::Empty-or $settings.ContainsKey($setting)){throw 'Ambiguous DHCP adapter identity'};$seen[$index]=$true;$settings[$setting]=$true;$server=$null;if(-not [string]::IsNullOrWhiteSpace([string]$_.DHCPServer)){$server=$null;if(-not [Net.IPAddress]::TryParse([string]$_.DHCPServer,[ref]$server)){throw 'Malformed DHCP server evidence'}};[pscustomobject]@{interfaceIndex=$index;settingId=$setting.ToString();hasUsableIpv4=(Has-UsableIPv4 $_.IPAddress);hasServer=($null-ne $server);leaseObtainedUtc=(To-Utc $_.DHCPLeaseObtained);leaseExpiresUtc=(To-Utc $_.DHCPLeaseExpires)}});" +
        "[pscustomobject]@{complete=($all.Count-le 256);capturedUtc=[DateTimeOffset]::UtcNow.ToString('o');adapters=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("DHCP lease inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(35), token: cancellationToken);
            var task = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("DHCP lease provider returned no task.");
            var evidence = await task.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("DHCP lease provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("DHCP lease inspection exceeded its overall deadline.",
                new(-1, "", "DHCP lease deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("DHCP lease inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("DHCP lease collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 3) || !Boolean(root, "complete", out bool complete) || !complete ||
                !Utc(root, "capturedUtc", required: true, out var captured) ||
                !root.TryGetProperty("adapters", out var adapters) || adapters.ValueKind != JsonValueKind.Array ||
                adapters.GetArrayLength() > MaximumAdapters)
                return Unknown("DHCP lease evidence was malformed, incomplete or exceeded its bound.", evidence);

            var rows = new List<LeaseRow>();
            var indexes = new HashSet<int>();
            var settings = new HashSet<Guid>();
            foreach (var item in adapters.EnumerateArray())
            {
                if (!Unique(item, 6) || !Integer(item, "interfaceIndex", out int index) ||
                    !GuidValue(item, "settingId", out var setting) || !indexes.Add(index) || !settings.Add(setting) ||
                    !Boolean(item, "hasUsableIpv4", out bool usable) || !Boolean(item, "hasServer", out bool server) ||
                    !Utc(item, "leaseObtainedUtc", required: false, out var obtained) ||
                    !Utc(item, "leaseExpiresUtc", required: false, out var expires) ||
                    obtained.HasValue != expires.HasValue)
                    return Unknown("DHCP lease evidence contained an invalid, duplicate or incomplete adapter.", evidence);
                rows.Add(new(index, usable, server, obtained, expires));
            }

            if (rows.Count == 0)
                return new("DHCP lease state", CheckState.Information,
                    "No connected physical IPv4 DHCP adapters were reported." + Scope, Evidence: evidence);

            int noAddress = rows.Count(row => !row.Usable);
            int noServer = rows.Count(row => !row.Server);
            int noTimestamps = rows.Count(row => !row.Obtained.HasValue);
            int expired = rows.Count(row => row.Expires <= captured);
            int inconsistent = rows.Count(row => row.Obtained.HasValue && row.Expires <= row.Obtained);
            string detail = $"Reviewed {rows.Count} connected physical IPv4 DHCP adapters: {noAddress} without a usable address, {expired} with an expired lease, {noServer} without reported server evidence and {noTimestamps} without complete lease timestamps.";
            CheckState state = inconsistent > 0 || noTimestamps > 0 ? CheckState.Unknown :
                noAddress > 0 || expired > 0 || noServer > 0 ? CheckState.Attention : CheckState.Information;
            if (inconsistent > 0)
                detail += $" {inconsistent} adapters reported an expiry that was not later than lease acquisition.";
            return new("DHCP lease state", state, detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException or OverflowException)
        {
            return Unknown("DHCP lease evidence could not be interpreted.", evidence);
        }
    }

    private sealed record LeaseRow(int Index, bool Usable, bool Server, DateTimeOffset? Obtained, DateTimeOffset? Expires);
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("DHCP lease state", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Integer(JsonElement owner, string name, out int value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number &&
            item.TryGetInt32(out value) && value >= 0;
    }
    private static bool GuidValue(JsonElement owner, string name, out Guid value)
    {
        value = Guid.Empty;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String &&
            Guid.TryParse(item.GetString(), out value) && value != Guid.Empty;
    }
    private static bool Utc(JsonElement owner, string name, bool required, out DateTimeOffset? value)
    {
        value = null;
        if (!owner.TryGetProperty(name, out var item)) return false;
        if (item.ValueKind == JsonValueKind.Null) return !required;
        if (item.ValueKind != JsonValueKind.String || !DateTimeOffset.TryParse(item.GetString(), CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var parsed) || parsed.Offset != TimeSpan.Zero) return false;
        value = parsed;
        return true;
    }
}
