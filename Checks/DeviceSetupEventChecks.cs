using KillerBench.Engine;

internal static class DeviceSetupEventChecks
{
    internal static void Run()
    {
        string Entry(int id, int level = 2, string provider = "Microsoft-Windows-DeviceSetupManager", ulong record = 1) =>
            $"<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='{provider}'/><EventID>{id}</EventID><EventRecordID>{record}</EventRecordID><Level>{level}</Level><TimeCreated SystemTime='2026-09-14T00:00:00Z'/></System><EventData><Data Name='Prop_ContainerId'>{{11111111-2222-3333-4444-555555555555}}</Data></EventData></Event>";
        var evidence = new ToolResult(0, "<Events>" + Entry(131, record: 1) + Entry(131, record: 2) + Entry(300, level: 4, record: 3) + "</Events>", "", false);
        var result = DeviceSetupEvents.Interpret(evidence);
        Require(result.State == CheckState.Attention && result.Repair is null && result.Evidence == evidence &&
            result.Detail.Contains("3 records retained; 2 warning") && result.Detail.Contains("Event 131, level 2: 2 records.") &&
            result.Detail.Contains("device container identities") && !result.Detail.Contains("11111111-2222") &&
            result.Detail.Contains("No device, driver, package, rescan or repair is selected"),
            "Device setup history retains grouped errors without copying device identities into the summary");
        var empty = DeviceSetupEvents.Interpret(evidence with { Output = "<Events/>" });
        Require(empty.State == CheckState.Information && empty.Repair is null && empty.Detail.Contains("0 records retained"),
            "Empty complete device setup history remains informational rather than a health verdict");
        foreach (var bad in new[]
        {
            evidence with { Output = "<Events>" }, evidence with { ExitCode = 5 }, evidence with { Error = "Access denied" },
            evidence with { TimedOut = true }, evidence with { Truncated = true },
            evidence with { Output = "<Events>" + Entry(131, provider: "Other") + "</Events>" },
            evidence with { Output = "<Events>" + Entry(131, level: 9) + "</Events>" },
            evidence with { Output = "<Events>" + Entry(131) + Entry(131) + "</Events>" },
            evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 201).Select(index => Entry(131, record: (ulong)index))) + "</Events>" }
        })
        {
            var unknown = DeviceSetupEvents.Interpret(bad);
            Require(unknown.State == CheckState.Unknown && unknown.Repair is null && unknown.Evidence == bad,
                "Incomplete device setup history cannot establish device state or repair eligibility");
        }
        var many = DeviceSetupEvents.Interpret(evidence with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 10).Select(id => Entry(id, record: (ulong)id))) + "</Events>" });
        Require(many.Detail.Split('\n').Count(line => line.StartsWith("Event ")) == 8 && many.Detail.Contains("2 additional event/level patterns"),
            "Device setup summaries retain bounded pattern output");
        try { DeviceSetupEvents.Interpret(evidence, new CancellationToken(true)); throw new Exception("Device setup history ignored cancellation"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: device setup event grouping, identity privacy, incomplete evidence and cancellation; no device changes.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
