using System.IO;
using System.Text.Json;
using KillerBench.Engine.Infrastructure;
using KillerBench.Engine.Updates;

internal static class WinGetExecutionChecks
{
    internal static async Task RunAsync(string scratchRoot)
    {
        string root = Path.Combine(scratchRoot, "winget-execution-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        foreach (var change in new Action<Dictionary<string, object>>[]
        {
            value => value["count"] = 0,
            value => value["count"] = 2,
            value => value["exists"] = false,
            value => value["family"] = "Other_8wekyb3d8bbwe",
            value => value["publisher"] = "CN=Other",
            value => value["publisherId"] = "other",
            value => value["development"] = true,
            value => value["packageStatus"] = "Modified",
            value => value["signature"] = "NotSigned",
            value => value["signer"] = "CN=Other, O=Other",
            value => value["redirected"] = true,
            value => value["original"] = "other.exe",
            value => value["path"] = Path.Combine(root, "winget.exe"),
            value => value["installLocation"] = root
        })
        {
            var evidence = TrustedEvidence();
            change(evidence);
            int starts = 0;
            var host = new WindowsThirdPartyUpdateExecutionHost(_ => Task.FromResult(WinGetExecutableResolver.Interpret(Tool(evidence))),
                (_, _, _, beforeStart, _) => { beforeStart?.Invoke(); starts++; return Task.FromResult(new ToolResult(0, "fixture-version", "", false)); });
            try { await host.CheckProviderAsync(); throw new Exception("Untrusted or missing WinGet evidence was accepted."); }
            catch (ToolNotStartedException) { }
            if (starts != 0) throw new Exception("Rejected WinGet trust evidence reached the process boundary.");
        }

        string? selectedPath = null;
        var trusted = TrustedEvidence();
        var trustedHost = new WindowsThirdPartyUpdateExecutionHost(_ => Task.FromResult(WinGetExecutableResolver.Interpret(Tool(trusted))),
            (path, arguments, _, beforeStart, _) =>
            {
                beforeStart?.Invoke();
                selectedPath = path;
                if (!arguments.SequenceEqual(["--version"])) throw new Exception("Fixture attempted something other than a harmless version request.");
                return Task.FromResult(new ToolResult(0, "fixture-version", "", false));
            });
        await trustedHost.CheckProviderAsync();
        if (selectedPath != (string)trusted["path"]) throw new Exception("Registered WinGet path did not reach the process boundary unchanged.");

        var missing = new WindowsThirdPartyUpdateExecutionHost(_ => Task.FromResult(Path.Combine(root, "missing-provider.exe")));
        var noStart = new BoundaryHost(token => missing.CheckProviderAsync(token));
        string noStartRoot = Path.Combine(root, "no-start");
        var result = await ThirdPartyUpdateExecution.RunAsync(Selection, true, noStartRoot, noStart);
        if (result.Status != "Blocked" || noStart.InventoryReads != 0 || File.Exists(Pending(noStartRoot)) ||
            Directory.GetFiles(noStartRoot, "*.prepared.json").Length != 1)
            throw new Exception("A proven process-start failure retained an update pending marker or attempted verification.");

        foreach (string scenario in new[] { "unknown-failure", "timed-out", "inventory-no-start", "canceled" })
        {
            using var stop = new CancellationTokenSource();
            var host = new BoundaryHost(_ =>
            {
                if (scenario == "unknown-failure") throw new IOException("Started operation lost its result.");
                if (scenario == "canceled") { stop.Cancel(); throw new OperationCanceledException(stop.Token); }
                return Task.FromResult(new ToolResult(0, "", "", false) { TimedOut = scenario == "timed-out" });
            }) { FailInventoryStart = scenario == "inventory-no-start" };
            string pendingRoot = Path.Combine(root, scenario);
            result = await ThirdPartyUpdateExecution.RunAsync(Selection, true, pendingRoot, host, token: stop.Token);
            if (result.Status != (scenario == "canceled" ? "Stopped" : "Needs review") || !File.Exists(Pending(pendingRoot)))
                throw new Exception("Possible dispatched update lost its pending state after " + scenario + ".");
        }
        await CancellationRacesAsync(root);
        Console.WriteLine("PASS: WinGet trust, exact path, no-start archival, cancellation before and after atomic startup, late callback exclusion and sequential host reuse; no package upgrade requested.");

        static ToolResult Tool(Dictionary<string, object> values) => new(0, JsonSerializer.Serialize(values), "", false);
        static string Pending(string folder) => Path.Combine(folder, "third-party-update.pending.json");
    }

    private static readonly ThirdPartyUpdateSelection Selection = new("Fixture.Tool", "winget", "2.0");

