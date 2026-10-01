using System.Text.Json;

namespace KillerBench.Engine.Storage;

public enum VolumeOptimizationMode { Recommended, ReTrim, Defragment }
public enum VolumeMediaKind { HardDisk, SolidState, ThinProvisioned, TieredStorage, Unknown }
public enum VolumeOptimizationOutcome { Preview, Blocked, Verified, Failed, Uncertain, Stopped }

public sealed record VolumeOptimizationState(string Volume, string Identity, bool Ready, bool Fixed,
    string? FileSystem, string? HealthStatus, string? OperationalStatus, VolumeMediaKind Media);

public sealed record VolumeOptimizationRequest(string Volume, VolumeOptimizationMode Mode);

public sealed record VolumeOptimizationResult(VolumeOptimizationRequest Request, VolumeOptimizationOutcome Outcome,
    string Detail, VolumeOptimizationState? BeforeState = null, ToolResult? BeforeAnalysis = null,
    ToolResult? Command = null, VolumeOptimizationState? AfterState = null, ToolResult? AfterAnalysis = null,
    string? JournalPath = null)
{
    public int GetExitCode() => Outcome switch
    {
        VolumeOptimizationOutcome.Preview or VolumeOptimizationOutcome.Verified => 0,
        VolumeOptimizationOutcome.Stopped => 130,
        _ => 3
    };
}

public interface IVolumeOptimizationHost
{
    bool IsAdministrator { get; }
    Task<VolumeOptimizationState> InspectAsync(string volume, CancellationToken token);
    Task<ToolResult> AnalyzeAsync(string volume, IProgress<string>? progress, CancellationToken token);
    Task<ToolResult> OptimizeAsync(string volume, VolumeOptimizationMode mode, IProgress<string>? progress, CancellationToken token);
}

public static class VolumeOptimization
{
    private static readonly JsonSerializerOptions StrictJson = new(Capture.JsonOptions) { RespectRequiredConstructorParameters = true };
    private sealed record PendingRecord(int Schema, string Machine, DateTimeOffset StartedUtc, VolumeOptimizationRequest Request,
        VolumeOptimizationState BeforeState, ToolResult BeforeAnalysis, ToolResult? Command);

    public static VolumeOptimizationRequest CreateRequest(string volume, VolumeOptimizationMode mode)
    {
        if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        return new(StorageDiagnostics.NormalizeVolume(volume), mode);
    }

    public static WorkflowCheck ToWorkflowCheck(VolumeOptimizationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        bool available = result.Outcome == VolumeOptimizationOutcome.Preview && result.BeforeState is not null &&
            result.BeforeAnalysis is { ExitCode: 0, TimedOut: false, Truncated: false } evidence &&
            string.IsNullOrWhiteSpace(evidence.Error);
        return new("Selected-volume optimization", available ? CheckState.Repairable :
            result.BeforeState is null ? CheckState.Unknown : CheckState.Attention,
            result.Detail, available ? RepairKind.VolumeOptimization : null, result.BeforeAnalysis);
    }

    public static RepairStep CreateAnalysisStep(string volume) => CreateStep(volume, "-Analyze");

