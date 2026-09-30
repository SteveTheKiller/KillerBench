# Symptom coverage matrix

The finish line for the diagnose-and-repair catalog. Every row is a problem a person would describe, mapped to how KillerBench detects it, what it can change, how it verifies the change, and the current state. A row is finished when its status is `pack`, `workflow` or `out`. Counts of recipes and workflows do not establish coverage; this matrix does.

Status values:

- `pack`: runnable through a recipe pack and a triage tree (`symptom run`), with fixtures.
- `workflow`: covered by an existing bespoke Engine workflow, recipe or explicit option; the triage tree hands off to it by name.
- `diag`: detection exists, no repair is possible or safe to automate; the report says what to do.
- `planned`: not implemented; the row says what would be built.
- `out`: deliberately out of scope, with the reason.

Symptom ids are lower-case dotted identifiers and match `WindowsSymptomCatalog` where a tree exists. Rules that apply to every row: read-only by default, explicit authorization for any change, fresh recheck immediately before dispatch, one dispatch, verification in a stated scope, and limitations printed with the result.

## 1. Startup and boot

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| boot.slow | Delayed-start services, startup apps, disk pressure, pending updates, Fast Startup off | Startup Performance workflow, Startup Applications workflow, Vitals | Disable selected startup entries (explicit option) | Boot time from Diagnostics-Performance events | workflow (detect), planned (repair) |
| boot.pending-restart-loop | Pending file operations, stuck servicing transaction | Restart diagnostics, servicing transitions | None automatic; guidance to complete restart, then DISM | Pending-restart evidence absent | diag |
| boot.winre-missing | WinRE disabled or image missing | Windows Recovery workflow | reagentc /enable (windows.recovery-environment) | reagentc /info Enabled | workflow |
| boot.bcd-damaged | Corrupt BCD, missing bootmgr | Not detectable from a running Windows session beyond BCD read | Out: requires offline recovery media | n/a | out (offline repair) |
| boot.bugcheck | Driver fault, hardware fault, storage failure | Reliability History, Hardware Errors, crash dump configuration and minidump inventory | None automatic; bug-check reference lookup | n/a | diag |
| boot.safe-mode-stuck | bcdedit safeboot flag left set | bcd.safeboot probe (boot.safeboot-clear recipe) | bcdedit /deletevalue {current} safeboot | safeboot value absent | pack |
| boot.fast-startup-corruption | Hybrid boot with stale kernel session | registry.value HiberbootEnabled (power.fast-startup-disable recipe) | Set HiberbootEnabled to 0 (explicit option, prior value journaled, rollback declared) | HiberbootEnabled reads 0 | pack |

## 2. Sign-in and user profiles

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| profile.temporary | ProfileList .bak entry, folder rename, permissions | registry.subkeys ProfileList for .bak (profile.temporary-detect recipe) | Manual rename with guidance; automated rename is a later explicit option | Next sign-in loads the normal profile | pack |
| profile.corrupt-cannot-load | User Profile Service failure, NTUSER.DAT locked or damaged | User Profile Service events, profile inventory | User Profile Service start (profiles.user-profile-service) | Service running; sign-in untested | pack |
| profile.stale-accounts | Old local profiles consuming space | Profiles (PRUNE) workflow | Backup then remove selected profile (explicit, guarded) | Profile absent from registry and disk | workflow |
| signin.hello-pin-broken | NGC container damaged, TPM reset | Windows Hello sign-in options | Out: removing the NGC container signs every account on the PC out of Windows Hello and needs SYSTEM ownership of the folder; guidance points at Sign-in options and, when that fails, the documented manual reset | n/a | out (destructive, per-machine impact) |
| signin.credential-prompts-repeat | Cached credentials stale, Credential Manager service, Office identity | Credential Manager service, WAM and Microsoft account services, Outlook credential review (MACE) | Service starts (security.credential-manager-service, accounts.web-account-manager-service, accounts.microsoft-account-sign-in-service) | Service running | pack |
| signin.domain-trust-broken | Secure channel broken, machine password mismatch | domain.secure-channel probe (identity.secure-channel recipe) | Manual: Test-ComputerSecureChannel -Repair with domain credentials; automated repair needs a credential-carrying explicit option | Secure channel test True | pack |
| signin.group-policy-not-applying | GP client service, secure channel, connectivity to DC, SYSVOL access | Group Policy Client service, secure channel, hours since computer policy applied (identity.group-policy-age) | Service start (core.group-policy-client-service) | Service running; policy age under 24 h | pack |
| signin.no-user-manager | User Manager service stopped | Service check | Service start (core.user-manager-service) | Service running | pack |
| signin.entra-join-broken | Missing primary refresh token, broken device registration, account broker services | entra.status probe (identity.entra-token recipe), WAM and Microsoft account services | Service starts; token refresh and rejoin are manual with guidance | Join YES with token YES | pack |

