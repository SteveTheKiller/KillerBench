using KillerBench.Engine;

internal static class ValidationReportCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        if (args.Length < 3)
        {
            Usage();
            return 2;
        }
        string output = args[1];
        string theme = "dark";
        string accent = "orange";
        bool overwrite = false;
        var inputs = new List<string>();
        for (int index = 2; index < args.Length; index++)
        {
            if (args[index] == "--overwrite") { overwrite = true; continue; }
            if (args[index] is "--theme" or "--accent")
            {
                if (++index >= args.Length) { Usage(); return 2; }
                if (args[index - 1] == "--theme") theme = args[index]; else accent = args[index];
                continue;
            }
            if (args[index].StartsWith("--", StringComparison.Ordinal)) { Usage(); return 2; }
            inputs.Add(args[index]);
        }
        if (inputs.Count is 0 or > NativeValidationEvidence.MaximumRecords) { Usage(); return 2; }
        string extension = Path.GetExtension(output);
        if (!extension.Equals(".html", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".txt", StringComparison.OrdinalIgnoreCase))
        { Console.Error.WriteLine("Output must use .html or .txt."); return 2; }
        if (File.Exists(output) && !overwrite) { Console.Error.WriteLine("Output already exists. Use --overwrite to replace it."); return 2; }
        try
        {
            var records = new List<NativeValidationRecord>();
            foreach (string input in inputs)
            {
                var info = new FileInfo(input);
                if (!info.Exists || info.Length is 0 or > NativeValidationEvidence.MaximumBytes)
                    throw new InvalidDataException("Each native validation record must be an existing file from 1 byte through 1 MiB.");
                records.Add(NativeValidationEvidence.Read(await File.ReadAllBytesAsync(info.FullName)));
            }
            var coverage = NativeValidationEvidence.Summarize(records);
            string report = extension.Equals(".html", StringComparison.OrdinalIgnoreCase)
                ? NativeValidationReport.BuildHtml(coverage, theme, accent)
                : NativeValidationReport.BuildText(coverage);
            await File.WriteAllTextAsync(output, report);
            Console.WriteLine(Path.GetFullPath(output));
            return 0;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or ArgumentException or NotSupportedException)
        { Console.Error.WriteLine("Validation report unavailable: " + error.Message); return 1; }
    }

    private static void Usage() => Console.Error.WriteLine("Usage: validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]");
}
