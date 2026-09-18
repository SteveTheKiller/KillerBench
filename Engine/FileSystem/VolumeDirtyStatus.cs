using System.Text.RegularExpressions;

namespace KillerBench.Engine;

public static class VolumeDirtyStatus
{
    public static RepairStep Step(string volume) => new("fsutil.exe", ["dirty", "query", StorageDiagnostics.NormalizeVolume(volume)], true);

    public static async Task<WorkflowCheck> CheckAsync(string volume, CancellationToken token = default)
    {
        volume = StorageDiagnostics.NormalizeVolume(volume);
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Volume dirty-bit inspection requires Windows.");
        return Interpret(volume, await BoundedTool.RunAsync(Step(volume), TimeSpan.FromSeconds(15), token: token).ConfigureAwait(false));
    }

    public static WorkflowCheck Interpret(string volume, ToolResult result)
    {
        volume = StorageDiagnostics.NormalizeVolume(volume);
        const string name = "Volume dirty bit";
        if (result is null || result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return new(name, CheckState.Unknown, $"Dirty-bit status for {volume} is unavailable or incomplete. No repair or restart was scheduled.", Evidence: result);
        var lines = result.Output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var matches = lines.Select(line => Regex.Match(line, @"^Volume - ([A-Za-z]:) is (NOT )?Dirty$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            .Where(match => match.Success).ToArray();
        if (matches.Length != 1 || !matches[0].Groups[1].Value.Equals(volume, StringComparison.OrdinalIgnoreCase))
            return new(name, CheckState.Unknown, $"Dirty-bit output did not identify exactly one result for {volume}. No repair or restart was scheduled.", Evidence: result);
        bool dirty = !matches[0].Groups[2].Success;
        return new(name, dirty ? CheckState.Attention : CheckState.Information,
            dirty
                ? $"Volume {volume} is marked dirty. Windows may run Autochk after restart. Back up important data and review current filesystem evidence before planning repair; nothing was scheduled."
                : $"Volume {volume} is not marked dirty. This flag alone does not establish filesystem or hardware health. No changes were made.",
            Evidence: result);
    }
}
