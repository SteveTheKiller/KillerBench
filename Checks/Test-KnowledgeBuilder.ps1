param([string]$Configuration = 'Debug')
$ErrorActionPreference = 'Stop'
$builder = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../Tools/Knowledge/bin/$Configuration/net10.0/KillerBench.KnowledgeBuilder.dll"))
$inputFile = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Knowledge/windows-win32-starter.json'))
$scratch = Join-Path ([IO.Path]::GetTempPath()) ('KillerBench-catalog-' + [guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($scratch) | Out-Null
function Invoke-Builder([string]$Arguments, [int]$ExpectedExit) {
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = (Get-Command dotnet).Source
    $start.Arguments = '"' + $builder + '" ' + $Arguments
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    $process = New-Object Diagnostics.Process
    $process.StartInfo = $start
    try {
        if (-not $process.Start()) { throw 'Builder did not start.' }
        $stdout = $process.StandardOutput.ReadToEndAsync()
        $stderr = $process.StandardError.ReadToEndAsync()
        if (-not $process.WaitForExit(35000)) { $process.Kill(); throw 'Builder exceeded its deadline.' }
        $message = $stdout.GetAwaiter().GetResult() + $stderr.GetAwaiter().GetResult()
        if ($process.ExitCode -ne $ExpectedExit) { throw "Unexpected builder result: $message" }
        return $message
    } finally { $process.Dispose() }
}
$before = (Get-FileHash $inputFile).Hash
$message = Invoke-Builder ('--check "' + $inputFile + '"') 0
if ($message -notmatch 'Signature and factual accuracy not checked') { throw 'Validation scope missing.' }
$first = Join-Path $scratch 'first.json'
$second = Join-Path $scratch 'second.json'
$built = Invoke-Builder ('"' + $inputFile + '" "' + $first + '"') 0
if ($built -notmatch ('Input SHA-256: ' + $before) -or
    $built -notmatch ('Output SHA-256: ' + (Get-FileHash $first).Hash) -or
    $built -notmatch ('Output bytes: ' + (Get-Item $first).Length)) { throw 'Builder fingerprints do not match source and output files.' }
Invoke-Builder ('"' + $first + '" "' + $second + '"') 0 | Out-Null
if ((Get-FileHash $first).Hash -ne (Get-FileHash $second).Hash) { throw 'Normalization is not repeatable.' }
$outputHash = (Get-FileHash $first).Hash
Invoke-Builder ('"' + $inputFile + '" "' + $first + '"') 1 | Out-Null
if ((Get-FileHash $first).Hash -ne $outputHash) { throw 'Existing output changed.' }
$invalid = Join-Path $scratch 'invalid.json'
[IO.File]::WriteAllText($invalid, '{"SchemaVersion":1,"SchemaVersion":2}')
Invoke-Builder ('--check "' + $invalid + '"') 1 | Out-Null
Invoke-Builder ('"' + $invalid + '" "' + (Join-Path $scratch 'rejected.json') + '"') 1 | Out-Null
if (Test-Path (Join-Path $scratch 'rejected.json')) { throw 'Invalid input created output.' }
$mal = Join-Path $scratch 'oui.csv'
$mam = Join-Path $scratch 'mam.csv'
$mas = Join-Path $scratch 'oui36.csv'
[IO.File]::WriteAllText($mal, "Registry,Assignment,Organization Name,Organization Address`r`nMA-L,001122,`"Fixture, Incorporated`",Address`r`n")
[IO.File]::WriteAllText($mam, "Registry,Assignment,Organization Name,Organization Address`nMA-M,3344556,Fixture Medium,Address`n")
[IO.File]::WriteAllText($mas, "Registry,Assignment,Organization Name,Organization Address`nMA-S,778899AAB,`"Fixture `"`"Small`"`"`",Address`n")
$ouiOutput = Join-Path $scratch 'ieee.json'
$ouiBuilt = Invoke-Builder ('--ieee-oui "' + $mal + '" "' + $mam + '" "' + $mas + '" 7 2026-09-14 "License review required" "' + $ouiOutput + '"') 0
$oui = Get-Content -Raw $ouiOutput | ConvertFrom-Json
if ($oui.Id -ne 'ieee-mac-vendors' -or $oui.Revision -ne 7 -or $oui.Entries.Count -ne 3 -or
    $oui.Entries[0].Code -ne '001122' -or $oui.Entries[0].Title -ne 'Fixture, Incorporated' -or
    $oui.Entries[2].Code -ne '778899AAB' -or $oui.Entries[2].Title -ne 'Fixture "Small"' -or
    $ouiBuilt -notmatch 'MA-L input SHA-256:' -or $ouiBuilt -notmatch 'Unsigned; not published') { throw 'IEEE OUI fixture import is incomplete or nondeterministic.' }
$pnpRegistry = Join-Path $scratch 'pnp-registry.csv'
$acpiRegistry = Join-Path $scratch 'acpi-registry.csv'
[IO.File]::WriteAllText($pnpRegistry, "Company,PNP ID,Approved On Date`n`"Fixture Displays, Inc.`",FXD,9/14/2026`n,AET,1996-12-13`n")
[IO.File]::WriteAllText($acpiRegistry, "Company,ACPI ID,Approved On Date`nFixture Firmware,FXFW,09/14/2026`n")
$uefiOutput = Join-Path $scratch 'uefi-vendors.json'
$uefiBuilt = Invoke-Builder ('--uefi-vendor-ids "' + $pnpRegistry + '" "' + $acpiRegistry + '" 9 2026-09-14 "License review required" "' + $uefiOutput + '"') 0
$uefi = Get-Content -Raw $uefiOutput | ConvertFrom-Json
if ($uefi.Id -ne 'uefi-pnp-acpi-vendors' -or $uefi.Revision -ne 9 -or $uefi.Entries.Count -ne 3 -or
    $uefi.Entries[0].Context -ne 'acpi-vendor' -or $uefi.Entries[0].Code -ne 'FXFW' -or
    $uefi.Entries[1].Context -ne 'pnp-vendor' -or $uefi.Entries[1].Code -ne 'AET' -or
    $uefi.Entries[1].Title -notmatch 'company not listed' -or $uefi.Entries[2].Title -ne 'Fixture Displays, Inc.' -or
    $uefiBuilt -notmatch 'PNP registry input SHA-256:' -or $uefiBuilt -notmatch 'ACPI registry input SHA-256:' -or
    $uefiBuilt -notmatch 'Unsigned; not published') { throw 'UEFI vendor ID fixture import is incomplete or nondeterministic.' }
$duplicatePnp = Join-Path $scratch 'duplicate-pnp.csv'
[IO.File]::WriteAllText($duplicatePnp, "Company,PNP ID,Approved On Date`nFirst,ABC,2026-09-14`nDuplicate,ABC,2026-09-14`n")
Invoke-Builder ('--uefi-vendor-ids "' + $duplicatePnp + '" "' + $acpiRegistry + '" 10 2026-09-14 "License review required" "' + (Join-Path $scratch 'duplicate-uefi.json') + '"') 1 | Out-Null
$invalidAcpi = Join-Path $scratch 'invalid-acpi.csv'
[IO.File]::WriteAllText($invalidAcpi, "Company,ACPI ID,Approved On Date`nInvalid,abc!,2026-09-14`n")
Invoke-Builder ('--uefi-vendor-ids "' + $pnpRegistry + '" "' + $invalidAcpi + '" 10 2026-09-14 "License review required" "' + (Join-Path $scratch 'invalid-uefi.json') + '"') 1 | Out-Null
$bluetoothYaml = Join-Path $scratch 'company_identifiers.yaml'
[IO.File]::WriteAllText($bluetoothYaml, "company_identifiers:`n`n  - value: 0x0006`n    name: 'Fixture Bluetooth Company'`n`n  - value: 0x1234`n    name: `"Fixture's Bluetooth Company`"`n")
$bluetoothOutput = Join-Path $scratch 'bluetooth.json'
$bluetoothBuilt = Invoke-Builder ('--bluetooth-company-ids "' + $bluetoothYaml + '" 10 2026-09-14 "License review required" "' + $bluetoothOutput + '"') 0
$bluetooth = Get-Content -Raw $bluetoothOutput | ConvertFrom-Json
if ($bluetooth.Id -ne 'bluetooth-company-identifiers' -or $bluetooth.Revision -ne 10 -or $bluetooth.Entries.Count -ne 2 -or
    $bluetooth.Entries[0].Code -ne '0x0006' -or $bluetooth.Entries[0].Title -ne 'Fixture Bluetooth Company' -or
    $bluetooth.Entries[1].Code -ne '0x1234' -or $bluetooth.Entries[1].Title -ne "Fixture's Bluetooth Company" -or
    $bluetoothBuilt -notmatch 'Bluetooth company identifier input SHA-256:' -or
    $bluetoothBuilt -notmatch 'Unsigned; not published') { throw 'Bluetooth company identifier fixture import is incomplete or nondeterministic.' }
$duplicateBluetooth = Join-Path $scratch 'duplicate-bluetooth.yaml'
[IO.File]::WriteAllText($duplicateBluetooth, "company_identifiers:`n  - value: 0x0006`n    name: 'First'`n  - value: 0x0006`n    name: 'Duplicate'`n")
Invoke-Builder ('--bluetooth-company-ids "' + $duplicateBluetooth + '" 11 2026-09-14 "License review required" "' + (Join-Path $scratch 'duplicate-bluetooth.json') + '"') 1 | Out-Null
$invalidBluetooth = Join-Path $scratch 'invalid-bluetooth.yaml'
[IO.File]::WriteAllText($invalidBluetooth, "company_identifiers:`n  - value: 0x0006`n    name: Unquoted`n")
Invoke-Builder ('--bluetooth-company-ids "' + $invalidBluetooth + '" 11 2026-09-14 "License review required" "' + (Join-Path $scratch 'invalid-bluetooth.json') + '"') 1 | Out-Null
$pciIds = Join-Path $scratch 'pci.ids'
[IO.File]::WriteAllText($pciIds, "# synthetic pci.ids fixture`n1234  Fixture Vendor`n`t5678  Fixture Device`n`t`t0001 0002  Ignored subsystem`nABCD  Other Vendor`nC 00  Class section ignored`n")
$pciOutput = Join-Path $scratch 'pci.json'
$pciBuilt = Invoke-Builder ('--pci-ids "' + $pciIds + '" 11 2026-09-14 "License review required" "' + $pciOutput + '"') 0
$pci = Get-Content -Raw $pciOutput | ConvertFrom-Json
if ($pci.Id -ne 'pci-hardware-vendors' -or $pci.Revision -ne 11 -or $pci.Entries.Count -ne 3 -or
    $pci.Entries[0].Context -ne 'pci-device' -or $pci.Entries[0].Code -ne 'VEN_1234&DEV_5678' -or
    $pci.Entries[1].Code -ne 'VEN_1234' -or $pci.Entries[2].Code -ne 'VEN_ABCD' -or
    $pciBuilt -notmatch 'PCI ID input SHA-256:' -or $pciBuilt -notmatch 'Unsigned; not published') { throw 'PCI ID fixture import is incomplete or nondeterministic.' }
$duplicatePci = Join-Path $scratch 'duplicate-pci.ids'
[IO.File]::WriteAllText($duplicatePci, "1234  First`n1234  Duplicate`n")
Invoke-Builder ('--pci-ids "' + $duplicatePci + '" 12 2026-09-14 "License review required" "' + (Join-Path $scratch 'duplicate-pci.json') + '"') 1 | Out-Null
$usbIds = Join-Path $scratch 'usb.ids'
[IO.File]::WriteAllText($usbIds, "# synthetic usb.ids fixture`n1234  Fixture USB Vendor`n`t5678  Fixture USB Product`n`t`t01  Ignored interface`nABCD  Other USB Vendor`nC 00  Class section ignored`n")
$usbOutput = Join-Path $scratch 'usb.json'
$usbBuilt = Invoke-Builder ('--usb-ids "' + $usbIds + '" 12 2026-09-14 "License review required" "' + $usbOutput + '"') 0
$usb = Get-Content -Raw $usbOutput | ConvertFrom-Json
if ($usb.Id -ne 'usb-hardware-vendors' -or $usb.Revision -ne 12 -or $usb.Entries.Count -ne 3 -or
    $usb.Entries[0].Context -ne 'usb-device' -or $usb.Entries[0].Code -ne 'VID_1234&PID_5678' -or
    $usb.Entries[1].Code -ne 'VID_1234' -or $usb.Entries[2].Code -ne 'VID_ABCD' -or
    $usbBuilt -notmatch 'USB ID input SHA-256:' -or $usbBuilt -notmatch 'Unsigned; not published') { throw 'USB ID fixture import is incomplete or nondeterministic.' }
$duplicateUsb = Join-Path $scratch 'duplicate-usb.ids'
[IO.File]::WriteAllText($duplicateUsb, "1234  First`n1234  Duplicate`n")
Invoke-Builder ('--usb-ids "' + $duplicateUsb + '" 13 2026-09-14 "License review required" "' + (Join-Path $scratch 'duplicate-usb.json') + '"') 1 | Out-Null
$bugChecks = Join-Path $scratch 'bug-check-code-reference2.md'
[IO.File]::WriteAllText($bugChecks, "# Bug check code reference`n`n| Code | Name and reference link |`n| --- | --- |`n| 0x000000D1 | [**DRIVER\_IRQL\_NOT\_LESS\_OR\_EQUAL**](bug-check-0xd1--driver-irql-not-less-or-equal.md) |`n| 0x000000DF | I[**MPERSONATING\_WORKER\_THREAD**](bug-check-0xdf--impersonating-worker-thread.md) |`n| 0x0000000A | [**IRQL_NOT_LESS_OR_EQUAL**](bug-check-0xa--irql-not-less-or-equal.md) |`n")
$bugCheckOutput = Join-Path $scratch 'bugchecks.json'
$bugCheckBuilt = Invoke-Builder ('--bugcheck-markdown "' + $bugChecks + '" 14 2026-09-14 "License review required" "' + $bugCheckOutput + '"') 0
$bugCheckPack = Get-Content -Raw $bugCheckOutput | ConvertFrom-Json
if ($bugCheckPack.Id -ne 'windows-bug-checks' -or $bugCheckPack.Revision -ne 14 -or $bugCheckPack.Entries.Count -ne 3 -or
    $bugCheckPack.Entries[0].Context -ne 'bugcheck' -or $bugCheckPack.Entries[0].Code -ne '0x0000000A' -or
    $bugCheckPack.Entries[1].Title -ne 'DRIVER_IRQL_NOT_LESS_OR_EQUAL' -or
    $bugCheckPack.Entries[1].SourceUrl -ne 'https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/bug-check-0xd1--driver-irql-not-less-or-equal' -or
    $bugCheckPack.Entries[2].Title -ne 'IMPERSONATING_WORKER_THREAD' -or
    $bugCheckBuilt -notmatch 'Bug-check reference input SHA-256:' -or $bugCheckBuilt -notmatch 'Unsigned; not published') {
    throw 'Bug-check Markdown fixture import is incomplete or nondeterministic.'
}
$duplicateBugChecks = Join-Path $scratch 'duplicate-bugchecks.md'
[IO.File]::WriteAllText($duplicateBugChecks, "| 0xA | [**FIRST_CODE**](bug-check-0xa--first.md) |`n| 0x0000000A | [**SECOND_CODE**](bug-check-0xa--second.md) |`n")
Invoke-Builder ('--bugcheck-markdown "' + $duplicateBugChecks + '" 15 2026-09-14 "License review required" "' + (Join-Path $scratch 'duplicate-bugchecks.json') + '"') 1 | Out-Null
$invalidBugChecks = Join-Path $scratch 'invalid-bugchecks.md'
[IO.File]::WriteAllText($invalidBugChecks, "| 0xA | [**IRQL_NOT_LESS_OR_EQUAL**](bug-check-../../outside.md) |`n")
Invoke-Builder ('--bugcheck-markdown "' + $invalidBugChecks + '" 15 2026-09-14 "License review required" "' + (Join-Path $scratch 'invalid-bugchecks.json') + '"') 1 | Out-Null
$unsupportedBugChecks = Join-Path $scratch 'unsupported-bugchecks.md'
[IO.File]::WriteAllText($unsupportedBugChecks, "| 0xDF | X[**MPERSONATING\_WORKER\_THREAD**](bug-check-0xdf--impersonating-worker-thread.md) |`n")
Invoke-Builder ('--bugcheck-markdown "' + $unsupportedBugChecks + '" 15 2026-09-14 "License review required" "' + (Join-Path $scratch 'unsupported-bugchecks.json') + '"') 1 | Out-Null
$driverCsv = Join-Path $scratch 'drivers.csv'
$driverHeader = 'Context,Code,Title,Explanation,SourceUrl,AppliesTo,HardwareId,Architecture,WindowsProduct,MinimumBuild,MaximumBuild,Vendor,DriverVersion,DownloadUrl'
[IO.File]::WriteAllText($driverCsv, $driverHeader + "`nnetwork,fixture-1,Fixture adapter,Reviewed fixture,https://example.com/reference,Fixture Windows builds,PCI\VEN_1234&DEV_5678,x64,client,22000,26100,Fixture Vendor,1.2.3,https://example.com/driver.exe`n")
$driverOutput = Join-Path $scratch 'drivers.json'
$driverBuilt = Invoke-Builder ('--driver-csv "' + $driverCsv + '" fixture-drivers 4 2026-09-14 "Synthetic test data" "' + $driverOutput + '"') 0
$drivers = Get-Content -Raw $driverOutput | ConvertFrom-Json
if ($drivers.SchemaVersion -ne 2 -or $drivers.Id -ne 'fixture-drivers' -or $drivers.Entries.Count -ne 1 -or
    $drivers.Entries[0].Driver.HardwareId -ne 'PCI\VEN_1234&DEV_5678' -or $drivers.Entries[0].Driver.MinimumBuild -ne 22000 -or
    $driverBuilt -notmatch 'Driver input SHA-256:' -or $driverBuilt -notmatch 'Unsigned; not published') { throw 'Driver fixture import is incomplete or nondeterministic.' }
$invalidDriver = Join-Path $scratch 'invalid-driver.csv'
[IO.File]::WriteAllText($invalidDriver, $driverHeader + "`nnetwork,fixture-1,Fixture adapter,Reviewed fixture,https://example.com/reference,Fixture Windows builds,PCI\VEN_1234&DEV_5678,any,client,22000,26100,Fixture Vendor,1.2.3,http://example.com/driver.exe`n")
Invoke-Builder ('--driver-csv "' + $invalidDriver + '" fixture-drivers 5 2026-09-14 "Synthetic test data" "' + (Join-Path $scratch 'invalid-drivers.json') + '"') 1 | Out-Null
$referenceCsv = Join-Path $scratch 'references.csv'
$referenceHeader = 'Context,Code,Title,Explanation,SourceUrl,AppliesTo'
[IO.File]::WriteAllText($referenceCsv, $referenceHeader + "`nwindows-win32,5,Access denied,`"Reviewed fixture, with comma`",https://example.com/win32-5,Fixture Windows builds`nvendor-printer,E42,Fixture vendor fault,Reviewed vendor fixture,https://example.com/vendor-e42,Fixture printer models`n")
$referenceOutput = Join-Path $scratch 'references.json'
$referenceBuilt = Invoke-Builder ('--reference-csv "' + $referenceCsv + '" fixture-references 9 2026-09-14 "Synthetic reviewed data" "' + $referenceOutput + '"') 0
$references = Get-Content -Raw $referenceOutput | ConvertFrom-Json
if ($references.SchemaVersion -ne 1 -or $references.Id -ne 'fixture-references' -or $references.Revision -ne 9 -or
    $references.Entries.Count -ne 2 -or $references.Entries[0].Context -ne 'vendor-printer' -or
    $references.Entries[1].Code -ne '5' -or $references.Entries[1].Explanation -ne 'Reviewed fixture, with comma' -or
    $referenceBuilt -notmatch 'Reference input SHA-256:' -or $referenceBuilt -notmatch 'Unsigned; not published') { throw 'Reference CSV fixture import is incomplete or nondeterministic.' }
$duplicateReference = Join-Path $scratch 'duplicate-reference.csv'
[IO.File]::WriteAllText($duplicateReference, $referenceHeader + "`nwindows-win32,5,First,Fixture,https://example.com/first,Fixture`nwindows-win32,5,Second,Fixture,https://example.com/second,Fixture`n")
Invoke-Builder ('--reference-csv "' + $duplicateReference + '" fixture-references 10 2026-09-14 "Synthetic reviewed data" "' + (Join-Path $scratch 'duplicate-reference.json') + '"') 1 | Out-Null
$invalidReference = Join-Path $scratch 'invalid-reference.csv'
[IO.File]::WriteAllText($invalidReference, $referenceHeader + "`nwindows-win32,5,Invalid,Fixture,http://example.com/insecure,Fixture`n")
Invoke-Builder ('--reference-csv "' + $invalidReference + '" fixture-references 10 2026-09-14 "Synthetic reviewed data" "' + (Join-Path $scratch 'invalid-reference.json') + '"') 1 | Out-Null
$windowsMessages = Join-Path $scratch 'windows-messages.csv'
$windowsBuilt = Invoke-Builder ('--windows-message-csv 0 5 2026-09-14 "' + $windowsMessages + '"') 0
$windowsRows = Import-Csv -LiteralPath $windowsMessages
if ($windowsRows.Count -ne 6 -or $windowsRows[0].Context -ne 'win32' -or $windowsRows[0].Code -ne '0' -or
    $windowsRows[5].Code -ne '5' -or $windowsRows[5].Explanation -notmatch 'denied' -or
    ($windowsRows | Where-Object { $_.SourceUrl -notmatch '^https://learn\.microsoft\.com/' -or $_.AppliesTo -notmatch 'Not automatically equivalent' })) {
    throw 'Installed Windows message review CSV is incomplete or ambiguous.'
}
if ($windowsBuilt -notmatch 'No knowledge pack was built or published' -or
    $windowsBuilt -notmatch ('Output SHA-256: ' + (Get-FileHash $windowsMessages).Hash)) {
    throw 'Windows message generator did not retain its review-only scope and fingerprint.'
}
$windowsHash = (Get-FileHash $windowsMessages).Hash
Invoke-Builder ('--windows-message-csv 0 5 2026-09-14 "' + $windowsMessages + '"') 1 | Out-Null
if ((Get-FileHash $windowsMessages).Hash -ne $windowsHash) { throw 'Existing Windows message review CSV changed.' }
foreach ($invalidRange in @('-1 5 2026-09-14', '6 5 2026-09-14', '0 16000 2026-09-14', '0 5 9999-01-01')) {
    Invoke-Builder ('--windows-message-csv ' + $invalidRange + ' "' + (Join-Path $scratch ([guid]::NewGuid().ToString('N') + '.csv')) + '"') 1 | Out-Null
}
$ntStatusMessages = Join-Path $scratch 'ntstatus-messages.csv'
$ntStatusBuilt = Invoke-Builder ('--ntstatus-message-csv 0xC0000005 0xC0000005 2026-09-14 "' + $ntStatusMessages + '"') 0
$ntStatusRows = @(Import-Csv -LiteralPath $ntStatusMessages)
if ($ntStatusRows.Count -ne 1 -or $ntStatusRows[0].Context -ne 'ntstatus' -or $ntStatusRows[0].Code -ne '0xC0000005' -or
    [string]::IsNullOrWhiteSpace($ntStatusRows[0].Explanation) -or
    $ntStatusRows[0].SourceUrl -notmatch '^https://learn\.microsoft\.com/' -or
    $ntStatusRows[0].AppliesTo -notmatch 'Not automatically equivalent') {
    throw 'Installed NTSTATUS message review CSV is incomplete or ambiguous.'
}
if ($ntStatusBuilt -notmatch 'No knowledge pack was built or published' -or
    $ntStatusBuilt -notmatch ('Output SHA-256: ' + (Get-FileHash $ntStatusMessages).Hash)) {
    throw 'NTSTATUS message generator did not retain its review-only scope and fingerprint.'
}
$ntStatusHash = (Get-FileHash $ntStatusMessages).Hash
Invoke-Builder ('--ntstatus-message-csv 0xC0000005 0xC0000005 2026-09-14 "' + $ntStatusMessages + '"') 1 | Out-Null
if ((Get-FileHash $ntStatusMessages).Hash -ne $ntStatusHash) { throw 'Existing NTSTATUS message review CSV changed.' }
foreach ($invalidRange in @('C0000005 0xC0000005 2026-09-14', '0xC0000006 0xC0000005 2026-09-14', '0x00000000 0x00010000 2026-09-14', '0xC0000005 0xC0000005 9999-01-01')) {
    Invoke-Builder ('--ntstatus-message-csv ' + $invalidRange + ' "' + (Join-Path $scratch ([guid]::NewGuid().ToString('N') + '.csv')) + '"') 1 | Out-Null
}
$hResultMessages = Join-Path $scratch 'hresult-messages.csv'
$hResultBuilt = Invoke-Builder ('--hresult-message-csv 0x80070005 0x80070005 2026-09-14 "' + $hResultMessages + '"') 0
$hResultRows = @(Import-Csv -LiteralPath $hResultMessages)
if ($hResultRows.Count -ne 1 -or $hResultRows[0].Context -ne 'hresult' -or $hResultRows[0].Code -ne '0x80070005' -or
    $hResultRows[0].Explanation -notmatch 'denied' -or
    $hResultRows[0].SourceUrl -notmatch '^https://learn\.microsoft\.com/' -or
    $hResultRows[0].AppliesTo -notmatch 'Not automatically equivalent') {
    throw 'Installed HRESULT message review CSV is incomplete or ambiguous.'
}
if ($hResultBuilt -notmatch 'No knowledge pack was built or published' -or
    $hResultBuilt -notmatch ('Output SHA-256: ' + (Get-FileHash $hResultMessages).Hash)) {
    throw 'HRESULT message generator did not retain its review-only scope and fingerprint.'
}
$hResultHash = (Get-FileHash $hResultMessages).Hash
Invoke-Builder ('--hresult-message-csv 0x80070005 0x80070005 2026-09-14 "' + $hResultMessages + '"') 1 | Out-Null
if ((Get-FileHash $hResultMessages).Hash -ne $hResultHash) { throw 'Existing HRESULT message review CSV changed.' }
foreach ($invalidRange in @('80070005 0x80070005 2026-09-14', '0x80070006 0x80070005 2026-09-14', '0x80070000 0x80080000 2026-09-14', '0x80070005 0x80070005 9999-01-01')) {
    Invoke-Builder ('--hresult-message-csv ' + $invalidRange + ' "' + (Join-Path $scratch ([guid]::NewGuid().ToString('N') + '.csv')) + '"') 1 | Out-Null
}
$duplicate = Join-Path $scratch 'duplicate.csv'
[IO.File]::WriteAllText($duplicate, "Registry,Assignment,Organization Name`nMA-L,001122,First`nMA-L,001122,Duplicate`n")
Invoke-Builder ('--ieee-oui "' + $duplicate + '" "' + $mam + '" "' + $mas + '" 8 2026-09-14 "License review required" "' + (Join-Path $scratch 'duplicate.json') + '"') 1 | Out-Null
Invoke-Builder '--check' 2 | Out-Null
if ((Get-FileHash $inputFile).Hash -ne $before) { throw 'Source changed.' }
Write-Output "PASS: read-only validation, repeatable normalization, IEEE OUI, UEFI PNP and ACPI vendor IDs, Bluetooth company IDs, PCI ID, USB ID, bug-check Markdown, driver, reviewed reference, Win32, NTSTATUS and HRESULT message CSV generation, existing-output preservation and malformed-input rejection. Scratch: $scratch"
