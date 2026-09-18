using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace KillerBench.Engine;

[Flags]
public enum DhcpRenewalReason { None = 0, MissingAddress = 1, ExpiredLease = 2 }

public sealed record DhcpAdapterSnapshot(string[] Addresses, bool? HasServer = null,
    DateTimeOffset? LeaseObtainedUtc = null, DateTimeOffset? LeaseExpiresUtc = null);

public sealed record DhcpSnapshot(int[] Targets, IReadOnlyDictionary<int, string[]> Adapters)
{
    public IReadOnlyDictionary<int, Guid> Identities { get; init; } = new Dictionary<int, Guid>();
    public IReadOnlyDictionary<int, DhcpAdapterSnapshot> Details { get; init; } = new Dictionary<int, DhcpAdapterSnapshot>();
    public IReadOnlyDictionary<int, DhcpRenewalReason> Reasons { get; init; } = new Dictionary<int, DhcpRenewalReason>();
    public DateTimeOffset? CapturedUtc { get; init; }
    public bool Detailed { get; init; }
}

public static class DhcpRecovery
{
    private const string Prefix = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
        "function Get-Eligible { $ids=@(Get-CimInstance Win32_NetworkAdapter -OperationTimeoutSec 15 | Where-Object { $_.PhysicalAdapter -eq $true -and $_.NetConnectionStatus -eq 2 } | Select-Object -ExpandProperty Index); " +
        "$seen=@{}; foreach($id in $ids) { [int]$index=0; if(-not [int]::TryParse([string]$id,[ref]$index) -or $index -lt 0 -or $seen.ContainsKey($index)) { throw 'Ambiguous physical adapter identity' }; $seen[$index]=$true }; " +
        "$eligible=@(Get-CimInstance Win32_NetworkAdapterConfiguration -OperationTimeoutSec 15 | Where-Object { $_.DHCPEnabled -eq $true -and $_.IPEnabled -eq $true -and $ids -contains $_.Index }); " +
        "$seen=@{}; $settings=@{}; foreach($adapter in $eligible) { [int]$index=0; if(-not [int]::TryParse([string]$adapter.Index,[ref]$index) -or $index -lt 0 -or $seen.ContainsKey($index)) { throw 'Ambiguous DHCP adapter identity' }; $seen[$index]=$true; " +
        "$setting=[guid]::Empty; if(-not [guid]::TryParse([string]$adapter.SettingID,[ref]$setting) -or $setting -eq [guid]::Empty -or $settings.ContainsKey($setting)) { throw 'Unavailable or duplicate adapter SettingID' }; $settings[$setting]=$true; " +
        "foreach($address in @($adapter.IPAddress)) { $ip=$null; if($null -ne $address -and -not [Net.IPAddress]::TryParse([string]$address,[ref]$ip)) { throw 'Malformed DHCP address evidence' } } }; $eligible }; " +
        "function Has-Address($addresses) { foreach($text in @($addresses)) { $ip=$null; if([Net.IPAddress]::TryParse($text,[ref]$ip) -and $ip.AddressFamily -eq [Net.Sockets.AddressFamily]::InterNetwork) { $b=$ip.GetAddressBytes(); if($b[0] -gt 0 -and $b[0] -lt 224 -and $b[0] -ne 127 -and -not ($b[0] -eq 169 -and $b[1] -eq 254)) { return $true } } }; return $false }; " +
        "function Read-Utc($value) { if($null -eq $value -or [string]::IsNullOrWhiteSpace([string]$value)) { return $null }; try { return ([DateTimeOffset]([datetime]$value)).ToUniversalTime() } catch { throw 'Malformed DHCP lease timestamp' } }; " +
        "function Read-State($adapter) { $server=$null; if(-not [string]::IsNullOrWhiteSpace([string]$adapter.DHCPServer)) { if(-not [Net.IPAddress]::TryParse([string]$adapter.DHCPServer,[ref]$server)) { throw 'Malformed DHCP server evidence' } }; " +
        "$obtained=Read-Utc $adapter.DHCPLeaseObtained; $expires=Read-Utc $adapter.DHCPLeaseExpires; if(($null-eq $obtained)-ne($null-eq $expires)) { throw 'Incomplete DHCP lease timestamp evidence' }; if($null-ne $obtained -and $expires-le $obtained) { throw 'Inconsistent DHCP lease timestamp evidence' }; " +
        "[pscustomobject]@{hasAddress=(Has-Address $adapter.IPAddress);hasServer=($null-ne $server);obtained=$obtained;expires=$expires} }; ";

