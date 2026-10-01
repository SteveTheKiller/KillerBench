using KillerBench.Engine;
using KillerBench.Engine.Core;
using Microsoft.Win32;

internal static class WindowsSettingsPackChecks
{
    private static readonly string[] MultiStringFixture = ["a", "b"];

    private sealed class FakeProbe(string name, IReadOnlyList<PrimitiveParameter> parameters, Func<IReadOnlyDictionary<string, string>, PrimitiveResult> observe) : IProbePrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters => parameters;
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> p, TimeSpan timeout, CancellationToken token) => Task.FromResult(observe(p));
    }

    private sealed class FakeAction(string name, IReadOnlyList<PrimitiveParameter> parameters, RecipeRisk risk, bool elevation, Func<IReadOnlyDictionary<string, string>, ActionResult> execute) : IActionPrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters => parameters;
        public RecipeRisk Risk => risk;
        public bool RequiresElevation => elevation;
        public int Calls;
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> p, TimeSpan timeout, IProgress<string>? progress, CancellationToken token) { Calls++; return Task.FromResult(execute(p)); }
    }

    internal static async Task RunAsync()
    {
        var library = WindowsPrimitiveLibrary.Create();
        var pack = WindowsSettingsPack.Build();
        var validation = RecipePackValidation.Validate(pack, library);
        if (!validation.IsValid) throw new Exception("Settings pack is invalid: " + string.Join("; ", validation.Issues.Select(i => i.RecipeId + ": " + i.Message)));
        if (RecipePackJson.Serialize(RecipePackJson.Deserialize(RecipePackJson.Serialize(pack))) != RecipePackJson.Serialize(pack)) throw new Exception("Settings pack did not round-trip.");
        if (WindowsPacks.All().SelectMany(p => p.Recipes).GroupBy(r => r.Id).Any(g => g.Count() > 1)) throw new Exception("Built-in packs share a recipe id.");

        // BCD interpretation.
        var normal = WindowsBcdSafebootProbe.Interpret(new ToolResult(0, "Windows Boot Loader\r\n-------------------\r\nidentifier              {current}\r\ndevice                  partition=C:\r\npath                    \\WINDOWS\\system32\\winload.efi\r\n", "", false));
        if (normal is not { Complete: true, Value: null }) throw new Exception("Normal boot entry was not reported as absent safeboot.");
        var safe = WindowsBcdSafebootProbe.Interpret(new ToolResult(0, "identifier              {current}\r\nsafeboot                Minimal\r\n", "", false));
        if (safe is not { Complete: true, Value: "Minimal" }) throw new Exception("Safe Mode flag was not read.");
        if (WindowsBcdSafebootProbe.Interpret(new ToolResult(0, "identifier              {default}\r\n", "", false)).Complete) throw new Exception("Non-current entry was accepted.");
        if (WindowsBcdSafebootProbe.Interpret(new ToolResult(0, "identifier              {current}\r\nsafeboot                Minimal\r\nsafeboot                Network\r\n", "", false)).Complete) throw new Exception("Duplicate safeboot values were accepted.");
        if (WindowsBcdSafebootProbe.Interpret(new ToolResult(1, "", "access denied", false)).Complete) throw new Exception("Failed bcdedit was accepted.");
        if (!WindowsBcdSafebootProbe.Interpret(new ToolResult(0, "identifier {current}", "", false) { TimedOut = true }).TimedOut) throw new Exception("Timeout was not retained.");
        var safebootRecipe = pack.Recipes.Single(r => r.Id == "boot.safeboot-clear");
        if (Expectation.Evaluate(safebootRecipe.Detect.Expect, safe).Verdict != ProbeVerdict.Repairable || Expectation.Evaluate(safebootRecipe.Detect.Expect, normal).Verdict != ProbeVerdict.Clear ||
            Expectation.Evaluate(safebootRecipe.Detect.Expect, new PrimitiveResult(true, "Weird", "")).Verdict != ProbeVerdict.Unknown)
            throw new Exception("Safeboot expectations misbehaved.");

        // Registry rendering and path rules.
        if (WindowsRegistryPrimitives.Render(unchecked((int)0xFFFFFFFF), RegistryValueKind.DWord) != "4294967295" || WindowsRegistryPrimitives.Render(MultiStringFixture,RegistryValueKind.MultiString) != "a;b" ||
            WindowsRegistryPrimitives.Render(new byte[] { 1, 255 }, RegistryValueKind.Binary) != "01FF") throw new Exception("Registry rendering misbehaved.");
        foreach (string bad in new[] { @"..\Foo", @"\Foo", @"Foo\", @"Foo\\Bar" })
        {
            if (WindowsRegistryPrimitives.TryOpen(new Dictionary<string, string> { ["hive"] = "HKLM", ["key"] = bad }, false, out var key, out _)) { key?.Dispose(); throw new Exception("Malformed key path was accepted: " + bad); }
        }
        if (WindowsRegistryPrimitives.TryOpen(new Dictionary<string, string> { ["hive"] = "HKCR", ["key"] = "Foo" }, false, out var hiveKey, out _)) { hiveKey?.Dispose(); throw new Exception("Unsupported hive was accepted."); }
        var fastStartup = pack.Recipes.Single(r => r.Id == "power.fast-startup-disable");
        if (Expectation.Evaluate(fastStartup.Detect.Expect, new PrimitiveResult(true, "1", "")).Verdict != ProbeVerdict.Repairable ||
            Expectation.Evaluate(fastStartup.Detect.Expect, new PrimitiveResult(true, "0", "")).Verdict != ProbeVerdict.Clear ||
            Expectation.Evaluate(fastStartup.Detect.Expect, new PrimitiveResult(true, null, "")).Verdict != ProbeVerdict.Information)
            throw new Exception("Fast Startup expectations misbehaved.");
        var fips = pack.Recipes.Single(r => r.Id == "security.fips-policy-enable");
        if (Expectation.Evaluate(fips.Detect.Expect, new PrimitiveResult(true, "1", "")).Verdict != ProbeVerdict.Clear ||
            Expectation.Evaluate(fips.Detect.Expect, new PrimitiveResult(true, "0", "")).Verdict != ProbeVerdict.Repairable ||
            Expectation.Evaluate(fips.Detect.Expect, new PrimitiveResult(true, null, "")).Verdict != ProbeVerdict.Repairable ||
            Expectation.Evaluate(fips.Detect.Expect, new PrimitiveResult(true, "7", "")).Verdict != ProbeVerdict.Unknown ||
            Expectation.Evaluate(fips.Preconditions[0].Expect, new PrimitiveResult(true, "1", "")).Verdict == ProbeVerdict.Clear ||
            Expectation.Evaluate(fips.Preconditions[0].Expect, new PrimitiveResult(true, null, "")).Verdict != ProbeVerdict.Clear ||
            !fips.RequiresExplicitOptions || !fips.RequiresElevation || fips.Rollback is null)
            throw new Exception("FIPS policy expectations or gates misbehaved.");
        var tpm = pack.Recipes.Single(r => r.Id == "security.tpm-initialize");
        ProbeVerdict Tpm(string output, int exit = 0) => Expectation.Evaluate(tpm.Detect.Expect, WindowsTpmStatusProbe.Interpret(new ToolResult(exit, output, exit == 0 ? "" : "denied", false))).Verdict;
        if (Tpm("True|True|True|True\r\n") != ProbeVerdict.Clear || Tpm("True|False|True|False\r\n") != ProbeVerdict.Repairable ||
            Tpm("False|False|False|False\r\n") != ProbeVerdict.Information || Tpm("True|maybe|True|True\r\n") != ProbeVerdict.Unknown || Tpm("", 1) != ProbeVerdict.Unknown)
            throw new Exception("TPM readiness expectations misbehaved.");
        if (WindowsTpmInitializeAction.Interpret(new ToolResult(0, "AlreadyReady\r\n", "", false)) is not { Dispatched: false, Succeeded: true } ||
            WindowsTpmInitializeAction.Interpret(new ToolResult(0, "True|False|False|False|False\r\n", "", false)) is not { Dispatched: true, Succeeded: true, RestartRequired: false } ||
            WindowsTpmInitializeAction.Interpret(new ToolResult(0, "False|True|False|False|False\r\n", "", false)) is not { Dispatched: true, Succeeded: true, RestartRequired: true } ||
            WindowsTpmInitializeAction.Interpret(new ToolResult(0, "False|False|False|True|False\r\n", "", false)) is not { Succeeded: false, Error: not null } ||
            WindowsTpmInitializeAction.Interpret(new ToolResult(0, "False|False|False|False|True\r\n", "", false)) is not { Succeeded: false } ||
            WindowsTpmInitializeAction.Interpret(new ToolResult(1, "", "Access denied", false)) is not { Dispatched: false, Succeeded: false })
            throw new Exception("TPM initialization outcomes misbehaved.");
        if (!tpm.RequiresExplicitOptions || !tpm.RequiresElevation || !tpm.MayRequireRestart) throw new Exception("TPM initialization gates misbehaved.");
        var profile = pack.Recipes.Single(r => r.Id == "profile.temporary-detect");
        if (Expectation.Evaluate(profile.Detect.Expect, new PrimitiveResult(true, "S-1-5-18;S-1-5-21-1-2-3-1001;S-1-5-21-1-2-3-1001.bak", "")).Verdict != ProbeVerdict.Attention ||
            Expectation.Evaluate(profile.Detect.Expect, new PrimitiveResult(true, "S-1-5-18;S-1-5-21-1-2-3-1001", "")).Verdict != ProbeVerdict.Clear)
            throw new Exception("Temporary profile expectations misbehaved.");

        // Live read-only probes on this machine: registry value read and process inventory only.
        var value = await new WindowsRegistryValueProbe().ObserveAsync(new Dictionary<string, string> { ["hive"] = "HKLM", ["key"] = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion", ["name"] = "CurrentBuild" }, TimeSpan.FromSeconds(5), CancellationToken.None);
        if (!value.Complete || string.IsNullOrEmpty(value.Value)) throw new Exception("Live CurrentBuild read failed: " + value.Error);
        var absent = await new WindowsRegistryValueProbe().ObserveAsync(new Dictionary<string, string> { ["hive"] = "HKCU", ["key"] = @"Software\KillerBench-Checks-Absent-" + Guid.NewGuid().ToString("N"), ["name"] = "Nope" }, TimeSpan.FromSeconds(5), CancellationToken.None);
        if (absent is not { Complete: true, Value: null }) throw new Exception("Absent key was not reported as absent.");
        var explorer = await new WindowsProcessRunningProbe().ObserveAsync(new Dictionary<string, string> { ["name"] = "explorer" }, TimeSpan.FromSeconds(5), CancellationToken.None);
        if (!explorer.Complete || explorer.Value is not ("Running" or "NotRunning")) throw new Exception("Process probe failed: " + explorer.Error);

        // Substituted runs: explicit-option recipe is never dispatched; safeboot clears through the runner.
        var setDword = new FakeAction("registry.set-dword", library.FindAction("registry.set-dword")!.Parameters, RecipeRisk.Persistent, true, _ => new ActionResult(true, true, "prior=DWord:1; after=0"));
        var clearSafeboot = new FakeAction("bcd.safeboot-clear", [], RecipeRisk.Persistent, true, _ => new ActionResult(true, true, "The operation completed successfully."));
        string safeboot = "Minimal";
        var substituted = new PrimitiveLibrary("Windows")
            .AddProbe(new FakeProbe("registry.value", library.FindProbe("registry.value")!.Parameters, _ => new PrimitiveResult(true, "1", "")))
            .AddProbe(new FakeProbe("registry.subkeys", library.FindProbe("registry.subkeys")!.Parameters, _ => new PrimitiveResult(true, "S-1-5-18", "")))
            .AddProbe(new FakeProbe("bcd.safeboot", [], _ => new PrimitiveResult(true, safeboot, "")))
            .AddProbe(new FakeProbe("tpm.status", [], _ => new PrimitiveResult(true, "Ready", "")))
            .AddProbe(new FakeProbe("process.running", library.FindProbe("process.running")!.Parameters, _ => new PrimitiveResult(true, "Running", "")))
            .AddAction(setDword).AddAction(clearSafeboot)
            .AddAction(new FakeAction("tpm.initialize", [], RecipeRisk.Persistent, true, _ => throw new InvalidOperationException("TPM initialization must not run in substituted checks.")))
            .AddAction(new FakeAction("explorer.start", [], RecipeRisk.Reversible, false, _ => new ActionResult(true, true, "started")));
        if (!RecipePackValidation.Validate(pack, substituted).IsValid) throw new Exception("Settings pack did not validate against the substituted library.");
        var runner = new RecipeRunner(substituted, new MemoryRecipeJournal());
        var explicitOnly = await runner.RunAsync(fastStartup, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true });
        if (explicitOnly.Outcome != RecipeRunOutcome.RepairAvailable || setDword.Calls != 0) throw new Exception("Explicit-option Fast Startup recipe dispatched without explicit options.");
        clearSafeboot.Calls = 0;
        var cleared = await runner.RunAsync(safebootRecipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true });
        if (cleared.Outcome != RecipeRunOutcome.NeedsAttention || !cleared.ExecutionUncertain || clearSafeboot.Calls != 1) throw new Exception("Safeboot verify with the flag still present was not reported as uncertain: " + cleared.Outcome);
        safeboot = "Minimal";
        var fixedRunner = new RecipeRunner(new PrimitiveLibrary("Windows").AddProbe(new FakeProbe("bcd.safeboot", [], _ => { var current = safeboot; return new PrimitiveResult(true, current, ""); }))
            .AddAction(new FakeAction("bcd.safeboot-clear", [], RecipeRisk.Persistent, true, _ => { safeboot = null!; return new ActionResult(true, true, "ok"); })), new MemoryRecipeJournal());
        var repaired = await fixedRunner.RunAsync(safebootRecipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true });
        if (repaired.Outcome != RecipeRunOutcome.Repaired) throw new Exception("Safeboot clear was not verified: " + repaired.Detail);
        safeboot = "Minimal";
        var standardUser = await fixedRunner.RunAsync(safebootRecipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = false });
        if (standardUser.Outcome != RecipeRunOutcome.Blocked || safeboot != "Minimal") throw new Exception("Standard user dispatched a BCD change.");

        Console.WriteLine("PASS: Windows settings pack (" + pack.Recipes.Count + " recipes) validates, BCD and registry interpretation fixtures pass, live reads were read-only and substituted runs never changed this machine.");
    }
}
