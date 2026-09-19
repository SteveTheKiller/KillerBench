using System.Globalization;
using System.Text.RegularExpressions;
using KillerBench.Engine.Core;

namespace KillerBench.Engine;

/// <summary>Known folders that maintenance primitives may count or clear. Packs name a token, never a path.</summary>
public static class WindowsMaintenanceFolders
{
    public const string TokenPattern = "recent-automatic-destinations|spool-printers";
    public static readonly PrimitiveParameter FolderParameter = new("folder", true, "Known folder token") { Pattern = TokenPattern };

    public static (string Path, string Filter)? Resolve(string? token) => token switch
    {
        "recent-automatic-destinations" => (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Recent", "AutomaticDestinations"), "*.automaticDestinations-ms"),
        "spool-printers" => (Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "spool", "PRINTERS"), "*"),
        _ => null
    };

    public static string RetainRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Retained");
}

/// <summary>Counts files in a known folder. Value is the count.</summary>
public sealed class WindowsFileCountProbe : IProbePrimitive
{
    public string Name => "file.count";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsMaintenanceFolders.FolderParameter];

    public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        token.ThrowIfCancellationRequested();
        if (WindowsMaintenanceFolders.Resolve(parameters.GetValueOrDefault("folder")) is not { } target) return Task.FromResult(PrimitiveResult.Unavailable("Unknown folder token."));
        try
        {
            if (!Directory.Exists(target.Path)) return Task.FromResult(new PrimitiveResult(true, "0", "absent: " + target.Path) { Detail = "The folder does not exist." });
            int count = Directory.EnumerateFiles(target.Path, target.Filter, SearchOption.TopDirectoryOnly).Take(10001).Count();
            if (count > 10000) return Task.FromResult(new PrimitiveResult(false, null, target.Path, "More than 10,000 files; not counted.") { Truncated = true });
            return Task.FromResult(new PrimitiveResult(true, count.ToString(CultureInfo.InvariantCulture), target.Path) { Detail = count + " file(s) in " + target.Path + "." });
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Task.FromResult(PrimitiveResult.Unavailable("Folder could not be read: " + error.Message));
        }
    }
}

/// <summary>Moves every matching file in a known folder to a retained folder under the local KillerBench data directory. Nothing is deleted.</summary>
public sealed class WindowsRetainFolderFilesAction : IActionPrimitive
{
    public string Name => "file.retain-folder";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsMaintenanceFolders.FolderParameter];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => false;

    public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        token.ThrowIfCancellationRequested();
        string tokenName = parameters.GetValueOrDefault("folder") ?? "";
        if (WindowsMaintenanceFolders.Resolve(tokenName) is not { } target) return Task.FromResult(new ActionResult(false, false, "", "Unknown folder token."));
        if (!Directory.Exists(target.Path)) return Task.FromResult(new ActionResult(false, true, "absent", "The folder does not exist; nothing to retain."));
        string retained = Path.Combine(WindowsMaintenanceFolders.RetainRoot, tokenName + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
        int moved = 0, failed = 0;
        try
        {
            Directory.CreateDirectory(retained);
            foreach (string file in Directory.EnumerateFiles(target.Path, target.Filter, SearchOption.TopDirectoryOnly).Take(10000).ToArray())
            {
                token.ThrowIfCancellationRequested();
                try { File.Move(file, Path.Combine(retained, Path.GetFileName(file)), false); moved++; }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { failed++; }
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return Task.FromResult(new ActionResult(moved > 0, false, "moved " + moved + " to " + retained, "Retention failed: " + error.Message));
        }
        if (moved == 0 && failed == 0) return Task.FromResult(new ActionResult(false, true, "nothing to move", "The folder held no matching files; nothing changed."));
        return Task.FromResult(new ActionResult(true, failed == 0, "moved " + moved + ", failed " + failed + ", retained under " + retained,
            failed == 0 ? null : failed + " file(s) were in use and stayed in place."));
    }
}

/// <summary>Stops the Print Spooler, retains every spool file under the local KillerBench data directory, and starts the spooler again. Queued jobs are lost from the queue but their files are kept.</summary>
public sealed class WindowsSpoolerClearAction : IActionPrimitive
{
    public string Name => "spooler.clear-spool";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => true;

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "Spooler changes require Windows.");
        var stop = await NativeTool.RunAsync("sc.exe", ["stop", "Spooler"], progress: progress, token: token).ConfigureAwait(false);
        if (stop.ExitCode is not (0 or 1062)) return new ActionResult(false, false, stop.Output, "The spooler could not be stopped (sc exit " + stop.ExitCode + "); no spool file was touched.");
        bool stopped = false;
        for (int i = 0; i < 40 && !stopped; i++)
        {
            var query = await NativeTool.RunAsync("sc.exe", ["query", "Spooler"], token: token).ConfigureAwait(false);
            stopped = ServiceRecovery.State(query) == 1;
            if (!stopped) await Task.Delay(500, token).ConfigureAwait(false);
        }
        if (!stopped) return new ActionResult(true, false, stop.Output, "The spooler did not reach the stopped state; no spool file was touched. Start it again if it stays stopped.");
        var retained = await new WindowsRetainFolderFilesAction().ExecuteAsync(new Dictionary<string, string> { ["folder"] = "spool-printers" }, timeout, progress, CancellationToken.None).ConfigureAwait(false);
        var start = await NativeTool.RunAsync("sc.exe", ["start", "Spooler"], progress: progress, token: CancellationToken.None).ConfigureAwait(false);
        bool started = start.ExitCode is 0 or 1056;
        string raw = "stop: " + stop.ExitCode + "; retain: " + retained.Raw + "; start: " + start.ExitCode;
        return new ActionResult(true, retained.Succeeded && started, raw,
            !started ? "Spool files were retained but the spooler did not start (sc exit " + start.ExitCode + ")." : retained.Error);
    }
}

