using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using KillerBench.Engine.Core;

namespace KillerBench.Engine.Windows.Primitives;

/// <summary>Reports the process with the largest working set as a percentage of physical memory. Raw lists the top five by image name only.</summary>
public sealed class WindowsTopMemoryProbe : IProbePrimitive
{
    private readonly Func<long> physicalMemory;
    private readonly Func<IReadOnlyDictionary<string, long>> processWorkingSets;
    public WindowsTopMemoryProbe(Func<long>? physicalMemory = null, Func<IReadOnlyDictionary<string, long>>? processWorkingSets = null)
    {
        this.physicalMemory = physicalMemory ?? ReadPhysicalMemory;
        this.processWorkingSets = processWorkingSets ?? ReadProcessWorkingSets;
    }

    public string Name => "process.top-memory";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];

    public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        long total;
        try { total = physicalMemory(); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        { return Task.FromResult(PrimitiveResult.Unavailable("Physical memory size is unavailable: " + error.Message)); }
        if (total <= 0) return Task.FromResult(PrimitiveResult.Unavailable("Physical memory size is unavailable."));
        var usage = processWorkingSets();
        if (usage.Count == 0) return Task.FromResult(PrimitiveResult.Unavailable("No process working set could be read."));
        var top = usage.OrderByDescending(pair => pair.Value).Take(5).ToArray();
        var raw = new StringBuilder();
        foreach (var pair in top) raw.Append(pair.Key).Append(' ').Append((pair.Value / 1048576.0).ToString("0", CultureInfo.InvariantCulture)).Append(" MB\n");
        double percent = top[0].Value * 100.0 / total;
        return Task.FromResult(new PrimitiveResult(true, percent.ToString("0.0", CultureInfo.InvariantCulture), raw.ToString())
        { Detail = "Largest working set: " + top[0].Key + " at " + (top[0].Value / 1048576.0).ToString("0", CultureInfo.InvariantCulture) + " MB (" + percent.ToString("0.0", CultureInfo.InvariantCulture) + "% of physical memory)." });
    }

    private static IReadOnlyDictionary<string, long> ReadProcessWorkingSets()
    {
        var usage = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try { usage[process.ProcessName] = usage.GetValueOrDefault(process.ProcessName) + process.WorkingSet64; }
                catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException) { }
            }
        }
        return usage;
    }

    private static long ReadPhysicalMemory()
    {
        if (!OperatingSystem.IsWindows()) return 0;
        var status = new MemoryStatus { Length = (uint)Marshal.SizeOf<MemoryStatus>() };
        return GlobalMemoryStatusEx(ref status) && status.TotalPhysical <= long.MaxValue ? (long)status.TotalPhysical : 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatus
    {
        public uint Length, Load;
        public ulong TotalPhysical, AvailablePhysical, TotalPageFile, AvailablePageFile, TotalVirtual, AvailableVirtual, AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatus status);
}

/// <summary>Samples total physical disk busy time through typeperf. Value is the average percent over the sample.</summary>
public sealed partial class WindowsDiskBusyProbe : IProbePrimitive
{
    [GeneratedRegex("^\"[^\"]+\",\"([0-9.]+)\"$", RegexOptions.CultureInvariant)]
    private static partial Regex SamplePattern();

    public string Name => "disk.busy-percent";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [new("samples", false, "Number of one-second samples, 2 through 10 (default 3)") { Pattern = "[2-9]|10" }];

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Disk sampling requires Windows.");
        string samples = parameters.GetValueOrDefault("samples") ?? "3";
        var result = await NativeTool.RunAsync("typeperf.exe", [@"\PhysicalDisk(_Total)\% Disk Time", "-sc", samples, "-si", "1"], token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static PrimitiveResult Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.Truncated) return new PrimitiveResult(false, null, raw, "typeperf output was truncated.") { Truncated = true, TimedOut = result.TimedOut };
        if (result.TimedOut) return new PrimitiveResult(false, null, raw, "typeperf timed out.") { TimedOut = true };
        if (result.ExitCode != 0) return PrimitiveResult.Unavailable("typeperf exited with " + result.ExitCode + ".");
        var values = new List<double>();
        foreach (string line in result.Output.Split('\n'))
        {
            var match = SamplePattern().Match(line.Trim());
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) values.Add(value);
        }
        if (values.Count == 0) return PrimitiveResult.Unavailable("typeperf output held no samples.");
        double average = Math.Min(100, values.Average());
        return new PrimitiveResult(true, average.ToString("0.0", CultureInfo.InvariantCulture), raw)
        { Detail = "Physical disks averaged " + average.ToString("0", CultureInfo.InvariantCulture) + "% busy over " + values.Count + " sample(s)." };
    }
}

/// <summary>Counts failed VSS writers, distinguishing normal backup transitions and unknown states.</summary>
public sealed partial class WindowsVssWritersProbe : IProbePrimitive
{
    private readonly Func<CancellationToken, Task<ToolResult>> runCommand;
    public WindowsVssWritersProbe(Func<CancellationToken, Task<ToolResult>>? runCommand = null) =>
        this.runCommand = runCommand ?? (token => NativeTool.RunAsync("vssadmin.exe", ["list", "writers"], token: token));

    [GeneratedRegex(@"(?m)^Writer name:\s*'([^']+)'", RegexOptions.CultureInvariant)]
    private static partial Regex WriterNamePattern();

