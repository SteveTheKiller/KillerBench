using KillerBench.Engine;

internal static class PrivacyCommands
{
    internal static int Run(string[] args)
    {
        var selected = new List<PrivacyArea>();
        bool inspect = false;
        for (int i = 1; i < args.Length; i++)
        {
            if (args[i] == "--inspect" && !inspect) { inspect = true; continue; }
            if (args[i] != "--select" || ++i >= args.Length ||
                !Enum.TryParse<PrivacyArea>(args[i], true, out var area) || !Enum.IsDefined(area)) return Usage();
            selected.Add(area);
        }
        try
        {
            var options = new PrivacyOptions(selected);
            if (!inspect) { Console.WriteLine(options.Describe()); return 0; }
            var values = PrivacyInspection.Inspect(options);
            var services = PrivacyInspection.InspectServicesAsync(options).GetAwaiter().GetResult();
            Console.WriteLine(PrivacyInspection.Format(values, services));
            return values.Any(v => v.State is "Unavailable" or "Unsupported value") ||
                services.Any(service => service.State == "Unavailable") ? 3 : 0;
        }
        catch (ArgumentException e) { Console.Error.WriteLine(e.Message); return 2; }
    }

    private static int Usage()
    {
        Console.Error.WriteLine("privacy-plan [--select AREA ...] [--inspect]. Preview or read-only registry inspection. Areas: " + string.Join(", ", Enum.GetNames<PrivacyArea>()));
        return 2;
    }
}
