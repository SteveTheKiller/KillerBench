using System.Text.Json;

namespace KillerBench.Engine.Printing;

public static class PrinterDriverDiagnostics
{
    private const string Script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; " +
        "$queues=@(Get-Printer | Select-Object -First 257 Name,DriverName,PortName); $drivers=$null; $driverError=$null; " +
        "try { $drivers=@(Get-PrinterDriver | Select-Object -First 513 Name,PrinterEnvironment) } catch { $driverError=$_.Exception.Message } " +
        "[pscustomobject]@{queues=$queues;drivers=$drivers;driverError=$driverError} | ConvertTo-Json -Depth 4 -Compress";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Print driver inventory requires Windows.");
        return Interpret(await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false));
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult result)
    {
        WorkflowCheck Unknown(string detail) => new("Print queue driver inventory", CheckState.Unknown, detail, Evidence: result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return [Unknown("Queue/driver collection failed or was incomplete. No driver or queue changes selected.")];
        try
        {
            using var json = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = json.RootElement;
            if (!Unique(root) || !root.TryGetProperty("queues", out var queues) || queues.ValueKind != JsonValueKind.Array)
                return [Unknown("Queue inventory was missing or ambiguous.")];
            bool driverComplete = root.TryGetProperty("drivers", out var drivers) && drivers.ValueKind == JsonValueKind.Array && drivers.GetArrayLength() <= 512;
            string errorDetail = "";
            if (root.TryGetProperty("driverError", out var driverError) && driverError.ValueKind != JsonValueKind.Null)
            {
                driverComplete = false;
                if (driverError.ValueKind == JsonValueKind.String)
                    errorDetail = " Reported error: " + new string([.. (driverError.GetString() ?? "").Take(512).Select(c => char.IsControl(c) ? ' ' : c)]);
            }
            var known = new List<(string Name, string Environment)>();
            if (driverComplete)
                foreach (var driver in drivers.EnumerateArray())
                {
                    if (!Unique(driver) || Text(driver, "Name") is not { } name || Text(driver, "PrinterEnvironment") is not { } environment)
                    { driverComplete = false; continue; }
                    known.Add((name, environment));
                }
            var checks = new List<WorkflowCheck>();
            if (queues.GetArrayLength() > 256)
                checks.Add(Unknown("Partial queue coverage: only the first 256 queues were evaluated. Additional queues remain unassessed."));
            if (!driverComplete) checks.Add(Unknown("Driver inventory was unavailable or incomplete. Queue-reported driver names are retained, but registration matches remain unverified." + errorDetail));
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var queue in queues.EnumerateArray().Take(256))
            {
                if (!Unique(queue) || Text(queue, "Name") is not { } name || !seen.Add(name) ||
                    Text(queue, "DriverName") is not { } driver || Text(queue, "PortName") is not { } port)
                { checks.Add(Unknown("An incomplete or duplicate queue could not be matched.")); continue; }
                var matches = known.Where(d => string.Equals(d.Name, driver, StringComparison.OrdinalIgnoreCase)).ToArray();
                string match = driverComplete && matches.Length == 1 ? "One registered name match, environment: " + matches[0].Environment + "." :
                    "A unique local driver registration could not be confirmed. Remote queues and multiple driver environments require separate review.";
                if (matches.Length > 1)
                    match += " " + matches.Length + " matching registrations. Reported environments: " +
                        string.Join(", ", matches.Select(m => m.Environment).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).Take(8)) + ". No environment was selected.";
                checks.Add(new("Print queue driver: " + name, driverComplete && matches.Length == 1 ? CheckState.Information : CheckState.Unknown,
                    "Queue-reported driver: " + driver + ". Port: " + port + ". " + match +
                    " This is queue configuration inventory, not the PnP driver, package trust, update availability or physical-print verification. No settings changed.", Evidence: result));
            }
            if (queues.GetArrayLength() == 0) checks.Add(new("Print queues", CheckState.Information, "No queues were reported for this process account. Other users and print servers were not enumerated.", Evidence: result));
            return checks.AsReadOnly();
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        { return [Unknown("Queue/driver evidence could not be interpreted. No changes selected.")]; }
    }

    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
    private static string? Text(JsonElement value, string property) => value.TryGetProperty(property, out var text) &&
        text.ValueKind == JsonValueKind.String && DriverReference.ValidText(text.GetString(), 512) ? text.GetString() : null;
}
