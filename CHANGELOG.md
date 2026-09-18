# Changelog

- Add separate raw console and formatted workflow report views with findings and next actions.
- Replace the fixed CRT console with a theme-aware smoked-glass output pane.
- Make Network a primary category and move BitLocker into Security.
- Expand offline CBS, CSI and Windows Setup analysis with reviewed code guidance, informational Setup outcomes, clear partial-evidence CLI exits and recipe-based restart reporting.
- Add shared validation, visible redacted plan summaries and read-only execution of option-rich workflow plans, including exact storage, printing, MSI repair, Windows Time source, Event Log channel, Firewall profile and computer-name reviews.
- Add exact BitLocker backup, encryption and protection-resume confirmations to shared workflow options with mutual-exclusion and prerequisite validation.
- Add privacy-bounded Microsoft Defender quick and full scan history plus explicit guarded scan coordination to the desktop, Engine and CLI.
- Expand Diagnostic Policy diagnosis with bounded operational events and a combined service-readiness conclusion.
- Expand Network Connections diagnosis with current profile assignments and a combined service-readiness conclusion.
- Expand WinHTTP Auto-Proxy and Windows Connection Manager diagnosis with configuration evidence and combined readiness conclusions.
- Expand Network Setup diagnosis with adapter-binding evidence and a combined service-readiness conclusion.
- Expand focused Storage Service diagnosis with current Storage-module jobs, exact history and a combined readiness conclusion.
- Add a combined Security Center conclusion across antivirus registrations, exact failure history and current service readiness.
- Add a combined WinRM conclusion across listener configuration, retained events, exact failure history and current service readiness.
- Add a combined Remote Desktop conclusion across configuration, retained events, exact failure history and current service readiness.
- Add a combined Remote Access conclusion across VPN profiles, exact failure history and current service readiness.
- Add a combined Windows Event Collector conclusion across subscriptions, exact failure history and current service readiness.
- Expand Windows Error Reporting diagnosis with dump configuration, bounded dump metadata, recent crash reports and a combined service-readiness conclusion.
- Add Windows SystemTemp as a separate bounded cleanup target in the Engine, desktop, CLI and validation plan.
- Expand the focused Windows Update Medic Service workflow with exact state, registered DLL, live update activity, exact failure history and a combined conclusion while preserving guarded recovery.
- Add exact Service Control Manager failure history to focused BITS, Cryptographic Services and Windows Modules Installer workflows.
- Add a focused Update Orchestrator Service workflow with exact state, failure history and guarded recovery.
- Expand the focused Windows Update Service workflow with exact state, registered DLL, live update activity, exact failure history and a combined conclusion while preserving guarded recovery.
- Add a focused Delivery Optimization workflow with live activity, policy and exact failure-history context, a combined conclusion and exact paused-only recovery.
- Expand the focused BITS workflow with current-account jobs, exact service state, failure history and a combined conclusion.
- Expand the focused Cryptographic Services workflow with registered DLL evidence and a combined readiness conclusion.
- Expand the focused Windows Modules Installer workflow with package-transition evidence and a combined readiness conclusion.
- Expand the focused Update Orchestrator workflow with exact task, current activity and combined readiness evidence.
- Add combined readiness conclusions across peripheral, association, discovery, font-cache and evidence-backed feature-service workflows.
- Correct MACE preserved-data eligibility so ordinary installed Store/MSIX packages remain blocked and only loose-file development packages can use the Windows preservation boundary.
- Add MACE package removal behind a separate red desktop control with consent, confirmation, fresh safeguards, durable records and no automatic retry.
- Add desktop reconciliation for interrupted MACE package removal without repeating the request.
- Add CLI reconciliation for interrupted MACE package removal without adding a removal command.
- Add a repeatable framework-dependent Windows x64 single-file publish profile.
- Remove the bright border around category buttons.
- Move report export into the main workspace and remove the inspector sidebar.
- Apply themed list rows to advanced diagnostic and recipe checks.
- Keep report export and category navigation available while a scan is running.
- Explain that live output can be browsed without interrupting the active scan.
- Keep focused CLI coverage aligned with the expanded Vitals result set.
- Make recipe-catalog CLI coverage reliable under Windows PowerShell JSON parsing.

## [0.1.0] - Unreleased

- Added bounded backup, focused Backup Services, system-stability, security, focused Defender, application-deployment, installation-environment, storage-environment, focused Windows Update and System Files, broad and focused service, audio, printing-environment, Windows Search, WMI, Event Log, Windows Time, file-sharing, background-transfer, DHCP, DNS and Winsock correlations.
- Expanded Vitals with device, reliability, hardware-error, crash and shutdown evidence plus one bounded system-stability conclusion.
- Expanded Applications with Reliability Monitor evidence and one bounded stability-history conclusion.
- Added one bounded startup-environment conclusion across registrations, approval markers and boot or logon tasks.
- Added one bounded Scheduled Tasks conclusion across runtime results, missed runs and exact service failure history.
- Added one bounded App Packages conclusion across registration state, optional provisioning, deployment history, Store history and restart evidence.
- Expanded Device Problems with SetupAPI, device setup and Plug and Play history plus one bounded operational conclusion.
- Expanded Installed Applications with bounded stability history, restart evidence and one scoped operational conclusion.
- Add read-only uninstall-command and install-location presence flags to installed-application inventory and comparisons.
- Add read-only fixed-root review for an exact, user-named application folder without traversal or deletion.
- Add a standalone read-only startup registration and boot or logon task review to the CLI.
- Add read-only browser-extension identity inventory for Chrome, Edge, Brave and Firefox in the desktop app, Engine and CLI, with explicit incomplete coverage at collection limits.
- Add desktop access to existing read-only duplicate-file and large-file reviews for one selected local folder.
- Added one bounded Certificates conclusion across personal-certificate validity and cache-only local chain evidence, using direct read-only store access for reliable validity collection.
- Added one bounded Windows Activation conclusion across current licensing and sanitized Software Protection history.
- Added one bounded Storage Spaces conclusion across pool and virtual-disk health evidence.
- Expanded Profiles with separate registration and dependency evidence plus one bounded review conclusion.
- Expanded Startup Performance with restart evidence and one bounded historical conclusion.
- Expanded Hardware Errors with memory, device and physical-disk evidence plus one bounded conclusion.
- Expanded Reliability History with Application errors, unexpected shutdowns and one bounded historical conclusion.
- Added one bounded Driver Installation History conclusion across retained SetupAPI failures and exact current device state.
- Added one bounded System Restore conclusion across point inventory and explicit creation eligibility.
- Expanded Windows Recovery with exact configured-partition correlation and one bounded readiness conclusion.
- Separated read-only service diagnosis from administrator-gated repair execution.
- Corrected ORCA planning and desktop consent to expose guarded cache retention only when exact prerequisites pass.

