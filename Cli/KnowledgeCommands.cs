using System.Text.Json;
using KillerBench.Engine;

internal static class KnowledgeCommands
{
    internal static async Task<int> RunAsync(string[] args)
    {
        bool setLookup = args[0] == "knowledge-set";
        bool drivers = args[0] == "driver-reference";
        bool macVendor = args[0] == "mac-vendor";
        bool pciVendor = args[0] == "pci-vendor";
        bool usbVendor = args[0] == "usb-vendor";
        bool uefiVendor = args[0] == "uefi-vendor";
        bool bluetoothCompany = args[0] == "bluetooth-company";
        bool info = args[0] == "knowledge-info";
        int build = 0;
        if ((setLookup && (args.Length < 4 || args.Length > 35)) || (!setLookup && !drivers && !macVendor && !pciVendor && !usbVendor && !uefiVendor && !bluetoothCompany && !info && args.Length != 4) || (info && args.Length != 2) || ((macVendor || pciVendor || usbVendor || bluetoothCompany) && args.Length != 3) || (uefiVendor && args.Length != 4) || (drivers && (args.Length != 6 ||
            !int.TryParse(args[5], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out build) || build <= 0)))
        { Console.Error.WriteLine(drivers ? "Usage: driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD" :
            setLookup ? "Usage: knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]" :
            macVendor ? "Usage: mac-vendor PACK_PATH MAC_ADDRESS" :
            pciVendor ? "Usage: pci-vendor PACK_PATH PCI_HARDWARE_ID" :
            usbVendor ? "Usage: usb-vendor PACK_PATH USB_HARDWARE_ID" :
            uefiVendor ? "Usage: uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID" :
            bluetoothCompany ? "Usage: bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID" :
            info ? "Usage: knowledge-info PACK_PATH" :
            args[0] == "knowledge-log" ? "Usage: knowledge-log PACK_PATH CONTEXT LOG_PATH" :
            args[0] == "knowledge-search" ? "Usage: knowledge-search PACK_PATH CONTEXT TEXT" : "Usage: knowledge PACK_PATH CONTEXT CODE"); return 2; }
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        bool userCanceled = false;
        ConsoleCancelEventHandler cancel = (_, e) => { e.Cancel = true; userCanceled = true; stop.Cancel(); };
        Console.CancelKeyPress += cancel;
        try
        {
            if (setLookup)
            {
                var catalogs = new KnowledgeCatalogCollection();
                for (int index = 3; index < args.Length; index++)
                    catalogs.AddOrReplace(await KnowledgeCatalog.ReadFileAsync(args[index], stop.Token));
                var setResolution = KnowledgeResolver.Resolve(catalogs, args[1], args[2]);
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    Packs = catalogs.Snapshot().Select(catalog => new { catalog.PackId, catalog.Revision, catalog.PublishedOn, catalog.License }),
                    Resolution = setResolution,
                    Note = setResolution.Ambiguous
                        ? "More than one active pack defines the same contextual identity. No reference was selected."
                        : "Local reference data only. Source authenticity and current applicability require review. No diagnosis or repair authorized."
                }, Capture.JsonOptions));
                return setResolution.Found ? 0 : 3;
            }
            var catalog = await KnowledgeCatalog.ReadFileAsync(args[1], stop.Token);
            if (info)
            {
                Console.WriteLine(JsonSerializer.Serialize(catalog.Summarize(stop.Token), Capture.JsonOptions));
                return 0;
            }
            if (bluetoothCompany)
            {
                var result = BluetoothCompanyLookup.Find(catalog, args[2]);
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.Match is null ? 3 : 0;
            }
            if (uefiVendor)
            {
                if (!Enum.TryParse<UefiVendorRegistry>(args[2], true, out var registry))
                    throw new ArgumentException("Registry must be PNP or ACPI.");
                var result = UefiVendorLookup.Find(catalog, registry, args[3]);
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.Match is null ? 3 : 0;
            }
            if (macVendor)
            {
                var result = MacVendorLookup.Find(catalog, args[2]);
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.Match is null ? 3 : 0;
            }
            if (pciVendor)
            {
                var result = PciHardwareLookup.Find(catalog, args[2]);
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.Vendor is null ? 3 : 0;
            }
            if (usbVendor)
            {
                var result = UsbHardwareLookup.Find(catalog, args[2]);
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(result, Capture.JsonOptions));
                return result.Vendor is null ? 3 : 0;
            }
            if (args[0] == "knowledge-search")
            {
                var result = catalog.Search(args[2], args[3], token: stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(new { catalog.PackId, catalog.Revision, catalog.PublishedOn,
                    catalog.License, result.TotalMatches, result.Truncated, result.Matches,
                    Note = "Local reference text matches only. Applicability and source authenticity require review; no diagnosis or repair authorized." }, Capture.JsonOptions));
                return result.TotalMatches == 0 ? 3 : 0;
            }
            if (args[0] == "knowledge-log")
            {
                var source = await TextEvidence.ReadAsync(args[3], stop.Token);
                var report = KnowledgeAnalysis.Analyze(source, catalog, args[2], stop.Token);
                Console.WriteLine(JsonSerializer.Serialize(report, Capture.JsonOptions));
                return 0;
            }
            if (drivers)
            {
                var matches = catalog.FindDriverReferences(args[2], args[3], args[4], build);
                stop.Token.ThrowIfCancellationRequested();
                Console.WriteLine(JsonSerializer.Serialize(new { catalog.PackId, catalog.Revision, catalog.PublishedOn,
                    catalog.License, Matches = matches,
                    Note = "Local reference metadata only; publisher authenticity and package compatibility are unverified. No download or installation performed." }, Capture.JsonOptions));
                return matches.Count == 0 ? 3 : 0;
            }
            var resolution = KnowledgeResolver.Resolve(catalog, args[2], args[3]);
            stop.Token.ThrowIfCancellationRequested();
            Console.WriteLine(JsonSerializer.Serialize(new { catalog.PackId, catalog.Revision, catalog.PublishedOn,
                catalog.License, Resolution = resolution,
                Note = "Local reference data only. Source authenticity and current applicability require review. No diagnosis or repair authorized." }, Capture.JsonOptions));
            return resolution.Found ? 0 : 3;
        }
        catch (OperationCanceledException)
        { Console.Error.WriteLine(userCanceled ? "Knowledge lookup stopped." : "Knowledge lookup timed out."); return userCanceled ? 130 : 1; }
        catch (Exception error) when (error is InvalidDataException or IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        { Console.Error.WriteLine("Knowledge lookup unavailable: " + error.Message); return 1; }
        finally { Console.CancelKeyPress -= cancel; }
    }
}
