# Diagnostic workflows

Workflows collect related evidence and return one `WorkflowResult`. They are the simplest high-level Engine surface for desktop applications, automation hosts, and support tools.

## Windows Update Medic Service

The focused `update-medic-service` workflow combines exact current `WaaSMedicSvc` state, registered ServiceDll presence and signature status, bounded current Windows Update activity and exact Service Control Manager failure history into one conservative conclusion. Incomplete evidence takes precedence. DLL observations, activity and history never authorize a start or component action. A stopped demand-start state remains informational in every broader workflow. When this exact tool is selected, optional recovery can issue one bounded start only after complete dependency, executable registration, administrator, restart and fresh final evidence checks pass.

Starting the service can resume Windows remediation or update activity under the current configuration. KillerBench preserves update policy, source enrollment, schedules, packages, BITS jobs, restart settings, service permissions and startup type. Verification covers stable service state only. It does not claim remediation, component health, applicability, download, installation or restart readiness.

The focused `windows-update-service` workflow combines exact current state, registered ServiceDll presence and signature status, bounded current Windows Update activity and exact Service Control Manager failure history into one conservative conclusion. Incomplete evidence takes precedence. DLL observations, activity and history never authorize its separately guarded start. The service can start only from a stopped automatic state after fresh dependency and executable-registration checks. Starting it may resume existing update activity under current policy. Policy, source enrollment, schedules, restart settings, packages, BITS jobs, service permissions and startup type remain unchanged. Stable service state does not establish source reachability, applicability, download, installation, completion or restart readiness.

BITS, Cryptographic Services, Windows Modules Installer and Update Orchestrator use their own focused evidence combinations and conservative conclusions. Historical events remain read-only context and cannot authorize a start or component change.

The focused `delivery-optimization-service` workflow shows exact service state, privacy-bounded current transfer aggregates, recognized policy surfaces, exact Service Control Manager failure history and one combined conclusion. Missing evidence takes precedence, while service, policy or historical concerns remain review-only. Historical failures cannot authorize continuation. Running and stopped demand-start states remain informational. Only an exact Paused current state can offer the separate recovery. Execution repeats that state immediately before one bounded continue request and requires two coherent Running observations. Startup type, cache contents, peers, bandwidth, update source and managed policy remain unchanged. Verification does not claim transfer activity, cache health, peer reachability, update progress or completion.

The focused `bits-service` workflow shows current-account transfer jobs, exact current service state, bounded exact Service Control Manager failure history and one combined conclusion before its service recipe is evaluated. Incomplete evidence takes precedence. Historical failures and transfer snapshots cannot authorize a start. Optional recovery still requires exact current stopped-service evidence, dependencies, executable registration, administrator access, clear restart evidence and one unchanged final check. Jobs, priorities, owners, destinations, proxy settings, policy and startup type remain unchanged. Stable service state does not prove transfer progress, application ownership, acknowledgment or completion.

The focused `cryptographic-service` workflow shows exact current service state, registered service DLL presence and signature status, bounded exact Service Control Manager failure history and one combined conclusion. Incomplete evidence takes precedence. Historical failures and DLL observations cannot authorize a service start or component replacement. Optional recovery still requires the normal exact-service prerequisites and one unchanged final check. Certificates, keys, catalogs, trust settings, policy and startup type remain unchanged. Stable service state and a present Valid DLL do not validate certificates, signatures, catalogs, trust chains, update payloads or application behavior.

The focused `modules-installer-service` workflow shows exact current service state, bounded servicing package transitions, exact Service Control Manager failure history and one combined conclusion. Incomplete evidence takes precedence. Historical failures and package states cannot authorize a service start or package operation. Optional recovery still requires the normal exact-service prerequisites and one unchanged final check. Packages, component-store content, sources, policy and startup type remain unchanged. Stable service state does not establish component-store health, package applicability, servicing completion or restart readiness.

