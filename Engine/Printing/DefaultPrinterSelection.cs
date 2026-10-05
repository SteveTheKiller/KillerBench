using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace KillerBench.Engine.Printing;

public sealed record DefaultPrinterState(bool Complete, IReadOnlyList<string> Queues,
    IReadOnlyList<string> Defaults, bool ModePresent, int? LegacyDefaultPrinterMode);

public enum DefaultPrinterSelectionOutcome
{
    Preview,
    Blocked,
    Verified,
    NotApplied,
    Uncertain,
    Stopped,
    NoPending
}

public sealed record DefaultPrinterSelectionResult(string Queue, DefaultPrinterSelectionOutcome Outcome,
    string Detail, DefaultPrinterState? Before = null, ToolResult? Command = null,
    DefaultPrinterState? After = null, string? RecordPath = null)
{
    public int GetExitCode() => Outcome switch
    {
        DefaultPrinterSelectionOutcome.Preview or DefaultPrinterSelectionOutcome.Verified or
            DefaultPrinterSelectionOutcome.NoPending => 0,
        DefaultPrinterSelectionOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IDefaultPrinterSelectionHost
{
    Task<DefaultPrinterState> InspectAsync(CancellationToken token);
    Task<ToolResult> SetDefaultAsync(string queue, CancellationToken token);
}

public sealed record DefaultPrinterSelectionPendingRecord(int Schema, DateTimeOffset StartedUtc,
    string Queue, DefaultPrinterState Before)
{
    public string Machine { get; init; } = "";
}

public static class DefaultPrinterSelection
{
    private const string PendingName = "default-printer.pending.json";
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };

    public static WorkflowCheck Assess(DefaultPrinterState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var evidence = new ToolResult(0, JsonSerializer.Serialize(state, Capture.JsonOptions), "", false);
        if (state is { Complete: true, Queues.Count: 0, Defaults.Count: 0 } &&
            (state.ModePresent ? state.LegacyDefaultPrinterMode is 0 or 1 : state.LegacyDefaultPrinterMode is null))
            return new("Default printer selection", CheckState.Information,
                "No installed printer queue was reported, so no default-printer repair is available.", Evidence: evidence);
        if (!Complete(state)) return new("Default printer selection", CheckState.Unknown,
            "Installed queue, default or automatic-management evidence is incomplete. No default-printer change is eligible.", Evidence: evidence);
        if (state.Defaults.Count == 1) return new("Default printer selection", CheckState.Information,
            "Windows reports one coherent default printer. Choosing a different installed queue remains a user preference.", Evidence: evidence);
        return new("Default printer selection", CheckState.Repairable,
            state.Defaults.Count == 0
                ? "Installed queues are present but no default is reported. Select one exact installed queue in the guarded printing controls before execution."
                : "Conflicting default-printer selections are reported. Select one exact installed queue in the guarded printing controls before execution.",
            RepairKind.DefaultPrinter, evidence);
    }

    public static bool HasPendingRecord(string directory) =>
        !string.IsNullOrWhiteSpace(directory) && File.Exists(Path.Combine(Path.GetFullPath(directory), PendingName));

    public static async Task<DefaultPrinterSelectionResult> RunAsync(string queue, bool execute, string directory,
        IDefaultPrinterSelectionHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        string target = ValidateQueue(queue);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        token.ThrowIfCancellationRequested();
        if (!execute) return new(target, DefaultPrinterSelectionOutcome.Preview,
            "Execution will confirm the exact installed queue twice, require a missing or conflicting default, preserve automatic printer management, set only the running account's default, then verify one exact default. No queue, port, driver or job changes.");

        string root = Path.GetFullPath(directory);
        Directory.CreateDirectory(root);
        using var operationLock = new FileStream(Path.Combine(root, "default-printer.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pending = Path.Combine(root, PendingName);
        if (File.Exists(pending)) return new(target, DefaultPrinterSelectionOutcome.Blocked,
            "A prior default-printer change needs reconciliation before another change can start.", RecordPath: pending);

        Report(progress, "Inspecting installed queues and the current default printer.");
        var first = await InspectAsync(host, token).ConfigureAwait(false);
        if (!Eligible(first, target, out string blocker))
            return new(target, DefaultPrinterSelectionOutcome.Blocked, blocker, Before: first);
        Report(progress, "Repeating default-printer evidence before the change.");
        var second = await InspectAsync(host, token).ConfigureAwait(false);
        if (!Same(first, second) || !Eligible(second, target, out _))
            return new(target, DefaultPrinterSelectionOutcome.Blocked,
                "Printer queues, default selection or automatic-management state changed during the fresh recheck. No setting changed.", Before: second);

        var prepared = new DefaultPrinterSelectionPendingRecord(1, DateTimeOffset.UtcNow, target, second)
        { Machine = Environment.MachineName };
        await WriteNewAsync(pending, JsonSerializer.SerializeToUtf8Bytes(prepared, Capture.JsonOptions), token).ConfigureAwait(false);
        ToolResult? command = null;
        bool commandDispatched = false;
        try
        {
            Report(progress, "Confirming final default-printer eligibility.");
            var final = await InspectAsync(host, token).ConfigureAwait(false);
            if (!Same(second, final) || !Eligible(final, target, out _))
                return await FinishAsync(new(target, DefaultPrinterSelectionOutcome.Blocked,
                    "Printer queues, default selection or automatic-management state changed after preparation. No setting changed.",
                    second, After: final, RecordPath: pending), root, true).ConfigureAwait(false);
            Report(progress, "Setting the exact selected default printer.");
            commandDispatched = true;
            command = await (host.SetDefaultAsync(target, token) ?? throw new InvalidDataException("The printer provider returned no command task."))
                .WaitAsync(TimeSpan.FromSeconds(30), token).ConfigureAwait(false);
            if (!Successful(command))
                return await FinishAsync(new(target, DefaultPrinterSelectionOutcome.Uncertain,
                    "The default-printer request did not return complete success evidence. Reconcile the pending record before another attempt.",
                    second, command, RecordPath: pending), root, false).ConfigureAwait(false);
            Report(progress, "Verifying the exact default and preserved management setting.");
            var after = await InspectAsync(host, token).ConfigureAwait(false);
            bool verified = SameInventoryAndMode(second, after) && after.Defaults.Count == 1 &&
                string.Equals(after.Defaults[0], target, StringComparison.OrdinalIgnoreCase);
            bool notApplied = Same(second, after);
            return await FinishAsync(new(target,
                verified ? DefaultPrinterSelectionOutcome.Verified : notApplied ? DefaultPrinterSelectionOutcome.NotApplied : DefaultPrinterSelectionOutcome.Uncertain,
                verified
                    ? "The running account now reports the exact selected queue as its only default. Automatic printer management, queues, ports, drivers and jobs were preserved. Printing was not tested."
                    : notApplied
                        ? "The command returned, but the complete default-printer state is unchanged. No change was verified."
                        : "The command returned, but queue inventory, default selection or automatic-management evidence changed unexpectedly. The result remains unresolved.",
                second, command, after, pending), root, verified || notApplied).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!commandDispatched)
                return await FinishAsync(new(target, DefaultPrinterSelectionOutcome.Stopped,
                    "The operation stopped after durable preparation and before any default-printer request was sent.",
                    second, RecordPath: pending), root, true).ConfigureAwait(false);
            return await FinishAsync(new(target, DefaultPrinterSelectionOutcome.Stopped,
                "The operation stopped after durable preparation. The request may have started; reconcile before another attempt.",
                second, command, RecordPath: pending), root, false).ConfigureAwait(false);
        }
        catch (Exception error) when (Unavailable(error))
        {
            if (!commandDispatched)
                return await FinishAsync(new(target, DefaultPrinterSelectionOutcome.Blocked,
                    "Final default-printer eligibility could not be established: " + error.Message + ". No setting changed.",
                    second, RecordPath: pending), root, true).ConfigureAwait(false);
            return await FinishAsync(new(target, DefaultPrinterSelectionOutcome.Uncertain,
                "The default-printer operation was interrupted: " + error.Message + ". The pending record remains and no retry started.",
                second, command, RecordPath: pending), root, false).ConfigureAwait(false);
        }
    }

    public static async Task<DefaultPrinterSelectionResult> ReconcileAsync(string directory,
        IDefaultPrinterSelectionHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(directory), pending = Path.Combine(root, PendingName);
        token.ThrowIfCancellationRequested();
        if (!File.Exists(pending)) return new("", DefaultPrinterSelectionOutcome.NoPending,
            "No interrupted default-printer change is present.", RecordPath: pending);
        using var operationLock = new FileStream(Path.Combine(root, "default-printer.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        if (!File.Exists(pending)) return new("", DefaultPrinterSelectionOutcome.NoPending,
            "The interrupted default-printer record was already reconciled. No change was repeated.", RecordPath: pending);
        DefaultPrinterSelectionPendingRecord record;
        try { record = await ReadPendingAsync(pending, token).ConfigureAwait(false); }
        catch (Exception error) when (Unavailable(error))
        {
            return new("", DefaultPrinterSelectionOutcome.Uncertain,
                "The pending default-printer record is unavailable or invalid: " + error.Message +
                ". It was retained and the change was not repeated.", RecordPath: pending);
        }
        if (!Complete(record.Before) || !Eligible(record.Before, record.Queue, out _))
            return new(record.Queue, DefaultPrinterSelectionOutcome.Uncertain,
                "The pending record lacks complete eligibility evidence. It was retained and the change was not repeated.",
                record.Before, RecordPath: pending);
        DefaultPrinterState current;
        try
        {
            Report(progress, "Inspecting current default-printer state without repeating the change.");
            current = await InspectAsync(host, token).ConfigureAwait(false);
        }
        catch (Exception error) when (Unavailable(error))
        {
            return new(record.Queue, DefaultPrinterSelectionOutcome.Uncertain,
                "Current default-printer evidence is unavailable: " + error.Message +
                ". The pending record was retained and the change was not repeated.", record.Before, RecordPath: pending);
        }
        bool verified = SameInventoryAndMode(record.Before, current) && current.Defaults.Count == 1 &&
            string.Equals(current.Defaults[0], record.Queue, StringComparison.OrdinalIgnoreCase);
        bool notApplied = Same(record.Before, current);
        if (!verified && !notApplied) return new(record.Queue, DefaultPrinterSelectionOutcome.Uncertain,
            "Current queue, default or automatic-management evidence conflicts with the prepared state. The pending record was retained and the change was not repeated.",
            record.Before, After: current, RecordPath: pending);
        return await FinishAsync(new(record.Queue,
            verified ? DefaultPrinterSelectionOutcome.Verified : DefaultPrinterSelectionOutcome.NotApplied,
            verified
                ? "The exact selected queue is now the only default and automatic printer management is unchanged. The change was not repeated."
                : "The complete prior default-printer state remains in place. No change was verified and the change was not repeated.",
            record.Before, After: current, RecordPath: pending), root, true).ConfigureAwait(false);
    }

    public static string ValidateQueue(string queue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queue);
        string value = queue.Trim();
        if (!DriverReference.ValidText(value, 512) || value != queue)
            throw new ArgumentException("Choose one exact installed printer queue without leading, trailing or control characters.", nameof(queue));
        return value;
    }

    private static bool Eligible(DefaultPrinterState state, string target, out string blocker)
    {
        blocker = !Complete(state)
            ? "Installed queue, default or automatic-management evidence is incomplete. No setting changed."
            : state.Queues.Count(name => string.Equals(name, target, StringComparison.OrdinalIgnoreCase)) != 1
                ? "The exact selected queue is missing or ambiguous. No setting changed."
                : state.Defaults.Count == 1
                    ? string.Equals(state.Defaults[0], target, StringComparison.OrdinalIgnoreCase)
                        ? "The exact selected queue is already the only default. No change is needed."
                        : "Windows already reports one coherent default. Use Windows printer settings for a preference change. Automatic repair is limited to missing or conflicting defaults."
                    : "";
        return blocker.Length == 0;
    }

    private static bool Complete(DefaultPrinterState? state) => state is { Complete: true } &&
        state.Queues is { Count: >= 1 and <= 512 } && state.Defaults.Count <= state.Queues.Count &&
        ValidNames(state.Queues) && ValidNames(state.Defaults) &&
        state.Defaults.All(name => state.Queues.Contains(name, StringComparer.OrdinalIgnoreCase)) &&
        (state.ModePresent ? state.LegacyDefaultPrinterMode is 0 or 1 : state.LegacyDefaultPrinterMode is null);

    private static bool ValidNames(IReadOnlyList<string> names) => names.All(name => DriverReference.ValidText(name, 512) && name == name.Trim()) &&
        names.Distinct(StringComparer.OrdinalIgnoreCase).Count() == names.Count;

    private static bool Same(DefaultPrinterState left, DefaultPrinterState right) =>
        SameInventoryAndMode(left, right) && left.Defaults.Order(StringComparer.OrdinalIgnoreCase)
            .SequenceEqual(right.Defaults.Order(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

    private static bool SameInventoryAndMode(DefaultPrinterState left, DefaultPrinterState right) => Complete(left) && Complete(right) &&
        left.ModePresent == right.ModePresent && left.LegacyDefaultPrinterMode == right.LegacyDefaultPrinterMode &&
        left.Queues.Order(StringComparer.OrdinalIgnoreCase).SequenceEqual(right.Queues.Order(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

    private static bool Successful(ToolResult value) => value.ExitCode == 0 && !value.TimedOut && !value.Truncated && string.IsNullOrWhiteSpace(value.Error);

    private static async Task<DefaultPrinterState> InspectAsync(IDefaultPrinterSelectionHost host, CancellationToken token)
    {
        var value = await (host.InspectAsync(token) ?? throw new InvalidDataException("The printer provider returned no inspection task."))
            .WaitAsync(TimeSpan.FromSeconds(30), token).ConfigureAwait(false);
        return value ?? throw new InvalidDataException("The printer provider returned no state.");
    }

    private static async Task<DefaultPrinterSelectionPendingRecord> ReadPendingAsync(string path, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 131_072) throw new InvalidDataException("Default-printer recovery record size is invalid.");
        try
        {
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: token).ConfigureAwait(false);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("Default-printer recovery record contains conflicting fields.");
            var record = document.RootElement.Deserialize<DefaultPrinterSelectionPendingRecord>(StrictJson);
            if (record is not { Schema: 1 } || record.StartedUtc == default ||
                !string.Equals(record.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Default-printer recovery record is invalid or belongs to another computer.");
            return record with { Queue = ValidateQueue(record.Queue) };
        }
        catch (JsonException error) { throw new InvalidDataException("Default-printer recovery record is not valid JSON.", error); }
        catch (ArgumentException error) { throw new InvalidDataException("Default-printer recovery queue is invalid.", error); }
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    private static async Task WriteNewAsync(string path, byte[] value, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096,
            FileOptions.Asynchronous | FileOptions.WriteThrough);
        await stream.WriteAsync(value, token).ConfigureAwait(false);
        await stream.FlushAsync(token).ConfigureAwait(false);
        stream.Flush(true);
    }

    private static async Task<DefaultPrinterSelectionResult> FinishAsync(DefaultPrinterSelectionResult result, string root, bool resolve)
    {
        string journal = Path.Combine(root, "default-printer-" + DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmssfff") +
            "-" + Guid.NewGuid().ToString("N") + ".json");
        await JournalPersistence.WriteAsync(journal, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        string pending = Path.Combine(root, PendingName);
        if (resolve && File.Exists(pending)) File.Move(pending, journal + ".prepared.json");
        return result with { RecordPath = resolve ? journal : pending };
    }

    private static bool Unavailable(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or NotSupportedException or TimeoutException or Win32Exception or System.Security.SecurityException;

    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed partial class WindowsDefaultPrinterSelectionHost : IDefaultPrinterSelectionHost
{
    public async Task<DefaultPrinterState> InspectAsync(CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Default-printer inspection requires Windows.");
        const string script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
            "$rows=@(Get-CimInstance -ClassName Win32_Printer -OperationTimeoutSec 20 -ErrorAction Stop|Sort-Object Name|Select-Object -First 513);" +
            "$kind='Absent';$mode=$null;$key=[Microsoft.Win32.Registry]::CurrentUser.OpenSubKey('Software\\Microsoft\\Windows NT\\CurrentVersion\\Windows');if($null-ne$key){try{$value=$key.GetValue('LegacyDefaultPrinterMode',$null,[Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames);if($null-ne$value){$kind=$key.GetValueKind('LegacyDefaultPrinterMode').ToString();if($value-is[int]){$mode=[int]$value}}}finally{$key.Dispose()}};" +
            "[pscustomobject]@{complete=($rows.Count-le 512);queues=@($rows|ForEach-Object{[string]$_.Name});defaults=@($rows|Where-Object Default|ForEach-Object{[string]$_.Name});modeKind=$kind;mode=$mode}|ConvertTo-Json -Depth 3 -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true),
            TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            throw new InvalidDataException("Default-printer inspection was incomplete.");
        try
        {
            using var document = JsonDocument.Parse(result.Output);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object || root.EnumerateObject().Count() != 5 ||
                !root.GetProperty("complete").GetBoolean()) throw new InvalidDataException("Default-printer state is incomplete.");
            string[] queues = [.. root.GetProperty("queues").EnumerateArray().Select(item => item.GetString()!)];
            string[] defaults = [.. root.GetProperty("defaults").EnumerateArray().Select(item => item.GetString()!)];
            string kind = root.GetProperty("modeKind").GetString()!;
            var modeValue = root.GetProperty("mode");
            int? mode = modeValue.ValueKind == JsonValueKind.Null ? null : modeValue.GetInt32();
            return new(true, queues, defaults, kind != "Absent", mode);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
        { throw new InvalidDataException("Default-printer inspection returned invalid data.", error); }
    }

    public Task<ToolResult> SetDefaultAsync(string queue, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Default-printer selection requires Windows.");
        string target = DefaultPrinterSelection.ValidateQueue(queue);
        bool changed = SetDefaultPrinterW(target);
        int error = changed ? 0 : Marshal.GetLastWin32Error();
        return Task.FromResult(changed
            ? new ToolResult(0, "Default printer request accepted.", "", false)
            : new ToolResult(error == 0 ? 1 : error, "", new Win32Exception(error).Message, false));
    }

    [LibraryImport("winspool.drv", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetDefaultPrinterW(string printerName);
}