- Add a concise Engine README and an embedded themed developer guide for workflows, evidence, repairs, CLI use, APIs, architecture and knowledge packs.
- Add Engine-owned workflow control metadata and CLI discovery for Cleanup, updates, Privacy, BitLocker, MACE, ORCA, DEPOT, BEAM, DEBLOAT and PEEK.
- Bind those Engine option definitions to every corresponding desktop control so defaults, bounds and help text stay synchronized across the app, Engine and CLI.
- Add privacy-bounded ORCA readiness and guarded cache retention with exact verification and no-repeat recovery.
- Add guarded previous Windows installation removal through supported rollback servicing, with backup confirmation, fresh eligibility checks and no-repeat recovery.
- Add read-only Microsoft Defender protection, monitoring and tamper settings plus recognized policy-surface evidence to the app, Engine and CLI without collecting exclusions or policy values.
- Add privacy-bounded effective Windows Firewall rule and policy-source counts to Network diagnosis, Full system scan, the Engine and CLI.
- Add privacy-bounded Windows connectivity classification policy evidence to Network diagnosis, Full system scan, the Engine and CLI.
- Add read-only UAC elevation policy and effective AppLocker collection summaries to security diagnosis, Full system scan, the Engine and CLI.
- Add deterministic VM validation plans, strict current-version evidence and themed coverage reports through the Engine and CLI.
- Add guarded BitLocker encryption through the app, Engine and CLI with verified recovery backup and no-repeat recovery.
- Add the configurable read-only PEEK servicing activity monitor with current update transitions, live servicing states, process metrics, transfer deltas, Setup result references, Windows Update scheduled-task evidence, readiness and Installer context to the app, Engine and CLI.
- Add bounded Windows Update engine files, exact servicing-chain states, service DLLs, failure histories, deterministic chain correlation and code-driven follow-ups to update diagnostics, Full system scan and CLI.
- Add deterministic offline importers, typed lookups and pack inventory for official UEFI vendor and Bluetooth company identifier snapshots.
- Expose focused service, VPN profile and WinRM listener diagnostics through the CLI.
- Distinguish dispatched, verification and skipped command plans in HTML and text repair reports.
- Require separate feature-upgrade and restart consent for one exact Windows feature upgrade across the app, Engine and CLI.
- Add guarded exact-version third-party application updates through the app, Engine and CLI with fresh rediscovery and no-repeat recovery.
- Add guarded create-only registration and exact no-retry recovery for disabled signed Local System tasks through the app, Engine and CLI.
- Add read-only firewall configuration and operational history plus exact firewall and core network service failure histories to Network diagnosis, Full system scan and CLI.
- Add exact storage, shadow-copy and backup service failure histories to Storage, Backup services, Full system scan and CLI.
- Add shared-snapshot exact service failure histories to Time, Search, WMI, Event Log, Audio and 51 application, identity, device and infrastructure service workflows and CLI.
- Add guarded camera, microphone, typing personalization, web search and OEM suggestion controls with exact undo and authority preservation.
- Require fresh exact local executable and optional ServiceDll presence before every automatic service start.
- Add guarded High performance, power-timer and hibernation controls with honest dispatch tracking and no-repeat recovery.
- Add privacy-bounded pending file operation counts and Installer-activity correlation with explicit wait or restart guidance.
- Extend guarded DHCP renewal with coherent expired leases, reason-bound verification, honest no-renewal outcomes and no-repeat recovery.
- Add privacy-bounded Delivery Optimization activity and policy evidence to Windows Update diagnosis, Full system scan and CLI.
- Require exact visible waiting-job evidence before resuming selected paused print queues.
- Add guarded profile backup coordination, complete source-to-copy verification, artifact tooling and no-repeat recovery through the app, Engine and CLI.
- Add exact ProfileList registration evidence and block stale-profile plans on `.bak`, state, reference-count or path ambiguity.
- Add bounded global Windows network offload evidence to Network diagnosis, Full system scan and CLI.
- Add bounded native Winsock protocol, namespace-provider and provider-file evidence with correlated readiness to Network diagnosis, Full system scan and CLI.
- Add guarded Windows Backup and Block Level Backup Engine service recovery with preserved backup configuration and bounded running-state verification.
- Add privacy-bounded Windows Backup operation history to backup workflows, Full system scan and CLI.
- Add privacy-bounded File History protection state and event review to backup workflows, Full system scan and CLI.
- Correlate retained system-crash events with exact reported dump paths, collected file metadata and bounded structural analysis without timestamp guessing.
- Add a deterministic selected network-target verdict across local route and DNS readiness, configured gateways, bounded hostname lookup and destination TCP reachability.
- Add guarded exact diagnostic event-channel enablement with final eligibility, honest dispatch tracking, preserved settings and no-repeat recovery across the app, Engine and CLI.
- Add bounded installed Windows hotfix inventory to Engine, CLI and update workflows.
- Add bounded Windows Memory Diagnostic result history to Vitals, Full system scan and CLI.
- Add bounded structural minidump exception and module-basename analysis to crash review and CLI.
- Add a guarded Windows Search index rebuild with repeated pre-mutation eligibility, honest dispatch tracking, retained prior data and no-repeat reconciliation across the app, Engine and CLI.
- Add endpoint volume, mute and exclusive-format capability evidence to default audio diagnostics.
- Add documented processing and transport evidence plus direction and Windows-role selection for exact default audio endpoints across the app, Engine and CLI.
- Add guarded current-user privacy and suggestion controls with repeated final eligibility, honest dispatch tracking, no-repeat recovery and exact-value undo across the app, Engine and CLI.
- Include complete Windows Installer policy, current service, engine, registration, transaction and exact service-history evidence with deterministic operational correlation in Full system scan, plus standalone logging-policy CLI output.
- Add bounded cache-only local chain-trust evidence for personal certificates across security workflows and CLI.
- Add optional bounded third-party application update discovery through Microsoft.WinGet.Client across the app, Engine and CLI.
- Add read-only feature targeting, policy surfaces, deadlines and notification diagnostics across update workflows and CLI.
- Add fresh raw-evidence gating, managed repair-source diagnosis and explicit unrecoverable-store outcomes before desktop and CLI DISM and SFC execution.
- Continue an exact paused Delivery Optimization service through the recipe engine with honest no-dispatch outcomes, stable running-state verification and later-boot no-repeat reconciliation.
- Expose every installed service-recovery recipe through Full system scan, preserve demand-start and startup-window guards, and group its advanced checkbox selection by functional area with All and None controls.
- Add privacy-bounded service trigger configuration to Services, Full system scan and CLI.
- Add bounded automatic CBS, newest plain-text and compressed CBS archive, DISM and Windows Setup log analysis to Full system scan, servicing workflows and CLI.
- Add read-only NTFS and ReFS delete-notification configuration to Storage, Full system scan and CLI.
- Add privacy-bounded IPv4 and IPv6 dynamic TCP port-pressure snapshots to Network, Full system scan and CLI.
- Add bounded Windows TCP template evidence to Network, Full system scan and CLI.
- Add bounded DHCP lease state and IPv4 and IPv6 Client event history to Network, Full system scan and CLI.
- Add machine and current-account Windows Installer source-order, media and browsing policy evidence to installation readiness.
- Add Windows Installer diagnostics and guarded exact-product and registration repair with fresh raw-evidence gates, honest dispatch tracking and no-repeat recovery.
- Add bounded local printer condition, queue operating-attribute, rendering and driver-isolation policy evidence to Printing, Full system scan and CLI.
- Add guarded exact default-printer and selected stuck print-job repair with preserved queue state and no-repeat recovery.
- Add Point and Print policy, current Print Spooler state, available PrintService Operational history, exact service failure history and deterministic operational correlation to Printing, Full system scan and CLI.
- Add bounded Windows Update cache totals to Full system scan, the Windows Update workflow and CLI.
- Add privacy-bounded service startup and dependency configuration, executable presence, path quoting, logon classes, DLL presence, recovery policy and persistent-transition evidence while keeping ordinary disabled, delayed and demand-start states from becoming false failures.
- Keep disabled scheduled-task history visible without counting stale results or missed runs as current problems.
- Separate application-specific scheduled-task exit codes from Scheduler-defined configuration failures and missed runs.
- Keep stale IP addresses on disconnected interfaces visible without counting them as current connectivity problems.
- Remove exact duplicate findings when aggregate readiness areas reuse previously collected evidence.
- Add bounded default-printer and automatic-management evidence to printing diagnosis.
- Add bounded DNS suffix, registration, devolution, effective namespace-policy, privacy-safe cache and Client failure-history evidence to network diagnosis.
- Add bounded Windows startup approval-state evidence without collecting command lines or binary timestamps.
- Organize the Engine source into capability, evidence, workflow, automation and infrastructure folders without changing its public API.
- Add a deterministic backup-readiness outcome across VSS history, writer and provider state, current shadow copies, restore points, storage and restart evidence.
- Add a deterministic system-stability outcome across current vitals, device faults, reliability, hardware-error, crash and shutdown evidence.
- Add a deterministic security-readiness outcome across hardware, certificates, licensing, registered protection products, Defender and restart evidence.
- Add a deterministic application-deployment outcome across registered and provisioned packaged-app and Win32 registration, installation activity, deployment failures, application history and restart evidence.
- Simplify focused diagnostics to 11 outcome groups by default while retaining all 49 individual evidence areas behind an expert option.
- Make CLI diagnostic discovery return the same 11 recommended outcome groups by default, with an explicit option for all 49 areas.
- Add dependency-ordered Base Filtering Engine and Windows Firewall service recovery to Full system scan.
- Add guarded Task Scheduler and Diagnostic Policy Service recovery to Full system scan.
- Add guarded Windows Error Reporting and Windows Event Collector recovery to Full system scan.
- Retain exact current Windows Update search HRESULTs with reviewed references and no inferred repair.
- Add focused shared-recipe discovery for the desktop, Engine and CLI Windows Update data-store reset, with final eligibility checks, retained prior store, preserved service configuration, replacement-search verification and no-repeat reconciliation.
- Add IP address diagnostics, bounded default-gateway checks and per-interface connectivity correlation to Full system scan, the Network workflow and CLI.
- Add registered MSI cache visibility and privacy-bounded source metadata diagnostics to installation readiness, the Installer workflow and CLI.
- Add a safe full-app demo mode with synthetic diagnostics, repair presentation and progress for every workflow.
- Make Full system scan the clear desktop, Engine and CLI entry point with numbered progress, selectable diagnostic areas and clear aggregate readiness verdicts.
- Expand Windows Update error references, direct lookup and automatic follow-up, and harden installed Windows message lookup.
- Add bounded Event Log size and saturation diagnostics across Engine, desktop and CLI.
- Add selected unmanaged Windows Firewall profile enablement with final eligibility, durable verification and no-repeat recovery across Engine, desktop and CLI.
- Add guarded Windows Time source configuration with final pre-command eligibility, shared recipe discovery and exact no-repeat recovery across Engine, desktop and CLI.
- Add guarded standalone computer rename with repeated pre-request eligibility, complete restart evidence, honest dispatch tracking and no-repeat recovery across Engine, desktop and CLI.
- Add guarded BitLocker protection resume with repeated final eligibility, honest dispatch tracking and exact verification across Engine, desktop and CLI.
- Add preview-first System Restore point creation with repeated pre-request eligibility, honest dispatch tracking and no-repeat recovery across Engine, desktop and CLI.
- Preserve focused Storage execution consent through the main GO handoff.
- Add selected-volume optimization analysis, final pre-command eligibility, honest dispatch tracking and guarded Windows-recommended, retrim and hard-disk defragmentation across Engine, desktop and CLI.
- Add themed HTML and text workflow reports with prominent area outcomes and separate diagnostic and diagnostic-plus-repair scopes across Engine, desktop, and CLI.
- Add separately selected thumbnail, icon and Delivery Optimization caches plus current-user Recycle Bin preview and bounded cleanup across Engine, desktop, and CLI.
- Add deterministic review CSV generation from installed Windows HRESULT messages.
- Add deterministic Windows bug-check knowledge-pack generation from retained Microsoft reference Markdown.
- Add exact bug-check reference resolution to Engine and CLI crash review.
- Use an opened bug-check pack for later desktop Vitals and Common problems diagnostics in the same session.
- Retain up to 32 session knowledge packs and reject conflicting contextual references instead of choosing by load order.
- Resolve exact references across bounded pack sets in the Engine and CLI, including repeated crash-review packs and provenance for direct or derived matches.
- Correlate problem devices with exact platform-specific driver references in desktop, Engine and CLI diagnostics.
- Add signed knowledge database configuration, CLI management and About-card updates.
- Model guarded filesystem repair as an option-dependent Engine recipe across desktop Storage and CLI, with explicit backup confirmation, repeated final exact-volume eligibility and no generic automatic dispatch.
- Add role-specific default audio endpoint identities, form factors and shared formats to Audio diagnostics.
- Add a reusable Engine, CLI and desktop catalog for all documented Device Manager problem codes with conservative next actions and unknown-code fallback.
- Add CLI workflow discovery and require every executable recipe to declare a supported interruption-recovery contract.
- Add fresh Defender signature eligibility, honest no-refresh outcomes and later-boot reconciliation without repeating refreshes.
- Add fresh pre-command component cleanup eligibility and later-boot reconciliation without repeating maintenance.
- Add later-boot DISM and SFC repair reconciliation without repeating interrupted servicing.
- Add honest no-dispatch outcomes and exact later-boot reconciliation across the shared service-start catalog without repeating starts.
- Add grouped recovery for ten core Windows infrastructure services.
- Add fresh pre-command WMI repository eligibility, honest no-dispatch outcomes and later-boot reconciliation without repeating salvage.
- Add compound Windows Time recovery with fresh pre-command eligibility, honest skipped-step journals and later-boot reconciliation without repeating resync.
- Add target-bound later-boot DHCP renewal reconciliation without renewing leases again.
- Add exact paused-queue snapshots and later-boot reconciliation without resuming queues again.
- Add honest no-change outcomes and later-boot reconciliation for selected printer offline-setting recovery.
- Add modern Windows Update class-source, active-hours and scheduled-installation policy diagnosis.
- Add configurable MACE application-removal review and guarded current-account package planning with durable recovery.
- Add configurable ORCA Outlook reset review across exact registrations and bounded local evidence.
- Add configurable DEPOT endpoint provisioning review with application and setup selections.
- Add configurable BEAM Intune deployment review with source, package and device target validation.
- Add configurable DEBLOAT review with recoverable desktop, Engine and CLI power actions, final pre-command eligibility checks, focused power recipe discovery and typed policy, telemetry, OEM service, power, task, application and vendor evidence.

