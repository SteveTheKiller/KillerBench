using System.IO;
using KillerBench.Engine.Core;
using KillerBench.Engine.Core.Primitives;
using KillerBench.Engine.Core.Recipes;
using KillerBench.Engine.Infrastructure;
using KillerBench.Engine.Windows.Packs;
using KillerBench.Engine.Windows.Primitives;
using KillerBench.Engine.Windows.Automation;
using KillerBench.Engine.Windows.Workflows;
using KillerBench.Engine.Workflows;

internal static class WindowsReviewRegressionChecks
{
    internal static async Task PhysicalMemoryAsync()
    {
        var parameters = new Dictionary<string, string>();
        var probe = new WindowsTopMemoryProbe(() => 8L * 1024 * 1024 * 1024,
            () => new Dictionary<string, long> { ["Fixture"] = 2L * 1024 * 1024 * 1024 });
        var observed = await probe.ObserveAsync(parameters, TimeSpan.FromSeconds(2), CancellationToken.None);
        if (!observed.Complete || observed.Value != "25.0") throw new Exception("W006 physical RAM percentage changed with the managed memory budget: " + observed.Value);
        var unavailable = await new WindowsTopMemoryProbe(() => 0, () => throw new Exception("Processes queried without physical RAM"))
            .ObserveAsync(parameters, TimeSpan.FromSeconds(2), CancellationToken.None);
        if (unavailable.Complete) throw new Exception("W006 unavailable physical RAM became a GC budget.");
        var nativeTotal = await WindowsNetAdapterPrimitives.RunAsync("[string]([uint64](Get-CimInstance Win32_OperatingSystem -ErrorAction Stop).TotalVisibleMemorySize*1024)", CancellationToken.None);
        if (nativeTotal.ExitCode != 0 || !long.TryParse(nativeTotal.Output.Trim(), out long physicalBytes) || physicalBytes <= 0)
            throw new Exception("W006 independent read-only Windows physical memory query failed.");
        var native = await new WindowsTopMemoryProbe(processWorkingSets: () => new Dictionary<string, long> { ["Fixture"] = 1073741824 })
            .ObserveAsync(parameters, TimeSpan.FromSeconds(2), CancellationToken.None);
        string expected = (1073741824 * 100.0 / physicalBytes).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        if (!native.Complete || native.Value != expected) throw new Exception("W006 native physical memory differs from independent Windows evidence.");
        Console.WriteLine("PASS W006: fixed physical RAM, unavailable provider and independent native read-only comparison.");
    }

    internal static async Task ServiceDispatchAsync()
    {
        foreach (bool beforeLaunch in new[] { false, true })
        {
            var action = new WindowsServiceStartAction((_, _, _) => throw (beforeLaunch
                ? new ToolNotStartedException("Fixture definitely did not start") : new IOException("Fixture failed after possible dispatch")));
            var result = await action.ExecuteAsync(new Dictionary<string, string> { ["name"] = "Spooler" }, TimeSpan.FromSeconds(2), null, CancellationToken.None);
            if (result.Dispatched != !beforeLaunch || result.Succeeded) throw new Exception("W003 service dispatch uncertainty was cleared: " + result);
        }
        bool outputObserved = false;
        var readOnlyHost = new WindowsRepairHost();
        var nativeAction = new WindowsServiceStartAction((_, progress, _) =>
            readOnlyHost.RunAsync(new RepairStep("sc.exe", ["query", "Spooler"], true), progress));
        var fixtureRecipe = WindowsServicePack.Build().Recipes.Single(recipe => recipe.Id == "printing.spooler");
        var store = new MemoryRecipeOperationStore();
        var library = new PrimitiveLibrary("Windows")
            .AddProbe(new FixtureProbe("service.check", () => "Repairable"))
            .AddProbe(new FixtureProbe("service.running", () => "Running"))
            .AddAction(nativeAction);
        var nativeResult = await new RecipeRunner(library, new MemoryRecipeJournal(), store).RunAsync(fixtureRecipe,
            new RecipeRunOptions { AuthorizeRepair = true, Elevated = true, Progress = new SynchronousProgress(value =>
                { if (!value.Contains("SERVICE_NAME:", StringComparison.Ordinal)) return; outputObserved = true; throw new IOException("Fixture output failed after the harmless query process started"); }) });
        if (!outputObserved || !nativeResult.ExecutionUncertain || !await store.IsPendingAsync(RecipeOperationIdentity.FromResolvedRecipe(fixtureRecipe)) ||
            nativeResult.Detail.Contains("No change was made", StringComparison.OrdinalIgnoreCase))
            throw new Exception("W003 actual postlaunch output failure cleared the pending guard.");
        Console.WriteLine("PASS W003: proven prelaunch refusal, possible dispatch and real read-only child output failure preserve uncertainty and pending recovery.");
    }

