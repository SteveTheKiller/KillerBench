using System.Text.Json;
using KillerBench.Engine;
using KillerBench.Engine.Core;

internal static class SymptomCommands
{
    private const string Usage = "Usage: symptom list | symptom search TEXT | symptom run SYMPTOM_ID [--repair] [--answer NODE=KEY]... [--json] (detect only unless --repair; --repair applies pack recipes once each with fresh rechecks)";

    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 2) { Console.Error.WriteLine(Usage); return 2; }
        var catalog = WindowsSymptomCatalog.Build();
        switch (args[1])
        {
            case "list" when args.Length == 2:
                foreach (var symptom in catalog.Symptoms.OrderBy(symptom => symptom.Area, StringComparer.Ordinal).ThenBy(symptom => symptom.Id, StringComparer.Ordinal))
                    Console.WriteLine(symptom.Id + "\t" + symptom.Area + "\t" + symptom.Title + (symptom.TreeId is null ? "\t(guidance only)" : ""));
                return 0;
            case "search" when args.Length >= 3:
            {
                var matches = catalog.Search(string.Join(' ', args.Skip(2)));
                if (matches.Count == 0) { Console.WriteLine("No matching symptom. Use symptom list."); return 3; }
                foreach (var symptom in matches) Console.WriteLine(symptom.Id + "\t" + symptom.Title);
                return 0;
            }
            case "run" when args.Length >= 3:
            {
                string id = args[2];
                bool repair = false, json = false;
                var answers = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int i = 3; i < args.Length; i++)
                {
                    if (args[i] == "--repair") repair = true;
                    else if (args[i] == "--json") json = true;
                    else if (args[i] == "--answer" && i + 1 < args.Length && args[i + 1].Split('=', 2) is [var node, var key]) { answers[node] = key; i++; }
                    else { Console.Error.WriteLine(Usage); return 2; }
                }
                if (catalog.FindSymptom(id) is null) { Console.Error.WriteLine("Unknown symptom '" + id + "'. Use symptom list or symptom search."); return 2; }
                using var stop = new CancellationTokenSource();
                Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "KillerBench", "Triage");
                var journal = new FileRecipeJournal(Path.Combine(folder, "triage-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8] + ".jsonl"));
                var runner = new GoalRunner(catalog, WindowsPacks.All(), WindowsPrimitiveLibrary.Create(), journal);
                var options = new GoalRunOptions
                {
                    AuthorizeRepair = repair,
                    Elevated = IsElevated(),
                    Progress = json ? null : new Progress<string>(line => Console.Error.WriteLine(line)),
                    Answer = (node, _, _) => Task.FromResult(answers.GetValueOrDefault(node.Id))
                };
                var report = await runner.RunAsync(id, options, stop.Token).ConfigureAwait(false);
                if (json) { Console.WriteLine(JsonSerializer.Serialize(report, RecipePackJson.Options)); }
                else
                {
                    Console.WriteLine(report.SymptomTitle + ": " + report.Outcome);
                    Console.WriteLine(report.Detail);
                    foreach (var step in report.Steps) Console.WriteLine("  " + step.Index + ". " + step.Summary);
                    if (report.RuledOut.Count > 0) Console.WriteLine("Ruled out: " + string.Join(", ", report.RuledOut));
                    if (report.Repaired.Count > 0) Console.WriteLine("Repaired: " + string.Join(", ", report.Repaired));
                    if (report.Unresolved.Count > 0) Console.WriteLine("Unresolved: " + string.Join(", ", report.Unresolved));
                    foreach (var question in report.OpenQuestions)
                        Console.WriteLine("Question: " + question.Text + " Options: " + string.Join(", ", question.Options.Select(option => option.Key + " (" + option.Label + ")")));
                    if (!string.IsNullOrWhiteSpace(report.Guidance)) Console.WriteLine("Next: " + report.Guidance);
                    Console.WriteLine("Journal: " + journal.Path);
                }
                return report.Outcome switch
                {
                    GoalOutcome.Resolved or GoalOutcome.RuledOut => 0,
                    GoalOutcome.Canceled => 130,
                    GoalOutcome.RestartRequired => 4,
                    _ => 3
                };
            }
            default:
                Console.Error.WriteLine(Usage);
                return 2;
        }
    }

    private static bool IsElevated()
    {
        if (!OperatingSystem.IsWindows()) return false;
        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        return new System.Security.Principal.WindowsPrincipal(identity).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
    }
}
