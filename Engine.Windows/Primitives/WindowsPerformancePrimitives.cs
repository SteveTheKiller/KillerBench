using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>Samples CPU use over a short window and reports the busiest process as a percentage of total capacity. Raw lists the top five by image name only.</summary>
public sealed class WindowsTopCpuProbe : IProbePrimitive
{
    public string Name => "process.top-cpu";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [new("seconds", false, "Sample window in seconds, 1 through 10 (default 3)") { Pattern = "[1-9]|10" }];

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        int seconds = int.TryParse(parameters.GetValueOrDefault("seconds"), NumberStyles.None, CultureInfo.InvariantCulture, out int requested) ? requested : 3;
        var first = Sample();
        await Task.Delay(TimeSpan.FromSeconds(seconds), token).ConfigureAwait(false);
        var second = Sample();
        double window = seconds * Environment.ProcessorCount;
        var usage = second
            .Where(pair => first.ContainsKey(pair.Key))
            .Select(pair => (pair.Key.Name, Percent: Math.Max(0, (pair.Value - first[pair.Key]).TotalSeconds) / window * 100))
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => (Name: group.Key, Percent: group.Sum(item => item.Percent)))
            .OrderByDescending(item => item.Percent).Take(5).ToArray();
        if (usage.Length == 0) return PrimitiveResult.Unavailable("No process could be sampled twice.");
        var raw = new StringBuilder();
        foreach (var item in usage) raw.Append(item.Name).Append(' ').Append(item.Percent.ToString("0.0", CultureInfo.InvariantCulture)).Append("%\n");
        var top = usage[0];
        return new PrimitiveResult(true, top.Percent.ToString("0.0", CultureInfo.InvariantCulture), raw.ToString())
        { Detail = "Busiest process over " + seconds + " s: " + top.Name + " at " + top.Percent.ToString("0.0", CultureInfo.InvariantCulture) + "% of total CPU." };
    }

    private static Dictionary<(int Id, string Name), TimeSpan> Sample()
    {
        var sample = new Dictionary<(int, string), TimeSpan>();
        foreach (var process in Process.GetProcesses())
        {
            try { sample[(process.Id, process.ProcessName)] = process.TotalProcessorTime; }
            catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException) { }
            finally { process.Dispose(); }
        }
        return sample;
    }
}

/// <summary>Reads the BCD boot sequence. Value is the one-time boot sequence entry list, or absent when none is set.</summary>
public sealed class WindowsBcdBootSequenceProbe : IProbePrimitive
{
    public string Name => "bcd.bootsequence";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("BCD access requires Windows.");
        var result = await NativeTool.RunAsync("bcdedit.exe", ["/enum", "{bootmgr}"], token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static PrimitiveResult Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new PrimitiveResult(false, null, raw, "bcdedit timed out.") { TimedOut = true };
        if (result.ExitCode != 0) return PrimitiveResult.Unavailable("bcdedit exited with " + result.ExitCode + "; administrator access is usually required.");
        if (!Regex.IsMatch(result.Output, @"(?m)^identifier\s+\{bootmgr\}\s*$", RegexOptions.CultureInvariant))
            return PrimitiveResult.Unavailable("bcdedit output did not describe the {bootmgr} entry.");
        var match = Regex.Match(result.Output, @"(?m)^bootsequence\s+(.+)$", RegexOptions.CultureInvariant);
        if (!match.Success) return new PrimitiveResult(true, null, raw) { Detail = "No one-time boot sequence is set." };
        string value = match.Groups[1].Value.Trim();
        return new PrimitiveResult(true, value, raw) { Detail = "A one-time boot sequence is set: " + value + "." };
    }
}

/// <summary>Schedules Windows Memory Diagnostic for the next boot through the BCD boot sequence.</summary>
public sealed class WindowsMemoryDiagnosticScheduleAction : IActionPrimitive
{
    public string Name => "memory-diagnostic.schedule";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => true;

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "BCD access requires Windows.");
        var result = await NativeTool.RunAsync("bcdedit.exe", ["/bootsequence", "{memdiag}"], progress: progress, token: token).ConfigureAwait(false);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new ActionResult(true, false, raw, "bcdedit timed out.") { TimedOut = true };
        bool ok = result.ExitCode == 0;
        return new ActionResult(true, ok, raw, ok ? null : "bcdedit exited with " + result.ExitCode + ".") { RestartRequired = ok };
    }
}

/// <summary>Ends every Explorer process for the current session and starts one shell. Open File Explorer windows close.</summary>
public sealed class WindowsExplorerRestartAction : IActionPrimitive
{
    public string Name => "explorer.restart";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    public RecipeRisk Risk => RecipeRisk.Reversible;
    public bool RequiresElevation => false;

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "Explorer restart requires Windows.");
        int ended = 0;
        try
        {
            foreach (var process in Process.GetProcessesByName("explorer"))
            {
                using (process)
                {
                    try { process.Kill(); ended++; }
                    catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception) { }
                }
            }
            await Task.Delay(1000, token).ConfigureAwait(false);
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            using var started = Process.Start(new ProcessStartInfo(path) { UseShellExecute = false });
            return new ActionResult(true, started is not null, "ended " + ended + "; started " + (started?.Id.ToString(CultureInfo.InvariantCulture) ?? "none"),
                started is null ? "Explorer did not start after " + ended + " process(es) were ended." : null);
        }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            return new ActionResult(ended > 0, false, "ended " + ended, "Explorer restart failed: " + error.Message);
        }
    }
}
