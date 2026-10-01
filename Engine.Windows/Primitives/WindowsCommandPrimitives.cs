using System.Diagnostics;
using System.Text.RegularExpressions;
using KillerBench.Engine.Core;

namespace KillerBench.Engine.Windows.Primitives;

/// <summary>Reads the current boot entry's safeboot setting. Value is the mode (Minimal, Network, DsRepair) or absent when Windows will boot normally.</summary>
public sealed partial class WindowsBcdSafebootProbe : IProbePrimitive
{
    [GeneratedRegex(@"(?m)^identifier\s+\{current\}\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex CurrentIdentifierPattern();

    [GeneratedRegex(@"(?m)^safeboot\s+(\S+)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex SafebootPattern();

    public string Name => "bcd.safeboot";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("BCD access requires Windows.");
        var result = await NativeTool.RunAsync("bcdedit.exe", ["/enum", "{current}"], token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static PrimitiveResult Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new PrimitiveResult(false, null, raw, "bcdedit timed out.") { TimedOut = true };
        if (result.Truncated) return new PrimitiveResult(false, null, raw, "bcdedit output was truncated.") { Truncated = true };
        if (result.ExitCode != 0) return PrimitiveResult.Unavailable("bcdedit exited with " + result.ExitCode + "; administrator access is usually required.");
        if (!CurrentIdentifierPattern().IsMatch(result.Output))
            return PrimitiveResult.Unavailable("bcdedit output did not describe the {current} entry.");
        var matches = SafebootPattern().Matches(result.Output);
        if (matches.Count > 1) return PrimitiveResult.Unavailable("bcdedit reported more than one safeboot value.");
        if (matches.Count == 0) return new PrimitiveResult(true, null, raw) { Detail = "No safeboot value is set on the current boot entry." };
        string mode = matches[0].Groups[1].Value;
        return new PrimitiveResult(true, mode, raw) { Detail = "The current boot entry is set to boot into Safe Mode (" + mode + ")." };
    }
}

/// <summary>Deletes the safeboot value from the current boot entry so the next boot is normal.</summary>
public sealed class WindowsBcdSafebootClearAction : IActionPrimitive
{
    public string Name => "bcd.safeboot-clear";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => true;

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "BCD access requires Windows.");
        var result = await NativeTool.RunAsync("bcdedit.exe", ["/deletevalue", "{current}", "safeboot"], progress: progress, token: token).ConfigureAwait(false);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new ActionResult(true, false, raw, "bcdedit timed out.") { TimedOut = true };
        return new ActionResult(true, result.ExitCode == 0, raw, result.ExitCode == 0 ? null : "bcdedit exited with " + result.ExitCode + ".");
    }
}

/// <summary>Resets the Winsock catalog. Windows requires a restart before the reset takes effect.</summary>
public sealed class WindowsWinsockResetAction : IActionPrimitive
{
    public string Name => "winsock.reset";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => true;

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "Winsock reset requires Windows.");
        var result = await NativeTool.RunAsync("netsh.exe", ["winsock", "reset"], progress: progress, token: token).ConfigureAwait(false);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new ActionResult(true, false, raw, "netsh timed out.") { TimedOut = true };
        bool ok = result.ExitCode == 0;
        return new ActionResult(true, ok, raw, ok ? null : "netsh exited with " + result.ExitCode + ".") { RestartRequired = ok };
    }
}

/// <summary>Reports whether at least one process with the given image name is running for any account. Value is Running or NotRunning.</summary>
public sealed class WindowsProcessRunningProbe : IProbePrimitive
{
    public string Name => "process.running";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [new("name", true, "Process image name without extension") { Pattern = "[A-Za-z0-9_-]{1,64}" }];

    public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        token.ThrowIfCancellationRequested();
        string name = parameters["name"];
        try
        {
            var processes = Process.GetProcessesByName(name);
            int count = processes.Length;
            foreach (var process in processes) process.Dispose();
            return Task.FromResult(new PrimitiveResult(true, count > 0 ? "Running" : "NotRunning", count + " process(es) named " + name)
            { Detail = count > 0 ? count + " " + name + " process(es) running." : "No " + name + " process is running." });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return Task.FromResult(PrimitiveResult.Unavailable("Process inventory failed: " + exception.Message));
        }
    }
}

/// <summary>Starts the Windows shell for the current session when no Explorer process is running. It never terminates an existing shell.</summary>
public sealed class WindowsExplorerStartAction : IActionPrimitive
{
    public string Name => "explorer.start";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    public RecipeRisk Risk => RecipeRisk.Reversible;
    public bool RequiresElevation => false;

    public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!OperatingSystem.IsWindows()) return Task.FromResult(new ActionResult(false, false, "", "Explorer start requires Windows."));
        try
        {
            var existing = Process.GetProcessesByName("explorer");
            int running = existing.Length;
            foreach (var process in existing) process.Dispose();
            if (running > 0) return Task.FromResult(new ActionResult(false, true, running + " explorer process(es) present", "Explorer is already running; nothing was started."));
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
            using var started = Process.Start(new ProcessStartInfo(path) { UseShellExecute = false });
            return Task.FromResult(new ActionResult(true, started is not null, started is null ? "" : "started pid " + started.Id, started is null ? "Explorer did not start." : null));
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception or IOException)
        {
            return Task.FromResult(new ActionResult(false, false, "", "Explorer could not be started: " + exception.Message));
        }
    }
}