## 3. Performance

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| perf.high-cpu | Runaway process, indexing, antivirus scan, WMI provider host | process.top-cpu three-second sample (performance.high-cpu recipe) | None automatic; the busiest process is named for the person to judge | n/a | pack |
| perf.high-memory | Memory leak, too many startup apps, low physical RAM | process.top-memory snapshot (performance.high-memory recipe), Vitals | None automatic; the largest process is named | n/a | pack |
| perf.disk-100-percent | Failing disk, SysMain, indexing, Windows Search, antivirus | disk.busy-percent typeperf sample (performance.disk-busy recipe), Storage Service | None automatic; guidance points at disk health first | n/a | pack |
| perf.slow-after-login | Startup apps, delayed services, roaming profile, mapped drives | Startup Applications workflow, Startup Performance | Disable selected startup entries (explicit) | Next-boot timing | workflow (detect), planned (repair) |
| perf.slow-file-explorer | Quick access history, thumbnail cache, network locations offline | Cleanup workflow thumbnail cache, mapped drive reachability | Thumbnail and icon cache cleanup (Cleanup workflow) | Cache files absent | workflow |
| perf.thermal-throttling | Dust, failed fan, power plan | Power plan review, thermal zone read | High performance plan (power.high-performance-plan, explicit) | Active plan matches | workflow |
| perf.low-disk-space | Temp, component store, previous installation, shader cache, profiles | System volume capacity, Cleanup, Component Cleanup, Previous Installation, PRUNE | Cleanup categories, component cleanup, previous installation removal (explicit) | Free space and target absence | workflow |

## 4. Windows Update and servicing

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| update.stuck | Update services stopped, BITS, data store corruption, pending restart | Windows Update workflow; service pack chain | Service starts (pack); data-store reset (update.data-store-reset, explicit) | Service running; fresh search completes | pack + workflow |
| update.error-code | Specific WU HRESULT | Windows Update error catalog lookup (`wu-code`) | Mapped to recipe where one exists | n/a | workflow |
| update.download-never-finishes | BITS job errors, Delivery Optimization paused, proxy | BITS workflow, Delivery Optimization state, proxy diagnostics | BITS job retry (update.bits-job-retry, explicit), DO continue (update.delivery-optimization) | Job state transferring or transferred | workflow |
| update.install-fails-restart-loop | Pending operations, component store corruption | Servicing transitions, DISM ScanHealth, SFC | DISM RestoreHealth, SFC (windows.component-store, windows.system-files) | ScanHealth clean, SFC no violations | workflow |
| update.feature-update-blocked | Policy, compatibility hold, disk space, TPM | Feature update policy diagnostics, applicability, capacity | None automatic; WURSA upgrade path is explicit | n/a | workflow (detect) |
| update.medic-tampered | WaaSMedic service missing or disabled | Focused Medic workflow | Service start under focused authorization | Service running | pack + workflow |
| update.managed-source-unreachable | WSUS/policy source down | Update policy surface, component repair source policy | None automatic | n/a | diag |
| update.driver-update-broke-device | Bad driver via WU | Driver Install History workflow, devices.problem-devices | Out for the rollback itself: Windows exposes driver rollback only through Device Manager; the device restart recipe covers a reload | n/a | workflow (detect), out (rollback needs Device Manager) |