The focused `update-orchestrator-service` workflow shows exact current service state, the exact built-in Windows Update Scheduled Start task, bounded current Windows Update activity, exact Service Control Manager failure history and one combined conclusion. Incomplete evidence takes precedence. Task results, activity and historical failures cannot authorize a service start or update operation. Optional recovery still requires the normal exact-service prerequisites and one unchanged final check. Policy, source enrollment, schedules, restart settings, packages, BITS jobs, service permissions and startup type remain unchanged. Stable service state does not establish applicability, download progress, installation progress, completion or restart readiness.

The focused `error-reporting-service` workflow combines stored crash-dump configuration, bounded dump-file metadata, recent system crash reports, exact Windows Error Reporting service-failure history and current service readiness into one conclusion. Incomplete evidence takes precedence. Historical reports and service failures remain context and never authorize recovery or report handling. The workflow does not open dump contents, submit or delete reports, change consent or upload policy, infer root cause or claim report delivery.

The focused `diagnostic-policy-service` workflow combines up to 200 Operational events from the last seven days, exact Diagnostic Policy Service failure history and current service readiness into one conclusion. Incomplete evidence takes precedence. Event summaries group warning, error and critical records by event ID and level while keeping account, process, scenario, troubleshooter and session payload in raw evidence. Historical records cannot authorize service recovery or a troubleshooting action. The workflow does not run a troubleshooter, accept a suggested fix, change policy or troubleshooting data, infer root cause, or claim that diagnosis or repair completed.

The focused `event-collector-service` workflow combines bounded subscription inventory, exact Windows Event Collector service-failure history and current service readiness into one conclusion. Incomplete evidence takes precedence. An empty inventory can be intentional, while subscription counts and historical failures remain context and cannot authorize recovery or subscription changes. The workflow does not establish source reachability, authentication, event delivery, event freshness or subscription health.

The focused `remote-access-service` workflow combines bounded current-account VPN profile configuration, exact Remote Access Connection Manager service-failure history and current service readiness into one conclusion. Incomplete evidence takes precedence. An empty inventory can be intentional, connection labels remain snapshots, and profile presence or historical failures cannot authorize recovery or a connection attempt. The workflow does not expose credentials, initiate a connection or establish profile applicability, server reachability, authentication, tunnel negotiation, route installation, name resolution or connectivity.

The focused `remote-desktop-services` workflow combines local enablement, NLA and listener-port configuration with bounded connection and session history, exact service-failure history and current service readiness. Incomplete evidence takes precedence. Current NLA or service concerns request review, while retained connection, session and service failures remain historical context and cannot authorize recovery or configuration changes. The workflow does not establish effective policy, listener binding, firewall access, certificate identity, permissions, authentication, network reachability, connection success or session creation.

The focused `windows-remote-management` workflow combines bounded WinRM listener configuration, operational history, exact service-failure history and current service readiness. Incomplete evidence takes precedence. Disabled listener registrations or current service concerns request review, while retained events and service failures remain historical context and cannot authorize recovery or configuration changes. The workflow does not establish effective policy, listener binding, firewall access, certificate validity, permissions, authentication, client reachability or remote command execution.

The focused `security-center-service` workflow combines bounded antivirus registrations with raw provider state, exact Security Center service-failure history and current service readiness. Incomplete evidence takes precedence. An empty inventory is not a protection-health verdict, and registration count, raw state or historical failures cannot authorize recovery or a security change. The workflow does not establish antivirus presence, protection state, signature freshness, notification delivery, registration ownership or effective policy.

The focused `network-connections-service` workflow combines bounded current network profile assignments, exact Network Connections service-failure history and current service readiness. Incomplete evidence takes precedence. Public profiles and historical failures remain context and cannot authorize recovery or a network change. The workflow does not establish adapter availability, address assignment, route selection, name resolution, firewall access, Internet reachability or connection success.