    internal static async Task AppxIdentityAsync()
    {
        const string selected = "Fixture.App_1.0.0.0_x64__fixture";
        foreach (int matches in new[] { 1, 0, 2 })
        {
            string prefix = "function Get-AppxPackage { [CmdletBinding()]param([string]$PackageTypeFilter) " +
                string.Concat(Enumerable.Repeat("[pscustomobject]@{PackageFullName='" + selected + "';Status='Ok';InstallLocation='C:\\Fixture'};", matches)) +
                "[pscustomobject]@{PackageFullName='Unselected.App_1.0.0.0_x64__fixture';Status='Error';InstallLocation='C:\\Other'} };" +
                "function Test-Path { $true }; function Add-AppxPackage { param([switch]$DisableDevelopmentMode,$Register,$ErrorAction) if($Register -notlike '*Fixture*'){throw 'Wrong selected registration'} };" +
                "function Remove-AppxPackage { param($Package,$ErrorAction) if($Package -cne '" + selected + "'){throw 'Wrong selected package'} };";
            Task<ToolResult> Run(string script, CancellationToken token, string[] arguments) => WindowsNetAdapterPrimitives.RunAsync(prefix + script, token, arguments);
            var parameters = new Dictionary<string, string> { ["package"] = selected };
            var status = await new WindowsAppxStatusProbe(Run).ObserveAsync(parameters, TimeSpan.FromSeconds(10), CancellationToken.None);
            if (status.Complete != (matches == 1) || matches == 1 && status.Value != "Ok")
                throw new Exception("W001 exact current-account status lookup failed: " + status.Error);
            var register = await new WindowsAppxReregisterAction(Run).ExecuteAsync(parameters, TimeSpan.FromSeconds(10), null, CancellationToken.None);
            if (register.Succeeded != (matches == 1) || matches != 1 && register.Dispatched)
                throw new Exception("W001 re-registration accepted missing/duplicate identity or rejected exact identity.");
            var remove = await new WindowsMacePackageRemovalHost(Run).RemoveCurrentAccountPackageAsync(selected, CancellationToken.None);
            if ((remove.ExitCode == 0) != (matches == 1)) throw new Exception("W001 MACE removal selection failed.");
        }
        var installed = await WindowsNetAdapterPrimitives.RunAsync("Get-AppxPackage -PackageTypeFilter All -ErrorAction Stop|Select-Object -First 1 -ExpandProperty PackageFullName", CancellationToken.None);
        if (installed.ExitCode != 0 || string.IsNullOrWhiteSpace(installed.Output)) throw new Exception("W001 native read-only AppX package fixture was unavailable.");
        var liveStatus = await new WindowsAppxStatusProbe().ObserveAsync(new Dictionary<string, string> { ["package"] = installed.Output.Trim() }, TimeSpan.FromSeconds(20), CancellationToken.None);
        if (!liveStatus.Complete) throw new Exception("W001 native exact current-account status lookup failed: " + liveStatus.Error);
        Console.WriteLine("PASS W001: exact status, re-registration and MACE removal with supported PowerShell binding, missing and duplicate identities.");
    }

