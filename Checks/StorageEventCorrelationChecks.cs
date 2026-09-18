using KillerBench.Engine;

internal static class StorageEventCorrelationChecks
{
    public static void Run()
    {
        const string topologyJson = "{\"complete\":true,\"disks\":[],\"partitions\":[{\"disk\":1,\"number\":2,\"letter\":\"C\"},{\"disk\":0,\"number\":1,\"letter\":\"D\"}],\"volumes\":[]}";
        const string eventTemplate = "<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='Microsoft-Windows-Ntfs'/><EventID>98</EventID><Level>4</Level><TimeCreated SystemTime='2026-09-14T12:00:00Z'/><EventRecordID>1</EventRecordID></System><EventData><Data Name='DriveName'>{0}</Data></EventData></Event>";
        WorkflowCheck Topology(string json = topologyJson, CheckState state = CheckState.Information) =>
            new("Storage topology", state, "fixture", Evidence: new(0, json, "", false));
        WorkflowCheck History(string xml, CheckState state = CheckState.Information) =>
            new("Recent storage events", state, "fixture", Evidence: new(0, xml, "", false));
        string Event(string target, int record) => string.Format(eventTemplate, target).Replace("<EventRecordID>1", "<EventRecordID>" + record);

        var mapped = StorageEventCorrelation.Check(Topology(), History("<Events>" + Event("C:", 1) + Event("C:\\", 2) + Event("D:", 3) + "</Events>"));
        Require(mapped.State == CheckState.Information && mapped.Repair is null &&
            mapped.Detail.Contains("C: to disk 1, partition 2 (2 records)", StringComparison.Ordinal) &&
            mapped.Detail.Contains("D: to disk 0, partition 1 (1 record)", StringComparison.Ordinal),
            "Drive-letter event identities map to exact reported disk and partition records");
        var unresolved = StorageEventCorrelation.Check(Topology(), History("<Events>" + Event("\\Device\\HarddiskVolume2", 1) + "</Events>"));
        Require(unresolved.State == CheckState.Information && unresolved.Detail.Contains("not a drive letter", StringComparison.Ordinal) && unresolved.Repair is null,
            "Device-only event identity remains explicit and non-mutating");
        var missing = StorageEventCorrelation.Check(Topology(), History("<Events>" + Event("E:", 1) + "</Events>"));
        Require(missing.State == CheckState.Unknown && missing.Detail.Contains("Unmapped drive letters: E:", StringComparison.Ordinal) && missing.Repair is null,
            "A changed or absent drive-letter identity cannot be mapped by assumption");
        foreach (var invalid in new[]
        {
            StorageEventCorrelation.Check(Topology(state: CheckState.Unknown), History("<Events/>")),
            StorageEventCorrelation.Check(Topology(), History("<Events/>", CheckState.Unknown)),
            StorageEventCorrelation.Check(Topology("{}"), History("<Events/>")),
            StorageEventCorrelation.Check(Topology(topologyJson.Replace("\"D\"", "\"C\"")), History("<Events/>")),
            StorageEventCorrelation.Check(Topology(), History("broken"))
        }) Require(invalid.State == CheckState.Unknown && invalid.Repair is null,
            "Incomplete, malformed and ambiguous inputs cannot produce storage event correlation");
        try { StorageEventCorrelation.Check(Topology(), History("<Events/>"), new CancellationToken(true)); throw new Exception("Correlation cancellation ignored"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: storage event drive-letter correlation, identity changes and incomplete evidence; no storage changes.");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
