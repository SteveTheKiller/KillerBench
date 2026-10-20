using System.IO;
using KillerBench.Engine;
using KillerBench.Engine.Core;

internal static class WindowsMaintenancePackChecks
{
    internal static async Task RunAsync()
    {
        await RunSpoolerCancellationChecksAsync();
        var library = WindowsPrimitiveLibrary.Create();
        var pack = WindowsMaintenancePack.Build();
        var validation = RecipePackValidation.Validate(pack, library);
        if (!validation.IsValid) throw new Exception("Maintenance pack is invalid: " + string.Join("; ", validation.Issues.Select(i => i.RecipeId + ": " + i.Message)));
        if (RecipePackJson.Serialize(RecipePackJson.Deserialize(RecipePackJson.Serialize(pack))) != RecipePackJson.Serialize(pack)) throw new Exception("Maintenance pack did not round-trip.");

        // Every registered primitive is referenced by at least one built-in pack.
        var referenced = WindowsPacks.All().SelectMany(p => p.Recipes).SelectMany(r =>
            new[] { r.Detect.Primitive, r.Verify?.Primitive, r.Action?.Primitive, r.Rollback?.Primitive }.Concat(r.Preconditions.Select(x => x.Primitive))).Where(name => name is not null).ToHashSet(StringComparer.Ordinal);
        var unused = library.Probes.Select(p => p.Name).Concat(library.Actions.Select(a => a.Name)).Where(name => !referenced.Contains(name)).ToArray();
        if (unused.Length > 0) throw new Exception("Registered primitives without a pack recipe: " + string.Join(", ", unused));

        // Interpretation fixtures.
        Verdict(pack, "network.mtu-set", WindowsInterfaceMtuProbe.Interpret(new ToolResult(0, "1200\r\n", "", false)), ProbeVerdict.Repairable, "low MTU");
        Verdict(pack, "network.mtu-set", WindowsInterfaceMtuProbe.Interpret(new ToolResult(0, "1500\r\n", "", false)), ProbeVerdict.Repairable, "normal MTU procedure");
        Verdict(pack, "network.mtu-set", WindowsInterfaceMtuProbe.Interpret(new ToolResult(0, "huge\r\n", "", false)), ProbeVerdict.Unknown, "MTU garbage");
        Verdict(pack, "power.wake-timers", WindowsWakeTimersProbe.Interpret(new ToolResult(0, "There are no active wake timers in the system.\r\n", "", false)), ProbeVerdict.Clear, "no wake timers");
        Verdict(pack, "power.wake-timers", WindowsWakeTimersProbe.Interpret(new ToolResult(0, "Timer set by [SERVICE] \\Device\\HarddiskVolume3\\Windows\\System32\\svchost.exe (SystemEventsBroker) expires at 3:00:00 AM.\r\n  Reason: Windows will execute 'NT TASK\\Microsoft\\Windows\\UpdateOrchestrator\\Reboot' scheduled task.\r\n", "", false)), ProbeVerdict.Attention, "wake timer present");
        Verdict(pack, "power.wake-timers", WindowsWakeTimersProbe.Interpret(new ToolResult(1, "", "denied", false)), ProbeVerdict.Unknown, "powercfg denied");
        Verdict(pack, "devices.problem-devices", WindowsProblemDevicesProbe.Interpret(new ToolResult(0, "", "", false)), ProbeVerdict.Clear, "no problem devices");
        Verdict(pack, "devices.problem-devices", WindowsProblemDevicesProbe.Interpret(new ToolResult(0, "USB\\VID_1234&PID_5678\\ABC|USB|Error|Unknown USB Device\r\n", "", false)), ProbeVerdict.Attention, "problem device");
        Verdict(pack, "devices.problem-devices", WindowsProblemDevicesProbe.Interpret(new ToolResult(0, "garbage\r\n", "", false)), ProbeVerdict.Unknown, "device garbage");
        Verdict(pack, "storage.unlettered-volumes", WindowsUnletteredVolumesProbe.Interpret(new ToolResult(0, "", "", false)), ProbeVerdict.Clear, "all lettered");
        Verdict(pack, "storage.unlettered-volumes", WindowsUnletteredVolumesProbe.Interpret(new ToolResult(0, "2|1|931.5|NTFS\r\n", "", false)), ProbeVerdict.Attention, "unlettered volume");
        Verdict(pack, "storage.unlettered-volumes", WindowsUnletteredVolumesProbe.Interpret(new ToolResult(0, "x|y\r\n", "", false)), ProbeVerdict.Unknown, "volume garbage");
        var status = WindowsDeviceStatusProbe.Interpret(new ToolResult(0, "OK\r\n", "", false));
        if (status is not { Complete: true, Value: "OK" }) throw new Exception("Device status OK was not read.");
        var appx = WindowsAppxStatusProbe.Interpret(new ToolResult(0, "Ok\r\n", "", false));
        if (appx is not { Complete: true, Value: "Ok" }) throw new Exception("Package status Ok was not read.");
        if (WindowsAppxStatusProbe.Interpret(new ToolResult(1, "", "not found", false)).Complete) throw new Exception("Missing package was accepted.");

        // Option substitution reaches the verify expectation for the MTU recipe.
        var mtu = pack.Recipes.Single(r => r.Id == "network.mtu-set");
        if (RecipeOptions.Resolve(mtu, new Dictionary<string, string> { ["interface"] = "Ethernet", ["mtu"] = "1400" }, out var values).Count != 0) throw new Exception("MTU options did not resolve.");
        var applied = RecipeOptions.Apply(mtu, values);
        if (applied.Verify!.Expect.Clauses[0].Value != "1400" || applied.Action!.Parameters["mtu"] != "1400" || applied.Detect.Parameters["family"] != "IPv6")
            throw new Exception("MTU option substitution did not reach the verify clause, action or default family.");

        // Live read-only: file counts and problem devices on this machine.
        var jumpLists = await new WindowsFileCountProbe().ObserveAsync(new Dictionary<string, string> { ["folder"] = "recent-automatic-destinations" }, TimeSpan.FromSeconds(5), CancellationToken.None);
        if (!jumpLists.Complete) throw new Exception("Jump-list count failed: " + jumpLists.Error);
        if (WindowsMaintenanceFolders.Resolve("not-a-token") is not null) throw new Exception("Unknown folder token resolved.");
        var retainedRoot = WindowsMaintenanceFolders.RetainRoot;
        if (!retainedRoot.Contains("KillerBench", StringComparison.Ordinal)) throw new Exception("Retained root is outside the KillerBench data folder.");

        // The unknown-token path remains inert with the production filesystem boundaries.
        var absent = await new WindowsRetainFolderFilesAction().ExecuteAsync(new Dictionary<string, string> { ["folder"] = "bogus" }, TimeSpan.FromSeconds(5), null, CancellationToken.None);
        if (absent.Dispatched) throw new Exception("Retain with an unknown token dispatched.");

        // Every explicit recipe stays inert through the plain runner with a substituted library that throws on dispatch.
        var inert = new PrimitiveLibrary("Windows");
        foreach (var probe in library.Probes) inert.AddProbe(new Fake(probe.Name, probe.Parameters));
        foreach (var action in library.Actions) inert.AddAction(new FakeAction(action.Name, action.Parameters, action.Risk, action.RequiresElevation));
        var runner = new RecipeRunner(inert, new MemoryRecipeJournal());
        foreach (var recipe in pack.Recipes.Where(r => r.RequiresExplicitOptions))
        {
            var result = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true });
            if (result.Outcome != RecipeRunOutcome.RepairAvailable || result.Action is not null) throw new Exception(recipe.Id + " did not stay inert: " + result.Outcome + " " + result.Detail);
        }
        Console.WriteLine("PASS: Windows maintenance pack (" + pack.Recipes.Count + " recipes) validates, every registered primitive is used, interpretation and option substitution fixtures pass, live reads were read-only and explicit recipes stayed inert.");
    }

    internal static async Task RunSpoolerCancellationChecksAsync()
    {
        using (var canceled = new CancellationTokenSource())
        {
            var fixture = new SpoolerFixture();
            fixture.Command = (operation, _) =>
            {
                if (operation != "stop") return null;
                fixture.Running = false;
                canceled.Cancel();
                return Task.FromResult(new ToolResult(0, "stop accepted", "", false));
            };
            var result = await fixture.ExecuteAsync(canceled.Token);
            if (!fixture.Running || fixture.StartAttempts != 1 || fixture.RetainAttempts != 0 || result is not { Dispatched: true, Succeeded: false })
                throw new Exception("Cancellation after an accepted stop must restore the Spooler without retaining files.");
        }
        using (var canceled = new CancellationTokenSource())
        {
            canceled.Cancel();
            var fixture = new SpoolerFixture();
            try { await fixture.ExecuteAsync(canceled.Token); throw new Exception("A precanceled clear was dispatched."); }
            catch (OperationCanceledException) { }
            if (fixture.StopAttempts != 0 || fixture.StartAttempts != 0 || fixture.RetainAttempts != 0)
                throw new Exception("A precanceled clear touched the service or spool files.");
        }
        using (var canceled = new CancellationTokenSource())
        {
            var fixture = new SpoolerFixture();
            fixture.Command = (operation, token) =>
            {
                if (operation != "stop") return null;
                fixture.Running = false;
                canceled.Cancel();
                throw new OperationCanceledException(token);
            };
            var result = await fixture.ExecuteAsync(canceled.Token);
            if (!fixture.Running || fixture.StartAttempts != 1 || fixture.RetainAttempts != 0 || result is not { Dispatched: true, Succeeded: false })
                throw new Exception("A canceled stop with an uncertain outcome did not attempt restoration.");
        }
        var stalledStop = new SpoolerFixture { Command = (operation, _) => operation == "stop" ? NeverCompletes() : null };
        var stalledStopResult = await stalledStop.ExecuteAsync();
        if (stalledStop.StartAttempts != 1 || stalledStop.RetainAttempts != 0 || stalledStopResult is not { Dispatched: true, Succeeded: false, TimedOut: true })
            throw new Exception("A stalled stop did not return bounded uncertainty and attempt restoration.");

        var stalledObservation = new SpoolerFixture();
        stalledObservation.Command = (operation, _) => operation == "query" && stalledObservation.StartAttempts == 0 ? NeverCompletes() : null;
        var stalledObservationResult = await stalledObservation.ExecuteAsync();
        if (!stalledObservation.Running || stalledObservation.StartAttempts != 1 || stalledObservation.RetainAttempts != 0 || stalledObservationResult is not { Succeeded: false, TimedOut: true })
            throw new Exception("A stalled stopped-state observation retained files or skipped restoration.");

        var wrongService = new SpoolerFixture();
        wrongService.Command = (operation, _) => operation == "query" && wrongService.StartAttempts == 0
            ? Task.FromResult(new ToolResult(0, "SERVICE_NAME: AnotherService\nSTATE : 1 STOPPED", "", false)) : null;
        var wrongServiceResult = await wrongService.ExecuteAsync();
        if (wrongService.RetainAttempts != 0 || wrongService.StartAttempts != 1 || wrongServiceResult.Succeeded)
            throw new Exception("Another service's stopped state authorized spool file retention.");

        var incompleteStop = new SpoolerFixture { Command = (operation, _) => operation == "stop"
            ? Task.FromResult(new ToolResult(0, "stop result incomplete", "", false) { TimedOut = true }) : null };
        var incompleteStopResult = await incompleteStop.ExecuteAsync();
        if (incompleteStop.RetainAttempts != 0 || incompleteStop.StartAttempts != 1 || incompleteStopResult is not { Succeeded: false, TimedOut: true })
            throw new Exception("An incomplete stop lost its timeout or authorized file retention.");

        var rejectedStop = new SpoolerFixture { Command = (operation, _) => operation == "stop"
            ? Task.FromResult(new ToolResult(5, "stop denied", "", false)) : null };
        var rejectedStopResult = await rejectedStop.ExecuteAsync();
        if (rejectedStop.StartAttempts != 0 || rejectedStop.RetainAttempts != 0 || rejectedStopResult is not { Dispatched: false, Succeeded: false })
            throw new Exception("A rejected stop changed files or reported a dispatched service change.");

        var retentionAndStartFailure = new SpoolerFixture
        {
            Retain = _ => Task.FromResult(new ActionResult(true, false, "retained partial spool copy", "retention fixture failure")),
            Command = (operation, _) => operation == "start" ? Task.FromResult(new ToolResult(5, "start output", "startup fixture denied", false)) : null
        };
        var failedResult = await retentionAndStartFailure.ExecuteAsync();
        if (failedResult.Succeeded || failedResult.Error is null || !failedResult.Error.Contains("retention fixture failure", StringComparison.Ordinal)
            || !failedResult.Error.Contains("startup fixture denied", StringComparison.Ordinal) || !failedResult.Raw.Contains("retained partial spool copy", StringComparison.Ordinal))
            throw new Exception("Retention and restoration failures were not both retained in the action result.");

        var retentionThrow = new SpoolerFixture { Retain = _ => throw new IOException("retention fixture threw") };
        var retentionThrowResult = await retentionThrow.ExecuteAsync();
        if (!retentionThrow.Running || retentionThrow.StartAttempts != 1 || retentionThrowResult.Succeeded || !retentionThrowResult.Error!.Contains("retention fixture threw", StringComparison.Ordinal))
            throw new Exception("A retention exception prevented restoration or lost its failure.");

        var stalledRestoration = new SpoolerFixture { Command = (operation, _) => operation == "start" ? NeverCompletes() : null };
        var stalledRestorationResult = await stalledRestoration.ExecuteAsync();
        if (stalledRestoration.StartAttempts != 1 || stalledRestorationResult is not { Succeeded: false, TimedOut: true })
            throw new Exception("A stalled restoration did not return a bounded failure.");

        var startNotRunning = new SpoolerFixture { Command = (operation, _) => operation == "start" ? Task.FromResult(new ToolResult(0, "start accepted", "", false)) : null };
        var startNotRunningResult = await startNotRunning.ExecuteAsync();
        if (startNotRunning.StartAttempts != 1 || startNotRunningResult is not { Succeeded: false, TimedOut: true }) throw new Exception("An accepted start was reported as verified without a running Spooler, or retried without observing a pending stop.");

        var incompleteRetention = new SpoolerFixture { Retain = _ => Task.FromResult(new ActionResult(true, true, "incomplete retention") { TimedOut = true }) };
        var incompleteRetentionResult = await incompleteRetention.ExecuteAsync();
        if (incompleteRetentionResult is not { Succeeded: false, TimedOut: true }) throw new Exception("Timed-out retention was reported successful.");

        var success = new SpoolerFixture();
        var successResult = await success.ExecuteAsync();
        if (!success.Running || success.StartAttempts != 1 || success.RetainAttempts != 1 || successResult is not { Dispatched: true, Succeeded: true, TimedOut: false, Error: null }
            || !successResult.Raw.Contains("spool files retained", StringComparison.Ordinal)) throw new Exception("Completed spool clearing did not retain its successful result.");
        using (var canceled = new CancellationTokenSource())
        {
            var accepted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var stopResponse = new TaskCompletionSource<ToolResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            bool observedStopped = false, prematureStart = false;
            int pendingQueries = 0;
            var fixture = new SpoolerFixture { Timeout = TimeSpan.FromSeconds(2) };
            fixture.Command = (operation, _) =>
            {
                if (operation == "stop")
                {
                    fixture.Running = false;
                    accepted.SetResult();
                    return stopResponse.Task;
                }
                if (operation == "start" && !observedStopped)
                {
                    prematureStart = true;
                    return Task.FromResult(new ToolResult(1061, "stop still pending", "", false));
                }
                if (operation == "query" && fixture.StartAttempts == 0)
                {
                    observedStopped = pendingQueries++ > 0;
                    return Task.FromResult(new ToolResult(0, "SERVICE_NAME: Spooler\nSTATE : "
                        + (observedStopped ? "1 STOPPED" : "3 STOP_PENDING"), "", false));
                }
                return null;
            };
            var operation = fixture.ExecuteAsync(canceled.Token);
            await accepted.Task.WaitAsync(TimeSpan.FromSeconds(1));
            canceled.Cancel();
            await Task.Delay(100);
            stopResponse.SetResult(new ToolResult(0, "stop accepted", "", false));
            var result = await operation;
            if (prematureStart || !observedStopped || !fixture.Running || fixture.StartAttempts != 1 || fixture.RetainAttempts != 0
                || result is not { Dispatched: true, Succeeded: false })
                throw new Exception("Canceling an in-flight accepted stop started the Spooler before its stopped state was observed.");
        }
        await RunPendingStopRecoveryChecksAsync();
        await RunPartialRetentionChecksAsync();
        Console.WriteLine("PASS: 15 controlled spooler cases cover cancellation, uncertain and rejected stops, observation deadlines, retention failures, restoration failures and verified success. No native commands or spool file operations ran.");

        static Task<ToolResult> NeverCompletes() => new TaskCompletionSource<ToolResult>().Task;
    }

    internal static async Task RunPendingStopRecoveryChecksAsync()
    {
        var failures = new List<string>();
        foreach (bool observationTimeout in new[] { false, true })
        foreach (int prematureExit in new[] { 1056, 1061 })
        {
            bool stopPending = true;
            int recoveryQueries = 0;
            var fixture = new SpoolerFixture { Timeout = TimeSpan.FromMilliseconds(800), Running = false };
            fixture.Command = (operation, _) =>
            {
                if (operation == "stop") return Task.FromResult(new ToolResult(0, "stop accepted", "", false) { TimedOut = !observationTimeout });
                if (operation == "start" && stopPending) return Task.FromResult(new ToolResult(prematureExit, "stop is pending", "", false));
                if (operation == "query" && fixture.StartAttempts == 0 && observationTimeout)
                    return new TaskCompletionSource<ToolResult>().Task;
                if (operation == "query" && !fixture.Running)
                {
                    stopPending = recoveryQueries++ == 0;
                    return Task.FromResult(new ToolResult(0, "SERVICE_NAME: Spooler\nSTATE : "
                        + (stopPending ? "3 STOP_PENDING" : "1 STOPPED"), "", false));
                }
                return null;
            };
            var result = await fixture.ExecuteAsync();
            if (!fixture.Running || fixture.StartAttempts != 2 || fixture.RetainAttempts != 0
                || result is not { Dispatched: true, Succeeded: false, TimedOut: true }
                || !result.Raw.Contains("4 RUNNING", StringComparison.Ordinal)
                || !result.Raw.Contains("exit=" + prematureExit, StringComparison.Ordinal)
                || !result.Raw.Contains("3 STOP_PENDING", StringComparison.Ordinal)
                || !result.Raw.Contains("1 STOPPED", StringComparison.Ordinal)
                || !result.Raw.Contains("start-after-stopped:", StringComparison.Ordinal))
                failures.Add((observationTimeout ? "observation timeout" : "stop timeout") + "/" + prematureExit
                    + ": starts=" + fixture.StartAttempts + ", running=" + fixture.Running + ", error=" + result.Error);
        }
        foreach (string mode in new[] { "disabled", "pending", "corrective" })
        {
            var fixture = new SpoolerFixture { Running = false };
            fixture.Command = (operation, _) => operation switch
            {
                "stop" => Task.FromResult(new ToolResult(0, "stop outcome incomplete", "", false) { TimedOut = true }),
                "start" => Task.FromResult(new ToolResult(mode == "disabled" ? 1058 : fixture.StartAttempts == 1 ? 1056 : 0, "start response", "", false)),
                "query" => Task.FromResult(new ToolResult(0, "SERVICE_NAME: Spooler\nSTATE : "
                    + (mode == "pending" ? "3 STOP_PENDING" : "1 STOPPED"), "", false)),
                _ => null
            };
            var result = await fixture.ExecuteAsync();
            if (fixture.StartAttempts != (mode == "corrective" ? 2 : 1) || fixture.RetainAttempts != 0
                || result is not { Succeeded: false, TimedOut: true } || string.IsNullOrWhiteSpace(result.Error))
                failures.Add(mode + ": disabled/pending restoration did not retain bounded uncertainty or the corrective-start cap.");
        }
        if (failures.Count != 0) throw new Exception("Pending accepted stops were not recovered inside the restoration budget: " + string.Join("; ", failures));
        Console.WriteLine("PASS: four pending-stop timeout cases recover after 1056/1061 and retain each start/state result; three failure guards preserve disabled/pending uncertainty and the one-corrective-start cap.");
    }

    internal static async Task RunPartialRetentionChecksAsync()
    {
        var failures = new List<string>();
        foreach (string mode in new[] { "caller", "deadline", "journal", "journal-deadline", "zero" })
        {
            string root = Path.Combine(Path.GetTempPath(), "KillerBench-retain-" + Guid.NewGuid().ToString("N"));
            string source = Path.Combine(root, "source"), retainedRoot = Path.Combine(root, "retained");
            Directory.CreateDirectory(source);
            File.WriteAllText(Path.Combine(source, "a.SHD"), "first owned bytes");
            File.WriteAllText(Path.Combine(source, "b.SPL"), "second owned bytes");
            using var canceled = new CancellationTokenSource();
            string? movedSource = null, movedDestination = null;
            int moves = 0;
            var fixture = new SpoolerFixture();
            fixture.Retain = retentionToken => new WindowsRetainFolderFilesAction(
                folder =>
                {
                    if (mode == "zero") canceled.Cancel();
                    return folder == "spool-printers" ? (source, "*") : null;
                },
                () => retainedRoot,
                (from, to) =>
                {
                    File.Move(from, to, false);
                    movedSource = from;
                    movedDestination = to;
                    moves++;
                    if (mode is "deadline" or "journal-deadline")
                    {
                        if (!retentionToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(mode == "journal-deadline" ? 22 : 2))) throw new Exception("The fixture retention deadline did not expire.");
                    }
                    else canceled.Cancel();
                }).ExecuteAsync(new Dictionary<string, string> { ["folder"] = "spool-printers" }, fixture.Timeout, null, retentionToken);
            try
            {
                ActionResult? result;
                MemoryRecipeJournal? journal = null;
                if (mode.StartsWith("journal", StringComparison.Ordinal))
                {
                    var library = new PrimitiveLibrary("Windows");
                    library.AddProbe(new Fake("file.count", [WindowsMaintenanceFolders.FolderParameter]));
                    library.AddAction(fixture.CreateAction());
                    journal = new MemoryRecipeJournal();
                    var recipe = WindowsMaintenancePack.Build().Recipes.Single(r => r.Id == "printing.spool-clear");
                    var run = await new RecipeRunner(library, journal).RunAsync(recipe,
                        new RecipeRunOptions { AuthorizeRepair = true, Elevated = true, ExplicitOptionsConfirmed = true }, canceled.Token);
                    if (!run.ExecutionUncertain) failures.Add("Journaled partial retention was not reported uncertain.");
                    result = run.Action;
                }
                else result = await fixture.ExecuteAsync(canceled.Token);

                string retained = Directory.GetDirectories(retainedRoot).Single();
                int expectedMoves = mode == "zero" ? 0 : 1;
                if (moves != expectedMoves || Directory.GetFiles(source).Length != 2 - expectedMoves
                    || Directory.GetFiles(retained).Length != expectedMoves
                    || (expectedMoves > 0 && (movedSource is null || movedDestination is null || !File.Exists(movedDestination)
                        || File.Exists(movedSource) || File.ReadAllText(movedDestination) != (Path.GetFileName(movedSource) == "a.SHD" ? "first owned bytes" : "second owned bytes")))
                    || (expectedMoves == 0 && (File.ReadAllText(Path.Combine(source, "a.SHD")) != "first owned bytes"
                        || File.ReadAllText(Path.Combine(source, "b.SPL")) != "second owned bytes")))
                    failures.Add(mode + ": partial retained bytes or later source file were not preserved.");
                if (!fixture.Running || fixture.StartAttempts != 1 || result is not { Dispatched: true, Succeeded: false }
                    || result.TimedOut != mode.EndsWith("deadline", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(result.Error)
                    || !result.Raw.Contains("moved " + expectedMoves, StringComparison.Ordinal) || retained.Length == 0
                    || !result.Raw.Contains(retained, StringComparison.Ordinal))
                    failures.Add(mode + ": partial move count, retained path, cancellation/deadline result, or service restoration evidence was lost.");
                if (journal != null && !journal.Entries.Any(entry => entry.Stage == RecipeJournalStage.Dispatched
                    && entry.Raw?.Contains("moved 1", StringComparison.Ordinal) == true
                    && entry.Raw.Contains(retained, StringComparison.Ordinal))) failures.Add("The dispatched journal lost partial retention evidence.");
            }
            finally { Directory.Delete(root, true); }
        }
        if (failures.Count != 0) throw new Exception(string.Join("; ", failures));
        Console.WriteLine("PASS: actual retention preserves one moved file, later source bytes, path/count and cancellation/deadline evidence through the spooler result and recipe journal.");
    }

    private sealed class SpoolerFixture
    {
        internal bool Running = true;
        internal TimeSpan Timeout = TimeSpan.FromMilliseconds(80);
        internal int StopAttempts, StartAttempts, RetainAttempts;
        internal Func<string, CancellationToken, Task<ToolResult>?>? Command;
        internal Func<CancellationToken, Task<ActionResult>>? Retain;

        internal WindowsSpoolerClearAction CreateAction() => new WindowsSpoolerClearAction(
            (operation, _, commandToken) =>
            {
                commandToken.ThrowIfCancellationRequested();
                if (operation == "stop") StopAttempts++;
                if (operation == "start") StartAttempts++;
                if (Command?.Invoke(operation, commandToken) is { } result) return result;
                if (operation == "stop") Running = false;
                if (operation == "start") Running = true;
                return Task.FromResult(new ToolResult(0, operation == "query"
                    ? "SERVICE_NAME: Spooler\nSTATE : " + (Running ? "4 RUNNING" : "1 STOPPED") : operation + " accepted", "", false));
            }, (_, _, retentionToken) =>
            {
                retentionToken.ThrowIfCancellationRequested();
                RetainAttempts++;
                return Retain?.Invoke(retentionToken) ?? Task.FromResult(new ActionResult(true, true, "spool files retained"));
            });

        internal Task<ActionResult> ExecuteAsync(CancellationToken token = default) => CreateAction()
            .ExecuteAsync(new Dictionary<string, string>(), Timeout, null, token).WaitAsync(TimeSpan.FromSeconds(3));
    }

    private static void Verdict(RecipePack pack, string recipeId, PrimitiveResult result, ProbeVerdict expected, string what)
    {
        var evaluation = Expectation.Evaluate(pack.Recipes.Single(r => r.Id == recipeId).Detect.Expect, result);
        if (evaluation.Verdict != expected) throw new Exception(what + " expected " + expected + " but was " + evaluation.Verdict + ": " + evaluation.Detail);
    }

    private sealed class Fake(string name, IReadOnlyList<PrimitiveParameter> parameters) : IProbePrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters => parameters;
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> p, TimeSpan timeout, CancellationToken token) => Task.FromResult(new PrimitiveResult(true, "1", ""));
    }

    private sealed class FakeAction(string name, IReadOnlyList<PrimitiveParameter> parameters, RecipeRisk risk, bool elevation) : IActionPrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters => parameters;
        public RecipeRisk Risk => risk;
        public bool RequiresElevation => elevation;
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> p, TimeSpan timeout, IProgress<string>? progress, CancellationToken token) =>
            throw new InvalidOperationException("Explicit recipe must not dispatch without confirmation.");
    }
}
