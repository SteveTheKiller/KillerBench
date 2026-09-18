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
            if (args.Length is < 1 or > 2 || args.Length == 2 && args[1] != "--report") return Usage();
            var recovered = await DefenderScanExecution.ReconcileAsync(folder, new WindowsDefenderScanHost()).ConfigureAwait(false);
            Console.WriteLine(args.Length == 2 ? FormatReport(recovered, recovered: true) : JsonSerializer.Serialize(recovered, Capture.JsonOptions));
            return Exit(recovered.Outcome);
        }
        if (args.Length is < 2 or > 4 || !Enum.TryParse<DefenderScanType>(args[1], true, out var type)) return Usage();
        var options = args.Skip(2).ToArray();
        if (options.Distinct(StringComparer.Ordinal).Count() != options.Length || options.Any(option => option is not "--execute" and not "--report")) return Usage();
        bool execute = options.Contains("--execute", StringComparer.Ordinal);
        bool report = options.Contains("--report", StringComparer.Ordinal);
        var host = new WindowsDefenderScanHost();
        var result = execute
            ? await DefenderScanExecution.ExecuteAsync(folder, type, true, host).ConfigureAwait(false)
            : await DefenderScanExecution.PreviewAsync(type, host).ConfigureAwait(false);
        Console.WriteLine(report ? FormatReport(result, recovered: false) : JsonSerializer.Serialize(result, Capture.JsonOptions));
        return Exit(result.Outcome);
    }

    internal static string FormatReport(DefenderScanResult result, bool recovered)
    {
        ArgumentNullException.ThrowIfNull(result);
        string next = recovered && result.Outcome == DefenderScanOutcome.Preview
            ? "No pending scan needs reconciliation. Run defender-scan Quick|Full --report to review eligibility before requesting a scan."
            : result.Outcome switch
        {
            DefenderScanOutcome.Preview => "Run the same command with --execute only after you intend to start this one scan.",
            DefenderScanOutcome.Started => "Wait for Microsoft Defender to finish, then run defender-scan-recover --report. Do not start another scan.",
            DefenderScanOutcome.Verified => "Review Defender protection and any related alerts. No additional scan is selected.",
            DefenderScanOutcome.Blocked => "Resolve the stated prerequisite, then run the read-only preview again before requesting a scan.",
            _ => "Run defender-scan-recover --report after Defender has had time to update its scan history. Do not immediately retry."
        };
        return string.Join(Environment.NewLine,
        [
            "MICROSOFT DEFENDER SCAN",
            "Scan type: " + result.Type,
            "Outcome: " + result.Outcome,
            recovered ? "Mode: Reconciliation only" : "Mode: " + (result.Outcome == DefenderScanOutcome.Preview ? "Preview" : "Scan request or result review"),
            "",
            "WHAT HAPPENED",
            result.Detail,
            "",
            "NEXT STEP",
            next,
            "",
            "SCOPE AND LIMITS",
            "KillerBench did not change Microsoft Defender exclusions, protection preferences, policy, threat remediation settings or files. A scan request does not by itself establish that the computer is free of threats."
        ]);
    }

    private static int Exit(DefenderScanOutcome outcome) => outcome switch
    {
        DefenderScanOutcome.Preview or DefenderScanOutcome.Started or DefenderScanOutcome.Verified => 0,
        DefenderScanOutcome.Blocked => 2,
        _ => 3
    };

    private static int Usage()
    {
        Console.Error.WriteLine("Usage: defender-scan Quick|Full [--execute] [--report] | defender-scan-recover [--report]");
        return 2;
    }
}
