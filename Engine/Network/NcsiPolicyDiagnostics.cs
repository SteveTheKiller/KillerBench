using System.Text.Json;

namespace KillerBench.Engine;

public static class NcsiPolicyDiagnostics
{
    private const string Scope = " These settings affect Windows connectivity classification, not actual network availability. Probe destinations and content are excluded from retained evidence. No registry, policy, adapter, route, DNS, proxy, firewall or service setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$policy='HKLM:\\SOFTWARE\\Policies\\Microsoft\\Windows\\NetworkConnectivityStatusIndicator';" +
        "$local='HKLM:\\SYSTEM\\CurrentControlSet\\Services\\NlaSvc\\Parameters\\Internet';" +
        "$p=$(if(Test-Path -LiteralPath $policy){Get-ItemProperty -LiteralPath $policy -ErrorAction Stop}else{$null});" +
        "$l=$(if(Test-Path -LiteralPath $local){Get-ItemProperty -LiteralPath $local -ErrorAction Stop}else{$null});" +
        "$v={param($o,$n)if($null-eq$o){$null}else{$x=$o.PSObject.Properties[$n];if($null-eq$x){$null}else{$x.Value}}};" +
        "$web=@{ActiveWebProbeHost='www.msftconnecttest.com';ActiveWebProbeHostV6='ipv6.msftconnecttest.com';ActiveWebProbePath='connecttest.txt';ActiveWebProbePathV6='connecttest.txt';ActiveWebProbeContent='Microsoft Connect Test';ActiveWebProbeContentV6='Microsoft Connect Test'};" +
        "$dns=@{ActiveDnsProbeHost='dns.msftncsi.com';ActiveDnsProbeHostV6='dns.msftncsi.com';ActiveDnsProbeContent='131.107.255.255';ActiveDnsProbeContentV6='fd3e:4f5a:5b81::1'};" +
        "$custom={param($defaults)@($defaults.GetEnumerator()|Where-Object{$actual=(&$v $l $_.Key);$null-ne$actual-and![string]::Equals([string]$actual,[string]$_.Value,[StringComparison]::OrdinalIgnoreCase)}).Count-gt 0};" +
        "[pscustomobject]@{policyNoActiveProbe=(&$v $p 'NoActiveProbe');policyDisablePassivePolling=(&$v $p 'DisablePassivePolling');localEnableActiveProbing=(&$v $l 'EnableActiveProbing');customWebProbe=[bool](&$custom $web);customDnsProbe=[bool](&$custom $dns)}|ConvertTo-Json -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("NCSI policy inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(25));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(20), token: cancellationToken);
            var evidence = await (execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("NCSI policy provider returned no task.")).WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("NCSI policy provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("NCSI policy inspection exceeded its overall deadline.",
                new(-1, "", "NCSI policy deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("NCSI policy inspection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated ||
            !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 16_384)
            return Unknown("NCSI policy collection failed or returned incomplete evidence.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 3 });
            var root = document.RootElement;
            if (!Unique(root, 5) || !NullableSwitch(root, "policyNoActiveProbe", out int? noActive) ||
                !NullableSwitch(root, "policyDisablePassivePolling", out int? noPassive) ||
                !NullableSwitch(root, "localEnableActiveProbing", out int? localActive) ||
                !Boolean(root, "customWebProbe", out bool customWeb) || !Boolean(root, "customDnsProbe", out bool customDns))
                return Unknown("NCSI policy evidence was malformed or contained unsupported values.", evidence);

            bool activeDisabled = noActive == 1 || noActive is null && localActive == 0;
            bool passiveDisabled = noPassive == 1;
            string detail = activeDisabled
                ? "Active connectivity probing is disabled by " + (noActive == 1 ? "policy" : "local configuration") + ". Windows Internet labels may rely on passive observations or remain limited."
                : "Active connectivity probing is enabled or no explicit disabling value was found.";
            detail += passiveDisabled
                ? " Passive polling is disabled by policy."
                : " Passive polling is enabled or no explicit disabling policy was found.";
            if (customWeb || customDns)
                detail += $" Custom probe configuration is present for {(customWeb && customDns ? "web and DNS" : customWeb ? "web" : "DNS")} checks; destinations and expected content are intentionally omitted.";
            else
                detail += " No explicit web or DNS probe override was found.";
            CheckState state = activeDisabled && passiveDisabled ? CheckState.Attention : CheckState.Information;
            if (state == CheckState.Attention)
                detail += " Both active and passive classification paths are explicitly disabled, so Windows connectivity labels require policy-owner review.";
            return new("Windows connectivity classification policy", state, detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return Unknown("NCSI policy evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) =>
        new("Windows connectivity classification policy", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int count) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == count && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == count;
    private static bool NullableSwitch(JsonElement owner, string name, out int? value)
    {
        value = null;
        if (!owner.TryGetProperty(name, out var item)) return false;
        if (item.ValueKind == JsonValueKind.Null) return true;
        if (item.ValueKind != JsonValueKind.Number || !item.TryGetInt32(out int number) || number is not (0 or 1)) return false;
        value = number;
        return true;
    }
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
}
