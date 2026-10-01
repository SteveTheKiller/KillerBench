using KillerBench.Engine;

internal static class PhysicalDiskChecks
{
    internal static void Run()
    {
        const string healthy = "{\"deviceId\":\"0\",\"name\":\"Fixture SSD\",\"health\":\"Healthy\",\"operational\":\"OK\",\"media\":\"SSD\",\"bus\":\"NVMe\",\"size\":107374182400,\"usage\":\"Auto-Select\",\"readOnly\":false}";
        static ToolResult Evidence(string records) => new(0, "{\"physicalDisks\":[" + records + "]}", "", false);
        var clear = PhysicalDiskDiagnostics.Interpret(Evidence(healthy)).Single();
        Require(clear.State == CheckState.Information && clear.Repair is null &&
            clear.Detail.Contains("100.0 GiB") && clear.Detail.Contains("Serial numbers") &&
            !clear.Detail.Contains("uniqueId", StringComparison.OrdinalIgnoreCase), "Healthy disk is bounded information");

        foreach (string changed in new[]
        {
            healthy.Replace("\"Healthy\"", "\"Warning\""),
            healthy.Replace("\"Healthy\"", "\"Unhealthy\""),
            healthy.Replace("\"OK\"", "\"Predictive Failure\""),
            healthy.Replace("\"OK\"", "\"Lost Communication\""),
            healthy.Replace("\"OK\"", "\"Degraded\"")
        })
        {
            var check = PhysicalDiskDiagnostics.Interpret(Evidence(changed)).Single();
            Require(check.State == CheckState.Attention && check.Repair is null,
                "Fault-class provider state requests review without repair");
        }

        foreach (string changed in new[]
        {
            healthy.Replace("\"Healthy\"", "\"Unknown\""),
            healthy.Replace("\"OK\"", "\"Starting\""),
            healthy.Replace("\"size\":107374182400", "\"size\":0"),
            healthy.Replace("\"name\":\"Fixture SSD\"", "\"name\":\"bad\\nname\""),
            healthy.Replace("\"readOnly\":false", "\"readOnly\":null")
        })
            Require(PhysicalDiskDiagnostics.Interpret(Evidence(changed)).Single().State == CheckState.Unknown,
                "Unrecognized or malformed disk evidence remains unknown");

        Require(PhysicalDiskDiagnostics.Interpret(Evidence(healthy + "," + healthy)).Single().State == CheckState.Unknown,
            "Duplicate physical-disk identity fails closed");
        Require(PhysicalDiskDiagnostics.Interpret(Evidence("")).Single().State == CheckState.Unknown,
            "Empty physical-disk inventory remains unknown");
        var capped = Evidence(string.Join(",", Enumerable.Range(0, 65)
            .Select(index => healthy.Replace("\"0\"", "\"" + index + "\""))));
        Require(PhysicalDiskDiagnostics.Interpret(capped) is { Count: 65 } checks && checks[^1].State == CheckState.Unknown,
            "Physical-disk inventory reports its 64-record bound");
        foreach (var failed in new[]
        {
            Evidence(healthy) with { ExitCode = 1, Error = "provider failed" },
            Evidence(healthy) with { TimedOut = true },
            Evidence(healthy) with { Truncated = true }
        })
            Require(PhysicalDiskDiagnostics.Interpret(failed).Single().State == CheckState.Unknown,
                "Incomplete physical-disk collection remains unknown");
        Console.WriteLine("PASS: physical-disk identity, health, operational state, privacy and bounds.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