- Add focused Windows Time demand-start recovery when current status reports the exact stopped-service error, with policy, configuration and dependency gates before start, resync and verification.

- Complete read-only SHADE registry and service coverage with separate app activity and typing and inking controls.

- Add configured, Windows Update and registered Microsoft Update discovery sources across the app, Engine and CLI.
- Add bounded IP route and Hosts file evidence to Network diagnosis without changing network configuration.
- Add bounded per-interface DNS server evidence to Network diagnosis and automatic update-failure follow-ups.
- Add automatic default-route and same-family DNS correlation to Network diagnosis without changing configuration.
- Add automatic active-network and firewall-profile alignment to Network diagnostics and the CLI.
- Add bounded Windows servicing package, optional feature and capability-state evidence to Windows Update and system-file diagnosis without changing components.
- Add automatic package, optional-feature and restart-state correlation to Windows servicing diagnosis.
- Add bounded print processor and monitor registration, binary-presence and signature evidence to Printing diagnosis without loading extensions.
- Add print spool directory diagnosis, repeated fresh eligibility, honest no-creation outcomes, exact verification and later-boot no-repeat reconciliation.
- Add automatic local queue mapping to Standard TCP/IP printer host, protocol, port and SNMP configuration without network contact.
- Add bounded connection checks for exact assigned Standard TCP/IP printer endpoints.
- Add exact current-queue correlation for retained PrintService warning and failure records.
- Add Windows Installer logging, package-override and debug-policy evidence to Installer and common-problem diagnosis.
- Add exact-product correlation of recent Installer failures with later retained success outcomes.
- Add bounded physical-disk, disk, partition and volume inventory with reliability and drive-letter event correlation to Storage diagnosis without changing storage.
- Add automatic 10% free-space review across every drive-letter volume in the bounded Storage topology.
- Add a bounded installed-service inventory and exact name or unique display-name correlation for recent Service Control Manager failures.
- Add bounded SMB share and server-configuration evidence to File sharing diagnostics and the CLI without collecting paths, permissions, sessions or accounts.
- Add SMB client signing, guest-access and transport configuration to Network diagnostics and the CLI.
- Add privacy-safe active SMB dialect, signing, encryption and continuous-availability evidence to Network diagnostics and the CLI.
- Add deterministic pci.ids and usb.ids knowledge-pack generation with exact local PCI and USB vendor/device lookup.

