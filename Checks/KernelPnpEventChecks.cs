using KillerBench.Engine;

internal static class KernelPnpEventChecks
{
    internal static void Run()
    {
        string Entry(int id, int level = 2, string provider = "Microsoft-Windows-Kernel-PnP", ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><EventData><Data Name='DeviceInstanceId'>PCI\\VEN_1234&amp;DEV_5678</Data><Data Name='DriverName'>private.inf</Data></EventData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(411, record: 1) + Entry(411, record: 2) + Entry(410, level: 4, record: 3) + "</Events>", "", false);
        var result = KernelPnpEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && result.Evidence == evidence &&
            result.Detail.Contains("3 records retained; 2 warning") && result.Detail.Contains("Event 411, level 2: 2 records.") &&
            result.Detail.Contains("device instance IDs") && !result.Detail.Contains("VEN_1234") && !result.Detail.Contains("private.inf") &&
            result.Detail.Contains("No device, driver, package, configuration or repair is selected"),
            "Plug and Play history groups errors without copying device or driver identities into the summary");
        var empty = KernelPnpEvents.Interpret(evidence with { Output = "<Events/>" });
        Require(empty.State == CheckState.Information && empty.Repair is null && empty.Detail.Contains("0 records retained"),
            "Empty complete Plug and Play history remains informational");
        foreach (var bad in new[]
        {
            evidence with { Output = "<Events>" }, evidence with { ExitCode = 5 }, evidence with { Error = "Access denied" },
            evidence with { TimedOut = true }, evidence with { Truncated = true },
            evidence with { Output = "<Events>" + Entry(411, provider: "Other") + "</Events>" },
            evidence with { Output = "<Events>" + Entry(411, level: 9) + "</Events>" },
            evidence with { Output = "<Events>" + Entry(411) + Entry(411) + "</Events>" },
            evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(411, record: (ulong)index))) + "</Events>" }
        })
        {
            var unknown = KernelPnpEvents.Interpret(bad);
            Require(unknown.State == CheckState.Unknown && unknown.Repair is null && unknown.Evidence == bad,
                "Incomplete Plug and Play history cannot establish device state or repair eligibility");
        }
        var many = KernelPnpEvents.Interpret(evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 10).Select(id => Entry(id, record: (ulong)id))) + "</Events>" });
        Require(many.Detail.Split('\n').Count(line => line.StartsWith("Event ")) == 8 && many.Detail.Contains("2 additional event/level patterns"),
            "Plug and Play summaries retain bounded pattern output");
        try { KernelPnpEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("Plug and Play history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: Plug and Play event grouping, identity privacy, incomplete evidence and cancellation; no device changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
