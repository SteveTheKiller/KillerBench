using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class DnsNrptPolicyDiagnostics
{
    private const int MaximumRules = 256;
    private const int MaximumValuesPerRule = 32;
    private const string Scope = " Namespace and server values remain in raw local evidence and are omitted from this summary. Effective policy does not prove DNS reachability, authentication, tunnel state or successful resolution. No namespace, resolver, adapter, route, VPN, DirectAccess, DNSSEC, IPsec or policy setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-DnsClientNrptPolicy -Effective -ErrorAction Stop|Select-Object -First 257);" +
        "$rows=@($all|Select-Object -First 256|ForEach-Object{[pscustomobject]@{" +
        "namespaces=@($_.Namespace|ForEach-Object{[string]$_}|Select-Object -First 33);nameServers=@($_.NameServers|ForEach-Object{[string]$_}|Select-Object -First 33);" +
        "directAccessDnsServers=@($_.DirectAccessDnsServers|ForEach-Object{[string]$_}|Select-Object -First 33);" +
        "directAccessEnabled=$(if($null-eq$_.DirectAccessEnabled){$null}else{[bool]$_.DirectAccessEnabled});" +
        "directAccessIpsecRequired=$(if($null-eq$_.DirectAccessQueryIPsecRequired){$null}else{[bool]$_.DirectAccessQueryIPsecRequired});" +
        "dnsSecIpsecRequired=$(if($null-eq$_.DnsSecQueryIPsecRequired){$null}else{[bool]$_.DnsSecQueryIPsecRequired});" +
        "dnsSecValidationRequired=$(if($null-eq$_.DnsSecValidationRequired){$null}else{[bool]$_.DnsSecValidationRequired});" +
        "queryPolicy=[string]$_.QueryPolicy;secureFallback=[string]$_.SecureNameQueryFallback}});" +
        "$complete=($all.Count-le 256-and@($rows|Where-Object{$_.namespaces.Count-gt 32-or$_.nameServers.Count-gt 32-or$_.directAccessDnsServers.Count-gt 32}).Count-eq 0);" +
        "[pscustomobject]@{complete=$complete;rules=$rows}|ConvertTo-Json -Depth 6 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Effective DNS policy inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(35), token: cancellationToken);
            var evidence = await (execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("DNS policy provider returned no task.")).WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("DNS policy provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        { return Unknown("Effective DNS policy inspection exceeded its overall deadline.", new(-1, "", "DNS policy deadline expired.", false) { TimedOut = true }); }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { return Unknown("Effective DNS policy inspection was unavailable.", new(-1, "", error.Message, false)); }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 1_000_000)
            return Unknown("Effective DNS policy collection failed or returned incomplete evidence.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 7 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) || !complete ||
                !root.TryGetProperty("rules", out var rules) || rules.ValueKind != JsonValueKind.Array || rules.GetArrayLength() > MaximumRules)
                return Unknown("Effective DNS policy evidence was malformed, incomplete or exceeded its rule bound.", evidence);
            if (rules.GetArrayLength() == 0)
                return new("Effective DNS policy", CheckState.Information, "No effective Name Resolution Policy Table rule was returned." + Scope, Evidence: evidence);

            int directAccess = 0, customServers = 0, dnsSecValidation = 0, ipsecRequired = 0;
            var namespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rule in rules.EnumerateArray())
            {
                if (!Unique(rule, 9) || !Strings(rule, "namespaces", required: true, namespaces, out _) ||
                    !Strings(rule, "nameServers", required: false, null, out int nameServerCount) ||
                    !Strings(rule, "directAccessDnsServers", required: false, null, out int directServerCount) ||
                    !NullableBoolean(rule, "directAccessEnabled", out bool? directEnabled) ||
                    !NullableBoolean(rule, "directAccessIpsecRequired", out bool? directIpsec) ||
                    !NullableBoolean(rule, "dnsSecIpsecRequired", out bool? dnsSecIpsec) ||
                    !NullableBoolean(rule, "dnsSecValidationRequired", out bool? validation) ||
                    !OptionalText(rule, "queryPolicy") || !OptionalText(rule, "secureFallback"))
                    return Unknown("Effective DNS policy evidence contained an invalid or duplicate rule.", evidence);
                if (directEnabled == true) directAccess++;
                if (nameServerCount > 0 || directServerCount > 0) customServers++;
                if (validation == true) dnsSecValidation++;
                if (directIpsec == true || dnsSecIpsec == true) ipsecRequired++;
            }

            string detail = $"Reviewed {rules.GetArrayLength()} effective DNS policy rule{(rules.GetArrayLength() == 1 ? "" : "s")} across {namespaces.Count} namespace value{(namespaces.Count == 1 ? "" : "s")}. " +
                $"{directAccess} use DirectAccess, {customServers} route to policy-specific DNS servers, {dnsSecValidation} require DNSSEC validation, and {ipsecRequired} require IPsec for DNS queries.";
            return new("Effective DNS policy", CheckState.Information, detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        { return Unknown("Effective DNS policy evidence could not be interpreted.", evidence); }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new("Effective DNS policy", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value) { value = false; if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false; value = item.GetBoolean(); return true; }
    private static bool NullableBoolean(JsonElement owner, string name, out bool? value) { value = null; if (!owner.TryGetProperty(name, out var item)) return false; if (item.ValueKind == JsonValueKind.Null) return true; if (item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false; value = item.GetBoolean(); return true; }
    private static bool OptionalText(JsonElement owner, string name) => owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String && item.GetString() is { } text && text.Length <= 128 && text == text.Trim() && !text.Any(char.IsControl);
    private static bool Strings(JsonElement owner, string name, bool required, HashSet<string>? identities, out int count)
    {
        count = 0;
        if (!owner.TryGetProperty(name, out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > MaximumValuesPerRule) return false;
        var local = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || item.GetString() is not { } text || !DriverReference.ValidText(text, 512) ||
                text != text.Trim() || !local.Add(text) || identities is not null && !identities.Add(text)) return false;
            count++;
        }
        return !required || count > 0;
    }
}