- Add guarded Update Orchestrator service recovery to Windows Update checks.

- Add focused Diagnostic Policy, Windows Error Reporting, Event Collector, Remote Access, Security Center and network-management service recovery, including subscription, VPN profile and antivirus registration inventory.
- Add Remote Desktop configuration and connection history diagnosis with guarded service recovery without changing remote-access settings.
- Add Windows Remote Management listener inventory, history and guarded service recovery without changing listeners or trust settings.

- Add AppX and user profile event history with guarded App Readiness, State Repository, AppX Deployment, Client License and User Profile service recovery, plus feature-specific readiness conclusions.
- Add Microsoft Store installation history with guarded Store Install, Windows License Manager and PushToInstall recovery and bounded installation or licensing readiness conclusions.
- Add payload-sanitized account broker history and guarded Web Account Manager and Microsoft Account Sign-in Assistant recovery without collecting or changing account data.
- Add ScheduledDefrag history with guarded Storage Service and Optimize Drives recovery without selecting or optimizing drives.
- Add guarded Windows Image Acquisition and Function Discovery service recovery with imaging-device inventory and current network-profile context.
- Add guarded Human Interface Device, Portable Device Enumerator and Windows Font Cache service recovery with device, driver and font-registration diagnostics.
- Add guarded Device Association, Text Input Management and Display Enhancement recovery with related device and event evidence.
- Add coordinated COM+ Event System and System Event Notification Service diagnosis and recovery with ordered prerequisite verification.
- Add bounded, read-only Storage Spaces pool and virtual-disk health, capacity, allocation and resiliency diagnosis.
- Add bounded current Storage module job evidence to Storage, Full system scan and CLI.
- Add privacy-bounded per-volume disk quota settings to Storage, Full system scan and CLI.
- Add VSS shadow-storage pressure to Storage, Backup services and backup readiness, plus selected-volume NTFS self-healing evidence to Storage.
- Add registered printer-driver file presence and executable signature review to Printing readiness.
- Add printer driver model and package-awareness evidence to Printing readiness.
- Add effective default-route metric comparison and equal-priority review to Network readiness.
- Add per-interface MTU, DHCP, forwarding, discovery and metric evidence to Network readiness.
- Add privacy-bounded neighbor reachability evidence to Network readiness.
- Add core network-adapter binding review to Network readiness without changing protocols.
- Add bounded network-adapter error and discard counters to Network readiness.
- Add a bounded Network readiness correlation across firewall, route, DNS, gateway and Winsock evidence.
- Add page-file evidence and a bounded Storage readiness correlation across capacity, dirty state, jobs, physical disks, events and exact service history.
- Add Storage Sense policy plus cleanup and disk-diagnostic task and event correlation without running tasks or disk operations.
- Add read-only Windows reserved-storage state to servicing and Windows Update readiness.
- Add a bounded Windows Update correlation across servicing, restart, capacity, transfer and network prerequisites.
- Add a key-free Windows activation workflow with payload-sanitized Software Protection event history.
- Add sanitized startup and shutdown performance history with bounded numeric duration evidence.
- Add privacy-filtered startup inventory across registry, Startup folders and boot or logon scheduled tasks.
- Require automatic startup configuration before a stopped service can authorize automatic recovery; idle demand-start services remain unchanged.
- Add guarded Credential Manager service recovery without collecting or changing stored credentials.
- Add Device Setup Manager and Plug and Play configuration history, plus storage and portable-device context for guarded Shell Hardware Detection and Device Install service recovery.

