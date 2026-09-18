using KillerBench.Engine;

internal static class AudioDeviceChecks
{
    internal static void Run()
    {
        var unclassified = new ToolResult(0, "{\"devices\":[{\"DeviceID\":\"FAULT1\",\"Present\":true,\"ConfigManagerErrorCode\":28}]}", "", false);
        var unclassifiedFault = DeviceDiagnostics.Interpret(unclassified, DeviceCategory.Problems).Single();
        Require(unclassifiedFault.State == CheckState.Attention && unclassifiedFault.Detail.Contains("FAULT1") &&
            unclassifiedFault.Evidence == unclassified && unclassifiedFault.Repair is null,
            "Missing setup class preserves an identified cross-class fault");
        foreach (var category in Enum.GetValues<DeviceCategory>().Where(category => category != DeviceCategory.Problems))
            Require(DeviceDiagnostics.Interpret(unclassified, category).Single().State == CheckState.Unknown,
                "Missing setup class cannot establish category membership");
        var malformedClass = unclassified with { Output = unclassified.Output.Replace("\"Present\"", "\"PNPClass\":123,\"Present\"") };
        Require(DeviceDiagnostics.Interpret(malformedClass, DeviceCategory.Problems).Single().State == CheckState.Unknown,
            "Malformed setup class remains invalid evidence");
        foreach (string nameField in new[] { "", "\"Name\":null,", "\"Name\":\"\"," })
        {
            var evidence = new ToolResult(0, "{\"devices\":[{\"DeviceID\":\"FAULT1\"," + nameField + "\"PNPClass\":null,\"Present\":true,\"ConfigManagerErrorCode\":28}]}", "", false);
            var finding = DeviceDiagnostics.Interpret(evidence, DeviceCategory.Problems).Single();
            Require(finding.State == CheckState.Attention && finding.Name.Contains("Unnamed device") && finding.Detail.Contains("FAULT1") &&
                finding.Evidence == evidence && finding.Repair is null, "Missing display names preserve identified device faults");
            Require(DeviceDiagnostics.Interpret(evidence with { Output = evidence.Output.Replace("FAULT1", "bad\\nidentity") }, DeviceCategory.Problems).Single().State == CheckState.Unknown,
                "Control characters cannot establish device identity");
        }
        foreach (string? deviceClass in new string?[] { "USB", "Bluetooth", "UnknownVendorClass", null })
        {
            string payload = System.Text.Json.JsonSerializer.Serialize(new { devices = new[] { new { DeviceID = "FAULT1", Name = "Fixture device", PNPClass = deviceClass, Present = true, ConfigManagerErrorCode = 28 } } });
            var finding = DeviceDiagnostics.Interpret(new(0, payload, "", false), DeviceCategory.Problems).Single();
            Require(finding.State == CheckState.Attention && finding.Repair is null && finding.Detail.Contains("drivers are not installed"),
                "Cross-class problems retain faults even when a setup class is unavailable");
            Require(DeviceDiagnostics.Interpret(new(0, payload.Replace(":28", ":0"), "", false), DeviceCategory.Problems).Single().State == CheckState.Unknown,
                "Unexpected zero-code records do not satisfy a problems-only query");
        }
        Require(DeviceDiagnostics.Interpret(new(0, "{\"devices\":[]}", "", false), DeviceCategory.Problems).Single().State == CheckState.Information &&
            !Workflows.SupportsRepair(WorkflowKind.DeviceProblems) && Workflows.Scopes(WorkflowKind.DeviceProblems).Length == 0,
            "Empty problems query is scoped information and never authorizes repair");
        foreach (string inputClass in new[] { "Keyboard", "Mouse", "HIDClass", "Net" })
        {
            string payload = System.Text.Json.JsonSerializer.Serialize(new { devices = new[] { new { DeviceID = "INPUT1", Name = "Fixture input", PNPClass = inputClass, Present = true, ConfigManagerErrorCode = 10 } } });
            var finding = DeviceDiagnostics.Interpret(new(0, payload, "", false), DeviceCategory.Input).Single();
            Require(finding.State == (inputClass == "Net" ? CheckState.Unknown : CheckState.Attention) && finding.Repair is null,
                "Input workflow accepts only supported device classes and never selects driver changes");
        }
        Require(!Workflows.SupportsRepair(WorkflowKind.Input) && Workflows.Scopes(WorkflowKind.Input).Length == 0, "Input diagnostics cannot dispatch repairs");
        string queueLimitPayload = System.Text.Json.JsonSerializer.Serialize(new
        {
            queues = Enumerable.Range(0, 257).Select(i => new { Name = "Queue" + i, DriverName = "Fixture", PortName = "PORT" }),
            drivers = new[] { new { Name = "Fixture", PrinterEnvironment = "Windows x64" } }
        });
        var queueLimitEvidence = new ToolResult(0, queueLimitPayload, "", false);
        var queueLimitFindings = PrinterDriverDiagnostics.Interpret(queueLimitEvidence);
        Require(queueLimitFindings.Count == 257 && queueLimitFindings[0].State == CheckState.Unknown &&
            queueLimitFindings[0].Detail.Contains("Partial queue coverage") &&
            queueLimitFindings.Count(c => c.State == CheckState.Information) == 256 &&
            queueLimitFindings.All(c => c.Repair is null && c.Evidence == queueLimitEvidence) &&
            queueLimitFindings.All(c => c.Name != "Print queue driver: Queue256"),
            "Large print inventories retain bounded queue matches with explicit incomplete coverage");
        foreach (string error in new[] { "\"Provider failed\"", "\"\"", "123", "{}" })
        {
            string payload = "{\"devices\":[{\"DeviceID\":\"GPU\",\"Name\":\"Fixture\",\"PNPClass\":\"Display\",\"Present\":true,\"ConfigManagerErrorCode\":0}]," +
                "\"drivers\":[{\"DeviceID\":\"GPU\",\"DriverVersion\":\"1.2\",\"DriverProviderName\":\"Fixture\"}],\"driverError\":" + error + "}";
            var evidence = new ToolResult(0, payload, "", false);
            var findings = DeviceDiagnostics.Interpret(evidence, DeviceCategory.Display);
            Require(findings.Count == 3 && findings[0].State == CheckState.Unknown && findings[1].State == CheckState.Clear &&
                findings[2].State == CheckState.Unknown && !findings[2].Detail.Contains("Version:") &&
                findings.All(c => c.Evidence == evidence && c.Repair is null),
                "Failed or malformed driver collection cannot establish a driver match from partial results");
        }
        string manyDevices = "{\"devices\":[" + string.Join(",", Enumerable.Range(0, 257).Select(i =>
            "{\"DeviceID\":\"GPU" + i + "\",\"Name\":\"Fixture\",\"PNPClass\":\"Display\",\"Present\":true,\"ConfigManagerErrorCode\":0}")) + "]}";
        var limited = DeviceDiagnostics.Interpret(new(0, manyDevices, "", false), DeviceCategory.Display);
        Require(limited.Count == 257 && limited.Count(c => c.State == CheckState.Clear) == 256 && limited[0].State == CheckState.Unknown &&
            limited.All(c => c.Repair is null), "Oversized device inventory preserves bounded observations and explicit incomplete coverage");
        var mixedEvidence = new ToolResult(0, "{\"devices\":[{\"DeviceID\":\"GPU\",\"Name\":\"Fixture\",\"PNPClass\":\"Display\",\"Present\":true,\"ConfigManagerErrorCode\":0}]}", "Provider returned incomplete data", false);
        var mixedFinding = DeviceDiagnostics.Interpret(mixedEvidence, DeviceCategory.Display).Single();
        Require(mixedFinding.State == CheckState.Unknown && mixedFinding.Evidence == mixedEvidence && mixedFinding.Repair is null,
            "Error-stream evidence prevents a clean device verdict and preserves both streams");
        foreach (uint code in new uint[] { 39, 43, 48, 52 })
        {
            string json = "{\"devices\":[{\"DeviceID\":\"GPU\",\"Name\":\"Fixture\",\"PNPClass\":\"Display\",\"Present\":true,\"ConfigManagerErrorCode\":" + code + "}]}";
            var finding = DeviceDiagnostics.Interpret(new(0, json, "", false), DeviceCategory.Display).Single();
            Require(finding.State == CheckState.Attention && finding.Repair is null && !finding.Detail.Contains("Review this code in Device Manager"),
                "Known driver faults have specific review guidance without automatic mutation");
        }
        foreach (string deviceClass in new[] { "Display", "Monitor", "Net" })
        {
            string displayJson = "{\"devices\":[{\"DeviceID\":\"GPU1\",\"Name\":\"Fixture\",\"PNPClass\":\"" + deviceClass + "\",\"Present\":true,\"ConfigManagerErrorCode\":43}]}";
            var displayFinding = DeviceDiagnostics.Interpret(new(0, displayJson, "", false), DeviceCategory.Display).Single();
            Require(displayFinding.State == (deviceClass == "Net" ? CheckState.Unknown : CheckState.Attention) && displayFinding.Repair is null,
                "Display workflow classifies supported device faults without driver changes");
        }
        Require(!Workflows.SupportsRepair(WorkflowKind.Display) && Workflows.Scopes(WorkflowKind.Display).Length == 0,
            "Display is diagnostic only");
        const string queueEvidence = "{\"queues\":[{\"Name\":\"Office\",\"DriverName\":\"Vendor Driver\",\"PortName\":\"PORT1\"}],\"drivers\":[{\"Name\":\"Vendor Driver\",\"PrinterEnvironment\":\"Windows x64\"}]}";
        var matched = PrinterDriverDiagnostics.Interpret(new(0, queueEvidence, "", false)).Single();
        var mixedPrintEvidence = new ToolResult(0, queueEvidence, "Provider returned partial registrations", false);
        var mixedPrintFinding = PrinterDriverDiagnostics.Interpret(mixedPrintEvidence).Single();
        Require(mixedPrintFinding.State == CheckState.Unknown && mixedPrintFinding.Evidence == mixedPrintEvidence &&
            mixedPrintFinding.Repair is null && !mixedPrintFinding.Detail.Contains("One registered name match"),
            "Printer error output invalidates a successful-looking registration match");
        Require(matched.State == CheckState.Information && matched.Detail.Contains("Windows x64") && matched.Repair is null, "Queue driver inventory matches exact names without authorizing changes");
        var multiple = PrinterDriverDiagnostics.Interpret(new(0, queueEvidence.Replace("\"Windows x64\"}]", "\"Windows x64\"},{\"Name\":\"Vendor Driver\",\"PrinterEnvironment\":\"Windows NT x86\"}]"), "", false)).Single();
        Require(multiple.State == CheckState.Unknown && multiple.Detail.Contains("2 matching registrations") && multiple.Detail.Contains("Windows NT x86") &&
            multiple.Detail.Contains("No environment was selected"), "Multiple driver environments remain explicit unresolved candidates");
        var failedDriverQuery = new ToolResult(0, queueEvidence.TrimEnd('}') + ",\"driverError\":\"Access denied\\nprovider\"}", "", false);
        var partialQueues = PrinterDriverDiagnostics.Interpret(failedDriverQuery);
        Require(partialQueues.Count == 2 && partialQueues.All(c => c.State == CheckState.Unknown && c.Repair is null && c.Evidence == failedDriverQuery) &&
            partialQueues[0].Detail.Contains("Access denied provider") && partialQueues[1].Detail.Contains("Vendor Driver"),
            "Driver query errors invalidate apparent matches while retaining queue identity and raw evidence");
        foreach (string invalid in new[] { "null", "{}", queueEvidence.Replace("Vendor Driver\",\"PrinterEnvironment", "Other\",\"PrinterEnvironment"),
            queueEvidence.Replace("\"PrinterEnvironment\":\"Windows x64\"", "\"PrinterEnvironment\":null"),
            queueEvidence.Replace("\"PortName\":\"PORT1\"", "\"PortName\":\"PORT1\",\"PortName\":\"PORT2\"") })
            Require(PrinterDriverDiagnostics.Interpret(new(0, invalid, "", false)).Any(c => c.State == CheckState.Unknown), "Missing or ambiguous queue-driver evidence stays unknown");
        foreach (var incomplete in new[] { new ToolResult(5, queueEvidence, "", false), new ToolResult(0, queueEvidence, "", true), new ToolResult(0, queueEvidence, "", false) { TimedOut = true } })
            Require(PrinterDriverDiagnostics.Interpret(incomplete).All(c => c.State == CheckState.Unknown && c.Repair is null && c.Evidence == incomplete), "Incomplete driver collection preserves evidence without a match");
        const string device = "\"devices\":[{\"DeviceID\":\"A\",\"Name\":\"Speaker\",\"PNPClass\":\"MEDIA\",\"Present\":true,\"ConfigManagerErrorCode\":0}]";
        foreach (string provider in new[] { "", "bad\\nprovider", new string('x', 257) })
        {
            var invalidMetadata = AudioDeviceDiagnostics.Interpret(new(0, "{" + device + ",\"drivers\":[{\"DeviceID\":\"A\",\"DriverVersion\":\"1.2\",\"DriverProviderName\":\"" + provider + "\"}]}", "", false));
            Require(invalidMetadata[1].State == CheckState.Unknown, "Invalid driver provider metadata cannot establish complete inventory");
        }
        foreach (string id in new[] { "A", "B" })
        {
            var inventory = AudioDeviceDiagnostics.Interpret(new(0, "{" + device + ",\"drivers\":[{\"DeviceID\":\"" + id + "\",\"DriverVersion\":\"1.2.3\",\"DriverProviderName\":\"Fixture\"}]}", "", false));
            Require(inventory.Count == 2 && inventory[1].Detail.Contains("1.2.3") == (id == "A"), "Driver inventory joins exact device identities only");
            Require(inventory[1].State == (id == "A" ? CheckState.Information : CheckState.Unknown), "Unmatched driver coverage remains unknown");
        }
        const string good = "{\"devices\":[{\"DeviceID\":\"fixture\",\"Name\":\"Fixture audio\",\"PNPClass\":\"MEDIA\",\"Present\":true,\"ConfigManagerErrorCode\":0}]}";
        foreach (var category in Enum.GetValues<DeviceCategory>())
        {
            string payload = good.Replace("MEDIA", category == DeviceCategory.Audio ? "MEDIA" : category == DeviceCategory.Network ? "Net" : category == DeviceCategory.Printing ? "Printer" : category == DeviceCategory.Display ? "Display" : category == DeviceCategory.Input ? "Keyboard" : category == DeviceCategory.Portable ? "WPD" : category == DeviceCategory.Bluetooth ? "Bluetooth" : category == DeviceCategory.Camera ? "Camera" : category == DeviceCategory.SmartCard ? "SmartCardReader" : category == DeviceCategory.Biometric ? "Biometric" : "DiskDrive")
                .Replace("\"Present\":", "\"HardwareID\":[\"fixture-id\"],\"Present\":")
                .Replace("}]}", "}],\"drivers\":null}");
            foreach (var evidence in new[] { new ToolResult(0, payload, "provider warning", false),
                new ToolResult(5, payload, "access denied", false), new ToolResult(0, payload, "", true),
                new ToolResult(0, "{\"devices\":[]}", "", false), new ToolResult(0, "invalid JSON", "", false) })
                Require(DeviceDiagnostics.Interpret(evidence, category).All(check => check.Evidence == evidence),
                    "Device observations retain exact provider output, errors and completeness flags for journals");
        }
        foreach (string deviceClass in new[] { "Printer", "PrintQueue" })
        {
            var printer = new ToolResult(0, good.Replace("MEDIA", deviceClass).Replace(":0", ":28"), "", false);
            var finding = DeviceDiagnostics.Interpret(printer, DeviceCategory.Printing).Single();
            Require(finding.State == CheckState.Attention && finding.Repair is null && finding.Detail.Contains("drivers are not installed"), "Printer device failures are separate diagnostic evidence, not queue repair authorization");
        }
        Require(DeviceDiagnostics.Interpret(new(0, "{\"devices\":[]}", "", false), DeviceCategory.Printing).Single().State == CheckState.Information,
            "No PnP printers does not claim missing remote queues");
        Require(DeviceDiagnostics.Interpret(new(0, good, "", false), DeviceCategory.Printing).Single().State == CheckState.Unknown,
            "Unrelated device classes cannot satisfy printing coverage");
        var portable = DeviceDiagnostics.Interpret(new(0, good.Replace("MEDIA", "WPD").Replace(":0", ":10"), "", false), DeviceCategory.Portable).Single();
        Require(portable.State == CheckState.Attention && portable.Repair is null,
            "Portable device faults remain diagnostic evidence and never authorize a driver change");
        Require(DeviceDiagnostics.Interpret(new(0, "{\"devices\":[]}", "", false), DeviceCategory.Portable).Single().State == CheckState.Information,
            "No connected portable device is normal scoped information");
        Require(DeviceDiagnostics.Interpret(new(0, good, "", false), DeviceCategory.Portable).Single().State == CheckState.Unknown,
            "Unrelated device classes cannot satisfy portable device coverage");
        Require(AudioDeviceDiagnostics.Interpret(new(0, good, "", false)).Single().State == CheckState.Clear, "Audio device configuration recognized");
        var failedDriverEvidence = new ToolResult(0, good.Replace("}]}", "}],\"drivers\":null,\"driverError\":\"Access denied\\nprovider\"}"), "", false);
        var failedDriver = DeviceDiagnostics.Interpret(failedDriverEvidence, DeviceCategory.Audio);
        Require(failedDriver.Count(check => check.State == CheckState.Clear) == 1 &&
            failedDriver.Count(check => check.State == CheckState.Unknown) == 2 &&
            failedDriver[0].Detail.Contains("Access denied provider") && failedDriver.All(check => check.Repair is null && check.Evidence == failedDriverEvidence),
            "Driver query failure remains unknown while independent device configuration evidence survives");
        foreach (string storageClass in new[] { "DiskDrive", "HDC", "SCSIAdapter" })
        {
            var storage = new ToolResult(0, good.Replace("MEDIA", storageClass).Replace(":0", ":10"), "", false);
            var check = DeviceDiagnostics.Interpret(storage, DeviceCategory.Storage).Single();
            Require(check.State == CheckState.Attention && check.Repair is null && check.Name.StartsWith("Storage device:"), "Storage device errors are identified without authorizing driver changes");
            Require(DeviceDiagnostics.Interpret(storage with { Truncated = true }, DeviceCategory.Storage).Single().State == CheckState.Unknown, "Incomplete storage inventory remains unknown");
        }
        Require(DeviceDiagnostics.Interpret(new(0, good, "", false), DeviceCategory.Storage).Single().State == CheckState.Unknown, "Unrelated device classes do not establish storage health");
        foreach (int code in new[] { 1, 10, 14, 18, 22, 24, 28, 31, 43 })
        {
            var check = AudioDeviceDiagnostics.Interpret(new(0, good.Replace(":0", ":" + code), "", false)).Single();
            Require(check.State == CheckState.Attention && check.Repair is null && check.Detail.Contains("code " + code), "Device faults remain explicit and do not change drivers or policy");
        }
        Require(AudioDeviceDiagnostics.Interpret(new(0, good.Replace("true", "false"), "", false)).Single().State == CheckState.Information, "Absent device is not proof of driver corruption");
        foreach (var invalid in new[] { new ToolResult(0, good.Replace("true", "null"), "", false), new ToolResult(0, good.Replace("MEDIA", "NET"), "", false), new ToolResult(5, good, "", false), new ToolResult(0, good, "", true), new ToolResult(0, good, "", false) { TimedOut = true } })
            Require(AudioDeviceDiagnostics.Interpret(invalid).All(c => c.State == CheckState.Unknown), "Incomplete device evidence cannot report healthy");
        Require(AudioDeviceDiagnostics.Interpret(new(0, "{\"devices\":[]}", "", false)).Single().State == CheckState.Attention, "No audio devices warrants investigation in selected audio workflow");
        string network = good.Replace("MEDIA", "Net");
        Require(DeviceDiagnostics.Interpret(new(0, network, "", false), DeviceCategory.Network).Single().State == CheckState.Clear, "Network device class recognized");
        foreach (int code in new[] { 10, 22, 28, 31 })
        {
            var fault = DeviceDiagnostics.Interpret(new(0, network.Replace(":0", ":" + code), "", false), DeviceCategory.Network).Single();
            Require(fault.Name.StartsWith("Network device") && fault.State == CheckState.Attention && fault.Repair is null, "Network driver faults never trigger a blind adapter reset");
        }
        Require(DeviceDiagnostics.Interpret(new(0, good, "", false), DeviceCategory.Network).Single().State == CheckState.Unknown, "An audio device cannot satisfy network device coverage");
        string bluetooth = good.Replace("MEDIA", "Bluetooth")
            .Replace("\"Present\":", "\"HardwareID\":[\"BTH\\\\MS_BTHBRB\"],\"Present\":")
            .Replace("}]}", "}],\"drivers\":[{\"DeviceID\":\"fixture\",\"DriverVersion\":\"10.0.1\",\"DriverProviderName\":\"Fixture\"}]}");
        var bluetoothChecks = DeviceDiagnostics.Interpret(new(0, bluetooth, "", false), DeviceCategory.Bluetooth);
        Require(bluetoothChecks.Count == 3 && bluetoothChecks[0].State == CheckState.Clear &&
            bluetoothChecks[1].State == CheckState.Information && bluetoothChecks[2].State == CheckState.Information &&
            bluetoothChecks[2].Detail.Contains("10.0.1") && bluetoothChecks.All(check => check.Repair is null),
            "Bluetooth inventory reports device, hardware and exact driver evidence without authorizing a change");
        Require(DeviceDiagnostics.Interpret(new(0, good, "", false), DeviceCategory.Bluetooth).Single().State == CheckState.Unknown,
            "An audio device cannot satisfy Bluetooth device coverage");
        foreach (string deviceClass in new[] { "Camera", "Image", "MEDIA" })
        {
            string payload = good.Replace("MEDIA", deviceClass).Replace(":0", ":10");
            var cameraFinding = DeviceDiagnostics.Interpret(new(0, payload, "", false), DeviceCategory.Camera).Single();
            Require(cameraFinding.State == (deviceClass == "MEDIA" ? CheckState.Unknown : CheckState.Attention) && cameraFinding.Repair is null,
                "Camera workflow accepts only camera and imaging device classes without authorizing driver changes");
        }
        string smartCard = good.Replace("MEDIA", "SmartCardReader")
            .Replace("\"Present\":", "\"HardwareID\":[\"USB\\\\VID_1234&PID_5678\"],\"Present\":")
            .Replace("}]}", "}],\"drivers\":[{\"DeviceID\":\"fixture\",\"DriverVersion\":\"2.4.1\",\"DriverProviderName\":\"Fixture\"}]}");
        var smartCardChecks = DeviceDiagnostics.Interpret(new(0, smartCard, "", false), DeviceCategory.SmartCard);
        Require(smartCardChecks.Count == 3 && smartCardChecks[0].State == CheckState.Clear &&
            smartCardChecks[1].State == CheckState.Information && smartCardChecks[2].State == CheckState.Information &&
            smartCardChecks[2].Detail.Contains("2.4.1") && smartCardChecks.All(check => check.Repair is null),
            "Smart-card inventory reports reader, hardware and exact driver evidence without authorizing a change");
        Require(DeviceDiagnostics.Interpret(new(0, good, "", false), DeviceCategory.SmartCard).Single().State == CheckState.Unknown,
            "An audio device cannot satisfy smart-card reader coverage");
        string biometric = good.Replace("MEDIA", "Biometric")
            .Replace("\"Present\":", "\"HardwareID\":[\"USB\\\\VID_8765&PID_4321\"],\"Present\":")
            .Replace("}]}", "}],\"drivers\":[{\"DeviceID\":\"fixture\",\"DriverVersion\":\"3.2.0\",\"DriverProviderName\":\"Fixture\"}]}");
        var biometricChecks = DeviceDiagnostics.Interpret(new(0, biometric, "", false), DeviceCategory.Biometric);
        Require(biometricChecks.Count == 3 && biometricChecks[0].State == CheckState.Clear &&
            biometricChecks[1].State == CheckState.Information && biometricChecks[2].State == CheckState.Information &&
            biometricChecks[2].Detail.Contains("3.2.0") && biometricChecks.All(check => check.Repair is null),
            "Biometric inventory reports sensor, hardware and exact driver evidence without authorizing a change");
        Require(DeviceDiagnostics.Interpret(new(0, good, "", false), DeviceCategory.Biometric).Single().State == CheckState.Unknown,
            "An audio device cannot satisfy biometric sensor coverage");
        foreach (var category in new[] { DeviceCategory.Audio, DeviceCategory.Network })
        {
            string baseline = category == DeviceCategory.Audio ? good : network;
            string validDevice = baseline[12..^2];
            foreach (string malformed in new[] { "null", "{}", "123", validDevice.Replace("true", "null"), validDevice.Replace(":0", ":-1") })
            {
                foreach (bool invalidFirst in new[] { true, false })
                {
                    string distinctInvalid = malformed.Replace("fixture\"", "broken\"");
                    string records = invalidFirst ? distinctInvalid + "," + validDevice : validDevice + "," + distinctInvalid;
                    var partial = DeviceDiagnostics.Interpret(new(0, "{\"devices\":[" + records + "]}", "", false), category);
                    Require(partial.Count == 2 && partial.Count(check => check.State == CheckState.Clear) == 1 &&
                        partial.Count(check => check.State == CheckState.Unknown) == 1 && partial.All(check => check.Repair is null),
                        "Malformed device records preserve independent valid observations in either order");
                }
            }
            foreach (string hardware in new[] { "null", "[]", "123", "[null]", "[123]", "[\"\"]", "[\" padded\"]", "[\"bad\\nvalue\"]",
                System.Text.Json.JsonSerializer.Serialize(new[] { new string('x', 513) }),
                System.Text.Json.JsonSerializer.Serialize(Enumerable.Repeat("fixture", 65)) })
            {
                string payload = baseline.Replace("\"Present\":", "\"HardwareID\":" + hardware + ",\"Present\":");
                var checks = DeviceDiagnostics.Interpret(new(0, payload, "", false), category);
                Require(checks.Count == 2 && checks[0].State == CheckState.Clear &&
                    checks[1].State == CheckState.Information && checks[1].Detail.Contains("unavailable or incomplete") &&
                    checks.All(check => check.Repair is null), "Malformed hardware identity cannot infer a driver match or authorize repair");
            }
            string valid = baseline.Replace("\"Present\":", "\"HardwareID\":[\"fixture-id\",\"FIXTURE-ID\",\"other-id\"],\"Present\":");
            var hardwareCheck = DeviceDiagnostics.Interpret(new(0, valid, "", false), category)[1];
            Require(hardwareCheck.Detail.Contains("Hardware IDs: fixture-id; other-id.") &&
                hardwareCheck.Detail.Contains("compatibility has not been verified") && hardwareCheck.Repair is null,
                "Hardware inventory deduplicates identity without claiming package compatibility");
        }
        Console.WriteLine("PASS: audio/network device errors, bounded hardware identities and read-only classification.");
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
