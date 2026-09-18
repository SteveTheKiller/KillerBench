using System.IO;
using System.Text.Json;
using KillerBench.Engine;

internal static class CrashDumpChecks
{
    internal static void Run(string scratch)
    {
        string crash = "<Events><Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='" + CrashEvents.Provider + "'/><EventID>1001</EventID><Level>2</Level><TimeCreated SystemTime='2026-09-13T12:00:00Z'/><EventRecordID>1</EventRecordID></System><EventData><Data Name='param1'>0x00000109</Data></EventData></Event></Events>";
        var crashFinding = CrashEvents.Interpret(new(0, crash, "", false));
        var bugCheckPack = new KnowledgePack(1, "fixture-bugchecks", 7, new(2026, 9, 14), "Fixture",
            [new("bugcheck", "0x00000109", "CRITICAL_STRUCTURE_CORRUPTION", "Fixture reference",
                "https://example.com/bugcheck-109", new(2026, 9, 14), "Fixture Windows")]);
        var bugCheckCatalog = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(bugCheckPack));
        var sessionCatalogs = new KnowledgeCatalogCollection([bugCheckCatalog]);
        if (!ReferenceEquals(new WindowsWorkflowHost { KnowledgeCatalogs = sessionCatalogs }.KnowledgeCatalogs, sessionCatalogs))
            throw new Exception("Workflow host lost the session knowledge catalogs.");
        var bugCheckReferences = CrashBugCheckReferences.Check(new(0, crash, "", false), bugCheckCatalog);
        if (bugCheckReferences.State != CheckState.Information ||
            !bugCheckReferences.Detail.Contains("0x00000109 | CRITICAL_STRUCTURE_CORRUPTION | 1 report(s)") ||
            !bugCheckReferences.Detail.Contains("fixture-bugchecks revision 7") || bugCheckReferences.Repair is not null)
            throw new Exception("Crash code did not resolve through the exact bug-check context.");
        var unknownReference = CrashBugCheckReferences.Check(new(0, crash.Replace("0x00000109", "0xFE"), "", false), bugCheckCatalog);
        if (unknownReference.State != CheckState.Information || !unknownReference.Detail.Contains("0x000000FE") ||
            !unknownReference.Detail.Contains("No exact reference"))
            throw new Exception("Unknown crash code was hidden or inferred.");
        var conflictingBugChecks = new KnowledgeCatalogCollection([bugCheckCatalog,
            KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(bugCheckPack with { Id = "conflicting-bugchecks" }))]);
        if (CrashBugCheckReferences.Check(new(0, crash, "", false), conflictingBugChecks).State != CheckState.Unknown)
            throw new Exception("Conflicting bug-check packs produced an automatic reference.");
        foreach (string invalidCode in new[] { "", "0x", "0x123456789", "not-a-code" })
            if (CrashBugCheckReferences.Check(new(0, crash.Replace("0x00000109", invalidCode), "", false), bugCheckCatalog).State != CheckState.Unknown)
                throw new Exception("Malformed crash code established reference coverage.");
        string withPath = crash.Replace("</EventData>", "<Data Name='param2'>C:\\Windows\\MEMORY.DMP</Data></EventData>");
        var dumpInventory = new CrashDumpInventoryResult([new("c:\\windows\\memory.dmp", 123, DateTime.UtcNow)], true, []);
        var correlation = CrashDumpCorrelation.Check(new(0, withPath, "", false), dumpInventory);
        if (correlation.State != CheckState.Information || !correlation.Detail.Contains("1 of 1") || correlation.Repair is not null)
            throw new Exception("Exact case-insensitive dump metadata match failed.");
        if (!CrashDumpCorrelation.Check(new(0, withPath, "", false), new([], true, [])).Detail.Contains("availability unverified"))
            throw new Exception("Unmatched dump path claimed absence.");
        if (CrashDumpCorrelation.Check(new(0, withPath, "", false), dumpInventory with { Complete = false }).State != CheckState.Unknown ||
            CrashDumpCorrelation.Check(new(1, withPath, "", false), dumpInventory).State != CheckState.Unknown)
            throw new Exception("Partial dump correlation claimed full coverage.");
        if (!CrashDumpCorrelation.Check(new(0, crash, "", false), dumpInventory).Detail.Contains("0 of 0"))
            throw new Exception("Dump correlation invented a path from an error code.");
        if (crashFinding.State != CheckState.Information || crashFinding.Repair is not null || !crashFinding.Detail.Contains("0x00000109"))
            throw new Exception("Crash event payload or classification missing.");
        foreach (string bad in new[] { crash.Replace(CrashEvents.Provider, "Other"), crash.Replace("1001", "1002"), crash.Replace("<Level>2", "<Level>4"), "<broken" })
            if (CrashEvents.Interpret(new(0, bad, "", false)).State != CheckState.Unknown) throw new Exception("Unexpected crash evidence accepted.");
        foreach (var failure in new[] { new ToolResult(1, "<Events/>", "", false), new ToolResult(0, crash, "", true),
            new ToolResult(0, crash, "Access denied", false), new ToolResult(0, crash, "", false) { TimedOut = true } })
        {
            var unknown = CrashEvents.Interpret(failure);
            if (unknown.State != CheckState.Unknown || unknown.Evidence != failure || unknown.Repair is not null)
                throw new Exception("Failed crash collection lost evidence or claimed coverage.");
        }
        if (CrashEvents.Interpret(new(0, "<Events/>", "", false)).State != CheckState.Information) throw new Exception("Empty crash history rejected.");
        string entry = crash["<Events>".Length..^"</Events>".Length];
        if (CrashEvents.Interpret(new(0, "<Events>" + string.Concat(Enumerable.Repeat(entry, 201)) + "</Events>", "", false)).State != CheckState.Unknown)
            throw new Exception("Crash overflow claimed full coverage.");
        try { CrashEvents.Interpret(new(0, crash, "", false), new CancellationToken(true)); throw new Exception("Crash cancellation ignored."); }
        catch (OperationCanceledException) { }
        var localPath = typeof(CrashDumpInventory).GetMethod("LocalUnlinked", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        string probe = Path.Combine(scratch, "ancestor", "file.dmp");
        int visited = 0;
        Func<string, FileAttributes> clean = _ => { visited++; return FileAttributes.Normal; };
        bool CheckPath(Func<string, FileAttributes> attributes, DriveType drive) =>
            (bool)localPath.Invoke(null, [probe, attributes, (Func<string, DriveType>)(_ => drive), CancellationToken.None])!;
        if (CheckPath(clean, DriveType.Network) || visited != 0) throw new Exception("Mapped drive reached filesystem traversal.");
        if (CheckPath(path => path.EndsWith("ancestor", StringComparison.Ordinal) ? FileAttributes.ReparsePoint : FileAttributes.Normal, DriveType.Fixed))
            throw new Exception("Linked ancestor accepted.");
        if (!CheckPath(clean, DriveType.Fixed)) throw new Exception("Ordinary fixed-drive path rejected.");
        foreach (string invalidName in new[] { "CrashDumpEnabled", "FilterPages", "DumpFile", "MinidumpDir" })
        {
            var settings = CrashDumpConfiguration.ReadValues(name => name == invalidName ? new byte[] { 7 } : name == "CrashDumpEnabled" ? 7 : null);
            if (settings.Error?.Contains(invalidName) != true || CrashDumpConfiguration.Interpret(settings).State != CheckState.Unknown)
                throw new Exception("Invalid registry value type disappeared into a default.");
        }
        var configured = CrashDumpConfiguration.ReadValues(name => name == "CrashDumpEnabled" ? 7 : name == "DumpFile" ? "%SystemRoot%\\custom.dmp" : null);
        if (configured.Error is not null || configured.DumpFile != "%SystemRoot%\\custom.dmp") throw new Exception("Stored path was changed.");
        foreach (int mode in new[] { 0, 1, 2, 3, 7, 99 })
        {
            var check = CrashDumpConfiguration.Interpret(new(mode, null, "%SystemRoot%\\MEMORY.DMP", null));
            if (check.State != (mode == 99 ? CheckState.Unknown : mode == 0 ? CheckState.Attention : CheckState.Information) || check.Repair is not null)
                throw new Exception("Dump configuration classification mismatch.");
        }
        if (!CrashDumpConfiguration.Interpret(new(1, 1, null, null)).Detail.Contains("Active memory") ||
            CrashDumpConfiguration.Interpret(new(1, 9, null, null)).State != CheckState.Unknown ||
            CrashDumpConfiguration.Interpret(new(7, null, "bad\npath", null)).State != CheckState.Unknown)
            throw new Exception("Dump configuration validity mismatch.");
        string root = Path.Combine(scratch, "dump-fixture");
        Directory.CreateDirectory(root);
        string custom = Path.Combine(root, "custom.dmp");
        File.WriteAllText(custom, "fixture");
        var customInventory = CrashDumpInventory.InspectDirectory(root, settings: new(7, null, custom, null));
        if (!customInventory.Complete || customInventory.Files.Single().Path != custom) throw new Exception("Configured dump file not discovered.");
        var rejectedLocation = CrashDumpInventory.InspectDirectory(root, settings: new(7, null, @"\\server\share\dump.dmp", null));
        if (rejectedLocation.Complete || !rejectedLocation.Notes.Any(note => note.Contains("Unsupported"))) throw new Exception("Remote configured path was not rejected.");
        if (!CrashDumpInventory.InspectDirectory(root).Complete) throw new Exception("Absent default dumps treated as failed coverage.");
        File.WriteAllText(Path.Combine(root, "MEMORY.DMP"), "not a real dump");
        foreach (string badPath in new[] { @"C:\bad<name.dmp", @"C:\bad>name.dmp", @"C:\bad|name.dmp", @"C:\bad:name.dmp", "%UNKNOWN%\\dump.dmp", "relative.dmp" })
        {
            var partial = CrashDumpInventory.InspectDirectory(root, settings: new(7, null, badPath, null));
            if (partial.Complete || partial.Files.Count != 1 || partial.Files[0].Path != Path.Combine(root, "MEMORY.DMP") ||
                !partial.Notes.Any(note => note.Contains("Unsupported")))
                throw new Exception("Invalid configured path lost default dump evidence or claimed complete coverage.");
        }
        string mini = Path.Combine(root, "Minidump");
        Directory.CreateDirectory(mini);
        for (int i = 0; i < 257; i++) File.WriteAllText(Path.Combine(mini, i + ".dmp"), "fixture");
        var result = CrashDumpInventory.InspectDirectory(root);
        if (result.Complete || result.Files.Count != 257 || CrashDumpInventory.Interpret(result).State != CheckState.Unknown)
            throw new Exception("Dump bound or coverage mismatch.");
        if (File.ReadAllText(Path.Combine(root, "MEMORY.DMP")) != "not a real dump") throw new Exception("Dump source changed.");
        using (var retained = System.Text.Json.JsonDocument.Parse(CrashDumpInventory.Interpret(result).Evidence!.Output))
        {
            if (retained.RootElement.GetProperty("files").GetArrayLength() != 257 || retained.RootElement.GetProperty("complete").GetBoolean())
                throw new Exception("Workflow evidence lost full metadata or partial coverage.");
        }
        string invalidRoot = Path.Combine(scratch, "dump-nonfile");
        Directory.CreateDirectory(Path.Combine(invalidRoot, "MEMORY.DMP"));
        if (CrashDumpInventory.InspectDirectory(invalidRoot).Complete) throw new Exception("Directory masquerading as dump established coverage.");
        try { CrashDumpInventory.InspectDirectory(root, new CancellationToken(true)); throw new Exception("Cancellation ignored."); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: dump metadata discovery bounds, absent paths, preserved content and cancellation; scratch files only.");
    }
}
