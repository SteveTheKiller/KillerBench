using System.Text.Json;

namespace KillerBench.Engine;

public static class DefaultPrinterDiagnostics
{
    private const int MaximumQueues = 512;

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$rows=@(Get-CimInstance -ClassName Win32_Printer -OperationTimeoutSec 20 -ErrorAction Stop|Sort-Object Name|Select-Object -First 513);$defaults=@($rows|Where-Object Default|ForEach-Object{[string]$_.Name});" +
            "$kind='Absent';$mode=$null;$key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\\Microsoft\\Windows NT\\CurrentVersion\\Windows');if($null-ne$key){try{$value=$key.GetValue('LegacyDefaultPrinterMode',$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames);if($null-ne$value){$kind=$key.GetValueKind('LegacyDefaultPrinterMode').ToString();if($value-is[int]){$mode=[int]$value}}}finally{$key.Dispose()}};" +
            "[pscustomobject]@{complete=($rows.Count-le 512);total=$rows.Count;defaults=$defaults;modeKind=$kind;mode=$mode}|ConvertTo-Json -Depth 3 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        const string title = "Default printer selection";
        WorkflowCheck Unknown() => new(title, CheckState.Unknown,
            "Default-printer evidence is unavailable, incomplete or malformed. No queue or per-user setting changed.", Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 5 ||
                !Unique(root, "complete", out var completeValue) || completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) || !completeValue.GetBoolean() ||
                !Unique(root, "total", out var totalValue) || !totalValue.TryGetInt32(out int total) || total is < 0 or > MaximumQueues ||
                !Unique(root, "defaults", out var defaultsValue) || defaultsValue.ValueKind != JsonValueKind.Array || defaultsValue.GetArrayLength() > total ||
                !Unique(root, "modeKind", out var kindValue) || kindValue.ValueKind != JsonValueKind.String ||
                !Unique(root, "mode", out var modeValue)) return Unknown();

            var defaults = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in defaultsValue.EnumerateArray())
                if (item.ValueKind != JsonValueKind.String || !DriverReference.ValidText(item.GetString(), 512) ||
                    item.GetString() != item.GetString()!.Trim() || !defaults.Add(item.GetString()!)) return Unknown();

            string kind = kindValue.GetString()!;
            int? mode = modeValue.ValueKind == JsonValueKind.Null ? null : modeValue.TryGetInt32(out int parsed) ? parsed : null;
            if (kind == "Absent" ? modeValue.ValueKind != JsonValueKind.Null : kind != "DWord" || mode is not (0 or 1)) return Unknown();

            string detail = total == 0
                ? "No printer queue was reported, so no default is expected."
                : defaults.Count == 0
                    ? $"Windows reported {total} printer queue{(total == 1 ? "" : "s")} but no default selection. Applications may choose differently or ask for a printer."
                    : defaults.Count == 1
                        ? $"Windows reported {total} printer queue{(total == 1 ? "" : "s")} and one default: {defaults.Single()}."
                        : $"Windows reported {total} printer queues and {defaults.Count} default selections. Conflicting provider evidence requires review.";
            detail += kind == "Absent"
                ? " No LegacyDefaultPrinterMode value is stored for the running account. This does not prove how every application chooses a printer."
                : mode == 1
                    ? " The running account stores legacy explicit default-printer mode."
                    : " The running account stores automatic default-printer management mode.";
            detail += " This snapshot does not test printing, application selection, policy ownership or future automatic changes. No queue, port, driver, job, document or per-user setting changed.";
            return new(title, total > 0 && defaults.Count != 1 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return Unknown(); }
    }

    private static bool Unique(JsonElement owner, string name, out JsonElement value)
    {
        var matches = owner.EnumerateObject().Where(property => property.NameEquals(name)).ToArray();
        value = matches.Length == 1 ? matches[0].Value : default;
        return matches.Length == 1;
    }
}
