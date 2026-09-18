param([string]$Configuration = 'Debug', [string[]]$Only = @())
$ErrorActionPreference = 'Stop'
$executed = @()
$cli = Join-Path $PSScriptRoot "../Cli/bin/$Configuration/net10.0/killerbench-cli.dll"
if (-not (Test-Path -LiteralPath $cli)) { throw 'Build the CLI before running these read-only checks.' }
foreach ($command in @('crash-review', 'bits-jobs', 'backup-events', 'vss-providers', 'common-check', 'security-check', 'device-check problems', 'device-check input', 'default-printer', 'printer-conditions', 'printer-queue-attributes', 'printer-drivers', 'printer-driver-files', 'print-extension-files', 'printer-driver-models', 'printer-ports', 'vitals-check', 'device-check audio', 'device-check network', 'device-check storage', 'device-check printing', 'device-check DISPLAY', 'task-check', 'event-channels', 'network-firewall', 'firewall-events', 'firewall-service-events', 'network-service-events', 'storage-service-events', 'backup-service-events', 'feature-service-events', 'platform-service-events', 'infrastructure-service-events', 'dns-client-config', 'dns-client-events', 'dns-nrpt-policy', 'windows-capabilities', 'route-priority', 'ip-interface-config', 'dhcp-check', 'dhcp-leases', 'dhcp-events', 'neighbor-cache', 'adapter-bindings', 'adapter-statistics', 'tcp-port-pressure', 'tcp-settings', 'winsock-catalog', 'winsock-namespaces', 'winsock-provider-files', 'winsock-readiness', 'pagefile-check', 'reserved-storage', 'storage-sense-policy', 'storage-sense-task', 'disk-cleanup-task', 'disk-diagnostic-tasks', 'disk-diagnostic', 'physical-disks', 'delete-notifications', 'smb-shares', 'smb-server-config', 'smb-client-config', 'smb-connections', 'installer-check', 'installer-engine-files', 'installer-service-registration', 'installer-security-policy', 'installer-logging', 'installer-activity', 'installer-pending-state', 'msi-sources', 'installer-events', 'installer-service-events', 'print-events', 'print-operational-events', 'print-spooler-events', 'print-policy', 'print-rendering-policy', 'service-events', 'service-binaries', 'service-dlls', 'service-recovery-policy', 'service-transitions', 'storage-events', 'time-events', 'time-config', 'application-events', 'service-check --all', 'service-check Spooler spooler Dhcp', 'service-check EventLog eventlog', "volume-capacity $env:SystemDrive", 'proxy-check', 'delivery-optimization', 'update-policy', 'component-repair-source', 'update-engine-files', 'update-service-files', 'update-service-events', 'servicing-service-states', 'servicing-service-events', 'update-followup 0x80246008', 'update-services', 'update-history', 'installed-hotfixes', 'restart-check', 'pending-file-operations', 'licensing-check')) {
    if ($Only.Count -gt 0 -and $command -notin $Only) { continue }
    $executed += $command
    foreach ($extra in @('', ' --execute')) {
        $start = New-Object System.Diagnostics.ProcessStartInfo
        $start.FileName = (Get-Command dotnet -ErrorAction Stop).Source
        $start.Arguments = '"' + $cli + '" ' + $command + $extra
        $start.UseShellExecute = $false
        $start.CreateNoWindow = $true
        $start.WindowStyle = [Diagnostics.ProcessWindowStyle]::Hidden
        $start.RedirectStandardOutput = $true
        $start.RedirectStandardError = $true
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $start
        try {
            if (-not $process.Start()) { throw 'Diagnostic did not start.' }
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            $limit = if ($command -eq 'common-check') { 300000 } elseif ($command -eq 'windows-capabilities') { 130000 } elseif ($command.StartsWith('update-followup ')) { 120000 } elseif ($command -in @('backup-events', 'bits-jobs', 'printer-conditions', 'printer-queue-attributes', 'printer-ports', 'printer-driver-files', 'print-extension-files', 'print-spooler-events', 'installer-service-events', 'printer-driver-models', 'dns-nrpt-policy', 'route-priority', 'ip-interface-config', 'dhcp-leases', 'dhcp-events', 'neighbor-cache', 'adapter-bindings', 'adapter-statistics', 'pagefile-check', 'reserved-storage')) { 55000 } else { 30000 }
            if (-not $process.WaitForExit($limit)) {
                $process.Kill()
                throw "$command exceeded the CLI check time limit."
            }
            $body = $stdout.GetAwaiter().GetResult()
            $messages = $stderr.GetAwaiter().GetResult()
            if ($extra) {
                if ($process.ExitCode -ne 2 -or $body.Length -ne 0 -or $messages -notmatch 'Usage:') {
                    throw "$command failed argument rejection."
                }
            } else {
                if ($command -eq 'task-check') {
                    $report = $body | ConvertFrom-Json
                    if ($null -eq $report.Evidence -or $report.Complete -isnot [bool] -or @($report.Tasks).Count -ne @($report.Findings).Count) {
                        throw 'Task diagnostic lost inventory, coverage or raw evidence.'
                    }
                    foreach ($finding in $report.Findings) {
                        if ($null -ne $finding.Repair -or [string]::IsNullOrWhiteSpace($finding.Detail)) {
                            throw 'Task diagnostic returned invalid or mutating findings.'
                        }
                    }
                    $expected = if ($report.Complete -and @($report.Findings | Where-Object State -ne 'Information').Count -eq 0) { 0 } else { 3 }
                    if ($process.ExitCode -ne $expected) { throw 'Task diagnostic exit disagrees with evidence.' }
                    Write-Output "task-check : $(@($report.Findings).Count) findings, exit $expected"
                    continue
                }
                if ($command.StartsWith('update-followup ')) {
                    $report = $body | ConvertFrom-Json
                    $checks = @($report.Checks)
                    if ($report.HexCode -ne '0x80246008' -or $report.Reference.Symbol -ne 'WU_E_DM_FAILTOCONNECTTOBITS' -or $checks.Count -lt 8) {
                        throw 'Update follow-up lost its requested code, reference or service and transfer coverage.'
                    }
                    if (@($checks | Where-Object { $null -ne $_.Repair -or $_.Name -notlike 'Update follow-up: *' }).Count -ne 0) {
                        throw 'Update follow-up returned a mutating or unlabeled result.'
                    }
                    $expected = if (@($checks | Where-Object State -notin @('Clear', 'Information')).Count -eq 0) { 0 } else { 3 }
                    if ($process.ExitCode -ne $expected) { throw 'Update follow-up exit disagrees with evidence.' }
                    Write-Output "update-followup : $($checks.Count) checks, exit $expected"
                    continue
                }
                $checks = @((ConvertFrom-Json -InputObject ($body -join "`n")) | ForEach-Object { $_ })
                $groupedEvidenceNames = @()
                if ($command -eq 'common-check') {
                    foreach ($addedScope in @('Registered app packages', 'Scheduled task inventory')) {
                        $summaries = @($checks | Where-Object { $_.Name -eq $addedScope -and $null -ne $_.Evidence })
                        if ($summaries.Count -ne 1) {
                            throw "Combined scan lost $addedScope coverage."
                        }
                        $summary = $summaries[0]
                        if ($summary.Evidence.ExitCode -eq 0 -and -not $summary.Evidence.TimedOut -and
                            -not $summary.Evidence.Truncated -and [string]::IsNullOrWhiteSpace($summary.Evidence.Error)) {
                            $inventory = $summary.Evidence.Output | ConvertFrom-Json
                            if ($addedScope -eq 'Registered app packages') {
                                $groupedEvidenceNames += @($inventory.packages | ForEach-Object { $_.fullName })
                            } else {
                                $groupedEvidenceNames += @($inventory.tasks | ForEach-Object { $_.path + $_.name })
                            }
                        }
                    }
                    $securitySummary = @($checks | Where-Object { $_.Name -eq 'Registered antivirus products' -and $null -ne $_.Evidence })
                    if ($securitySummary.Count -ne 1) { throw 'Combined scan lost registered security-product coverage.' }
                    if ($securitySummary[0].Evidence.ExitCode -eq 0 -and -not $securitySummary[0].Evidence.TimedOut -and
                        -not $securitySummary[0].Evidence.Truncated -and [string]::IsNullOrWhiteSpace($securitySummary[0].Evidence.Error)) {
                        $securityInventory = $securitySummary[0].Evidence.Output | ConvertFrom-Json
                        $groupedEvidenceNames += @($securityInventory.products | ForEach-Object { 'Security product: ' + $_.name })
                    }
                    if (@($checks | Where-Object { $_.Name -like 'Printer port*' }).Count -eq 0) {
                        throw 'Combined scan lost printer port registration coverage.'
                    }
                    foreach ($historyName in @('Recent application errors', 'Recent storage events', 'Recent installer events', 'Recent service failures', 'Recent time-service events', 'Volume capacity', 'Recent unexpected shutdowns', 'Recent VSS events', 'Recent volsnap events', 'VSS writers', 'BITS transfer jobs')) {
                        if (@($checks | Where-Object Name -eq $historyName).Count -ne 1) {
                            throw "Combined scan lost history scope: $historyName"
                        }
                    }
                }
                $expectedCount = if ($command -eq 'infrastructure-service-events') { 29 } elseif ($command -eq 'platform-service-events') { 22 } elseif ($command -in @('servicing-service-states', 'servicing-service-events', 'network-service-events', 'feature-service-events')) { 6 } elseif ($command -eq 'security-check') { 5 } elseif ($command -in @('event-channels', 'dhcp-events', 'storage-service-events', 'backup-service-events', 'winsock-readiness')) { 4 } elseif ($command -eq 'installer-pending-state') { 3 } elseif ($command -eq 'service-check --all') { 80 } elseif ($command -eq 'vitals-check') { 10 } elseif ($command -eq 'disk-diagnostic') { 6 } elseif ($command -in @('backup-events', 'dns-client-events', 'firewall-service-events', 'service-check Spooler spooler Dhcp', 'proxy-check', 'disk-diagnostic-tasks')) { 2 } else { 1 }
                if ($command -eq 'crash-review') {
                    $expectedCount = 5
                    foreach ($name in @('Crash dump configuration', 'Crash dump files', 'Recent system crash reports', 'Reported dump file matches', 'Recent unexpected shutdowns')) {
                        if (@($checks | Where-Object Name -eq $name).Count -ne 1) { throw "Crash review lost scope: $name" }
                    }
                }
                if ($command -eq 'service-check --all' -and @($checks | Where-Object Name -eq 'Workstation').Count -ne 1) {
                    throw 'All-service inspection lost SMB client coverage.'
                }
                if ($command.StartsWith('device-check ') -or $command -in @('printer-conditions', 'printer-queue-attributes', 'printer-drivers', 'printer-driver-files', 'print-extension-files', 'printer-ports', 'network-firewall', 'smb-shares', 'physical-disks', 'common-check')) {
                    if ($checks.Count -lt 1) { throw "$command returned no coverage result." }
                } elseif ($checks.Count -ne $expectedCount) { throw "$command returned an unexpected number of checks." }
                if ($command -eq 'service-check EventLog eventlog' -and $checks[0].Name -ne 'WindowsEventLog') {
                    throw 'Event Log inspection returned a different service identity.'
                }
                if ($command -eq 'network-firewall' -and @($checks | Where-Object Name -eq 'Active network firewall alignment').Count -ne 1) {
                    throw 'Network firewall correlation lost its combined alignment result.'
                }
                foreach ($check in $checks) {
                    if ($command -eq 'component-repair-source' -and ($check.Name -ne 'Component repair source' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Managed policy was preserved' -or
                        $check.Detail -notmatch 'No repair source, update, servicing or registry setting changed')) {
                        throw 'Component repair source diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'neighbor-cache' -and ($check.Name -ne 'Neighbor cache state' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Neighbor addresses and link-layer identifiers are excluded' -or
                        $check.Evidence.Output -match 'IPAddress|LinkLayerAddress')) {
                        throw 'Neighbor cache diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'pending-file-operations' -and ($check.Name -ne 'Pending file operations' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'File paths and identities are excluded' -or
                        $check.Detail -notmatch 'registration was not cleared or changed' -or
                        $check.Evidence.Output -match '\\Temp\\|Program Files|Users\\')) {
                        throw 'Pending file operation diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'installer-pending-state' -and ($check.Name -notin @('Pending file operations', 'Installer activity', 'Installer pending-operation correlation') -or
                        $check.Detail -match '\\Temp\\|Program Files|Users\\' -or
                        $check.Name -eq 'Installer pending-operation correlation' -and
                        $check.Detail -notmatch 'does not authorize clearing a registration, ending a process, retrying an installation or restarting Windows')) {
                        throw 'Installer pending-state correlation lost its expected sources, privacy boundary or non-mutating scope.'
                    }
                    if ($command -eq 'ip-interface-config' -and ($check.Name -ne 'IP interface configuration' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No interface, address, MTU, metric, route, DHCP, forwarding, router-discovery, advertisement or policy setting changed')) {
                        throw 'IP interface configuration diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'storage-sense-policy' -and ($check.Name -ne 'Storage Sense machine policy' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'User settings, file identities and contents are excluded' -or
                        $check.Detail -notmatch 'No file, folder, cloud placeholder, Recycle Bin item, policy or Storage Sense setting changed')) {
                        throw 'Storage Sense policy diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'storage-sense-task' -and ($check.Name -ne 'Storage Sense scheduled task' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'does not prove cleanup ran' -or
                        $check.Detail -notmatch 'never authorizes task execution, schedule changes, cleanup, file removal or a Storage Sense setting change')) {
                        throw 'Storage Sense task diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'disk-cleanup-task' -and ($check.Name -ne 'Disk Cleanup scheduled task' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'does not prove cleanup ran' -or
                        $check.Detail -notmatch 'never authorizes task execution, schedule changes, cleanup, file removal or a storage setting change')) {
                        throw 'Disk Cleanup task diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'disk-diagnostic-tasks' -and ($check.Name -notin @('Disk diagnostic data collector task', 'Disk diagnostic resolver task') -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'does not prove a disk is healthy, failing or repaired' -or
                        $check.Detail -notmatch 'never authorizes task execution, schedule changes, disk tests, storage repair, formatting, cleanup, file removal or a storage setting change')) {
                        throw 'Disk diagnostic task inspection lost its exact identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'disk-diagnostic') {
                        $expectedNames = @('Disk diagnostic data collector task', 'Disk diagnostic resolver task', 'Disk diagnostic operational history', 'Disk diagnostic collector history', 'Disk diagnostic resolver history', 'Windows disk diagnostic correlation')
                        if ($check.Name -notin $expectedNames -or $check.Detail -notmatch 'No logging, task, disk, file or storage setting changed|never authorizes task execution|does not identify a failing disk') {
                            throw 'Combined disk diagnostic lost an exact source, privacy boundary or non-mutating outcome.'
                        }
                    }
                    if ($command -eq 'physical-disks' -and ($check.Name -notlike 'Physical disk:*' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Serial numbers, firmware data and unique hardware identifiers are excluded' -or
                        $check.Detail -notmatch 'No disk, pool, volume, file, driver or storage setting changed')) {
                        throw 'Physical-disk diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'dhcp-leases' -and ($check.Name -ne 'DHCP lease state' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No lease, address, adapter, route, resolver, service, policy or network setting changed')) {
                        throw 'DHCP lease diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'dhcp-events' -and ($check.Name -notlike 'Recent DHCP Client * events' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Ordinary summaries exclude those values' -or
                        $check.Detail -notmatch 'No lease renewal, release, adapter change, service start or network setting is selected')) {
                        throw 'DHCP Client event diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'dns-client-events' -and ($check.Name -notlike 'Recent DNS Client * events' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Ordinary summaries exclude those values' -or
                        $check.Detail -notmatch 'No cache flush, service action, adapter change, server change, registration change or policy change is selected')) {
                        throw 'DNS Client event diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'tcp-settings' -and ($check.Name -ne 'TCP setting templates' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Numeric enum values are retained exactly' -or
                        $check.Detail -notmatch 'No TCP template, connection, port range, adapter, route, firewall, service or policy setting changed')) {
                        throw 'TCP setting diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'winsock-catalog' -and ($check.Name -ne 'Winsock protocol catalog' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Protocol names, DLL paths, namespace providers and application traffic are excluded' -or
                        $check.Detail -notmatch 'No catalog entry, provider, socket, network setting or service changed')) {
                        throw 'Winsock catalog diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'winsock-namespaces' -and ($check.Name -ne 'Winsock namespace providers' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Provider names, DLL paths, query contents and application traffic are excluded' -or
                        $check.Detail -notmatch 'No provider, catalog entry, socket, cache, resolver, network setting or service changed')) {
                        throw 'Winsock namespace diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'winsock-provider-files' -and ($check.Name -ne 'Winsock protocol provider files' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Full paths, provider names, signer identities and file contents are excluded' -or
                        $check.Detail -notmatch 'No file, provider, catalog entry, socket, network setting or service changed')) {
                        throw 'Winsock provider file diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'winsock-readiness' -and ($check.Name -notin @('Winsock protocol catalog', 'Winsock namespace providers', 'Winsock protocol provider files', 'Winsock name-resolution readiness') -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No .*network setting.* changed|No catalog entry, provider, socket, network setting or service changed')) {
                        throw 'Winsock readiness command lost an expected source, evidence or non-mutating boundary.'
                    }
                    if ($command -eq 'print-operational-events' -and ($check.Name -ne 'PrintService operational history' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No logging, queue, job, driver, port, service or print setting changed')) {
                        throw 'PrintService Operational diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'print-spooler-events' -and ($check.Name -ne 'Print Spooler failure history' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'tied to the local Print Spooler service identity' -or
                        $check.Detail -notmatch 'do not establish a current outage')) {
                        throw 'Print Spooler event diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'installer-service-events' -and ($check.Name -ne 'Windows Installer service failure history' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'exact local Windows Installer service identity' -or
                        $check.Detail -notmatch 'do not establish a current outage')) {
                        throw 'Windows Installer service event diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'installer-engine-files' -and ($check.Name -ne 'Windows Installer engine files' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Paths and signer identities are excluded' -or
                        $check.Detail -notmatch 'No file, package, registration, service, policy or Installer setting changed')) {
                        throw 'Windows Installer engine file diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'installer-service-registration' -and ($check.Name -ne 'Windows Installer service registration' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Raw executable paths and account values are excluded' -or
                        $check.Detail -notmatch 'No service, file, registry value, package, policy or Installer setting changed')) {
                        throw 'Windows Installer service registration diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'print-policy' -and ($check.Name -ne 'Point and Print policy' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Approved server identities are excluded' -or
                        $check.Detail -notmatch 'No policy, registry value, queue, driver, connection, credential or print setting changed')) {
                        throw 'Point and Print policy diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'print-rendering-policy' -and ($check.Name -ne 'Print rendering and isolation policy' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No policy, registry value, process, driver, queue, job or print setting changed')) {
                        throw 'Print rendering policy diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'print-extension-files' -and ($check.Name -notlike 'Print * file:*' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Full paths and signer identities are excluded' -or
                        $check.Detail -notmatch 'No extension was loaded')) {
                        throw 'Print extension file diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'windows-capabilities' -and ($check.Name -ne 'Windows capabilities' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No capability, payload, source, package, policy or servicing setting changed')) {
                        throw 'Windows capability diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'dns-nrpt-policy' -and ($check.Name -ne 'Effective DNS policy' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No namespace, resolver, adapter, route, VPN, DirectAccess, DNSSEC, IPsec or policy setting changed')) {
                        throw 'Effective DNS policy diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'smb-connections' -and ($check.Name -ne 'Active SMB connections' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'Server, share, account, credential and path identities are excluded' -or
                        $check.Evidence.Output -match 'ServerName|ShareName|UserName|Credential')) {
                        throw 'SMB connection diagnostic lost its identity, privacy boundary, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'smb-client-config' -and ($check.Name -ne 'SMB client configuration' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No SMB setting is changed')) {
                        throw 'SMB client configuration diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'smb-server-config' -and ($check.Name -ne 'SMB server configuration' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No SMB setting is changed')) {
                        throw 'SMB server configuration diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'smb-shares' -and ($check.Name -notlike 'SMB share*' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No share or SMB setting is changed')) {
                        throw 'SMB share diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'printer-ports' -and ($check.Name -notlike 'Printer port*' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No settings changed')) {
                        throw 'Printer port diagnostic lost its identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'printer-conditions' -and ($check.Name -notlike 'Printer condition:*' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No queue, job, port, driver or printer setting changed')) {
                        throw 'Printer condition diagnostic lost its exact identity, evidence or non-mutating scope.'
                    }
                    if ($command -eq 'printer-queue-attributes' -and ($check.Name -notlike 'Printer queue attributes:*' -or
                        $null -eq $check.Evidence -or $check.Detail -notmatch 'No queue, job, document, share, directory publication or printer setting changed')) {
                        throw 'Printer queue attribute diagnostic lost its exact identity, evidence or non-mutating scope.'
                    }
                    if ($null -ne $check.Repair -or [string]::IsNullOrWhiteSpace($check.Detail)) {
                        throw "$command did not retain a read-only diagnostic result."
                    }
                }
                $expected = if (@($checks | Where-Object { $_.State -notin @('Clear', 'Information') }).Count -eq 0) { 0 } else { 3 }
                if ($process.ExitCode -ne $expected) { throw "$command exit code disagrees with its diagnostic state." }
                Write-Output "$command : $($checks.Count) checks, exit $expected"
            }
        } finally { $process.Dispose() }
    }
}
if (@($Only | Where-Object { $_ -notin $executed }).Count -gt 0) { throw 'Requested diagnostic check is not supported by this test runner.' }
Write-Output 'PASS: standalone diagnostics retain JSON evidence and reject execution options. No repairs requested.'