/// <summary>Reports the IPv6 or IPv4 MTU of one interface through Get-NetIPInterface. Value is the MTU in bytes.</summary>
public sealed class WindowsInterfaceMtuProbe : IProbePrimitive
{
    public string Name => "netip.mtu";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsNetAdapterPrimitives.NameParameter, new("family", false, "IPv6 (default) or IPv4") { Pattern = "IPv6|IPv4" }];
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "& { param([string]$name,[string]$family) $i=@(Get-NetIPInterface -InterfaceAlias $name -AddressFamily $family -ErrorAction Stop); if($i.Count -ne 1){throw 'Exact interface not found.'}; [string]$i[0].NlMtu }";

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("Interface inspection requires Windows.");
        var result = await WindowsNetAdapterPrimitives.RunAsync(Script, token, parameters["name"], parameters.GetValueOrDefault("family") ?? "IPv6").ConfigureAwait(false);
        return Interpret(result);
    }

    public static PrimitiveResult Interpret(ToolResult result) => WindowsNetAdapterPrimitives.Lines(result, "Get-NetIPInterface", lines =>
        lines.Length == 1 && int.TryParse(lines[0], NumberStyles.None, CultureInfo.InvariantCulture, out int mtu) && mtu is >= 68 and <= 65535
            ? new PrimitiveResult(true, mtu.ToString(CultureInfo.InvariantCulture), lines[0]) { Detail = "The interface MTU is " + mtu + " bytes." }
            : PrimitiveResult.Unavailable("MTU output was not recognized."));
}

/// <summary>Sets the MTU of one interface through Set-NetIPInterface with a persistent store.</summary>
public sealed class WindowsInterfaceMtuSetAction : IActionPrimitive
{
    public string Name => "netip.mtu-set";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } =
        [WindowsNetAdapterPrimitives.NameParameter, new("family", false, "IPv6 (default) or IPv4") { Pattern = "IPv6|IPv4" }, new("mtu", true, "MTU in bytes, 1280 through 9000") { Pattern = "12[8-9][0-9]|1[3-9][0-9]{2}|[2-8][0-9]{3}|9000" }];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => true;
    private const string Script = "$ErrorActionPreference='Stop';$ProgressPreference='SilentlyContinue';" +
        "& { param([string]$name,[string]$family,[int]$mtu) Set-NetIPInterface -InterfaceAlias $name -AddressFamily $family -NlMtuBytes $mtu -PolicyStore PersistentStore -ErrorAction Stop; Set-NetIPInterface -InterfaceAlias $name -AddressFamily $family -NlMtuBytes $mtu -ErrorAction Stop; 'Set' }";

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "Interface changes require Windows.");
        var result = await WindowsNetAdapterPrimitives.RunAsync(Script, token, parameters["name"], parameters.GetValueOrDefault("family") ?? "IPv6", parameters["mtu"]).ConfigureAwait(false);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new ActionResult(true, false, raw, "Set-NetIPInterface timed out.") { TimedOut = true };
        if (result.ExitCode != 0) return new ActionResult(false, false, raw, "Set-NetIPInterface failed: " + result.Error.Trim());
        return new ActionResult(true, result.Output.Trim() == "Set", raw);
    }
}

/// <summary>Counts active wake timers reported by powercfg. Value is the count; Raw lists the owners.</summary>
public sealed class WindowsWakeTimersProbe : IProbePrimitive
{
    public string Name => "powercfg.wake-timers";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];

    public async Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return PrimitiveResult.Unavailable("powercfg requires Windows.");
        var result = await NativeTool.RunAsync("powercfg.exe", ["/waketimers"], token: token).ConfigureAwait(false);
        return Interpret(result);
    }

    public static PrimitiveResult Interpret(ToolResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        string raw = result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
        if (result.TimedOut) return new PrimitiveResult(false, null, raw, "powercfg timed out.") { TimedOut = true };
        if (result.ExitCode != 0) return PrimitiveResult.Unavailable("powercfg exited with " + result.ExitCode + "; administrator access is required.");
        if (Regex.IsMatch(result.Output, @"(?i)no active wake timers|There are no active wake timers"))
            return new PrimitiveResult(true, "0", raw) { Detail = "No active wake timers." };
        var owners = Regex.Matches(result.Output, @"(?m)^Timer set by \[(.+?)\]", RegexOptions.CultureInvariant);
        if (owners.Count == 0) return PrimitiveResult.Unavailable("powercfg output was not recognized.");
        return new PrimitiveResult(true, owners.Count.ToString(CultureInfo.InvariantCulture), string.Join("\n", owners.Select(match => match.Groups[1].Value)))
        { Detail = owners.Count + " active wake timer(s) can wake the PC from sleep." };
    }
}