- Expand Services with recent startup, dependency, timeout and unexpected-termination diagnosis.
- Add a bounded Services correlation across registrations, configuration, current states and exact failure history.

- Add bounded WMI activity history to WMI diagnosis.

- Add bounded Microsoft Defender event and threat history with privacy-preserving current active counts to security diagnosis and the CLI.

- Add signed knowledge database retrieval, activation, retained-version recovery and update status with bounded HTTPS transport and CLI access.

- Add bounded Windows Installer activity, exact product evidence and guarded exact-product repair with shared recipe discovery and durable no-repeat reconciliation across the workbench, Engine and CLI.

- Add a read-only installed MSI application picker to the desktop Installer workflow.

- Add guarded standalone-workgroup computer renaming with restart-required and no-repeat recovery outcomes across the app, Engine and CLI.

- Add bounded System Restore point inventory to Diagnostics, Common problems, the Engine and CLI.

- Add bounded Windows Recovery Environment status and location evidence to Diagnostics, Common problems, the Engine and CLI.

- Add guarded Windows Recovery Environment enablement with fresh pre-command eligibility, honest no-dispatch outcomes, exact verification and later-boot no-repeat reconciliation.

- Add bounded personal-certificate expiration evidence to Security, Common problems, the Engine and CLI.

- Add SMB server, share, network and firewall context with guarded Server service recovery across the workbench, Engine and CLI.

- Add guarded Task Scheduler service recovery with bounded task runtime and missed-run evidence, explicit missed-task effects and preserved task definitions across the workbench, Engine and CLI.

- Add focused guarded BITS, Cryptographic Services and Windows Modules Installer recovery workflows without requiring a full servicing scan.

- Add reviewed Windows error catalogs, installed Win32 and NTSTATUS message generators, and transparent Win32 HRESULT lookup.

- Add read-only Windows Update source, history, context and policy evidence with automatic service, transfer, routing, Hosts, proxy and servicing follow-ups for recognized failures.

- Add source-specific restart evidence and fail-closed restart prerequisites to the Engine, workflows and CLI.

- Add guarded NTFS repair with non-system dismount consent, system-volume restart scheduling, honest dispatch tracking and no-repeat reconciliation across the desktop, Engine and CLI.

- Add final exact-job dispatch gating and two-snapshot BITS retry reconciliation that archives stable outcomes and never repeats a transfer request.

- Add guarded BitLocker recovery-file backup with repeated pre-write eligibility and honest interruption reconciliation across the app, Engine and CLI.

- Add bounded Windows, activation, system, account, processor clock/load, graphics, memory-module and battery runtime evidence to Vitals in the Engine, workflows and CLI.

- Add bounded Windows Reliability Monitor history with grouped crash, hang and unexpected-shutdown evidence to Diagnostics, Common problems, the Engine and CLI.

- Add bounded WHEA event history with grouped hardware-error evidence to Diagnostics, Common problems, the Engine and CLI.

- Add bounded SetupAPI driver-install history with exact current-device correlation and log evidence to Diagnostics, Common problems, the Engine and CLI.

- Reject contradictory service state labels, retain the last observation and report embedded provider failures as unverified.

- Verify cleared printer offline settings in both operation and follow-up snapshots.

- Report volume dirty-bit state through Common problems, Storage, the Engine and CLI without scheduling repair or restart.

- Require one exact CHKDSK clean verdict line before reporting a clear filesystem check.

- Match DHCP adapter SettingID across renewal and verification to reject reused adapter indexes.

- Verify resumed print queues and renewed DHCP adapters against both operation and follow-up snapshots.

- Reject malformed and duplicate Windows Time status fields before selecting or verifying synchronization.

- Preserve completed task-plan evidence in CLI and desktop reports when later inspection fails or is canceled.

- Repeat exact task-registration eligibility at dispatch and archive attempts that send no creation request.

- Flag DLLs and unverified executable images for review in task-plan inspection.

- Inspect live Windows Search catalog activity and pause reasons, then refresh after service recovery.

- Add exact stored task-definition inspection through the Engine and CLI, including task-conflict reviews and exported reports.

- Include Authenticode signature observations in scheduled-task inspection and CLI review using the bundled PowerShell security module.

- Report PE header, machine and DLL details during scheduled-task executable review.

- Distinguish an administrator prerequisite from other unresolved workflow findings.

- Include grouped Windows Search event history in the Search workflow.

- Include current Windows Installer activity and Windows Search catalog and event evidence in Common problems.

- Handle missing profile-removal and recovery provider operations and preserve unresolved recovery records.

- Add guarded Wired AutoConfig recovery for 802.1X networks.
- Add guarded hostname-specific DNS cache recovery with honest dispatch tracking, verification and no-repeat interruption review.

- Add Bluetooth, camera, smart-card and biometric device and driver diagnostics with guarded service recovery.

- Recognize never-started service status without misclassifying it as a failed start.

- Bound combined-diagnostic checkpoint waits and honor cancellation during saves.

- Add scheduled-task result guidance, run timestamps and missed-run diagnostics without retrying tasks.

- Add wireless connection history with guarded WLAN service recovery and explicit connectivity limits.
- Add wired 802.1X authentication history with guarded Wired AutoConfig service recovery.

- Defer automatic service starts when Windows reports an unmapped failure code.

- Add selectable storage reliability counters to Storage and Common problems, with temperature, wear and uncorrected I/O review findings.

- Add disabled scheduled-task XML export through the app, Engine and CLI for review without registration.

- Add disk event 7, 9, 11 and 51 guidance and retain unavailable storage-provider outcomes without selecting repairs.

