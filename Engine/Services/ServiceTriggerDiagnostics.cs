using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace KillerBench.Engine;

public sealed record ServiceTriggerObservation(string Type, string Action, uint DataItemCount);
public sealed record ServiceTriggerEntry(string Name, bool Readable, IReadOnlyList<ServiceTriggerObservation> Triggers);
public sealed record ServiceTriggerSnapshot(bool Complete, IReadOnlyList<ServiceTriggerEntry> Services);

public static class ServiceTriggerDiagnostics
{
    public const int MaximumTriggersPerService = 64;
    private static readonly HashSet<string> Types = new([
        "DeviceInterface", "IpAddress", "DomainJoin", "FirewallPort", "GroupPolicy", "NetworkEndpoint", "Custom"
    ], StringComparer.Ordinal);
    private static readonly HashSet<string> Actions = new(["Start", "Stop"], StringComparer.Ordinal);

    public static async Task<WorkflowCheck> CheckAsync(WorkflowCheck inventory, CancellationToken token = default,
        Func<IReadOnlyList<string>, CancellationToken, Task<ServiceTriggerSnapshot>>? collect = null)
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
            Win32Exception or TimeoutException or InvalidOperationException or NotSupportedException or OverflowException)
        {
            return Unknown(inventory.Evidence);
        }
    }

    public static WorkflowCheck Assess(ServiceTriggerSnapshot snapshot, ServiceInventorySnapshot installed, ToolResult? evidence = null)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(installed);
        if (!installed.Complete || !snapshot.Complete || snapshot.Services.Count != installed.Services.Count)
            return Unknown(evidence);
        var installedNames = installed.Services.Select(service => service.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var service in snapshot.Services)
        {
            if (!ValidName(service.Name) || !seen.Add(service.Name) || !installedNames.Contains(service.Name) || !service.Readable ||
                service.Triggers.Count > MaximumTriggersPerService || service.Triggers.Any(trigger =>
                    !Types.Contains(trigger.Type) || !Actions.Contains(trigger.Action) || trigger.DataItemCount > MaximumTriggersPerService))
                return Unknown(evidence);
        }
        if (seen.Count != installedNames.Count) return Unknown(evidence);
        int configured = snapshot.Services.Count(service => service.Triggers.Count > 0);
        int startServices = snapshot.Services.Count(service => service.Triggers.Any(trigger => trigger.Action == "Start"));
        int stopServices = snapshot.Services.Count(service => service.Triggers.Any(trigger => trigger.Action == "Stop"));
        var typeCounts = Types.OrderBy(type => type, StringComparer.Ordinal).Select(type =>
            (Type: type, Count: snapshot.Services.Sum(service => service.Triggers.Count(trigger => trigger.Type == type))))
            .Where(item => item.Count > 0).ToArray();
        string detail = $"Reviewed trigger configuration for {snapshot.Services.Count} installed services: {configured} have triggers, {startServices} have a start trigger and {stopServices} have a stop trigger.";
        if (typeCounts.Length > 0)
            detail += " Trigger counts by event type: " + string.Join(", ", typeCounts.Select(item => item.Type + " " + item.Count)) + ".";
        detail += " A stopped demand-start service with configured triggers can be idle while Windows waits for its event. Trigger configuration does not prove that an event occurred, a service started or stopped, or a feature works.";
        return new("Service triggers", CheckState.Information, detail + Scope, Evidence: evidence);
    }

    private const string Scope = " Trigger subtype identifiers and trigger-specific data, including device IDs, firewall ports, executable paths, named pipes and RPC interfaces, are excluded. No service, trigger, startup setting, executable, account, permission or policy changed.";
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
    private static WorkflowCheck Unknown(ToolResult? evidence) => new("Service triggers", CheckState.Unknown,
        "Complete installed-service and trigger evidence is required." + Scope, Evidence: evidence);

    private static class NativeCollector
    {
        private const uint ScManagerConnect = 0x0001;
        private const uint ServiceQueryConfig = 0x0001;
        private const uint TriggerInfo = 8;
        private const int ErrorInsufficientBuffer = 122;

        [StructLayout(LayoutKind.Sequential)]
        private struct TriggerInfoValue
        {
            public uint TriggerCount;
            public IntPtr Triggers;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TriggerValue
        {
            public uint Type;
            public uint Action;
            public IntPtr Subtype;
            public uint DataItemCount;
            public IntPtr DataItems;
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

        public static Task<ServiceTriggerSnapshot> CollectAsync(IReadOnlyList<string> names, CancellationToken token) =>
            Task.Run(() => Collect(names, token), token);

        private static ServiceTriggerSnapshot Collect(IReadOnlyList<string> names, CancellationToken token)
        {
            if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Service trigger inspection requires Windows.");
            IntPtr manager = OpenSCManagerW(null, null, ScManagerConnect);
            if (manager == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            try
            {
                var entries = new List<ServiceTriggerEntry>(names.Count);
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

        private static ServiceTriggerEntry Read(IntPtr manager, string name)
        {
            IntPtr service = OpenServiceW(manager, name, ServiceQueryConfig);
            if (service == IntPtr.Zero) return Unreadable(name);
            try
            {
                QueryServiceConfig2W(service, TriggerInfo, IntPtr.Zero, 0, out uint required);
                if (Marshal.GetLastWin32Error() != ErrorInsufficientBuffer || required == 0 || required > 1_048_576) return Unreadable(name);
                using var buffer = new NativeBuffer(required);
                if (!QueryServiceConfig2W(service, TriggerInfo, buffer.Pointer, required, out uint actual) || actual > required ||
                    buffer.Size < Marshal.SizeOf<TriggerInfoValue>()) return Unreadable(name);
                var info = Marshal.PtrToStructure<TriggerInfoValue>(buffer.Pointer);
                if (info.Reserved != IntPtr.Zero || info.TriggerCount > MaximumTriggersPerService ||
                    info.TriggerCount > 0 && info.Triggers == IntPtr.Zero) return Unreadable(name);
                int size = Marshal.SizeOf<TriggerValue>();
                long bytes = checked((long)info.TriggerCount * size);
                long start = buffer.Pointer.ToInt64();
                long end = checked(start + buffer.Size);
                long triggerStart = info.Triggers.ToInt64();
                if (info.TriggerCount > 0 && (triggerStart < start || triggerStart > end || bytes > end - triggerStart)) return Unreadable(name);
                var triggers = new List<ServiceTriggerObservation>((int)info.TriggerCount);
                for (int index = 0; index < info.TriggerCount; index++)
                {
                    var trigger = Marshal.PtrToStructure<TriggerValue>(IntPtr.Add(info.Triggers, checked(index * size)));
                    string type = trigger.Type switch
                    {
                        1 => "DeviceInterface", 2 => "IpAddress", 3 => "DomainJoin", 4 => "FirewallPort",
                        5 => "GroupPolicy", 6 => "NetworkEndpoint", 20 => "Custom", _ => ""
                    };
                    string action = trigger.Action switch { 1 => "Start", 2 => "Stop", _ => "" };
                    if (type.Length == 0 || action.Length == 0 || trigger.DataItemCount > MaximumTriggersPerService)
                        return Unreadable(name);
                    triggers.Add(new(type, action, trigger.DataItemCount));
                }
                return new(name, true, triggers.AsReadOnly());
            }
            finally { CloseServiceHandle(service); }
        }

        private static ServiceTriggerEntry Unreadable(string name) => new(name, false, []);
        private sealed class NativeBuffer : IDisposable
        {
            public IntPtr Pointer { get; private set; }
            public int Size { get; }
            public NativeBuffer(uint size) { Size = checked((int)size); Pointer = Marshal.AllocHGlobal(Size); }
            public void Dispose() { if (Pointer == IntPtr.Zero) return; Marshal.FreeHGlobal(Pointer); Pointer = IntPtr.Zero; }
        }
    }
}
