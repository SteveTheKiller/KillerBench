using System.Text.Json;

namespace KillerBench.Engine.SystemHealth;

public enum SystemRestorePointReason { ModifySettings, ApplicationInstall, DeviceDriverInstall }
public enum SystemRestoreCreationOutcome { Preview, Blocked, Verified, Failed, Uncertain, Stopped }

public sealed record SystemRestoreCreationRequest(string Description, SystemRestorePointReason Reason);
public sealed record SystemRestoreCreationResult(SystemRestoreCreationRequest Request, SystemRestoreCreationOutcome Outcome,
    string Detail, SystemRestoreInventory? Before = null, ToolResult? Command = null,
    SystemRestoreInventory? After = null, string? JournalPath = null)
{
    public int GetExitCode() => Outcome switch
    {
        SystemRestoreCreationOutcome.Preview or SystemRestoreCreationOutcome.Verified => 0,
        SystemRestoreCreationOutcome.Stopped => 130,
        _ => 3
    };
}

public interface ISystemRestoreCreationHost
{
    bool IsAdministrator { get; }
    bool? IsSupportedClientOperatingSystem { get; }
    TimeSpan? MinimumCreationInterval { get; }
    Task<SystemRestoreInventory> InspectAsync(CancellationToken token);
    Task<ToolResult> CreateAsync(SystemRestoreCreationRequest request, IProgress<string>? progress, CancellationToken token);
}

