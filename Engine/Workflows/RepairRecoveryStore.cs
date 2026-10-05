using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Workflows;

public sealed record BootStamp(long RecordId, DateTimeOffset StartedUtc);
public sealed record RepairRecoveryRecord(int Schema, string Machine, string JournalPath, bool Pending, BootStamp? Boot);

public sealed class RepairRecoveryStore(string folder, Func<CancellationToken, Task<BootStamp?>> readBoot)
{
    private static readonly JsonSerializerOptions RecoveryJson = new(Capture.JsonOptions) { RespectRequiredConstructorParameters = true };
    private static readonly string[] BootEventSystemElements = ["Provider", "EventID", "EventRecordID", "TimeCreated"];
    private readonly string path = Path.Combine(folder, "recovery.json");
    private RepairRecoveryRecord? active;

    public static RepairRecoveryStore ForCurrentUser() => new(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Repairs"), ReadBootAsync);

    public async Task BeginAsync(string journalPath, CancellationToken token = default)
    {
        var previous = await ReadAsync(token).ConfigureAwait(false);
        var boot = await readBoot(token).ConfigureAwait(false);
        if (previous?.Pending == true && (previous.Machine != Environment.MachineName || previous.Boot is null || boot is null ||
            boot.RecordId == previous.Boot.RecordId || boot.StartedUtc <= previous.Boot.StartedUtc))
            throw new InvalidOperationException("A previous repair may still be completing inside Windows. Restart Windows and run diagnosis again before repairing. If startup evidence is unavailable, review the recovery record. Recovery journal: " + previous.JournalPath);
        active = new(1, Environment.MachineName, journalPath, true, boot);
        await SaveAsync(active, token).ConfigureAwait(false);
    }

    public async Task CompleteAsync(CancellationToken token = default)
    {
        if (active is null) throw new InvalidOperationException("No repair recovery record is active.");
        await SaveAsync(active with { Pending = false }, token).ConfigureAwait(false);
    }

    public Task<BootStamp?> ReadBootStampAsync(CancellationToken token = default) => readBoot(token);

    public static bool IsRelatedJournal([NotNullWhen(true)] RepairRecoveryRecord? pending, [NotNullWhen(true)] RepairResult? journal, RepairKind kind) =>
        pending is { Pending: true } && journal is not null && Enum.IsDefined(kind) && journal.Kind == kind &&
        string.Equals(journal.JournalPath, pending.JournalPath, StringComparison.Ordinal);

    public static bool MatchesPendingJournal([NotNullWhen(true)] RepairRecoveryRecord? pending, [NotNullWhen(true)] RepairResult? journal, RepairKind kind) =>
        IsRelatedJournal(pending, journal, kind) &&
        string.Equals(pending.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(journal.Machine, pending.Machine, StringComparison.OrdinalIgnoreCase);

    public async Task ResolvePendingAsync(string journalPath, CancellationToken token = default)
    {
        var record = await ReadAsync(token).ConfigureAwait(false);
        if (record is not { Pending: true } || !string.Equals(record.Machine, Environment.MachineName, StringComparison.Ordinal) ||
            !string.Equals(record.JournalPath, journalPath, StringComparison.Ordinal))
            throw new InvalidOperationException("The pending repair identity changed before reconciliation.");
        await SaveAsync(record with { Pending = false }, token).ConfigureAwait(false);
    }

    public async Task<RepairRecoveryRecord?> ReadAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        try
        {
            await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.Asynchronous);
            if (stream.Length > 16_384) throw new InvalidOperationException("Repair recovery record is oversized; review required.");
            var bytes = new byte[checked((int)stream.Length)];
            await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
            ReadOnlyMemory<byte> json = bytes;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) json = json[3..];
            using var document = JsonDocument.Parse(json);
            if (!UniqueFields(document.RootElement))
                throw new InvalidOperationException("Repair recovery record has conflicting fields; review required.");
            var record = document.RootElement.Deserialize<RepairRecoveryRecord>(RecoveryJson);
            if (record is null || record.Schema != 1 || string.IsNullOrWhiteSpace(record.Machine) || string.IsNullOrWhiteSpace(record.JournalPath) || record.Boot is { RecordId: <= 0 })
                throw new InvalidOperationException("Repair recovery record is invalid; review required.");
            return record;
        }
        catch (FileNotFoundException) { return null; }
        catch (DirectoryNotFoundException) { return null; }
        catch (JsonException e) { throw new InvalidOperationException("Repair recovery record could not be read; review required.", e); }
    }

    private async Task SaveAsync(RepairRecoveryRecord record, CancellationToken token)
    {
        Directory.CreateDirectory(folder);
        await JournalPersistence.WriteAsync(path, JsonSerializer.Serialize(record, Capture.JsonOptions), token: token).ConfigureAwait(false);
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    public static BootStamp? ParseBoot(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error)) return null;
        try
        {
            using var reader = XmlReader.Create(new StringReader(result.Output), new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 16_384 });
            var document = XDocument.Load(reader);
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
            var events = document.Descendants(ns + "Event").ToArray();
            if (events.Length != 1) return null;
            var systems = events[0].Elements(ns + "System").ToArray();
            if (systems.Length != 1) return null;
            var system = systems[0];
            if (BootEventSystemElements
                .Any(name => system.Elements(ns + name).Count() != 1)) return null;
            if ((string?)system?.Element(ns + "Provider")?.Attribute("Name") != "Microsoft-Windows-Kernel-General" ||
                (string?)system.Element(ns + "EventID") != "12" ||
                !long.TryParse((string?)system.Element(ns + "EventRecordID"), NumberStyles.None, CultureInfo.InvariantCulture, out long id) || id <= 0 ||
                !DateTimeOffset.TryParse((string?)system.Element(ns + "TimeCreated")?.Attribute("SystemTime"), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var started)) return null;
            return new(id, started);
        }
        catch (XmlException) { return null; }
    }

    public static Task<BootStamp?> ReadCurrentBootAsync(CancellationToken token = default) => ReadBootAsync(token);

    private static async Task<BootStamp?> ReadBootAsync(CancellationToken token)
    {
        var step = new RepairStep("wevtutil.exe", ["qe", "System", "/q:*[System[Provider[@Name='Microsoft-Windows-Kernel-General'] and EventID=12]]", "/rd:true", "/c:1", "/f:xml", "/e:Events", "/uni:false"], true);
        return ParseBoot(await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(10), token: token).ConfigureAwait(false));
    }
}
