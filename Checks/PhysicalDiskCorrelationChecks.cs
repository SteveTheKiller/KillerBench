using KillerBench.Engine;

internal static class PhysicalDiskCorrelationChecks
{
    internal static void Run()
    {
        WorkflowCheck Disk(string id, CheckState state = CheckState.Information) => new("Physical disk: " + id, state, "fixture");
        WorkflowCheck Counter(string id, CheckState state = CheckState.Information) => new("Storage reliability: " + id, state, "fixture");
        var complete = PhysicalDiskCorrelation.Check([Disk("0"), Disk("1")], [Counter("0")]);
        Require(complete.State == CheckState.Information && complete.Repair is null &&
            complete.Detail.Contains("1 disk has no provider reliability counters") &&
            complete.Detail.Contains("unavailable counters are not treated as zero"),
            "Partial counter availability remains honest information when exact identities agree");
        var attention = PhysicalDiskCorrelation.Check([Disk("0"), Disk("1", CheckState.Attention)],
            [Counter("0", CheckState.Attention)]);
        Require(attention.State == CheckState.Attention && attention.Detail.Contains("disk IDs: 0, 1") && attention.Repair is null,
            "Current inventory and counter findings combine by exact provider identity");
        var incomplete = PhysicalDiskCorrelation.Check([Disk("0", CheckState.Unknown)], [Counter("0", CheckState.Attention)]);
        Require(incomplete.State == CheckState.Unknown && incomplete.Detail.Contains("1 source result is incomplete") && incomplete.Repair is null,
            "Incomplete evidence takes precedence over attention");
        foreach (var invalid in new[]
        {
            PhysicalDiskCorrelation.Check([], [Counter("0")]),
            PhysicalDiskCorrelation.Check([new("Physical disk inventory", CheckState.Unknown, "fixture")], [Counter("0")]),
            PhysicalDiskCorrelation.Check([Disk("0"), Disk("0")], [Counter("0")]),
            PhysicalDiskCorrelation.Check([Disk("0")], [new("Storage reliability", CheckState.Unknown, "fixture")]),
            PhysicalDiskCorrelation.Check([Disk("0")], [Counter("1")]),
            PhysicalDiskCorrelation.Check([Disk("0")], [Counter("0"), Counter("0")])
        })
            Require(invalid.State == CheckState.Unknown && invalid.Repair is null,
                "Missing, duplicate, unmatched or generic evidence fails closed");
        try
        {
            PhysicalDiskCorrelation.Check([Disk("0")], [Counter("0")], new CancellationToken(true));
            throw new Exception("Physical-disk correlation ignored cancellation");
        }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: exact physical-disk and reliability identity correlation, precedence and missing counters; no repair selected.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