## 5. Storage and file system

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| storage.volume-corruption | Dirty bit, NTFS errors | Storage workflow filesystem check | CHKDSK SpotFix or OfflineScanAndFix (storage.filesystem, explicit) | Fresh read-only check clean | workflow |
| storage.disk-failing | SMART predictive failure, reallocated sectors | Vitals physical disk health, storage history events | Out: hardware replacement | n/a | diag |
| storage.fragmented-or-untrimmed | Optimize Drives never runs | Storage optimization analysis | Optimize or TRIM selected volume (storage.optimization, explicit) | Fresh analysis | workflow |
| storage.spaces-degraded | Storage Spaces pool or virtual disk unhealthy | Storage Spaces workflow | None automatic | n/a | diag |
| storage.storage-service-down | Storage Service stopped | Service check | Service start (storage.service) | Service running | pack |
| storage.optimize-service-down | Optimize Drives service stopped | Service check | Service start (storage.optimize-drives-service) | Service running | pack |
| storage.usb-drive-not-mounting | Missing drive letter, offline disk, unformatted partition | volume.unlettered (storage.unlettered-volumes recipe), Plug and Play and Storage services | Set-Partition -NewDriveLetter (storage.assign-letter, explicit with disk, partition and letter options) | No unlettered data volume remains | pack |
| storage.recycle-bin-corrupt | $Recycle.Bin damaged | Cleanup workflow recycle bin | Empty current shell recycle bin (explicit) | Folder state | workflow |
| storage.storage-sense-policy | Managed Storage Sense settings | Storage Sense policy diagnostics | None automatic | n/a | diag |

## 6. Networking

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| network.no-internet | DHCP/DNS/NLA services, no lease, no gateway, DNS failure, proxy | Network workflow; service pack chain | Service starts (pack); DHCP renew (network.dhcp-lease); DNS cache flush (network.dns-cache) | Address, gateway reply, hostname resolves | pack + workflow |
| network.limited-apipa | DHCP server unreachable, adapter | IP address diagnostics APIPA detection | DHCP renew (network.dhcp-lease) | Non-APIPA address | workflow |
| network.dns-fails | DNS client service, cache poison, NRPT, hosts file, bad server | DNS client, hosts file, NRPT, DNS server diagnostics | DNS cache flush | Selected hostname resolves | workflow |
| network.wifi-missing | WLAN service, radio off, driver | WLAN AutoConfig check | Service start (network.wireless) | Service running | pack |
| network.wifi-drops | Power management on adapter, driver, roaming | netadapter.power-saving for the Wi-Fi adapter (network.wifi-power-saving recipe), WLAN service | Disable adapter power saving (explicit option) | AllowComputerToTurnOffDevice Disabled | pack |
| network.wired-auth | Wired AutoConfig, 802.1X profile, certificate | Wired AutoConfig check, events | Service start (network.wired-authentication) | Service running | pack |
| network.shares-unreachable | Workstation service, DNS, SMB1 expectation, credentials, firewall | SMB client configuration and connections, Workstation service | Service start (network.workstation) | Service running | pack |
| network.file-sharing-inbound-fails | Server service, firewall rule, discovery services | File Sharing workflow, SMB server configuration, Function Discovery services | Service starts (network.smb-server, network.function-discovery-*) | Service running | pack |
| network.vpn-fails | Remote Access Connection Manager, profile, split tunnel | VPN profile diagnostics, remote access service | Service start (network.remote-access-service) | Service running | pack |
| network.proxy-wrong | WinHTTP proxy, user proxy, PAC, WinHTTP AutoProxy service | Proxy diagnostics; winhttp.proxy (network.winhttp-proxy-reset recipe) | Service start (network.winhttp-autoproxy-service); netsh winhttp reset proxy (explicit); browser and per-user proxy stay manual | Service running; WinHTTP reads back Direct | pack |
| network.winsock-corrupt | LSP catalog damage | Winsock catalog and provider file diagnostics | None automatic: the catalog inventory has no reliable local signal that separates corruption from an unusual but valid LSP set, so a reset would be a guess; netsh winsock reset stays a manual step after the Network workflow evidence is reviewed | n/a | diag |
| network.adapter-missing | Disabled adapter, driver problem code | netadapter.disabled-physical (network.disabled-adapters recipe), Device Problems workflow | Enable-NetAdapter for the named adapter (network.adapter-enable, explicit option with adapter=NAME) | Adapter admin status Up | pack |
| network.ipv6-broken | IP Helper stopped, transition tech | IP Helper check, interface config | Service start (network.ip-helper) | Service running | pack |
| network.discovery-off | Network profile Public, discovery services | Network profile diagnostics, discovery services | Service starts (pack); profile change is explicit | Service running | pack |
| network.port-exhaustion | Dynamic port range consumed | TCP port pressure diagnostics | None automatic | n/a | diag |
| network.gateway-unreachable | Cable, switch, gateway down | Gateway reachability probe | None automatic | n/a | diag |
| network.ncsi-false-no-internet | NCSI probe blocked by policy or proxy | NCSI policy diagnostics | None automatic | n/a | diag |
| network.mtu-black-hole | MTU below 1280 on IPv6, VPN | netip.mtu for the named interface (network.mtu-set recipe) | Set-NetIPInterface -NlMtuBytes (explicit with interface, family and mtu options) | Interface reports the requested MTU | pack |