    [GeneratedRegex(@"(?m)^\s*State:\s*\[(\d+)\]\s*(.+?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex WriterStatePattern();

    public string Name => "vss.unstable-writers";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("VSS inspection requires Windows.");
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(timeout);
        var evidence = new List<string>();
        try
        {
            for (int sample = 0; ; sample++)
            {
                var observed = Interpret(await runCommand(deadline.Token).WaitAsync(deadline.Token).ConfigureAwait(false));
                evidence.Add(observed.Raw);
                if (!observed.Complete || observed.Value != "InProgress" || sample == 2)
                    return observed with { Raw = string.Join("\n", evidence) };
                await Task.Delay(TimeSpan.FromMilliseconds(250), deadline.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            return new PrimitiveResult(false, null, string.Join("\n", evidence), "VSS observation exceeded its time limit; writer state is unavailable.") { TimedOut = true };
        }
    }

    public static PrimitiveResult Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.Truncated) return new PrimitiveResult(false, null, raw, "vssadmin output was truncated.") { Truncated = true, TimedOut = result.TimedOut };
        if (result.TimedOut) return new PrimitiveResult(false, null, raw, "vssadmin timed out.") { TimedOut = true };
        if (result.ExitCode != 0) return PrimitiveResult.Unavailable("vssadmin exited with " + result.ExitCode + "; administrator access is required.");
        var names = WriterNamePattern().Matches(result.Output);
        var states = WriterStatePattern().Matches(result.Output);
        if (names.Count == 0 || names.Count != states.Count) return PrimitiveResult.Unavailable("vssadmin output did not pair every writer with a state.");
        int failed = 0, active = 0;
        var summary = new StringBuilder();
        for (int i = 0; i < names.Count; i++)
        {
            if (!int.TryParse(states[i].Groups[1].Value, out int state) || state is < 1 or > 15)
                return new PrimitiveResult(false, null, raw, "A VSS writer reported an unknown state; backup health is unavailable.");
            if (state >= 6) failed++;
            else if (state >= 2) active++;
            summary.Append(names[i].Groups[1].Value).Append(": ").Append(states[i].Groups[2].Value).Append('\n');
        }
        return new PrimitiveResult(true, failed > 0 ? failed.ToString(CultureInfo.InvariantCulture) : active > 0 ? "InProgress" : "0", summary.ToString())
        { Detail = failed > 0 ? failed + " of " + names.Count + " VSS writers report a documented failure. Review the backup and writer event logs before taking action."
            : active > 0 ? active + " VSS writer(s) are participating in backup activity. Allow the backup to finish and check again."
            : "All " + names.Count + " VSS writers are stable." };
    }
}

/// <summary>Reads shadow storage use for each volume. Value is the highest used percentage of its maximum; unlimited maximums count as 0.</summary>
public sealed partial class WindowsShadowStorageProbe : IProbePrimitive
{
    [GeneratedRegex(@"(?m)^\s*Used Shadow Copy Storage space:\s*(.+?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex UsedStoragePattern();

    [GeneratedRegex(@"(?m)^\s*Maximum Shadow Copy Storage space:\s*(.+?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex MaximumStoragePattern();

    [GeneratedRegex(@"^([0-9]+(?:\.[0-9]+)?)\s*(bytes|KB|MB|GB|TB)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex SizePattern();

    public string Name => "vss.shadowstorage-percent";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("VSS inspection requires Windows.");
        var result = await NativeTool.RunAsync("vssadmin.exe", ["list", "shadowstorage"], token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static PrimitiveResult Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.Truncated) return new PrimitiveResult(false, null, raw, "vssadmin output was truncated.") { Truncated = true, TimedOut = result.TimedOut };
        if (result.TimedOut) return new PrimitiveResult(false, null, raw, "vssadmin timed out.") { TimedOut = true };
        if (result.ExitCode != 0) return PrimitiveResult.Unavailable("vssadmin exited with " + result.ExitCode + "; administrator access is required.");
        if (result.Output.Contains("No items found", StringComparison.OrdinalIgnoreCase))
            return new PrimitiveResult(true, "0", raw) { Detail = "No shadow storage associations exist." };
        var used = UsedStoragePattern().Matches(result.Output);
        var maximum = MaximumStoragePattern().Matches(result.Output);
        if (used.Count == 0 || used.Count != maximum.Count) return PrimitiveResult.Unavailable("vssadmin output did not pair used and maximum storage for every association.");
        int highest = 0;
        var summary = new StringBuilder();
        for (int i = 0; i < used.Count; i++)
        {
            bool unbounded = maximum[i].Groups[1].Value.StartsWith("UNBOUNDED", StringComparison.OrdinalIgnoreCase);
            if (!Bytes(used[i].Groups[1].Value, out double usedBytes) || (!unbounded && !Bytes(maximum[i].Groups[1].Value, out _)))
                return PrimitiveResult.Unavailable("vssadmin storage sizes were not recognized.");
            int percent = 0;
            if (!unbounded && Bytes(maximum[i].Groups[1].Value, out double maximumBytes) && maximumBytes > 0)
                percent = (int)Math.Min(100, Math.Round(usedBytes * 100 / maximumBytes));
            highest = Math.Max(highest, percent);
            summary.Append("association ").Append(i + 1).Append(": ").Append(unbounded ? "unbounded" : percent + "% of maximum").Append('\n');
        }
        return new PrimitiveResult(true, highest.ToString(CultureInfo.InvariantCulture), summary.ToString())
        { Detail = "Highest shadow storage use is " + highest + "% of its maximum across " + used.Count + " association(s)." };
    }

    private static bool Bytes(string text, out double bytes)
    {
        bytes = 0;
        var match = SizePattern().Match(text);
        if (!match.Success || !double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number)) return false;
        double scale = match.Groups[2].Value.ToUpperInvariant() switch { "KB" => 1024, "MB" => 1048576, "GB" => 1073741824, "TB" => 1099511627776, _ => 1 };
        bytes = number * scale;
        return true;
    }
}