    private const string SelectTargets = "$captured=[DateTimeOffset]::UtcNow; $targets=@(); $targetSettings=@{}; $reasons=@{}; $eligible=@(Get-Eligible); " +
        "foreach($adapter in $eligible) { $state=Read-State $adapter; $reason=0; if(-not $state.hasAddress) { $reason=$reason-bor 1 }; if($state.hasServer -and $null-ne $state.expires -and $state.expires-le $captured) { $reason=$reason-bor 2 }; " +
        "if($reason-ne 0) { $id=[int]$adapter.Index; $targets+=$id; $targetSettings[$id]=[guid]$adapter.SettingID; $reasons[$id]=$reason } }; ";

    private const string Renew = "foreach($adapter in $eligible) { $id=[int]$adapter.Index; if($targetSettings.ContainsKey($id)) { $r=Invoke-CimMethod -InputObject $adapter -MethodName RenewDHCPLease -OperationTimeoutSec 60; if($r.ReturnValue -eq 1) { exit 3010 }; if($r.ReturnValue -ne 0) { throw ('DHCP renewal failed for adapter '+$id+' with code '+$r.ReturnValue) } } }; ";

    private const string Snapshot = "$snapshotUtc=[DateTimeOffset]::UtcNow; $adapters=@(Get-Eligible | ForEach-Object { $id=[int]$_.Index; if($targetSettings.ContainsKey($id) -and $targetSettings[$id] -ne [guid]$_.SettingID) { throw 'Adapter identity changed during renewal' }; $state=Read-State $_; " +
        "[pscustomobject]@{id=$id;settingId=([guid]$_.SettingID).ToString();addresses=@($_.IPAddress);hasServer=[bool]$state.hasServer;leaseObtainedUtc=$(if($null-eq $state.obtained){$null}else{$state.obtained.ToString('o')});leaseExpiresUtc=$(if($null-eq $state.expires){$null}else{$state.expires.ToString('o')})} }); " +
        "$reasonRows=@($targets | ForEach-Object { [pscustomobject]@{id=[int]$_;reason=[int]$reasons[[int]$_]} }); [pscustomobject]@{schema=2;capturedUtc=$snapshotUtc.ToString('o');targets=@($targets);reasons=$reasonRows;adapters=$adapters} | ConvertTo-Json -Depth 5 -Compress";

