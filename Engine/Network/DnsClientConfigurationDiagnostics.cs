using System.Text.Json;

namespace KillerBench.Engine.Network;

public static class DnsClientConfigurationDiagnostics
{
    private const int MaximumRows = 512;
    private const int MaximumSuffixes = 64;
    private static readonly HashSet<string> PolicyNames = new(StringComparer.Ordinal)
    {
        "SearchList", "PrimaryDnsSuffix", "UseDomainNameDevolution", "DomainNameDevolutionLevel", "RegistrationEnabled"
    };

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$global=Get-DnsClientGlobalSetting -ErrorAction Stop;$suffixes=@($global.SuffixSearchList|ForEach-Object{[string]$_}|Select-Object -First 65);" +
            "$rows=@(Get-DnsClient -ErrorAction Stop|Sort-Object InterfaceIndex|Select-Object -First 513|ForEach-Object{[pscustomobject]@{interfaceIndex=[int]$_.InterfaceIndex;interfaceAlias=[string]$_.InterfaceAlias;connectionSuffix=[string]$_.ConnectionSpecificSuffix;registerAddress=[bool]$_.RegisterThisConnectionsAddress;useSuffixWhenRegistering=[bool]$_.UseSuffixWhenRegistering}});" +
            "$policy=@();$path='HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows NT\\DNSClient';if(Test-Path -LiteralPath $path){$policy=@((Get-Item -LiteralPath $path -ErrorAction Stop).Property|Where-Object{$_-in @('SearchList','PrimaryDnsSuffix','UseDomainNameDevolution','DomainNameDevolutionLevel','RegistrationEnabled')}|Sort-Object -Unique)};" +
            "[pscustomobject]@{complete=($rows.Count-le 512-and$suffixes.Count-le 64);suffixes=$suffixes;useDevolution=[bool]$global.UseDevolution;devolutionLevel=[int]$global.DevolutionLevel;policyValues=$policy;rows=$rows}|ConvertTo-Json -Depth 5 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        const string title = "DNS client configuration";
        WorkflowCheck Unknown(string detail = "DNS client suffix and registration evidence is unavailable or incomplete. No adapter, DNS or policy setting changed.") =>
            new(title, CheckState.Unknown, detail, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 6 ||
                !Unique(root, "complete", out var completeValue) || completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !Unique(root, "suffixes", out var suffixesValue) || suffixesValue.ValueKind != JsonValueKind.Array ||
                !Unique(root, "useDevolution", out var devolutionValue) || devolutionValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !Unique(root, "devolutionLevel", out var levelValue) || !levelValue.TryGetInt32(out int level) || level is < 0 or > 255 ||
                !Unique(root, "policyValues", out var policyValue) || policyValue.ValueKind != JsonValueKind.Array ||
                !Unique(root, "rows", out var rowsValue) || rowsValue.ValueKind != JsonValueKind.Array ||
                !completeValue.GetBoolean() || suffixesValue.GetArrayLength() > MaximumSuffixes ||
                policyValue.GetArrayLength() > PolicyNames.Count || rowsValue.GetArrayLength() > MaximumRows) return Unknown();

            var suffixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var suffixValue in suffixesValue.EnumerateArray())
                if (suffixValue.ValueKind != JsonValueKind.String || !ValidRequired(suffixValue.GetString(), 255) ||
                    !suffixes.Add(suffixValue.GetString()!)) return Unknown();
            var policies = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in policyValue.EnumerateArray())
                if (item.ValueKind != JsonValueKind.String || !PolicyNames.Contains(item.GetString()!) || !policies.Add(item.GetString()!)) return Unknown();

            var seen = new HashSet<int>();
            int suffixed = 0, registered = 0, suffixRegistration = 0;
            foreach (var row in rowsValue.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object || row.EnumerateObject().Count() != 5 ||
                    !Unique(row, "interfaceIndex", out var indexValue) || !indexValue.TryGetInt32(out int index) || index < 1 || !seen.Add(index) ||
                    !Unique(row, "interfaceAlias", out var aliasValue) || aliasValue.ValueKind != JsonValueKind.String || !ValidRequired(aliasValue.GetString(), 256) ||
                    !Unique(row, "connectionSuffix", out var suffixValue) || suffixValue.ValueKind != JsonValueKind.String || !ValidOptional(suffixValue.GetString(), 255) ||
                    !Unique(row, "registerAddress", out var registerValue) || registerValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                    !Unique(row, "useSuffixWhenRegistering", out var useSuffixValue) || useSuffixValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return Unknown();
                if (suffixValue.GetString()!.Length > 0) suffixed++;
                if (registerValue.GetBoolean()) registered++;
                if (useSuffixValue.GetBoolean()) suffixRegistration++;
            }

            string detail = $"Reviewed {rowsValue.GetArrayLength()} DNS client interface{(rowsValue.GetArrayLength() == 1 ? "" : "s")}. {suffixed} have a connection suffix, {registered} register their addresses, and {suffixRegistration} use their suffix when registering." +
                $" The global search list contains {suffixes.Count} suffix{(suffixes.Count == 1 ? "" : "es")}; devolution is {(devolutionValue.GetBoolean() ? "enabled" : "disabled")} at level {level}.";
            detail += policies.Count == 0
                ? " No supported machine DNS policy value was registered. Absence does not prove that every effective setting is unmanaged."
                : $" {policies.Count} supported machine DNS policy value{(policies.Count == 1 ? " is" : "s are")} registered; managed settings are preserved.";
            detail += " Suffix values remain in raw evidence and are omitted from this summary. Configuration does not prove DNS registration, resolution or domain connectivity. No suffix, adapter, resolver, cache or policy setting changed.";
            return new(title, CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return Unknown(); }
    }

    private static bool ValidRequired(string? value, int maximum) =>
        value is not null && DriverReference.ValidText(value, maximum) && value == value.Trim();

    private static bool ValidOptional(string? value, int maximum) => value is not null && value.Length <= maximum &&
        value == value.Trim() && !value.Any(char.IsControl);

    private static bool Unique(JsonElement owner, string name, out JsonElement value)
    {
        var matches = owner.EnumerateObject().Where(property => property.NameEquals(name)).ToArray();
        value = matches.Length == 1 ? matches[0].Value : default;
        return matches.Length == 1;
    }
}
