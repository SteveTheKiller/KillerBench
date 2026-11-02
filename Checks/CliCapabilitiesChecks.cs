using System.IO;
using System.Text.Json;
using KillerBench.Cli;

internal static class CliCapabilitiesChecks
{
    internal static async Task RunAsync()
    {
        foreach (string[] arguments in new[] { new[] { "capabilities" }, new[] { "capabilities", "--json" } })
        {
            var result = await InvokeAsync(arguments);
            if (result.Exit != 0 || result.Error.Length != 0) throw new Exception("CLI capability discovery failed: " + result.Error);
            using var json = JsonDocument.Parse(result.Output);
            var root = json.RootElement;
            if (root.GetProperty("schemaVersion").GetInt32() != 1 || string.IsNullOrWhiteSpace(root.GetProperty("version").GetString()))
                throw new Exception("CLI capability discovery omitted its versioned contract.");
            string[] commands = root.GetProperty("commands").EnumerateArray().Select(item => item.GetString()!).ToArray();
            if (commands.Length < 300 || commands.Distinct(StringComparer.Ordinal).Count() != commands.Length ||
                !commands.SequenceEqual(commands.Order(StringComparer.Ordinal)) ||
                !new[] { "capabilities", "full-scan", "profiles", "workflow-journal", "knowledge-search", "msi-code" }.All(commands.Contains))
                throw new Exception("CLI capability discovery lost supported operations or stable unique ordering.");
        }
        foreach (string[] arguments in new[] { new[] { "capabilities", "--execute" }, new[] { "capabilities", "unexpected" } })
        {
            var result = await InvokeAsync(arguments);
            if (result.Exit != 2 || result.Output.Length != 0 || !result.Error.Contains("Usage:", StringComparison.Ordinal))
                throw new Exception("CLI capability discovery accepted unrelated operation arguments.");
        }
        Console.WriteLine("PASS: versioned CLI capability discovery reports stable supported names and rejects execution or unrelated arguments.");
    }

    private static async Task<(int Exit, string Output, string Error)> InvokeAsync(string[] arguments)
    {
        var savedOutput = Console.Out; var savedError = Console.Error;
        using var output = new StringWriter(); using var error = new StringWriter();
        try
        {
            Console.SetOut(output); Console.SetError(error);
            int exit = await CommandEngine.RunAsync(arguments);
            return (exit, output.ToString(), error.ToString());
        }
        finally { Console.SetOut(savedOutput); Console.SetError(savedError); }
    }
}
