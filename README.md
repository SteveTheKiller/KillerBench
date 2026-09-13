# KillerBench

For embedding, framework targets and platform-provider boundaries, see the [Core README](Core/README.md). Database generator organization is documented in [Tools/Knowledge](Tools/Knowledge/README.md); production generators are not implemented yet.

Windows diagnostic workbench, version 0.1.0 in development. Choose a workflow, configure its options, then press GO. Cleanup supports optional permanent removal; update installation requires explicit selections. SHADE privacy and BERET BitLocker currently expose read-only inspection and option previews. See [native tool coverage](SCRIPT-CAPABILITIES.md) for implemented and unfinished script capabilities.

**Windows Search** checks and optionally starts the enabled WSearch service with bounded running-state verification. It preserves the index, indexed locations and startup configuration. Starting it may resume indexing activity. Index rebuilding and search-result verification are not included. The CLI command is `workflow search --json`.

Network diagnostics also inspect adapter Device Manager codes using the same bounded device collector as Audio. Missing drivers, disabled adapters and startup failures receive distinct findings before DHCP recovery. Virtual and disconnected device records remain visible as observations; no adapter is enabled, removed or reset automatically. A clean device code does not establish network reachability.

**Audio** collects audio/media Device Manager status, then checks Audio Endpoint Builder before Windows Audio, optionally starting stopped, enabled services with bounded running-state verification. Missing drivers, disabled devices and startup errors receive specific findings based on [Windows device error codes](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-pnpentity). It preserves drivers, startup types, selected devices and volume settings. Endpoint recovery must verify before Windows Audio runs. Audible playback, microphone input and routing require separate validation. Use `workflow audio --json` for the same diagnostic flow through the CLI.

