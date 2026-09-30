# Diagnostics

Diagnostics is where every visit should start. Each tool reads the machine and writes a report. Nothing here changes Windows. The default tool, the full system scan, runs a broad set of read-only checks and ranks what needs attention.

## Tools in this section

Check the machine first, then read the report for what needs attention and the next safe step.

### Full system scan

Inspect 49 diagnostic areas, then evaluate every installed service-recovery recipe together with the servicing, DHCP, printing, Defender, time and WMI recipes. Checks can take several minutes. Auto-repair is optional and requires each recipe's own current evidence, machine prerequisites and dependency checks. Eligible repairs can renew missing DHCP leases, release queued print jobs, resynchronize the clock, refresh Defender signatures, restore eligible services and repair confirmed Windows corruption. Automatic stopped services may be started after their startup window and dependencies are verified. A stopped demand-start service remains unchanged when idle state is the only evidence; narrowly focused workflows can require it only when current feature evidence supports the start. Startup types, task definitions, reports, dumps, subscriptions, credentials, channel settings, firewall policy and managed settings are preserved. Historical, backup and transfer snapshots are read-only. Dump contents are not opened. Dirty-bit inspection does not schedule repair or restart. This workflow does not install updates, change drivers, delete files or perform a complete hardware test.

### Vitals

Capture available memory, provider-reported disk health and BitLocker protection status, then correlate current vitals with device problems, reliability history, hardware-error records, crash evidence, unexpected shutdowns and Windows Memory Diagnostic results. Restart indicators, system disk free space, elevation and update service configuration remain visible. Recovery passwords are not collected. No repairs run in this workflow. The combined result is not a complete hardware, software or stability test.

### Reliability history

Read the last 14 days of Windows Reliability Monitor records together with recent Application errors and unexpected shutdown events, then return one bounded historical conclusion. Historical records do not establish current health or root cause. No repair is selected and no applications, services or settings are changed.

### Startup performance

Read the last 14 days of Windows startup and shutdown performance event metadata, retaining only provider identity and bounded numeric duration fields, then correlate it with supported pending restart indicators. Paths, process names, accounts and other event payload are removed before evidence is saved. Historical degradation records do not identify a current bottleneck or authorize startup, service, application, driver, task, power, restart or policy changes.

### Hardware error history

Read the last 14 days of Windows Hardware Error Architecture and Memory Diagnostic history, current Plug and Play device problems, physical-disk health and provider reliability counters, then return one bounded hardware conclusion. Records require correlation with their payload, firmware diagnostics and vendor hardware tests. No failing component or root cause is inferred, no repair is selected, and no hardware, firmware, driver, disk, pool, volume, file, service, power, restart or setting is changed.

### Driver installation history

Read the retained tail of Windows SetupAPI device-install history, correlate failed sections with exact current Plug and Play identities and return one bounded conclusion. Historical log sections do not prove a device is still present or a failure is current. No driver is installed, removed, rolled back or updated.

### System Restore points

List up to 256 available System Restore points, review explicit creation eligibility and return one bounded readiness conclusion. The inventory does not prove that protection is enabled for every volume or that a restore will succeed. No restore point is created, deleted or applied during diagnosis.

### Windows Recovery Environment

Read the current Windows Recovery Environment configuration with REAgentC, correlate an enabled local location with the exact current disk and partition, and return one bounded readiness conclusion. Optional automatic repair can enable a disabled existing configuration, then verify its reported state. The recovery image is not downloaded, relocated or replaced, no recovery boot is initiated, and Windows is not restarted. Enabled status and partition presence do not prove that the recovery image can boot or repair this PC.

### Windows activation

Read current Windows license status without collecting full or partial product keys, retain the last seven days of sanitized Software Protection warning, error and critical event metadata, then return one bounded operational conclusion. Event payload is removed before evidence is saved. Historical records remain context when current licensing reports Licensed. No activation, product-key, token, license, account, service or policy action is selected. Provider status and event history do not prove subscription entitlement, activation-server reachability, token-store integrity, future validity or root cause.

### Storage

Map bounded disk, partition and drive-letter volume identities, evaluate 10% free-space pressure across every drive-letter volume, inspect VSS shadow-storage allocation and per-volume quota defaults, summarize current Storage module job progress, check the selected NTFS volume and its online self-healing state, and inspect disk/controller devices across this PC. Capacity thresholds, quota settings and job states are review evidence and do not select cleanup, resizing, quota changes or job control. Drive-letter mapping is point-in-time and does not cover every mount path. Device findings are not mapped to the selected volume. The Windows volume is the default. No files, snapshots, jobs or drivers are changed, no setting is changed, no volume is dismounted, and no restart is scheduled.

### Storage Spaces

Inventory non-primordial Storage Spaces pools and virtual disks, including health, operational state, capacity, allocation and resiliency, then return one bounded operational conclusion. Windows may demand-start its Storage Spaces management provider while answering this read-only query. Pools, virtual disks, physical disks, volumes, resiliency, allocation, maintenance settings, policy and startup type are preserved. No pool, disk, volume, allocation, attachment, repair, optimization or setting operation is requested. Provider results do not verify pool accessibility, redundancy, data integrity, backup recoverability or application I/O.

### Windows services

Inventory installed service names, display names, states, start modes and privacy-bounded recovery action types without collecting accounts, executable paths, recovery commands or reboot messages. Review startup-mode coherence, direct dependency identities and cycles, repeat exact pending states, inspect supported services and dependencies of stopped services, then correlate recent Service Control Manager failures only when one exact service name or unique display name is present. Load-order group use is counted without expanding membership, and ambiguous identities remain unresolved. Stopped demand-start services can be normal. No service is started and startup, dependency and recovery settings are preserved. Use the corresponding Repair workflow when a specific feature needs recovery.

### Application errors

Read recent Application error and critical events together with bounded Reliability Monitor history, then return one stability-history conclusion. Historical records do not prove a current fault or root cause and only direct narrower diagnosis. No applications are stopped, repaired, removed or reconfigured.

### Display devices

Inspect graphics adapters and monitors for reported device errors, hardware IDs and installed driver versions. No driver installation, device reset or resolution change. Picture quality and physical connections are not tested.

### Input devices

Inspect keyboard, mouse and HID device errors and driver inventory. No keystrokes are captured, devices reset or drivers changed. Physical input operation is not tested.

### Device problems

Find nonzero Plug and Play configuration error codes across device classes, then correlate present device evidence with bounded SetupAPI driver installation, device setup and Plug and Play history. Historical records alone do not establish a current fault. Review exact device identity and approved driver evidence without changing hardware, drivers or configuration. Missing devices and physical operation are not verified.
