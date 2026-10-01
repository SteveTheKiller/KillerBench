using System.Text.Json;
using KillerBench.Engine;

internal static class KnowledgeChecks
{
    internal static void Run()
    {
        var day = new DateOnly(2026, 9, 12);
        var entry = new KnowledgeEntry("fixture-a", "5", "Fixture", "Synthetic reference only", "https://example.com/reference", day, "Fixture product");
        var pack = new KnowledgePack(1, "fixture", 1, day, "Synthetic test data", [entry, entry with { Context = "fixture-b", Title = "Other context" }]);
        var catalog = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack));
        var summary = catalog.Summarize();
        if (summary.PackId != "fixture" || summary.SchemaVersion != 1 || summary.Revision != 1 ||
            summary.PublishedOn != day || summary.License != "Synthetic test data" || summary.Entries != 2 ||
            !summary.Contexts.SequenceEqual([new KnowledgeContextSummary("fixture-a", 1, 0), new KnowledgeContextSummary("fixture-b", 1, 0)]))
            throw new Exception("Knowledge pack summary lost metadata, deterministic contexts or entry counts.");
        try { catalog.Summarize(new CancellationToken(true)); throw new Exception("Canceled pack summary proceeded."); }
        catch (OperationCanceledException) { }
        var collection = new KnowledgeCatalogCollection([catalog]);
        var collected = collection.Resolve("fixture-a", "5");
        if (!collected.Found || collected.Entry != entry || collected.PackId != "fixture" ||
            collected.Revision != 1 || collected.Ambiguous || collection.Count != 1)
            throw new Exception("Multi-pack collection lost an exact contextual reference.");
        var otherCatalog = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with
        {
            Id = "other",
            Entries = [entry with { Context = "other", Code = "7" }]
        }));
        collection.AddOrReplace(otherCatalog);
        if (collection.Count != 2 || collection.Snapshot().Select(item => item.PackId).SequenceEqual(["fixture", "other"]) == false)
            throw new Exception("Multi-pack collection lost deterministic identity ordering.");
        var conflictCatalog = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with
        {
            Id = "conflict",
            Entries = [entry]
        }));
        collection.AddOrReplace(conflictCatalog);
        var ambiguous = collection.Resolve("fixture-a", "5");
        if (!ambiguous.Ambiguous || ambiguous.Found || ambiguous.Entry is not null ||
            !ambiguous.MatchingPackIds.SequenceEqual(["conflict", "fixture"]))
            throw new Exception("Conflicting contextual references were not reported explicitly.");
        collection.AddOrReplace(KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with
            { Id = "conflict", Entries = [entry with { Code = "different" }] })));
        if (!collection.Resolve("fixture-a", "5").Found || collection.Count != 3)
            throw new Exception("Same-ID pack replacement did not remove an old contextual conflict.");
        var resolvedSet = KnowledgeResolver.Resolve(collection, "fixture-a", "5");
        if (!resolvedSet.Found || resolvedSet.PackId != "fixture" || resolvedSet.Revision != 1 ||
            resolvedSet.Ambiguous || !resolvedSet.MatchingPackIds.SequenceEqual(["fixture"]))
            throw new Exception("Set resolver lost exact match provenance.");
        var setWin32Entry = entry with { Context = "win32", Code = "5", Title = "Access denied" };
        collection.AddOrReplace(KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with
            { Id = "win32-pack", Entries = [setWin32Entry] })));
        var derivedSet = KnowledgeResolver.Resolve(collection, "hresult", "0x80070005");
        if (!derivedSet.Found || !derivedSet.DerivedFromHResult || derivedSet.MatchedContext != "win32" ||
            derivedSet.MatchedCode != "5" || derivedSet.PackId != "win32-pack")
            throw new Exception("Set resolver lost the bounded HRESULT_FROM_WIN32 fallback.");
        collection.AddOrReplace(KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with
            { Id = "win32-conflict", Entries = [setWin32Entry] })));
        var ambiguousDerived = KnowledgeResolver.Resolve(collection, "hresult", "0x80070005");
        if (!ambiguousDerived.Ambiguous || ambiguousDerived.Found || ambiguousDerived.Match is not null ||
            !ambiguousDerived.DerivedFromHResult ||
            !ambiguousDerived.MatchingPackIds.SequenceEqual(["win32-conflict", "win32-pack"]))
            throw new Exception("Set resolver selected a conflicting derived reference.");
        foreach (var invalidLookup in new[] { (" fixture-a", "5"), ("fixture-a", " 5"), ("", "5") })
        {
            try { collection.Resolve(invalidLookup.Item1, invalidLookup.Item2); throw new Exception("Invalid collection lookup accepted."); }
            catch (ArgumentException) { }
        }
        var boundedCollection = new KnowledgeCatalogCollection();
        for (int index = 0; index < 32; index++)
            boundedCollection.AddOrReplace(KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with { Id = "pack-" + index })));
        try
        {
            boundedCollection.AddOrReplace(KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with { Id = "pack-overflow" })));
            throw new Exception("More than 32 simultaneous knowledge packs were accepted.");
        }
        catch (InvalidOperationException) { }
        var revised = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with { Revision = 2,
            Entries = [entry with { Explanation = "Changed fixture" }, entry with { Code = "6" }] }));
        var comparison = catalog.Compare(revised, 2);
        if (comparison.Added != 1 || comparison.Removed != 1 || comparison.Changed != 1 || !comparison.Truncated ||
            comparison.Changes.Count != 2 || comparison.BeforeRevision != 1 || comparison.AfterRevision != 2)
            throw new Exception("Pack comparison lost changed, added or removed reference coverage.");
        var reordered = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with { Entries = [.. pack.Entries.Reverse()] }));
        if (catalog.Compare(reordered).Changes.Count != 0) throw new Exception("Entry ordering changed pack comparison.");
        foreach (var metadataPack in new[] { pack with { License = "Updated fixture license" }, pack with { PublishedOn = day.AddDays(1) }, pack with { SchemaVersion = 2 } })
        {
            var metadata = catalog.Compare(KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(metadataPack)));
            if (!metadata.MetadataChanged || metadata.Changes.Count != 0 || metadata.Added + metadata.Removed + metadata.Changed != 0)
                throw new Exception("Metadata-only pack change was lost or counted as reference changes.");
            if (metadata.BeforeMetadata != new KnowledgeMetadata(pack.SchemaVersion, pack.PublishedOn, pack.License) ||
                metadata.AfterMetadata != new KnowledgeMetadata(metadataPack.SchemaVersion, metadataPack.PublishedOn, metadataPack.License))
                throw new Exception("Comparison lost exact before/after pack metadata.");
        }
        foreach (int limit in new[] { 0, 201 })
        {
            try { catalog.Compare(revised, limit); throw new Exception("Invalid comparison limit accepted."); }
            catch (ArgumentOutOfRangeException) { }
        }
        try { catalog.Compare(KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with { Id = "other-fixture" }))); throw new Exception("Unrelated pack comparison accepted."); }
        catch (ArgumentException) { }
        try { catalog.Compare(revised, token: new CancellationToken(true)); throw new Exception("Canceled comparison proceeded."); }
        catch (OperationCanceledException) { }
        var searchable = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with { Entries =
            [entry with { Code = "20" }, entry with { Code = "10" }, entry with { Context = "other" }] }));
        var search = searchable.Search("FIXTURE-A", "synthetic", 1);
        if (search.TotalMatches != 2 || !search.Truncated || search.Matches.Single().Code != "10" ||
            searchable.Search("other", "synthetic").TotalMatches != 1 || searchable.Search("missing", "synthetic").TotalMatches != 0 ||
            searchable.Search("fixture-a", "Fixture product").TotalMatches != 2 || searchable.Search("fixture-a", "20").TotalMatches != 1)
            throw new Exception("Knowledge search must preserve context, count omitted matches and sort bounded results");
        foreach (var limit in new[] { 0, 201 })
        {
            try { searchable.Search("fixture-a", "synthetic", limit); throw new Exception("Invalid search limit accepted"); }
            catch (ArgumentOutOfRangeException) { }
        }
        using (var searchStop = new CancellationTokenSource())
        {
            searchStop.Cancel();
            try { searchable.Search("fixture-a", "synthetic", token: searchStop.Token); throw new Exception("Canceled search accepted"); }
            catch (OperationCanceledException) { }
        }
        using var signer = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        var payload = JsonSerializer.SerializeToUtf8Bytes(pack);
        var signature = signer.SignData(payload, System.Security.Cryptography.HashAlgorithmName.SHA256,
            System.Security.Cryptography.DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        var publicKey = signer.ExportSubjectPublicKeyInfo();
        SignedKnowledgePack Sign(KnowledgePack value)
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(value);
            return new(data, signer.SignData(data, System.Security.Cryptography.HashAlgorithmName.SHA256,
                System.Security.Cryptography.DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        }
        var bundled = Sign(pack);
        var newer = Sign(pack with { Revision = 2 });
        if (!KnowledgeSelection.Select(bundled, newer, publicKey, "fixture").UsingExternal ||
            KnowledgeSelection.Select(newer, bundled, publicKey, "fixture").UsingExternal ||
            KnowledgeSelection.Select(bundled, Sign(pack with { License = "Conflicting revision" }), publicKey, "fixture").UsingExternal)
            throw new Exception("Knowledge selection must prefer newer verified data and reject revision conflicts");
        var corrupt = new SignedKnowledgePack(newer.Payload, new byte[64]);
        var changedSigned = Sign(pack with { Revision = 2, License = "Updated license", Entries = [entry with { Explanation = "Updated signed explanation" }] });
        var verifiedChanges = KnowledgeSelection.Select(bundled, changedSigned, publicKey, "fixture");
        if (!verifiedChanges.UsingExternal || verifiedChanges.Comparison is not { Changed: 1, Removed: 1, MetadataChanged: true } ||
            verifiedChanges.Comparison.AfterMetadata?.License != "Updated license")
            throw new Exception("Verified external selection lost its exact change review.");
        foreach (var rejected in new[] { corrupt, Sign(pack with { Id = "other", Revision = 2 }), Sign(pack with { License = "Conflict" }) })
        {
            var fallback = KnowledgeSelection.Select(bundled, rejected, publicKey, "fixture");
            if (fallback.UsingExternal || fallback.Comparison is not null || fallback.Catalog.Revision != 1)
                throw new Exception("Rejected candidate exposed accepted update changes.");
        }
        if (KnowledgeSelection.Select(bundled, corrupt, publicKey, "fixture").UsingExternal)
            throw new Exception("Invalid external signature replaced bundled data");
        if (KnowledgeTrust.Verify(payload, signature, publicKey, "fixture", 1).Revision != 1)
            throw new Exception("Signed pack was not loaded");
        var tampered = (byte[])payload.Clone();
        tampered[10] ^= 1;
        try { KnowledgeTrust.Verify(tampered, signature, publicKey, "fixture", 1); throw new Exception("Tampered pack accepted"); }
        catch (System.Security.Cryptography.CryptographicException) { }
        foreach (var expected in new[] { ("other", 1L), ("fixture", 2L) })
        {
            try { KnowledgeTrust.Verify(payload, signature, publicKey, expected.Item1, expected.Item2); throw new Exception("Wrong identity or rollback accepted"); }
            catch (System.IO.InvalidDataException) { }
        }
        if (catalog.Find("FIXTURE-A", "5")?.Title != "Fixture" || catalog.Find("fixture-b", "5")?.Title != "Other context" || catalog.Find("unknown", "5") is not null)
            throw new Exception("Knowledge keys must preserve code context");
        var macPack = new KnowledgePack(1, "ieee-mac-vendors", 3, day, "Synthetic test data",
        [
            entry with { Context = "mac-vendor", Code = "001122", Title = "Large assignment" },
            entry with { Context = "mac-vendor", Code = "0011223", Title = "Medium assignment" },
            entry with { Context = "mac-vendor", Code = "001122334", Title = "Small assignment" }
        ]);
        var macCatalog = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(macPack));
        var smallMac = MacVendorLookup.Find(macCatalog, "00:11:22:33:44:55");
        if (smallMac.MatchedPrefix != "001122334" || smallMac.Match?.Title != "Small assignment" || smallMac.Address != "001122334455" ||
            MacVendorLookup.Find(KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(macPack with { Entries = [.. macPack.Entries.Take(2)] })), "0011.2233.4455").MatchedPrefix != "0011223" ||
            MacVendorLookup.Find(KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(macPack with { Entries = [.. macPack.Entries.Take(1)] })), "00-11-22-33-44-55").MatchedPrefix != "001122")
            throw new Exception("MAC vendor lookup did not prefer the longest exact IEEE assignment prefix");
        var localMac = MacVendorLookup.Find(macCatalog, "02:11:22:33:44:55");
        var multicastMac = MacVendorLookup.Find(macCatalog, "01:11:22:33:44:55");
        if (!localMac.LocallyAdministered || localMac.Match is not null || !multicastMac.Multicast || multicastMac.Match is not null)
            throw new Exception("Local and multicast MAC addresses must not produce vendor claims");
        foreach (string invalidMac in new[] { "", "00112233445", "00:11-22:33:44:55", "000000000000", "FFFFFFFFFFFF", " 001122334455" })
        {
            try { MacVendorLookup.Normalize(invalidMac); throw new Exception("Invalid MAC address accepted"); }
            catch (ArgumentException) { }
        }
        var pciPack = new KnowledgePack(1, "pci-hardware-vendors", 11, day, "Synthetic test data",
        [
            entry with { Context = "pci-vendor", Code = "VEN_1234", Title = "Fixture Vendor" },
            entry with { Context = "pci-device", Code = "VEN_1234&DEV_5678", Title = "Fixture Device" },
            entry with { Context = "pci-vendor", Code = "VEN_ABCD", Title = "Vendor Only" }
        ]);
        var pciCatalog = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pciPack));
        var pci = PciHardwareLookup.Find(pciCatalog, @"pci\ven_1234&dev_5678&subsys_00000001&rev_02");
        if (pci.HardwareId != @"PCI\VEN_1234&DEV_5678&SUBSYS_00000001&REV_02" ||
            pci.Vendor?.Title != "Fixture Vendor" || pci.Device?.Title != "Fixture Device" ||
            pci.VendorCode != "VEN_1234" || pci.DeviceCode != "VEN_1234&DEV_5678")
            throw new Exception("Exact PCI vendor and device lookup lost normalized identity or pack provenance");
        var vendorOnly = PciHardwareLookup.Find(pciCatalog, @"PCI\VEN_ABCD&DEV_0001");
        var missingPci = PciHardwareLookup.Find(pciCatalog, @"PCI\VEN_9999&DEV_0001");
        if (vendorOnly.Vendor?.Title != "Vendor Only" || vendorOnly.Device is not null ||
            missingPci.Vendor is not null || missingPci.Device is not null)
            throw new Exception("PCI lookup invented a device or vendor assignment");
        foreach (string invalidPci in new[] { "", @"USB\VID_1234&PID_5678", @"PCI\VEN_123&DEV_5678", @"PCI\VEN_1234&DEV_567", @"PCI\VEN_1234&DEV_5678&DEV_9999", " PCI\\VEN_1234&DEV_5678" })
        {
            try { PciHardwareLookup.Normalize(invalidPci); throw new Exception("Invalid PCI hardware ID accepted"); }
            catch (ArgumentException) { }
        }
        var usbPack = new KnowledgePack(1, "usb-hardware-vendors", 12, day, "Synthetic test data",
        [
            entry with { Context = "usb-vendor", Code = "VID_1234", Title = "Fixture USB Vendor" },
            entry with { Context = "usb-device", Code = "VID_1234&PID_5678", Title = "Fixture USB Product" },
            entry with { Context = "usb-vendor", Code = "VID_ABCD", Title = "USB Vendor Only" }
        ]);
        var usbCatalog = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(usbPack));
        var usb = UsbHardwareLookup.Find(usbCatalog, @"usb\vid_1234&pid_5678&mi_01");
        if (usb.HardwareId != @"USB\VID_1234&PID_5678&MI_01" ||
            usb.Vendor?.Title != "Fixture USB Vendor" || usb.Product?.Title != "Fixture USB Product" ||
            usb.VendorCode != "VID_1234" || usb.ProductCode != "VID_1234&PID_5678")
            throw new Exception("Exact USB vendor and product lookup lost normalized identity or pack provenance");
        var usbVendorOnly = UsbHardwareLookup.Find(usbCatalog, @"USB\VID_ABCD&PID_0001");
        var missingUsb = UsbHardwareLookup.Find(usbCatalog, @"USB\VID_9999&PID_0001");
        if (usbVendorOnly.Vendor?.Title != "USB Vendor Only" || usbVendorOnly.Product is not null ||
            missingUsb.Vendor is not null || missingUsb.Product is not null)
            throw new Exception("USB lookup invented a product or vendor assignment");
        foreach (string invalidUsb in new[] { "", @"PCI\VEN_1234&DEV_5678", @"USB\VID_123&PID_5678", @"USB\VID_1234&PID_567", @"USB\VID_1234&PID_5678&PID_9999", " USB\\VID_1234&PID_5678" })
        {
            try { UsbHardwareLookup.Normalize(invalidUsb); throw new Exception("Invalid USB hardware ID accepted"); }
            catch (ArgumentException) { }
        }
        var uefiPack = new KnowledgePack(1, "uefi-pnp-acpi-vendors", 13, day, "Synthetic test data",
        [
            entry with { Context = "pnp-vendor", Code = "ABC", Title = "Fixture PNP Vendor" },
            entry with { Context = "acpi-vendor", Code = "A1B2", Title = "Fixture ACPI Vendor" }
        ]);
        var uefiCatalog = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(uefiPack));
        var pnpVendor = UefiVendorLookup.Find(uefiCatalog, UefiVendorRegistry.Pnp, "abc");
        var acpiVendor = UefiVendorLookup.Find(uefiCatalog, UefiVendorRegistry.Acpi, "a1b2");
        var missingUefi = UefiVendorLookup.Find(uefiCatalog, UefiVendorRegistry.Acpi, "NONE");
        if (pnpVendor.VendorId != "ABC" || pnpVendor.Match?.Title != "Fixture PNP Vendor" ||
            acpiVendor.VendorId != "A1B2" || acpiVendor.Match?.Title != "Fixture ACPI Vendor" ||
            pnpVendor.PackId != "uefi-pnp-acpi-vendors" || pnpVendor.Revision != 13 || missingUefi.Match is not null)
            throw new Exception("UEFI vendor lookup lost exact registry identity or pack provenance");
        foreach (var invalidUefi in new[]
        {
            (UefiVendorRegistry.Pnp, "AB1"), (UefiVendorRegistry.Pnp, "ABCD"),
            (UefiVendorRegistry.Acpi, "ABC"), (UefiVendorRegistry.Acpi, "AB_C"),
            (UefiVendorRegistry.Acpi, " A1B2")
        })
        {
            try { UefiVendorLookup.Normalize(invalidUefi.Item1, invalidUefi.Item2); throw new Exception("Invalid UEFI vendor ID accepted"); }
            catch (ArgumentException) { }
        }
        var bluetoothPack = new KnowledgePack(1, "bluetooth-company-identifiers", 14, day, "Synthetic test data",
        [
            entry with { Context = "bluetooth-company", Code = "0x0006", Title = "Fixture Bluetooth Company" }
        ]);
        var bluetoothCatalog = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(bluetoothPack));
        var bluetoothHex = BluetoothCompanyLookup.Find(bluetoothCatalog, "0x0006");
        var bluetoothDecimal = BluetoothCompanyLookup.Find(bluetoothCatalog, "6");
        var missingBluetooth = BluetoothCompanyLookup.Find(bluetoothCatalog, "65535");
        if (bluetoothHex.CompanyId != "0x0006" || bluetoothHex.NumericId != 6 ||
            bluetoothDecimal.Match?.Title != "Fixture Bluetooth Company" || bluetoothDecimal.PackId != "bluetooth-company-identifiers" ||
            bluetoothDecimal.Revision != 14 || missingBluetooth.CompanyId != "0xFFFF" || missingBluetooth.Match is not null)
            throw new Exception("Bluetooth company lookup lost numeric equivalence or pack provenance");
        foreach (string invalidBluetooth in new[] { "", "-1", "65536", "0x10000", "0xGGGG", " 6", "+6" })
        {
            try { BluetoothCompanyLookup.Normalize(invalidBluetooth); throw new Exception("Invalid Bluetooth company identifier accepted"); }
            catch (ArgumentException) { }
        }
        var driver = new DriverReference(@"PCI\VEN_1234&DEV_5678&SUBSYS_00000001", "x64", "client", 22000, 26100,
            "Synthetic vendor", "1.2.3", "https://example.com/driver");
        var driverPack = pack with { SchemaVersion = 2, Entries = [entry with { Driver = driver }] };
        var drivers = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(driverPack));
        if (drivers.FindDriverReferences(driver.HardwareId.ToLowerInvariant(), "X64", "client", 22000).Count != 1 ||
            drivers.FindDriverReferences(driver.HardwareId, "x64", "client", 26100).Count != 1 ||
            drivers.FindDriverReferences(driver.HardwareId, "arm64", "client", 26100).Count != 0 ||
            drivers.FindDriverReferences(driver.HardwareId, "x64", "server", 26100).Count != 0 ||
            drivers.FindDriverReferences(driver.HardwareId, "x64", "client", 26101).Count != 0 ||
            drivers.FindDriverReferences(@"PCI\VEN_1234&DEV_5678", "x64", "client", 26100).Count != 0)
            throw new Exception("Driver reference filtering broadened its declared hardware or OS applicability");
        var driverCatalogs = new KnowledgeCatalogCollection([drivers]);
        var driverSearch = driverCatalogs.FindDriverReferences(driver.HardwareId, "x64", "client", 26100);
        if (driverSearch.TotalMatches != 1 || driverSearch.Truncated || driverSearch.Ambiguous ||
            driverSearch.Matches.Single().PackId != drivers.PackId || driverSearch.Matches.Single().Revision != drivers.Revision)
            throw new Exception("Multi-pack driver lookup lost bounds or pack provenance.");
        string deviceJson = JsonSerializer.Serialize(new
        {
            devices = new[] { new { DeviceID = "DEVICE-1", Name = "Fixture adapter", PNPClass = "Net", Present = true,
                ConfigManagerErrorCode = 28u, HardwareID = new[] { driver.HardwareId } } },
            drivers = Array.Empty<object>(), driverError = (string?)null
        });
        var deviceEvidence = new ToolResult(0, deviceJson, "", false);
        var driverFinding = DeviceDriverReferences.Check(deviceEvidence, driverCatalogs,
            new("x64", "client", 26100));
        if (driverFinding.State != CheckState.Attention || !driverFinding.Detail.Contains(driver.DownloadUrl, StringComparison.Ordinal) ||
            !driverFinding.Detail.Contains("pack fixture revision 1", StringComparison.Ordinal) || driverFinding.Repair is not null)
            throw new Exception("Problem-device correlation lost the exact declared driver reference or safety boundary.");
        var wrongPlatform = DeviceDriverReferences.Check(deviceEvidence, driverCatalogs, new("arm64", "client", 26100));
        if (wrongPlatform.State != CheckState.Attention || !wrongPlatform.Detail.Contains("no exact declared driver reference", StringComparison.Ordinal))
            throw new Exception("Driver correlation broadened architecture applicability.");
        var conflictingDrivers = new KnowledgeCatalogCollection([drivers,
            KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(driverPack with { Id = "driver-conflict" }))]);
        var conflictSearch = conflictingDrivers.FindDriverReferences(driver.HardwareId, "x64", "client", 26100);
        var conflictFinding = DeviceDriverReferences.Check(deviceEvidence, conflictingDrivers, new("x64", "client", 26100));
        if (!conflictSearch.Ambiguous || !conflictSearch.ConflictingIdentities.SequenceEqual(["FIXTURE-A/5"]) ||
            conflictFinding.State != CheckState.Unknown || !conflictFinding.Detail.Contains("conflicting reference", StringComparison.Ordinal))
            throw new Exception("Conflicting driver references were selected or hidden.");
        if (DeviceDriverReferences.Check(deviceEvidence, driverCatalogs, null).State != CheckState.Unknown ||
            DeviceDriverReferences.Check(deviceEvidence with { Truncated = true }, driverCatalogs, new("x64", "client", 26100)).State != CheckState.Unknown)
            throw new Exception("Driver correlation accepted missing platform or incomplete device evidence.");
        foreach (var badDriver in new[] { driver with { MaximumBuild = 1 }, driver with { DownloadUrl = "file:///driver.exe" },
            driver with { Architecture = "any" }, driver with { WindowsProduct = "any" } })
        {
            try { KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(driverPack with { Entries = [entry with { Driver = badDriver }] })); throw new Exception("Invalid driver reference accepted"); }
            catch (System.IO.InvalidDataException) { }
        }
        foreach (var invalid in new[] { pack with { SchemaVersion = 3 }, pack with { Entries = [entry, entry] },
            pack with { Entries = [entry with { Driver = driver }] },
            pack with { Entries = [entry with { SourceUrl = "file:///fixture" }] }, pack with { Entries = [entry with { VerifiedOn = day.AddDays(1) }] } })
        {
            try { KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(invalid)); throw new Exception("Invalid knowledge pack accepted"); }
            catch (System.IO.InvalidDataException) { }
        }
        var largeEntry = entry with { Context = "scale", Title = "Test", Explanation = "Synthetic", AppliesTo = "Fixture" };
        var largePack = pack with { Entries = [.. Enumerable.Range(0, 100_000)
            .Select(i => largeEntry with { Code = i.ToString(System.Globalization.CultureInfo.InvariantCulture) })] };
        var largeBytes = JsonSerializer.SerializeToUtf8Bytes(largePack);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var largeCatalog = KnowledgeCatalog.Read(largeBytes);
        var largeComparison = largeCatalog.Compare(largeCatalog, 1);
        if (largeComparison.Added + largeComparison.Removed + largeComparison.Changed != 0 || largeComparison.Truncated)
            throw new Exception("Maximum-size unchanged pack comparison is inconsistent.");
        long loadMilliseconds = timer.ElapsedMilliseconds;
        for (int i = 0; i < 100_000; i++)
            if (largeCatalog.Find("scale", i.ToString(System.Globalization.CultureInfo.InvariantCulture)) is null)
                throw new Exception("Full-size knowledge pack lost an indexed entry");
        Console.WriteLine($"Knowledge scale fixture: {largeCatalog.Count} entries, {largeBytes.Length} bytes, load {loadMilliseconds} ms, lookup loop {timer.ElapsedMilliseconds - loadMilliseconds} ms.");
        foreach (var invalidBytes in new[] { new byte[32_000_001],
            JsonSerializer.SerializeToUtf8Bytes(largePack with { Entries = [.. largePack.Entries.Append(largeEntry with { Code = "100000" })] }),
            System.Text.Encoding.UTF8.GetBytes("{\"SchemaVersion\":1,\"SchemaVersion\":2}") })
        {
            try { KnowledgeCatalog.Read(invalidBytes); throw new Exception("Oversized or ambiguous knowledge pack accepted"); }
            catch (System.IO.InvalidDataException) { }
        }
        var referencePack = pack with { Entries = [entry with { Code = "0x80070005" }] };
        var references = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(referencePack));
        var source = new TextEvidence("fixture", ["code 0x80070005", "again 0x80070005 and 0x80070002"], true);
        var joined = KnowledgeAnalysis.Analyze(source, references, "fixture-a");
        if (joined.Codes.Count != 2 || joined.Codes.Count(c => c.Reference is not null) != 1 ||
            joined.Codes.Single(c => c.Reference is not null).Observation.Occurrences != 2 || !joined.Analysis.SourceTruncated ||
            joined.Codes.Single(c => c.Reference is not null).Observation.Evidence[1].LineNumber != 2 ||
            joined.Codes.Single(c => c.Reference is not null).MatchedContext != "fixture-a" ||
            joined.Codes.Single(c => c.Reference is not null).MatchedCode != "0x80070005" ||
            joined.Codes.Single(c => c.Reference is not null).DerivedFromHResult ||
            KnowledgeAnalysis.Analyze(source, references, "fixture-b").Codes.Any(c => c.Reference is not null))
            throw new Exception("Knowledge analysis lost evidence, unmatched codes, truncation or context isolation");
        var win32Entry = entry with { Context = "win32", Code = "5", Title = "Access denied" };
        var hresultReferences = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with { Entries = [win32Entry] }));
        var derived = KnowledgeAnalysis.Analyze(new("fixture", ["0x80070005", "0x80070002", "0x800F0005"], false),
            hresultReferences, "windows-update");
        var accessDenied = derived.Codes.Single(code => code.Observation.Code == "0x80070005");
        var fileMissing = derived.Codes.Single(code => code.Observation.Code == "0x80070002");
        var otherFacility = derived.Codes.Single(code => code.Observation.Code == "0x800F0005");
        if (derived.Codes.Count != 3 || accessDenied.Reference != win32Entry || !accessDenied.DerivedFromHResult ||
            accessDenied.MatchedContext != "win32" || accessDenied.MatchedCode != "5" ||
            fileMissing.Reference is not null || fileMissing.MatchedCode is not null ||
            otherFacility.Reference is not null || !derived.Limitation.Contains("0x8007xxxx", StringComparison.Ordinal))
            throw new Exception("HRESULT_FROM_WIN32 fallback lost exact derivation, transparency or context isolation");
        var directWins = KnowledgeCatalog.Read(JsonSerializer.SerializeToUtf8Bytes(pack with
        {
            Entries = [win32Entry, entry with { Context = "windows-update", Code = "0x80070005", Title = "Specific update reference" }]
        }));
        var direct = KnowledgeAnalysis.Analyze(new("fixture", ["0x80070005"], false), directWins, "windows-update").Codes.Single();
        if (direct.Reference?.Title != "Specific update reference" || direct.DerivedFromHResult || direct.MatchedContext != "windows-update")
            throw new Exception("Context-specific error reference did not take precedence over Win32 fallback");
        var directResolution = KnowledgeResolver.Resolve(directWins, "windows-update", "0x80070005");
        var derivedResolution = KnowledgeResolver.Resolve(hresultReferences, "windows-update", "0X80070005");
        var missingResolution = KnowledgeResolver.Resolve(hresultReferences, "windows-update", "0x800F0005");
        if (!directResolution.Found || directResolution.DerivedFromHResult || directResolution.MatchedContext != "windows-update" ||
            !derivedResolution.Found || !derivedResolution.DerivedFromHResult || derivedResolution.MatchedCode != "5" ||
            missingResolution.Found || missingResolution.MatchedContext is not null ||
            !KnowledgeResolution.Limitation.Contains("does not establish a diagnosis", StringComparison.Ordinal))
            throw new Exception("Standalone knowledge resolution lost precedence, derivation or honest missing outcomes");
        foreach (var invalid in new[] { ("", "5"), ("context", ""), (" context", "5"), ("context", " 5") })
        {
            try { KnowledgeResolver.Resolve(hresultReferences, invalid.Item1, invalid.Item2); throw new Exception("Invalid knowledge resolution input accepted"); }
            catch (ArgumentException) { }
        }
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        try { KnowledgeCatalog.Read(largeBytes, canceled.Token); throw new Exception("Canceled catalog read returned data"); }
        catch (OperationCanceledException) { }
        using var duringLoad = new CancellationTokenSource();
        var loading = Task.Run(() => KnowledgeCatalog.Read(largeBytes, duringLoad.Token));
        duringLoad.CancelAfter(TimeSpan.FromMilliseconds(10));
        try { loading.GetAwaiter().GetResult(); throw new Exception("Interrupted catalog load returned data"); }
        catch (OperationCanceledException) { }
        try { KnowledgeAnalysis.Analyze(source, references, "fixture-a", canceled.Token); throw new Exception("Canceled reference analysis ran"); }
        catch (OperationCanceledException) { }
        Console.WriteLine("PASS: contextual knowledge lookup and log references, driver applicability, schema, provenance, scale bounds and duplicate-key rejection (synthetic data).");
    }
}
