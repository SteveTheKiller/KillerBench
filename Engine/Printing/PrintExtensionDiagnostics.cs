using System.Text.Json;

namespace KillerBench.Engine.Printing;

public static class PrintExtensionDiagnostics
{
    private const int MaximumQueues = 256;
    private const int MaximumProcessors = 256;
    private const int MaximumMonitors = 128;

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$queues=@(Get-Printer -ErrorAction Stop|Select-Object -First 257 Name,Type,PrintProcessor);" +
            "$processors=[Collections.Generic.List[object]]::new();" +
            "$environmentRoot='Registry::HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Print\\Environments';" +
            "foreach($environment in @(Get-ChildItem -LiteralPath $environmentRoot -ErrorAction Stop)){" +
            "$processorRoot=Join-Path $environment.PSPath 'Print Processors';if(Test-Path -LiteralPath $processorRoot){" +
            "foreach($processor in @(Get-ChildItem -LiteralPath $processorRoot -ErrorAction Stop)){if($processors.Count-ge 257){break};" +
            "$driver=[string](Get-ItemProperty -LiteralPath $processor.PSPath -Name Driver -ErrorAction Stop).Driver;" +
            "$processors.Add([pscustomobject]@{name=[string]$processor.PSChildName;environment=[string]$environment.PSChildName;driver=[IO.Path]::GetFileName($driver)})}}};" +
            "$monitors=[Collections.Generic.List[object]]::new();$monitorRoot='Registry::HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Print\\Monitors';" +
            "foreach($monitor in @(Get-ChildItem -LiteralPath $monitorRoot -ErrorAction Stop)){if($monitors.Count-ge 129){break};" +
            "$driver=[string](Get-ItemProperty -LiteralPath $monitor.PSPath -Name Driver -ErrorAction Stop).Driver;" +
            "$monitors.Add([pscustomobject]@{name=[string]$monitor.PSChildName;driver=[IO.Path]::GetFileName($driver)})};" +
            "[pscustomobject]@{complete=($queues.Count-le 256-and$processors.Count-le 256-and$monitors.Count-le 128);queues=$queues;processors=$processors;monitors=$monitors}|ConvertTo-Json -Depth 5 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        const string title = "Print processors and monitors";
        const string scope = " No print extension was loaded, and no queue, processor, monitor, port, driver or registry setting changed.";
        WorkflowCheck Unknown(string detail = "Print processor or monitor registration evidence is unavailable or incomplete.") =>
            new(title, CheckState.Unknown, detail + scope, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 6 });
            var root = document.RootElement;
            if (!Unique(root) || !root.TryGetProperty("complete", out var completeValue) || completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !root.TryGetProperty("queues", out var queuesValue) || queuesValue.ValueKind != JsonValueKind.Array ||
                !root.TryGetProperty("processors", out var processorsValue) || processorsValue.ValueKind != JsonValueKind.Array ||
                !root.TryGetProperty("monitors", out var monitorsValue) || monitorsValue.ValueKind != JsonValueKind.Array) return Unknown();
            if (!completeValue.GetBoolean() || queuesValue.GetArrayLength() > MaximumQueues || processorsValue.GetArrayLength() > MaximumProcessors || monitorsValue.GetArrayLength() > MaximumMonitors)
                return Unknown("Print extension inventory exceeded its evidence bound.");

            var processors = new List<(string Name, string Environment, string Driver)>();
            var processorKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var processor in processorsValue.EnumerateArray())
            {
                if (!Unique(processor) || Text(processor, "name") is not { } name || Text(processor, "environment") is not { } environment ||
                    FileName(processor, "driver") is not { } driver || !processorKeys.Add(environment + "\n" + name)) return Unknown();
                processors.Add((name, environment, driver));
            }
            var monitorKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var monitor in monitorsValue.EnumerateArray())
                if (!Unique(monitor) || Text(monitor, "name") is not { } name || FileName(monitor, "driver") is null || !monitorKeys.Add(name)) return Unknown();

            var queues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var missingLocal = new List<string>();
            int remoteUnverified = 0;
            foreach (var queue in queuesValue.EnumerateArray())
            {
                if (!Unique(queue) || Text(queue, "Name") is not { } name || !queues.Add(name) || Text(queue, "PrintProcessor") is not { } processor ||
                    !queue.TryGetProperty("Type", out var typeValue) || !typeValue.TryGetInt32(out int type) || type is not (0 or 1)) return Unknown();
                bool registered = processors.Any(item => string.Equals(item.Name, processor, StringComparison.OrdinalIgnoreCase));
                if (!registered && type == 0) missingLocal.Add(name + " -> " + processor);
                else if (!registered) remoteUnverified++;
            }
            string detail = $"Reviewed {queues.Count} queue{(queues.Count == 1 ? "" : "s")}, {processors.Count} print processor registration{(processors.Count == 1 ? "" : "s")} and {monitorsValue.GetArrayLength()} print monitor registration{(monitorsValue.GetArrayLength() == 1 ? "" : "s")}.";
            if (missingLocal.Count > 0)
                detail += " Local queues without a matching processor name: " + string.Join("; ", missingLocal.Take(20)) + (missingLocal.Count > 20 ? "; additional queues remain in evidence." : ".");
            if (remoteUnverified > 0) detail += $" {remoteUnverified} remote queue processor reference{(remoteUnverified == 1 ? " was" : "s were")} not verified against the remote server.";
            detail += " Registration does not establish DLL trust, binary integrity, architecture compatibility, successful rendering or physical printing." + scope;
            return new(title, missingLocal.Count > 0 ? CheckState.Attention : CheckState.Information, detail, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException) { return Unknown(); }
    }

    private static bool Unique(JsonElement value) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == value.EnumerateObject().Count();
    private static string? Text(JsonElement value, string key) => value.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.String &&
        DriverReference.ValidText(item.GetString(), 512) && item.GetString() == item.GetString()!.Trim() ? item.GetString() : null;
    private static string? FileName(JsonElement value, string key) => Text(value, key) is { } name && name.Length <= 260 &&
        !name.Contains('/') && !name.Contains('\\') && name is not "." and not ".." ? name : null;
}