## 7. Firewall and security

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| network.firewall-off | BFE or firewall service stopped, profile disabled | Firewall services and profiles | Service starts (pack); enable profiles (network.firewall-profiles, explicit) | Service running, profile enabled | pack + workflow |
| security.defender-off | Signatures stale, protection disabled by policy, third-party AV | Defender workflow, Security Center registrations | Signature refresh (security.defender-signatures) | Signature age | workflow |
| security.center-blank | Security Center service stopped | Service check | Service start (security.center-service) | Service running | pack |
| security.bitlocker-suspended | Protection suspended after update | BitLocker workflow | Resume protection (security.bitlocker-resume, explicit) | Protection on | workflow |
| security.bitlocker-key-not-backed-up | No escrow | BitLocker workflow protector inventory | Back up recovery protector to file, or to Active Directory or Entra ID (explicit) | File exists and verified; directory request accepted | workflow |
| security.tpm-not-ready | TPM not provisioned, turned off in firmware, needs clearing | tpm.status (security.tpm-initialize recipe) | Initialize-Tpm without clearing (explicit); clearing and firmware prompts stay manual | Get-Tpm reports ready | pack |
| security.fips-required | Compliance needs the legacy FIPS policy for BitLocker PIN protectors | registry.value FIPSAlgorithmPolicy Enabled (security.fips-policy-enable recipe), MDMEnabled precondition | Set Enabled to 1 (explicit, reversible) | Enabled reads 1 | pack |
| security.uac-misconfigured | UAC policy changed | UAC policy diagnostics | None automatic | n/a | diag |
| security.certificate-errors | Expired or untrusted certificates, wrong clock | Certificates workflow, time | Time sync (time.synchronization) | Sync reported | workflow |
| security.applocker-blocks-app | AppLocker policy | AppLocker policy diagnostics | Out: policy decision | n/a | out (policy) |
| security.smart-card-not-read | Smart Card service | Service check | Service start (devices.smart-card-service) | Service running | pack |
| security.biometric-missing | Biometric service | Service check | Service start (devices.biometric-service) | Service running | pack |

## 8. Printing

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| printing.nothing-prints | Spooler stopped, paused queue, stuck job, offline | Printing workflow; spooler chain | Service start (pack); resume paused queues, cancel stuck job, offline setting (explicit) | Queue state | pack + workflow |
| printing.printer-offline | Use Printer Offline set, port IP mismatch | Printer offline settings | Clear offline setting (printing.offline-setting) | Setting cleared | pack + workflow |
| printing.stuck-job | Job in Error or Blocked | Job inventory | Cancel selected job (printing.stuck-job, explicit) | Job absent | workflow |
| printing.wrong-default | Multiple or no default | Default printer diagnostics | Set selected default (printing.default-printer, explicit) | One default | workflow |
| printing.spool-folder-missing | Spool directory absent | Spool directory diagnostics | Recreate default directory (printing.spool-directory) | Directory present | workflow |
| printing.driver-missing | Queue points at unregistered driver | Printer drivers diagnostic | Out: driver install needs vendor package | n/a | diag |
| printing.print-nightmare-policy | RestrictDriverInstallationToAdministrators, point-and-print | Print policy and rendering policy diagnostics | None automatic | n/a | diag |
| printing.spooler-crashes | Bad driver, corrupt spool files | file.count of the spool folder (printing.spool-clear recipe), PrintService events | Stop spooler, retain spool files, start spooler (spooler.clear-spool, explicit) | Spooler running afterwards | pack |

