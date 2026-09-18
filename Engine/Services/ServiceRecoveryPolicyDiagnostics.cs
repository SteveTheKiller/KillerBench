using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record ServiceRecoveryAction(string Type, uint DelayMilliseconds);
public sealed record ServiceRecoveryPolicyEntry(string Name, bool Readable, uint ResetPeriodSeconds,
    bool AppliesToNonCrashFailures, IReadOnlyList<ServiceRecoveryAction> Actions);
public sealed record ServiceRecoveryPolicySnapshot(bool Complete, IReadOnlyList<ServiceRecoveryPolicyEntry> Services);

public static class ServiceRecoveryPolicyDiagnostics
{
    public const int MaximumActionsPerService = 16;
    private static readonly HashSet<string> ActionTypes = new(["None", "Restart", "Reboot", "RunCommand"], StringComparer.Ordinal);

    public static async Task<WorkflowCheck> CheckAsync(WorkflowCheck inventory, CancellationToken token = default,
        Func<IReadOnlyList<string>, CancellationToken, Task<ServiceRecoveryPolicySnapshot>>? collect = null)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        token.ThrowIfCancellationRequested();
        if (!TryInventory(inventory, out var installed)) return Unknown(inventory.Evidence);
        collect ??= NativeCollector.CollectAsync;
        try
        {
            var pending = collect(installed.Services.Select(service => service.Name).ToArray(), token);
            if (pending is null) return Unknown(inventory.Evidence);
            var snapshot = await pending.WaitAsync(token).ConfigureAwait(false);
            if (snapshot is null) return Unknown(inventory.Evidence);
            token.ThrowIfCancellationRequested();
            var evidence = new ToolResult(0, JsonSerializer.Serialize(snapshot, Capture.JsonOptions), "", false);
            return Assess(snapshot, installed, evidence);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or
            Win32Exception or TimeoutException or InvalidOperationException or NotSupportedException)
        {
            return Unknown(inventory.Evidence);
        }
    }

    public static WorkflowCheck Assess(ServiceRecoveryPolicySnapshot snapshot, ServiceInventorySnapshot installed, ToolResult? evidence = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(installed);
        const string scope = " Recovery commands and reboot messages are excluded. Configured actions do not prove a service is unhealthy or that recovery succeeded. No service, recovery action, startup setting, executable, account or permission changed.";
        if (!installed.Complete || !snapshot.Complete || snapshot.Services.Count != installed.Services.Count)
            return Unknown(evidence);
        var installedNames = installed.Services.Select(service => service.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var service in snapshot.Services)
        {
            if (!ValidName(service.Name) || !seen.Add(service.Name) || !installedNames.Contains(service.Name) || !service.Readable ||
                service.Actions.Count > MaximumActionsPerService || service.Actions.Any(action => !ActionTypes.Contains(action.Type)))
                return Unknown(evidence);
        }
        if (seen.Count != installedNames.Count) return Unknown(evidence);
        int none = snapshot.Services.Count(service => service.Actions.Count == 0 || service.Actions.All(action => action.Type == "None"));
        int restart = snapshot.Services.Count(service => service.Actions.Any(action => action.Type == "Restart"));
        int reboot = snapshot.Services.Count(service => service.Actions.Any(action => action.Type == "Reboot"));
        int command = snapshot.Services.Count(service => service.Actions.Any(action => action.Type == "RunCommand"));
        int nonCrash = snapshot.Services.Count(service => service.AppliesToNonCrashFailures);
        string detail = $"Reviewed recovery policy for {snapshot.Services.Count} installed services: {restart} configure service restart, {reboot} configure computer restart, {command} configure a recovery command, {nonCrash} apply actions to non-crash failures, and {none} have no effective action in the retained sequence.";
        return new("Service recovery policy", CheckState.Information, detail + scope, Evidence: evidence);
    }

    private static bool TryInventory(WorkflowCheck check, out ServiceInventorySnapshot snapshot)
    {
        snapshot = new(false, []);
        if (check.EffectiveState == CheckState.Unknown || check.Evidence is not { } evidence || evidence.ExitCode != 0 ||
            evidence.TimedOut || evidence.Truncated || !string.IsNullOrWhiteSpace(evidence.Error)) return false;
        try
        {
            snapshot = ServiceInventoryDiagnostics.Parse(evidence.Output);
            return snapshot.Complete;
        }
        catch (Exception error) when (error is JsonException or InvalidDataException or InvalidOperationException)
        {
            return false;
        }
    }

    private static bool ValidName(string name) => name.Length is > 0 and <= 256 && name == name.Trim() &&
        !name.Any(character => char.IsControl(character) || character is '\\' or '/');

    private static WorkflowCheck Unknown(ToolResult? evidence) => new("Service recovery policy", CheckState.Unknown,
        "Complete installed-service and recovery-policy evidence is required. Recovery commands and reboot messages are excluded. No service, recovery action, startup setting, executable, account or permission changed.", Evidence: evidence);

    private static class NativeCollector
    {
        private const uint ScManagerConnect = 0x0001;
        private const uint ServiceQueryConfig = 0x0001;
        private const uint FailureActions = 2;
        private const uint FailureActionsFlag = 4;
        private const int ErrorInsufficientBuffer = 122;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct FailureActionsValue
        {
            public uint ResetPeriod;
            public IntPtr RebootMessage;
            public IntPtr Command;
            public uint ActionCount;
            public IntPtr Actions;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ActionValue
        {
            public int Type;
            public uint Delay;
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenSCManagerW(string? machineName, string? databaseName, uint desiredAccess);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr OpenServiceW(IntPtr manager, string serviceName, uint desiredAccess);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryServiceConfig2W(IntPtr service, uint informationLevel, IntPtr buffer, uint bufferSize, out uint bytesNeeded);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr handle);

        public static Task<ServiceRecoveryPolicySnapshot> CollectAsync(IReadOnlyList<string> names, CancellationToken token) =>
            Task.Run(() => Collect(names, token), token);

        private static ServiceRecoveryPolicySnapshot Collect(IReadOnlyList<string> names, CancellationToken token)
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Service recovery policy requires Windows.");
            IntPtr manager = OpenSCManagerW(null, null, ScManagerConnect);
            if (manager == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                var entries = new List<ServiceRecoveryPolicyEntry>(names.Count);
                bool complete = true;
                foreach (string name in names)
                {
                    token.ThrowIfCancellationRequested();
                    var entry = Read(manager, name);
                    entries.Add(entry);
                    complete &= entry.Readable;
                }
                return new(complete, entries.AsReadOnly());
            }
            finally { CloseServiceHandle(manager); }
        }

        private static ServiceRecoveryPolicyEntry Read(IntPtr manager, string name)
        {
            IntPtr service = OpenServiceW(manager, name, ServiceQueryConfig);
            if (service == IntPtr.Zero) return Unreadable(name);
            try
            {
                if (!TryRead(service, FailureActions, out var actionsBuffer)) return Unreadable(name);
                using (actionsBuffer)
                {
                    if (actionsBuffer.Size < Marshal.SizeOf<FailureActionsValue>()) return Unreadable(name);
                    var value = Marshal.PtrToStructure<FailureActionsValue>(actionsBuffer.Pointer);
                    if (value.ActionCount > MaximumActionsPerService || (value.ActionCount > 0 && value.Actions == IntPtr.Zero)) return Unreadable(name);
                    var actions = new List<ServiceRecoveryAction>((int)value.ActionCount);
                    int size = Marshal.SizeOf<ActionValue>();
                    long actionBytes = checked((long)value.ActionCount * size);
                    long bufferStart = actionsBuffer.Pointer.ToInt64();
                    long bufferEnd = checked(bufferStart + actionsBuffer.Size);
                    long actionStart = value.Actions.ToInt64();
                    if (value.ActionCount > 0 && (actionStart < bufferStart || actionStart > bufferEnd || actionBytes > bufferEnd - actionStart))
                        return Unreadable(name);
                    for (int index = 0; index < value.ActionCount; index++)
                    {
                        var action = Marshal.PtrToStructure<ActionValue>(IntPtr.Add(value.Actions, checked(index * size)));
                        string type = action.Type switch { 0 => "None", 1 => "Restart", 2 => "Reboot", 3 => "RunCommand", _ => "" };
                        if (type.Length == 0) return Unreadable(name);
                        actions.Add(new(type, action.Delay));
                    }
                    if (!TryRead(service, FailureActionsFlag, out var flagBuffer)) return Unreadable(name);
                    using (flagBuffer)
                    {
                        if (flagBuffer.Size < sizeof(int)) return Unreadable(name);
                        bool nonCrash = Marshal.ReadInt32(flagBuffer.Pointer) != 0;
                        return new(name, true, value.ResetPeriod, nonCrash, actions.AsReadOnly());
                    }
                }
            }
            finally { CloseServiceHandle(service); }
        }

        private static bool TryRead(IntPtr service, uint level, out NativeBuffer buffer)
        {
            buffer = NativeBuffer.Empty;
            QueryServiceConfig2W(service, level, IntPtr.Zero, 0, out uint required);
            if (Marshal.GetLastWin32Error() != ErrorInsufficientBuffer || required == 0 || required > 1_048_576) return false;
            buffer = new(required);
            if (QueryServiceConfig2W(service, level, buffer.Pointer, required, out uint actual) && actual <= required) return true;
            buffer.Dispose();
            buffer = NativeBuffer.Empty;
            return false;
        }

        private static ServiceRecoveryPolicyEntry Unreadable(string name) => new(name, false, 0, false, []);

        private sealed class NativeBuffer : IDisposable
        {
            public static NativeBuffer Empty => new();
            public IntPtr Pointer { get; private set; }
            public int Size { get; }
            private NativeBuffer() { }
            public NativeBuffer(uint size)
            {
                Size = checked((int)size);
                Pointer = Marshal.AllocHGlobal(Size);
            }
            public void Dispose()
            {
                if (Pointer == IntPtr.Zero) return;
                Marshal.FreeHGlobal(Pointer);
                Pointer = IntPtr.Zero;
            }
        }
    }
}
