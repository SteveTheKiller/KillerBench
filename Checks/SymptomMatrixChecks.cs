using System.IO;
using System.Text.RegularExpressions;
using KillerBench.Engine;

/// <summary>Keeps SYMPTOM-MATRIX.md honest: pack rows must be runnable symptoms, catalog symptoms must be matrix rows, and the counts section must match the table.</summary>
internal static partial class SymptomMatrixChecks
{
    [GeneratedRegex(@"^\| ([a-z]+\.[a-z0-9-]+) \|(.*)\|\s*$")]
    private static partial Regex Row();

    internal static void Run()
    {
        string? path = Find("SYMPTOM-MATRIX.md");
        if (path is null) { Console.WriteLine("SKIP: SYMPTOM-MATRIX.md not found beside the repository; matrix consistency not checked."); return; }
        var lines = File.ReadAllLines(path);
        var rows = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string line in lines)
        {
            var match = Row().Match(line);
            if (!match.Success) continue;
            string id = match.Groups[1].Value;
            string status = match.Groups[2].Value.Split('|')[^1].Trim().Split(' ', '(')[0];
            if (!rows.TryAdd(id, status)) throw new Exception("Duplicate matrix row " + id + ".");
        }
        if (rows.Count < 100) throw new Exception("Matrix has only " + rows.Count + " rows.");
        string[] allowed = ["pack", "workflow", "diag", "planned", "out"];
        foreach (var pair in rows)
            if (!allowed.Contains(pair.Value)) throw new Exception("Matrix row " + pair.Key + " has unknown status '" + pair.Value + "'.");

        var catalog = WindowsSymptomCatalog.Build();
        var runnable = catalog.Symptoms.Where(symptom => symptom.TreeId is not null).Select(symptom => symptom.Id).ToHashSet(StringComparer.Ordinal);
        var missingTrees = rows.Where(pair => pair.Value == "pack" && !runnable.Contains(pair.Key)).Select(pair => pair.Key).ToArray();
        if (missingTrees.Length > 0) throw new Exception("Matrix rows marked pack have no runnable triage tree: " + string.Join(", ", missingTrees));
        var missingRows = catalog.Symptoms.Select(symptom => symptom.Id).Where(id => !rows.ContainsKey(id)).ToArray();
        if (missingRows.Length > 0) throw new Exception("Catalog symptoms missing from the matrix: " + string.Join(", ", missingRows));

        var counts = rows.Values.GroupBy(value => value).ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        foreach (string status in allowed)
        {
            var declared = lines.FirstOrDefault(line => line.StartsWith("- " + status + ": ", StringComparison.Ordinal));
            int expected = counts.GetValueOrDefault(status);
            if (declared is null || declared != "- " + status + ": " + expected)
                throw new Exception("Matrix count for " + status + " should read '- " + status + ": " + expected + "' but is '" + (declared ?? "missing") + "'.");
        }
        var rowLine = lines.FirstOrDefault(line => line.StartsWith("- rows: ", StringComparison.Ordinal));
        if (rowLine != "- rows: " + rows.Count) throw new Exception("Matrix row count should read '- rows: " + rows.Count + "' but is '" + (rowLine ?? "missing") + "'.");
        Console.WriteLine("PASS: symptom matrix has " + rows.Count + " rows, every pack row is a runnable catalog symptom, every catalog symptom is a row, and the counts section matches.");
    }

    private static string? Find(string name)
    {
        foreach (string start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
            for (var directory = new DirectoryInfo(start); directory is not null; directory = directory.Parent)
            {
                string candidate = Path.Combine(directory.FullName, name);
                if (File.Exists(candidate)) return candidate;
            }
        return null;
    }
}
