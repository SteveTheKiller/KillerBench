using System.Globalization;
using System.Text.Json;

namespace KillerBench.Engine.Backup;

public static class ShadowCopyDiagnostics
{
    private const int MaximumCopies = 512;
    private const string Name = "VSS shadow copies";
    private const string Boundary = " Snapshot, set, provider, volume, device, machine and exposed-path identities are excluded. Inventory does not prove backup currency, application consistency, destination integrity or restore success. No shadow copy, backup data, provider, volume or setting changed.";
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "$all=@(Get-CimInstance -ClassName Win32_ShadowCopy -ErrorAction Stop|Select-Object -First 513);" +
        "$items=@($all|ForEach-Object{$created=$null;if($null-ne$_.InstallDate){$created=([DateTimeOffset]([datetime]$_.InstallDate)).ToUniversalTime().ToString('o')};" +
        "[pscustomobject]@{id=[string]$_.ID;createdUtc=$created;state=[uint32]$_.State;status=[string]$_.Status;persistent=[bool]$_.Persistent;clientAccessible=[bool]$_.ClientAccessible;noAutoRelease=[bool]$_.NoAutoRelease;noWriters=[bool]$_.NoWriters;hardwareAssisted=[bool]$_.HardwareAssisted;differential=[bool]$_.Differential;imported=[bool]$_.Imported;exposedLocally=[bool]$_.ExposedLocally;exposedRemotely=[bool]$_.ExposedRemotely}});" +
        "[pscustomobject]@{complete=($all.Count-le 512);items=$items}|ConvertTo-Json -Depth 4 -Compress";

