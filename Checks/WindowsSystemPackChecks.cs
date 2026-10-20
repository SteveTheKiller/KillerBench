using KillerBench.Engine;
using KillerBench.Engine.Core;

internal static class WindowsSystemPackChecks
{
    internal static async Task RunAsync()
    {
        var library = WindowsPrimitiveLibrary.Create();
        var pack = WindowsSystemPack.Build();
        var explorerRestart = pack.Recipes.Single(recipe => recipe.Id == "shell.explorer-restart");
        if (!explorerRestart.Detect.Parameters.TryGetValue("scope", out var detectScope) || detectScope != "current-session" ||
            explorerRestart.Verify?.Parameters.TryGetValue("scope", out var verifyScope) != true || verifyScope != "current-session")
            throw new Exception("Explorer restart diagnosis and verification must inspect only the current session.");
        var validation = RecipePackValidation.Validate(pack, library);
        if (!validation.IsValid) throw new Exception("System pack is invalid: " + string.Join("; ", validation.Issues.Select(i => i.RecipeId + ": " + i.Message)));
        if (RecipePackJson.Serialize(RecipePackJson.Deserialize(RecipePackJson.Serialize(pack))) != RecipePackJson.Serialize(pack)) throw new Exception("System pack did not round-trip.");

        // Secure channel.
        var secure = pack.Recipes.Single(r => r.Id == "identity.secure-channel");
        Verdict(secure, WindowsSecureChannelProbe.Interpret(new ToolResult(0, "True\r\n", "", false)), ProbeVerdict.Clear, "secure channel True");
        Verdict(secure, WindowsSecureChannelProbe.Interpret(new ToolResult(0, "False\r\n", "", false)), ProbeVerdict.Attention, "secure channel False");
        Verdict(secure, WindowsSecureChannelProbe.Interpret(new ToolResult(0, "NotDomainJoined\r\n", "", false)), ProbeVerdict.Information, "not domain joined");
        Verdict(secure, WindowsSecureChannelProbe.Interpret(new ToolResult(3, "Error: The RPC server is unavailable", "", false)), ProbeVerdict.Unknown, "secure channel error");
        Verdict(secure, WindowsSecureChannelProbe.Interpret(new ToolResult(0, "Maybe", "", false)), ProbeVerdict.Unknown, "secure channel garbage");

        // Entra.
        var entra = pack.Recipes.Single(r => r.Id == "identity.entra-token");
        const string joined = "+----------------------------------------------------------------------+\r\n| Device State                                                         |\r\n+----------------------------------------------------------------------+\r\n\r\n             AzureAdJoined : YES\r\n          EnterpriseJoined : NO\r\n              DomainJoined : NO\r\n";
        Verdict(entra, WindowsEntraStatusProbe.Interpret(new ToolResult(0, joined + "                AzureAdPrt : YES\r\n", "", false)), ProbeVerdict.Clear, "entra joined with PRT");
        Verdict(entra, WindowsEntraStatusProbe.Interpret(new ToolResult(0, joined + "                AzureAdPrt : NO\r\n", "", false)), ProbeVerdict.Attention, "entra joined without PRT");
        Verdict(entra, WindowsEntraStatusProbe.Interpret(new ToolResult(0, joined, "", false)), ProbeVerdict.Information, "entra joined, token not reported");
        Verdict(entra, WindowsEntraStatusProbe.Interpret(new ToolResult(0, joined.Replace("AzureAdJoined : YES", "AzureAdJoined : NO").Replace("DomainJoined : NO", "DomainJoined : YES"), "", false)), ProbeVerdict.Information, "domain only");
        Verdict(entra, WindowsEntraStatusProbe.Interpret(new ToolResult(0, "nothing useful", "", false)), ProbeVerdict.Unknown, "dsregcmd garbage");
        Verdict(entra, WindowsEntraStatusProbe.Interpret(new ToolResult(0, joined + "             AzureAdJoined : NO\r\n", "", false)), ProbeVerdict.Unknown, "dsregcmd duplicate field");

        // Group Policy age.
        var policy = pack.Recipes.Single(r => r.Id == "identity.group-policy-age");
        Verdict(policy, WindowsGroupPolicyAgeProbe.Interpret(new ToolResult(0, "1.5", "", false)), ProbeVerdict.Clear, "policy fresh");
        Verdict(policy, WindowsGroupPolicyAgeProbe.Interpret(new ToolResult(0, "72.25", "", false)), ProbeVerdict.Attention, "policy stale");
        Verdict(policy, WindowsGroupPolicyAgeProbe.Interpret(new ToolResult(0, "NeverApplied", "", false)), ProbeVerdict.Attention, "policy never applied");
        Verdict(policy, WindowsGroupPolicyAgeProbe.Interpret(new ToolResult(0, "NotDomainJoined", "", false)), ProbeVerdict.Information, "policy not domain joined");
        Verdict(policy, WindowsGroupPolicyAgeProbe.Interpret(new ToolResult(1, "", "Access denied", false)), ProbeVerdict.Unknown, "policy denied");
        Verdict(policy, WindowsGroupPolicyAgeProbe.Interpret(new ToolResult(0, "-5", "", false)), ProbeVerdict.Unknown, "policy negative age");

        // Boot sequence and memory diagnostic.
        var memory = pack.Recipes.Single(r => r.Id == "hardware.memory-diagnostic");
        Verdict(memory, WindowsBcdBootSequenceProbe.Interpret(new ToolResult(0, "Windows Boot Manager\r\n--------------------\r\nidentifier              {bootmgr}\r\nbootsequence            {memdiag}\r\n", "", false)), ProbeVerdict.Clear, "memdiag scheduled");
        Verdict(memory, WindowsBcdBootSequenceProbe.Interpret(new ToolResult(0, "identifier              {bootmgr}\r\ndefault                 {current}\r\n", "", false)), ProbeVerdict.Repairable, "memdiag not scheduled");
        Verdict(memory, WindowsBcdBootSequenceProbe.Interpret(new ToolResult(1, "", "denied", false)), ProbeVerdict.Unknown, "bootmgr denied");
        Verdict(memory, WindowsBcdBootSequenceProbe.Interpret(new ToolResult(0, "identifier              {current}\r\n", "", false)), ProbeVerdict.Unknown, "wrong entry");

        // CPU sample expectation.
        var cpu = pack.Recipes.Single(r => r.Id == "performance.high-cpu");
        Verdict(cpu, new PrimitiveResult(true, "85.0", ""), ProbeVerdict.Attention, "cpu high");
        Verdict(cpu, new PrimitiveResult(true, "12.5", ""), ProbeVerdict.Clear, "cpu normal");

        // Disk, VSS and adapter interpretation.
        var disk = pack.Recipes.Single(r => r.Id == "performance.disk-busy");
        const string typeperf = "\"(PDH-CSV 4.0)\",\"\\\\PC\\PhysicalDisk(_Total)\\% Disk Time\"\r\n\"09/18/2026 20:00:01.000\",\"95.5\"\r\n\"09/18/2026 20:00:02.000\",\"97.0\"\r\nExiting, please wait...\r\nThe command completed successfully.\r\n";
        Verdict(disk, WindowsDiskBusyProbe.Interpret(new ToolResult(0, typeperf, "", false)), ProbeVerdict.Attention, "disk saturated");
        Verdict(disk, WindowsDiskBusyProbe.Interpret(new ToolResult(0, typeperf.Replace("95.5", "3.0").Replace("97.0", "5.0"), "", false)), ProbeVerdict.Clear, "disk idle");
        Verdict(disk, WindowsDiskBusyProbe.Interpret(new ToolResult(0, "no samples here", "", false)), ProbeVerdict.Unknown, "typeperf no samples");
        var writers = pack.Recipes.Single(r => r.Id == "backup.vss-writers");
        const string vssWriters = "Writer name: 'System Writer'\r\n   Writer Id: {e8132975-6f93-4464-a53e-1050253ae220}\r\n   State: [1] Stable\r\n   Last error: No error\r\n\r\nWriter name: 'Registry Writer'\r\n   State: [5] Waiting for completion\r\n";
        Verdict(writers, WindowsVssWritersProbe.Interpret(new ToolResult(0, vssWriters, "", false)), ProbeVerdict.Clear, "writers stable");
        Verdict(writers, WindowsVssWritersProbe.Interpret(new ToolResult(0, vssWriters.Replace("[5] Waiting for completion", "[8] Failed"), "", false)), ProbeVerdict.Attention, "writer failed");
        Verdict(writers, WindowsVssWritersProbe.Interpret(new ToolResult(0, "Writer name: 'Orphan'\r\n", "", false)), ProbeVerdict.Unknown, "writer without state");
        Verdict(writers, WindowsVssWritersProbe.Interpret(new ToolResult(1, "", "denied", false)), ProbeVerdict.Unknown, "vssadmin denied");
        var storage = pack.Recipes.Single(r => r.Id == "backup.shadow-storage");
        const string shadow = "Shadow Copy Storage association\r\n   For volume: (C:)\\\\?\\Volume{1}\\\r\n   Shadow Copy Storage volume: (C:)\\\\?\\Volume{1}\\\r\n   Used Shadow Copy Storage space: 9.5 GB (4%)\r\n   Allocated Shadow Copy Storage space: 10 GB (4%)\r\n   Maximum Shadow Copy Storage space: 10 GB (4%)\r\n";
        Verdict(storage, WindowsShadowStorageProbe.Interpret(new ToolResult(0, shadow, "", false)), ProbeVerdict.Attention, "shadow storage nearly full");
        Verdict(storage, WindowsShadowStorageProbe.Interpret(new ToolResult(0, shadow.Replace("9.5 GB (4%)", "1 GB (1%)"), "", false)), ProbeVerdict.Clear, "shadow storage roomy");
        Verdict(storage, WindowsShadowStorageProbe.Interpret(new ToolResult(0, shadow.Replace("Maximum Shadow Copy Storage space: 10 GB (4%)", "Maximum Shadow Copy Storage space: UNBOUNDED (100%)"), "", false)), ProbeVerdict.Clear, "shadow storage unbounded");
        Verdict(storage, WindowsShadowStorageProbe.Interpret(new ToolResult(0, "No items found that satisfy the query.", "", false)), ProbeVerdict.Clear, "no shadow storage");
        Verdict(storage, WindowsShadowStorageProbe.Interpret(new ToolResult(0, shadow.Replace("9.5 GB (4%)", "lots"), "", false)), ProbeVerdict.Unknown, "shadow storage unparsable");
        var network = WindowsNetworkPack.Build();
        var networkValidation = RecipePackValidation.Validate(network, library);
        if (!networkValidation.IsValid) throw new Exception("Network pack is invalid: " + string.Join("; ", networkValidation.Issues.Select(i => i.RecipeId + ": " + i.Message)));
        var adapters = network.Recipes.Single(r => r.Id == "network.disabled-adapters");
        Verdict(adapters, WindowsDisabledAdaptersProbe.Interpret(new ToolResult(0, "Ethernet|Up|Up\r\nWi-Fi|Disconnected|Up\r\n", "", false)), ProbeVerdict.Clear, "adapters enabled");
        Verdict(adapters, WindowsDisabledAdaptersProbe.Interpret(new ToolResult(0, "Ethernet|Disabled|Down\r\n", "", false)), ProbeVerdict.Attention, "adapter disabled");
        Verdict(adapters, WindowsDisabledAdaptersProbe.Interpret(new ToolResult(0, "garbage line\r\n", "", false)), ProbeVerdict.Unknown, "adapter line garbage");
        var wifi = network.Recipes.Single(r => r.Id == "network.wifi-power-saving");
        Verdict(wifi, WindowsAdapterPowerSavingProbe.Interpret(new ToolResult(0, "Enabled\r\n", "", false)), ProbeVerdict.Repairable, "wifi power saving on");
        Verdict(wifi, WindowsAdapterPowerSavingProbe.Interpret(new ToolResult(0, "Disabled\r\n", "", false)), ProbeVerdict.Clear, "wifi power saving off");
        Verdict(wifi, WindowsAdapterPowerSavingProbe.Interpret(new ToolResult(1, "", "No MSFT_NetAdapterPowerManagementSettingData objects found", false)), ProbeVerdict.Unknown, "wifi adapter absent");
        var winhttp = network.Recipes.Single(r => r.Id == "network.winhttp-proxy-reset");
        const string directBytes = "1800000000000000010000000000000000000000";
        const string proxyBytes = "180000000500000003000000" + "0a000000" + "70726f78793a38303830" + "07000000" + "3c6c6f63616c3e";
        Verdict(winhttp, WindowsWinHttpProxyProbe.Interpret(new ToolResult(0, "Absent\r\n", "", false)), ProbeVerdict.Clear, "winhttp never stored");
        Verdict(winhttp, WindowsWinHttpProxyProbe.Interpret(new ToolResult(0, directBytes + "\r\n", "", false)), ProbeVerdict.Clear, "winhttp direct");
        var proxyResult = WindowsWinHttpProxyProbe.Interpret(new ToolResult(0, proxyBytes + "\r\n", "", false));
        Verdict(winhttp, proxyResult, ProbeVerdict.Repairable, "winhttp proxy set");
        if (proxyResult.Raw != "Proxy|proxy:8080|1") throw new Exception("WinHTTP proxy evidence was not retained exactly: " + proxyResult.Raw);
        Verdict(winhttp, WindowsWinHttpProxyProbe.Interpret(new ToolResult(0, "1800000005000000030000000000000000000000\r\n", "", false)), ProbeVerdict.Unknown, "winhttp proxy flag without server");
        Verdict(winhttp, WindowsWinHttpProxyProbe.Interpret(new ToolResult(0, "18000000050000000300000064000000\r\n", "", false)), ProbeVerdict.Unknown, "winhttp length overruns");
        Verdict(winhttp, WindowsWinHttpProxyProbe.Interpret(new ToolResult(0, "zz\r\n", "", false)), ProbeVerdict.Unknown, "winhttp not hex");
        Verdict(winhttp, WindowsWinHttpProxyProbe.Interpret(new ToolResult(1, "", "denied", false)), ProbeVerdict.Unknown, "winhttp read failed");
        if (WindowsWinHttpProxyResetAction.Interpret(new ToolResult(0, "AlreadyDirect\r\n", "", false)) is not { Dispatched: false, Succeeded: true }) throw new Exception("An already-direct WinHTTP proxy was reported as changed.");
        if (WindowsWinHttpProxyResetAction.Interpret(new ToolResult(0, "Reset\r\n", "", false)) is not { Dispatched: true, Succeeded: true }) throw new Exception("A WinHTTP reset was not reported as dispatched.");
        if (WindowsWinHttpProxyResetAction.Interpret(new ToolResult(1, "", "netsh exited with 1", false)) is not { Dispatched: false, Succeeded: false }) throw new Exception("A failed WinHTTP reset was reported as successful.");
        var liveProxy = await new WindowsWinHttpProxyProbe().ObserveAsync(new Dictionary<string, string>(), TimeSpan.FromSeconds(20), CancellationToken.None);
        if (!liveProxy.Complete && string.IsNullOrEmpty(liveProxy.Error)) throw new Exception("Live WinHTTP proxy read returned an inconsistent result.");

        // Live read-only probes: dsregcmd and a CPU sample.
        var liveEntra = await new WindowsEntraStatusProbe().ObserveAsync(new Dictionary<string, string>(), TimeSpan.FromSeconds(20), CancellationToken.None);
        if (!liveEntra.Complete && !liveEntra.TimedOut && string.IsNullOrEmpty(liveEntra.Error)) throw new Exception("Live dsregcmd probe returned an inconsistent result.");
        var liveCpu = await new WindowsTopCpuProbe().ObserveAsync(new Dictionary<string, string> { ["seconds"] = "1" }, TimeSpan.FromSeconds(10), CancellationToken.None);
        if (!liveCpu.Complete || !double.TryParse(liveCpu.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double percent) || percent < 0 || percent > 100)
            throw new Exception("Live CPU sample was not a percentage: " + (liveCpu.Value ?? liveCpu.Error));

        // Explicit-option recipes never dispatch through the plain runner.
        var runner = new RecipeRunner(new PrimitiveLibrary("Windows")
            .AddProbe(new Fake("bcd.bootsequence", _ => new PrimitiveResult(true, null, "")))
            .AddProbe(new Fake("process.running", _ => new PrimitiveResult(true, "Running", "")))
            .AddAction(new FakeAction("memory-diagnostic.schedule", RecipeRisk.Persistent, true))
            .AddAction(new FakeAction("explorer.restart", RecipeRisk.Reversible, false)), new MemoryRecipeJournal(), new MemoryRecipeOperationStore());
        foreach (var recipe in new[] { memory, pack.Recipes.Single(r => r.Id == "shell.explorer-restart") })
        {
            var result = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true });
            if (result.Outcome != RecipeRunOutcome.RepairAvailable || result.Action is not null) throw new Exception(recipe.Id + " dispatched without explicit options.");
        }

        // Foxit per-user updater: policy precondition, option substitution, explicit gate and read-back, all substituted.
        var foxit = pack.Recipes.Single(r => r.Id == "apps.foxit-auto-update-off");
        Verdict(foxit, new PrimitiveResult(true, "2", "String: 2"), ProbeVerdict.Repairable, "foxit automatic install");
        Verdict(foxit, new PrimitiveResult(true, "1", "String: 1"), ProbeVerdict.Repairable, "foxit download and prompt");
        Verdict(foxit, new PrimitiveResult(true, "0", "String: 0"), ProbeVerdict.Clear, "foxit updates off");
        Verdict(foxit, new PrimitiveResult(true, null, "value absent"), ProbeVerdict.Repairable, "foxit default updates");
        Verdict(foxit, PrimitiveResult.Unavailable("denied"), ProbeVerdict.Unknown, "foxit read failed");
        foreach (bool managed in new[] { false, true })
        {
            string? updateMode = "2";
            int writes = 0;
            string? writtenKey = null;
            var foxitLibrary = new PrimitiveLibrary("Windows")
                .AddProbe(new StoreProbe("registry.value", library.FindProbe("registry.value")!.Parameters, p =>
                    p["hive"] == "HKLM" ? new PrimitiveResult(true, managed ? "Updater.fpi" : null, managed ? "String: Updater.fpi" : "value absent") : new PrimitiveResult(true, updateMode, "String: " + updateMode)))
                .AddAction(new StoreAction("registry.set-string", library.FindAction("registry.set-string")!.Parameters, p => { writes++; writtenKey = p["key"]; updateMode = p["value"]; return new ActionResult(true, true, "prior=String:2; after=String:0"); }));
            var foxitRunner = new RecipeRunner(foxitLibrary, new MemoryRecipeJournal(), new MemoryRecipeOperationStore());
            var options = new Dictionary<string, string> { ["product"] = "Foxit PDF Editor 12.0" };
            var gated = await foxitRunner.RunAsync(foxit, new RecipeRunOptions { AuthorizeRepair = true, Options = options });
            if (gated.Outcome != RecipeRunOutcome.RepairAvailable || writes != 0) throw new Exception("Foxit updater change dispatched without explicit confirmation.");
            var run = await foxitRunner.RunAsync(foxit, new RecipeRunOptions { AuthorizeRepair = true, Options = options, ExplicitOptionsConfirmed = true });
            if (managed && (run.Outcome != RecipeRunOutcome.Blocked || writes != 0)) throw new Exception("Foxit updater change ignored machine policy: " + run.Outcome);
            if (!managed && (run.Outcome != RecipeRunOutcome.Repaired || writes != 1 || writtenKey != @"Software\Foxit Software\Foxit PDF Editor 12.0\plugins\Updater"))
                throw new Exception("Foxit updater change did not write the exact product key and verify: " + run.Outcome + " " + writtenKey);
        }
        var guardLibrary = new PrimitiveLibrary("Windows")
            .AddProbe(new StoreProbe("registry.value", library.FindProbe("registry.value")!.Parameters, _ => new PrimitiveResult(true, "2", "String: 2")))
            .AddAction(new StoreAction("registry.set-string", library.FindAction("registry.set-string")!.Parameters, _ => throw new InvalidOperationException("A malformed Foxit product name reached the registry.")));
        var badProduct = await new RecipeRunner(guardLibrary, new MemoryRecipeJournal(), new MemoryRecipeOperationStore()).RunAsync(foxit, new RecipeRunOptions { AuthorizeRepair = true, ExplicitOptionsConfirmed = true, Options = new Dictionary<string, string> { ["product"] = @"Foxit PDF Editor\..\x" } });
        if (badProduct.Outcome == RecipeRunOutcome.Repaired || badProduct.Action is not null) throw new Exception("A malformed Foxit product name reached the registry.");
        Console.WriteLine("PASS: Windows system pack (" + pack.Recipes.Count + " recipes): secure channel, Entra, Group Policy age and boot sequence interpretation fixtures; Foxit updater policy, option and explicit gates substituted; live dsregcmd and CPU sample were read-only; explicit-option recipes never dispatched.");
    }

    private static void Verdict(RecipeDefinition recipe, PrimitiveResult result, ProbeVerdict expected, string what)
    {
        var evaluation = Expectation.Evaluate(recipe.Detect.Expect, result);
        if (evaluation.Verdict != expected) throw new Exception(what + " expected " + expected + " but was " + evaluation.Verdict + ": " + evaluation.Detail);
    }

    private sealed class StoreProbe(string name, IReadOnlyList<PrimitiveParameter> parameters, Func<IReadOnlyDictionary<string, string>, PrimitiveResult> observe) : IProbePrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters => parameters;
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> p, TimeSpan timeout, CancellationToken token) => Task.FromResult(observe(p));
    }

    private sealed class StoreAction(string name, IReadOnlyList<PrimitiveParameter> parameters, Func<IReadOnlyDictionary<string, string>, ActionResult> execute) : IActionPrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters => parameters;
        public RecipeRisk Risk => RecipeRisk.Persistent;
        public bool RequiresElevation => false;
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> p, TimeSpan timeout, IProgress<string>? progress, CancellationToken token) => Task.FromResult(execute(p));
    }

    private sealed class Fake(string name, Func<IReadOnlyDictionary<string, string>, PrimitiveResult> observe) : IProbePrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters { get; } = name == "process.running" ? [new("name", true, "n")] : [];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> p, TimeSpan timeout, CancellationToken token) => Task.FromResult(observe(p));
    }

    private sealed class FakeAction(string name, RecipeRisk risk, bool elevation) : IActionPrimitive
    {
        public string Name => name;
        public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
        public RecipeRisk Risk => risk;
        public bool RequiresElevation => elevation;
        public Task<ActionResult> ExecuteAsync(IReadOnlyDictionary<string, string> p, TimeSpan timeout, IProgress<string>? progress, CancellationToken token) =>
            throw new InvalidOperationException("Explicit-option action must not be dispatched by the plain runner.");
    }
}