public static class SystemRestoreCreation
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions)
    {
        RespectRequiredConstructorParameters = true
    };
    private sealed record PendingRecord(int Schema, string Machine, DateTimeOffset StartedUtc, SystemRestoreCreationRequest Request,
        SystemRestoreInventory Before, ToolResult? Command);

    public static SystemRestoreCreationRequest CreateRequest(string description, SystemRestorePointReason reason)
    {
        description = description?.Trim() ?? "";
        if (description.Length is < 1 or > 64 || description.Any(char.IsControl))
            throw new ArgumentException("Enter a restore point description from 1 through 64 printable characters.", nameof(description));
        if (!Enum.IsDefined(reason)) throw new ArgumentOutOfRangeException(nameof(reason));
        return new(description, reason);
    }

    public static Task<SystemRestoreCreationResult> PreviewAsync(SystemRestoreCreationRequest request,
        CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        request = CreateRequest(request.Description, request.Reason);
        return Task.FromResult(new SystemRestoreCreationResult(request, SystemRestoreCreationOutcome.Preview,
            "Preview only. Execution will confirm a supported Windows client, read the current restore-point inventory and configured frequency limit, require administrator access, save a pending record, request one restore point, then verify a new exact entry. No restore point is created by this preview."));
    }

    public static async Task<SystemRestoreCreationResult> ReviewAsync(SystemRestoreCreationRequest request,
        ISystemRestoreCreationHost host, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        request = CreateRequest(request.Description, request.Reason);
        SystemRestoreInventory before;
        try { before = await Required(host.InspectAsync, "System Restore inventory", TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(request, SystemRestoreCreationOutcome.Blocked, "System Restore eligibility could not be established: " + error.Message); }
        if (!before.Complete)
            return new(request, SystemRestoreCreationOutcome.Blocked,
                "The current restore-point inventory is incomplete. No restore point can be offered.", before);
        if (host.IsSupportedClientOperatingSystem != true)
            return new(request, SystemRestoreCreationOutcome.Blocked,
                host.IsSupportedClientOperatingSystem == false
                    ? "Restore-point creation is supported only on Windows client operating systems."
                    : "The Windows client product type could not be confirmed.", before);
        TimeSpan? minimumInterval = host.MinimumCreationInterval;
        if (minimumInterval is null)
            return new(request, SystemRestoreCreationOutcome.Blocked,
                "The configured restore-point creation frequency could not be read safely.", before);
        if (minimumInterval > TimeSpan.Zero && before.Points.Any(point => point.CreatedUtc > DateTimeOffset.UtcNow.Subtract(minimumInterval.Value)))
            return new(request, SystemRestoreCreationOutcome.Blocked,
                "The configured restore-point creation interval has not elapsed. Existing frequency policy is preserved.", before);
        if (!host.IsAdministrator)
            return new(request, SystemRestoreCreationOutcome.Blocked,
                "Administrator access is required before a restore point can be offered.", before);
        return new(request, SystemRestoreCreationOutcome.Preview,
            "This Windows client is eligible for one explicitly described restore point. Creation still requires a reason and separate confirmation.", before);
    }

    public static WorkflowCheck ToWorkflowCheck(SystemRestoreCreationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        bool available = result.Outcome == SystemRestoreCreationOutcome.Preview && result.Before is { Complete: true };
        return new("System Restore point creation", available ? CheckState.Repairable :
            result.Before is null ? CheckState.Unknown : CheckState.Attention,
            result.Detail, available ? RepairKind.SystemRestorePoint : null);
    }

    public static async Task<SystemRestoreCreationResult> RunAsync(SystemRestoreCreationRequest request, bool execute,
        string journalFolder, ISystemRestoreCreationHost host, IProgress<string>? progress = null,
        TimeSpan? operationTimeout = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(host);
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        request = CreateRequest(request.Description, request.Reason);
        if (!execute) return await PreviewAsync(request, token).ConfigureAwait(false);
        TimeSpan limit = operationTimeout ?? TimeSpan.FromMinutes(5);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromMinutes(15)) throw new ArgumentOutOfRangeException(nameof(operationTimeout));
        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "system-restore-create.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "system-restore-create.pending.json");
        if (File.Exists(pendingPath))
            return new(request, SystemRestoreCreationOutcome.Blocked,
                "A prior restore-point request needs reconciliation before another can start.", JournalPath: pendingPath);

        Report(progress, "Reading current System Restore points.");
        SystemRestoreInventory before;
        try { before = await Required(host.InspectAsync, "System Restore inventory", TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(request, SystemRestoreCreationOutcome.Blocked, "System Restore eligibility could not be established: " + error.Message); }
        if (!before.Complete)
            return new(request, SystemRestoreCreationOutcome.Blocked,
                "The current restore-point inventory is incomplete. No restore point was requested.", before);
        if (host.IsSupportedClientOperatingSystem != true)
            return new(request, SystemRestoreCreationOutcome.Blocked,
                host.IsSupportedClientOperatingSystem == false
                    ? "Restore-point creation is supported only on Windows client operating systems. No restore point was requested."
                    : "The Windows client product type could not be confirmed. No restore point was requested.", before);
        TimeSpan? minimumInterval = host.MinimumCreationInterval;
        if (minimumInterval is null)
            return new(request, SystemRestoreCreationOutcome.Blocked,
                "The configured restore-point creation frequency could not be read safely. No restore point was requested.", before);
        if (minimumInterval > TimeSpan.Zero && before.Points.Any(point => point.CreatedUtc > DateTimeOffset.UtcNow.Subtract(minimumInterval.Value)))
            return new(request, SystemRestoreCreationOutcome.Blocked,
                "The configured restore-point creation interval has not elapsed. No restore point was requested and the existing frequency policy was preserved.", before);
        if (!host.IsAdministrator)
            return new(request, SystemRestoreCreationOutcome.Blocked,
                "Administrator access is required. No restore point was requested.", before);

        var pending = new PendingRecord(1, Environment.MachineName, DateTimeOffset.UtcNow, request, before, null);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending, Capture.JsonOptions), token: token).ConfigureAwait(false);
        string journalPath = Path.Combine(root, "system-restore-create-" + Guid.NewGuid().ToString("N") + ".json");
        ToolResult? command = null;
        bool requestDispatched = false;
        SystemRestoreCreationResult result;
        try
        {
            Report(progress, "Repeating restore-point eligibility immediately before creation.");
            var immediate = await Required(host.InspectAsync, "Final System Restore inventory", TimeSpan.FromSeconds(30),
                token: token).ConfigureAwait(false);
            TimeSpan? immediateInterval = host.MinimumCreationInterval;
            bool intervalElapsed = immediateInterval is not null && (immediateInterval <= TimeSpan.Zero ||
                !immediate.Points.Any(point => point.CreatedUtc > DateTimeOffset.UtcNow.Subtract(immediateInterval.Value)));
            if (!immediate.Complete || host.IsSupportedClientOperatingSystem != true || immediateInterval is null ||
                !intervalElapsed || !host.IsAdministrator)
            {
                result = new(request, SystemRestoreCreationOutcome.Blocked,
                    !intervalElapsed && immediateInterval is not null
                        ? "A restore point now falls inside the configured creation interval. No restore-point request was sent."
                        : "Final client, inventory, frequency or administrator evidence no longer authorizes creation. No restore-point request was sent.",
                    before, After: immediate, JournalPath: journalPath);
                await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions),
                    token:                     CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, "Requesting restore point: " + request.Description);
            requestDispatched = true;
            command = await Required(ct => host.CreateAsync(request, progress, ct), "Restore-point creation", limit, token: token).ConfigureAwait(false);
            await JournalPersistence.WriteAsync(pendingPath,
                JsonSerializer.Serialize(pending with { Command = command }, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
            if (!Complete(command))
                result = new(request, command.TimedOut || command.Truncated ? SystemRestoreCreationOutcome.Uncertain : SystemRestoreCreationOutcome.Failed,
                    "Windows did not provide a complete successful creation result. Reconcile before requesting another restore point.", before, command, JournalPath: journalPath);
            else
                result = await VerifyAsync(request, pending.StartedUtc, before, command, journalPath, host, progress, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result = new(request, SystemRestoreCreationOutcome.Stopped,
                requestDispatched
                    ? "Restore-point creation stopped after the request may have reached Windows. Reconcile before trying again."
                    : "Restore-point creation stopped before any request was sent. No restore point was requested.",
                before, command, JournalPath: journalPath);
        }
        catch (Exception error) when (Unavailable(error))
        {
            result = new(request, requestDispatched ? SystemRestoreCreationOutcome.Uncertain : SystemRestoreCreationOutcome.Blocked,
                requestDispatched
                    ? "Restore-point creation was interrupted: " + error.Message + ". Reconcile before trying again."
                    : "Final restore-point eligibility was unavailable: " + error.Message + ". No restore-point request was sent.",
                before, command, JournalPath: journalPath);
        }
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == SystemRestoreCreationOutcome.Verified || !requestDispatched)
            File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    public static async Task<SystemRestoreCreationResult> ReconcileAsync(string journalFolder,
        ISystemRestoreCreationHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(journalFolder);
        string pendingPath = Path.Combine(root, "system-restore-create.pending.json");
        if (!File.Exists(pendingPath))
            return new(new("KillerBench restore point", SystemRestorePointReason.ModifySettings),
                SystemRestoreCreationOutcome.Blocked, "No pending restore-point request was found.");
        using var gate = new FileStream(Path.Combine(root, "system-restore-create.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        PendingRecord pending;
        try
        {
            pending = await ReadPendingAsync(pendingPath, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception error) when (Unavailable(error))
        {
            return new(new("KillerBench restore point", SystemRestorePointReason.ModifySettings),
                SystemRestoreCreationOutcome.Uncertain,
                "The interrupted restore-point record could not be verified for this computer. It was retained and the request was not repeated.",
                JournalPath: pendingPath);
        }
        if (pending.Schema != 1 || pending.Command is null || !Complete(pending.Command))
            return new(pending.Request, SystemRestoreCreationOutcome.Uncertain,
                "The pending record does not prove that Windows accepted the request. It was not repeated.",
                pending.Before, pending.Command, JournalPath: pendingPath);
        string journalPath = Path.Combine(root, "system-restore-create-recovered-" + Guid.NewGuid().ToString("N") + ".json");
        var result = await VerifyAsync(pending.Request, pending.StartedUtc, pending.Before, pending.Command,
            journalPath, host, progress, token).ConfigureAwait(false);
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), token: CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == SystemRestoreCreationOutcome.Verified)
            File.Move(pendingPath, journalPath + ".prepared.json");
        return result;
    }

    private static async Task<PendingRecord> ReadPendingAsync(string path, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
        if (stream.Length is <= 0 or > 1_048_576) throw new InvalidDataException("Pending restore-point record size is invalid.");
        var bytes = new byte[checked((int)stream.Length)];
        await stream.ReadExactlyAsync(bytes, token).ConfigureAwait(false);
        try
        {
            using var document = JsonDocument.Parse(bytes);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("Pending restore-point record contains conflicting fields.");
            var pending = document.RootElement.Deserialize<PendingRecord>(StrictJson) ??
                throw new InvalidDataException("Pending restore-point record is empty.");
            if (pending.Schema != 1 || !pending.Machine.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
                pending.StartedUtc == default || pending.Before is not { Complete: true })
                throw new InvalidDataException("Pending restore-point record is invalid.");
            _ = CreateRequest(pending.Request.Description, pending.Request.Reason);
            return pending;
        }
        catch (JsonException error) { throw new InvalidDataException("Pending restore-point record is invalid.", error); }
        catch (ArgumentException error) { throw new InvalidDataException("Pending restore-point request is invalid.", error); }
    }

    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }

    public static uint ReasonCode(SystemRestorePointReason reason) => reason switch
    {
        SystemRestorePointReason.ApplicationInstall => 0,
        SystemRestorePointReason.DeviceDriverInstall => 10,
        SystemRestorePointReason.ModifySettings => 12,
        _ => throw new ArgumentOutOfRangeException(nameof(reason))
    };

    private static async Task<SystemRestoreCreationResult> VerifyAsync(SystemRestoreCreationRequest request,
        DateTimeOffset startedUtc, SystemRestoreInventory before, ToolResult command, string journalPath,
        ISystemRestoreCreationHost host, IProgress<string>? progress, CancellationToken token)
    {
        Report(progress, "Verifying the new restore point.");
        SystemRestoreInventory after;
        try { after = await Required(host.InspectAsync, "System Restore verification", TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        {
            return new(request, SystemRestoreCreationOutcome.Uncertain,
                "Windows accepted the request, but verification was unavailable: " + error.Message,
                before, command, JournalPath: journalPath);
        }
        var previous = before.Points.Select(point => point.Sequence).ToHashSet();
        var matches = after.Complete ? after.Points.Where(point => !previous.Contains(point.Sequence) &&
            point.Description == request.Description && point.Type == ReasonCode(request.Reason) &&
            point.CreatedUtc >= startedUtc.AddMinutes(-2)).ToArray() : [];
        bool verified = matches.Length == 1;
        return new(request, verified ? SystemRestoreCreationOutcome.Verified : SystemRestoreCreationOutcome.Uncertain,
            verified
                ? "Windows reported one new restore point with the requested description and reason. This verifies the inventory entry, not that a future restore will succeed."
                : "Windows accepted the request, but a single new exact restore-point entry was not verified. It was not requested again.",
            before, command, after, journalPath);
    }

    private static bool Complete(ToolResult result) => result.ExitCode == 0 && !result.TimedOut && !result.Truncated && string.IsNullOrWhiteSpace(result.Error);
    private static bool Unavailable(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
        System.Security.SecurityException or OperationCanceledException;
    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name,
        TimeSpan limit, CancellationToken token)
    {
        var pending = operation(token) ?? throw new InvalidOperationException(name + " provider returned no task.");
        return await pending.WaitAsync(limit, token).ConfigureAwait(false) ?? throw new InvalidDataException(name + " provider returned no evidence.");
    }
    private static void Report(IProgress<string>? progress, string message)
    {
        try { progress?.Report(message); }
        catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException) { }
    }
}

public sealed class WindowsSystemRestoreCreationHost : ISystemRestoreCreationHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();
    public bool? IsSupportedClientOperatingSystem
    {
        get
        {
            if (!OperatingSystem.IsWindows()) return false;
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\ProductOptions", false);
                return key?.GetValue("ProductType") is string value ? string.Equals(value, "WinNT", StringComparison.Ordinal) : null;
            }
            catch (Exception error) when (error is UnauthorizedAccessException or System.Security.SecurityException or IOException) { return null; }
        }
    }
    public TimeSpan? MinimumCreationInterval
    {
        get
        {
            if (!OperatingSystem.IsWindows()) return null;
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", false);
                object? value = key?.GetValue("SystemRestorePointCreationFrequency", null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames);
                if (value is null) return TimeSpan.FromHours(24);
                return value is int minutes && minutes >= 0 ? TimeSpan.FromMinutes(minutes) : null;
            }
            catch (Exception error) when (error is UnauthorizedAccessException or System.Security.SecurityException or IOException) { return null; }
        }
    }

    public async Task<SystemRestoreInventory> InspectAsync(CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("System Restore requires Windows.");
        var result = await BoundedTool.RunAsync(SystemRestoreDiagnostics.Step(), TimeSpan.FromSeconds(30), token: token).ConfigureAwait(false);
        return SystemRestoreDiagnostics.Read(result) ?? throw new InvalidDataException("System Restore inventory was unavailable or malformed.");
    }

    public Task<ToolResult> CreateAsync(SystemRestoreCreationRequest request, IProgress<string>? progress, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("System Restore requires Windows.");
        request = SystemRestoreCreation.CreateRequest(request.Description, request.Reason);
        string type = request.Reason switch
        {
            SystemRestorePointReason.ApplicationInstall => "APPLICATION_INSTALL",
            SystemRestorePointReason.DeviceDriverInstall => "DEVICE_DRIVER_INSTALL",
            _ => "MODIFY_SETTINGS"
        };
        string description = PowerShellText.Literal(request.Description);
        var step = new RepairStep(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command",
                "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';Checkpoint-Computer -Description " + description + " -RestorePointType '" + type + "' -ErrorAction Stop"], false);
        return BoundedTool.RunAsync(step, TimeSpan.FromMinutes(5), progress, token);
    }
}