    internal static async Task DeviceRestartAsync()
    {
        foreach (var result in new[] { new ToolResult(3010, "restart accepted", "", false), new ToolResult(0, "done", "", false),
            new ToolResult(1, "", "failed", false), new ToolResult(3010, "partial", "", true), new ToolResult(-1, "", "", false) { TimedOut = true } })
        {
            var action = new WindowsDeviceRestartAction((arguments, _, _) => arguments.SequenceEqual(new[] { "/restart-device", "PCI\\FIXTURE" })
                ? Task.FromResult(result) : throw new Exception("W002 selected device changed."));
            var observed = await action.ExecuteAsync(new Dictionary<string, string> { ["instance"] = "PCI\\FIXTURE" }, TimeSpan.FromSeconds(2), null, CancellationToken.None);
            bool success = result.ExitCode is 0 or 3010 && !result.Truncated && !result.TimedOut;
            if (!observed.Dispatched || observed.Succeeded != success || observed.RestartRequired != (success && result.ExitCode == 3010))
                throw new Exception("W002 native " + result.ExitCode + " reboot success was misclassified: " + observed);
        }
        string root = Path.Combine(Path.GetTempPath(), "KillerBenchDeviceRestart-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            int dispatches = 0;
            string status = "Error", boot = "boot-A";
            var action = new WindowsDeviceRestartAction((_, _, _) => { dispatches++; return Task.FromResult(new ToolResult(3010, "accepted", "", false)); });
            var library = new PrimitiveLibrary("Windows").AddProbe(new FixtureProbe("pnp.device-status", () => status)).AddAction(action);
            var recipe = WindowsMaintenancePack.Build().Recipes.Single(r => r.Id == "devices.restart-device");
            var options = new RecipeRunOptions { AuthorizeRepair = true, Elevated = true, ExplicitOptionsConfirmed = true,
                Options = new Dictionary<string, string> { ["instance"] = "PCI\\FIXTURE" }, BootIdentity = () => boot };
            Task<RecipeRunResult> Run() => new RecipeRunner(library, new MemoryRecipeJournal(), new FileRecipeOperationStore(root)).RunAsync(recipe, options);
            var first = await Run();
            if (first.Outcome != RecipeRunOutcome.RestartRequired || dispatches != 1 || Directory.GetFiles(root, "*.pending.json").Length != 1)
                throw new Exception("W002 reboot-required device action did not retain durable restart recovery.");
            var sameBoot = await Run();
            if (!sameBoot.ExecutionUncertain || dispatches != 1) throw new Exception("W002 same-boot recovery repeated mutation.");
            boot = "boot-B"; status = "OK";
            var afterBoot = await Run();
            if (afterBoot.Outcome != RecipeRunOutcome.Repaired || dispatches != 1 || Directory.GetFiles(root, "*.pending.json").Length != 0)
                throw new Exception("W002 changed-boot recovery did not verify without redispatch.");
        }
        finally { Directory.Delete(root, true); }
        Console.WriteLine("PASS W002: device restart exit boundaries and durable same/new-boot recovery without redispatch.");
    }

    internal static async Task RetentionLimitsAsync()
    {
        string root = Path.Combine(Path.GetTempPath(), "KillerBenchRetentionLimit-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string source = Path.Combine(root, "source");
            Directory.CreateDirectory(source);
            for (int i = 0; i < 10001; i++) File.WriteAllText(Path.Combine(source, i + ".spl"), "");
            var action = new WindowsRetainFolderFilesAction(_ => (source, "*"), () => Path.Combine(root, "retained"));
            var parameters = new Dictionary<string, string> { ["folder"] = "spool-printers" };
            var overflow = await action.ExecuteAsync(parameters, TimeSpan.FromSeconds(30), null, CancellationToken.None);
            if (overflow.Succeeded || overflow.Dispatched || Directory.EnumerateFiles(source).Count() != 10001)
                throw new Exception("W005 over-limit retention moved a partial queue or claimed success.");
            bool running = true;
            var clear = new WindowsSpoolerClearAction((operation, _, _) =>
            {
                if (operation == "stop") running = false;
                if (operation == "start") running = true;
                return Task.FromResult(new ToolResult(0, operation == "query" ? "SERVICE_NAME: Spooler\nSTATE : " + (running ? "4 RUNNING" : "1 STOPPED") : "accepted", "", false));
            }, (timeout, progress, token) => action.ExecuteAsync(parameters, timeout, progress, token));
            var recipe = WindowsMaintenancePack.Build().Recipes.Single(r => r.Id == "printing.spool-clear");
            var library = new PrimitiveLibrary("Windows").AddProbe(new FixtureProbe("file.count", () => "1"))
                .AddProbe(new FixtureProbe("service.running", () => running ? "Running" : "NotRunning")).AddAction(clear);
            var recipeResult = await new RecipeRunner(library, new MemoryRecipeJournal(), new MemoryRecipeOperationStore()).RunAsync(recipe,
                new RecipeRunOptions { AuthorizeRepair = true, Elevated = true, ExplicitOptionsConfirmed = true });
            if (recipeResult.Outcome == RecipeRunOutcome.Repaired || !recipeResult.ExecutionUncertain || !running || Directory.EnumerateFiles(source).Count() != 10001)
                throw new Exception("W005 recipe growth beyond detection limit claimed a complete clear or failed restoration.");
            File.Delete(Path.Combine(source, "10000.spl"));
            var exact = await action.ExecuteAsync(parameters, TimeSpan.FromSeconds(30), null, CancellationToken.None);
            if (!exact.Succeeded || !exact.Dispatched || Directory.EnumerateFiles(source).Any())
                throw new Exception("W005 exact-limit retention did not empty the source.");
            File.WriteAllText(Path.Combine(source, "growth.spl"), "");
            bool added = false;
            var growth = new WindowsRetainFolderFilesAction(_ => (source, "*"), () => Path.Combine(root, "growth-retained"), null, null, null,
                dispatchMove: (_, _, move) => { move(); if (!added) { added = true; File.WriteAllText(Path.Combine(source, "late.spl"), ""); } });
            var late = await growth.ExecuteAsync(parameters, TimeSpan.FromSeconds(30), null, CancellationToken.None);
            if (late.Succeeded || !late.Dispatched || !File.Exists(Path.Combine(source, "late.spl")))
                throw new Exception("W005 concurrent remainder was reported as a complete clear.");
            Console.WriteLine("PASS W005: 10000, 10001 and post-enumeration growth retention.");
        }
        finally { Directory.Delete(root, true); }
    }

