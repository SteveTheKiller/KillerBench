using KillerBench.Engine;

internal static class ServiceEventChecks
{
    internal static void Run()
    {
        foreach (Func<ToolResult, CancellationToken, WorkflowCheck> interpret in new Func<ToolResult, CancellationToken, WorkflowCheck>[] {
            ServiceEvents.Interpret, PrintServiceEvents.Interpret, TimeServiceEvents.Interpret,
            StorageEvents.Interpret, InstallerEvents.Interpret, ApplicationEvents.Interpret, ShutdownEvents.Interpret })
        {
            var failedCollection = new ToolResult(0, "<Events/>", "Fixture access failure", false);
            var rejected = interpret(failedCollection, default);
            Require(rejected.State == CheckState.Unknown && rejected.Repair is null && ReferenceEquals(rejected.Evidence, failedCollection),
                "All event-history collectors retain errors without claiming complete empty history");
        }
        const string entry = "<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='Service Control Manager'/><EventID>7000</EventID><EventRecordID>1</EventRecordID><Level>2</Level><TimeCreated SystemTime='2026-09-12T12:00:00Z'/></System></Event>";
        foreach (var (provider, id, level) in new[] { ("Microsoft-Windows-Kernel-Power", 41, "1"), ("EventLog", 6008, "2") })
        {
            string shutdown = entry.Replace("Service Control Manager", provider).Replace("7000", id.ToString()).Replace("<Level>2", "<Level>" + level);
            var report = EventEvidence.Analyze(EventEvidence.FromXml("System", "<Events>" + string.Concat(Enumerable.Repeat(shutdown, 10)) + "</Events>").Evidence!);
            var finding = report.Findings.Single(f => f.RuleId == "KB-SHUTDOWN-" + id);
            var history = ShutdownEvents.Interpret(new(0, "<Events>" + shutdown + "</Events>", "", false));
            Require(history.State == CheckState.Information && history.Repair is null && history.Detail.Contains(finding.NextStep),
                "Live shutdown history shares guidance and does not authorize repair");
            Require(ShutdownEvents.Interpret(new(0, "<Events>" + entry + "</Events>", "", false)).State == CheckState.Unknown,
                "Unexpected shutdown-history provider remains unknown");
            Require(finding.Occurrences == 10 && finding.Evidence.Count == 8 && finding.NextStep.Contains("Do not select a repair"),
                "Shutdown findings retain bounded references without inferring a repair or unique incident count");
            foreach (var negative in new[] { shutdown.Replace(provider, "Other"), shutdown.Replace("<Level>" + level, "<Level>4") })
                Require(!EventEvidence.Analyze(EventEvidence.FromXml("System", "<Events>" + negative + "</Events>").Evidence!).Findings.Any(f => f.RuleId.StartsWith("KB-SHUTDOWN-")),
                    "Shutdown findings require exact provider and severity");
        }
        foreach (var (provider, id, key) in new[] { ("Ntfs", 55, "filesystem"), ("Microsoft-Windows-Ntfs", 98, "filesystem"),
            ("storahci", 129, "reset"), ("stornvme", 129, "reset"), ("disk", 153, "retry"), ("disk", 157, "removed") })
        {
            string storageEntry = entry.Replace("Service Control Manager", provider).Replace("7000", id.ToString());
            var storageEvidence = new ToolResult(0, "<Events>" + string.Concat(Enumerable.Repeat(storageEntry, 10)) + "</Events>", "", false);
            var archive = EventEvidence.FromXml("System", storageEvidence.Output);
            var storageReport = EventEvidence.Analyze(archive.Evidence!);
            var finding = storageReport.Findings.Single(f => f.RuleId == "KB-STORAGE-" + key);
            Require(finding.Occurrences == 10 && finding.Evidence.Count == 8 && finding.Evidence.All(e => e.LineNumber is >= 1 and <= 10),
                "Storage patterns group occurrences and retain bounded source references");
            var storageCheck = StorageEvents.Interpret(storageEvidence);
            Require(storageCheck.Repair is null && storageCheck.State == CheckState.Information && storageCheck.Detail.Contains(finding.NextStep),
                "Storage workflow exposes pattern guidance without selecting historical repairs");
            foreach (string rejected in new[] { storageEntry.Replace(provider, "UnrelatedProvider"), storageEntry.Replace("<Level>2", "<Level>4") })
                Require(!EventEvidence.Analyze(EventEvidence.FromXml("System", "<Events>" + rejected + "</Events>").Evidence!).Findings.Any(f => f.RuleId.StartsWith("KB-STORAGE-")),
                    "Storage pattern requires matching provider and warning/error severity");
            Require(StorageEvents.Interpret(storageEvidence with { Error = "Fixture collection failure" }).State == CheckState.Unknown,
                "Storage stdout cannot hide a collector error");
        }
        var source = new ToolResult(0, "<Events>" + entry + "</Events>", "", false);
        var check = ServiceEvents.Interpret(source);
        var printSource = source with { Output = source.Output.Replace("Service Control Manager", "Microsoft-Windows-PrintService") };
        var printCheck = PrintServiceEvents.Interpret(printSource);
        string printEntry = entry.Replace("Service Control Manager", "Microsoft-Windows-PrintService");
        var importedPrint = EventEvidence.FromXml("fixture-print", "<Events>" + printEntry + printEntry + "</Events>").Evidence!;
        var printFinding = EventEvidence.Analyze(importedPrint).Findings.Single(finding => finding.RuleId.StartsWith("KB-PRINT-EVENT-"));
        Require(printFinding.RuleId == "KB-PRINT-EVENT-7000-2" && printFinding.NextStep.Contains("Preserve queued jobs"),
            "Imported print events create scoped findings with non-destructive review guidance");
        var repeatedEvidence = EventEvidence.FromXml("fixture-print", "<Events>" + string.Concat(Enumerable.Repeat(printEntry, 12)) +
            printEntry.Replace("<Level>2</Level>", "<Level>3</Level>") + "</Events>").Evidence!;
        var groupedPrint = EventEvidence.Analyze(repeatedEvidence).Findings.Where(finding => finding.RuleId.StartsWith("KB-PRINT-EVENT-")).ToArray();
        Require(groupedPrint.Length == 2 && groupedPrint[0].Occurrences == 12 && groupedPrint[1].Occurrences == 1 &&
            groupedPrint[0].Evidence.Count == 8 && groupedPrint[1].Evidence.Single().LineNumber == 13 &&
            groupedPrint[0].Evidence.Select(reference => reference.LineNumber).SequenceEqual(Enumerable.Range(1, 8)) &&
            groupedPrint.SelectMany(finding => finding.Evidence).All(reference => reference.Source == repeatedEvidence.Source),
            "Print findings retain full counts, bounded original-line references and separate severities");
        var informationalPrint = EventEvidence.FromXml("fixture-print", "<Events>" + printEntry.Replace("<Level>2</Level>", "<Level>4</Level>") + "</Events>").Evidence!;
        Require(!EventEvidence.Analyze(informationalPrint).Findings.Any(finding => finding.RuleId.StartsWith("KB-PRINT-EVENT-")),
            "Informational printing events do not become warning findings");
        var repeatedPrint = PrintServiceEvents.Interpret(source with { Output = "<Events>" + printEntry + printEntry + "</Events>" });
        Require(repeatedPrint.Detail.Contains("2 records.") && repeatedPrint.Detail.Contains("do not count unique failed print jobs") &&
            repeatedPrint.Repair is null, "Print pattern counts do not imply unique incidents or repair authority");
        string Pattern(int id, string level = "2") => printEntry.Replace("<EventID>7000</EventID>", $"<EventID>{id}</EventID>")
            .Replace("<Level>2</Level>", $"<Level>{level}</Level>");
        var manyPatterns = source with { Output = "<Events>" + string.Concat(Enumerable.Range(1, 10).Select(id => Pattern(id))) +
            Pattern(10) + Pattern(10) + Pattern(99, "4") + "</Events>" };
        var boundedPatterns = PrintServiceEvents.Interpret(manyPatterns);
        var patternLines = boundedPatterns.Detail.Split('\n').Where(line => line.StartsWith("Event ") && line.Contains(", level ")).ToArray();
        Require(patternLines.Length == 8 && patternLines[0] == "Event 10, level 2: 3 records." &&
            patternLines[1] == "Event 1, level 2: 1 record." &&
            boundedPatterns.Detail.Contains("2 additional event/level patterns") &&
            boundedPatterns.Detail.Contains("12 warning, error or critical records") && boundedPatterns.Evidence == manyPatterns,
            "Print pattern summary retains frequency ordering, bounded details and full raw evidence");
        var severities = PrintServiceEvents.Interpret(source with { Output = "<Events>" + Pattern(10, "2") + Pattern(10, "3") + "</Events>" });
        Require(severities.Detail.Contains("Event 10, level 2: 1 record.") && severities.Detail.Contains("Event 10, level 3: 1 record."),
            "Print pattern grouping preserves distinct severities");
        Require(printCheck.State == CheckState.Information && printCheck.Repair is null && printCheck.Evidence == printSource &&
            printCheck.Detail.Contains("1 warning, error or critical records"), "Print history retains failures without authorizing repairs");
        Require(PrintServiceEvents.Interpret(printSource with { Output = "<Events/>" }).State == CheckState.Information,
            "Empty print history does not establish printer health");
        foreach (var invalidPrint in new[] { source, printSource with { TimedOut = true }, printSource with { Truncated = true },
            printSource with { ExitCode = 5 }, printSource with { Output = "<Events>" },
            printSource with { Output = "<Events>" + string.Concat(Enumerable.Repeat(entry.Replace("Service Control Manager", "Microsoft-Windows-PrintService"), 201)) + "</Events>" } })
            Require(PrintServiceEvents.Interpret(invalidPrint) is { State: CheckState.Unknown, Repair: null }, "Invalid print history remains unverified");
        try { PrintServiceEvents.Interpret(printSource, new CancellationToken(true)); throw new Exception("Print cancellation ignored"); }
        catch (OperationCanceledException) { }
        foreach (string payloadXml in new[] {
            "<EventData><Data Name='PrinterName'>Office &amp; lab</Data><Data Name='ErrorCode'>1722</Data></EventData>",
            "<UserData><Failure xmlns='urn:print-fixture'><PrinterName>Office &amp; lab</PrinterName><ErrorCode>1722</ErrorCode></Failure></UserData>" })
        {
            var payloadEvidence = printSource with { Output = printSource.Output.Replace("</Event>", payloadXml + "</Event>") };
            var payloadCheck = PrintServiceEvents.Interpret(payloadEvidence);
            Require(payloadCheck.Detail.Contains("PrinterName=Office & lab") && payloadCheck.Detail.Contains("ErrorCode=1722") &&
                payloadCheck.Evidence == payloadEvidence && payloadCheck.Repair is null,
                "Print EventData and namespaced UserData retain reported fields without selecting repairs");
        }
        var longPrint = printSource with { Output = printSource.Output.Replace("</Event>",
            "<EventData>" + string.Concat(Enumerable.Range(1, 7).Select(i => "<Data Name='field" + i + "'>" + new string('x', 201) + "</Data>")) + "</EventData></Event>") };
        var preview = PrintServiceEvents.Interpret(longPrint);
        Require(preview.Detail.Contains("[shortened]") && preview.Detail.Contains("field6=") && !preview.Detail.Contains("field7=") &&
            preview.Detail.Contains("Additional fields retained") && preview.Evidence == longPrint,
            "Print payload previews bound field lengths and count while preserving raw evidence");
        foreach (string id in new[] { "7000", "7001", "7003", "7009", "7011", "7023", "7024", "7031", "7034" })
        {
            string xml = source.Output.Replace("7000", id);
            var evidence = EventEvidence.FromXml("System", xml).Evidence!;
            var findings = EventEvidence.Analyze(evidence).Findings;
            Require(findings.Count == 1 && findings[0].RuleId == "KB-SCM-" + id, "Service failure category is identified from validated provider and event ID");
            Require(EventEvidence.Analyze(EventEvidence.FromXml("System", xml.Replace("Service Control Manager", "Other")).Evidence!).Findings.Count == 0, "Other providers do not acquire service diagnoses");
            Require(EventEvidence.Analyze(EventEvidence.FromXml("System", xml.Replace("<Level>2", "<Level>4")).Evidence!).Findings.Count == 0, "Informational records do not acquire failure diagnoses");
        }
        var serviceCategories = new Dictionary<string, (string Title, string Guidance)>
        {
            ["7009"] = ("connection timeouts", "global service timeout"),
            ["7011"] = ("control response timeouts", "current responsiveness"),
            ["7023"] = ("terminated with errors", "reported error code"),
            ["7024"] = ("service-specific errors", "service-specific code"),
            ["7031"] = ("recovery actions", "configured recovery action"),
            ["7034"] = ("Unexpected service terminations", "nearby process or application failures")
        };
        foreach (var category in serviceCategories)
        {
            var finding = EventEvidence.Analyze(EventEvidence.FromXml("System", source.Output.Replace("7000", category.Key)).Evidence!).Findings.Single();
            Require(finding.Title.Contains(category.Value.Title, StringComparison.OrdinalIgnoreCase) && finding.Certainty == "Recorded event" &&
                finding.NextStep.Contains(category.Value.Guidance, StringComparison.OrdinalIgnoreCase),
                "Expanded service event categories retain specific review guidance without authorizing recovery");
        }
        Require(check.State == CheckState.Information && check.Repair is null && check.Evidence == source && check.Detail.Contains("Event 7000, record 1"), "Historical service event retains evidence without enabling repair");
        Require(ServiceEvents.Interpret(source with { Output = "<Events/>" }).State == CheckState.Information, "Empty event history is not a health verdict");
        var payload = source with { Output = source.Output.Replace("</Event>", "<EventData><Data Name='param1'>Fixture service</Data><Data Name='param2'>Access &amp; dependency details</Data><Data>" + new string('x', 250) + "</Data><Data>extra</Data></EventData></Event>") };
        payload = payload with { Output = payload.Output.Replace("</EventData>", "<Data>fifth</Data><Data>sixth</Data><Data>seventh</Data></EventData>") };
        var summarized = ServiceEvents.Interpret(payload);
        Require(summarized.Detail.Contains("param1=Fixture service") && summarized.Detail.Contains("Access & dependency details") &&
            summarized.Detail.Contains("[shortened]") && summarized.Detail.Contains("Additional fields retained") && summarized.Evidence == payload,
            "Service payload summaries are bounded and retain full raw evidence");
        var userPayload = source with { Output = source.Output.Replace("</Event>", "<UserData><Failure xmlns='urn:fixture'><Service>Example</Service><Code>5</Code></Failure></UserData></Event>") };
        var userSummary = ServiceEvents.Interpret(userPayload);
        Require(userSummary.Detail.Contains("Service=Example") && userSummary.Detail.Contains("Code=5") && userSummary.Repair is null && userSummary.Evidence == userPayload,
            "Service UserData is readable without selecting recovery from historical parameters");
        foreach (var invalid in new[] { source with { TimedOut = true }, source with { Truncated = true }, source with { ExitCode = 5 }, source with { Output = "<Events>" }, source with { Output = source.Output.Replace("Service Control Manager", "Other") }, source with { Output = source.Output.Replace("<Level>2", "<Level>4") }, source with { Output = "<Events>" + string.Concat(Enumerable.Repeat(entry, 201)) + "</Events>" } })
            Require(ServiceEvents.Interpret(invalid).State == CheckState.Unknown, "Incomplete or unexpected service history cannot establish coverage");
        var repeated = Enumerable.Range(1, 201).Select(i => entry.Replace("<EventRecordID>1", "<EventRecordID>" + i)).ToArray();
        var capped = EventEvidence.Analyze(new TextEvidence("fixture", repeated, false));
        Require(capped.Partial && capped.LinesAnalyzed == 200 && capped.ParsedLines == 200,
            "Direct event analysis reports only the bounded rows actually inspected");
        var grouped = capped.Findings.Single();
        Require(grouped.Occurrences == 200 && grouped.Evidence.Count == 8 &&
            grouped.Evidence.All(reference => reference.LineNumber is >= 1 and <= 200),
            "Repeated service events preserve bounded references and the retained occurrence count");
        var damaged = EventEvidence.Analyze(new TextEvidence("fixture", [entry, entry.Replace("2026-09-12T12:00:00Z", "invalid")], false));
        Require(damaged.Partial && damaged.Findings.Single().Occurrences == 1,
            "Malformed metadata cannot add a service occurrence");
        Console.WriteLine("PASS: service event evidence, grouping, reference bounds, partial analysis, wrong provider and malformed coverage.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
}
