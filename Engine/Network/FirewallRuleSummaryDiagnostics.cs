using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record FirewallRuleSummary(int Total, int Enabled, int Disabled,
    int InboundAllow, int InboundBlock, int OutboundAllow, int OutboundBlock,
    int Local, int GroupPolicy, int Dynamic, int System, int OtherSource);

public static class FirewallRuleSummaryDiagnostics
{
    private const string Scope = " Effective ActiveStore rule aggregates only. Rule names, groups, applications, services, packages, users, ports, addresses, interfaces and security descriptors are excluded. Traffic enforcement and third-party filtering are not tested. No firewall setting was changed.";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Firewall rule collection requires Windows.");
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$rules=@(Get-NetFirewallRule -PolicyStore ActiveStore -ErrorAction Stop);" +
            "$enabled=@($rules|Where-Object {$_.Enabled.ToString() -eq 'True'});" +
            "$source=@{Local=0;GroupPolicy=0;Dynamic=0;System=0;Other=0};" +
            "foreach($rule in $rules){switch($rule.PolicyStoreSourceType.ToString()){'Local'{$source.Local++}'GroupPolicy'{$source.GroupPolicy++}'Dynamic'{$source.Dynamic++}'Generated'{$source.System++}'Hardcoded'{$source.System++}default{$source.Other++}}};" +
            "[pscustomobject]@{complete=$true;total=$rules.Count;enabled=$enabled.Count;disabled=@($rules|Where-Object {$_.Enabled.ToString() -eq 'False'}).Count;" +
            "inAllow=@($enabled|Where-Object {$_.Direction.ToString() -eq 'Inbound' -and $_.Action.ToString() -eq 'Allow'}).Count;" +
            "inBlock=@($enabled|Where-Object {$_.Direction.ToString() -eq 'Inbound' -and $_.Action.ToString() -eq 'Block'}).Count;" +
            "outAllow=@($enabled|Where-Object {$_.Direction.ToString() -eq 'Outbound' -and $_.Action.ToString() -eq 'Allow'}).Count;" +
            "outBlock=@($enabled|Where-Object {$_.Direction.ToString() -eq 'Outbound' -and $_.Action.ToString() -eq 'Block'}).Count;" +
            "local=$source.Local;groupPolicy=$source.GroupPolicy;dynamic=$source.Dynamic;system=$source.System;other=$source.Other}|ConvertTo-Json -Compress";
        ToolResult evidence;
        try
        {
            evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
                TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            evidence = new(-1, "", error.Message, false);
        }
        token.ThrowIfCancellationRequested();
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult? evidence)
    {
        WorkflowCheck Unknown(string detail) => new("Windows Firewall rules", CheckState.Unknown, detail + Scope, Evidence: evidence);
        if (evidence is null || evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Firewall rule collection failed or was incomplete.");
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 3 });
            var root = document.RootElement;
            string[] names = ["complete", "total", "enabled", "disabled", "inAllow", "inBlock", "outAllow", "outBlock", "local", "groupPolicy", "dynamic", "system", "other"];
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != names.Length ||
                root.EnumerateObject().Select(property => property.Name).Any(name => !names.Contains(name, StringComparer.Ordinal)) ||
                !root.TryGetProperty("complete", out var complete) || complete.ValueKind != JsonValueKind.True)
                return Unknown("Firewall rule summary had an unrecognized shape.");
            int Read(string name)
            {
                if (!root.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Number ||
                    !value.TryGetInt32(out int parsed) || parsed < 0 || parsed > 1_000_000)
                    throw new InvalidDataException("Invalid firewall rule count.");
                return parsed;
            }
            var summary = new FirewallRuleSummary(Read("total"), Read("enabled"), Read("disabled"),
                Read("inAllow"), Read("inBlock"), Read("outAllow"), Read("outBlock"),
                Read("local"), Read("groupPolicy"), Read("dynamic"), Read("system"), Read("other"));
            if (summary.Enabled + summary.Disabled != summary.Total ||
                summary.InboundAllow + summary.InboundBlock + summary.OutboundAllow + summary.OutboundBlock != summary.Enabled ||
                summary.Local + summary.GroupPolicy + summary.Dynamic + summary.System + summary.OtherSource != summary.Total)
                return Unknown("Firewall rule counts were internally inconsistent.");
            string detail = "Rules: " + Show(summary.Total) + " total, " + Show(summary.Enabled) + " enabled and " + Show(summary.Disabled) +
                " disabled. Enabled direction and action: inbound allow " + Show(summary.InboundAllow) + ", inbound block " + Show(summary.InboundBlock) +
                ", outbound allow " + Show(summary.OutboundAllow) + ", outbound block " + Show(summary.OutboundBlock) +
                ". Policy source: local " + Show(summary.Local) + ", Group Policy " + Show(summary.GroupPolicy) + ", dynamic " + Show(summary.Dynamic) +
                ", Windows-generated or built-in " + Show(summary.System) + ", other or future source " + Show(summary.OtherSource) +
                ". Counts describe configuration and do not establish whether a rule is required, safe, reachable or effective." + Scope;
            return new("Windows Firewall rules", summary.OtherSource > 0 ? CheckState.Attention : CheckState.Information,
                (summary.OtherSource > 0 ? "One or more rules use an unrecognized policy source. Review effective policy before changing them. " : "") + detail,
                Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or InvalidDataException or OverflowException)
        {
            return Unknown("Firewall rule summary could not be interpreted.");
        }
    }

    private static string Show(int value) => value.ToString(CultureInfo.InvariantCulture);
}