Vitals also reads BitLocker protection and conversion status with a 45-second limit. Suspended protection or paused conversion calls for review; it never resumes, decrypts or changes protectors automatically. Only mount point, conversion state, percentage, protection and lock status are selected. Recovery passwords and protector objects are not exported or retained in findings. See [Microsoft's BitLocker status reference](https://learn.microsoft.com/en-us/powershell/module/bitlocker/get-bitlockervolume).

**Vitals** captures available memory and storage-provider disk health with a 45-second collection limit. Less than 5% available memory is an investigation threshold, not a sustained-pressure diagnosis. Missing providers remain unknown. Warning or unhealthy disks call for backup and hardware review; no repair writes are selected. The data uses Microsoft's [operating-system memory fields](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-operatingsystem) and [physical-disk health states](https://learn.microsoft.com/en-us/windows-hardware/drivers/storage/msft-physicaldisk). A provider's healthy status is not a complete hardware test.

When a repair returns restart-required evidence, the workflow records `RestartRequired`, defers subsequent recipes and asks for a fresh run after restart. It never restarts the PC itself. JSON exposes `requiresRestart`; the workflow CLI returns exit code 4, and the app shows an incomplete status light instead of ready. Restart-required is not a verified repair.

**Windows Update** includes optional recovery of stopped, enabled Windows Update, BITS, Cryptographic Services and Windows Modules Installer services before integrity checks. It preserves startup types, update policy and transfer jobs. Service recovery verifies running state only; it does not install updates or prove transfers succeed. Disabled or unavailable service configuration requires review. Each service command has a 20-second bound.

Printing also checks the local queue WorkOffline setting. Optional recovery clears it on affected queues, potentially releasing waiting jobs, then verifies those same queues by identifier. It preserves job contents and excludes remote connections. The Windows PowerShell WMI provider enables privileges required for printer updates; missing state or write failures remain unresolved. This does not establish physical printer availability. See [Microsoft's Win32_Printer reference](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-printer). Tests substitute WMI providers; real queue updates still require isolated validation.

**Windows Installer** now checks explicit DisableMSI policy, the Installer InProgress marker, and engine binary availability before attempting service recovery. Restrictions or incomplete state defer the recovery and explain the next investigation. Markers and policies are preserved, and a marker alone is not treated as proof of a running installer. This does not test package-specific sources or custom actions. Policy interpretation follows [Microsoft's DisableMSI reference](https://learn.microsoft.com/en-us/windows/win32/msi/disablemsi).

Repairs now write a per-user recovery record before each command. A timeout, interrupted process, or restart-required result keeps it pending across app restarts. Further repairs are deferred until a later Windows startup event is available; diagnostic-only runs remain available. Missing or invalid startup evidence requires review rather than assuming the old operation stopped. Ordinary command completion clears the pending marker. This guard applies to newly recorded operations, not historical runs from earlier builds, and does not coordinate repairs launched by other user accounts or tools.

**Network** checks DHCP Client availability and missing IPv4 leases on connected physical DHCP adapters. Optional renewal does not release addresses, reset the network stack, or edit static settings; the DHCP server can supply new lease settings and connections may be interrupted. Each lease operation has a three-minute limit. Verification requires the actual renewed adapters to have usable IPv4 addresses, then refreshes the local configuration summary. DNS resolution and Internet access remain untested. [Microsoft documents the adapter-specific renewal and restart return codes](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/renewdhcplease-method-in-class-win32-networkadapterconfiguration). Script tests substitute CIM providers; no real leases are renewed on the development machine.

DISM and SFC checks and repair commands have a one-hour limit per command. Timeout results are recorded explicitly and block remaining recipes in that run because Windows services may still be completing the operation. A timeout is not proof of rollback or cancellation inside Windows; reassess before starting another repair. Ordinary Stop still waits for an active repair command, up to its limit, then skips later steps.

**Windows Time** now checks synchronization after service availability. Optional resync uses existing configured sources and may adjust the system clock; it preserves time servers and domain policy and skips disabled or unrecognized synchronization types. Each command has a one-minute limit. Verification checks Windows-reported synchronization, not absolute accuracy or source trust. See [Microsoft's Windows Time command reference](https://learn.microsoft.com/en-us/windows-server/networking/windows-time-service/windows-time-service-tools-and-settings). Clock-changing commands are tested with fixtures, not executed on the development PC.

**WMI repository** checks consistency and optionally salvages confirmed corruption, then checks again. Each command has a five-minute limit. Salvage merges readable content and may interrupt management clients; unreadable registrations can require provider reinstallation. It never resets or deletes the repository. A timeout leaves the service's final state unverified. `workflow wmi --json` exposes the same workflow through the CLI. Behavior follows [Microsoft's winmgmt documentation](https://learn.microsoft.com/en-us/windows/win32/wmisdk/winmgmt). Actual salvage needs disposable-machine validation.

The Printing workflow also checks paused local print queues after the spooler check succeeds. Auto-repair resumes paused local queues and can release waiting jobs; it does not delete jobs or alter remote printer connections. Queue operations have a 90-second limit and verify the resulting paused count. A successful queue-state check does not prove paper was printed.

**Storage** runs a read-only CHKDSK of the fixed NTFS system volume with a ten-minute limit. It never uses repair switches, dismounts a volume, or schedules a reboot. Findings on an active volume need confirmation before offline repair. This does not assess physical disk health. `workflow storage --json` exposes the same result through the CLI. Diagnostic-only recipes cannot execute repairs even when auto-repair is selected.

The recipe catalog is available through `RecipeCatalog.All` and `killerbench-cli recipes`. Workflows execute versioned recipes through `RecipeEngine.RunAsync`, retaining recipe IDs, versions, diagnoses and repair outcomes in their journal. Dependencies must appear before dependent recipes; invalid graphs are rejected before any check runs. Independent recipes can continue when another branch fails. This is an initial catalog, not comprehensive Windows repair coverage.

Windows Diagnostic Workbench. Initial local prototype, version 0.1.0.

## Run

Requires Windows and the .NET 10 SDK for development. Open `KillerBench.slnx` in a compatible Visual Studio installation, or run:

```powershell
dotnet run --project KillerBench.csproj
```

Paste a text-log path and click Open Text Log, or drop one file onto the window. Find filters lines while retaining original line numbers. Ctrl+O opens the supplied path; Esc cancels a read. Source reads are limited to 1,000,000 characters and displayed results to 2,000 matching lines. Oversized sources are explicitly marked as truncated. Text decoding supports UTF-8 and BOM-detected encodings.

The default is Dark/Amber, using the existing Orange accent. The paint-roller button at the bottom of the left rail opens the KillerShell-style theme picker with 13 themes and a vertical accent strip. Dark, Light, Black, and 98SE support accents. Amber, green, and white phosphor presets affect only the console. Crisp disables scanlines and glow. App theme, accents, CRT settings, and interface scale persist locally. Existing saved theme choices are preserved.

The language button above the theme picker offers the same 15 languages as KillerShell. Selection persists and the resource system uses English fallback. Translation dictionaries are intentionally empty until the translation pass; selecting another language currently leaves the interface in English.

The left sidebar keeps up to 12 evidence sources for the current session. F2 toggles the sidebar, F3 toggles the separate activity log, and F4 toggles the animated inspector. The inspector switches between display controls and details for the selected source. F1 shows shortcuts. Scroll over the title wordmark to scale the interface.

## Current scope

Implemented: WPF workbench, app-owned family controls and 13 palettes, custom window chrome, typewriter wordmark, source sidebar, contextual inspector, activity log, synthetic sample mode, text-file import, search, cancellation, input bounds, and CRT display controls. The diagnostic core has no WPF dependency. There are no third-party package references.

Not implemented: EVTX file import, comprehensive CBS or Panther parsing, translated UI text, keyboard-map shortcut view, and About card. This prototype does not assess overall machine health.

No existing Killer app was changed. No repository, remote, or release was created.

## Verification

```powershell
dotnet build KillerBench.slnx
dotnet run --project Checks/KillerBench.Checks.csproj
```

The checks cover input and display bounds, mixed newlines, original line references, cancellation, binary rejection, UI construction, sample labeling, search, CRT presets, all 13 app themes, panels, and the theme menu. They render previews into a unique temporary folder and print its location.

See [DESIGN.md](DESIGN.md) for the proposed diagnostic architecture and subsequent milestones.

## Core API and CLI

The WPF app and CLI share `LogAnalyzer.Analyze(TextEvidence)` and `LogAnalyzer.AnalyzeFileAsync(path, cancellationToken)` in KillerBench.Engine. The engine and CLI target .NET 10 without WPF or third-party packages. Live collectors require Windows; offline analysis and capture reopening have no Windows calls. Cross-platform runtime behavior has not yet been tested outside Windows.

```powershell
dotnet run --project Cli/KillerBench.Cli.csproj -- analyze "C:\captures\CBS.log" --json
```

Omit `--json` for readable console output. JSON schema version 1 includes source coverage, normalized eight-digit hexadecimal tokens, occurrence counts, and original line references. These tokens can also be addresses or successful status codes: they are observations, not diagnoses. The desktop Source inspector shows the same observations.

Limits: 1,000,000 input characters, 256 distinct codes, eight excerpts per code (320 characters each). Occurrence counts continue beyond the excerpt limit. The CLI times out after 15 seconds; Ctrl+C cancels. Exit codes: 0 completed (including explicitly marked partial results), 1 input failure, 2 invalid arguments, 130 canceled or timed out. JSON is written to stdout; failures go to stderr. No customer evidence is bundled.

## Saved-log investigation

Click **Load demo** in the inspector to import the bundled synthetic CBS log and show two findings. Select **Show evidence** to inspect an original line in context; **FIND** returns to the regular evidence view. **Export report** saves JSON containing source paths, parsed log entries, and excerpts. No report is uploaded. An empty path followed by **Open log** opens a file picker.

Also try `Samples/setupact.log` (one grouped Setup finding) and `Samples/clean.log` (no findings). These are synthetic examples, not validation against real customer captures.

```powershell
dotnet run --project Cli/KillerBench.Cli.csproj -- investigate Samples/CBS.log --json
```

`Investigation.Analyze` and `AnalyzeFileAsync` are shared by desktop and CLI. The initial parser supports timestamped CBS/CSI lines and recognized components in files named setupact.log or setuperr.log. Other lines remain available in the evidence viewer. Parsed timestamps retain their text and unknown time zone; entries remain in source order. At most 10,000 entries are retained. The report marks partial analysis, and incomplete final lines are excluded from rules.

Rules v1: KB-CBS-001 recognizes source-missing codes in structured error/fatal entries; KB-CBS-002 recognizes component-store inconsistency codes in those entries; KB-SETUP-001 groups Setup error/fatal entries for review. Informational code mentions do not trigger these rules. Findings describe historical recorded events, carry source links, and do not claim a current fault or execute repairs. Definitions follow [Microsoft's servicing guidance](https://learn.microsoft.com/en-us/troubleshoot/windows-server/installing-updates-features-roles/fix-windows-update-errors) and [Setup log guidance](https://learn.microsoft.com/en-us/windows/deployment/upgrade/log-files).

## Windows Update capture

Click **Scan Windows Update** for an on-demand, read-only collection. The scan reads CBS.log, DISM.log, Panther and rollback setup logs, WindowsUpdateClient/Operational, BITS/Operational, Setup, Application, and update/servicing providers in System. It also captures update-service states, system disk free space, uptime, pending-reboot indicators and selected Windows Update policies. Additional sources are available as evidence; the existing CBS, Setup and servicing-event rules do not diagnose every new source. The overview shows the machine, OS, process elevation, capture times, source coverage and grouped findings. Each finding has one **Show evidence** action that opens its first reference and lists related lines. Select **Windows Update capture** in the sidebar to return to all findings.

Each source has a 15-second limit; the scan has a two-minute limit. **Stop** or Esc keeps completed sources and records skipped ones. Missing files, denied access, failures and truncation are separate outcomes. The app requests administrator access at startup. CLI scans work without elevation where access permits. Capture does not change services, update settings or log configuration.

Text snapshots retain the first 1,000,000 characters, not the latest tail. Later entries in oversized logs and archived CBS files are not analyzed. Event queries retain the newest 200 events per channel from the preceding seven days, preserve raw XML and record IDs, and mark an exceeded event limit. Event SystemTime is retained as recorded, normally UTC; text-log time zones remain unknown. Sources are collected sequentially and are not an atomic snapshot. KB-EVENT-001 groups error/critical events from Windows Update and Servicing as review leads.

**Save capture** writes a local `.kbench` JSON file with machine identity, source paths, coverage and the retained raw evidence. **Open capture** works without the original source files and re-evaluates evidence with the installed rules. With the same rules, findings and references are reproduced; future rule changes may change findings. The format accepts up to 24 sources and 64 MB. **Export report** from the capture overview exports all source reports; from an individual source it exports that source only. Reports contain paths and excerpts; captures contain full retained evidence. Neither is uploaded. Starting a scan or opening another capture replaces the displayed session, so save a capture you want to retain first.

```powershell
dotnet run --project Cli/KillerBench.Cli.csproj -- scan --output "C:\captures\update.kbench" --json
dotnet run --project Cli/KillerBench.Cli.csproj -- open "C:\captures\update.kbench" --json
```

Create the destination folder first. The CLI requires a new output filename. Progress goes to stderr and JSON to stdout. Scan/open exit codes are 0 for completed coverage, 3 for partial collection, 1 for failure, 2 for invalid arguments, and 130 for cancellation or overall timeout. A canceled scan saves completed evidence before returning. No exit code certifies machine health.

The shared API is `Capture.CollectAsync(IEnumerable<IEvidenceCollector>, progress, cancellationToken)`, with `WindowsUpdateCollectors.Create()` supplying Windows collectors. `Capture.SaveAsync`, `OpenAsync`, `Analyze`, and `ReportJson` support offline investigations. Custom collectors must honor cancellation. Windows event queries use the built-in [wevtutil query command](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/wevtutil); no new package dependency is required.

## Automated workflows

Service recovery flows are also available: **Printing service**, **Windows Time service**, and **Windows Installer service** (CLI names `printing`, `time`, and `installer`). With auto-repair enabled they start the selected stopped, enabled service and query its state afterward. Disabled, missing, paused, transitioning, or unrecognized service states require attention instead. A stopped demand-start service can be normal; these are explicitly selected availability checks, not proof of a fault. They preserve startup configuration, print queues, time sources and installer settings. Verification proves only that the service reached Running, not that a print job, time synchronization or installation succeeded. These flows share the local journals and simulated repair checks. No service was started during development validation.

Choose a workflow in the main selector, configure its visible options, and click **GO**. Diagnosis is the default. **Auto-repair confirmed issues** permits supported repairs in eligible workflows. Vitals, Privacy and BitLocker inspection do not repair; cleanup and update installation have separate explicit controls. KillerBench requests administrator access at startup.

Windows Update checks current prerequisites and runs component-store and system-file checks in order. Confirmed corruption selects the matching repair, followed by verification. Failed verification stops dependent work. Historical logs, unknown output, failed commands and truncated results never select a repair. Pending restart, low free space (a conservative 5 GB guard) or missing administrator rights defer servicing. Disabled services and managed update policies are reported without changing them. This workflow checks servicing health; it does not install an update or prove that Windows Update now works.

Vitals reports restart, free space and service configuration. Network inspects local adapter addresses, gateways and DNS configuration without sending requests or resetting networking. Filter adapters without IP bindings are informational. Network results do not prove Internet access or working DNS resolution.

The shared API is `Workflows.RunAsync(kind, autoRepair, journalFolder, progress, cancellationToken)`. CLI: `killerbench-cli workflow <update|files|vitals|network> [--auto-repair] [--json]`. Exit 0 means no unresolved issue in checked scopes, 3 means attention required, 130 means stopped, 2 means invalid arguments, and 1 means a startup or journal error. Progress goes to stderr. Each run saves a JSON journal under local application data in KillerBench/Workflows, including decisions, bounded integrity-check output and repair results. Stop waits for the current servicing command. Unknown or localized SFC output requires review. No automatic restart or rollback is performed.

Automated tests cover clean checks, repair selection and ordering, diagnosis-only mode, blocked prerequisites, failed verification, unknown output, cancellation, journals, network configuration and workflow controls. Actual servicing repairs still require disposable-VM validation.

## Manual repairs

### BitLocker options

Select **BitLocker (BERET)** to choose a volume, operation, encryption method, used-space-only and recovery destination/path. GO previews those choices and inspects the selected volume without changing protection. The CLI equivalent is `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId --inspect`; omit `--inspect` for options preview alone. File destinations must be outside the protected volume. ActiveDirectory and EntraId are choices, not proof of escrow support. Guarded file-backup primitives and outcome journals exist in Core, but graphical backup execution, native encryption and escrow remain unfinished.

### Privacy options

Select **Privacy (SHADE)** and check the areas to inspect. All start unchecked. GO saves a read-only registry snapshot for the selected areas. Camera and microphone are separate, as are clipboard history and sync. CLI: `privacy-plan --select Camera --select ClipboardSync --inspect`. Without `--inspect`, the command describes the selected areas only. Current-user data belongs to the account running KillerBench. Coverage is partial; recorded registry values do not establish effective permissions or policy ownership. Privacy mutation and undo are not implemented.

### Available updates

In the app, run discovery and expand **Select updates after discovery**. Select individual rows, choose whether to include updates that may need a restart, and press **Preview selected**. This uses the shared planning rules and shows blockers in the evidence console. **Install selected** starts fresh checks and installs eligible selections with live status and a journal. The old selection is cleared afterward. No automatic restart is scheduled. Native installation still requires disposable-machine validation.

The Core `UpdateExecution` coordinator records prepared operations, outcomes and readiness blockers in a journal. Unverified or restart-required operations retain a pending marker that blocks another attempt in the same journal folder. `UpdateReconciliation.TryResolveAsync` archives that marker only after verifying the exact revision is installed and the machine is ready; unknown or not-installed states remain blocked. `WindowsUpdateExecutionHost` implements single-update download, installation and exact-revision verification with a one-hour process bound. App and CLI execution are wired. Tests substitute COM objects; native installation still needs disposable-machine validation.

Prepare a fresh installation preview with `killerbench-cli update-plan --select GUID:REVISION --drivers --allow-restart-required --json`. Repeat `--select` for additional identities from discovery; category switches match discovery. Restart allowance never schedules a restart. Shared planning rejects incomplete discovery and changed or duplicate selections, and reports license, interaction, beta, exclusive-installation and restart blockers. Add `--execute` for installation through fresh prerequisites and a durable journal. `killerbench-cli update-recover` checks an interrupted operation without installing or retrying it.

Discovery also retains update classifications, beta status, installation impact, possible user interaction and reboot behavior. Missing or malformed metadata is marked unavailable while preserving the update identity. These are prerequisites for future installation planning, not authorization to install. Metadata follows [IInstallationBehavior](https://learn.microsoft.com/en-us/windows/win32/wua_sdk/iinstallationbehavior-properties) and [IUpdate](https://learn.microsoft.com/en-us/windows/win32/wua_sdk/iupdate-properties).

Available updates uses Windows Update Agent's configured source with software and driver checkboxes. GO discovers updates; installation uses the separate selected-update action. Software may include other Microsoft products already offered by that source. Separate Microsoft Update opt-in and third-party application updates remain unfinished.

CLI: `killerbench-cli workflow updates --drivers --json` searches software and drivers; add `--no-software` to search drivers alone. At least one category is required. The Core API is `UpdateDiscovery.SearchAsync(UpdateSearchOptions)`. Searches have a five-minute process bound and retain up to 500 update identities, revisions, titles and download/license states. Failed, timed-out or incomplete searches do not claim that the machine is up to date. Tests substitute the update agent and validate category selection and partial results.

Reference: [Windows Update Agent search criteria](https://learn.microsoft.com/en-us/windows/win32/api/wuapi/nf-wuapi-iupdatesearcher-search).

### Defender signatures

Before GO, use the TPM, Secure Boot and virtualization checkboxes to select the hardware checks. Unchecked providers are not queried and their report entries say Not selected. Defender signature checks remain included. CLI equivalents are `--skip-tpm`, `--skip-secure-boot` and `--skip-virtualization`, accepted only for `workflow defender`. Core consumers use `SecurityOptions`, either on `WindowsWorkflowHost` or with `SecurityDiagnostics.CheckAsync`.

The same workflow includes read-only TPM readiness, Secure Boot, VBS, memory integrity and Credential Guard snapshots. Missing providers or privileges remain Unknown without hiding independent results. Configured protections that are not running require review; optional protections not configured are informational baseline comparisons. No TPM initialization or clearing, firmware changes or security-policy edits are performed.

Hardware-status references: [Get-Tpm](https://learn.microsoft.com/en-us/powershell/module/trustedplatformmodule/get-tpm?view=windowsserver2025-ps), [Confirm-SecureBootUEFI](https://learn.microsoft.com/en-us/powershell/module/secureboot/confirm-securebootuefi?view=windowsserver2025-ps), and [Win32_DeviceGuard status values](https://learn.microsoft.com/en-us/windows/security/hardware-security/enable-virtualization-based-protection-of-code-integrity).

The Microsoft Defender workflow inspects active protection and Defender-reported signature freshness. Auto-repair can call Update-MpSignature only when Defender reports Normal mode, enabled antivirus and real-time protection, and outdated signatures. It preserves exclusions, protection controls, policy and source configuration. Passive or disabled protection is reported for review. The refresh has a five-minute bound and verification has a 45-second bound; a timed-out operation does not count as repaired. This does not scan for malware or establish complete security health.

Use `killerbench-cli workflow defender` for diagnosis, optionally adding `--auto-repair` to permit eligible refresh. Provider fixtures exercise fresh, stale, passive, disabled and incomplete states and substitute the update command. Real signature refresh still requires disposable-machine validation.

References: [Get-MpComputerStatus](https://learn.microsoft.com/en-us/powershell/module/defender/get-mpcomputerstatus) and [Update-MpSignature](https://learn.microsoft.com/en-us/powershell/module/defender/update-mpsignature).

### Configurable temporary-file cleanup

Select Cleanup, choose temporary files, shader caches and/or browser caches, and set the minimum age from 1 to 365 days. GO previews by default. Browser discovery searches default current-user Chrome, Edge, Brave, Firefox and Opera cache folders with bounded profile coverage; custom locations are not searched. Cookies, logins and profile settings are outside those targets. The separate permanent-removal checkbox enables deletion of up to 100 retained candidates per target per run; it bypasses the Recycle Bin and has no undo. Older creation, modification and access timestamps are required, but timestamps alone do not prove that an application no longer needs a file. Cache rebuilding can slow initial loading.

Removal recollects the selected targets, opens candidates exclusively, rejects changed paths, links, protected files and changed metadata, and requests deletion through the checked file handle. Each attempt is journaled before mutation and each outcome afterward. Path absence is checked after the handle closes. Unverified results are not retried automatically. Partial collection, remaining candidates and cancellation are reported explicitly. Stop is checked between individual files and the removal loop stops scheduling work after two minutes; a single operating-system file call is synchronous.

CLI: `killerbench-cli cleanup --user-temp --days 14` previews; add `--windows-temp`, `--shader-caches` or `--browser-caches` for those targets, `--json` for structured output, or `--execute` for permanent removal. The generic `workflow cleanup` remains diagnostic-only with default settings. Removal tests use substituted providers; native deletion, locked-file behavior and filesystem redirection still require disposable-machine tests. No real cache files were deleted during development validation.

Native API references: [SetFileInformationByHandle](https://learn.microsoft.com/en-us/windows/win32/api/fileapi/nf-fileapi-setfileinformationbyhandle) and [FILE_DISPOSITION_INFO](https://learn.microsoft.com/en-us/windows/win32/api/winbase/ns-winbase-file_disposition_info).

### System servicing

Open **Repairs**, choose DISM component-store repair or SFC system-file repair, read the preview, select **Run this repair on this PC**, then click **GO**. Run the app as administrator first. Reboot indicators block execution until the machine is restarted and reassessed. Repairs always target the current PC, even when viewing another machine's saved capture. They are manual tools, not automatic recommendations inferred from historical log findings.

DISM runs RestoreHealth and then ScanHealth with automatic restart disabled. It can contact the configured repair source. SFC runs scannow and then verifyonly. Stop waits for the current servicing command, up to its one-hour bound, before skipping later steps; closing waits for operation shutdown. A timeout cannot prove Windows stopped servicing or rolled back changes. Failures, restart requirements and unrecognized verification output remain unresolved.

A separate JSON execution journal is created before any command under the user's local application data folder, in KillerBench/Repairs. It records the target, commands, exit codes, bounded output, and verification state. An interrupted app can leave a journal marked Running; it is not proof of completion. Captures taken before a repair remain historical snapshots. Scan again to collect post-repair evidence. Repairs have no automatic rollback. One repair at a time is allowed across KillerBench processes running as the same user.

CLI preview: `killerbench-cli repair dism` or `killerbench-cli repair sfc`. Adding `--execute` runs the previewed action on the current PC. Ctrl+C requests a stop between commands. Exit codes: 0 for recognized successful verification, 3 for a result requiring review (including failed commands or restart required), 1 for preflight/execution errors, 2 for invalid arguments, and 130 for cancellation. Output is retained up to 250,000 characters per stream per step and truncation is recorded.

Repair orchestration is covered by simulated success, denied access, failure, restart-required, cancellation, and verification tests. Native DISM/SFC repair execution still requires hands-on validation in a disposable Windows VM. No repair was executed on the development machine during implementation.