The focused `winhttp-autoproxy-service` workflow combines stored WinHTTP defaults, current-account Internet settings, exact service-failure history and current service readiness. Incomplete evidence takes precedence. Configuration and history cannot authorize recovery or discovery. The workflow does not fetch a URL or establish PAC or WPAD resolution, proxy reachability, authentication, policy ownership, application overrides or Internet access.

The focused `connection-manager-service` workflow combines bounded current network profile assignments, exact Windows Connection Manager failure history and current service readiness. Incomplete evidence takes precedence. Public profiles and historical failures remain context and cannot authorize recovery or a connection decision. The workflow does not establish profile applicability, connection selection, metering, roaming, authentication, routing, name resolution or connectivity.

The focused `network-setup-service` workflow combines bounded current adapter protocol bindings, exact Network Setup Service failure history and current service readiness. Incomplete evidence takes precedence. An active hardware adapter with both IPv4 and IPv6 disabled requests review, while binding state and historical failures cannot authorize recovery or a setup change. The workflow does not establish adapter availability, driver suitability, pending setup completion, address assignment, routing, name resolution or connectivity.

The focused `storage-service` workflow combines bounded current Storage-module jobs, exact Storage Service failure history and current service readiness. Incomplete evidence takes precedence. Suspended or failed jobs and current service concerns request review, while historical failures remain context. Job state cannot authorize service recovery or storage changes, and a completed job is not an operation-result verdict.

## Focused workflows

`WorkflowKind` identifies a diagnostic area. Current groups include:

| Group | Examples |
| --- | --- |
| Servicing | Windows Update, system files, component cleanup, installer, servicing activity |
| Hardware | Devices, storage, audio, camera, Bluetooth, display, input |
| Network | Network, wireless, wired authentication, firewall services, file sharing |
| Windows services | BITS, Cryptographic Services, Windows Modules Installer, Task Scheduler, WinRM |
| User environment | Profiles, applications, startup applications, Appx packages, privacy |
| Recovery, backup and security | Backup services, Defender, BitLocker, System Restore, Windows Recovery, certificates |
| Reliability | Vitals, reliability history, hardware errors, crash evidence, event logs |

The enum is intentionally broader than the repair catalog. A diagnostic workflow can exist without a safe automatic repair.

## Check states

| State | Meaning |
| --- | --- |
| `Clear` | Complete evidence matched the workflow's healthy condition |
| `Repairable` | Complete current evidence matched a supported repair recipe |
| `Attention` | Evidence deserves review, but it does not authorize a repair by itself |
| `Unknown` | Collection or interpretation was incomplete, contradictory, truncated, or unavailable |
| `Information` | Context or history that is not a current fault verdict |

Historical warnings and errors usually produce `Attention`, not `Repairable`. An empty event history can be useful context, but it does not certify current health.

The Network workflow also returns a DNS operational correlation. It combines the current DNS Client service state, per-interface server configuration, effective namespace policy, route alignment and both recent DNS Client event sources. Incomplete evidence takes precedence. Historical warnings remain supporting context and cannot establish a current outage or select a repair. Use an exact hostname lookup when current evidence or the reported symptom needs live validation.

The DHCP operational correlation combines the current DHCP Client service, bounded lease evidence, current IP address results and all four IPv4 and IPv6 Client event sources. Current lease or address problems request review without selecting renewal. Historical warnings alone remain informational, and incomplete evidence prevents a readiness conclusion.

The top-level Network operational result then combines firewall alignment, route and gateway consistency, Winsock structure, DNS readiness and DHCP readiness. It summarizes the diagnostic layers without adding a public probe, selecting a reset or converting history into a current fault.

The Printing workflow returns a printing environment result that combines Point and Print policy, rendering isolation, queue and driver registrations, registered file checks, ports, configured TCP/IP endpoints, bounded endpoint reachability, and print processor and monitor registrations. Its top-level operational result combines that environment with the current Print Spooler, default selection, queue conditions, queue attributes, visible jobs, spool-directory state and exact service history. Incomplete current evidence prevents a conclusion. Historical service failures alone remain informational and never select a repair.

