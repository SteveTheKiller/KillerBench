using KillerBench.Engine;

internal static class VitalsChecks
{
    internal static void Run()
    {
        foreach (string property in new[] { "memoryError", "diskError", "processorError", "batteryError", "hardwareError", "administratorError", "graphicsError", "memoryModuleError" })
        foreach (string error in new[] { "\"Access denied\\nprovider\"", "123", "\"\"" })
        {
            var evidence = new ToolResult(0, "{\"memory\":{\"total\":100,\"free\":50},\"disks\":[0],\"" + property + "\":" + error + "}", "", false);
            var findings = VitalsDiagnostics.Interpret(evidence);
            int failed = property == "memoryError" ? 0 : property == "diskError" ? 1 : property == "processorError" ? 3 : property == "batteryError" ? 4 :
                property == "hardwareError" ? 5 : property == "administratorError" ? 6 : property == "graphicsError" ? 8 : 9;
            Require(findings[failed].State == CheckState.Unknown && findings[failed].Detail.Contains("Provider collection failed") &&
                !findings[failed].Detail.Contains('\n') && findings[property == "diskError" ? 0 : 1].State != CheckState.Unknown &&
                findings.All(c => c.Evidence == evidence && c.Repair is null),
                "Provider errors invalidate only their own snapshot and retain bounded failure evidence");
            if (property == "memoryError") Require(findings[2].State == CheckState.Unknown && findings[2].Detail.Contains("Provider collection failed") &&
                findings[7].State == CheckState.Unknown && findings[7].Detail.Contains("Provider collection failed"),
                "Operating-system provider failure invalidates its related uptime and version evidence");
        }
        foreach (bool faultWithinLimit in new[] { false, true })
        {
            var values = Enumerable.Repeat(0, 257).ToArray();
            values[faultWithinLimit ? 0 : 256] = 2;
            var evidence = new ToolResult(0, "{\"disks\":" + System.Text.Json.JsonSerializer.Serialize(values) + "}", "", false);
            var finding = VitalsDiagnostics.Interpret(evidence)[1];
            Require(finding.State == (faultWithinLimit ? CheckState.Attention : CheckState.Unknown) &&
                finding.Detail.Contains("Partial coverage") && finding.Detail.Contains("256 evaluated") &&
                finding.Evidence == evidence && finding.Repair is null,
                "Oversized inventories retain bounded fault observations without assessing disks outside the limit");
        }
        const string identified = "{\"disks\":[{\"DeviceId\":\"0\",\"FriendlyName\":\"Fixture SSD\",\"HealthStatus\":2},{\"DeviceId\":\"1\",\"FriendlyName\":\"Other SSD\",\"HealthStatus\":0}]}";
        var identifiedResult = new ToolResult(0, identified, "", false);
        var identifiedFinding = VitalsDiagnostics.Interpret(identifiedResult)[1];
        Require(identifiedFinding.State == CheckState.Attention && identifiedFinding.Detail.Contains("Fixture SSD (device 0): unhealthy") &&
            !identifiedFinding.Detail.Contains("Other SSD") && identifiedFinding.Repair is null && identifiedFinding.Evidence == identifiedResult,
            "Unhealthy disk finding identifies the affected provider disk without selecting a repair");
        foreach (string invalid in new[] { identified.Replace("\"1\"", "\"0\""), identified.Replace("Other SSD", "bad\\nname"), identified.Replace("\"HealthStatus\":0", "\"HealthStatus\":null") })
        {
            var partial = VitalsDiagnostics.Interpret(new(0, invalid, "", false))[1];
            Require(partial.State == CheckState.Attention && partial.Detail.Contains("1 unknown") && partial.Detail.Contains("Fixture SSD"),
                "Malformed or duplicate disk identity preserves an independent unhealthy disk finding");
        }
        Require(VitalsDiagnostics.Interpret(identifiedResult with { Error = "Provider warning" }).All(c => c.State == CheckState.Unknown),
            "Mixed provider error output cannot establish complete vitals evidence");
        const string system = "\"lastBoot\":\"2026-09-12T00:00:00.0000000+00:00\",\"captured\":\"2026-09-13T02:03:00.0000000+00:00\"";
        const string cpu = "\"processors\":[{\"DeviceID\":\"CPU0\",\"Name\":\"Fixture CPU\",\"LoadPercentage\":25,\"NumberOfCores\":8,\"NumberOfLogicalProcessors\":16,\"CurrentClockSpeed\":4200}]";
        var good = VitalsDiagnostics.Interpret(new(0, "{\"memory\":{\"total\":10000,\"free\":5000," + system + "},\"disks\":[0,0]," + cpu + "}", "", false));
        Require(StorageDiagnostics.Interpret(new(0, "Windows has scanned the file system and found no problems", "", false) { TimedOut = true }).State == CheckState.Unknown,
            "Timed-out filesystem evidence cannot establish a clean result");
        Require(good[0].State == CheckState.Information && good[1].State == CheckState.Clear && good[2].State == CheckState.Information &&
            good[2].Detail.Contains("1d 2h 3m") && good[3].State == CheckState.Information && good[3].Detail.Contains("8 cores, 16 logical, 4200 MHz current clock, 25% load"),
            "Memory, uptime and processor snapshots remain scoped observations rather than broad health verdicts");
        var bad = VitalsDiagnostics.Interpret(new(0, "{\"memory\":{\"total\":10000,\"free\":100},\"disks\":[0,2,5]}", "", false));
        Require(bad.Take(2).All(c => c.State == CheckState.Attention && c.Repair is null), "Low memory and unhealthy storage require investigation, not automatic writes");
        foreach (string invalid in new[] {
            "{\"memory\":{" + system.Replace("2026-09-12", "2036-09-12") + "}}",
            "{\"memory\":{\"lastBoot\":\"bad\",\"captured\":\"2026-09-13T02:03:00.0000000+00:00\"}}",
            "{\"memory\":{\"lastBoot\":\"2026-09-12T00:00:00.0000000-07:00\",\"captured\":\"2026-09-13T02:03:00.0000000+00:00\"}}" })
            Require(VitalsDiagnostics.Interpret(new(0, invalid, "", false))[2].State == CheckState.Unknown,
                "Malformed, future and non-UTC uptime evidence stays unknown");
        string longUptime = "{\"memory\":{\"lastBoot\":\"2026-09-01T00:00:00.0000000+00:00\",\"captured\":\"2026-09-13T02:03:00.0000000+00:00\"}}";
        Require(VitalsDiagnostics.Interpret(new(0, longUptime, "", false))[2].State == CheckState.Attention,
            "Uptime beyond the review threshold is visible without selecting a repair");
        foreach (string invalidCpu in new[] {
            cpu.Replace("25", "101"), cpu.Replace("CPU0", "bad\\nidentity"), cpu.Replace("16", "4"), cpu.Replace("4200", "0"),
            cpu.Replace("}]", "},{\"DeviceID\":\"CPU0\",\"Name\":\"Second CPU\",\"LoadPercentage\":10,\"NumberOfCores\":4,\"NumberOfLogicalProcessors\":8,\"CurrentClockSpeed\":3200}]") })
        {
            string payload = invalidCpu.StartsWith("\"processors\"") ? "{" + invalidCpu + "}" : "{" + cpu + "}";
            Require(VitalsDiagnostics.Interpret(new(0, payload, "", false))[3].State == CheckState.Unknown,
                "Malformed or contradictory processor evidence stays unknown");
        }
        var noBattery = VitalsDiagnostics.Interpret(new(0, "{\"batteries\":[]}", "", false))[4];
        Require(noBattery.State == CheckState.Information && noBattery.Detail.Contains("no Win32_Battery devices"),
            "An empty battery provider result remains a scoped desktop-compatible observation");
        const string battery = "{\"batteries\":[{\"DeviceID\":\"BAT0\",\"Name\":\"Fixture Battery\",\"EstimatedChargeRemaining\":78,\"BatteryStatus\":3,\"EstimatedRunTime\":120,\"DesignCapacity\":50000,\"FullChargeCapacity\":40000}]}";
        var batteryFinding = VitalsDiagnostics.Interpret(new(0, battery, "", false))[4];
        Require(batteryFinding.State == CheckState.Information && batteryFinding.Detail.Contains("78% charge") &&
            batteryFinding.Detail.Contains("20.0% capacity loss reported") && batteryFinding.Detail.Contains("estimated runtime 2h 0m") && batteryFinding.Repair is null,
            "Battery identity, charge, status and coherent capacity evidence remain read-only");
        string criticalBattery = battery.Replace(":78", ":5").Replace("\"BatteryStatus\":3", "\"BatteryStatus\":5");
        Require(VitalsDiagnostics.Interpret(new(0, criticalBattery, "", false))[4].State == CheckState.Attention,
            "Critical battery status and low charge request review without selecting a repair");
        foreach (string invalidBattery in new[] {
            battery.Replace(":78", ":101"), battery.Replace("Fixture Battery", "bad\\nname"),
            battery.Replace("\"BatteryStatus\":3", "\"BatteryStatus\":12"), battery.Replace("120", "10081"),
            battery.Replace("}]", "},{\"DeviceID\":\"BAT0\",\"Name\":\"Duplicate\",\"EstimatedChargeRemaining\":50,\"BatteryStatus\":3,\"EstimatedRunTime\":60}]") })
            Require(VitalsDiagnostics.Interpret(new(0, invalidBattery, "", false))[4].State == CheckState.Unknown,
                "Malformed or duplicate battery evidence stays unknown");
        string unknownRuntime = battery.Replace("120", "71582788");
        Require(VitalsDiagnostics.Interpret(new(0, unknownRuntime, "", false))[4].Detail.Contains("estimated runtime unavailable"),
            "Windows unknown-runtime sentinel remains explicit without invalidating battery evidence");
        const string hardware = "{\"hardware\":{\"manufacturer\":\"Fixture OEM\",\"model\":\"Model 1\",\"domain\":\"CONTOSO\",\"domainRole\":1,\"installedMemory\":34359738368,\"biosManufacturer\":\"Fixture BIOS\",\"biosVersion\":\"1.2.3\",\"serial\":\"ABC123\"}}";
        var hardwareFinding = VitalsDiagnostics.Interpret(new(0, hardware, "", false))[5];
        Require(hardwareFinding.State == CheckState.Information && hardwareFinding.Detail.Contains("Fixture OEM Model 1") &&
            hardwareFinding.Detail.Contains("32.0 GiB") && hardwareFinding.Detail.Contains("member workstation in CONTOSO") &&
            hardwareFinding.Detail.Contains("Fixture BIOS 1.2.3") && hardwareFinding.Detail.Contains("serial ABC123") && hardwareFinding.Repair is null,
            "System and firmware identities remain bounded informational evidence");
        var unavailableSerial = VitalsDiagnostics.Interpret(new(0, hardware.Replace("\"ABC123\"", "null"), "", false))[5];
        Require(unavailableSerial.State == CheckState.Information && unavailableSerial.Detail.Contains("serial unavailable"),
            "Unavailable placeholder serial remains explicit without invalidating independent system identity");
        foreach (string invalidHardware in new[] {
            hardware.Replace("\"domainRole\":1", "\"domainRole\":6"), hardware.Replace("Fixture OEM", "bad\\nOEM"),
            hardware.Replace("34359738368", "0"), hardware.Replace("\"serial\":\"ABC123\"", "\"serial\":\"ABC123\",\"serial\":\"OTHER\"") })
            Require(VitalsDiagnostics.Interpret(new(0, invalidHardware, "", false))[5].State == CheckState.Unknown,
                "Malformed, duplicate or contradictory system identity stays unknown");
        const string administrators = "{\"administrators\":[{\"name\":\"Local Admin\",\"adsPath\":\"WinNT://FIXTURE/Local Admin\",\"class\":\"User\"},{\"name\":\"Support Team\",\"adsPath\":\"WinNT://CONTOSO/Support Team\",\"class\":\"Group\"}]}";
        var administratorFinding = VitalsDiagnostics.Interpret(new(0, administrators, "", false))[6];
        Require(administratorFinding.State == CheckState.Information && administratorFinding.Detail.Contains("2 local Administrators members evaluated") &&
            administratorFinding.Detail.Contains("Local Admin (User)") && administratorFinding.Detail.Contains("Support Team (Group)") && administratorFinding.Repair is null,
            "Local Administrators membership is bounded informational inventory without a repair");
        var emptyAdministrators = VitalsDiagnostics.Interpret(new(0, "{\"administrators\":[]}", "", false))[6];
        Require(emptyAdministrators.State == CheckState.Information && emptyAdministrators.Detail.Contains("No members were reported"),
            "An empty Administrators result remains a scoped inventory observation");
        foreach (string invalidAdministrator in new[] {
            administrators.Replace("WinNT://FIXTURE/Local Admin", "bad\\npath"), administrators.Replace("\"User\"", "\"Computer\""),
            administrators.Replace("\"class\":\"Group\"", "\"class\":\"Group\",\"class\":\"User\""),
            administrators.Replace("WinNT://CONTOSO/Support Team", "WinNT://FIXTURE/Local Admin") })
            Require(VitalsDiagnostics.Interpret(new(0, invalidAdministrator, "", false))[6].State == CheckState.Unknown,
                "Malformed or duplicate local Administrators evidence stays unknown");
        var manyAdministrators = Enumerable.Range(0, 257).Select(i => new { name = "Member" + i, adsPath = "WinNT://FIXTURE/Member" + i, @class = "User" });
        string oversizedAdministrators = System.Text.Json.JsonSerializer.Serialize(new { administrators = manyAdministrators });
        var boundedAdministrators = VitalsDiagnostics.Interpret(new(0, oversizedAdministrators, "", false))[6];
        Require(boundedAdministrators.State == CheckState.Unknown && boundedAdministrators.Detail.Contains("256 local Administrators members evaluated") &&
            boundedAdministrators.Detail.Contains("Partial coverage") && boundedAdministrators.Detail.Contains("first 32 recognized names"),
            "Oversized local Administrators inventory is bounded and reports partial coverage");
        const string windows = "{\"memory\":{\"caption\":\"Microsoft Windows 11 Pro\",\"version\":\"10.0.26200\",\"build\":\"26200\",\"architecture\":\"64-bit\"}}";
        var windowsFinding = VitalsDiagnostics.Interpret(new(0, windows, "", false))[7];
        Require(windowsFinding.State == CheckState.Information && windowsFinding.Detail.Contains("Microsoft Windows 11 Pro") &&
            windowsFinding.Detail.Contains("version 10.0.26200") && windowsFinding.Detail.Contains("build 26200") &&
            windowsFinding.Detail.Contains("64-bit") && windowsFinding.Repair is null,
            "Windows version identity is bounded informational evidence without a compliance claim");
        foreach (string invalidWindows in new[] { windows.Replace("26200\"", "bad\""), windows.Replace("64-bit", "bad\\narchitecture"),
            windows.Replace("\"build\":\"26200\"", "\"build\":\"26200\",\"build\":\"26100\"") })
            Require(VitalsDiagnostics.Interpret(new(0, invalidWindows, "", false))[7].State == CheckState.Unknown,
                "Malformed or duplicate Windows version evidence stays unknown");
        const string graphics = "{\"graphics\":[{\"PNPDeviceID\":\"PCI\\\\VEN_1234&DEV_5678\",\"Name\":\"Fixture GPU\",\"AdapterRAM\":8589934592,\"DriverVersion\":\"32.1.2.3\"}]}";
        var graphicsFinding = VitalsDiagnostics.Interpret(new(0, graphics, "", false))[8];
        Require(graphicsFinding.State == CheckState.Information && graphicsFinding.Detail.Contains("Fixture GPU") &&
            graphicsFinding.Detail.Contains("8.0 GiB") && graphicsFinding.Detail.Contains("32.1.2.3") && graphicsFinding.Repair is null,
            "Graphics identity, reported memory and driver version remain read-only inventory");
        foreach (string invalidGraphics in new[] { graphics.Replace("8589934592", "0"), graphics.Replace("Fixture GPU", "bad\\nGPU"),
            graphics.Replace("}]", "},{\"PNPDeviceID\":\"PCI\\\\VEN_1234&DEV_5678\",\"Name\":\"Duplicate\",\"AdapterRAM\":1,\"DriverVersion\":\"1\"}]") })
            Require(VitalsDiagnostics.Interpret(new(0, invalidGraphics, "", false))[8].State == CheckState.Unknown,
                "Malformed or duplicate graphics evidence stays unknown");
        const string modules = "{\"memoryModules\":[{\"Tag\":\"Physical Memory 0\",\"DeviceLocator\":\"DIMM_A1\",\"Capacity\":17179869184,\"Speed\":3600,\"ConfiguredClockSpeed\":3200,\"Manufacturer\":\"Fixture RAM\",\"PartNumber\":\"PART-1\"}]}";
        var modulesFinding = VitalsDiagnostics.Interpret(new(0, modules, "", false))[9];
        Require(modulesFinding.State == CheckState.Information && modulesFinding.Detail.Contains("16.0 GiB") && modulesFinding.Detail.Contains("3200 MHz configured") &&
            modulesFinding.Detail.Contains("3600 MHz rated") && modulesFinding.Detail.Contains("Fixture RAM PART-1") && modulesFinding.Repair is null,
            "Physical memory module capacity and speed remain read-only provider evidence");
        foreach (string invalidModule in new[] { modules.Replace("17179869184", "0"), modules.Replace("3200", "25000"),
            modules.Replace("PART-1", "bad\\npart"), modules.Replace("}]", "},{\"Tag\":\"Physical Memory 0\",\"DeviceLocator\":\"DIMM_B1\",\"Capacity\":1,\"Speed\":1,\"ConfiguredClockSpeed\":1,\"Manufacturer\":\"X\",\"PartNumber\":\"Y\"}]") })
            Require(VitalsDiagnostics.Interpret(new(0, invalidModule, "", false))[9].State == CheckState.Unknown,
                "Malformed or duplicate physical memory module evidence stays unknown");
        foreach (string payload in new[] { "{}", "{\"memory\":null,\"disks\":[]}", "{\"memory\":{\"total\":1,\"free\":2},\"disks\":[null]}", "not json" })
            Require(VitalsDiagnostics.Interpret(new(0, payload, "", false)).All(c => c.State == CheckState.Unknown), "Unavailable and malformed state never claims healthy");
        Require(VitalsDiagnostics.Interpret(new(0, "{\"disks\":[0]}", "", false))[1].State == CheckState.Clear, "Independent disk evidence survives missing memory data");
        foreach (string invalid in new[] { "null", "true", "\"unknown\"", "{}", "[]", "-1", "1.5" })
        foreach (string field in new[] { "total", "free" })
        {
            string payload = "{\"memory\":{\"total\":10000,\"free\":5000},\"disks\":[2]}";
            payload = payload.Replace("\"" + field + "\":" + (field == "total" ? "10000" : "5000"), "\"" + field + "\":" + invalid);
            var partial = VitalsDiagnostics.Interpret(new(0, payload, "", false));
            Require(partial[0].State == CheckState.Unknown && partial[1].State == CheckState.Attention && partial.All(c => c.Repair is null),
                "Malformed memory values cannot hide independent unhealthy disk evidence");
        }
        foreach (var failure in new[] { new ToolResult(5, "{}", "", false), new ToolResult(0, "{}", "", true), new ToolResult(0, "{}", "", false) { TimedOut = true } })
            Require(VitalsDiagnostics.Interpret(failure).All(c => c.State == CheckState.Unknown), "Failed collection cannot pass");
        Console.WriteLine("PASS: vitals memory pressure, uptime, processor, battery, system, Windows, graphics, memory-module and local Administrators inventory, disk-health states, unavailable providers and malformed snapshots.");
    }
    private static void Require(bool value, string message) { if (!value) throw new Exception(message); }
}