    internal static void VssStates()
    {
        var recipe = WindowsSystemPack.Build().Recipes.Single(r => r.Id == "backup.vss-writers");
        static ToolResult State(int state) => new(0, "Writer name: 'Fixture writer'\n State: [" + state + "] Reported state\n", "", false);
        for (int state = 0; state <= 16; state++)
        {
            var observed = WindowsVssWritersProbe.Interpret(State(state));
            var expected = state == 1 ? ProbeVerdict.Clear : state is >= 2 and <= 5 ? ProbeVerdict.Information
                : state is >= 6 and <= 15 ? ProbeVerdict.Attention : ProbeVerdict.Unknown;
            var verdict = Expectation.Evaluate(recipe.Detect.Expect, observed).Verdict;
            if (verdict != expected) throw new Exception("W007 writer state " + state + " expected " + expected + ", got " + verdict);
            if (state is >= 2 and <= 5 && (observed.Detail ?? "").Contains("restart", StringComparison.OrdinalIgnoreCase))
                throw new Exception("W007 normal backup activity recommended restart.");
        }
        var mixed = WindowsVssWritersProbe.Interpret(State(1) with { Output = State(1).Output + State(3).Output + State(8).Output });
        if (Expectation.Evaluate(recipe.Detect.Expect, mixed).Verdict != ProbeVerdict.Attention)
            throw new Exception("W007 failed writer was hidden by active writers.");
        Console.WriteLine("PASS W007: documented VSS states and mixed inventory.");
    }

    internal static async Task VssSamplingAsync()
    {
        foreach (bool staysActive in new[] { false, true })
        {
            int samples = 0;
            var probe = new WindowsVssWritersProbe(_ => Task.FromResult(new ToolResult(0,
                "Writer name: 'Fixture'\n State: [" + (++samples == 1 || staysActive ? 3 : 1) + "] Reported state\n", "", false)));
            var result = await probe.ObserveAsync(new Dictionary<string, string>(), TimeSpan.FromSeconds(5), CancellationToken.None);
            if (!result.Complete || result.Value != (staysActive ? "InProgress" : "0") || samples != (staysActive ? 3 : 2))
                throw new Exception("W007 VSS transition sampling was unbounded or lost final state.");
        }
        Console.WriteLine("PASS W007: bounded transition resampling and completion.");
    }

    private sealed class SynchronousProgress(Action<string> report) : IProgress<string>
    {
        public void Report(string value) => report(value);
    }

    private sealed class FixtureProbe(string name, Func<string> value) : IProbePrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters => [];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token) =>
            Task.FromResult(new PrimitiveResult(true, value(), "fixture"));
    }
}