The Installer workflow returns an installation environment result across Installer readiness, security and patch policy, logging policy, cached-package visibility, source metadata, correlated MSI outcomes, AppX deployment history and Microsoft Store installation history. Current cache, source or policy concerns request review. Historical deployment failures alone remain informational, and incomplete evidence prevents a conclusion. The result never selects a retry, repair, package, source, service or policy change.

The Storage workflow returns a storage environment result across Storage Spaces, shadow storage, selected-volume self-healing, delete notifications, paging-file state, disk quotas, Storage Sense policy and task state, Disk Cleanup task state and disk diagnostic history. Current configuration concerns request review. Historical disk diagnostics alone remain informational, and incomplete evidence prevents a conclusion. This stays separate from the operational disk and filesystem result so configuration evidence is not presented as physical damage.

Focused service workflows with exact failure history return one service-readiness result after their recipe checks. It reports verified recovery, current issues and incomplete evidence separately. Historical failures alone remain context and never become repair eligibility or proof that the service is currently unavailable.

The System Files workflow returns one protected-file conclusion after its recipe check. It combines the current SFC result with administrator and restart readiness, system-disk capacity, servicing transitions, reserved storage and bounded CBS/DISM history. A blocked recipe or incomplete required source stays Unknown. Historical log findings alone remain informational and cannot establish current corruption or authorize another repair.

The focused Windows Update workflow returns one local readiness conclusion. It combines all six servicing-chain service states, registered DLLs and exact histories with update engine files, restart and servicing transitions, system-disk capacity, reserved storage, BITS jobs, managed update policy and component repair source evidence. It does not claim external update-source or Internet reachability, search for updates, determine applicability, or authorize another repair.

The Microsoft Defender workflow returns one focused conclusion across selected security hardware, effective AppLocker collections, Defender protection settings, quick and full scan history, threat history, recent events, administrator state and restart state. Scan evidence retains only timestamps, provider age and aggregate in-progress state. Incomplete evidence takes precedence. The conclusion is not a malware scan and does not prove protection, compliance or threat removal.

An explicit Defender scan action is available through the Engine and CLI. The user chooses Quick or Full, reviews eligibility, and must separately request execution. The coordinator records durable state before the command boundary, repeats protection and scan-state checks, and verifies only a newer matching completion timestamp. Recovery reads history without starting another scan. It preserves exclusions, policy, signature sources and remediation settings.

The Backup Services workflow returns one focused conclusion across Windows Backup and File History observations, VSS and volsnap history, current VSS writers, provider registrations, snapshots, shadow storage, exact service histories, administrator state and restart state. Incomplete evidence takes precedence. Historical records alone remain informational, and the conclusion does not prove that a backup is configured, current, restorable or successful.

The Vitals workflow returns one system-stability conclusion across live memory, disk and uptime snapshots, Windows Memory Diagnostic results, current device problems, reliability history, hardware-error records, crash evidence and unexpected shutdowns. It keeps incomplete coverage visible and does not claim that hardware, software, drivers, firmware or future operation are stable.

The Applications workflow combines the bounded Application error log with Reliability Monitor history and returns one stability-history conclusion. It keeps collection gaps explicit and uses retained crash or hang records only to choose a narrower diagnostic area. Historical records do not become a current fault or automatic repair.

The Startup Applications workflow returns one environment conclusion across registration names, Windows approval markers and boot or logon scheduled-task identities. It keeps incomplete sources visible while excluding commands, arguments, accounts and credentials. The result does not measure startup cost or decide that an item is safe to disable.

The Scheduled Tasks workflow returns one operational conclusion across bounded task runtime results, missed-run counts and exact Task Scheduler service failure history. The focused Task Scheduler Service workflow now collects the same task evidence before separately checking current service availability and guarded recovery eligibility. Incomplete inventory or history prevents a combined task conclusion. Task-specific results and service failures request narrower review and cannot authorize a service start. No task is started, enabled, disabled or changed.

