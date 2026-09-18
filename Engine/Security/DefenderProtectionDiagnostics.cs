using System.Text.Json;

namespace KillerBench.Engine;

public static class DefenderProtectionDiagnostics
{
    private static readonly string[] PolicyPaths =
    [
        @"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender",
        @"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection",
        @"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\MpEngine",
        @"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Spynet",
        @"HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender\Windows Defender Exploit Guard\Network Protection"
    ];

    private static readonly HashSet<string> RecognizedPolicyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "DisableAntiSpyware", "DisableAntiVirus", "DisableRealtimeMonitoring", "DisableBehaviorMonitoring",
        "DisableIOAVProtection", "DisableIntrusionPrevention", "PUAProtection",
        "MpEnablePus", "EnableNetworkProtection", "SpynetReporting", "SubmitSamplesConsent",
        "CloudBlockLevel", "EnableControlledFolderAccess"
    };

    public static RepairStep QueryStep()
    {
        string paths = string.Join(',', PolicyPaths.Select(path => "'" + path.Replace("'", "''", StringComparison.Ordinal) + "'"));
        string script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
            "$s=Get-MpComputerStatus; $p=Get-MpPreference; $managed=@(); " +
            "foreach($path in @(" + paths + ")){ if(Test-Path -LiteralPath $path){ " +
            "$managed += @((Get-Item -LiteralPath $path -ErrorAction Stop).GetValueNames() | " +
            "Where-Object { $_ -in @('DisableAntiSpyware','DisableAntiVirus','DisableRealtimeMonitoring','DisableBehaviorMonitoring','DisableIOAVProtection','DisableIntrusionPrevention','PUAProtection','MpEnablePus','EnableNetworkProtection','SpynetReporting','SubmitSamplesConsent','CloudBlockLevel','EnableControlledFolderAccess') }) } }; " +
            "[pscustomobject]@{complete=$true; mode=[string]$s.AMRunningMode; service=[bool]$s.AMServiceEnabled; antivirus=[bool]$s.AntivirusEnabled; realtime=[bool]$s.RealTimeProtectionEnabled; " +
            "behavior=[bool]$s.BehaviorMonitorEnabled; ioav=[bool]$s.IoavProtectionEnabled; nis=[bool]$s.NISEnabled; tamper=[bool]$s.IsTamperProtected; " +
            "pua=[int]$p.PUAProtection; network=[int]$p.EnableNetworkProtection; maps=[int]$p.MAPSReporting; samples=[int]$p.SubmitSamplesConsent; cloud=[int]$p.CloudBlockLevel; cfa=[int]$p.EnableControlledFolderAccess; managed=@($managed | Sort-Object -Unique) } | ConvertTo-Json -Compress";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true);
    }

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        ToolResult result = await BoundedTool.RunAsync(QueryStep(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult result)
    {
        string[] names = ["Defender active protection", "Behavior monitoring", "Downloaded-file scanning", "Network inspection system", "Tamper protection", "Potentially unwanted application protection", "Network protection", "Cloud protection", "Automatic sample submission", "Controlled folder access"];
        WorkflowCheck[] unknown = names.Select(name => new WorkflowCheck(name, CheckState.Unknown,
            "Microsoft Defender protection settings are unavailable or incomplete. No setting or policy was changed.", Evidence: result)).ToArray();
        if (result.ExitCode != 0 || result.Truncated || result.TimedOut || !string.IsNullOrWhiteSpace(result.Error)) return unknown;
        try
        {
            using JsonDocument json = JsonDocument.Parse(result.Output);
            JsonElement root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || Boolean(root, "complete") != true ||
                Text(root, "mode", 64) is not string mode || Boolean(root, "service") is not bool service ||
                Boolean(root, "antivirus") is not bool antivirus || Boolean(root, "realtime") is not bool realtime ||
                Boolean(root, "behavior") is not bool behavior || Boolean(root, "ioav") is not bool ioav ||
                Boolean(root, "nis") is not bool nis || Boolean(root, "tamper") is not bool tamper ||
                Integer(root, "pua", 0, 2) is not int pua || Integer(root, "network", 0, 2) is not int network ||
                Integer(root, "maps", 0, 2) is not int maps || Integer(root, "samples", 0, 3) is not int samples ||
                Integer(root, "cloud", 0, 6) is not int cloud || Integer(root, "cfa", 0, 6) is not int cfa ||
                Managed(root) is not string[] managed) return unknown;

            string authority = managed.Length == 0
                ? "No recognized machine policy value name was observed in the inspected Defender policy locations. This does not prove that the settings are unmanaged."
                : $"Recognized machine policy value names observed: {string.Join(", ", managed)}. Effective policy ownership requires policy tooling.";
            bool active = mode.Equals("Normal", StringComparison.OrdinalIgnoreCase) && service && antivirus && realtime;
            return
            [
                Check(names[0], active ? CheckState.Clear : CheckState.Attention,
                    active ? "Microsoft Defender reports normal mode with its service, antivirus and real-time protection enabled. This is not a malware scan."
                        : $"Microsoft Defender reports mode {mode}, service {OnOff(service)}, antivirus {OnOff(antivirus)} and real-time protection {OnOff(realtime)}. Review the intended security product and effective policy.", authority, result),
                Check(names[1], behavior ? CheckState.Clear : CheckState.Attention,
                    behavior ? "Behavior monitoring is enabled."
                        : "Behavior monitoring is disabled. Compare this with the required security baseline and effective policy.", authority, result),
                Check(names[2], ioav ? CheckState.Clear : CheckState.Attention,
                    ioav ? "Scanning of downloaded files and attachments is enabled."
                        : "Scanning of downloaded files and attachments is disabled. Compare this with the required security baseline and effective policy.", authority, result),
                Check(names[3], nis ? CheckState.Clear : CheckState.Attention,
                    nis ? "The network inspection system is enabled."
                        : "The network inspection system is disabled. Compare this with the required security baseline and effective policy.", authority, result),
                Check(names[4], tamper ? CheckState.Clear : CheckState.Attention,
                    tamper ? "Tamper protection is enabled."
                        : "Tamper protection is disabled. Review Windows Security and the effective management policy before changing it.", authority, result),
                Check(names[5], pua == 1 ? CheckState.Clear : pua == 2 ? CheckState.Information : CheckState.Attention,
                    pua == 1 ? "Potentially unwanted application protection is enabled."
                        : pua == 2 ? "Potentially unwanted application protection is in audit mode. Detection is recorded without blocking."
                        : "Potentially unwanted application protection is disabled. Compare this with the required security baseline.", authority, result),
                Check(names[6], network == 1 ? CheckState.Clear : network == 2 ? CheckState.Information : CheckState.Attention,
                    network == 1 ? "Network protection is enabled."
                        : network == 2 ? "Network protection is in audit mode. Network events can be recorded without blocking."
                        : "Network protection is disabled. Compare this with the required security baseline.", authority, result),
                Check(names[7], maps == 0 ? CheckState.Attention : CheckState.Information,
                    $"Cloud-delivered protection reporting mode is {maps}; cloud blocking level is {cloud}. Numeric values are retained so future Windows values are not assigned an outdated label.", authority, result),
                Check(names[8], CheckState.Information,
                    $"Automatic sample submission mode is {samples}. File contents, sample history and submitted identities were not collected.", authority, result),
                Check(names[9], CheckState.Information,
                    $"Controlled folder access mode is {cfa}. Protected folders, allowed applications and event history were not collected by this check.", authority, result)
            ];
        }
        catch (JsonException) { return unknown; }
    }

    private static WorkflowCheck Check(string name, CheckState state, string detail, string authority, ToolResult evidence) =>
        new(name, state, detail + " " + authority + " No Defender preference, exclusion or policy was changed.", Evidence: evidence);

    private static string OnOff(bool value) => value ? "enabled" : "disabled";
    private static bool? Boolean(JsonElement root, string name) => root.TryGetProperty(name, out JsonElement value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;
    private static int? Integer(JsonElement root, string name, int minimum, int maximum) => root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number) && number >= minimum && number <= maximum ? number : null;
    private static string? Text(JsonElement root, string name, int maximum) => root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String && value.GetString() is string text && text.Length is > 0 && text.Length <= maximum ? text : null;

    private static string[]? Managed(JsonElement root)
    {
        if (!root.TryGetProperty("managed", out JsonElement list) || list.ValueKind != JsonValueKind.Array || list.GetArrayLength() > RecognizedPolicyNames.Count) return null;
        var names = new List<string>();
        foreach (JsonElement item in list.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || item.GetString() is not string name || !RecognizedPolicyNames.Contains(name) || names.Contains(name, StringComparer.OrdinalIgnoreCase)) return null;
            names.Add(name);
        }
        names.Sort(StringComparer.OrdinalIgnoreCase);
        return names.ToArray();
    }
}
