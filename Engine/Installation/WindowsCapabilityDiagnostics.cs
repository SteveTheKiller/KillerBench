using System.Text.Json;

namespace KillerBench.Engine.Installation;

public static class WindowsCapabilityDiagnostics
{
    private const int MaximumCapabilities = 4096;
    private static readonly HashSet<string> States = new(StringComparer.Ordinal)
    {
        "NotPresent", "UninstallPending", "Staged", "Removed", "Installed", "InstallPending", "Superseded", "PartiallyInstalled"
    };
    private const string Scope = " Capability state is configuration evidence, not proof of a fault or authorization to install, remove, stage or restore a capability. No capability, payload, source, package, policy or servicing setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';$rows=[Collections.Generic.List[object]]::new();" +
        "foreach($capability in @(Get-WindowsCapability -Online -ErrorAction Stop|Sort-Object Name)){if($rows.Count-ge 4097){break};" +
        "$rows.Add([pscustomobject]@{name=[string]$capability.Name;state=[string]$capability.State})};" +
        "[pscustomobject]@{complete=($rows.Count-le 4096);capabilities=$rows}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Windows capability inspection requires Windows.");
        execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromMinutes(2), token: cancellationToken);
        var evidence = await (execute(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), token)
            ?? throw new InvalidOperationException("Windows capability provider returned no task.")).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Windows capability provider returned no evidence.");
        token.ThrowIfCancellationRequested();
        return Interpret(evidence);
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 2_000_000)
            return Unknown("Windows capability state is unavailable or incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 4 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !root.TryGetProperty("complete", out var complete) || complete.ValueKind is not (JsonValueKind.True or JsonValueKind.False) ||
                !complete.GetBoolean() || !root.TryGetProperty("capabilities", out var values) || values.ValueKind != JsonValueKind.Array || values.GetArrayLength() > MaximumCapabilities)
                return Unknown("Windows capability evidence was malformed, incomplete or exceeded its inventory bound.", evidence);

            var capabilities = new List<(string Name, string State)>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values.EnumerateArray())
            {
                if (!Unique(value, 2) || !Text(value, "name", 512, out string name) || !Text(value, "state", 64, out string state) ||
                    !States.Contains(state) || !seen.Add(name))
                    return Unknown("Windows capability evidence contained an invalid or duplicate record.", evidence);
                capabilities.Add((name, state));
            }

            var pending = capabilities.Where(capability => capability.State is "InstallPending" or "UninstallPending" or "PartiallyInstalled").ToArray();
            string counts = string.Join(", ", capabilities.GroupBy(capability => capability.State, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal).Select(group => group.Key + ": " + group.Count()));
            string detail = $"Reviewed {capabilities.Count} Windows capabilit{(capabilities.Count == 1 ? "y" : "ies")}. " +
                (counts.Length == 0 ? "No capabilities were returned." : counts + ".");
            if (pending.Length > 0)
                detail += " Pending or partial capabilities: " + string.Join("; ", pending.Take(20).Select(capability => capability.Name + " (" + capability.State + ")")) +
                    (pending.Length > 20 ? "; additional capabilities remain in evidence." : ".");
            return new("Windows capabilities", pending.Length > 0 ? CheckState.Attention : CheckState.Information, detail + Scope, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException)
        { return Unknown("Windows capability evidence could not be interpreted.", evidence); }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new("Windows capabilities", CheckState.Unknown, detail + Scope, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Text(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            !DriverReference.ValidText(text, maximum) || text != text.Trim()) return false;
        value = text;
        return true;
    }
}