The App Packages workflow returns one operational conclusion across current registered-package state, optional provisioning inventory, restart state, AppX deployment history and Microsoft Store installation history. Incomplete sources remain explicit. Historical failures direct narrower transaction review without becoming a current package fault or authorizing a repair.

The Device Problems workflow returns one operational conclusion across current nonzero Plug and Play configuration codes, exact retained SetupAPI driver-install correlations, recent device setup history and recent Plug and Play history. Incomplete sources prevent a combined conclusion. Historical failures direct narrower identity-based review without becoming a current device fault or authorizing a driver change.

The Installed Applications workflow preserves its machine or current-account scope while combining the selected uninstall registrations with bounded Application error history, Reliability Monitor history and restart state. Registration and broad stability history are not treated as an exact identity match. The conclusion directs narrower diagnosis without launching, repairing or removing an application.

The Certificates workflow returns one operational conclusion across bounded personal-certificate validity and cache-only local chain evidence from the machine and current-user personal stores. Incomplete evidence takes precedence. Validity or local-chain concerns request exact certificate review without changing trust, enrollment, bindings or private keys.

The Windows Activation workflow returns one operational conclusion across current Windows licensing and sanitized Software Protection history. Incomplete evidence takes precedence. Current licensing concerns request review, while historical warnings remain informational when the current provider reports Licensed. No activation, product-key, token, license, account, service or policy action is selected.

The Storage Spaces workflow returns one operational conclusion across bounded pool and virtual-disk health evidence. Incomplete evidence takes precedence, reported health or operational-state concerns request exact identity review, and a complete empty inventory remains informational. No pool, disk, volume, allocation, attachment, repair, optimization, resiliency or setting operation is selected.

The Profiles workflow returns separate inventory, exact ProfileList registration and optional service or scheduled-task account dependency results plus one bounded review conclusion. Incomplete evidence takes precedence. Registration or dependency concerns require narrower identity-based review. Age and snapshot checks never authorize removal or prove that data is backed up, restorable or unused.

The Startup Performance workflow returns one conclusion across sanitized startup and shutdown history and supported pending restart indicators. Incomplete evidence takes precedence. A pending restart remains a current review item, while historical degradation directs narrower time-based correlation without becoming a current bottleneck diagnosis or repair authorization.

The Hardware Errors workflow returns one conclusion across WHEA history, Windows Memory Diagnostic history, current Plug and Play device problems and exact physical-disk reliability correlation. Incomplete evidence takes precedence. Current or historical concerns direct exact identity and timestamp review without becoming a component diagnosis or repair authorization.

The Reliability History workflow returns one conclusion across bounded Reliability Monitor records, recent Application errors and recent unexpected shutdown events. Incomplete evidence takes precedence. Completed historical concerns remain informational and direct narrower identity and timestamp review without becoming a current health verdict, root-cause diagnosis or repair authorization.

The Driver Installation History workflow returns one conclusion across retained SetupAPI failed sections and exact current Plug and Play identity matches. Incomplete evidence takes precedence. Only a matched present device with a current nonzero configuration code requests review. Historical failures remain informational and never select a driver action.

The System Restore workflow returns one conclusion across the bounded restore-point inventory and explicit creation eligibility. Incomplete evidence takes precedence. The conclusion never claims protection coverage or future restore success and does not itself select creation. Creating one point remains a separately confirmed repair with fresh eligibility and exact verification.

The Windows Recovery workflow correlates enabled REAgentC configuration with the exact currently reported disk and partition, then returns one bounded readiness conclusion. Missing or incomplete location evidence takes precedence. Enabled status and partition presence do not verify image files, BCD correctness, bootability or repair success. Guarded enablement remains limited to an existing disabled configuration.

