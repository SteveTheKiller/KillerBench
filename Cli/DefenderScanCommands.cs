using System.Text.Json;
using KillerBench.Engine;

internal static class DefenderScanCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KillerBench", "Repairs", "DefenderScan");
        if (args[0] == "defender-scan-recover")
        {
            if (args.Length != 1) return Usage();
            var recovered = await DefenderScanExecution.ReconcileAsync(folder, new WindowsDefenderScanHost()).ConfigureAwait(false);
            Console.WriteLine(JsonSerializer.Serialize(recovered, Capture.JsonOptions));
            return Exit(recovered.Outcome);
        }
        if (args.Length is < 2 or > 3 || !Enum.TryParse<DefenderScanType>(args[1], true, out var type) ||
            args.Length == 3 && args[2] != "--execute") return Usage();
        var host = new WindowsDefenderScanHost();
        var result = args.Length == 3
            ? await DefenderScanExecution.ExecuteAsync(folder, type, true, host).ConfigureAwait(false)
            : await DefenderScanExecution.PreviewAsync(type, host).ConfigureAwait(false);
        Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
        return Exit(result.Outcome);
    }

    private static int Exit(DefenderScanOutcome outcome) => outcome switch
    {
        DefenderScanOutcome.Preview or DefenderScanOutcome.Started or DefenderScanOutcome.Verified => 0,
        DefenderScanOutcome.Blocked => 2,
        _ => 3
    };

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: defender-scan Quick|Full [--execute] | defender-scan-recover");
        return 2;
    }
}
