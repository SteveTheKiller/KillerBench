using System.Text.Json;

namespace KillerBench.Engine;

public sealed record ServiceInventoryEntry(string Name, string DisplayName, string State, string StartMode, bool DelayedAutoStart, string ServiceType);
public sealed record ServiceInventorySnapshot(bool Complete, IReadOnlyList<ServiceInventoryEntry> Services);

public static class ServiceInventoryDiagnostics
{
    public const int MaximumServices = 1024;
    private static readonly HashSet<string> States = new(["Stopped", "Start Pending", "Stop Pending", "Running", "Continue Pending", "Pause Pending", "Paused", "Unknown"], StringComparer.Ordinal);
    private static readonly HashSet<string> StartModes = new(["Auto", "Manual", "Disabled", "Boot", "System", "Unknown"], StringComparer.Ordinal);

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Service inventory requires Windows.");
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$items=@(Get-CimInstance Win32_Service -ErrorAction Stop|Sort-Object Name|Select-Object -First 1025|ForEach-Object{[pscustomobject]@{" +
            "name=[string]$_.Name;displayName=[string]$_.DisplayName;state=[string]$_.State;startMode=[string]$_.StartMode;" +
            "delayedAutoStart=[bool]$_.DelayedAutoStart;serviceType=[string]$_.ServiceType}});" +
            "[pscustomobject]@{complete=($items.Count-le 1024);services=$items}|ConvertTo-Json -Depth 4 -Compress";
        var evidence = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false);
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        const string name = "Installed service inventory";
        const string scope = " Executable paths, service accounts, process IDs, descriptions and dependencies are excluded. No service, startup setting, account, permission, executable or recovery configuration changed.";
        WorkflowCheck Unknown(string detail = "Installed service inventory is unavailable or incomplete.") =>
            new(name, CheckState.Unknown, detail + scope, Evidence: evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return Unknown();
        try
        {
            var snapshot = Parse(evidence.Output);
            if (!snapshot.Complete) return Unknown("Installed service inventory exceeded its 1,024-service evidence bound.");
            int running = snapshot.Services.Count(service => service.State == "Running");
            int stopped = snapshot.Services.Count(service => service.State == "Stopped");
            int disabled = snapshot.Services.Count(service => service.StartMode == "Disabled");
            int transitional = snapshot.Services.Count - running - stopped;
            string detail = $"Reviewed {snapshot.Services.Count} installed services: {running} running, {stopped} stopped, {transitional} transitional or unknown, and {disabled} disabled. Stopped manual and demand-start services can be normal. Inventory alone does not establish feature health or repair eligibility.";
            return new(name, CheckState.Information, detail + scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return Unknown();
        }
    }

    public static ServiceInventorySnapshot Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 6 });
        var root = document.RootElement;
        if (!Unique(root, 2) || !root.TryGetProperty("complete", out var completeValue) || completeValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
            !root.TryGetProperty("services", out var servicesValue) || servicesValue.ValueKind != JsonValueKind.Array || servicesValue.GetArrayLength() > MaximumServices)
            throw new InvalidDataException("Service inventory root is invalid.");
        bool complete = completeValue.GetBoolean();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var services = new List<ServiceInventoryEntry>(servicesValue.GetArrayLength());
        foreach (var item in servicesValue.EnumerateArray())
        {
            if (!Unique(item, 6) || Text(item, "name", 256) is not { } name || !ValidServiceName(name) || !names.Add(name) ||
                Text(item, "displayName", 512) is not { } displayName || Text(item, "state", 32) is not { } state || !States.Contains(state) ||
                Text(item, "startMode", 16) is not { } startMode || !StartModes.Contains(startMode) ||
                !item.TryGetProperty("delayedAutoStart", out var delayedValue) || delayedValue.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                Text(item, "serviceType", 64) is not { } serviceType)
                throw new InvalidDataException("Service inventory entry is invalid.");
            services.Add(new(name, displayName, state, startMode, delayedValue.GetBoolean(), serviceType));
        }
        return new(complete, services.AsReadOnly());
    }

    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == expected &&
        value.EnumerateObject().Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static string? Text(JsonElement owner, string name, int maximum) => owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String &&
        item.GetString() is { } text && text == text.Trim() && DriverReference.ValidText(text, maximum) ? text : null;
    private static bool ValidServiceName(string name) => !name.Any(character => char.IsControl(character) || character is '\\' or '/');
}
