using KillerBench.Engine;
using KillerBench.Engine.Core;

internal static class WindowsSystemPackChecks
{
    internal static async Task RunAsync()
    {
        var library = WindowsPrimitiveLibrary.Create();
        var pack = WindowsSystemPack.Build();
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
            .AddAction(new FakeAction("explorer.restart", RecipeRisk.Reversible, false)), new MemoryRecipeJournal());
        foreach (var recipe in new[] { memory, pack.Recipes.Single(r => r.Id == "shell.explorer-restart") })
        {
            var result = await runner.RunAsync(recipe, new RecipeRunOptions { AuthorizeRepair = true, Elevated = true });
            if (result.Outcome != RecipeRunOutcome.RepairAvailable || result.Action is not null) throw new Exception(recipe.Id + " dispatched without explicit options.");
        }
        Console.WriteLine("PASS: Windows system pack (" + pack.Recipes.Count + " recipes): secure channel, Entra, Group Policy age and boot sequence interpretation fixtures; live dsregcmd and CPU sample were read-only; explicit-option recipes never dispatched.");
    }

    private static void Verdict(RecipeDefinition recipe, PrimitiveResult result, ProbeVerdict expected, string what)
    {
        var evaluation = Expectation.Evaluate(recipe.Detect.Expect, result);
        if (evaluation.Verdict != expected) throw new Exception(what + " expected " + expected + " but was " + evaluation.Verdict + ": " + evaluation.Detail);
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
