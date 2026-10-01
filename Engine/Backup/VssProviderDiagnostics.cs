using System.Text.Json;

namespace KillerBench.Engine.Backup;

public static class VssProviderDiagnostics
{
    private const int MaximumProviders = 64;
    private const string Name = "VSS provider registrations";
    private const string Boundary = " Provider registration does not prove that snapshot creation, backup or restore will succeed. Provider IDs and COM class IDs are excluded from the result. No provider, service, snapshot, backup data, registration or setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-CimInstance -ClassName Win32_ShadowProvider -ErrorAction Stop|Select-Object -First 65);" +
        "$items=@($all|ForEach-Object{[pscustomobject]@{id=[string]$_.ID;name=[string]$_.Name;type=[uint32]$_.Type;version=[string]$_.Version;versionId=[string]$_.VersionID}});" +
        "[pscustomobject]@{complete=($all.Count-le 64);items=$items}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(CancellationToken token = default,
        Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("VSS provider collection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(40));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(30), token: cancellationToken);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("VSS provider collector returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("VSS provider collector returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Collection exceeded its overall deadline.",
                new(-1, "", "VSS provider collection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Collection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) || !complete ||
                !root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array ||
                items.GetArrayLength() > MaximumProviders)
                return Unknown("Evidence was malformed or exceeded its coverage limit.", evidence);
            if (items.GetArrayLength() == 0)
                return new(Name, CheckState.Attention,
                    "No VSS provider was reported. Windows normally supplies a system provider; confirm the VSS installation and provider registrations before attempting registration changes." + Boundary,
                    Evidence: evidence);

            int system = 0, software = 0, hardware = 0, fileShare = 0, unknownType = 0;
            var identities = new HashSet<Guid>();
            var summaries = new List<string>();
            foreach (var item in items.EnumerateArray())
            {
                if (!Unique(item, 5) || !GuidText(item, "id", out Guid id) || !Text(item, "name", 256, out string providerName) ||
                    !UInt32(item, "type", out uint type) || !Text(item, "version", 128, out string version) ||
                    !GuidText(item, "versionId", out _) || !identities.Add(id))
                    return Unknown("One or more provider registrations were incomplete, duplicated or malformed.", evidence);
                string typeName = type switch
                {
                    1 => Count(ref system, "System"),
                    2 => Count(ref software, "Software"),
                    3 => Count(ref hardware, "Hardware"),
                    4 => Count(ref fileShare, "File share"),
                    _ => Count(ref unknownType, "Unknown type " + type)
                };
                if (summaries.Count < 8) summaries.Add(providerName + " " + version + " (" + typeName + ")");
            }
            CheckState state = unknownType > 0 || system == 0 ? CheckState.Attention : CheckState.Information;
            string detail = $"{items.GetArrayLength()} provider registration{(items.GetArrayLength() == 1 ? "" : "s")} reported: {system} system, {software} software, {hardware} hardware, {fileShare} file share and {unknownType} unknown type.";
            if (summaries.Count > 0) detail += " Registered: " + string.Join("; ", summaries) + ".";
            if (items.GetArrayLength() > summaries.Count) detail += " Additional registrations remain in raw evidence.";
            if (system == 0) detail += " The Windows system provider was not reported; review provider registration and VSS event evidence.";
            if (unknownType > 0) detail += " Unknown provider types require review before relying on them.";
            return new(Name, state, detail + Boundary, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException or FormatException)
        {
            return Unknown("Evidence could not be interpreted.", evidence);
        }
    }

    private static string Count(ref int value, string label) { value++; return label; }
    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new(Name, CheckState.Unknown, detail + Boundary, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object &&
        value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value)
    {
        value = false;
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false;
        value = item.GetBoolean();
        return true;
    }
    private static bool UInt32(JsonElement owner, string name, out uint value)
    {
        value = 0;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetUInt32(out value);
    }
    private static bool GuidText(JsonElement owner, string name, out Guid value)
    {
        value = Guid.Empty;
        return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String &&
            Guid.TryParse(item.GetString(), out value) && value != Guid.Empty;
    }
    private static bool Text(JsonElement owner, string name, int maximum, out string value)
    {
        value = "";
        if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text ||
            text.Length is < 1 || text.Length > maximum || text != text.Trim() || text.Any(char.IsControl)) return false;
        value = text;
        return true;
    }
}
