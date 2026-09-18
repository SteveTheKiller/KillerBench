using System.Text.Json;
using KillerBench.Engine;

bool ieeeImport = args.Length > 0 && args[0] == "--ieee-oui";
bool driverImport = args.Length > 0 && args[0] == "--driver-csv";
bool referenceImport = args.Length > 0 && args[0] == "--reference-csv";
bool windowsMessages = args.Length > 0 && args[0] == "--windows-message-csv";
bool ntStatusMessages = args.Length > 0 && args[0] == "--ntstatus-message-csv";
bool hResultMessages = args.Length > 0 && args[0] == "--hresult-message-csv";
bool pciImport = args.Length > 0 && args[0] == "--pci-ids";
bool usbImport = args.Length > 0 && args[0] == "--usb-ids";
bool bugCheckImport = args.Length > 0 && args[0] == "--bugcheck-markdown";
bool uefiImport = args.Length > 0 && args[0] == "--uefi-vendor-ids";
bool bluetoothImport = args.Length > 0 && args[0] == "--bluetooth-company-ids";
if ((!ieeeImport && !driverImport && !referenceImport && !windowsMessages && !ntStatusMessages && !hResultMessages && !pciImport && !usbImport && !bugCheckImport && !uefiImport && !bluetoothImport && args.Length != 2) || (ieeeImport && args.Length != 8) ||
    (driverImport && args.Length != 7) || (referenceImport && args.Length != 7) || (uefiImport && args.Length != 7) || (windowsMessages && args.Length != 5) || ((ntStatusMessages || hResultMessages) && args.Length != 5) || ((pciImport || usbImport || bugCheckImport || bluetoothImport) && args.Length != 6))
{
    Console.Error.WriteLine("Usage: KnowledgeBuilder REVIEWED_JSON NEW_OUTPUT_JSON | --check REVIEWED_JSON | --ieee-oui MA_L_CSV MA_M_CSV MA_S_CSV REVISION YYYY-MM-DD LICENSE NEW_OUTPUT_JSON | --uefi-vendor-ids PNP_CSV ACPI_CSV REVISION YYYY-MM-DD LICENSE NEW_OUTPUT_JSON | --bluetooth-company-ids COMPANY_YAML REVISION YYYY-MM-DD LICENSE NEW_OUTPUT_JSON | --pci-ids PCI_IDS REVISION YYYY-MM-DD LICENSE NEW_OUTPUT_JSON | --usb-ids USB_IDS REVISION YYYY-MM-DD LICENSE NEW_OUTPUT_JSON | --bugcheck-markdown REFERENCE_MD REVISION YYYY-MM-DD LICENSE NEW_OUTPUT_JSON | --driver-csv REVIEWED_CSV PACK_ID REVISION YYYY-MM-DD LICENSE NEW_OUTPUT_JSON | --reference-csv REVIEWED_CSV PACK_ID REVISION YYYY-MM-DD LICENSE NEW_OUTPUT_JSON | --windows-message-csv FIRST_CODE LAST_CODE YYYY-MM-DD NEW_REVIEW_CSV | --ntstatus-message-csv FIRST_STATUS LAST_STATUS YYYY-MM-DD NEW_REVIEW_CSV | --hresult-message-csv FIRST_RESULT LAST_RESULT YYYY-MM-DD NEW_REVIEW_CSV");
    return 2;
}
bool outputCreated = false;
bool checkOnly = !ieeeImport && !driverImport && !referenceImport && !windowsMessages && !ntStatusMessages && !hResultMessages && !pciImport && !usbImport && !bugCheckImport && !uefiImport && !bluetoothImport && args[0] == "--check";
using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(30));
bool userCanceled = false;
ConsoleCancelEventHandler cancel = (_, e) =>
{
    e.Cancel = true;
    userCanceled = true;
    stop.Cancel();
};
Console.CancelKeyPress += cancel;
try
{
    byte[] output;
    string outputPath;
    KnowledgeCatalog validated;
    var fingerprints = new List<(string Label, byte[] Bytes)>();
    if (windowsMessages)
    {
        if (!int.TryParse(args[1], out int firstCode) || !int.TryParse(args[2], out int lastCode) ||
            !DateOnly.TryParseExact(args[3], "yyyy-MM-dd", out DateOnly verifiedOn))
            throw new ArgumentException("Windows message generation requires decimal bounds and an exact YYYY-MM-DD verification date.");
        var generated = WindowsMessageCsvGenerator.Build(firstCode, lastCode, verifiedOn, token: stop.Token);
        outputPath = args[4];
        output = generated.Bytes;
        stop.Token.ThrowIfCancellationRequested();
        await using var reviewDestination = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        outputCreated = true;
        await reviewDestination.WriteAsync(output, stop.Token);
        await reviewDestination.FlushAsync(stop.Token);
        reviewDestination.Flush(true);
        Console.WriteLine($"Generated {generated.Entries} installed Windows messages for review. No knowledge pack was built or published.");
        Console.WriteLine("Output SHA-256: " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(output)));
        Console.WriteLine("Output bytes: " + output.Length);
        return 0;
    }
    if (ntStatusMessages)
    {
        if (!TryParseStatus(args[1], out uint firstStatus) || !TryParseStatus(args[2], out uint lastStatus) ||
            !DateOnly.TryParseExact(args[3], "yyyy-MM-dd", out DateOnly verifiedOn))
            throw new ArgumentException("NTSTATUS message generation requires exact 0x plus eight-hex-digit bounds and an exact YYYY-MM-DD verification date.");
        var generated = NtStatusMessageCsvGenerator.Build(firstStatus, lastStatus, verifiedOn, token: stop.Token);
        outputPath = args[4];
        output = generated.Bytes;
        stop.Token.ThrowIfCancellationRequested();
        await using var reviewDestination = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        outputCreated = true;
        await reviewDestination.WriteAsync(output, stop.Token);
        await reviewDestination.FlushAsync(stop.Token);
        reviewDestination.Flush(true);
        Console.WriteLine($"Generated {generated.Entries} installed NTSTATUS messages for review. No knowledge pack was built or published.");
        Console.WriteLine("Output SHA-256: " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(output)));
        Console.WriteLine("Output bytes: " + output.Length);
        return 0;
    }
    if (hResultMessages)
    {
        if (!TryParseStatus(args[1], out uint firstResult) || !TryParseStatus(args[2], out uint lastResult) ||
            !DateOnly.TryParseExact(args[3], "yyyy-MM-dd", out DateOnly verifiedOn))
            throw new ArgumentException("HRESULT message generation requires exact 0x plus eight-hex-digit bounds and an exact YYYY-MM-DD verification date.");
        var generated = HResultMessageCsvGenerator.Build(firstResult, lastResult, verifiedOn, token: stop.Token);
        outputPath = args[4];
        output = generated.Bytes;
        stop.Token.ThrowIfCancellationRequested();
        await using var reviewDestination = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        outputCreated = true;
        await reviewDestination.WriteAsync(output, stop.Token);
        await reviewDestination.FlushAsync(stop.Token);
        reviewDestination.Flush(true);
        Console.WriteLine($"Generated {generated.Entries} installed HRESULT messages for review. No knowledge pack was built or published.");
        Console.WriteLine("Output SHA-256: " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(output)));
        Console.WriteLine("Output bytes: " + output.Length);
        return 0;
    }
    if (bluetoothImport)
    {
        if (!long.TryParse(args[2], out long revision) || !DateOnly.TryParseExact(args[3], "yyyy-MM-dd", out DateOnly publishedOn))
            throw new ArgumentException("Bluetooth company identifier import requires a positive revision and an exact YYYY-MM-DD publication date.");
        var imported = await BluetoothCompanyIdentifierImporter.BuildAsync(args[1], revision, publishedOn, args[4], stop.Token);
        outputPath = args[5];
        output = JsonSerializer.SerializeToUtf8Bytes(imported.Pack, new JsonSerializerOptions { WriteIndented = true });
        fingerprints.Add(("Bluetooth company identifier input", imported.Source));
    }
    else if (uefiImport)
    {
        if (!long.TryParse(args[3], out long revision) || !DateOnly.TryParseExact(args[4], "yyyy-MM-dd", out DateOnly publishedOn))
            throw new ArgumentException("UEFI vendor ID import requires a positive revision and an exact YYYY-MM-DD publication date.");
        var imported = await UefiVendorIdImporter.BuildAsync(args[1], args[2], revision, publishedOn, args[5], stop.Token);
        outputPath = args[6];
        output = JsonSerializer.SerializeToUtf8Bytes(imported.Pack, new JsonSerializerOptions { WriteIndented = true });
        fingerprints.AddRange(imported.Sources.Select(source => (source.Registry + " registry input", source.Bytes)));
    }
    else if (bugCheckImport)
    {
        if (!long.TryParse(args[2], out long revision) || !DateOnly.TryParseExact(args[3], "yyyy-MM-dd", out DateOnly publishedOn))
            throw new ArgumentException("Bug-check import requires a positive revision and an exact YYYY-MM-DD publication date.");
        var imported = await BugCheckMarkdownImporter.BuildAsync(args[1], revision, publishedOn, args[4], stop.Token);
        outputPath = args[5];
        output = JsonSerializer.SerializeToUtf8Bytes(imported.Pack, new JsonSerializerOptions { WriteIndented = true });
        fingerprints.Add(("Bug-check reference input", imported.Source));
    }
    else if (usbImport)
    {
        if (!long.TryParse(args[2], out long revision) || !DateOnly.TryParseExact(args[3], "yyyy-MM-dd", out DateOnly publishedOn))
            throw new ArgumentException("USB ID import requires a positive revision and an exact YYYY-MM-DD publication date.");
        var imported = await UsbIdsImporter.BuildAsync(args[1], revision, publishedOn, args[4], stop.Token);
        outputPath = args[5];
        output = JsonSerializer.SerializeToUtf8Bytes(imported.Pack, new JsonSerializerOptions { WriteIndented = true });
        fingerprints.Add(("USB ID input", imported.Source));
    }
    else if (pciImport)
    {
        if (!long.TryParse(args[2], out long revision) || !DateOnly.TryParseExact(args[3], "yyyy-MM-dd", out DateOnly publishedOn))
            throw new ArgumentException("PCI ID import requires a positive revision and an exact YYYY-MM-DD publication date.");
        var imported = await PciIdsImporter.BuildAsync(args[1], revision, publishedOn, args[4], stop.Token);
        outputPath = args[5];
        output = JsonSerializer.SerializeToUtf8Bytes(imported.Pack, new JsonSerializerOptions { WriteIndented = true });
        fingerprints.Add(("PCI ID input", imported.Source));
    }
    else if (referenceImport)
    {
        if (!long.TryParse(args[3], out long revision) || !DateOnly.TryParseExact(args[4], "yyyy-MM-dd", out DateOnly publishedOn))
            throw new ArgumentException("Reference import requires a positive revision and an exact YYYY-MM-DD publication date.");
        var imported = await ReferenceCsvImporter.BuildAsync(args[1], args[2], revision, publishedOn, args[5], stop.Token);
        outputPath = args[6];
        output = JsonSerializer.SerializeToUtf8Bytes(imported.Pack, new JsonSerializerOptions { WriteIndented = true });
        fingerprints.Add(("Reference input", imported.Source));
    }
    else if (driverImport)
    {
        if (!long.TryParse(args[3], out long revision) || !DateOnly.TryParseExact(args[4], "yyyy-MM-dd", out DateOnly publishedOn))
            throw new ArgumentException("Driver import requires a positive revision and an exact YYYY-MM-DD publication date.");
        var imported = await DriverReferenceImporter.BuildAsync(args[1], args[2], revision, publishedOn, args[5], stop.Token);
        outputPath = args[6];
        output = JsonSerializer.SerializeToUtf8Bytes(imported.Pack, new JsonSerializerOptions { WriteIndented = true });
        fingerprints.Add(("Driver input", imported.Source));
    }
    else if (ieeeImport)
    {
        if (!long.TryParse(args[4], out long revision) || !DateOnly.TryParseExact(args[5], "yyyy-MM-dd", out DateOnly publishedOn))
            throw new ArgumentException("IEEE import requires a positive revision and an exact YYYY-MM-DD publication date.");
        var imported = await IeeeOuiImporter.BuildAsync(args[1], args[2], args[3], revision, publishedOn, args[6], stop.Token);
        outputPath = args[7];
        output = JsonSerializer.SerializeToUtf8Bytes(imported.Pack, new JsonSerializerOptions { WriteIndented = true });
        fingerprints.AddRange(imported.Sources.Select(source => (source.Registry + " input", source.Bytes)));
    }
    else
    {
        await using var source = new FileStream(checkOnly ? args[1] : args[0], FileMode.Open, FileAccess.Read, FileShare.Read);
        if (source.Length is 0 or > 32_000_000) throw new InvalidDataException("Input exceeds catalog size limits.");
        var input = new byte[(int)source.Length];
        await source.ReadExactlyAsync(input, stop.Token);
        validated = KnowledgeCatalog.Read(input, stop.Token);
        if (checkOnly)
        {
            Console.WriteLine($"Valid schema: {validated.PackId} revision {validated.Revision}: {validated.Count} entries. Signature and factual accuracy not checked.");
            return 0;
        }
        var pack = JsonSerializer.Deserialize<KnowledgePack>(input)!;
        var normalized = pack with { Entries = pack.Entries.OrderBy(e => e.Context, StringComparer.Ordinal)
            .ThenBy(e => e.Code, StringComparer.Ordinal).ToArray() };
        output = JsonSerializer.SerializeToUtf8Bytes(normalized, new JsonSerializerOptions { WriteIndented = true });
        outputPath = args[1];
        fingerprints.Add(("Input", input));
    }
    validated = KnowledgeCatalog.Read(output, stop.Token);
    stop.Token.ThrowIfCancellationRequested();
    await using var destination = new FileStream(outputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    outputCreated = true;
    await destination.WriteAsync(output, stop.Token);
    await destination.FlushAsync(stop.Token);
    destination.Flush(true);
    Console.WriteLine($"Built {validated.PackId} revision {validated.Revision}: {validated.Count} entries. Unsigned; not published.");
    foreach (var source in fingerprints)
        Console.WriteLine(source.Label + " SHA-256: " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(source.Bytes)));
    Console.WriteLine("Output SHA-256: " + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(output)));
    Console.WriteLine("Output bytes: " + output.Length);
    return 0;
}
catch (Exception error) when (error is IOException or InvalidDataException or JsonException or UnauthorizedAccessException or
    ArgumentException or OperationCanceledException or PlatformNotSupportedException)
{
    Console.Error.WriteLine("Build failed: " + error.Message + (outputCreated
        ? " The new output may be incomplete; do not publish it."
        : " No output file was created or overwritten."));
    return error is OperationCanceledException && userCanceled ? 130 : 1;
}
finally
{
    Console.CancelKeyPress -= cancel;
}

static bool TryParseStatus(string value, out uint status)
{
    status = 0;
    return value.Length == 10 && value.StartsWith("0x", StringComparison.OrdinalIgnoreCase) &&
        uint.TryParse(value.AsSpan(2), System.Globalization.NumberStyles.AllowHexSpecifier,
            System.Globalization.CultureInfo.InvariantCulture, out status);
}
