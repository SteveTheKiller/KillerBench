using Microsoft.Win32;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace KillerBench.Engine.Evidence;

public sealed record EventChannelState(string Channel, bool Enabled, bool Retention, bool AutoBackup,
    ulong MaximumSize, string ConfigurationHash);
public enum EventChannelEnableOutcome { Preview, Blocked, Verified, Failed, Uncertain, Stopped, NoPending }
public sealed record EventChannelEnableResult(EventChannelEnableOutcome Outcome, string Detail, string Channel,
    EventChannelState? Before = null, ToolResult? Command = null, EventChannelState? After = null, string? RecordPath = null)
{
    public int GetExitCode() => Outcome is EventChannelEnableOutcome.Preview or EventChannelEnableOutcome.Verified or EventChannelEnableOutcome.NoPending ? 0 :
        Outcome == EventChannelEnableOutcome.Stopped ? 130 : 3;
}

public interface IEventChannelEnableHost
{
    bool IsAdministrator { get; }
    Task<bool?> IsManagedAsync(string channel, CancellationToken token);
    Task<EventChannelState> ReadAsync(string channel, CancellationToken token);
    Task<ToolResult> EnableAsync(string channel, CancellationToken token);
}

public static class EventChannelEnable
{
    private const string PendingName = "event-channel-enable.pending.json";
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions) { RespectRequiredConstructorParameters = true };

    public static string ValidateChannel(string channel)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channel);
        return EventLogCapacityDiagnostics.Channels.SingleOrDefault(item => string.Equals(item, channel, StringComparison.Ordinal))
            ?? throw new ArgumentException("Choose one exact supported diagnostic event channel.", nameof(channel));
    }

    public static EventChannelState Parse(string channel, ToolResult evidence)
    {
        channel = ValidateChannel(channel);
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.ExitCode != 0 || evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error) || evidence.Output.Length > 65_536)
            throw new InvalidDataException("Event channel configuration evidence is incomplete.");
        try
        {
            using var reader = XmlReader.Create(new StringReader(evidence.Output), new XmlReaderSettings
            { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 65_536 });
            var document = XDocument.Load(reader, LoadOptions.None);
            var root = document.Root;
            XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events";
            if (root?.Name != ns + "channel" || !string.Equals((string?)root.Attribute("name"), channel, StringComparison.Ordinal))
                throw new InvalidDataException("Event channel identity did not match the request.");
            var logging = root.Elements(ns + "logging").ToArray();
            if (logging.Length != 1) throw new InvalidDataException("Event channel logging configuration is incomplete.");
            string? Field(string name)
            {
                var fields = logging[0].Elements(ns + name).ToArray();
                return fields.Length == 1 ? fields[0].Value : null;
            }
            if (!bool.TryParse((string?)root.Attribute("enabled"), out bool enabled) ||
                !bool.TryParse(Field("retention"), out bool retention) ||
                !bool.TryParse(Field("autoBackup"), out bool autoBackup) ||
                !ulong.TryParse(Field("maxSize"), NumberStyles.None, CultureInfo.InvariantCulture, out ulong maximumSize) ||
                maximumSize == 0 || autoBackup && !retention)
                throw new InvalidDataException("Event channel configuration fields are malformed.");
            root.Attribute("enabled")!.Value = "STATE";
            string normalized = document.ToString(SaveOptions.DisableFormatting);
            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
            return new(channel, enabled, retention, autoBackup, maximumSize, hash);
        }
        catch (XmlException error) { throw new InvalidDataException("Event channel configuration is not valid XML.", error); }
    }

    public static async Task<WorkflowCheck> ReviewAsync(string channel, IEventChannelEnableHost host,
        CancellationToken token = default)
    {
        channel = ValidateChannel(channel);
        ArgumentNullException.ThrowIfNull(host);
        token.ThrowIfCancellationRequested();
        try
        {
            var state = await ReadAsync(host, channel, token).ConfigureAwait(false);
            bool? managed = await ManagedAsync(host, channel, token).ConfigureAwait(false);
            string ownership = managed == true ? "managed policy" : managed == false ? "local policy" : "unknown policy ownership";
            string detail = $"Selected channel: {channel}. Enabled: {state.Enabled}. Retention: {state.Retention}. Automatic backup: {state.AutoBackup}. Maximum size: {state.MaximumSize} bytes. Ownership: {ownership}. ";
            if (state.Enabled)
                return new("Selected event channel", CheckState.Information,
                    detail + "No change is needed. Event delivery and log contents remain unverified.");
            if (managed == true)
                return new("Selected event channel", CheckState.Attention,
                    detail + "The channel is disabled and managed. Review it with the policy owner; KillerBench will not override it.");
            if (managed is null)
                return new("Selected event channel", CheckState.Unknown,
                    detail + "The channel is disabled, but ownership is unresolved. No change is authorized.");
            return new("Selected event channel", CheckState.Attention,
                detail + "The channel is disabled and a separate guarded enable action can review eligibility. This read-only review does not authorize or run that action.");
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or
            TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            return new("Selected event channel", CheckState.Unknown,
                "The selected channel could not be reviewed completely: " + error.Message + ". No setting was changed.");
        }
    }

    public static async Task<EventChannelEnableResult> RunAsync(string channel, bool execute, string directory,
        IEventChannelEnableHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        channel = ValidateChannel(channel);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(directory);
        string pending = Path.Combine(root, PendingName);
        if (File.Exists(pending)) throw new InvalidOperationException("An earlier event channel change is unresolved. No command was repeated.");
        token.ThrowIfCancellationRequested();
        Report(progress, "Inspecting the selected event channel configuration.");
        var first = await ReadAsync(host, channel, token).ConfigureAwait(false);
        if (first.Enabled) return Result(EventChannelEnableOutcome.Blocked, "The selected event channel is already enabled. No change is required.", channel, first);
        bool? managed = await ManagedAsync(host, channel, token).ConfigureAwait(false);
        if (managed != false) return Result(EventChannelEnableOutcome.Blocked,
            managed == true ? "The selected event channel has managed policy. KillerBench will not override it." :
            "Event channel policy ownership could not be verified. No setting was changed.", channel, first);
        if (!execute) return Result(EventChannelEnableOutcome.Preview,
            "Eligible preview. Execution will recheck the same disabled unmanaged channel, enable it once and verify every other reported configuration field remains unchanged.", channel, first);
        if (!host.IsAdministrator) return Result(EventChannelEnableOutcome.Blocked,
            "Administrator access is required. No event channel setting was changed.", channel, first);
        var second = await ReadAsync(host, channel, token).ConfigureAwait(false);
        if (second != first || await ManagedAsync(host, channel, token).ConfigureAwait(false) != false)
            return Result(EventChannelEnableOutcome.Blocked, "Channel configuration or policy ownership changed during the fresh recheck. No command was run.", channel, second);

        Directory.CreateDirectory(root);
        await WriteNewAsync(pending, JsonSerializer.SerializeToUtf8Bytes(new EventChannelPendingRecord(1,
            Environment.MachineName, DateTimeOffset.UtcNow, channel, second), Capture.JsonOptions), token).ConfigureAwait(false);
        ToolResult? command = null;
        bool commandDispatched = false;
        try
        {
            Report(progress, "Repeating event channel eligibility immediately before the enable request.");
            var immediate = await ReadAsync(host, channel, token).ConfigureAwait(false);
            bool? immediateManaged = await ManagedAsync(host, channel, token).ConfigureAwait(false);
            if (immediate.Enabled || immediate != second || immediateManaged != false || !host.IsAdministrator)
            {
                string detail = immediate.Enabled && SameExceptEnabled(second, immediate)
                    ? "The selected event channel became enabled after preparation. No enable command was run."
                    : "Final channel, policy or administrator evidence no longer authorizes enablement. No command was run.";
                return await FinishAsync(Result(EventChannelEnableOutcome.Blocked, detail, channel,
                    second, after: immediate, recordPath: pending), root, true).ConfigureAwait(false);
            }

            Report(progress, "Enabling the selected event channel.");
            commandDispatched = true;
            command = await (host.EnableAsync(channel, token) ?? throw new InvalidDataException("The event channel provider returned no command task."))
                .WaitAsync(TimeSpan.FromMinutes(1), token).ConfigureAwait(false);
            if (command is null || command.ExitCode != 0 || command.TimedOut || command.Truncated || !string.IsNullOrWhiteSpace(command.Error))
                return await FinishAsync(Result(EventChannelEnableOutcome.Uncertain,
                    "The enable command was not confirmed. The pending record remains for read-only reconciliation.", channel, second, command, recordPath: pending), root, false).ConfigureAwait(false);
            var after = await ReadAsync(host, channel, token).ConfigureAwait(false);
            bool sameConfiguration = SameExceptEnabled(second, after);
            bool verified = after.Enabled && sameConfiguration;
            bool unchanged = !after.Enabled && sameConfiguration;
            return await FinishAsync(Result(verified ? EventChannelEnableOutcome.Verified : unchanged ? EventChannelEnableOutcome.Failed : EventChannelEnableOutcome.Uncertain,
                verified ? "The selected event channel is enabled and every other reported channel setting is unchanged. Event delivery remains unverified." :
                unchanged ? "The command returned, but the channel remains disabled. No change was verified." :
                "The channel or another reported setting changed after the command. The result is unresolved.", channel, second, command, after, pending), root, verified || unchanged).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!commandDispatched)
                return await FinishAsync(Result(EventChannelEnableOutcome.Stopped,
                    "The operation stopped after durable preparation and before any event channel command was sent.",
                    channel, second, recordPath: pending), root, true).ConfigureAwait(false);
            return await FinishAsync(Result(EventChannelEnableOutcome.Stopped,
                "The operation stopped after its pending record was saved. The command may have started; recovery will inspect state without repeating it.", channel, second, command, recordPath: pending), root, false).ConfigureAwait(false);
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or
            TimeoutException or System.Security.SecurityException or System.ComponentModel.Win32Exception)
        {
            if (!commandDispatched)
                return await FinishAsync(Result(EventChannelEnableOutcome.Blocked,
                    "Final event channel eligibility could not be established: " + error.Message + ". No command was run.",
                    channel, second, recordPath: pending), root, true).ConfigureAwait(false);
            return await FinishAsync(Result(EventChannelEnableOutcome.Uncertain,
                "Event channel enablement was interrupted: " + error.Message + ". The pending record remains and no retry was started.", channel, second, command, recordPath: pending), root, false).ConfigureAwait(false);
        }
    }

    public static async Task<EventChannelEnableResult> ReconcileAsync(string directory, IEventChannelEnableHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(directory);
        string pending = Path.Combine(root, PendingName);
        if (!File.Exists(pending)) return Result(EventChannelEnableOutcome.NoPending, "No interrupted event channel change is present.", "");
        EventChannelPendingRecord record;
        try { record = await ReadRecordAsync(pending, token).ConfigureAwait(false); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or System.Security.SecurityException)
        { return Result(EventChannelEnableOutcome.Uncertain, "The pending event channel record is unavailable or invalid: " + error.Message, "", recordPath: pending); }
        Report(progress, "Inspecting the selected event channel without repeating the enable command.");
        var current = await ReadAsync(host, record.Channel, token).ConfigureAwait(false);
        bool sameConfiguration = SameExceptEnabled(record.Before, current);
        if (!sameConfiguration) return Result(EventChannelEnableOutcome.Uncertain,
            "The event channel or another reported setting changed. The pending record was retained and no command was repeated.", record.Channel, record.Before, after: current, recordPath: pending);
        return await FinishAsync(Result(current.Enabled ? EventChannelEnableOutcome.Verified : EventChannelEnableOutcome.Failed,
            current.Enabled ? "The selected channel is enabled. The interrupted command was not repeated." :
            "The selected channel remains in its exact prior disabled state. No change was verified and the command was not repeated.",
            record.Channel, record.Before, after: current, recordPath: pending), root, true).ConfigureAwait(false);
    }

    public static WorkflowCheck ToWorkflowCheck(EventChannelEnableResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        var state = result.Outcome switch
        {
            EventChannelEnableOutcome.Preview => CheckState.Repairable,
            EventChannelEnableOutcome.Verified => CheckState.Clear,
            EventChannelEnableOutcome.Blocked when result.Before?.Enabled == true => CheckState.Clear,
            EventChannelEnableOutcome.Blocked or EventChannelEnableOutcome.Failed => CheckState.Attention,
            EventChannelEnableOutcome.NoPending => CheckState.Information,
            _ => CheckState.Unknown
        };
        return new("Diagnostic event channel", state, result.Detail,
            state == CheckState.Repairable ? RepairKind.EventChannelEnable : null, result.Command);
    }

    private static async Task<EventChannelState> ReadAsync(IEventChannelEnableHost host, string channel, CancellationToken token)
    {
        var state = await (host.ReadAsync(channel, token) ?? throw new InvalidDataException("The event channel provider returned no inspection task."))
            .WaitAsync(TimeSpan.FromSeconds(20), token).ConfigureAwait(false);
        return state is not null && Valid(state, channel)
            ? state : throw new InvalidDataException("The event channel provider returned invalid state.");
    }
    private static async Task<bool?> ManagedAsync(IEventChannelEnableHost host, string channel, CancellationToken token) =>
        await (host.IsManagedAsync(channel, token) ?? throw new InvalidDataException("The policy provider returned no inspection task."))
            .WaitAsync(TimeSpan.FromSeconds(10), token).ConfigureAwait(false);
    private static bool Valid(EventChannelState state, string channel) => string.Equals(state.Channel, channel, StringComparison.Ordinal) &&
        state.MaximumSize > 0 && state.ConfigurationHash is { Length: 64 } && state.ConfigurationHash.All(Uri.IsHexDigit) && (!state.AutoBackup || state.Retention);
    private static bool SameExceptEnabled(EventChannelState left, EventChannelState right) =>
        string.Equals(left.Channel, right.Channel, StringComparison.Ordinal) && left.Retention == right.Retention &&
        left.AutoBackup == right.AutoBackup && left.MaximumSize == right.MaximumSize &&
        string.Equals(left.ConfigurationHash, right.ConfigurationHash, StringComparison.Ordinal);
    private static EventChannelEnableResult Result(EventChannelEnableOutcome outcome, string detail, string channel,
        EventChannelState? before = null, ToolResult? command = null, EventChannelState? after = null, string? recordPath = null) =>
        new(outcome, detail, channel, before, command, after, recordPath);
    private static async Task<EventChannelEnableResult> FinishAsync(EventChannelEnableResult result, string root, bool resolve)
    {
        string journal = Path.Combine(root, "event-channel-enable-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N") + ".json");
        await JournalPersistence.WriteAsync(journal, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        string pending = Path.Combine(root, PendingName);
        if (resolve && File.Exists(pending)) File.Move(pending, journal + ".prepared.json");
        return result with { RecordPath = resolve ? journal : pending };
    }
    private static async Task WriteNewAsync(string path, byte[] data, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(data, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
        stream.Flush(true);
    }
    private static async Task<EventChannelPendingRecord> ReadRecordAsync(string path, CancellationToken token)
    {
        byte[] data;
        await using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
        {
            if (stream.Length is <= 0 or > 65_536) throw new InvalidDataException("Event channel recovery record size is invalid.");
            data = new byte[stream.Length];
            await stream.ReadExactlyAsync(data, token).ConfigureAwait(false);
        }
        try
        {
            using var document = JsonDocument.Parse(data, new JsonDocumentOptions { MaxDepth = 8 });
            CheckDuplicates(document.RootElement);
            var record = JsonSerializer.Deserialize<EventChannelPendingRecord>(data, StrictJson);
            return record is { Schema: 1 } && record.StartedUtc != default &&
                string.Equals(record.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase) &&
                Valid(record.Before, ValidateChannel(record.Channel)) ? record : throw new InvalidDataException("Event channel recovery record is invalid.");
        }
        catch (JsonException error) { throw new InvalidDataException("Event channel recovery record is not valid JSON.", error); }
    }
    private static void CheckDuplicates(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw new JsonException("Duplicate event channel recovery field.");
                CheckDuplicates(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) CheckDuplicates(item);
    }
    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed record EventChannelPendingRecord(int Schema, string Machine, DateTimeOffset StartedUtc,
    string Channel, EventChannelState Before);

public sealed class WindowsEventChannelEnableHost : IEventChannelEnableHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();
    public async Task<EventChannelState> ReadAsync(string channel, CancellationToken token)
    {
        channel = EventChannelEnable.ValidateChannel(channel);
        var result = await BoundedTool.RunAsync(new("wevtutil.exe", ["gl", channel, "/f:xml"], true),
            TimeSpan.FromSeconds(15), token: token).ConfigureAwait(false);
        return EventChannelEnable.Parse(channel, result);
    }
    public Task<ToolResult> EnableAsync(string channel, CancellationToken token)
    {
        channel = EventChannelEnable.ValidateChannel(channel);
        return BoundedTool.RunAsync(new("wevtutil.exe", ["sl", channel, "/e:true"], false),
            TimeSpan.FromMinutes(1), token: token);
    }
    public Task<bool?> IsManagedAsync(string channel, CancellationToken token)
    {
        channel = EventChannelEnable.ValidateChannel(channel);
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Event channel policy inspection requires Windows.");
        try
        {
            using var root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = root.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows\EventLog\" + channel);
            return Task.FromResult<bool?>(key is not null && (key.ValueCount > 0 || key.SubKeyCount > 0));
        }
        catch (Exception error) when (error is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        { return Task.FromResult<bool?>(null); }
    }
}
