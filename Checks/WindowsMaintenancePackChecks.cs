using System.IO;
using KillerBench.Engine;
using KillerBench.Engine.Core;

internal static class WindowsMaintenancePackChecks
{
    internal static async Task RunAsync()
    {
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

        // Retain primitive with a scratch folder mapping is not possible without a token, so prove the no-op path only.
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
