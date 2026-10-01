using System.Text.Json;

namespace KillerBench.Engine.Storage;

public static class StorageSpacesDiagnostics
{
    private const string Scope = " Storage Spaces evidence does not replace backup verification, physical-disk testing or application I/O tests. No pool, virtual disk, physical disk, volume or setting is changed.";
    private const string Script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; $p=@(); $v=@(); $pe=$null; $ve=$null; " +
        "try { $p=@(Get-StoragePool -ErrorAction Stop | Where-Object { -not $_.IsPrimordial } | Sort-Object FriendlyName | Select-Object -First 33 " +
        "@{n='identity';e={[string]$_.UniqueId}},@{n='name';e={[string]$_.FriendlyName}},@{n='health';e={[string]$_.HealthStatus}},@{n='operation';e={[string]::Join(',',@($_.OperationalStatus))}},@{n='readOnly';e={[bool]$_.IsReadOnly}},@{n='size';e={[uint64]$_.Size}},@{n='allocated';e={[uint64]$_.AllocatedSize}}) } catch { $pe=$_.Exception.Message }; " +
        "try { $v=@(Get-VirtualDisk -ErrorAction Stop | Sort-Object FriendlyName | Select-Object -First 33 " +
        "@{n='identity';e={[string]$_.UniqueId}},@{n='name';e={[string]$_.FriendlyName}},@{n='health';e={[string]$_.HealthStatus}},@{n='operation';e={[string]::Join(',',@($_.OperationalStatus))}},@{n='resiliency';e={[string]$_.ResiliencySettingName}},@{n='size';e={[uint64]$_.Size}},@{n='footprint';e={[uint64]$_.FootprintOnPool}}) } catch { $ve=$_.Exception.Message }; " +
        "[pscustomobject]@{pools=$p;virtualDisks=$v;poolError=$pe;virtualDiskError=$ve} | ConvertTo-Json -Depth 4 -Compress";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Storage Spaces collection requires Windows.");
        try
        {
            var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true),
                TimeSpan.FromSeconds(35), token: token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            return Interpret(result);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
            NotSupportedException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            var evidence = new ToolResult(-1, "", error.Message, false);
            return UnknownSet("Storage Spaces collection was unavailable.", evidence);
        }
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult? result)
    {
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return UnknownSet("Storage Spaces collection failed or was incomplete.", result);
        try
        {
            using var document = JsonDocument.Parse(result.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 4 ||
                !root.TryGetProperty("pools", out var pools) || pools.ValueKind != JsonValueKind.Array ||
                !root.TryGetProperty("virtualDisks", out var virtualDisks) || virtualDisks.ValueKind != JsonValueKind.Array ||
                !OptionalError(root, "poolError", out string? poolError) ||
                !OptionalError(root, "virtualDiskError", out string? virtualDiskError))
                return UnknownSet("Storage Spaces data was malformed.", result);

            return Array.AsReadOnly<WorkflowCheck>(
            [
                InterpretItems("Storage pools", pools, poolError, result, pool: true),
                InterpretItems("Storage virtual disks", virtualDisks, virtualDiskError, result, pool: false)
            ]);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException)
        {
            return UnknownSet("Storage Spaces data could not be interpreted.", result);
        }
    }

    private static WorkflowCheck InterpretItems(string title, JsonElement items, string? collectionError, ToolResult result, bool pool)
    {
        if (collectionError is not null)
            return new(title, CheckState.Unknown, "The provider did not return this Storage Spaces inventory: " + collectionError + "." + Scope, Evidence: result);
        if (items.GetArrayLength() > 32)
            return new(title, CheckState.Unknown, "More than 32 items were returned, so coverage was capped." + Scope, Evidence: result);
        if (items.GetArrayLength() == 0)
            return new(title, CheckState.Information, pool ? "No non-primordial Storage Spaces pools were reported." + Scope : "No Storage Spaces virtual disks were reported." + Scope, Evidence: result);

        int attention = 0, unknown = 0;
        ulong totalSize = 0, totalSecondary = 0;
        var identities = new HashSet<string>(StringComparer.Ordinal);
        var affected = new List<string>();
        var resiliencyTypes = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in items.EnumerateArray())
        {
            ulong secondary = 0;
            string resiliency = "";
            bool specificFields = item.ValueKind == JsonValueKind.Object &&
                (pool ? PoolFields(item, out secondary) : VirtualDiskFields(item, out secondary, out resiliency));
            if (item.ValueKind != JsonValueKind.Object || item.EnumerateObject().Count() != 7 ||
                !Text(item, "identity", 512, out string identity) || !identities.Add(identity) ||
                !Text(item, "name", 256, out string name) || !Text(item, "health", 64, out string health) ||
                !Text(item, "operation", 256, out string operation) || !UInt64(item, "size", out ulong size) ||
                !specificFields)
            {
                unknown++;
                continue;
            }
            totalSize = totalSize > ulong.MaxValue - size ? ulong.MaxValue : totalSize + size;
            totalSecondary = totalSecondary > ulong.MaxValue - secondary ? ulong.MaxValue : totalSecondary + secondary;
            if (!pool) resiliencyTypes.Add(resiliency);
            bool indeterminate = health.Equals("Unknown", StringComparison.OrdinalIgnoreCase) || operation.Contains("Unknown", StringComparison.OrdinalIgnoreCase);
            bool unhealthy = !health.Equals("Healthy", StringComparison.OrdinalIgnoreCase) || !operation.Equals("OK", StringComparison.OrdinalIgnoreCase) ||
                pool && item.GetProperty("readOnly").GetBoolean();
            if (indeterminate) unknown++;
            else if (unhealthy)
            {
                attention++;
                if (affected.Count < 8) affected.Add(name + " (" + health + "; " + operation + (pool && item.GetProperty("readOnly").GetBoolean() ? "; read-only" : "") + ")");
            }
        }
        var state = attention > 0 ? CheckState.Attention : unknown > 0 ? CheckState.Unknown : CheckState.Clear;
        string detail = $"{items.GetArrayLength()} reported; {attention} need attention; {unknown} could not be classified; {FormatBytes(totalSize)} total reported size; " +
            (pool ? $"{FormatBytes(totalSecondary)} allocated." : $"{FormatBytes(totalSecondary)} pool footprint; resiliency {string.Join(", ", resiliencyTypes)}.");
        if (affected.Count > 0) detail += " Review: " + string.Join("; ", affected) + ".";
        return new(title, state, detail + Scope, Evidence: result);
    }

    private static bool PoolFields(JsonElement item, out ulong allocated)
    {
        allocated = 0;
        return item.TryGetProperty("readOnly", out var readOnly) && readOnly.ValueKind is JsonValueKind.True or JsonValueKind.False &&
            UInt64(item, "allocated", out allocated);
    }

    private static bool VirtualDiskFields(JsonElement item, out ulong footprint, out string resiliency)
    {
        footprint = 0;
        resiliency = "";
        return Text(item, "resiliency", 128, out resiliency) && UInt64(item, "footprint", out footprint);
    }

    private static bool Text(JsonElement item, string property, int maximum, out string value)
    {
        value = "";
        if (!item.TryGetProperty(property, out var element) || element.ValueKind != JsonValueKind.String ||
            !DriverReference.ValidText(element.GetString(), maximum) || element.GetString() != element.GetString()!.Trim()) return false;
        value = element.GetString()!;
        return true;
    }

    private static bool UInt64(JsonElement item, string property, out ulong value)
    {
        value = 0;
        return item.TryGetProperty(property, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetUInt64(out value);
    }

    private static bool OptionalError(JsonElement root, string property, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(property, out var element)) return false;
        if (element.ValueKind == JsonValueKind.Null) return true;
        if (element.ValueKind != JsonValueKind.String || !DriverReference.ValidText(element.GetString(), 2048)) return false;
        value = element.GetString();
        return true;
    }

    private static string FormatBytes(ulong bytes) => bytes >= 1UL << 40
        ? $"{bytes / (double)(1UL << 40):F1} TiB"
        : $"{bytes / (double)(1UL << 30):F1} GiB";

    private static System.Collections.ObjectModel.ReadOnlyCollection<WorkflowCheck> UnknownSet(string detail, ToolResult? evidence) =>
        Array.AsReadOnly<WorkflowCheck>(
        [
            new WorkflowCheck("Storage pools", CheckState.Unknown, detail + Scope, Evidence: evidence),
            new WorkflowCheck("Storage virtual disks", CheckState.Unknown, detail + Scope, Evidence: evidence)
        ]);
}
