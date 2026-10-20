using System.IO;
using System.Text.Json;
using KillerBench.Engine;
using KillerBench.Engine.Core;

internal static class WindowsEntraPrivacyChecks
{
    internal static async Task RunAsync(string scratchRoot)
    {
        string folder = Path.Combine(scratchRoot, "entra-privacy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string[] markers = ["PRIVATE-DEVICE-71", "PRIVATE-TENANT-72", "PRIVATE-ACCOUNT-73", "PRIVATE-KEY-74", "private-url-75.invalid", "PRIVATE-STDERR-76"];
            string identities = "DeviceId : " + markers[0] + "\nTenantId : " + markers[1] + "\nUserEmail : " + markers[2] + "\nKeyContainerId : " + markers[3] + "\nMdmUrl : https://" + markers[4] + "\n";
            const string flags = "AzureAdJoined : YES\nDomainJoined : NO\nAzureAdPrt : YES\n";
            (ToolResult Tool, bool Complete, string? Value)[] cases =
            [
                (new(0, identities + flags, markers[5], false), true, "YES/YES"),
                (new(0, flags + identities, markers[5], true), false, null),
                (new(0, identities + flags, markers[5], false) { TimedOut = true }, false, null),
                (new(3, identities + flags, markers[5], false), false, null),
                (new(0, identities, markers[5], false), false, null),
                (new(0, flags + "AzureAdJoined : NO\n" + identities, markers[5], false), false, null),
                (new(0, "AzureAdJoined : YES\nDomainJoined : NO\n" + identities, markers[5], false), true, "YES/NONE"),
                (new(0, "AzureAdJoined : NO\nDomainJoined : YES\n" + identities, markers[5], false), true, "NO/NONE"),
                (new(0, flags.Replace("AzureAdPrt : YES", "AzureAdPrt : NO") + identities, markers[5], false), true, "YES/NO")
            ];
            var recipe = WindowsSystemPack.Build().Recipes.Single(recipe => recipe.Id == "identity.entra-token");
            int index = 0;
            foreach (var item in cases)
            {
                var result = WindowsEntraStatusProbe.Interpret(item.Tool);
                if (result.Complete != item.Complete || result.Value != item.Value) throw new Exception("Entra privacy projection changed diagnostic behavior.");
                string path = Path.Combine(folder, (++index) + ".jsonl");
                var journal = new FileRecipeJournal(path);
                var library = new PrimitiveLibrary("Windows").AddProbe(new EntraEvidence(item.Tool));
                var progress = new CaptureProgress();
                var report = await new RecipeRunner(library, journal, new MemoryRecipeOperationStore()).RunAsync(recipe, new RecipeRunOptions { Progress = progress });
                var cliReport = new GoalReport("fixture", "Entra", GoalOutcome.Unresolved, report.Detail,
                    [new GoalStep(1, "entra", TriageNodeKind.Recipe, report.Detail) { Recipe = report }]);
                string[] ordinaryOutputs = [JsonSerializer.Serialize(result, RecipePackJson.Options), JsonSerializer.Serialize(report, RecipePackJson.Options),
                    JsonSerializer.Serialize(cliReport, RecipePackJson.Options), await File.ReadAllTextAsync(path), progress.Text];
                foreach (string output in ordinaryOutputs)
                    if (markers.Any(marker => output.Contains(marker, StringComparison.Ordinal)))
                        throw new Exception("Ordinary Entra result, report, journal, or progress retained a private identity marker.");
            }
        }
        finally { Directory.Delete(folder, true); }
    }

    private sealed class EntraEvidence(ToolResult tool) : IProbePrimitive
    {
        public string Name => "entra.status";
        public IReadOnlyList<PrimitiveParameter> Parameters { get; } = [];
        public Task<PrimitiveResult> ObserveAsync(IReadOnlyDictionary<string, string> parameters, TimeSpan timeout, CancellationToken token)
            => Task.FromResult(WindowsEntraStatusProbe.Interpret(tool));
    }

    private sealed class CaptureProgress : IProgress<string>
    {
        public string Text = "";
        public void Report(string value) => Text += value + "\n";
    }
}