Bluetooth, camera, smart card, biometric, image acquisition, human interface, portable device, text input and display enhancement workflows return one additional peripheral readiness conclusion across current Plug and Play configuration, bounded driver inventory, exact service availability and matching service-failure history. Shell Hardware Detection applies the same conservative model to storage-controller, disk and portable-device evidence. Incomplete evidence takes precedence, and current concerns remain attention without widening repair authority. These conclusions do not prove AutoPlay behavior, media readiness, volume access, physical device operation or end-to-end feature behavior.

The Device Association workflow returns one readiness conclusion across bounded Device Setup Manager history, Plug and Play configuration history and the exact service conclusion. Incomplete evidence takes precedence. Historical warnings remain review context when the current service is available, and the conclusion never claims that discovery, pairing, association, driver installation or device operation works.

The focused Function Discovery Provider Host and Resource Publication workflows return feature-specific readiness conclusions across current network profile assignments and the exact service conclusion. A Public profile or current service concern requests review, while incomplete profile or service evidence remains Unknown. These conclusions do not prove provider health, discovery, publication, visibility, firewall effectiveness, share access or connectivity.

The Windows Font Cache workflow returns one readiness conclusion across bounded machine and current-user font registrations and the exact service conclusion. Missing registered files or a current service concern request review, while incomplete evidence remains Unknown. The conclusion does not parse fonts, inspect cache contents, render text, authorize a cache rebuild or prove application behavior.

App Readiness, State Repository, AppX Deployment, Client License, Microsoft Store installation, Windows License Manager, PushToInstall, Web Account Manager, Microsoft Account Sign-in, User Profile, Device Installation and Optimize Drives workflows also return feature-specific conclusions. AppX history provides bounded context for readiness, repository and deployment services. Microsoft Store history provides bounded context for installation, licensing and remote-install services. The account workflows retain only provider, event ID, level, timestamp and record ID from account broker history; payload and identity fields are removed before storage. Event-backed workflows keep retained warnings historical when the exact service is currently available. Optimize Drives treats the exact ScheduledDefrag task as current evidence. Incomplete feature or service evidence takes precedence, and none of these conclusions selects a package, license, request, account, credential, token, profile, device, task, drive or service repair.

The Microsoft app removal workflow provides a review for explicit OneDrive, Outlook, Office/Microsoft 365, Project and Teams selections. Alongside exact registration evidence, bounded safeguards review selected app processes, OneDrive known-folder containment, product-level credential references, fixed application-data locations and fixed machine or current-account policy areas. They exclude process identities, paths, filenames, contents, policy names or values, usernames, attributes and secrets. Exact provider classification supports preserved data only for an unprotected current-account loose-file development package. Ordinary installed Store/MSIX packages remain blocked because Windows limits this preservation option to development-mode packages. The desktop presents supported candidates in one selector and renders the selected plan in the main evidence display. For the supported boundary, Engine coordination requires preserved data and credentials, clear processes and policy, repeated exact eligibility, durable preparation, a final boundary check, one injected provider request and exact absence verification. Interrupted requests are never repeated automatically. Native execution remains disconnected, and the CLI exposes eligibility preview only.

## Full system scan

`FullSystemScan.RunAsync` uses the common diagnostic catalog. Hosts can pass selected diagnostic scopes when they want a smaller run. The result records which checks actually ran and preserves unknown coverage rather than filling gaps with assumptions.

## Live servicing activity

`ServicingActivityMonitor.ObserveAsync` is the Engine boundary for live Windows Update and Setup visibility. It emits bounded snapshots with process activity, recognized update transitions, setup mode, compatibility counts, readiness evidence, and optional histories.

The monitor reports observed state. It does not invent a percentage, declare a stall from elapsed time alone, or authorize a repair.

## Progress and status

Pass `IProgress<string>` to surface the current stage in a desktop status area, console, or log. Progress messages are for live presentation. The final `WorkflowResult` remains the durable record.

## Workflow journals

Every workflow receives a journal folder. The journal records the machine, workflow kind, start time, checks, repair outcomes, recipes, and final status. A saved journal is historical evidence. Reopening it never turns recorded claims into a fresh machine check.
