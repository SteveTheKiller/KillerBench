using System.Text.Json;

namespace KillerBench.Engine.SystemHealth;

public static class VitalsDiagnostics
{
    private const string Script = "$ErrorActionPreference='Stop'; $ProgressPreference='SilentlyContinue'; $m=$null; $d=$null; $p=$null; $b=$null; $h=$null; $a=$null; $g=$null; $r=$null; $me=$null; $de=$null; $pe=$null; $be=$null; $he=$null; $ae=$null; $ge=$null; $re=$null; " +
        "try { $os=Get-CimInstance -ClassName Win32_OperatingSystem -OperationTimeoutSec 15; $m=[pscustomobject]@{total=$os.TotalVisibleMemorySize; free=$os.FreePhysicalMemory; lastBoot=$os.LastBootUpTime.ToUniversalTime().ToString('o'); captured=(Get-Date).ToUniversalTime().ToString('o'); caption=[string]$os.Caption; version=[string]$os.Version; build=[string]$os.BuildNumber; architecture=[string]$os.OSArchitecture} } catch { $me=$_.Exception.Message } " +
        "try { $d=@(Get-CimInstance -Namespace root/Microsoft/Windows/Storage -ClassName MSFT_PhysicalDisk -OperationTimeoutSec 15 | Select-Object -First 257 DeviceId,FriendlyName,HealthStatus) } catch { $de=$_.Exception.Message } " +
        "try { $p=@(Get-CimInstance -ClassName Win32_Processor -OperationTimeoutSec 15 | Select-Object -First 65 DeviceID,Name,LoadPercentage,NumberOfCores,NumberOfLogicalProcessors,CurrentClockSpeed) } catch { $pe=$_.Exception.Message } " +
        "try { $b=@(Get-CimInstance -ClassName Win32_Battery -OperationTimeoutSec 15 | Select-Object -First 17 DeviceID,Name,EstimatedChargeRemaining,BatteryStatus,EstimatedRunTime,DesignCapacity,FullChargeCapacity) } catch { $be=$_.Exception.Message } " +
        "try { $cs=Get-CimInstance -ClassName Win32_ComputerSystem -OperationTimeoutSec 10 | Select-Object -First 1; $bios=Get-CimInstance -ClassName Win32_BIOS -OperationTimeoutSec 10 | Select-Object -First 1; $board=Get-CimInstance -ClassName Win32_BaseBoard -OperationTimeoutSec 10 | Select-Object -First 1; $make=[string]$cs.Manufacturer; $model=[string]$cs.Model; $serial=[string]$bios.SerialNumber; if ([string]::IsNullOrWhiteSpace($make) -or $make -match '^(To Be Filled|Default string|System manufacturer)') { $make=[string]$board.Manufacturer }; if ([string]::IsNullOrWhiteSpace($model) -or $model -match '^(To Be Filled|Default string|System Product Name)') { $model=[string]$board.Product }; if ([string]::IsNullOrWhiteSpace($serial) -or $serial -match '^(To Be Filled|Default string|System Serial Number)') { $serial=$null }; $h=[pscustomobject]@{manufacturer=$make; model=$model; domain=[string]$cs.Domain; domainRole=$cs.DomainRole; installedMemory=$cs.TotalPhysicalMemory; biosManufacturer=[string]$bios.Manufacturer; biosVersion=[string]$bios.SMBIOSBIOSVersion; serial=$serial} } catch { $he=$_.Exception.Message } " +
        "try { $ag=@(Get-CimInstance -ClassName Win32_Group -Filter \"LocalAccount=True AND SID='S-1-5-32-544'\" -OperationTimeoutSec 10 | Select-Object -First 2); if ($ag.Count -ne 1) { throw 'The built-in Administrators group identity was not unique.' }; $group=[ADSI]('WinNT://./'+[string]$ag[0].Name+',group'); $a=@(); $count=0; foreach ($member in @($group.psbase.Invoke('Members'))) { if ($count -ge 257) { break }; $type=$member.GetType(); $a += [pscustomobject]@{name=[string]$type.InvokeMember('Name','GetProperty',$null,$member,$null); adsPath=[string]$type.InvokeMember('AdsPath','GetProperty',$null,$member,$null); class=[string]$type.InvokeMember('Class','GetProperty',$null,$member,$null)}; $count++ } } catch { $ae=$_.Exception.Message } " +
        "try { $g=@(Get-CimInstance -ClassName Win32_VideoController -OperationTimeoutSec 10 | Select-Object -First 17 PNPDeviceID,Name,AdapterRAM,DriverVersion) } catch { $ge=$_.Exception.Message } " +
        "try { $r=@(Get-CimInstance -ClassName Win32_PhysicalMemory -OperationTimeoutSec 10 | Select-Object -First 65 Tag,DeviceLocator,Capacity,Speed,ConfiguredClockSpeed,Manufacturer,PartNumber) } catch { $re=$_.Exception.Message } " +
        "[pscustomobject]@{memory=$m; disks=$d; processors=$p; batteries=$b; hardware=$h; administrators=$a; graphics=$g; memoryModules=$r; memoryError=$me; diskError=$de; processorError=$pe; batteryError=$be; hardwareError=$he; administratorError=$ae; graphicsError=$ge; memoryModuleError=$re} | ConvertTo-Json -Depth 4 -Compress";

