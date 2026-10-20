using System.Globalization;
using System.Text.RegularExpressions;
using KillerBench.Engine.Core;

namespace KillerBench.Engine.Windows.Primitives;

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
    private readonly Func<string?, (string Path, string Filter)?> resolveFolder;
    private readonly Func<string> retainRoot;
    private readonly Action<string, string> moveFile;

    public WindowsRetainFolderFilesAction(
        Func<string?, (string Path, string Filter)?>? resolveFolder = null,
        Func<string>? retainRoot = null,
        Action<string, string>? moveFile = null)
    {
        this.resolveFolder = resolveFolder ?? WindowsMaintenanceFolders.Resolve;
        this.retainRoot = retainRoot ?? (() => WindowsMaintenanceFolders.RetainRoot);
        this.moveFile = moveFile ?? ((source, destination) => File.Move(source, destination, false));
    }

    public string Name => "file.retain-folder";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [WindowsMaintenanceFolders.FolderParameter];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => false;

    public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        token.ThrowIfCancellationRequested();
        string tokenName = parameters.GetValueOrDefault("folder") ?? "";
        if (resolveFolder(tokenName) is not { } target) return Task.FromResult(new ActionResult(false, false, "", "Unknown folder token."));
        if (!Directory.Exists(target.Path)) return Task.FromResult(new ActionResult(false, true, "absent", "The folder does not exist; nothing to retain."));
        string retained = Path.Combine(retainRoot(), tokenName + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture));
        int moved = 0, failed = 0;
        try
        {
            Directory.CreateDirectory(retained);
            foreach (string file in Directory.EnumerateFiles(target.Path, target.Filter, SearchOption.TopDirectoryOnly).Take(10000).ToArray())
            {
                token.ThrowIfCancellationRequested();
                try { moveFile(file, Path.Combine(retained, Path.GetFileName(file))); moved++; }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { failed++; }
            }
        }
        catch (OperationCanceledException)
        {
            return Task.FromResult(new ActionResult(moved > 0, false,
                "moved " + moved + ", failed " + failed + ", retained under " + retained,
                "Retention was canceled; no further files were moved."));
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
    private readonly Func<string, IProgress<string>?, CancellationToken, Task<ToolResult>> runCommand;
    private readonly Func<TimeSpan, IProgress<string>?, CancellationToken, Task<ActionResult>> retainFiles;

    public WindowsSpoolerClearAction(
        Func<string, IProgress<string>?, CancellationToken, Task<ToolResult>>? runCommand = null,
        Func<TimeSpan, IProgress<string>?, CancellationToken, Task<ActionResult>>? retainFiles = null)
    {
        this.runCommand = runCommand ?? ((operation, progress, token) => NativeTool.RunAsync("sc.exe", [operation, "Spooler"], progress: progress, token: token));
        this.retainFiles = retainFiles ?? ((timeout, progress, token) => new WindowsRetainFolderFilesAction().ExecuteAsync(
            new Dictionary<string, string> { ["folder"] = "spool-printers" }, timeout, progress, token));
    }

    public string Name => "spooler.clear-spool";
    public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
    public RecipeRisk Risk => RecipeRisk.Persistent;
    public bool RequiresElevation => true;

    public async Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, IProgress<string>? progress, CancellationToken token)
    {
        if (!OperatingSystem.IsWindows()) return new ActionResult(false, false, "", "Spooler changes require Windows.");
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        token.ThrowIfCancellationRequested();
        TimeSpan limit = timeout < TimeSpan.FromSeconds(20) ? timeout : TimeSpan.FromSeconds(20);
        var evidence = new Dictionary<string, string>();
        string? failure = null;
        bool restore = false, started = false, timedOut = false;
        ActionResult? retained = null;
        string stage = "Stopping the Spooler";
        try
        {
            using var stopDeadline = new CancellationTokenSource(limit);
            // Once dispatched, a canceled or incomplete stop may still have reached the service.
            restore = true;
            var stop = await CommandAsync("stop", progress, stopDeadline.Token).ConfigureAwait(false);
            evidence["stop"] = Describe(stop);
            timedOut |= stop.TimedOut;
            if (stop.ExitCode is not (0 or 1062) && !stop.TimedOut && !stop.Truncated)
            {
                restore = false;
                return new ActionResult(false, false, Raw(), "The Spooler stop was rejected; no spool file was touched.");
            }
            if (!Complete(stop)) throw new IOException("The Spooler stop returned incomplete evidence.");
            stage = "Confirming the stopped Spooler";
            using (var observation = new CancellationTokenSource(limit))
                await ObserveAsync(1, "stopped-state", observation.Token).ConfigureAwait(false);

            stage = "Retaining spool files";
            token.ThrowIfCancellationRequested();
            using var retentionDeadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            retentionDeadline.CancelAfter(limit);
            retained = await retainFiles(timeout, progress, retentionDeadline.Token).WaitAsync(retentionDeadline.Token).ConfigureAwait(false);
            if (retentionDeadline.IsCancellationRequested && !token.IsCancellationRequested)
                retained = retained with { TimedOut = true, Error = (retained.Error ?? "") + " The retention time limit expired." };
            evidence["retain"] = retained.Raw + (string.IsNullOrWhiteSpace(retained.Error) ? "" : "\n" + retained.Error);
            timedOut |= retained.TimedOut;
            if (!retained.Succeeded || retained.TimedOut) failure = "Spool file retention did not complete: " + (retained.Error ?? "no complete result was returned.");
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or OperationCanceledException or System.ComponentModel.Win32Exception)
        {
            timedOut |= error is OperationCanceledException && !token.IsCancellationRequested;
            failure = stage + " failed: " + (error is OperationCanceledException
                ? token.IsCancellationRequested ? "the operation was canceled; no further spool files were retained." : "the operation exceeded its time limit."
                : error.Message);
        }
        finally
        {
            if (restore)
            {
                try
                {
                    // Restoration has its own deadline and outlives the caller's cancellation.
                    using var restoration = new CancellationTokenSource(limit);
                    await RestoreAsync(restoration.Token).ConfigureAwait(false);
                    started = true;
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or OperationCanceledException or System.ComponentModel.Win32Exception)
                {
                    timedOut |= error is OperationCanceledException;
                    failure = (failure is null ? "" : failure + " ") + "Spooler restoration was not verified: "
                        + (error is OperationCanceledException ? "the restoration exceeded its time limit." : error.Message);
                }
            }
        }
        return new ActionResult(true, retained is { Succeeded: true } && started && failure is null, Raw(), failure) { TimedOut = timedOut };

        async Task<ToolResult> CommandAsync(string operation, IProgress<string>? output, CancellationToken commandToken)
            => await runCommand(operation, output, commandToken).WaitAsync(commandToken).ConfigureAwait(false);

        async Task ObserveAsync(int desired, string key, CancellationToken observationToken)
        {
            int attempt = 0;
            while (true)
            {
                var query = await CommandAsync("query", null, observationToken).ConfigureAwait(false);
                evidence[key + "-" + ++attempt] = Describe(query);
                timedOut |= query.TimedOut;
                if (!Complete(query) || !ServiceRecovery.MatchesService(query, "Spooler")) throw new IOException("The exact Spooler state could not be confirmed.");
                if (ServiceRecovery.State(query) == desired) return;
                await Task.Delay(TimeSpan.FromMilliseconds(500), observationToken).ConfigureAwait(false);
            }
        }

        async Task RestoreAsync(CancellationToken restorationToken)
        {
            var start = await StartAsync("start").ConfigureAwait(false);
            bool correctiveAllowed = start.ExitCode is 1056 or 1061, corrected = false;
            int attempt = 0;
            while (true)
            {
                var query = await CommandAsync("query", null, restorationToken).ConfigureAwait(false);
                evidence["running-state-" + ++attempt] = Describe(query);
                timedOut |= query.TimedOut;
                if (!Complete(query) || !ServiceRecovery.MatchesService(query, "Spooler")) throw new IOException("The exact Spooler state could not be confirmed.");
                int? state = ServiceRecovery.State(query);
                if (state == 4) return;
                if (state == 3 && !corrected) correctiveAllowed = true;
                if (state == 1 && correctiveAllowed && !corrected)
                {
                    // The accepted stop has now finished. Permit one start after STOPPED under
                    // this same deadline, preserving the earlier response and every state query.
                    corrected = true;
                    await StartAsync("start-after-stopped").ConfigureAwait(false);
                    continue;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(500), restorationToken).ConfigureAwait(false);
            }

            async Task<ToolResult> StartAsync(string key)
            {
                var result = await CommandAsync("start", progress, restorationToken).ConfigureAwait(false);
                evidence[key] = Describe(result);
                timedOut |= result.TimedOut;
                if (!Complete(result) || result.ExitCode is not (0 or 1056 or 1061)) throw new IOException("The start command failed or returned incomplete evidence: " + Describe(result));
                return result;
            }
        }

        string Raw() => string.Join("\n", evidence.Select(pair => pair.Key + ": " + pair.Value));
    }

    private static bool Complete(ToolResult result) => !result.TimedOut && !result.Truncated && string.IsNullOrWhiteSpace(result.Error);
    private static string Describe(ToolResult result) => "exit=" + result.ExitCode + "; timedOut=" + result.TimedOut + "; truncated=" + result.Truncated
        + "\n" + result.Output + (string.IsNullOrWhiteSpace(result.Error) ? "" : "\n" + result.Error);
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
public sealed partial class WindowsWakeTimersProbe : IProbePrimitive
{
    [GeneratedRegex(@"(?i)no active wake timers|There are no active wake timers")]
    private static partial Regex NoWakeTimersPattern();

    [GeneratedRegex(@"(?m)^Timer set by \[(.+?)\]", RegexOptions.CultureInvariant)]
    private static partial Regex WakeTimerOwnerPattern();

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
        if (NoWakeTimersPattern().IsMatch(result.Output))
            return new PrimitiveResult(true, "0", raw) { Detail = "No active wake timers." };
        var owners = WakeTimerOwnerPattern().Matches(result.Output);
        if (owners.Count == 0) return PrimitiveResult.Unavailable("powercfg output was not recognized.");
        return new PrimitiveResult(true, owners.Count.ToString(CultureInfo.InvariantCulture), string.Join("\n", owners.Select(match => match.Groups[1].Value)))
        { Detail = owners.Count + " active wake timer(s) can wake the PC from sleep." };
    }
}