    public static RepairStep CreateOptimizationStep(string volume, VolumeOptimizationMode mode) => mode switch
    {
        VolumeOptimizationMode.Recommended => CreateStep(volume, null),
        VolumeOptimizationMode.ReTrim => CreateStep(volume, "-ReTrim"),
        VolumeOptimizationMode.Defragment => CreateStep(volume, "-Defrag"),
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    public static async Task<VolumeOptimizationResult> ReviewAsync(VolumeOptimizationRequest request,
        IVolumeOptimizationHost host, IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(host);
        request = CreateRequest(request.Volume, request.Mode);
        Report(progress, "Inspecting the selected volume and media type.");
        VolumeOptimizationState state;
        try { state = await Required(ct => host.InspectAsync(request.Volume, ct), "Volume inspection", token, TimeSpan.FromSeconds(20)).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(request, VolumeOptimizationOutcome.Blocked, "Volume eligibility could not be established: " + error.Message); }
        string? blocker = EligibilityBlocker(request, state);
        if (blocker is not null) return new(request, VolumeOptimizationOutcome.Blocked, blocker, state);
        Report(progress, "Analyzing " + request.Volume + " without changing it.");
        ToolResult analysis;
        try { analysis = await Required(ct => host.AnalyzeAsync(request.Volume, progress, ct), "Volume analysis", token, TimeSpan.FromMinutes(15)).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(request, VolumeOptimizationOutcome.Blocked, "Volume analysis was unavailable: " + error.Message, state); }
        return new(request, Complete(analysis) ? VolumeOptimizationOutcome.Preview : VolumeOptimizationOutcome.Blocked,
            Complete(analysis)
                ? Describe(request) + " Analysis completed. No optimization was performed."
                : "Volume analysis did not return complete successful evidence. No optimization was performed.",
            state, analysis);
    }

    public static async Task<VolumeOptimizationResult> RunAsync(VolumeOptimizationRequest request, bool execute,
        string journalFolder, IVolumeOptimizationHost host, IProgress<string>? progress = null,
        CancellationToken token = default, TimeSpan? operationTimeout = null)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        request = CreateRequest(request.Volume, request.Mode);
        TimeSpan limit = operationTimeout ?? TimeSpan.FromMinutes(65);
        if (limit <= TimeSpan.Zero || limit > TimeSpan.FromHours(2)) throw new ArgumentOutOfRangeException(nameof(operationTimeout));
        token.ThrowIfCancellationRequested();
        if (!execute)
            return new(request, VolumeOptimizationOutcome.Preview,
                Describe(request) + " Execution will reinspect and analyze the exact volume before changing it.");

        string root = Path.GetFullPath(journalFolder);
        Directory.CreateDirectory(root);
        using var gate = new FileStream(Path.Combine(root, "volume-optimization.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        string pendingPath = Path.Combine(root, "volume-optimization.pending.json");
        if (File.Exists(pendingPath))
            return new(request, VolumeOptimizationOutcome.Blocked,
                "A prior volume optimization needs reconciliation before another operation can start.", JournalPath: pendingPath);

        Report(progress, "Inspecting the selected volume and media type.");
        VolumeOptimizationState before;
        try { before = await Required(ct => host.InspectAsync(request.Volume, ct), "Volume inspection", token, TimeSpan.FromSeconds(20)).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(request, VolumeOptimizationOutcome.Blocked, "Volume eligibility could not be established: " + error.Message); }
        string? blocker = EligibilityBlocker(request, before);
        if (blocker is not null) return new(request, VolumeOptimizationOutcome.Blocked, blocker, before);
        if (!host.IsAdministrator)
            return new(request, VolumeOptimizationOutcome.Blocked, "Administrator access is required. No optimization was started.", before);

        Report(progress, "Analyzing " + request.Volume + " before optimization.");
        ToolResult analysis;
        try { analysis = await Required(ct => host.AnalyzeAsync(request.Volume, progress, ct), "Volume analysis", token, TimeSpan.FromMinutes(15)).ConfigureAwait(false); }
        catch (Exception error) when (!token.IsCancellationRequested && Unavailable(error))
        { return new(request, VolumeOptimizationOutcome.Blocked, "Pre-operation analysis was unavailable: " + error.Message, before); }
        if (!Complete(analysis))
            return new(request, VolumeOptimizationOutcome.Blocked,
                "A complete successful pre-operation analysis is required. No optimization was started.", before, analysis);

        var pending = new PendingRecord(1, Environment.MachineName, DateTimeOffset.UtcNow, request, before, analysis, null);
        await JournalPersistence.WriteAsync(pendingPath, JsonSerializer.Serialize(pending, Capture.JsonOptions), token).ConfigureAwait(false);
        string journalPath = Path.Combine(root, "volume-optimization-" + Guid.NewGuid().ToString("N") + ".json");
        ToolResult? command = null;
        bool optimizationDispatched = false;
        VolumeOptimizationResult result;
        try
        {
            Report(progress, "Confirming final selected-volume eligibility.");
            var current = await Required(ct => host.InspectAsync(request.Volume, ct), "Final volume inspection", token,
                TimeSpan.FromSeconds(20)).ConfigureAwait(false);
            var currentAnalysis = await Required(ct => host.AnalyzeAsync(request.Volume, progress, ct),
                "Final volume analysis", token, TimeSpan.FromMinutes(15)).ConfigureAwait(false);
            string? finalBlocker = EligibilityBlocker(request, current);
            if (finalBlocker is not null || !string.Equals(before.Identity, current.Identity, StringComparison.Ordinal) ||
                !Complete(currentAnalysis) || !host.IsAdministrator)
            {
                string detail = finalBlocker is not null
                    ? finalBlocker + " No optimization was started."
                    : !string.Equals(before.Identity, current.Identity, StringComparison.Ordinal)
                        ? "The selected volume identity changed after preparation. No optimization was started."
                        : !Complete(currentAnalysis)
                            ? "Final volume analysis was incomplete. No optimization was started."
                            : "Administrator access changed after preparation. No optimization was started.";
                result = new(request, VolumeOptimizationOutcome.Blocked, detail, before, analysis,
                    AfterState: current, AfterAnalysis: currentAnalysis, JournalPath: journalPath);
                await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
                File.Move(pendingPath, journalPath + ".prepared.json");
                Report(progress, result.Detail);
                return result;
            }
            Report(progress, "Running " + OperationName(request.Mode) + " on " + request.Volume + ".");
            optimizationDispatched = true;
            command = await Required(ct => host.OptimizeAsync(request.Volume, request.Mode, progress, ct),
                "Volume optimization", token, limit).ConfigureAwait(false);
            await JournalPersistence.WriteAsync(pendingPath,
                JsonSerializer.Serialize(pending with { Command = command }, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
            if (!Complete(command))
                result = new(request, command.TimedOut || command.Truncated ? VolumeOptimizationOutcome.Uncertain : VolumeOptimizationOutcome.Failed,
                    "Windows did not provide a complete successful optimization result. Reconcile before retrying.", before, analysis, command,
                    JournalPath: journalPath);
            else
                result = await VerifyAsync(request, before, analysis, command, journalPath, host, progress, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            result = new(request, VolumeOptimizationOutcome.Stopped,
                optimizationDispatched
                    ? "Optimization stopped after the request may have reached Windows. Reconcile before retrying."
                    : "Optimization stopped during final eligibility before any request was sent. No optimization was started.",
                before, analysis, command, JournalPath: journalPath);
        }
        catch (Exception error) when (Unavailable(error))
        {
            result = new(request, optimizationDispatched ? VolumeOptimizationOutcome.Uncertain : VolumeOptimizationOutcome.Blocked,
                optimizationDispatched
                    ? "Optimization was interrupted: " + error.Message + ". Reconcile before retrying."
                    : "Final optimization eligibility was unavailable: " + error.Message + ". No optimization was started.",
                before, analysis, command, JournalPath: journalPath);
        }
        await JournalPersistence.WriteAsync(journalPath, JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == VolumeOptimizationOutcome.Verified || !optimizationDispatched)
            File.Move(pendingPath, journalPath + ".prepared.json");
        Report(progress, result.Detail);
        return result;
    }

    public static async Task<VolumeOptimizationResult> ReconcileAsync(string journalFolder, IVolumeOptimizationHost host,
        IProgress<string>? progress = null, CancellationToken token = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(journalFolder);
        ArgumentNullException.ThrowIfNull(host);
        string root = Path.GetFullPath(journalFolder);
        string pendingPath = Path.Combine(root, "volume-optimization.pending.json");
        if (!File.Exists(pendingPath))
            return new(new("C:", VolumeOptimizationMode.Recommended), VolumeOptimizationOutcome.Blocked,
                "No pending volume optimization was found.");
        using var gate = new FileStream(Path.Combine(root, "volume-optimization.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        PendingRecord pending;
        try
        {
            var info = new FileInfo(pendingPath);
            if (!info.Exists || info.Length is <= 0 or > 262_144) throw new InvalidDataException("Pending optimization record size is invalid.");
            byte[] bytes = await File.ReadAllBytesAsync(pendingPath, token).ConfigureAwait(false);
            using var document = JsonDocument.Parse(bytes);
            if (!UniqueFields(document.RootElement)) throw new InvalidDataException("Pending optimization record contains conflicting fields.");
            pending = document.RootElement.Deserialize<PendingRecord>(StrictJson)
                ?? throw new InvalidDataException("Pending optimization record is empty.");
            if (!ValidPending(pending)) throw new InvalidDataException("Pending optimization record identity or evidence is invalid.");
        }
        catch (Exception error) when (error is IOException or JsonException or InvalidDataException or UnauthorizedAccessException or
            System.Security.SecurityException)
        {
            return new(new("C:", VolumeOptimizationMode.Recommended), VolumeOptimizationOutcome.Uncertain,
                "The pending optimization record is unavailable or invalid: " + error.Message +
                ". It was retained and no volume was inspected or changed.", JournalPath: pendingPath);
        }
        if (pending.Command is null || !Complete(pending.Command))
            return new(pending.Request, VolumeOptimizationOutcome.Uncertain,
                "The pending record does not prove that Windows completed the optimization. It was not repeated.",
                pending.BeforeState, pending.BeforeAnalysis, pending.Command, JournalPath: pendingPath);
        var result = await VerifyAsync(pending.Request, pending.BeforeState, pending.BeforeAnalysis, pending.Command,
            Path.Combine(root, "volume-optimization-recovered-" + Guid.NewGuid().ToString("N") + ".json"), host, progress, token).ConfigureAwait(false);
        await JournalPersistence.WriteAsync(result.JournalPath!, JsonSerializer.Serialize(result, Capture.JsonOptions), CancellationToken.None).ConfigureAwait(false);
        if (result.Outcome == VolumeOptimizationOutcome.Verified)
            File.Move(pendingPath, result.JournalPath + ".prepared.json");
        return result;
    }

    public static string? EligibilityBlocker(VolumeOptimizationRequest request, VolumeOptimizationState state)
    {
        if (!string.Equals(request.Volume, state.Volume, StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(state.Identity) ||
            state.Identity.Length > 512 || state.Identity.Any(char.IsControl)) return "The exact selected volume identity could not be confirmed.";
        if (!state.Ready || !state.Fixed) return "Only a ready, mounted fixed volume can be optimized.";
        if (!string.Equals(state.FileSystem, "NTFS", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(state.FileSystem, "ReFS", StringComparison.OrdinalIgnoreCase))
            return "Only NTFS and ReFS volumes are supported by this operation.";
        if (!string.Equals(state.HealthStatus, "Healthy", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(state.OperationalStatus, "OK", StringComparison.OrdinalIgnoreCase))
            return "The volume must report Healthy and OK before optimization. Diagnose storage health first.";
        if (state.Media == VolumeMediaKind.Unknown) return "The media type is unknown. KillerBench will not guess an optimization method.";
        if (request.Mode == VolumeOptimizationMode.ReTrim && state.Media is not (VolumeMediaKind.SolidState or VolumeMediaKind.ThinProvisioned))
            return "Retrim is limited to confirmed solid-state or thin-provisioned media.";
        if (request.Mode == VolumeOptimizationMode.Defragment && state.Media != VolumeMediaKind.HardDisk)
            return "Traditional defragmentation is limited to confirmed hard-disk media.";
        return null;
    }

    private static async Task<VolumeOptimizationResult> VerifyAsync(VolumeOptimizationRequest request,
        VolumeOptimizationState before, ToolResult beforeAnalysis, ToolResult command, string journalPath,
        IVolumeOptimizationHost host, IProgress<string>? progress, CancellationToken token)
    {
        Report(progress, "Reinspecting and analyzing the exact volume after optimization.");
        var after = await Required(ct => host.InspectAsync(request.Volume, ct), "Volume verification inspection", token, TimeSpan.FromSeconds(20)).ConfigureAwait(false);
        if (!string.Equals(before.Identity, after.Identity, StringComparison.Ordinal) || EligibilityBlocker(request, after) is not null)
            return new(request, VolumeOptimizationOutcome.Uncertain,
                "The exact volume identity or eligibility changed after the command. Completion is not verified.",
                before, beforeAnalysis, command, after, JournalPath: journalPath);
        var afterAnalysis = await Required(ct => host.AnalyzeAsync(request.Volume, progress, ct),
            "Volume verification analysis", token, TimeSpan.FromMinutes(15)).ConfigureAwait(false);
        bool verified = Complete(afterAnalysis);
        return new(request, verified ? VolumeOptimizationOutcome.Verified : VolumeOptimizationOutcome.Uncertain,
            verified
                ? "Windows completed the requested operation, and the same healthy volume completed a fresh analysis. This verifies execution and post-operation readability, not improved performance or physical media health."
                : "Windows completed the requested operation, but fresh analysis was incomplete. Completion is not verified.",
            before, beforeAnalysis, command, after, afterAnalysis, journalPath);
    }

    private static RepairStep CreateStep(string volume, string? operation)
    {
        char drive = StorageDiagnostics.NormalizeVolume(volume)[0];
        string command = "Optimize-Volume -DriveLetter '" + drive + "'" + (operation is null ? "" : " " + operation) + " -Verbose -Confirm:$false -ErrorAction Stop";
        return new(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command], true);
    }

    private static string Describe(VolumeOptimizationRequest request) => request.Mode switch
    {
        VolumeOptimizationMode.Recommended => "Windows will choose the supported optimization for " + request.Volume + " from the confirmed media type.",
        VolumeOptimizationMode.ReTrim => "Windows will issue retrim hints for unused sectors on " + request.Volume + ".",
        VolumeOptimizationMode.Defragment => "Windows will perform traditional defragmentation on " + request.Volume + ".",
        _ => throw new ArgumentOutOfRangeException(nameof(request))
    };

    private static string OperationName(VolumeOptimizationMode mode) => mode switch
    {
        VolumeOptimizationMode.Recommended => "Windows-recommended optimization",
        VolumeOptimizationMode.ReTrim => "retrim",
        VolumeOptimizationMode.Defragment => "traditional defragmentation",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private static bool Complete(ToolResult result) => result.ExitCode == 0 && !result.TimedOut && !result.Truncated && string.IsNullOrWhiteSpace(result.Error);
    private static bool ValidPending(PendingRecord pending)
    {
        if (pending.Schema != 1 || !string.Equals(pending.Machine, Environment.MachineName, StringComparison.OrdinalIgnoreCase) ||
            pending.StartedUtc == default || pending.Request is null || pending.BeforeState is null || pending.BeforeAnalysis is null ||
            !Enum.IsDefined(pending.Request.Mode) || !Complete(pending.BeforeAnalysis)) return false;
        try
        {
            return CreateRequest(pending.Request.Volume, pending.Request.Mode) == pending.Request &&
                EligibilityBlocker(pending.Request, pending.BeforeState) is null;
        }
        catch (ArgumentException) { return false; }
    }
    private static bool UniqueFields(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array) return value.EnumerateArray().All(UniqueFields);
        if (value.ValueKind != JsonValueKind.Object) return true;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return value.EnumerateObject().All(property => names.Add(property.Name) && UniqueFields(property.Value));
    }
    private static bool Unavailable(Exception error) => error is IOException or InvalidDataException or UnauthorizedAccessException or
        InvalidOperationException or NotSupportedException or TimeoutException or System.ComponentModel.Win32Exception or
        System.Security.SecurityException or OperationCanceledException;
    private static async Task<T> Required<T>(Func<CancellationToken, Task<T>> operation, string name,
        CancellationToken token, TimeSpan limit)
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

public sealed class WindowsVolumeOptimizationHost : IVolumeOptimizationHost
{
    public bool IsAdministrator => WindowsState.IsAdministrator();

    public async Task<VolumeOptimizationState> InspectAsync(string volume, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Volume optimization requires Windows.");
        char drive = StorageDiagnostics.NormalizeVolume(volume)[0];
        string script = "$ErrorActionPreference='Stop';$v=Get-Volume -DriveLetter '" + drive + "' -ErrorAction Stop;" +
            "$p=Get-Partition -DriveLetter '" + drive + "' -ErrorAction Stop;$d=$p|Get-Disk -ErrorAction Stop;" +
            "$pd=@(Get-PhysicalDisk -ErrorAction Stop|Where-Object {[string]$_.DeviceId -eq [string]$d.Number});" +
            "$media=if($pd.Count -eq 1){[string]$pd[0].MediaType}else{'Unknown'};" +
            "$kind=if($media -eq 'HDD'){0}elseif($media -eq 'SSD'){1}elseif([string]$d.ProvisioningType -eq 'Thin'){2}elseif([string]$d.BusType -eq 'Spaces'){3}else{4};" +
            "[pscustomobject]@{volume='" + char.ToUpperInvariant(drive) + ":';identity=[string]$v.UniqueId;ready=($null-ne$v.Path);fixed=([string]$v.DriveType-eq'Fixed');fileSystem=[string]$v.FileSystem;healthStatus=[string]$v.HealthStatus;operationalStatus=[string]$v.OperationalStatus;media=$kind}|ConvertTo-Json -Compress";
        var result = await BoundedTool.RunAsync(new(PrintRecovery.Tool,
            ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", script], true), TimeSpan.FromSeconds(20), token: token).ConfigureAwait(false);
        if (!CompleteNative(result)) throw new InvalidDataException("Volume inspection did not return complete evidence.");
        try
        {
            return JsonSerializer.Deserialize<VolumeOptimizationState>(result.Output, Capture.JsonOptions)
                ?? throw new InvalidDataException("Volume inspection returned no state.");
        }
        catch (JsonException error) { throw new InvalidDataException("Volume inspection returned invalid state.", error); }
    }

    public Task<ToolResult> AnalyzeAsync(string volume, IProgress<string>? progress, CancellationToken token) =>
        BoundedTool.RunAsync(VolumeOptimization.CreateAnalysisStep(volume), TimeSpan.FromMinutes(15), progress, token);

    public Task<ToolResult> OptimizeAsync(string volume, VolumeOptimizationMode mode, IProgress<string>? progress, CancellationToken token) =>
        BoundedTool.RunAsync(VolumeOptimization.CreateOptimizationStep(volume, mode), TimeSpan.FromMinutes(65), progress, token);

    private static bool CompleteNative(ToolResult result) => result.ExitCode == 0 && !result.TimedOut && !result.Truncated && string.IsNullOrWhiteSpace(result.Error);
}