- Bound repair-provider waits, isolate display failures, reject missing evidence, pass Stop through native repairs, and preserve pending recovery before retry.

- Add firewall profile inspection to Common problems and the optional Firewall services workflow, with dependency ordering, guarded starts and running-state verification.

- Add print-job status diagnostics to Printing and Common problems, including intervention guidance, bounded collection and explicit provider failures.

- Recognize eight more Windows Installer outcomes with targeted guidance and retained log references.

- Bound recipe checkpoint, workflow, repair and recovery journal waits; preserve prior records on incomplete writes and stop when persistence fails.

- Task-plan import accepts UTF-8 byte-order marks while preserving the full-file size limit.

- Task registration preserves evidence and pending recovery when Stop arrives with a completed provider response.

- Task registration preserves blocked or unverified outcomes when providers or journal saves return missing tasks or results.

- DNS and TCP evidence records retain the configured timeout for exported results.

- Network workflow options include a timeout slider for optional DNS and TCP checks.

- DNS and TCP CLI checks support configurable one-to-sixty-second timeouts and explicit provider-failure exits.

- Embedded DNS and TCP checks report missing provider tasks and address results as explicit failures.

- Service inspection honors Stop during progress callbacks before querying or publishing completion.

- Service workflows and CLI stream each observation as it completes, including unavailable checks; CLI progress stays separate from JSON output.

- Read-only service inspection retains provider errors as unknown observations while continuing independent checks.

- Service diagnostics recognize startup timeouts and unexpected exits, retaining evidence and deferring automatic retries for review.

- Update planning rejects failed discovery evidence and malformed classifications from embedded providers.

- Update execution rejects provider results that change the requested software or driver scope before installation.

- Desktop, Engine and CLI update discovery support product metadata and combined product filters.
- Update search lists discovered classifications and supports desktop and CLI classification filtering alongside title and download-state filters.
- Update result filters distinguish incomplete discovery, no matching updates and discovery not yet run.
- Update discovery isolates progress callback failures and bounds embedded provider waits with cancellation support.
- Conflicting update metadata and duplicate or empty update identities cannot establish installation eligibility.
- Repair-provider waits now honor Stop and execution deadlines, retaining uncertain outcomes and preventing subsequent repairs.
- Recovery suggestions reject duplicate recipe results and prerequisites with mismatched versions or repair actions.
- Interrupted-repair recovery bounds saved-record reads and rejects failed startup queries, ambiguous boot events and duplicate fields.
- Printing diagnostics and CLI now check queue-to-port registrations and flag unmatched or incomplete evidence without changing printers.
- Engine and CLI support focused diagnostic scans with explicitly selected areas, including scheduled tasks and registered app package status with fault-specific follow-up guidance.
- Conflicting duplicate package identities are excluded from diagnosis and selection while their raw evidence is retained.
- Task diagnostics reject ambiguous inventory records and report missing action counts as incomplete evidence while preserving intentional manual and disabled-task states.
- Saved workflow journals can be reviewed and exported from the desktop or CLI without rerunning diagnostics. Imported results retain their historical scope.
- Common problems adds backup history, VSS writer and provider state, and BITS inventory. Failed or incomplete evidence stays unresolved in summaries, repair prerequisites and next-step guidance.

- Desktop, Engine and CLI compare knowledge-pack revisions with added, removed and changed reference counts.

- Installer log diagnosis adds patch-removal and sequencing outcomes with specific review guidance.

- Network diagnostics include inactive adapter link states and preserve unknown states without changing connections.

- Desktop and CLI add recovery-protector discovery and explicit file backup. Backup workflows stop waiting on stalled providers and never accept a late result as verified success.

- Vitals, Common problems, Engine and CLI collect crash reports, inspect dump configuration and discover default/configured dump metadata. Vitals also includes unexpected shutdown history; files and settings are preserved.

- Desktop, Engine and CLI add shared-recipe discovery for targeted BITS retry, with explicit selection, fresh owner/state checks, live progress, persistent attempt guards and distinct request, transfer and uncertain outcomes.

- Windows Update includes BITS job evidence. Maintenance adds account-scoped transfer inventory and exact-job review, also exposed through Engine and CLI, without changing transfers.

- Added a backup-service workflow with VSS/volsnap history, writer status and optional VSS and software-provider starts, dependency guards and running-state verification. Snapshot and backup settings are preserved.

- Desktop saves application inventories for later comparison; desktop, Engine and CLI compare registration and version differences while rejecting incomplete or mismatched scopes.

- Maintenance, Engine and CLI inventory machine-wide or running-account application registrations with registry identities, versions and publishers.

- Package workflows optionally include provisioned packages for new users, with separate evidence and coverage from existing user registrations.

- UI tests use a separate, non-visible desktop so test resizing and menus cannot interrupt the working desktop.

- Maintenance includes registered app-package inventory, Windows-reported status and selected-package review for the running account or all users. Engine and CLI also review exact targets with dependent packages and protection flags. Removal remains unavailable; the inventory CLI rejects auto-repair.

- Recipe metadata and the desktop check selector include repair effects and preservation limits for pre-run review.

- Service-verification timeouts retain the last observed state; workflow reports show bounded captured output without claiming successful recovery.

- Installer log diagnosis recognizes patch-format, patch-access and customization-policy failures, and retains successful restart outcomes as informational history.

- Common problems collects unexpected-shutdown history; event analysis groups records with source references and investigation guidance without inferring a cause or automatic repair.

- Reference catalogs support context-specific text search in the app, Engine and CLI, with bounded results and total match counts.

- Common problems includes application, service and clock history, system-volume capacity, live evidence and per-area collection deadlines. Timed-out preliminary collection defers repairs until fresh checks.

- Installer history and imported events identify feature detection, configuration and package failures with grouped evidence and specific investigation guidance.

- Storage history and imported events identify filesystem errors, controller resets, I/O retries and unexpected removal with grouped evidence and targeted guidance.

- Catalog tooling supports read-only schema validation with repeatability and file-preservation checks.

- Knowledge tooling imports retained IEEE MA-L, MA-M and MA-S CSV snapshots into a deterministic unsigned MAC-vendor pack with source fingerprints and strict prefix validation; the Engine and CLI perform local longest-prefix lookup without vendor claims for randomized or multicast addresses.

- Knowledge tooling converts reviewed driver-reference CSV into deterministic schema 2 packs with exact applicability fields, source fingerprints and Engine validation.

- Open saved EVTX files in the app or investigate them through the Engine and CLI, with bounded event analysis and explicit archive coverage.

- Network and Common problems can recover enabled, stopped SMB client, Network Location Awareness, Network List and IP Helper services while preserving their configuration and verifying service state.