    public static RepairStep Step(bool verification) => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Prefix + SelectTargets + (verification ? "" : Renew) + Snapshot], verification);

    public static bool HasUsableAddress(IEnumerable<string> addresses) => addresses.Any(text =>
    {
        if (!IPAddress.TryParse(text, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork) return false;
        var bytes = ip.GetAddressBytes();
        return bytes[0] is > 0 and < 224 and not 127 && !(bytes[0] == 169 && bytes[1] == 254);
    });

    public static DhcpSnapshot? Read(ToolResult result)
    {
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return null;
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 6 });
            if (!Unique(json.RootElement)) return null;
            return json.RootElement.TryGetProperty("schema", out var schema)
                ? schema.ValueKind == JsonValueKind.Number && schema.TryGetInt32(out int version) && version == 2
                    ? ReadDetailed(json.RootElement)
                    : null
                : ReadLegacy(json.RootElement);
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException) { return null; }
    }

    private static DhcpSnapshot? ReadDetailed(JsonElement root)
    {
        if (root.EnumerateObject().Count() != 5 || !Utc(root, "capturedUtc", out var captured) ||
            !root.TryGetProperty("targets", out var targetValues) || targetValues.ValueKind != JsonValueKind.Array ||
            !root.TryGetProperty("reasons", out var reasonValues) || reasonValues.ValueKind != JsonValueKind.Array ||
            !root.TryGetProperty("adapters", out var adapterValues) || adapterValues.ValueKind != JsonValueKind.Array ||
            targetValues.GetArrayLength() > 256 || reasonValues.GetArrayLength() != targetValues.GetArrayLength() || adapterValues.GetArrayLength() > 256) return null;
        var targets = targetValues.EnumerateArray().Select(ReadIndex).ToArray();
        if (targets.Any(id => id < 0) || targets.Distinct().Count() != targets.Length) return null;
        var reasons = new Dictionary<int, DhcpRenewalReason>();
        foreach (var item in reasonValues.EnumerateArray())
        {
            if (!Unique(item) || item.EnumerateObject().Count() != 2 || !item.TryGetProperty("id", out var idValue) ||
                !item.TryGetProperty("reason", out var reasonValue) || !idValue.TryGetInt32(out int id) || id < 0 ||
                !reasonValue.TryGetInt32(out int raw) || raw is < 1 or > 3 || !targets.Contains(id) ||
                !reasons.TryAdd(id, (DhcpRenewalReason)raw)) return null;
        }
        var addresses = new Dictionary<int, string[]>();
        var identities = new Dictionary<int, Guid>();
        var details = new Dictionary<int, DhcpAdapterSnapshot>();
        foreach (var item in adapterValues.EnumerateArray())
        {
            if (!Unique(item) || item.EnumerateObject().Count() != 6 || !item.TryGetProperty("id", out var idValue) ||
                !idValue.TryGetInt32(out int id) || id < 0 || !item.TryGetProperty("settingId", out var identityValue) ||
                !Guid.TryParse(identityValue.GetString(), out var identity) || identity == Guid.Empty || identities.ContainsValue(identity) ||
                !item.TryGetProperty("addresses", out var addressValues) || addressValues.ValueKind != JsonValueKind.Array ||
                addressValues.GetArrayLength() > 64 || !Boolean(item, "hasServer", out bool server) ||
                !NullableUtc(item, "leaseObtainedUtc", out var obtained) || !NullableUtc(item, "leaseExpiresUtc", out var expires) ||
                obtained.HasValue != expires.HasValue || obtained.HasValue && expires <= obtained) return null;
            var adapterAddresses = addressValues.EnumerateArray().Where(value => value.ValueKind != JsonValueKind.Null).Select(value => value.GetString()!).ToArray();
            if (adapterAddresses.Any(address => !IPAddress.TryParse(address, out _)) || !addresses.TryAdd(id, adapterAddresses) ||
                !identities.TryAdd(id, identity) || !details.TryAdd(id, new(adapterAddresses, server, obtained, expires))) return null;
        }
        if (targets.Any(id => !addresses.ContainsKey(id))) return null;
        return new(targets, addresses) { Identities = identities, Details = details, Reasons = reasons, CapturedUtc = captured, Detailed = true };
    }

    private static DhcpSnapshot? ReadLegacy(JsonElement root)
    {
        if (root.EnumerateObject().Count() != 2 || !root.TryGetProperty("targets", out var targetValues) || targetValues.ValueKind != JsonValueKind.Array ||
            !root.TryGetProperty("adapters", out var adapterValues) || adapterValues.ValueKind != JsonValueKind.Array ||
            targetValues.GetArrayLength() > 256 || adapterValues.GetArrayLength() > 256) return null;
        var targets = targetValues.EnumerateArray().Select(ReadIndex).ToArray();
        var adapters = new Dictionary<int, string[]>();
        var identities = new Dictionary<int, Guid>();
        foreach (var item in adapterValues.EnumerateArray())
        {
            if (!Unique(item) || item.EnumerateObject().Count() != 3 || !item.TryGetProperty("id", out var idValue) ||
                !idValue.TryGetInt32(out int id) || id < 0 || !item.TryGetProperty("settingId", out var identityValue) ||
                !Guid.TryParse(identityValue.GetString(), out var identity) || identity == Guid.Empty || identities.ContainsValue(identity) ||
                !item.TryGetProperty("addresses", out var addressValues) || addressValues.ValueKind != JsonValueKind.Array || addressValues.GetArrayLength() > 64) return null;
            var values = addressValues.EnumerateArray().Where(value => value.ValueKind != JsonValueKind.Null).Select(value => value.GetString()!).ToArray();
            if (values.Any(address => !IPAddress.TryParse(address, out _)) || !adapters.TryAdd(id, values) || !identities.TryAdd(id, identity)) return null;
        }
        if (targets.Any(id => id < 0 || !adapters.ContainsKey(id)) || targets.Distinct().Count() != targets.Length) return null;
        return new(targets, adapters) { Identities = identities,
            Reasons = targets.ToDictionary(id => id, _ => DhcpRenewalReason.MissingAddress) };
    }

    public static WorkflowCheck Interpret(ToolResult result)
    {
        var state = Read(result);
        if (state is null) return new("DHCP leases", CheckState.Unknown, "DHCP adapter state could not be read. No renewal selected.", Evidence: result);
        int missing = state.Reasons.Count(pair => pair.Value.HasFlag(DhcpRenewalReason.MissingAddress));
        int expired = state.Reasons.Count(pair => pair.Value.HasFlag(DhcpRenewalReason.ExpiredLease));
        if (state.Detailed && state.Targets.Length != state.Reasons.Count)
            return new("DHCP leases", CheckState.Unknown, "DHCP renewal reasons were incomplete. No renewal selected.", Evidence: result);
        if (!state.Detailed)
            missing = state.Adapters.Values.Count(addresses => !HasUsableAddress(addresses));
        return missing > 0 || expired > 0
            ? new("DHCP leases", CheckState.Repairable,
                $"{missing} connected physical DHCP adapter{(missing == 1 ? "" : "s")} lack a usable IPv4 address and {expired} have an expired coherent lease. Renewal requests fresh leases only for those exact adapters without releasing addresses or changing static settings.", RepairKind.DhcpLease, result)
            : new("DHCP leases", CheckState.Clear, $"{state.Adapters.Count} eligible adapters checked. No missing or expired DHCP IPv4 lease was found. DNS and Internet reachability are not verified.", Evidence: result);
    }

    public static bool TargetResolved(DhcpSnapshot before, int id, DhcpSnapshot after)
    {
        if (!before.Reasons.TryGetValue(id, out var reason) || reason == DhcpRenewalReason.None ||
            !before.Identities.TryGetValue(id, out var identity) || !after.Identities.TryGetValue(id, out var finalIdentity) ||
            identity != finalIdentity || !after.Adapters.TryGetValue(id, out var addresses)) return false;
        if (reason.HasFlag(DhcpRenewalReason.MissingAddress) && !HasUsableAddress(addresses)) return false;
        if (reason.HasFlag(DhcpRenewalReason.ExpiredLease))
        {
            if (!after.Detailed || after.CapturedUtc is not { } captured || !after.Details.TryGetValue(id, out var detail) ||
                detail.HasServer != true || detail.LeaseObtainedUtc is null || detail.LeaseExpiresUtc is null ||
                detail.LeaseExpiresUtc <= captured) return false;
        }
        return true;
    }

    public static bool Verify(IReadOnlyList<RepairStepResult> steps)
    {
        if (steps.Count != 2) return false;
        var before = Read(steps[0].Result);
        var after = Read(steps[1].Result);
        return before is not null && after is not null && before.Targets.Length > 0 &&
            before.Targets.All(id => TargetResolved(before, id, after));
    }

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        return Interpret(await InspectEvidenceAsync(token).ConfigureAwait(false));
    }

    public static Task<ToolResult> InspectEvidenceAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("DHCP inventory requires Windows.");
        return BoundedTool.RunAsync(Step(true), TimeSpan.FromSeconds(40), token: token);
    }

    public static async Task<ToolResult> ExecuteAsync(RepairStep step, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        var result = await BoundedTool.RunAsync(step, TimeSpan.FromMinutes(3), progress, token).ConfigureAwait(false);
        if (!step.Verification && Read(result) is { Targets.Length: 0 })
            throw new RepairStepSkippedException(
                "Fresh DHCP evidence no longer identifies a missing or expired lease. No renewal request was sent.", result);
        return result;
    }

    private static int ReadIndex(JsonElement value) => value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int index) ? index : -1;
    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool Utc(JsonElement owner, string name, out DateTimeOffset value)
    {
        value = default;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String &&
            DateTimeOffset.TryParse(item.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value) && value.Offset == TimeSpan.Zero;
    }
    private static bool NullableUtc(JsonElement owner, string name, out DateTimeOffset? value)
    {
        value = null;
        if (!owner.TryGetProperty(name, out var item)) return false;
        if (item.ValueKind == JsonValueKind.Null) return true;
        if (item.ValueKind != JsonValueKind.String || !DateTimeOffset.TryParse(item.GetString(), CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind, out var parsed) || parsed.Offset != TimeSpan.Zero) return false;
        value = parsed;
        return true;
    }
}