## 9. Audio

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| audio.no-sound | Audio services, wrong endpoint, muted, driver | Audio workflow; audio chain | Service starts (pack) | Service running | pack |
| audio.wrong-device | Default endpoint changed | Default audio endpoints | Out: Windows has no supported API for setting the default endpoint (IPolicyConfig is undocumented) and decision 0005 rules out a dependency; guidance points at Sound settings | n/a | out (no supported API) |
| audio.crackling | Driver, enhancements, sample rate | Audio device inventory | Out: audio enhancement and format settings live in per-device property stores with vendor-specific keys; guidance points at the device properties | n/a | out (vendor-specific settings) |
| audio.microphone-blocked | Privacy setting denies microphone | Privacy (SHADE) microphone review | Allow microphone (explicit) | Registry value | workflow |

## 10. Display, graphics and input

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| display.black-screen-after-login | Explorer not starting, display driver TDR | process.running explorer (shell.explorer-start recipe) | Start explorer.exe when absent; never terminates a running shell | Explorer process present | pack |
| display.tdr-resets | GPU driver timeouts | Display events | None automatic | n/a | diag |
| display.wrong-resolution-scaling | Driver missing, scaling override | Display device inventory | None automatic | n/a | diag |
| display.fonts-broken | Font cache service or corrupt cache | Font Cache service, font registration | Service start (display.font-cache-service); rebuild cache (explicit) | Service running | pack |
| display.enhancement-service-down | Display Enhancement service stopped | Service check | Service start (display.enhancement-service) | Service running | pack |
| input.keyboard-mouse-dead | HID service, driver, USB | HID service, device problems | Service start (devices.human-interface-service) | Service running | pack |
| input.touch-keyboard-missing | Text Input Management service | Service check | Service start (input.text-management-service) | Service running | pack |
| input.wrong-layout | Layout preference | Input workflow | Out: user preference | n/a | out (preference) |

## 11. Devices, USB and Bluetooth

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| devices.usb-not-detected | Plug and Play, Device Install, driver, hub power | Device problems, service chain | Service starts (pack) | Service running | pack |
| devices.problem-code | Driver missing, code 10/28/43 | pnp.problem-devices (devices.problem-devices recipe), Device Problems workflow | pnputil /restart-device (devices.restart-device, explicit with instance option); a driver package is the vendor's | Device reports OK | pack |
| bluetooth.missing | Bluetooth Support service, radio, driver | Bluetooth workflow | Service start (devices.bluetooth-support) | Service running | pack |
| devices.camera-not-working | Frame Server service, privacy setting | Camera workflow, privacy review | Service start (devices.camera-frame-server); allow camera (explicit) | Service running | pack + workflow |
| devices.scanner-not-found | Image Acquisition service | Service check | Service start (devices.image-acquisition-service) | Service running | pack |
| devices.phone-not-detected | Portable Device Enumerator service | Service check | Service start (devices.portable-device-enumerator) | Service running | pack |
| devices.autoplay-broken | Shell Hardware Detection service | Service check | Service start (devices.shell-hardware-detection-service) | Service running | pack |
| devices.pairing-service-down | Device Association service | Service check | Service start (devices.association-service) | Service running | pack |

## 12. Power and battery

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| power.sleeps-too-soon | Timers, policy | Power timer review | Change sleep and display timers (power.sleep-display-timers, explicit) | Values match | workflow |
| power.hibernation-file-space | hiberfil.sys large | Hibernation state | Disable hibernation (power.hibernation-disable, explicit) | File absent | workflow |
| power.wrong-plan | Balanced when performance expected | Active plan | High performance plan (explicit) | Active plan | workflow |
| power.service-down | Power service stopped | Service check | Service start (core.power-service) | Service running | pack |
| power.battery-drains | Wake timers, background apps | powercfg.wake-timers (power.wake-timers recipe) | None automatic; owners are named for the person to disable | n/a | pack |

