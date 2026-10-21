using KillerBench.Engine;
using KillerBench.Engine.Core;

internal static class PackCommands
{
    private const string Usage = "Usage: pack list | pack export PACK_ID OUTPUT.json | pack validate PACK.json (read-only; no recipe runs)";

    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 2) { Console.Error.WriteLine(Usage); return 2; }
        var library = WindowsPrimitiveLibrary.Create();
        switch (args[1])
        {
            case "list" when args.Length == 2:
                foreach (var pack in BuiltIn())
                    Console.WriteLine(pack.Id + "\t" + pack.Recipes.Count + " recipes\t" + pack.Title);
                Console.WriteLine("Primitives: " + string.Join(", ", library.Probes.Select(p => p.Name).Concat(library.Actions.Select(a => a.Name)).Order(StringComparer.Ordinal)));
                return 0;
            case "export" when args.Length == 4:
            {
                var pack = BuiltIn().SingleOrDefault(candidate => candidate.Id == args[2]);
                if (pack is null) { Console.Error.WriteLine("Unknown built-in pack. Use pack list."); return 2; }
                var validation = RecipePackValidation.Validate(pack, library);
                if (!validation.IsValid) { Report(validation); return 3; }
                string path = Path.GetFullPath(args[3]);
                if (File.Exists(path)) { Console.Error.WriteLine("Output exists; choose a new path."); return 2; }
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                await File.WriteAllTextAsync(path, RecipePackJson.Serialize(pack)).ConfigureAwait(false);
                Console.WriteLine("Exported " + pack.Recipes.Count + " recipes to " + path);
                return 0;
            }
            case "validate" when args.Length == 3:
            {
                RecipePack pack;
                try { pack = await RecipePackJson.LoadAsync(args[2]).ConfigureAwait(false); }
                catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException)
                { Console.Error.WriteLine("Pack could not be loaded: " + error.Message); return 3; }
                var validation = RecipePackValidation.Validate(pack, library);
                // Summarize only the entries that exist; null entries are reported as validation issues below.
                var recipes = (pack.Recipes ?? []).Where(recipe => recipe is not null).ToArray();
                Console.WriteLine(pack.Id + " revision " + pack.Revision + ": " + recipes.Length + " recipes, " +
                    recipes.Count(recipe => recipe.DiagnosticOnly) + " diagnostic-only.");
                if (validation.IsValid) { Console.WriteLine("Valid against the " + library.Platform + " primitive library."); return 0; }
                Report(validation);
                return 3;
            }
            default:
                Console.Error.WriteLine(Usage);
                return 2;
        }
    }

    private static IReadOnlyList<RecipePack> BuiltIn() => WindowsPacks.All();

    private static void Report(RecipePackValidationResult validation)
    {
        foreach (var issue in validation.Issues)
            Console.Error.WriteLine((issue.RecipeId.Length == 0 ? "pack" : issue.RecipeId) + ": " + issue.Message);
        Console.Error.WriteLine(validation.Issues.Count + " issue(s).");
    }
}