- Cleanup supports current-user Office caches and a selectable limit of up to 10,000 files per target in the app, Engine and CLI, with existing eligibility and time limits preserved.

- Workflow recovery retains the mutation guard through target-specific verification and prioritizes interrupted work, failures, timeouts and restarts before another repair.

- Common problems includes network, proxy, licensing and event-channel state plus storage, installer and printing history, retaining incomplete collection as unknown.

- Common problems has a collapsed advanced check selector that shows prerequisite expansion and resets auto-repair when selection changes.

- CLI workflows accept --recipes selections with dependency expansion and validation before collection.

- Journaled workflows accept selected recipe subsets and record requested scopes separately from automatically included prerequisites.

- Engine consumers can select a workflow subset with required recipe dependencies included automatically and out-of-scope selections rejected.

- The recipe engine rejects error-bearing diagnostic evidence even when a custom provider claims a clear or repairable result.

- DHCP, printing, service, time and WMI repair parsers reject command errors before accepting diagnostic or verification evidence.

- Selected-printer recovery rejects command errors before interpreting eligibility or verifying changed queues.

- Live diagnostic previews prioritize faults and incomplete evidence when large inventories exceed the display limit.

- Time configuration findings retain the effective-query evidence and explain failures such as a stopped Windows Time service.

- Added standalone common-check CLI diagnostics with combined JSON evidence and live progress, without entering a repair workflow.

- Combined scans show completed findings and review counts in live output, with bounded previews and full details retained in the journal.

- CLI recipe listings can now show a workflow's ordered dependencies, verification scopes and diagnostic deadlines without running it.

- Recipe diagnostics have explicit deadlines; expired checks retain timeout evidence and defer subsequent repairs even when a provider ignores cancellation.

- Workflow reports and JSON now summarize verified repairs, available repairs, unresolved findings and incomplete or deferred work without double-counting recipe evidence.

- Combined diagnostics save completed evidence during collection and retain it after Stop; late provider checkpoints cannot overwrite the final journal.

- Common problems now includes device faults, vitals, printer-driver matching, security and time configuration, with independent collection failures and retained evidence.

- DISM, SFC and component cleanup reject command error output before repair eligibility or verification; integrity results retain raw evidence.

- Added Common problems to run 23 existing recipes in one session, including Installer recovery that rechecks policy and in-progress markers before starting the service.

- Added update filters and standalone JSON `update-search`, selection-specific planning guidance and installation buttons that enable only for eligible selections. Controls wrap inside the sidebar.

- Stop releases stalled recipe diagnostics and update prerequisite checks. Update discovery retains raw evidence and blocks installation planning when collection reports errors.

- Added standalone security diagnostics with JSON evidence for TPM, Secure Boot and virtualization protections. Defender refresh eligibility and verification reject status output accompanied by errors.

- Added Device problems diagnostics across Plug and Play classes in the app, Engine and CLI, retaining identified faults when names or classes are missing and providing next steps for faults or incomplete evidence.

- Added Input devices diagnostics for keyboard, mouse and HID errors in the app, Engine and CLI.

- Profile review and the removal coordinator honor Stop with stalled providers and have bounded execution deadlines.

- Added `printer-drivers` JSON diagnostics, retained bounded observations from large queue inventories and rejected registration matches from incomplete command evidence.

- Added Display diagnostics and standalone `device-check` JSON for audio, network, storage, printing and display devices. Failed driver collection cannot report a verified inventory match.

- Storage diagnostics identify provider-reported unhealthy disks and honor Stop when an asynchronous provider stalls. Standalone `vitals-check` provides memory and disk snapshots as JSON.

- Scheduled task diagnostics flag enabled tasks without actions and distinguish unknown task states, with standalone JSON results through `task-check`.

- Time-source changes stop when command error output leaves the result uncertain.

- Event Log diagnostics now inspect channel availability and retention settings, also available through the CLI.

- Network recovery now includes guarded DNS Client service recovery without changing DNS settings.

- Added Application error history to Diagnostics and the shared workflow API with reported event details.

- Storage reports selected-volume capacity and flags low available space for review.

- Added Windows Event Log service inspection and guarded recovery to Repair and the shared CLI workflow catalog.

- Workflow reports lead with next steps, including elevation guidance, and unresolved items. Console summaries retain the recorded outcome and check counts while filtering.

- Network diagnostics include stored WinHTTP and current-account Internet proxy settings with preserved evidence.

- Windows Time diagnostics now report source configuration, domain role and policy ownership with preserved evidence.

- Bound time-source prerequisite collection and preserve unknown status for unavailable or malformed environment data.

- Report specific service exit-code findings and enforce review requirements at the final service-start gate.

- Inspect 12 supported Windows services from Diagnostics or the CLI, retaining state and dependency evidence without starting services.

- Service recovery requires two consecutive Running observations before reporting verification.

- Keep the workspace within the window when changing tools or zoom. Simplify the CRT frame to a single thin outline.

- Installer readiness reports machine and user AlwaysInstallElevated policy exposure without changing managed settings. Collection failures retain unknown readiness and error evidence while other diagnostics continue.
- Standalone CLI commands expose Installer readiness, MSI history and PrintService history with JSON evidence and incomplete-result exit codes.
- Installer failure summaries show reported product, package and error fields while retaining full event evidence.

- Network findings distinguish missing host, gateway and DNS configuration with specific review guidance. Non-admin scans retain read-only DHCP service and lease evidence before the recovery elevation gate.
- Unreadable adapter configuration no longer prevents inspecting the remaining network interfaces.
- Hostname and TCP checks retain structured targets, results, socket codes and timeout evidence in workflow journals, with targeted review guidance.

- DHCP renewal validates adapter identities and address evidence before changing any lease, and rejects ambiguous or overflowing result fields. The dhcp-check CLI command provides read-only eligibility and evidence.

