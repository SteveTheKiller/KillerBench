using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine;

public static class InstallerActivityDiagnostics
{
    private const string Script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
        "$rows=@(Get-CimInstance -ClassName Win32_Process -Filter \"Name='msiexec.exe'\" -OperationTimeoutSec 15 | Select-Object -First 129); " +
        "$items=@($rows | ForEach-Object {[pscustomobject]@{id=[uint32]$_.ProcessId;parent=[uint32]$_.ParentProcessId;created=if ($_.CreationDate) {$_.CreationDate.ToUniversalTime().ToString('o')} else {$null}}}); " +
        "$marker=Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\InProgress'; " +
        "[pscustomobject]@{complete=($rows.Count -le 128);marker=[bool]$marker;processes=$items} | ConvertTo-Json -Depth 3 -Compress";

    public static RepairStep Step() => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows Installer activity collection requires Windows.");
        return Interpret(await BoundedTool.RunAsync(Step(), TimeSpan.FromSeconds(25), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        WorkflowCheck Unknown() => new("Installer activity", CheckState.Unknown,
            "Windows Installer activity evidence was unavailable, incomplete or malformed. No transaction state was inferred and no recovery was selected.", Evidence: result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return Unknown();
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 3 ||
                !Unique(root, "complete", out var complete) || complete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) || !complete.GetBoolean() ||
                !Unique(root, "marker", out var marker) || marker.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !Unique(root, "processes", out var processes) || processes.ValueKind != JsonValueKind.Array || processes.GetArrayLength() > 128)
                return Unknown();
            var identities = new HashSet<uint>();
            var observations = new List<string>();
            foreach (var process in processes.EnumerateArray())
            {
                if (process.ValueKind != JsonValueKind.Object || process.EnumerateObject().Count() != 3 ||
                    !Unique(process, "id", out var id) || !id.TryGetUInt32(out uint processId) || processId == 0 || !identities.Add(processId) ||
                    !Unique(process, "parent", out var parent) || !parent.TryGetUInt32(out uint parentId) ||
                    !Unique(process, "created", out var created) || created.ValueKind != JsonValueKind.String ||
                    !DateTimeOffset.TryParse(created.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var started))
                    return Unknown();
                if (observations.Count < 8) observations.Add($"PID {processId}, parent {parentId}, started {started.ToUniversalTime():O}");
            }
            bool hasMarker = marker.GetBoolean();
            int count = processes.GetArrayLength();
            if (count == 0 && !hasMarker)
                return new("Installer activity", CheckState.Information,
                    "No msiexec.exe process or Installer InProgress marker was observed at capture time. This single snapshot does not prove that no transaction exists. No process command line was collected and no recovery was selected.", Evidence: result);
            string detail = count == 0
                ? "An Installer InProgress marker exists, but no msiexec.exe process was observed. The marker may describe activity under another context or an interrupted transaction."
                : $"{count} msiexec.exe process{(count == 1 ? " was" : "es were")} observed{(hasMarker ? " with an Installer InProgress marker" : " without an observed Installer InProgress marker")}. This may represent installation, repair, registration work or a suspended client.";
            if (observations.Count > 0) detail += " Observed process references: " + string.Join("; ", observations) + ".";
            return new("Installer activity", CheckState.Attention,
                detail + " Wait for known deployment work to finish or review its journal before recovery. Process command lines were not collected. The marker and processes were not changed.", Evidence: result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException) { return Unknown(); }
    }

    private static bool Unique(JsonElement owner, string name, out JsonElement value)
    {
        var matches = owner.EnumerateObject().Where(property => property.NameEquals(name)).ToArray();
        value = matches.Length == 1 ? matches[0].Value : default;
        return matches.Length == 1;
    }
}
