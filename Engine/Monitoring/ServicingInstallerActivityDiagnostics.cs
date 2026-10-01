using System.Text.Json;

namespace KillerBench.Engine.Monitoring;

public sealed record ServicingInstallerActivitySnapshot(int ProcessCount, bool InProgressMarker);

public static class ServicingInstallerActivityDiagnostics
{
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$rows=@(Get-CimInstance -ClassName Win32_Process -Filter \"Name='msiexec.exe'\" -OperationTimeoutSec 15|Select-Object -First 129);" +
        "$marker=Test-Path -LiteralPath 'Registry::HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Installer\\InProgress';" +
        "[pscustomobject]@{complete=($rows.Count-le 128);processCount=[int]([Math]::Min($rows.Count,128));marker=[bool]$marker}|ConvertTo-Json -Compress";

    public static RepairStep QueryStep() => new(PrintRecovery.Tool,
        ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default) =>
        Interpret(await BoundedTool.RunAsync(QueryStep(), TimeSpan.FromSeconds(25), token: token).ConfigureAwait(false));

    public static WorkflowCheck Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        WorkflowCheck Unknown(string detail) => new("Installer activity summary", CheckState.Unknown,
            detail + " No Installer process or transaction was changed.", Evidence: result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return Unknown("The focused Windows Installer activity snapshot is unavailable or incomplete.");
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 3 ||
                !root.TryGetProperty("complete", out var complete) || complete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) || !complete.GetBoolean() ||
                !root.TryGetProperty("processCount", out var countValue) || !countValue.TryGetInt32(out int count) || count is < 0 or > 128 ||
                !root.TryGetProperty("marker", out var markerValue) || markerValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
                return Unknown("Windows returned malformed or out-of-range Installer activity evidence.");
            return Assess(new(count, markerValue.GetBoolean()), result);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or FormatException)
        {
            return Unknown("The focused Windows Installer activity snapshot could not be interpreted (" + error.GetType().Name + ").");
        }
    }

    public static WorkflowCheck Assess(ServicingInstallerActivitySnapshot snapshot, ToolResult? evidence = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.ProcessCount is < 0 or > 128)
            return new("Installer activity summary", CheckState.Unknown,
                "Installer activity evidence is outside its bounds. No Installer process or transaction was changed.", Evidence: evidence);
        string detail = snapshot.ProcessCount == 0 && !snapshot.InProgressMarker
            ? "No msiexec process or Installer in-progress marker was observed in this point-in-time snapshot. This does not prove that no transaction exists."
            : $"Observed {snapshot.ProcessCount} msiexec process{(snapshot.ProcessCount == 1 ? "" : "es")}; Installer in-progress marker: {(snapshot.InProgressMarker ? "present" : "absent")}. Wait for known deployment work to finish before selecting a conflicting repair.";
        detail += " Process IDs, parent identities, command lines, users and paths are excluded. No Installer process or transaction was changed.";
        return new("Installer activity summary",
            snapshot.ProcessCount > 0 || snapshot.InProgressMarker ? CheckState.Attention : CheckState.Information,
            detail, Evidence: evidence);
    }
}
