using System.Text.Json;

namespace KillerBench.Engine;

public static class PrinterConditionDiagnostics
{
    private const int MaximumQueues = 256;
    private const string Scope = " These are driver-reported queue observations. They do not verify paper, toner, mechanics, physical output or current device reachability. No queue, job, port, driver or printer setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-CimInstance Win32_Printer -OperationTimeoutSec 20 -ErrorAction Stop|Where-Object{$_.Local-eq $true}|Sort-Object Name|Select-Object -First 257);" +
        "$rows=@($all|Select-Object -First 256|ForEach-Object{[pscustomobject]@{name=[string]$_.Name;printerStatus=[int]$_.PrinterStatus;detectedErrorState=[int]$_.DetectedErrorState;workOffline=[bool]$_.WorkOffline}});" +
        "[pscustomobject]@{complete=($all.Count-le 256);queues=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Printer condition inspection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(35));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var evidence = await (execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Printer-condition provider returned no task.")).WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Printer-condition provider returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        { return [Unknown("Printer-condition inspection exceeded its overall deadline.", new(-1, "", "Printer-condition deadline expired.", false) { TimedOut = true })]; }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        { return [Unknown("Printer-condition inspection was unavailable.", new(-1, "", error.Message, false))]; }
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 1_000_000)
            return [Unknown("Printer-condition collection failed or returned incomplete evidence.", evidence)];
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) || !complete ||
                !root.TryGetProperty("queues", out var queues) || queues.ValueKind != JsonValueKind.Array || queues.GetArrayLength() > MaximumQueues)
                return [Unknown("Printer-condition evidence was malformed, incomplete or exceeded its queue bound.", evidence)];
            if (queues.GetArrayLength() == 0)
                return [new("Local printer conditions", CheckState.Information, "No local printer queue returned a condition record." + Scope, Evidence: evidence)];
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var results = new List<WorkflowCheck>();
            foreach (var row in queues.EnumerateArray())
            {
                if (!Unique(row, 4) || !Text(row, "name", 512, out string name) || !names.Add(name) ||
                    !Integer(row, "printerStatus", 0, 7, out int printerStatus) ||
                    !Integer(row, "detectedErrorState", 0, 12, out int errorState) || !Boolean(row, "workOffline", out bool workOffline))
                    return [Unknown("Printer-condition evidence contained an invalid or duplicate queue.", evidence)];
                string status = printerStatus switch { 3 => "Idle", 4 => "Printing", 5 => "Warmup", 6 => "Stopped printing", 7 => "Offline", 1 => "Other", 2 => "Unknown", _ => "Not reported" };
                string error = errorState switch { 2 => "No error", 3 => "Low paper", 4 => "No paper", 5 => "Low toner", 6 => "No toner", 7 => "Door open", 8 => "Jammed", 9 => "Offline", 10 => "Service requested", 11 => "Output bin full", 12 => "Page punt", _ => "Not reported" };
                bool attention = workOffline || printerStatus is 6 or 7 || errorState is >= 3 and <= 12;
                results.Add(new("Printer condition: " + name, attention ? CheckState.Attention : CheckState.Information,
                    $"Printer status: {status}. Detected error state: {error}. WorkOffline: {(workOffline ? "Yes" : "No")}." +
                    (attention ? " Review this exact queue and its device evidence before selecting recovery." : " No adverse condition code was reported in this snapshot.") + Scope,
                    Evidence: evidence));
            }
            return results.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        { return [Unknown("Printer-condition evidence could not be interpreted.", evidence)]; }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new("Local printer conditions", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value) { value = false; if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false; value = item.GetBoolean(); return true; }
    private static bool Integer(JsonElement owner, string name, int minimum, int maximum, out int value) { value = 0; return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetInt32(out value) && value >= minimum && value <= maximum; }
    private static bool Text(JsonElement owner, string name, int maximum, out string value) { value = ""; if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text || text.Length is < 1 || text.Length > maximum || text != text.Trim() || text.Any(char.IsControl)) return false; value = text; return true; }
}
