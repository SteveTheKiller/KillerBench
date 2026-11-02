using System.IO;
using System.Text.Json;
using KillerBench.Cli;

internal static class CliDebloatRecoveryChecks
{
    internal static async Task RunAsync(string scratch)
    {
        string root = Path.Combine(scratch, "cli-power-recovery-" + Guid.NewGuid().ToString("N"));
        foreach (string kind in new[] { "power-plan", "power-timers", "hibernation" })
        {
            string folder = Path.Combine(root, kind);
            var result = await InvokeAsync(["debloat-recover", kind, "--folder", folder, "--json"]);
            if (result.Exit != 0 || result.Error.Length != 0) throw new Exception("Explicit power recovery did not handle absent evidence: " + result.Error);
            using var json = JsonDocument.Parse(result.Output);
            if (json.RootElement.GetProperty("outcome").GetString() != "NoPendingRecord" || Directory.Exists(folder))
                throw new Exception("Absent power recovery created evidence or claimed a repeated operation.");
        }
        foreach (string[] arguments in new[]
        {
            new[] { "debloat-recover", "power-plan" },
            new[] { "debloat-recover", "unknown", "--folder", root },
            new[] { "debloat-recover", "power-timers", "--folder", root, "--execute" },
            new[] { "debloat-recover", "hibernation", "--folder", root, "--folder", root }
        })
        {
            var invalid = await InvokeAsync(arguments);
            if (invalid.Exit != 2 || invalid.Output.Length != 0 || Directory.Exists(root))
                throw new Exception("Power recovery accepted missing, duplicate or execution arguments.");
        }
        string malformed = Path.Combine(root, "malformed");
        Directory.CreateDirectory(malformed);
        string pending = Path.Combine(malformed, "debloat-power-timers.pending.json");
        const string record = "{\"schema\":1,\"request\":null}";
        await File.WriteAllTextAsync(pending, record);
        var rejected = await InvokeAsync(["debloat-recover", "power-timers", "--folder", malformed]);
        using var report = JsonDocument.Parse(rejected.Output);
        if (rejected.Exit != 3 || report.RootElement.GetProperty("outcome").GetString() != "Inconclusive" ||
            await File.ReadAllTextAsync(pending) != record || Directory.EnumerateFiles(malformed).Count() != 1)
            throw new Exception("Malformed power recovery evidence was lost or misreported.");
        Console.WriteLine("PASS: explicit power recovery handles absent/malformed evidence and rejects execution flags; no native provider was reached.");
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