## 13. Applications

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| store.apps-fail | AppX deployment, App Readiness, State Repository, licensing services | App Packages workflow; service chain | Service starts (pack) | Service running | pack |
| store.package-corrupt | Package registration damaged | appx.status for the named package (apps.package-reregister recipe), app platform services | Add-AppxPackage -Register from the install location (explicit with package option) | Package reports Ok | pack |
| apps.crash-on-launch | Faulting module, missing runtime | Application history, WER | None automatic; crash reference | n/a | diag |
| installer.msi-fails | Windows Installer service, registration, policy | Installer workflow | Service start (pack); /regserver (installer.registration, explicit); exact product repair (installer.msi-product, explicit) | Service running, product state | pack + workflow |
| apps.outlook-slow-or-stuck | Cache file bloat, profile | Outlook cache review (ORCA) | Retain and reset selected cache (applications.outlook-cache-reset, explicit) | Cache absent, retained copy exists | workflow |
| apps.office-click-to-run-broken | ClickToRun service or install | Office Click-to-Run inventory | None automatic | n/a | diag |
| apps.foxit-outdated | Old version, updater setting | Foxit inventory (FACTS) | None automatic | n/a | diag |
| apps.foxit-auto-updates | Per-user UpdateMode 1 or 2, absent setting, machine updater policy | registry.value HKCU Foxit Updater UpdateMode (apps.foxit-auto-update-off recipe), PDF Editor updater policy precondition | Set UpdateMode to string 0 for the running account (explicit with product) | UpdateMode reads back 0 | pack |
| apps.browser-extension-unwanted | Sideloaded or policy extension | Browser Extensions workflow | None automatic | n/a | diag |
| apps.file-association-wrong | UserChoice hash reset, app default lost | registry.value .pdf UserChoice ProgId (apps.pdf-association recipe) | Manual: Windows signs the user choice | n/a | pack |
| apps.default-apps-reset | Same | registry.value http UserChoice ProgId (apps.http-association recipe) | Manual: Windows signs the user choice | n/a | pack |
| apps.mace-removal | Unwanted preinstalled packages | MACE workflow | Remove selected package for current account (explicit) | Package absent | workflow |

## 14. Time and licensing

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| time.wrong-clock | Time service, source, policy, CMOS battery, time zone | Time workflow | Resync (time.synchronization); source change (time.source-configuration, explicit) | Windows reports synchronized | workflow |
| time.service-stopped | W32Time stopped | Time workflow focused check | Service start under focused authorization | Service running | workflow |
| license.not-activated | Product key, KMS reachability, hardware change | Windows Activation workflow, licensing services | Service starts (applications.license-manager-service, applications.client-license-service) | Service running | pack + workflow |

## 15. Remote access

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| remote.rdp-fails | Remote Desktop Services, firewall, listener, NLA, allowed users | Remote Desktop workflow; service chain | Service starts (pack) | Service running | pack |
| remote.winrm-fails | WinRM service, listener, firewall | WinRM workflow | Service start (remote.winrm-service) | Service running | pack |
| remote.rdp-listener-missing | Listener configuration | Remote Desktop listener state | Out: registry rebuild of listener is not automated | n/a | diag |

## 16. Backup and recovery

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| backup.fails | VSS, provider, backup services, writers | Backup Services workflow; service chain | Service starts (pack) | Service running | pack |
| backup.vss-writer-failed | Writer in failed state, VSS services | vss.unstable-writers (backup.vss-writers recipe), VSS and provider services | Service starts; owning-service restart is manual with guidance | Writers stable | pack |
| backup.restore-point-missing | System Restore disabled, frequency policy | System Restore workflow | Create restore point (windows.restore-point, explicit) | New point in inventory | workflow |
| backup.shadow-storage-full | Shadow storage limit | vss.shadowstorage-percent (backup.shadow-storage recipe) | Manual resize with guidance | Use under 90% of maximum | pack |

