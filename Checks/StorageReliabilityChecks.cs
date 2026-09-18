using KillerBench.Engine;

internal static class StorageReliabilityChecks
{
    internal static void Run()
    {
        const string record = "{\"DeviceId\":\"0\",\"Temperature\":30,\"TemperatureMax\":70,\"Wear\":10,\"ReadErrorsUncorrected\":0,\"WriteErrorsUncorrected\":0}";
        ToolResult Evidence(string value) => new(0, "{\"counters\":[" + value + "]}", "", false);
        var original = Evidence(record);
        var denied = original with { ExitCode = 1, Error = "MI RESULT 2,Microsoft.Management.Infrastructure.CimCmdlets.GetCimInstanceCommand" };
        var deniedCheck = StorageReliability.Interpret(denied).Single();
        Require(deniedCheck.State == CheckState.Unknown && deniedCheck.Repair is null && deniedCheck.Evidence == denied &&
            deniedCheck.Detail.Contains("administrator diagnostic session"), "CIM access denial retains error evidence and actionable scope");
        Require(StorageReliability.Interpret(original).Single() is { State: CheckState.Information, Repair: null }, "Counters alone do not establish disk health");
        foreach (string changed in new[] { record.Replace(":30", ":71"), record.Replace(":10", ":100"),
            record.Replace("\"ReadErrorsUncorrected\":0", "\"ReadErrorsUncorrected\":1"), record.Replace("\"WriteErrorsUncorrected\":0", "\"WriteErrorsUncorrected\":1") })
        {
            var evidence = Evidence(changed);
            var check = StorageReliability.Interpret(evidence).Single();
            Require(check.State == CheckState.Attention && check.Repair is null && check.Evidence == evidence,
                "Temperature, wear and I/O findings retain evidence without selecting repair");
        }
        foreach (string value in new[] { record.Replace(":30", ":70"), record.Replace(":10", ":99") })
            Require(StorageReliability.Interpret(Evidence(value)).Single().State == CheckState.Information, "Threshold boundaries are exact");
        string unavailable = "{\"DeviceId\":\"0\",\"Temperature\":null,\"TemperatureMax\":null,\"Wear\":null,\"ReadErrorsUncorrected\":null,\"WriteErrorsUncorrected\":null}";
        Require(StorageReliability.Interpret(Evidence(unavailable)).Single().State == CheckState.Unknown, "Missing counters are not zero");
        foreach (var malformed in new[] { Evidence(""), Evidence(record + "," + record), Evidence(record.Replace(":30", ":-1")),
            Evidence(record.Replace(":10", ":256")), Evidence(record.Replace("\"Wear\":10", "\"Other\":10")),
            original with { Truncated = true }, original with { TimedOut = true }, original with { Error = "provider failed" } })
            Require(StorageReliability.Interpret(malformed).All(check => check.State == CheckState.Unknown && check.Repair is null), "Incomplete counters remain unknown");
        var capped = Evidence(string.Join(",", Enumerable.Range(0, 65).Select(index => record.Replace("\"0\"", $"\"{index}\""))));
        Require(StorageReliability.Interpret(capped) is { Count: 65 } checks && checks[^1].State == CheckState.Unknown, "Device bound reports omitted coverage");
        Console.WriteLine("PASS: storage reliability thresholds, unavailable counters, identity and bounded coverage; fixtures only.");
    }
    private static void Require(bool value, string detail) { if (!value) throw new Exception(detail); }
}