    private static async Task CancellationRacesAsync(string root)
    {
        foreach (string scenario in new[] { "resolution", "before-start", "started" })
        {
            using var stop = new CancellationTokenSource();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int resolutions = 0, executions = 0, starts = 0;
            Action? oldClaim = null;
            var native = new WindowsThirdPartyUpdateExecutionHost(async _ =>
            {
                int call = Interlocked.Increment(ref resolutions);
                if (scenario == "resolution" && call == 1) { entered.SetResult(); await release.Task; }
                return (string)TrustedEvidence()["path"];
            }, async (_, _, _, claim, _) =>
            {
                int call = Interlocked.Increment(ref executions);
                if (call == 1 && scenario != "resolution")
                {
                    oldClaim = claim;
                    if (scenario == "started") { claim!(); Interlocked.Increment(ref starts); }
                    entered.SetResult();
                    await release.Task;
                    if (scenario == "before-start") { claim!(); Interlocked.Increment(ref starts); }
                }
                else { claim!(); Interlocked.Increment(ref starts); }
                return new ToolResult(0, "fixture", "", false);
            });
            var host = new ControlledHost(native);
            string folder = Path.Combine(root, "cancel-" + scenario);
            var run = ThirdPartyUpdateExecution.RunAsync(Selection, true, folder, host, token: stop.Token);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            stop.Cancel();
            var result = await run.WaitAsync(TimeSpan.FromSeconds(10));
            bool started = scenario == "started";
            if (result.Status != "Stopped" || File.Exists(Path.Combine(folder, "third-party-update.pending.json")) != started || starts != (started ? 1 : 0))
                throw new Exception("Cancellation did not distinguish native startup in " + scenario + ".");
            string finishedJournal = File.ReadAllText(result.JournalPath);
            var previousUpgrade = host.ActiveUpgrade!;
            ThirdPartyUpdateExecutionResult? reused = null;
            if (!started)
                reused = await ThirdPartyUpdateExecution.RunAsync(Selection, true, Path.Combine(root, "reuse-" + scenario), host);
            release.SetResult();
            try { await previousUpgrade.WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (ToolStartCanceledException) when (!started) { }
            if (starts != 1 || File.ReadAllText(result.JournalPath) != finishedJournal)
                throw new Exception("A late provider callback launched work or rewrote the finalized journal.");

            // Reusing the host cannot reactivate the sealed earlier callback or inherit its gate.
            reused ??= await ThirdPartyUpdateExecution.RunAsync(Selection, true, Path.Combine(root, "reuse-" + scenario), host);
            if (reused.Status != "Completed" || starts != (started ? 2 : 1))
                throw new Exception("Sequential host reuse inherited a prior call's startup gate.");
            if (oldClaim is not null)
            {
                try { oldClaim(); throw new Exception("An old startup callback became usable after host reuse."); }
                catch (ToolStartCanceledException) { }
            }
        }
    }

    private static Dictionary<string, object> TrustedEvidence()
    {
        const string fullName = "Microsoft.DesktopAppInstaller_1.2.3.4_x64__8wekyb3d8bbwe";
        string location = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps", fullName);
        return new()
        {
            ["count"] = 1, ["name"] = "Microsoft.DesktopAppInstaller", ["family"] = "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe",
            ["fullName"] = fullName, ["publisher"] = "CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US",
            ["publisherId"] = "8wekyb3d8bbwe", ["development"] = false, ["packageStatus"] = "Ok",
            ["installLocation"] = location, ["path"] = Path.Combine(location, "winget.exe"), ["exists"] = true,
            ["redirected"] = false, ["signature"] = "Valid", ["signer"] = "CN=Microsoft Corporation, O=Microsoft Corporation", ["original"] = "winget.exe"
        };
    }

    private sealed class BoundaryHost(Func<CancellationToken, Task<ToolResult>> execute) : IThirdPartyUpdateExecutionHost
    {
        internal int InventoryReads;
        internal bool FailInventoryStart;
        public Task<ThirdPartyUpdateResult> DiscoverAsync(CancellationToken token) => Task.FromResult(new ThirdPartyUpdateResult(true, "fixture", true, 1,
            [new ThirdPartyUpdateCandidate(Selection.Id, "Fixture", "1.0", Selection.Source, [Selection.TargetVersion])], "Fixture"));
        public Task<ToolResult> UpgradeAsync(ThirdPartyUpdateSelection selection, CancellationToken token) => execute(token);
        public Task<string?> ReadInstalledVersionAsync(string id, string source, CancellationToken token)
        {
            InventoryReads++;
            if (FailInventoryStart) throw new ToolNotStartedException("Inventory subprocess did not start after the upgrade returned.");
            return Task.FromResult<string?>("1.0");
        }
    }

    private sealed class ControlledHost(WindowsThirdPartyUpdateExecutionHost native) : IThirdPartyUpdateExecutionHost, IThirdPartyUpdateStartControl
    {
        internal Task<ToolResult>? ActiveUpgrade;
        public Task<ThirdPartyUpdateResult> DiscoverAsync(CancellationToken token) => Task.FromResult(new ThirdPartyUpdateResult(true, "fixture", true, 1,
            [new ThirdPartyUpdateCandidate(Selection.Id, "Fixture", "1.0", Selection.Source, [Selection.TargetVersion])], "Fixture"));
        public Task<ToolResult> UpgradeAsync(ThirdPartyUpdateSelection selection, CancellationToken token) => ActiveUpgrade = native.UpgradeAsync(selection, token);
        public Task<string?> ReadInstalledVersionAsync(string id, string source, CancellationToken token) => Task.FromResult<string?>(Selection.TargetVersion);
        public bool TryCancelBeforeStart(CancellationToken operationToken) => native.TryCancelBeforeStart(operationToken);
    }

    internal static async Task CheckInstalledProviderAsync()
    {
        var result = await new WindowsThirdPartyUpdateExecutionHost().CheckProviderAsync();
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error) || string.IsNullOrWhiteSpace(result.Output))
            throw new Exception("The installed trusted WinGet provider did not answer a harmless version request.");
        Console.WriteLine("PASS: installed WinGet answered a harmless version request through the public execution host: " + result.Output.Trim());
    }
}