## 17. Diagnostics infrastructure and core services

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| eventlog.stopped | Event Log service stopped | Service check | Service start (eventlog.service) | Service running | pack |
| eventlog.channel-disabled | Diagnostic channel disabled | Channel state | Enable selected channel (eventlog.channel-enable, explicit) | Channel enabled | workflow |
| eventlog.collector-down | Windows Event Collector service | Service check | Service start (events.collector-service) | Service running | pack |
| wmi.broken | WMI service, repository inconsistent, provider | WMI workflow | Service start (wmi.service); repository salvage (wmi.repository) | Consistent verdict | pack + workflow |
| wer.not-reporting | WER service or policy | WER workflow | Service start (diagnostics.error-reporting-service) | Service running | pack |
| diagnostics.policy-service-down | Diagnostic Policy service | Service check | Service start (diagnostics.policy-service) | Service running | pack |
| tasks.not-running | Task Scheduler service, task disabled, missed runs | Scheduled Tasks workflow | Service start (tasks.scheduler-service); task registration (explicit, guarded) | Service running, task definition | pack + workflow |
| core.rpc-failures | RPC, endpoint mapper, DCOM launcher stopped | Core services workflow | Service starts (core.remote-procedure-call, core.rpc-endpoint-mapper, core.dcom-launcher) | Service running | pack |
| core.background-broker-down | Broker infrastructure, CoreMessaging | Service check | Service starts (core.background-tasks-infrastructure, core.messaging) | Service running | pack |
| core.network-store-down | NSI service | Service check | Service start (core.network-store-interface-service) | Service running | pack |
| notifications.broken | COM+ Event System, System Event Notification | Notifications workflow | Service starts (notifications.com-event-system, notifications.system-event-notification) | Service running | pack |

## 18. Search and shell

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| search.no-results | Search service stopped, index corrupt | Search workflow | Service start (search.service); index rebuild (search.index-rebuild, explicit) | Service running, index directory | pack + workflow |
| shell.explorer-crashes | Shell extension, corrupt icon cache | Application history, shell events | Icon cache cleanup (Cleanup workflow) | Cache rebuilt | workflow (partial) |
| shell.start-menu-broken | StateRepository, AppX, ShellExperienceHost | State Repository service, App Packages | Service start (applications.state-repository-service) | Service running | pack |
| shell.taskbar-unresponsive | Same plus Explorer hang | process.running explorer (shell.explorer-restart recipe) | Restart Explorer (explicit option; closes File Explorer windows) | Explorer process running after restart | pack |
| shell.quick-access-broken | Automatic destinations corrupt | file.count of AutomaticDestinations (shell.quick-access-reset recipe) | Retain the jump-list files to the KillerBench retained folder (explicit) | Folder empty until Explorer rebuilds it | pack |

## 19. Hardware errors and stability

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| hardware.whea-errors | CPU, memory, PCIe faults | Hardware Errors workflow | Out: hardware | n/a | diag |
| hardware.memory-suspect | ECC or random crashes | bcd.bootsequence probe (hardware.memory-diagnostic recipe) | Schedule Windows Memory Diagnostic (explicit option, restart) | Boot sequence names {memdiag} | pack |
| hardware.unexpected-shutdowns | Power loss, thermal, bug check | Shutdown history | None automatic | n/a | diag |
| hardware.driver-install-failed | SetupAPI failures | Driver Install History workflow | None automatic | n/a | diag |

## 20. Privacy and debloat

| Symptom | Likely causes in order | Detect | Repair | Verify | Status |
| --- | --- | --- | --- | --- | --- |
| privacy.telemetry-settings | Diagnostic data level, ads, clipboard history | Privacy (SHADE) workflow | Set selected boolean settings, clipboard history (explicit) | Registry values | workflow |
| debloat.unwanted-apps | Preinstalled packages | Debloat workflow | Remove selected packages (explicit) | Package absent | workflow |
| debloat.power-tweaks | Timers and plans | Debloat power diagnostics | Explicit timers | Values | workflow |

## 21. Cross-platform placeholder rows

The Engine.Core model is platform-neutral. When a Linux or macOS provider exists, these rows get their own tables; until then they are `out` with the reason "no provider".

| Symptom | Status |
| --- | --- |
| linux.service-failed | out (no provider) |
| linux.disk-full | out (no provider) |
| linux.dns-fails | out (no provider) |
| macos.launchd-service-failed | out (no provider) |
| macos.disk-full | out (no provider) |

## Counts

Update these when rows change. A row counts once by its first status token.

- rows: 153
- pack: 81
- workflow: 38
- diag: 23
- planned: 0
- out: 11

The loop is finished when `planned` is zero and every remaining `diag` row states why no safe automatic repair exists.
