using System.Text.Json;
using KillerBench.Engine;

internal static partial class InvestigationChecks
{
    private static readonly int[] ActionFailureLineOrder = [3, 1, 2];

    [System.Text.RegularExpressions.GeneratedRegex("Detail=line1 +line2")]
    private static partial System.Text.RegularExpressions.Regex DetailLineRegex();

    internal static void Run()
    {
        var providerEvidence = new ToolResult(0, "{\"count\":1,\"version\":\"10.0.26200\",\"build\":\"26200\"}", "", false);
        Check(WmiProviderDiagnostics.Interpret(providerEvidence).State == CheckState.Clear && WmiProviderDiagnostics.Interpret(providerEvidence).Evidence == providerEvidence,
            "WMI provider query retains its scoped evidence");
        foreach (string invalid in new[] { "null", "[]", "{}", providerEvidence.Output.Replace("1,", "0,"), providerEvidence.Output.Replace("26200\"}", "bad\"}"),
            providerEvidence.Output.Replace("\"count\":1", "\"count\":0,\"count\":1"), providerEvidence.Output.Replace("\"10.0.26200\"", "null") })
            Check(WmiProviderDiagnostics.Interpret(providerEvidence with { Output = invalid }).State == CheckState.Unknown,
                "Invalid WMI provider result cannot establish functionality");
        foreach (var incomplete in new[] { providerEvidence with { TimedOut = true }, providerEvidence with { Truncated = true }, providerEvidence with { ExitCode = 5 } })
            Check(WmiProviderDiagnostics.Interpret(incomplete).State == CheckState.Unknown && WmiProviderDiagnostics.Interpret(incomplete).Repair is null,
                "Incomplete provider evidence never authorizes repository repair");
        const string installerEvent = "<Event xmlns='http://schemas.microsoft.com/win/2004/08/events/event'><System><Provider Name='MsiInstaller'/><EventID>11708</EventID><Level>2</Level><TimeCreated SystemTime='2026-09-12T12:00:00Z'/><EventRecordID>42</EventRecordID></System></Event>";
        string timeEvent = installerEvent.Replace("MsiInstaller", "Microsoft-Windows-Time-Service");
        var applicationEvidence = new ToolResult(0, "<Events>" + installerEvent.Replace("MsiInstaller", "Example application").Replace("</Event>", "<EventData><Data Name='Module'>example.dll</Data></EventData></Event>") + "</Events>", "", false);
        var applicationHistory = ApplicationEvents.Interpret(applicationEvidence);
        var failedApplicationCollection = ApplicationEvents.Interpret(applicationEvidence with { Error = "Fixture collection error" });
        Check(failedApplicationCollection.State == CheckState.Unknown && failedApplicationCollection.Repair is null &&
            failedApplicationCollection.Evidence!.Error == "Fixture collection error",
            "Application collector error remains unknown with evidence retained");
        var repeatedApplications = applicationEvidence with { Output = "<Events>" + installerEvent + installerEvent.Replace("<EventRecordID>42", "<EventRecordID>43") + installerEvent.Replace("MsiInstaller", "Other") + "</Events>" };
        string groupedApplications = ApplicationEvents.Interpret(repeatedApplications).Detail;
        Check(groupedApplications.Contains("MsiInstaller | Event 11708 | 2 records") && groupedApplications.Contains("Other | Event 11708 | 1 records"),
            "Application summary groups structured event IDs within each provider without merging unrelated providers");
        Check(applicationHistory.State == CheckState.Information && applicationHistory.Repair is null && applicationHistory.Evidence == applicationEvidence &&
            applicationHistory.Detail.Contains("Module=example.dll"), "Application history retains bounded reported fields without authorizing repairs");
        foreach (var invalidApplication in new[] { applicationEvidence with { TimedOut = true }, applicationEvidence with { ExitCode = 5 },
            applicationEvidence with { Output = "broken" }, applicationEvidence with { Output = applicationEvidence.Output.Replace("<Level>2", "<Level>4") },
            applicationEvidence with { Output = "<Events>" + string.Concat(Enumerable.Repeat(installerEvent, 201)) + "</Events>" } })
            Check(ApplicationEvents.Interpret(invalidApplication).State == CheckState.Unknown, "Application history incomplete or unexpected evidence remains unknown");
        Check(ApplicationEvents.Interpret(applicationEvidence with { Output = "<Events/>" }).State == CheckState.Information &&
            !Workflows.SupportsRepair(WorkflowKind.Applications) && Workflows.Scopes(WorkflowKind.Applications).Length == 0,
            "Application history is informational even when empty and has no repair actions");
        var storageEvidence = new ToolResult(0, "<Events>" + installerEvent.Replace("MsiInstaller", "disk") + "</Events>", "", false);
        var storageHistory = StorageEvents.Interpret(storageEvidence);
        foreach (int diskEvent in new[] { 7, 9, 11, 51 })
        {
            string expected = diskEvent == 51 ? "Buffered disk I/O errors recorded" : "Disk error events recorded";
            var diskEvidence = storageEvidence with { Output = storageEvidence.Output.Replace("11708", diskEvent.ToString()) };
            var identified = StorageEvents.Interpret(diskEvidence);
            Check(identified.Detail.Contains(expected) && identified.Repair is null && identified.Evidence == diskEvidence &&
                identified.State == CheckState.Information, "Disk event guidance preserves historical scope and evidence");
            foreach (string unrelated in new[] { diskEvidence.Output.Replace("Name='disk'", "Name='Ntfs'"),
                diskEvidence.Output.Replace("<Level>2", "<Level>4"),
                diskEvidence.Output.Replace($"<EventID>{diskEvent}<", $"<EventID>{diskEvent + 1000}<") })
                Check(!StorageEvents.Interpret(diskEvidence with { Output = unrelated }).Detail.Contains(expected),
                    "Disk event guidance requires matching provider, severity and exact event ID");
        }
        Check(storageHistory.State == CheckState.Information && storageHistory.Repair is null && storageHistory.Evidence == storageEvidence &&
            storageHistory.Detail.Contains("1 warning, error or critical") && storageHistory.Detail.Contains("record 42"), "Storage history retains exact evidence without selecting repairs");
        Check(StorageEvents.Interpret(storageEvidence with { Output = "<Events/>" }).State == CheckState.Information, "Empty storage history is not a health verdict");
        string storagePayload = "<EventData><Data Name='DeviceName'>\\Device\\Harddisk2\\DR2</Data><Data Name='Status'>0xC0000185</Data>" +
            "<Data Name='Detail'>line1&#xA;line2</Data><Data>unnamed</Data><Data Name='Long'>" + new string('x', 201) +
            "</Data><Data Name='Sixth'>six</Data><Data Name='Seventh'>hidden-preview</Data></EventData>";
        var payloadEvidence = storageEvidence with { Output = storageEvidence.Output.Replace("</Event>", storagePayload + "</Event>") };
        var payloadHistory = StorageEvents.Interpret(payloadEvidence);
        Check(payloadHistory.Detail.Contains(@"DeviceName=\Device\Harddisk2\DR2") && payloadHistory.Detail.Contains("Status=0xC0000185") &&
            DetailLineRegex().IsMatch(payloadHistory.Detail) && payloadHistory.Detail.Contains("value=unnamed") &&
            payloadHistory.Detail.Contains("[shortened]") && payloadHistory.Detail.Contains("Additional fields retained") &&
            !payloadHistory.Detail.Contains("hidden-preview") && payloadHistory.Evidence == payloadEvidence && payloadHistory.Repair is null,
            "Storage payload preview retains device evidence, bounds fields, neutralizes line breaks and preserves full XML: " + payloadHistory.Detail);
        var userPayload = storageEvidence with { Output = storageEvidence.Output.Replace("</Event>", "<UserData><Storage xmlns='urn:fixture'><Volume>D:</Volume><Device>disk2</Device></Storage></UserData></Event>") };
        var userHistory = StorageEvents.Interpret(userPayload);
        Check(userHistory.Detail.Contains("Volume=D:") && userHistory.Detail.Contains("Device=disk2") && userHistory.Repair is null && userHistory.Evidence == userPayload,
            "Storage UserData fields are previewed without selecting a repair or changing raw evidence");
        foreach (var badStorage in new[] { storageEvidence with { TimedOut = true }, storageEvidence with { Truncated = true }, storageEvidence with { ExitCode = 5 },
            storageEvidence with { Output = "broken" }, storageEvidence with { Output = storageEvidence.Output.Replace("disk", "Other") },
            storageEvidence with { Output = "<Events>" + string.Concat(Enumerable.Repeat(installerEvent.Replace("MsiInstaller", "disk"), 201)) + "</Events>" } })
            Check(StorageEvents.Interpret(badStorage).State == CheckState.Unknown, "Incomplete storage history stays unknown");
        try { StorageEvents.Interpret(storageEvidence, new CancellationToken(true)); throw new Exception("Storage history cancellation ignored"); }
        catch (OperationCanceledException) { }
        var timeEvidence = new ToolResult(0, "<Events>" + timeEvent + "</Events>", "", false);
        var timeHistory = TimeServiceEvents.Interpret(timeEvidence);
        var timePayload = timeEvidence with { Output = timeEvidence.Output.Replace("</Event>", "<EventData><Data Name='Peer'>time.example.test</Data><Data Name='Detail'>" + new string('x', 210) + "</Data></EventData></Event>") };
        var timePayloadHistory = TimeServiceEvents.Interpret(timePayload);
        Check(timePayloadHistory.Detail.Contains("Peer=time.example.test") && timePayloadHistory.Detail.Contains("[shortened]") &&
            timePayloadHistory.Evidence == timePayload && timePayloadHistory.Repair is null,
            "Time history previews bounded source fields without authorizing synchronization or changing evidence");
        Check(timeHistory.State == CheckState.Information && timeHistory.Repair is null && timeHistory.Evidence == timeEvidence &&
            timeHistory.Detail.Contains("1 warning, error or critical") && timeHistory.Detail.Contains("record 42"), "Time history retains references without authorizing clock changes");
        Check(TimeServiceEvents.Interpret(timeEvidence with { Output = "<Events />" }).State == CheckState.Information, "Empty time history is not a health verdict");
        foreach (var invalidTime in new[] { timeEvidence with { TimedOut = true }, timeEvidence with { ExitCode = 5 },
            timeEvidence with { Truncated = true }, timeEvidence with { Output = "broken" },
            timeEvidence with { Output = timeEvidence.Output.Replace("Microsoft-Windows-Time-Service", "Other") },
            timeEvidence with { Output = timeEvidence.Output.Replace("<EventRecordID>42</EventRecordID>", "") } })
            Check(TimeServiceEvents.Interpret(invalidTime).State == CheckState.Unknown, "Incomplete time history stays unknown");
        var timeCap = TimeServiceEvents.Interpret(timeEvidence with { Output = "<Events>" + string.Concat(Enumerable.Repeat(timeEvent, 201)) + "</Events>" });
        Check(timeCap.State == CheckState.Unknown && timeCap.Detail.Contains("200-event limit"), "Time history reports bounded coverage");
        try { TimeServiceEvents.Interpret(timeEvidence, new CancellationToken(true)); throw new Exception("Time history cancellation ignored"); }
        catch (OperationCanceledException) { }
        var snapshot = new ToolResult(0, "<Events>" + installerEvent + "</Events>", "", false);
        var history = InstallerEvents.Interpret(snapshot);
        foreach (int id in new[] { 1001, 1002, 1003, 1004, 10005, 11708 })
        {
            string pattern = installerEvent.Replace("11708", id.ToString());
            var installerReport = EventEvidence.Analyze(EventEvidence.FromXml("Application", "<Events>" + pattern + pattern + "</Events>").Evidence!);
            var finding = installerReport.Findings.Single(f => f.RuleId == "KB-MSI-" + id);
            Check(finding.Occurrences == 2 && finding.Evidence.Count == 2 && finding.Evidence[1].LineNumber == 2,
                "Installer event rules retain grouped source references");
            Check(InstallerEvents.Interpret(snapshot with { Output = "<Events>" + pattern + "</Events>" }).Detail.Contains(finding.NextStep),
                "Installer workflow exposes specific guidance");
            foreach (string other in new[] { pattern.Replace("MsiInstaller", "Other"), pattern.Replace("<Level>2", "<Level>4") })
                Check(!EventEvidence.Analyze(EventEvidence.FromXml("Application", "<Events>" + other + "</Events>").Evidence!).Findings.Any(f => f.RuleId.StartsWith("KB-MSI-")),
                    "Installer pattern requires supported provider and severity");
        }
        Check(InstallerEvents.Interpret(snapshot with { Error = "Fixture failure" }).State == CheckState.Unknown,
            "Installer collection error overrides valid stdout");
        var productSnapshot = snapshot with { Output = snapshot.Output.Replace("</Event>",
            "<EventData><Data Name=\"ProductName\">Example App</Data><Data Name=\"ErrorCode\">1603</Data><Data>Package.msi</Data></EventData></Event>") };
        var productHistory = InstallerEvents.Interpret(productSnapshot);
        Check(productHistory.Detail.Contains("ProductName=Example App") && productHistory.Detail.Contains("ErrorCode=1603") &&
            productHistory.Detail.Contains("value=Package.msi") && productHistory.Repair is null && productHistory.Evidence == productSnapshot,
            "Installer failure summary retains reported product and code without inferring a repair");
        var longPayload = snapshot with { Output = snapshot.Output.Replace("</Event>", "<EventData>" +
            string.Concat(Enumerable.Range(0, 7).Select(i => "<Data Name=\"Field" + i + "\">" + new string('x', 250) + "</Data>")) + "</EventData></Event>") };
        var boundedPayload = InstallerEvents.Interpret(longPayload);
        Check(boundedPayload.Detail.Contains("[shortened]") && boundedPayload.Detail.Contains("Additional fields retained") &&
            !boundedPayload.Detail.Contains("Field6=") && boundedPayload.Evidence == longPayload,
            "Installer payload previews are bounded while original XML remains intact");
        var cappedHistory = InstallerEvents.Interpret(snapshot with { Output = "<Events>" + string.Concat(Enumerable.Repeat(installerEvent, 201)) + "</Events>" });
        Check(cappedHistory.State == CheckState.Unknown && cappedHistory.Detail.Contains("200-event limit") &&
            !cappedHistory.Detail.Contains("malformed or unexpected"), "Capped valid installer history explains omitted records without claiming corruption");
        Check(history.State == CheckState.Information && history.Repair is null && history.Detail.Contains("1 error or critical") && history.Detail.Contains("record 42"),
            "Installer history provides record references without selecting a repair");
        Check(InstallerEvents.Interpret(snapshot with { Output = "<Events />" }).State == CheckState.Information,
            "Empty installer history cannot establish healthy installation");
        foreach (string malformed in new[] {
            installerEvent.Replace("<EventID>11708</EventID>", ""),
            installerEvent.Replace("<Level>2</Level>", "<Level>2</Level><Level>4</Level>"),
            installerEvent.Replace("<EventRecordID>42</EventRecordID>", "<EventRecordID>0</EventRecordID>"),
            installerEvent.Replace("11708", "65536"), installerEvent.Replace("2026-09-12T12:00:00Z", "not-a-time"),
            installerEvent.Replace("</System>", "</System><System />") })
        {
            var mixed = InstallerEvents.Interpret(snapshot with { Output = "<Events>" + malformed + installerEvent + "</Events>" });
            Check(mixed.State == CheckState.Unknown && mixed.Detail.Contains("1 events retained; 1 error") && mixed.Repair is null,
                "Malformed event metadata marks coverage incomplete while retaining valid event evidence");
        }
        foreach (var incomplete in new[] { snapshot with { TimedOut = true }, snapshot with { Truncated = true }, snapshot with { ExitCode = 5 },
            snapshot with { Output = "broken" }, snapshot with { Output = snapshot.Output.Replace("MsiInstaller", "Other") },
            snapshot with { Output = "<Events>" + string.Concat(Enumerable.Repeat(installerEvent, 201)) + "</Events>" } })
            Check(InstallerEvents.Interpret(incomplete).State == CheckState.Unknown, "Incomplete installer history remains unresolved");
        try { InstallerEvents.Interpret(snapshot, new CancellationToken(true)); throw new Exception("Installer history cancellation ignored"); }
        catch (OperationCanceledException) { }
        string error = "2026-09-12 12:00:01.123, Error CBS Failed source [HRESULT = 0x800f081f]";
        const string msi = "MSI (s) (EC:BC) [12:34:56:123]: MainEngineThread is returning 1603";
        const string actionFailure = "Action ended 12:34:56: InstallFiles. Return value 3.";
        var actionReport = Investigation.Analyze(new("install.log", [msi.Replace("1603", "0"), "Package-specific detail", actionFailure], false));
        var actionFinding = actionReport.Findings.Single();
        Check(actionFinding.RuleId == "KB-MSI-ACTION" && actionFinding.Evidence.Select(e => e.LineNumber).SequenceEqual(ActionFailureLineOrder) &&
            actionFinding.Evidence[0].Excerpt.Contains("InstallFiles"), "MSI action failure leads with the failed action before bounded context");
        Check(Investigation.Analyze(new("notes.txt", [actionFailure], false)).Findings.Count == 0, "Action phrase alone does not establish MSI context");
        foreach (string invalidAction in new[] { actionFailure.Replace("value 3", "value 1"), actionFailure.Replace("12:34", "99:34"), "example " + actionFailure })
            Check(Investigation.Analyze(new("install.log", [msi.Replace("1603", "0"), invalidAction], false)).Findings.Count == 0,
                "Successful, malformed and quoted MSI actions excluded");
        Check(Investigation.Analyze(new("install.log", [msi.Replace("1603", "0"), actionFailure], true)).Findings.Count == 0,
            "Truncated action failure excluded");
        foreach (string code in new[] { "1601", "1603", "1604", "1605", "1606", "1607", "1610", "1612", "1613", "1618", "1619", "1620", "1621", "1622", "1623", "1624", "1625", "1631", "1632", "1633", "1635", "1636", "1637", "1638", "1639", "1642", "1643", "1644", "1646", "1647", "1648", "1649", "1652", "1653" })
        {
            var installation = Investigation.Analyze(new("install.log", [msi.Replace("1603", code)], false));
            Check(installation.Format == "Windows Installer" && installation.Findings.Single().RuleId == "KB-MSI-" + code &&
                installation.Findings.Single().Evidence.Single().LineNumber == 1, "MSI outcomes retain exact source references");
            foreach (string invalidOutcome in new[] { "example " + msi.Replace("1603", code),
                msi.Replace("1603", code + "0"), msi.Replace("1603", code) + " explanation" })
                Check(Investigation.Analyze(new("install.log", [invalidOutcome], false)).Findings.Count == 0,
                    "Each MSI outcome rejects quoted, extended and suffixed records");
            Check(Investigation.Analyze(new("install.log", [msi.Replace("1603", code)], true)).Findings.Count == 0,
                "Each MSI outcome rejects incomplete records");
        }
        foreach (string code in new[] { "0", "1641", "3010" })
        {
            string successful = msi.Replace("1603", code);
            var outcome = Investigation.Analyze(new("install.log", [successful], false));
            Check(outcome.Format == "Windows Installer" && outcome.Findings.Count == 0 && outcome.Timeline.Single().Level == "INFO" &&
                outcome.Timeline.Single().LineNumber == 1 && outcome.Timeline.Single().Message.Contains("unverified"),
                "Successful installer outcomes retain informational timeline evidence without current-health claims");
            if (code != "0") Check(outcome.Timeline.Single().Message.Contains("restart"), "Historical restart outcome remains visible");
            Check(Investigation.Analyze(new("install.log", [successful], true)).Timeline.Count == 0,
                "Incomplete success lines do not establish an outcome");
        }
        foreach (string invalid in new[] { msi.Replace("1603", "0"), msi.Replace("1603", "3010"), msi.Replace("1603", "1641"),
            msi.Replace("1603", "16030"), msi.Replace("12:34", "99:34"), "quoted: " + msi, msi + " example", "error 1603" })
            Check(Investigation.Analyze(new("install.log", [invalid], false)).Findings.Count == 0,
                "MSI success, restart-success, malformed and unrelated text cannot become failure findings");
        Check(Investigation.Analyze(new("install.log", [msi], true)).Findings.Count == 0, "Incomplete MSI outcome excluded");
        var installationRepeated = Investigation.Analyze(new("install.log", [.. Enumerable.Repeat(msi, 12)], false));
        Check(installationRepeated.Findings.Single().Occurrences == 12 && installationRepeated.Findings.Single().Evidence.Count == 8,
            "Repeated MSI outcomes retain bounded evidence without claiming unique installations");
        var report = Investigation.Analyze(new("CBS.log", [error, error.Replace("Error", "Info"), error.Replace("800f081f", "80073712")], false));
        Check(report.Findings.Count == 2 && report.Findings.All(f => f.Occurrences == 1), "Positive rules and informational exclusion");
        Check(report.Timeline[0].Timestamp.EndsWith(".123") && report.Findings[0].Evidence[0].LineNumber > 0, "Preserve timestamp precision and references");
        var catalogReport = Investigation.Analyze(new("CBS.log", [error.Replace("800f081f", "80246008")], false));
        var catalogFinding = catalogReport.Findings.Single();
        Check(catalogFinding.RuleId == "KB-CBS-80246008" && catalogFinding.Title.Contains("WU_E_DM_FAILTOCONNECTTOBITS") &&
            catalogFinding.NextStep.Contains("BITS", StringComparison.Ordinal) && catalogFinding.Evidence.Single().LineNumber == 1,
            "CBS log analysis routes reviewed Windows Update codes without selecting a repair");
        Check(Investigation.Analyze(new("CBS.log", [error.Replace("800f081f", "deadbeef")], false)).Findings.Count == 0,
            "Unknown CBS hexadecimal values do not receive invented guidance");
        Check(Investigation.Analyze(new("CBS.log", ["error 0x800f081f", error.Replace("CBS", "OTHER"), error.Replace("2026-09-12", "2026-99-99")], false)).Findings.Count == 0, "Reject unrelated and malformed entries");
        Check(Investigation.Analyze(new("CBS.log", [error], true)).Findings.Count == 0, "Do not diagnose an incomplete final line");
        Check(Investigation.Analyze(new("CBS.log", [error, "partial"], true)).Partial, "Preserve source truncation");
        string setup = error.Replace("CBS", "SP");
        Check(Investigation.Analyze(new("setupact.log", [setup], false)).Findings.Single().RuleId == "KB-SETUP-001", "Recognize Setup error");
        var setupCatalogReport = Investigation.Analyze(new("setupact.log", [setup.Replace("800f081f", "C1900208")], false));
        var setupCatalogFinding = setupCatalogReport.Findings.Single();
        Check(setupCatalogFinding.RuleId == "KB-SETUP-C1900208" && setupCatalogFinding.Title.Contains("MOSETUP_E_COMPAT_INSTALLREQ_BLOCK") &&
            setupCatalogFinding.NextStep.Contains("CompatData", StringComparison.Ordinal) && setupCatalogFinding.Evidence.Single().LineNumber == 1,
            "Setup log analysis routes reviewed Setup codes without selecting a repair");
        var setupPairFinding = Investigation.Analyze(new("setuperr.log", [setup.Replace("800f081f", "C1900101 - 0x20017")], false)).Findings.Single();
        Check(setupPairFinding.RuleId == "KB-SETUP-C1900101-20017" && setupPairFinding.Explanation.Contains("Safe OS phase") &&
            setupPairFinding.Explanation.Contains("boot operation") && setupPairFinding.Evidence.Single().LineNumber == 1,
            "Setup result and extend-code pairs retain reviewed phase and operation context");
        foreach (string invalidPair in new[] { "C1900101 - 0x60017", "C1900101 - 0x20117", "C1900101 - 0x20021" })
        {
            var fallback = Investigation.Analyze(new("setuperr.log", [setup.Replace("800f081f", invalidPair)], false)).Findings.Single();
            Check(fallback.RuleId == "KB-SETUP-C1900101" && !fallback.Explanation.Contains("companion extend code"),
                "Unknown Setup extend codes do not receive invented phase or operation guidance");
        }
        Check(Investigation.Analyze(new("setupact.log", [setup.Replace("800f081f", "C1900210")], false)).Findings.Single().Severity == "Information",
            "Informational Setup results remain distinct from repair-review findings");
        Check(Investigation.Analyze(new("random.log", [setup], false)).Findings.Count == 0, "Require setup source identity");
        var repeated = Investigation.Analyze(new("CBS.log", [.. Enumerable.Repeat(error, 15)], false));
        Check(repeated.Findings.Single().Occurrences == 15 && repeated.Findings.Single().Evidence.Count == 8, "Bound finding references");
        var bounded = Investigation.Analyze(new("CBS.log", [.. Enumerable.Repeat("2026-09-12 12:00:01, Info CBS x", 10002)], false));
        Check(bounded.ParsedLines == 10000 && bounded.Partial, "Bound parsed entries");
        try { Investigation.Analyze(new("CBS.log", [error], false), new CancellationToken(true)); throw new Exception("Cancellation ignored"); }
        catch (OperationCanceledException) { }
        using var json = JsonDocument.Parse(Investigation.ToJson(report));
        Check(json.RootElement.GetProperty("findings").GetArrayLength() == 2 && json.RootElement.GetProperty("source").GetString() == "CBS.log", "Export retains findings and provenance");
        Console.WriteLine("PASS: CBS/Setup rules, catalog-backed result and extend-code guidance, negative cases, timestamps, partial input, reference bounds, cancellation, JSON report.");
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