    public static async Task<WorkflowCheck> CheckAsync(Func<RepairStep, CancellationToken, Task<ToolResult>>? execute = null,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (execute is null && !OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Shadow copy collection requires Windows.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        try
        {
            execute ??= (step, cancellationToken) => BoundedTool.RunAsync(step, TimeSpan.FromSeconds(35), token: cancellationToken);
            var pending = execute(new(PrintRecovery.Tool,
                ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true), deadline.Token)
                ?? throw new InvalidOperationException("Shadow copy collector returned no task.");
            var evidence = await pending.WaitAsync(deadline.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Shadow copy collector returned no evidence.");
            token.ThrowIfCancellationRequested();
            return Interpret(evidence, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested && deadline.IsCancellationRequested)
        {
            return Unknown("Collection exceeded its overall deadline.", new(-1, "", "Shadow copy collection deadline expired.", false) { TimedOut = true });
        }
        catch (Exception error) when (error is IOException or InvalidOperationException or NotSupportedException or
            UnauthorizedAccessException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return Unknown("Collection was unavailable.", new(-1, "", error.Message, false));
        }
    }

    public static WorkflowCheck Interpret(ToolResult evidence, DateTimeOffset? observedUtc = null)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error))
            return Unknown("Collection failed or was incomplete.", evidence);
        try
        {
            using var document = JsonDocument.Parse(evidence.Output, new JsonDocumentOptions { MaxDepth = 5 });
            var root = document.RootElement;
            if (!Unique(root, 2) || !Boolean(root, "complete", out bool complete) || !complete ||
                !root.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() > MaximumCopies)
                return Unknown("Evidence was malformed or exceeded its coverage limit.", evidence);
            if (items.GetArrayLength() == 0)
                return new(Name, CheckState.Information, "No current shadow copy was reported. This does not establish whether backup or restore protection is configured or required." + Boundary, Evidence: evidence);

            DateTimeOffset now = observedUtc ?? DateTimeOffset.UtcNow;
            int created = 0, transitioning = 0, aborted = 0, deleted = 0, adverseStatus = 0;
            int persistent = 0, clientAccessible = 0, hardware = 0, imported = 0, exposed = 0;
            DateTimeOffset? oldest = null, newest = null;
            var identities = new HashSet<Guid>();
            foreach (var item in items.EnumerateArray())
            {
                if (!Unique(item, 13) || !GuidText(item, "id", out Guid id) || !identities.Add(id) ||
                    !Time(item, "createdUtc", out DateTimeOffset createdUtc) || createdUtc > now.AddMinutes(5) ||
                    !UInt32(item, "state", out uint state) || state > 11 || !Text(item, "status", out string status) ||
                    !Boolean(item, "persistent", out bool isPersistent) || !Boolean(item, "clientAccessible", out bool isClientAccessible) ||
                    !Boolean(item, "noAutoRelease", out _) || !Boolean(item, "noWriters", out _) ||
                    !Boolean(item, "hardwareAssisted", out bool isHardware) || !Boolean(item, "differential", out _) ||
                    !Boolean(item, "imported", out bool isImported) || !Boolean(item, "exposedLocally", out bool local) ||
                    !Boolean(item, "exposedRemotely", out bool remote))
                    return Unknown("One or more shadow copy records were incomplete, duplicated or malformed.", evidence);
                oldest = oldest is null || createdUtc < oldest ? createdUtc : oldest;
                newest = newest is null || createdUtc > newest ? createdUtc : newest;
                if (state == 9) created++; else if (state is >= 1 and <= 8) transitioning++; else if (state == 10) aborted++; else if (state == 11) deleted++;
                else return Unknown("A shadow copy reported an unknown state.", evidence);
                if (!status.Equals("OK", StringComparison.OrdinalIgnoreCase)) adverseStatus++;
                if (isPersistent) persistent++;
                if (isClientAccessible) clientAccessible++;
                if (isHardware) hardware++;
                if (isImported) imported++;
                if (local || remote) exposed++;
            }
            CheckState result = aborted > 0 || adverseStatus > 0 ? CheckState.Attention : CheckState.Information;
            string detail = $"{items.GetArrayLength()} current record{(items.GetArrayLength() == 1 ? "" : "s")}: {created} created, {transitioning} transitioning, {aborted} aborted and {deleted} deleted; {persistent} persistent, {clientAccessible} client-accessible, {hardware} hardware-assisted, {imported} imported and {exposed} exposed.";
            detail += " Reported range: " + oldest!.Value.ToString("u", CultureInfo.InvariantCulture) + " through " + newest!.Value.ToString("u", CultureInfo.InvariantCulture) + ".";
            if (adverseStatus > 0) detail += " " + adverseStatus + " record(s) reported a status other than OK.";
            if (transitioning > 0) detail += " Transitional state can be normal during active VSS work and requires later correlation rather than immediate repair.";
            if (result == CheckState.Attention) detail += " Review VSS and backup event evidence before deleting or recreating anything.";
            return new(Name, result, detail + Boundary, Evidence: evidence);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or OverflowException or FormatException)
        {
            return Unknown("Evidence could not be interpreted.", evidence);
        }
    }

    private static WorkflowCheck Unknown(string detail, ToolResult evidence) => new(Name, CheckState.Unknown, detail + Boundary, Evidence: evidence);
    private static bool Unique(JsonElement value, int expected) => value.ValueKind == JsonValueKind.Object && value.EnumerateObject().Count() == expected && value.EnumerateObject().Select(item => item.Name).Distinct(StringComparer.Ordinal).Count() == expected;
    private static bool Boolean(JsonElement owner, string name, out bool value) { value = false; if (!owner.TryGetProperty(name, out var item) || item.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) return false; value = item.GetBoolean(); return true; }
    private static bool UInt32(JsonElement owner, string name, out uint value) { value = 0; return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.Number && item.TryGetUInt32(out value); }
    private static bool GuidText(JsonElement owner, string name, out Guid value) { value = Guid.Empty; return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out value) && value != Guid.Empty; }
    private static bool Time(JsonElement owner, string name, out DateTimeOffset value) { value = default; return owner.TryGetProperty(name, out var item) && item.ValueKind == JsonValueKind.String && DateTimeOffset.TryParseExact(item.GetString(), "o", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value); }
    private static bool Text(JsonElement owner, string name, out string value) { value = ""; if (!owner.TryGetProperty(name, out var item) || item.ValueKind != JsonValueKind.String || item.GetString() is not { } text || text.Length is < 1 or > 64 || text != text.Trim() || text.Any(char.IsControl)) return false; value = text; return true; }
}