- Printing workflows collect device errors, hardware IDs and PnP drivers, then match queue driver names and ports against local registrations. Queue/driver inventory is also available through the printer-drivers CLI command; ambiguous matches retain review details without selecting changes.
- Print-queue recovery validates all local queue identities and states before resuming, and rejects ambiguous repair evidence.
- Printing workflows include recent PrintService warnings and errors with bounded event-field previews, retained evidence and incomplete-history reporting.
- WMI workflows and the standalone wmi-provider CLI command include a bounded read-only operating-system provider query, separate from repository consistency and repair decisions.
- WMI workflows check service availability before repository work and can start an eligible stopped service with dependency checks and verification. Shared service repair verification requires the exact service identity and running state.
- WMI repository checks require a single complete verdict and reject timed-out, quoted or repeated output before selecting salvage.
- Service recovery checks nested dependencies with cycle and size limits, retains prerequisite evidence in reports, and requires matching identity and complete stopped-service evidence.
- Non-elevated workflows collect available service states, preserve independent results when a provider fails, and classify incomplete evidence as unknown. Repairs still require elevation.
- Foxit inventory includes read-only updater values with exact registry paths, types and bounded traversal, preserving readable observations when other paths fail.
- Workflows show recipe outcomes live and identify blocked prerequisites. Repair calls that end without a result retain execution uncertainty and block later work.
- Missing diagnostic results stay unknown, block dependent steps and preserve independent checks.
- Windows Time checks include bounded operational history with warning summaries and retained event evidence.
- Installer checks inspect machine and process-account rollback policies, and log analysis recognizes 17 MSI outcomes with specific review guidance and source references.
- Added scheduled-task inventory, calendar previews and validated action/account review drafts in the Engine and CLI, plus a supplied-host registration coordinator requiring recovery journaling before creation. Recovery review rejects malformed attempt identities without crashing.
- Added bounded TCP endpoint checks to the Engine, CLI and desktop Network workflow.
- Storage checks include disk and controller device errors, driver inventory and bounded storage-event history with device-field previews and retained XML, also available through the standalone storage-events CLI command.
- Malformed memory data no longer hides independent disk-health findings.
- Device findings retain provider evidence and report driver-query failures separately from valid device observations.

- Repair workflows preserve their starting definitions and stop for review when a provider returns an unexpected repair identity or missing command evidence.
- Workflow reports show command failure text, exit codes, timeouts and incomplete capture, with recorded output retained in the journal.
- Repair CLI exit codes distinguish verified success, restart and cancellation; incomplete evidence cannot report success and workflow prerequisite failures return a handled error.
- Service recovery rechecks dependencies before starting and verifies the requested service identity, including manual repairs.

- Service diagnosis identifies recorded startup, account, dependency and pending-removal failures, retains evidence and defers inappropriate automatic starts.

- Update installation repeats exact readiness and selected-update eligibility after durable preparation, preserves interrupted-operation records and rejects incomplete outcomes as success.

- Zoom enlarges inputs and output inside fixed panes; workspace scrollbars are disabled from startup and tool changes preserve the main workspace size.

- Renamed the shared library to KillerBench.Engine, formally The KillerBench.Engine, across projects, namespaces and solution references.

- Added a slim, softly shaded CRT bezel with compact phosphor color dots beneath the screen.

- Added versioned knowledge catalogs, offline Windows code lookup, and Engine/CLI driver-reference filtering by exact hardware ID and declared OS scope, with source and applicability details.
- Audio and network diagnostics include driver versions and providers matched by exact device ID.
- Added optional hostname lookup in the Network tool, Engine and CLI with distinct failure, empty-answer and timeout results.
- Storage checks now include provider-reported physical disk health. Timed-out filesystem and servicing evidence remains unverified and cannot authorize repairs.
- Reorganized the workbench with compact raised category buttons that light up when selected, a collapsible tool sidebar with its icon rail beside the main pane, and a separate inspector, sources and activity log.
- Added printer discovery and selected local-queue diagnostics in app, Engine and CLI, explicit Engine recovery targets, and rejection of ambiguous queue evidence.
- Preserved printer selection during discovery refreshes and made switching back to general checks explicit.
- Integrated selected-printer plans with the Engine repair runner, journals and target-specific verification, optional repair through GO, and CLI previews and explicit execution.
- Added time-source previews, explicit custom-peer configuration in the app and CLI, before/after evidence, verification and bounded recovery without retrying changes.
- Added read-only profile review with age controls, individual selections, fresh dependency checks and saved plans in app, Engine and CLI, plus a CLI recovery check. Native profile deletion remains unavailable.
- Added component-store analysis and optional recommended cleanup with servicing guards and follow-up verification. Native cleanup still needs disposable-machine validation.
- Added configurable software/driver discovery, app and CLI update planning and explicit installation with verification, journals and an interrupted-update check that rejects ambiguous recovery records. Native installation still needs disposable-machine validation.
- Added selectable SHADE privacy snapshots in app, Engine and CLI, plus BitLocker options previews and guarded file-backup primitives. Privacy changes outside the guarded controls and native encryption remain unfinished.
- Added selectable TPM, Secure Boot, VBS, memory integrity and Credential Guard diagnostics in app and CLI, plus optional verified Defender signature refresh while preserving protection settings and policy.
- Added configurable temporary-file, cache, Windows error-report, application crash-dump and Windows minidump cleanup with selected targets, an age slider, preview by default and optional permanent removal with per-file rechecks and journals in app, Engine and CLI.
- Added verified recovery for paused and offline local print queues and confirmed WMI repository corruption.
- Added bounded filesystem diagnostics, network and audio device-error checks, memory, disk-health and BitLocker snapshots, and explicit diagnostic-only recipe support.
- Added versioned repair recipes, dependency handling, diagnostic checkpoints and explicit restart-required outcomes that defer later work.
- Added DHCP lease recovery, update, audio, search, printing and Installer service recovery with readiness checks, and Windows Time synchronization using configured sources.
- Fixed diagnostic cancellation, bounded servicing commands, and added persistent protection against repeating interrupted repairs.
- Workflow scans show live output, refresh device checks after recovery, and retain completed repairs when follow-up checks fail. Final reports separate resolved repairs from remaining findings with a clear next action.
- KillerBench now requests administrator access at startup through Windows UAC.
- Simplified the main workflow controls, added tactile phosphor buttons and a status light, sharpened help, and exposed the pane shadow above the footer.
- Added Windows Update, system-file, vitals and local network workflows, with optional automatic repairs, verification, saved results and CLI support.
- Added read-only Windows Update scans, Operational event history, grouped findings, saved captures, and matching CLI commands with progress and partial results.
- Expanded diagnostic sources, consolidated evidence navigation, and added previewed DISM/SFC repairs with verification and execution journals.
- Started the diagnostic workbench with 13 family themes, CRT display controls, evidence sidebars, a separate activity log, and bounded text-log inspection.
- Refined pane spacing and the footer, adopted the family theme picker, set Dark/Amber (Orange accent) as the default, and wired 15 language choices with English fallback.
- Restored continuous workspace grain and family pane borders and shadows.
- Added a pre-release landing shell with family artwork.
- Added a shared log-analysis API and CLI with JSON output and source references; styled evidence headings with the accented typewriter font.
- Added initial CBS and Setup findings, evidence navigation, JSON report export, and synthetic investigation examples.
