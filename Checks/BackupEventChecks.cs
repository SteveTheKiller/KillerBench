using KillerBench.Engine;

internal static class BackupEventChecks
{
    internal static void Run()
    {
        foreach (var source in Enum.GetValues<BackupEventSource>())
        {
            string provider = source == BackupEventSource.Vss ? "VSS" : "volsnap";
            string entry = "<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='" + provider + "'/><EventID>8193</EventID><EventRecordID>1</EventRecordID><Level>2</Level><TimeCreated SystemTime='2026-09-12T12:00:00Z'/></System><EventData><Data Name='Fixture'>Retained backup payload</Data></EventData></Event>";
            var raw = new ToolResult(0, "<Events>" + entry + "</Events>", "", false);
            var check = BackupEvents.Interpret(source, raw);
            if (check.State != CheckState.Information || check.Repair is not null || check.Evidence != raw || !check.Detail.Contains("Retained backup payload"))
                throw new Exception("Backup history lost evidence or authorized repair.");
            foreach (var invalid in new[] { raw with { TimedOut = true }, raw with { Truncated = true }, raw with { Error = "Denied" },
                raw with { Output = "<broken" }, raw with { Output = raw.Output.Replace(provider, "Other") },
                raw with { Output = raw.Output.Replace("<Level>2", "<Level>4") },
                raw with { Output = "<Events>" + string.Concat(Enumerable.Repeat(entry, 201)) + "</Events>" } })
                if (BackupEvents.Interpret(source, invalid).State != CheckState.Unknown) throw new Exception("Invalid backup event coverage was accepted.");
            var empty = BackupEvents.Interpret(source, raw with { Output = "<Events/>" });
            if (empty.State != CheckState.Information || !empty.Detail.Contains("0 retained")) throw new Exception("Empty backup history claimed health.");
            try { BackupEvents.Interpret(source, raw, new CancellationToken(true)); throw new Exception("Backup history ignored cancellation."); }
            catch (OperationCanceledException) { }
        }
        Console.WriteLine("PASS: backup event provider/severity checks, payload retention, empty/partial/malformed coverage and cancellation.");
    }
}