    public static async Task<IReadOnlyList<WorkflowCheck>> CheckAsync(CancellationToken token = default)
    {
        var step = new RepairStep(PrintRecovery.Tool, ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", Script], true);
        return Interpret(await BoundedTool.RunAsync(step, TimeSpan.FromSeconds(45), token: token).ConfigureAwait(false));
    }

    public static IReadOnlyList<WorkflowCheck> Interpret(ToolResult result)
    {
        WorkflowCheck Unknown(string name) => new(name, CheckState.Unknown, "Windows did not provide a complete, recognized snapshot. No repair selected.", Evidence: result);
        IReadOnlyList<WorkflowCheck> UnknownSet() => [Unknown("Memory snapshot"), Unknown("Physical disk health"), Unknown("System uptime"),
            Unknown("Processor snapshot"), Unknown("Battery snapshot"), Unknown("System identity"), Unknown("Local Administrators"), Unknown("Windows version"),
            Unknown("Graphics adapters"), Unknown("Physical memory modules")];
        if (result.ExitCode != 0 || result.TimedOut || result.Truncated || !string.IsNullOrWhiteSpace(result.Error))
            return UnknownSet();
        try
        {
            using var json = JsonDocument.Parse(result.Output);
            var memory = Unknown("Memory snapshot");
            var disks = Unknown("Physical disk health");
            var uptime = Unknown("System uptime");
            var processors = Unknown("Processor snapshot");
            var batteries = Unknown("Battery snapshot");
            var hardware = Unknown("System identity");
            var administrators = Unknown("Local Administrators");
            var windows = Unknown("Windows version");
            var graphics = InterpretGraphics(json.RootElement, result);
            var memoryModules = InterpretMemoryModules(json.RootElement, result);
            if (json.RootElement.TryGetProperty("memory", out var m) && m.ValueKind == JsonValueKind.Object &&
                m.TryGetProperty("total", out var t) && t.ValueKind == JsonValueKind.Number && t.TryGetUInt64(out ulong total) && total > 0 &&
                m.TryGetProperty("free", out var f) && f.ValueKind == JsonValueKind.Number && f.TryGetUInt64(out ulong free) && free <= total)
            {
                double percent = free * 100d / total;
                memory = new("Memory snapshot", percent < 5 ? CheckState.Attention : CheckState.Information,
                    $"{free / 1048576d:F1} GiB available of {total / 1048576d:F1} GiB visible to Windows ({percent:F1}%). " +
                    (percent < 5 ? "Less than 5% available at capture time. Review sustained usage before changing applications or paging settings." : "Single snapshot; this does not test memory hardware or establish sustained pressure."), Evidence: result);
            }
            if (m.ValueKind == JsonValueKind.Object &&
                m.TryGetProperty("lastBoot", out var boot) && boot.ValueKind == JsonValueKind.String &&
                m.TryGetProperty("captured", out var captured) && captured.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParseExact(boot.GetString(), "O", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var bootUtc) &&
                DateTimeOffset.TryParseExact(captured.GetString(), "O", System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var capturedUtc) &&
                bootUtc.Offset == TimeSpan.Zero && capturedUtc.Offset == TimeSpan.Zero && bootUtc <= capturedUtc &&
                capturedUtc - bootUtc <= TimeSpan.FromDays(3650))
            {
                var age = capturedUtc - bootUtc;
                bool longRunning = age > TimeSpan.FromDays(7);
                uptime = new("System uptime", longRunning ? CheckState.Attention : CheckState.Information,
                    $"Last boot: {bootUtc:yyyy-MM-dd HH:mm:ss} UTC. Uptime at capture: {(int)age.TotalDays}d {age.Hours}h {age.Minutes}m. " +
                    (longRunning ? "More than seven days since startup. Review pending maintenance and restart requirements; uptime alone is not a fault." :
                        "This timestamp does not establish restart health or pending maintenance."), Evidence: result);
            }
            if (m.ValueKind == JsonValueKind.Object &&
                Unique(m, "caption", out var caption) && caption.ValueKind == JsonValueKind.String && DriverReference.ValidText(caption.GetString(), 256) &&
                Unique(m, "version", out var versionValue) && versionValue.ValueKind == JsonValueKind.String && Version.TryParse(versionValue.GetString(), out var version) &&
                Unique(m, "build", out var buildValue) && buildValue.ValueKind == JsonValueKind.String && int.TryParse(buildValue.GetString(), out int build) && build is > 0 and <= 999999 &&
                Unique(m, "architecture", out var architecture) && architecture.ValueKind == JsonValueKind.String && DriverReference.ValidText(architecture.GetString(), 64))
                windows = new("Windows version", CheckState.Information,
                    $"{caption.GetString()!.Trim()}; version {version}; build {build}; {architecture.GetString()!.Trim()}. " +
                    "Reported version identity does not establish update compliance, support status or licensing.", Evidence: result);
            if (json.RootElement.TryGetProperty("disks", out var d) && d.ValueKind == JsonValueKind.Array && d.GetArrayLength() > 0)
            {
                int warning = 0, unknown = 0;
                bool limited = d.GetArrayLength() > 256;
                var affected = new List<string>();
                var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var disk in d.EnumerateArray().Take(256))
                {
                    var value = disk;
                    string? identity = null;
                    if (disk.ValueKind == JsonValueKind.Object)
                    {
                        if (!disk.TryGetProperty("DeviceId", out var id) || id.ValueKind != JsonValueKind.String || !DriverReference.ValidText(id.GetString(), 256) ||
                            !identities.Add(id.GetString()!) || !disk.TryGetProperty("FriendlyName", out var name) || name.ValueKind != JsonValueKind.String ||
                            !DriverReference.ValidText(name.GetString(), 256) || !disk.TryGetProperty("HealthStatus", out value))
                        { unknown++; continue; }
                        identity = name.GetString() + " (device " + id.GetString() + ")";
                    }
                    if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int health) || health is not (0 or 1 or 2)) unknown++;
                    else if (health is 1 or 2)
                    {
                        warning++;
                        if (identity is not null) affected.Add(identity + (health == 1 ? ": warning" : ": unhealthy"));
                    }
                }
                disks = new("Physical disk health", warning > 0 ? CheckState.Attention : unknown > 0 || limited ? CheckState.Unknown : CheckState.Clear,
                    $"{Math.Min(d.GetArrayLength(), 256)} evaluated provider-reported disks: {warning} warning/unhealthy, {unknown} unknown. " +
                    (limited ? "Partial coverage: only the first 256 disks were evaluated. Additional disks remain unassessed. " : "") +
                    (warning > 0 ? "Confirm backups and investigate the affected hardware before repair writes. " : "") +
                    (affected.Count > 0 ? "Affected disks: " + string.Join("; ", affected) + ". " : "") +
                    "Provider health does not replace a surface test, SMART review or backup verification.", Evidence: result);
            }
            if (json.RootElement.TryGetProperty("processors", out var p) && p.ValueKind == JsonValueKind.Array && p.GetArrayLength() > 0)
            {
                bool limited = p.GetArrayLength() > 64;
                int unknown = 0, high = 0;
                var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var details = new List<string>();
                foreach (var processor in p.EnumerateArray().Take(64))
                {
                    if (processor.ValueKind != JsonValueKind.Object ||
                        !processor.TryGetProperty("DeviceID", out var id) || id.ValueKind != JsonValueKind.String || !DriverReference.ValidText(id.GetString(), 128) || !identities.Add(id.GetString()!) ||
                        !processor.TryGetProperty("Name", out var name) || name.ValueKind != JsonValueKind.String || !DriverReference.ValidText(name.GetString(), 256) ||
                        !processor.TryGetProperty("LoadPercentage", out var loadValue) || loadValue.ValueKind != JsonValueKind.Number || !loadValue.TryGetInt32(out int load) || load is < 0 or > 100 ||
                        !processor.TryGetProperty("NumberOfCores", out var coreValue) || coreValue.ValueKind != JsonValueKind.Number || !coreValue.TryGetInt32(out int cores) || cores is < 1 or > 4096 ||
                        !processor.TryGetProperty("NumberOfLogicalProcessors", out var logicalValue) || logicalValue.ValueKind != JsonValueKind.Number || !logicalValue.TryGetInt32(out int logical) || logical < cores || logical > 8192 ||
                        !processor.TryGetProperty("CurrentClockSpeed", out var clockValue) || clockValue.ValueKind != JsonValueKind.Number || !clockValue.TryGetInt32(out int clock) || clock is < 1 or > 100000)
                    { unknown++; continue; }
                    if (load >= 95) high++;
                    details.Add($"{name.GetString()!.Trim()} ({id.GetString()}): {cores} cores, {logical} logical, {clock} MHz current clock, {load}% load");
                }
                processors = new("Processor snapshot", unknown > 0 || limited ? CheckState.Unknown : CheckState.Information,
                    $"{Math.Min(p.GetArrayLength(), 64)} processors evaluated: {high} at or above 95% load, {unknown} unknown. " +
                    (limited ? "Partial coverage: only the first 64 processors were evaluated. " : "") +
                    string.Join("; ", details) + (details.Count > 0 ? ". " : "") +
                    "Load is a single provider snapshot and does not establish sustained CPU pressure or processor health.", Evidence: result);
            }
            if (json.RootElement.TryGetProperty("batteries", out var b) && b.ValueKind == JsonValueKind.Array)
            {
                if (b.GetArrayLength() == 0)
                    batteries = new("Battery snapshot", CheckState.Information,
                        "Windows reports no Win32_Battery devices. This is normal for many desktop systems and does not exclude an external UPS.", Evidence: result);
                else
                {
                    bool limited = b.GetArrayLength() > 16;
                    int unknown = 0, attention = 0;
                    var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var details = new List<string>();
                    foreach (var battery in b.EnumerateArray().Take(16))
                    {
                        if (battery.ValueKind != JsonValueKind.Object ||
                            !battery.TryGetProperty("DeviceID", out var id) || id.ValueKind != JsonValueKind.String || !DriverReference.ValidText(id.GetString(), 256) || !identities.Add(id.GetString()!) ||
                            !battery.TryGetProperty("Name", out var name) || name.ValueKind != JsonValueKind.String || !DriverReference.ValidText(name.GetString(), 256) ||
                            !battery.TryGetProperty("EstimatedChargeRemaining", out var chargeValue) || chargeValue.ValueKind != JsonValueKind.Number || !chargeValue.TryGetInt32(out int charge) || charge is < 0 or > 100 ||
                            !battery.TryGetProperty("BatteryStatus", out var statusValue) || statusValue.ValueKind != JsonValueKind.Number || !statusValue.TryGetInt32(out int status) || status is < 1 or > 11 ||
                            !battery.TryGetProperty("EstimatedRunTime", out var runtimeValue) || runtimeValue.ValueKind is not (JsonValueKind.Number or JsonValueKind.Null) ||
                            runtimeValue.ValueKind == JsonValueKind.Number && (!runtimeValue.TryGetUInt32(out uint runtime) || runtime > 10080 && runtime != 71582788))
                        { unknown++; continue; }
                        bool needsAttention = charge < 10 || status is 4 or 5 or 8 or 9;
                        if (needsAttention) attention++;
                        string capacity = "capacity wear unavailable";
                        if (battery.TryGetProperty("DesignCapacity", out var designValue) && designValue.ValueKind == JsonValueKind.Number && designValue.TryGetUInt64(out ulong design) && design > 0 &&
                            battery.TryGetProperty("FullChargeCapacity", out var fullValue) && fullValue.ValueKind == JsonValueKind.Number && fullValue.TryGetUInt64(out ulong full) && full <= design)
                            capacity = $"{(1d - full / (double)design) * 100d:F1}% capacity loss reported";
                        string runtimeText = runtimeValue.ValueKind == JsonValueKind.Number && runtimeValue.GetUInt32() is > 0 and not 71582788
                            ? $", estimated runtime {runtimeValue.GetUInt32() / 60}h {runtimeValue.GetUInt32() % 60}m" : ", estimated runtime unavailable";
                        details.Add($"{name.GetString()!.Trim()} ({id.GetString()}): {charge}% charge, status code {status}, {capacity}{runtimeText}");
                    }
                    batteries = new("Battery snapshot", attention > 0 ? CheckState.Attention : unknown > 0 || limited ? CheckState.Unknown : CheckState.Information,
                        $"{Math.Min(b.GetArrayLength(), 16)} batteries evaluated: {attention} need charge/status review, {unknown} unknown. " +
                        (limited ? "Partial coverage: only the first 16 batteries were evaluated. " : "") +
                        string.Join("; ", details) + (details.Count > 0 ? ". " : "") +
                        "Provider charge, status and capacity are point-in-time reports; they do not establish runtime or cell health.", Evidence: result);
                }
            }
            if (json.RootElement.TryGetProperty("hardware", out var h) && h.ValueKind == JsonValueKind.Object &&
                Unique(h, "manufacturer", out var manufacturer) && manufacturer.ValueKind == JsonValueKind.String && DriverReference.ValidText(manufacturer.GetString(), 256) &&
                Unique(h, "model", out var model) && model.ValueKind == JsonValueKind.String && DriverReference.ValidText(model.GetString(), 256) &&
                Unique(h, "domain", out var domain) && domain.ValueKind == JsonValueKind.String && DriverReference.ValidText(domain.GetString(), 256) &&
                Unique(h, "domainRole", out var roleValue) && roleValue.ValueKind == JsonValueKind.Number && roleValue.TryGetInt32(out int role) && role is >= 0 and <= 5 &&
                Unique(h, "installedMemory", out var installedValue) && installedValue.ValueKind == JsonValueKind.Number && installedValue.TryGetUInt64(out ulong installed) && installed > 0 &&
                Unique(h, "biosManufacturer", out var biosManufacturer) && biosManufacturer.ValueKind == JsonValueKind.String && DriverReference.ValidText(biosManufacturer.GetString(), 256) &&
                Unique(h, "biosVersion", out var biosVersion) && biosVersion.ValueKind == JsonValueKind.String && DriverReference.ValidText(biosVersion.GetString(), 256) &&
                Unique(h, "serial", out var serial) && (serial.ValueKind == JsonValueKind.Null ||
                    serial.ValueKind == JsonValueKind.String && DriverReference.ValidText(serial.GetString(), 256)))
            {
                string roleText = role switch
                {
                    0 => "standalone workstation", 1 => "member workstation", 2 => "standalone server",
                    3 => "member server", 4 => "backup domain controller", 5 => "primary domain controller", _ => "unknown"
                };
                string serialText = serial.ValueKind == JsonValueKind.String ? serial.GetString()!.Trim() : "unavailable";
                hardware = new("System identity", CheckState.Information,
                    $"{manufacturer.GetString()!.Trim()} {model.GetString()!.Trim()}; serial {serialText}; " +
                    $"{installed / 1073741824d:F1} GiB installed memory; {roleText} in {domain.GetString()!.Trim()}. " +
                    $"BIOS: {biosManufacturer.GetString()!.Trim()} {biosVersion.GetString()!.Trim()}. " +
                    "Provider identity does not establish warranty, firmware currency, ownership or hardware health.", Evidence: result);
            }
            if (json.RootElement.TryGetProperty("administrators", out var a) && a.ValueKind == JsonValueKind.Array)
            {
                bool limited = a.GetArrayLength() > 256;
                int unknown = 0;
                var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var members = new List<string>();
                foreach (var member in a.EnumerateArray().Take(256))
                {
                    if (member.ValueKind != JsonValueKind.Object ||
                        !Unique(member, "name", out var name) || name.ValueKind != JsonValueKind.String || !DriverReference.ValidText(name.GetString(), 256) ||
                        !Unique(member, "adsPath", out var path) || path.ValueKind != JsonValueKind.String || !DriverReference.ValidText(path.GetString(), 1024) ||
                        !Unique(member, "class", out var memberClass) || memberClass.ValueKind != JsonValueKind.String ||
                        memberClass.GetString() is not ("User" or "Group") || !identities.Add(path.GetString()!))
                    { unknown++; continue; }
                    if (members.Count < 32) members.Add(name.GetString()!.Trim() + " (" + memberClass.GetString() + ")");
                }
                administrators = new("Local Administrators", unknown > 0 || limited ? CheckState.Unknown : CheckState.Information,
                    $"{Math.Min(a.GetArrayLength(), 256)} local Administrators members evaluated: {unknown} unknown. " +
                    (limited ? "Partial coverage: only the first 256 members were evaluated. " : "") +
                    (members.Count > 0 ? "Members: " + string.Join("; ", members) + ". " : "No members were reported. ") +
                    (a.GetArrayLength() > members.Count && members.Count == 32 ? "Only the first 32 recognized names are displayed. " : "") +
                    "Membership is inventory only; it does not establish that an account is active, safe, authorized or still needed.", Evidence: result);
            }
            WorkflowCheck ApplyProviderError(WorkflowCheck check, string property)
            {
                if (!json.RootElement.TryGetProperty(property, out var error) || error.ValueKind == JsonValueKind.Null) return check;
                string reason = error.ValueKind == JsonValueKind.String ? error.GetString() ?? "" : "Invalid provider error data.";
                reason = new string([.. reason.Take(512).Select(c => char.IsControl(c) ? ' ' : c)]);
                return Unknown(check.Name) with { Detail = "Provider collection failed. " + reason + " Partial results are unverified. No repair selected." };
            }
            var memoryResult = ApplyProviderError(memory, "memoryError");
            return [memoryResult, ApplyProviderError(disks, "diskError"),
                memoryResult.State == CheckState.Unknown && json.RootElement.TryGetProperty("memoryError", out var memoryError) && memoryError.ValueKind != JsonValueKind.Null
                    ? ApplyProviderError(uptime, "memoryError") : uptime,
                ApplyProviderError(processors, "processorError"), ApplyProviderError(batteries, "batteryError"),
                ApplyProviderError(hardware, "hardwareError"), ApplyProviderError(administrators, "administratorError"),
                ApplyProviderError(windows, "memoryError"), ApplyProviderError(graphics, "graphicsError"),
                ApplyProviderError(memoryModules, "memoryModuleError")];
        }
        catch (Exception e) when (e is JsonException or InvalidOperationException or FormatException)
        { return UnknownSet(); }
    }

    private static bool Unique(JsonElement source, string name, out JsonElement value)
    {
        value = default;
        int matches = 0;
        foreach (var property in source.EnumerateObject())
            if (property.NameEquals(name)) { value = property.Value; matches++; }
        return matches == 1;
    }

    private static WorkflowCheck InterpretGraphics(JsonElement root, ToolResult evidence)
    {
        WorkflowCheck Unknown() => new("Graphics adapters", CheckState.Unknown, "Windows did not provide a complete, recognized graphics snapshot. No repair selected.", Evidence: evidence);
        if (!root.TryGetProperty("graphics", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0) return Unknown();
        bool limited = items.GetArrayLength() > 16;
        int unknown = 0;
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var details = new List<string>();
        foreach (var item in items.EnumerateArray().Take(16))
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !Unique(item, "PNPDeviceID", out var id) || id.ValueKind != JsonValueKind.String || !DriverReference.ValidText(id.GetString(), 512) || !identities.Add(id.GetString()!) ||
                !Unique(item, "Name", out var name) || name.ValueKind != JsonValueKind.String || !DriverReference.ValidText(name.GetString(), 256) ||
                !Unique(item, "AdapterRAM", out var ram) || ram.ValueKind is not (JsonValueKind.Number or JsonValueKind.Null) ||
                ram.ValueKind == JsonValueKind.Number && (!ram.TryGetUInt64(out ulong bytes) || bytes == 0) ||
                !Unique(item, "DriverVersion", out var driver) || driver.ValueKind != JsonValueKind.String || !DriverReference.ValidText(driver.GetString(), 128))
            { unknown++; continue; }
            string capacity = ram.ValueKind == JsonValueKind.Number ? $", {ram.GetUInt64() / 1073741824d:F1} GiB reported adapter memory" : ", adapter memory unavailable";
            details.Add($"{name.GetString()!.Trim()} ({driver.GetString()!.Trim()}{capacity})");
        }
        return new("Graphics adapters", unknown > 0 || limited ? CheckState.Unknown : CheckState.Information,
            $"{Math.Min(items.GetArrayLength(), 16)} graphics adapters evaluated: {unknown} unknown. " +
            (limited ? "Partial coverage: only the first 16 adapters were evaluated. " : "") + string.Join("; ", details) + ". " +
            "Provider memory and driver identity do not establish graphics performance, stability or driver currency.", Evidence: evidence);
    }

    private static WorkflowCheck InterpretMemoryModules(JsonElement root, ToolResult evidence)
    {
        WorkflowCheck Unknown() => new("Physical memory modules", CheckState.Unknown, "Windows did not provide a complete, recognized physical-memory snapshot. No repair selected.", Evidence: evidence);
        if (!root.TryGetProperty("memoryModules", out var items) || items.ValueKind != JsonValueKind.Array || items.GetArrayLength() == 0) return Unknown();
        bool limited = items.GetArrayLength() > 64;
        int unknown = 0;
        ulong capacity = 0;
        var identities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var details = new List<string>();
        foreach (var item in items.EnumerateArray().Take(64))
        {
            if (item.ValueKind != JsonValueKind.Object ||
                !Unique(item, "Tag", out var tag) || tag.ValueKind != JsonValueKind.String || !DriverReference.ValidText(tag.GetString(), 128) || !identities.Add(tag.GetString()!) ||
                !Unique(item, "DeviceLocator", out var locator) || locator.ValueKind != JsonValueKind.String || !DriverReference.ValidText(locator.GetString(), 128) ||
                !Unique(item, "Capacity", out var moduleCapacity) || moduleCapacity.ValueKind != JsonValueKind.Number || !moduleCapacity.TryGetUInt64(out ulong bytes) || bytes == 0 ||
                !Unique(item, "Speed", out var speedValue) || speedValue.ValueKind != JsonValueKind.Number || !speedValue.TryGetUInt32(out uint speed) || speed == 0 || speed > 20000 ||
                !Unique(item, "ConfiguredClockSpeed", out var configuredValue) || configuredValue.ValueKind != JsonValueKind.Number || !configuredValue.TryGetUInt32(out uint configured) || configured == 0 || configured > 20000 ||
                !Unique(item, "Manufacturer", out var manufacturer) || manufacturer.ValueKind != JsonValueKind.String || !DriverReference.ValidText(manufacturer.GetString(), 128) ||
                !Unique(item, "PartNumber", out var part) || part.ValueKind != JsonValueKind.String || !DriverReference.ValidText(part.GetString(), 128) || ulong.MaxValue - capacity < bytes)
            { unknown++; continue; }
            capacity += bytes;
            details.Add($"{locator.GetString()!.Trim()} ({tag.GetString()!.Trim()}): {bytes / 1073741824d:F1} GiB, {configured} MHz configured ({speed} MHz rated), {manufacturer.GetString()!.Trim()} {part.GetString()!.Trim()}");
        }
        return new("Physical memory modules", unknown > 0 || limited ? CheckState.Unknown : CheckState.Information,
            $"{Math.Min(items.GetArrayLength(), 64)} memory modules evaluated: {unknown} unknown, {capacity / 1073741824d:F1} GiB recognized. " +
            (limited ? "Partial coverage: only the first 64 modules were evaluated. " : "") + string.Join("; ", details) + ". " +
            "Configured and rated speeds are firmware/provider reports; they do not establish memory stability or health.", Evidence: evidence);
    }
}
