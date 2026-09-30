# Command line

`killerbench-cli` runs the same Engine as the desktop app from PowerShell or a remote console. Every command that could change the machine previews by default and needs an explicit flag such as `--execute` before it acts. Commands print JSON evidence to standard output and progress to standard error.

Exit code 0 means the preview or verified result succeeded, 3 means blocked, failed or unverified, 2 means the arguments were not recognized, and 130 means you stopped it.

Run `killerbench-cli` with no arguments to print this list on your own machine.

## Updates and servicing

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Exact transfer review or two-snapshot observation; no retry.
- `bits-jobs [--all-users]`: BITS job states, owners and byte counts; no transfer changes.
- `bits-retry JOB_GUID [--execute]`: Preview by default; explicit exact-job retry, no application-completion claim.
- `bits-retry-recover JOB_GUID`: Two read-only snapshots; archives stable outcomes without another retry.
- `component-repair-source`: Managed DISM repair-source policy and conflicts; no changes.
- `delivery-optimization`: Read-only Delivery Optimization service state; JSON evidence.
- `delivery-optimization-activity`: Privacy-bounded current job counts and byte totals; no transfer identities or changes.
- `delivery-optimization-policy`: Read-only classic and Policy CSP settings; sensitive values and provider identities excluded.
- `feature-service-events`: Read exact Time, Search, WMI, Event Log and audio service failure history.
- `feature-update-policy`: Read-only target release, deferral, pause and safeguard policy; JSON evidence.
- `pending-file-operations`: Read-only privacy-bounded pending file operation summary; JSON evidence.
- `restart-check`: Read-only pending restart sources; JSON evidence.
- `servicing-logs`: Bounded newest CBS, plain-text and compressed CBS archives, DISM and Windows Setup log analysis; no repair.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Read-only Servicing activity snapshots.
- `servicing-service-events`: Read-only exact Windows Update servicing-chain failure histories.
- `servicing-service-states`: Read-only current state for the six-service Windows Update servicing chain.
- `setup-code DECIMAL_OR_0xHEX`: Documented Windows Setup result-code reference; no machine inspection or repair.
- `update-applicability`: Read-only Windows edition, build and architecture evidence.
- `update-cache`: Bounded read-only SoftwareDistribution cache totals; no filenames.
- `update-code DECIMAL_OR_0xHEX`: Documented Windows Update result-code reference; no machine inspection or repair.
- `update-context`: Read-only current elevation and desktop-session evidence; no account identifiers.
- `update-engine-files`: Read-only native Windows Update engine presence, version and signature evidence.
- `update-events`: Read-only Windows Update Client history from the last seven days; JSON including raw evidence.
- `update-followup DECIMAL_OR_0xHEX`: Fresh read-only diagnostics selected by a documented Windows Update result code.
- `update-history`: Read-only newest 100 Windows Update operation outcomes; JSON evidence.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: Repeat --select; preview by default.
- `update-policy`: Read-only managed Windows Update policy; JSON evidence.
- `update-policy-surfaces`: Read-only classic and Policy CSP configuration-surface evidence; values and provider identities excluded.
- `update-recover`: Check a pending update; never installs or retries it.
- `update-restart-policy`: Read-only compliance deadlines, grace periods, restart suppression and notification behavior.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: JSON discovery and matches; no installation.
- `update-service-events`: Read-only exact Windows Update service failure history.
- `update-service-files`: Read-only exact Windows Update service DLL registration and signature evidence.
- `update-services`: Read-only Windows Update service registrations; JSON evidence.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Fresh-search gated; preview by default; retains the old store.
- `update-store-reset-recover`: Reconcile a prepared store reset without repeating it.
- `windows-capabilities`: Read-only online Windows capability states; JSON result.
- `windows-update-task`: Read-only exact Scheduled Start task state and runtime history; no task execution or update operation.

## BitLocker and TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Preview by default; turns on auto-unlock for an encrypted data volume.
- `bitlocker-auto-unlock-recover`: Verify an interrupted auto-unlock request without repeating it.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Preview by default; new recovery backup file only.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Verify an interrupted backup without writing it again.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Preview by default; starts decryption, for example before re-encrypting with a required method.
- `bitlocker-decrypt-recover`: Verify an interrupted decryption request without repeating it.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Preview by default; requires existing TPM and recovery protectors.
- `bitlocker-encrypt-recover`: Verify pending conversion state without repeating the request.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Preview by default; backs up an existing recovery password to Active Directory or Microsoft Entra ID.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Options preview only.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Preview by default; adds one TPM or recovery-password protector; the password is never printed.
- `bitlocker-protector-recover`: Verify an interrupted protector request without repeating it.
- `bitlocker-protectors DRIVE:`: Protector identities and types only; no recovery passwords.
- `bitlocker-resume DRIVE: [--execute]`: Preview by default; exact fully encrypted suspended volume only.
- `bitlocker-resume-recover`: Verify pending protection state without repeating the request.

## Security and privacy

- `certificate-chains`: Read-only local cache-only chain trust for personal certificates; no revocation or downloads.
- `defender-overview [--report]`: Read-only protection, scan timing and aggregate threat overview; no action.
- `defender-protection [--report]`: Read-only protection preferences and recognized policy surfaces; sensitive details excluded; no changes.
- `defender-scan Quick|Full [--execute] [--report]`: Preview by default; one explicit guarded Microsoft Defender scan.
- `defender-scan-history [--report]`: Read-only quick and full scan timing and overdue status; sensitive details excluded; no scan.
- `defender-scan-recover [--report]`: Read-only reconciliation without starting another scan.
- `defender-threats [--report]`: Aggregate threat history, current active flags and severity IDs; sensitive details excluded; no action.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Preview by default; unmanaged local profiles only.
- `firewall-enable-recover`: Inspect a pending change without repeating it.
- `firewall-events`: Read bounded Windows Firewall warning, error and critical operational history.
- `firewall-logging`: Read Domain, Private and Public traffic logging configuration without opening logs.
- `firewall-rules`: Read privacy-bounded effective rule and policy-source counts.
- `firewall-service-events`: Read exact BFE and Windows Defender Firewall Service Control Manager failure history.
- `privacy-clipboard-history on|off [--execute]`: Preview by default; exact current-user value with policy preservation.
- `privacy-clipboard-history-recover`: Recheck an interrupted change without repeating it.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: Guarded restoration of the prior value.
- `privacy-plan --select Camera --select ClipboardSync`: Review selections only; no settings changed.
- `privacy-setting KEY on|off [--execute]`: Preview by default; policy-preserving current-user change; invalid keys list supported choices.
- `privacy-setting-recover KEY`: Recheck an interrupted change without repeating it.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: Guarded exact-value restoration.
- `security-check`: Read-only TPM, Secure Boot, virtualization protections and UAC policy; JSON evidence.

## Services

- `service-binaries`: Read-only local service binary presence and Authenticode status; paths excluded and nonlocal paths not contacted.
- `service-check NAME [NAME ...] | service-check --all`: Read-only supported service states and dependency evidence.
- `service-dependencies`: Read-only direct dependency integrity review; no service changes.
- `service-dlls`: Read-only registered service DLL presence and Authenticode status; paths excluded and nonlocal paths not contacted.
- `service-events`: Read-only service failure history; JSON including raw evidence.
- `service-events | storage-events | time-events`: Read-only recent event history with JSON evidence.
- `service-logons`: Read-only service account-class and desktop-interaction review; account identities excluded.
- `service-path-quoting`: Read-only service executable path quoting review; full paths and arguments excluded.
- `service-recovery-policy`: Read-only installed-service recovery action summary; commands and reboot messages excluded.
- `service-startup-config`: Read-only startup-mode coherence review; no service changes.
- `service-transitions`: Read-only repeated observation of pending service states; no service action.
- `service-triggers`: Read-only installed-service trigger summary; trigger payloads excluded; no changes.

## Storage, files and backup

- `backup-events`: VSS and volsnap history; JSON evidence; no changes.
- `backup-service-events`: Read exact VSS, software shadow provider, Windows Backup and block-level engine failure history.
- `backup-summary`: Local Windows Backup operation summary; no backup changes.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Preview by default; Recycle Bin is all-or-nothing and age does not apply.
- `disk-cleanup-task`: Read-only exact task state and runtime history; no task execution or cleanup.
- `disk-diagnostic`: Correlate exact disk diagnostic tasks and event history; no task or disk action.
- `disk-diagnostic-tasks`: Read-only exact collector and resolver task history; no task or disk action.
- `disk-quotas`: Read-only per-volume quota defaults and state; user identities excluded; no changes.
- `file-history-events`: Privacy-projected File History warning and error counts; no changes.
- `file-history-status`: Current-account File History protection state; no changes.
- `search-index-rebuild [--execute]`: Preview by default; retains the prior index and verifies fresh state.
- `search-index-rebuild-recover`: Read-only reconciliation without repeating repair commands.
- `storage-check DRIVE_LETTER`: Read-only fixed NTFS check; JSON results and live progress.
- `storage-events`: Read-only storage history; JSON including raw evidence.
- `storage-jobs`: Read-only aggregate Storage module job state and progress; JSON; no changes.
- `storage-reliability`: Read-only temperature, wear and I/O counters; JSON; no repair.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Preview by default; execution requires backup confirmation; system volumes schedule at restart.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Preview only; no repair or scheduling.
- `storage-repair-recover`: Verify a completed or scheduled filesystem repair without repeating it.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Collect and bind current evidence; no repair or scheduling.
- `storage-sense-policy`: Read-only recognized machine cleanup policy; user settings and file identities excluded.
- `storage-sense-task`: Read-only exact task state and runtime history; no task execution or cleanup.
- `storage-service-events`: Read exact Storage Service, Optimize Drives, VSS and software shadow provider failure history.
- `volume-capacity DRIVE:`: Read-only available space with JSON evidence.
- `volume-dirty DRIVE:`: Read-only dirty-bit status; no repair or restart.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Read-only analysis by default; exact healthy volume and compatible media required.
- `volume-optimize-recover`: Verify a pending completed operation without repeating it.
- `vss-providers`: Read-only VSS provider names, versions and types; identifiers excluded; no registration changes.
- `vss-writers`: Reported writer states; English output interpretation; no reset.

## Printing

- `print-events`: Read-only PrintService Admin history from the last seven days; JSON evidence.
- `print-extension-files`: Read-only print processor and monitor binary presence and signature summary; full paths excluded.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Preview by default; exact Error or Blocked job only.
- `print-job-cancel-recover [--folder PATH]`: Recheck an interrupted cancellation without repeating it.
- `print-job-list`: List current visible jobs without changing them.
- `print-operational-events`: Read-only PrintService Operational channel state and available history; JSON evidence.
- `print-policy`: Read-only Point and Print restrictions and driver-installation policy; JSON evidence.
- `print-rendering-policy`: Read-only rendering and driver-isolation policy; JSON evidence.
- `print-spooler-events`: Read-only Service Control Manager failures tied to the local Print Spooler identity; JSON evidence.
- `printer-check --queue NAME [--queue NAME ...]`: Read-only selected local queue settings.
- `printer-conditions`: Read-only local queue condition codes; no physical verification or changes.
- `printer-driver-files`: Read-only registered file presence and executable signature summary; full paths excluded.
- `printer-driver-models`: Read-only version 3, version 4 and package-awareness summary; JSON evidence.
- `printer-drivers`: Read-only queue/driver registration inventory; JSON evidence.
- `printer-drivers`: Read-only queue/driver registration matching; JSON evidence.
- `printer-endpoint-check`: Bounded connections to exact assigned Standard TCP/IP printer endpoints; no discovery, SNMP or print data.
- `printer-list`: Discover local queues without changing settings.
- `printer-ports`: Read-only queue/port registration matching; JSON evidence.
- `printer-queue-attributes`: Read-only local queue spooling, retention and sharing attributes.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Preview by default; clearing offline mode can release waiting jobs.

## Network

- `dhcp-check`: Read-only missing or expired DHCP lease eligibility; JSON evidence, no renewal.
- `dhcp-events`: Read-only IPv4 and IPv6 DHCP Client event history; JSON result.
- `dhcp-leases`: Read-only lease timestamp, expiry and server evidence; JSON result.
- `dhcp-renew [--execute]`: Preview by default; exact missing or expired connected physical DHCP leases only.
- `dhcp-renew-recover`: Read-only reconciliation without requesting another lease.
- `dns-cache HOST [--execute]`: Preview by default; guarded local cache flush with exact-host verification.
- `dns-cache-recover`: Recheck an interrupted cache operation without repeating the flush.
- `dns-cache-summary`: Privacy-bounded local DNS cache status and record-type counts; no changes.
- `dns-check HOST [--timeout-seconds 1..60]`: Bounded system name resolution; JSON result.
- `dns-client-config`: Read-only suffix, registration, devolution and policy evidence; JSON result.
- `dns-client-events`: Read-only privacy-bounded DNS Client warning and failure history; JSON result.
- `dns-nrpt-policy`: Read-only effective DNS namespace policy evidence; JSON result.
- `ip-addresses`: Read-only current IP address state, origin and conflict review; JSON evidence.
- `ip-interface-config`: Read-only IP interface configuration evidence; JSON result.
- `network-firewall`: Correlate active network assignments with matching firewall profile enablement.
- `network-offload`: Read-only global Windows network offload configuration; JSON result.
- `network-profiles`: Read-only current profile categories and connectivity classifications; network names excluded.
- `network-service-events`: Read exact DHCP, DNS, Workstation, NLA, Network List and IP Helper failure history.
- `proxy-check`: Read-only stored WinHTTP and current-account Internet proxy evidence.
- `route-priority`: Read-only effective default-route metric comparison; JSON evidence.
- `smb-client-config`: Read-only SMB signing, guest-access and transport configuration.
- `smb-connections`: Read-only active protocol negotiation; remote identities excluded.
- `smb-server-config`: Read-only SMB protocol, signing, encryption and multichannel configuration.
- `smb-shares`: Read-only bounded SMB share inventory; paths, permissions, sessions and accounts excluded.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Bounded connection check; no application data sent.
- `tcp-port-pressure`: Read-only dynamic TCP ranges and aggregate local-port usage; endpoints and processes excluded.
- `tcp-settings`: Read-only Windows TCP template configuration evidence; JSON result.
- `vpn-profiles`: Read-only current-account VPN profile configuration; credentials excluded.
- `winrm-listeners`: Read-only WinRM listener registration summary; certificate thumbprints excluded.
- `winsock-catalog`: Read-only native Winsock protocol catalog structure; JSON result.
- `winsock-namespaces`: Read-only native Winsock namespace-provider structure; JSON result.
- `winsock-provider-files`: Read-only protocol provider file presence and signatures; JSON result.
- `winsock-readiness`: Read-only native Winsock protocol, namespace-provider and provider-file readiness; JSON results.

## Apps

- `app-folder-review FOLDER_NAME [--report]`: Fixed-root exact folder review; no deletion.
- `app-package-review FULL_NAME [--all-users]`: Exact target and dependent packages; no removal.
- `app-packages [--all-users]`: Registered Appx/MSIX inventory only; no removal.
- `browser-extensions`: Chrome, Edge, Brave and Firefox extension identity review; no removal.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Review only; no system changes.
- `depot-review [application and configuration selections] [--json]`: Review only; no provisioning.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Read-only Authenticode and vendor-signer check; nothing is run.
- `foxit-inventory`: Read-only machine product inventory; JSON output.
- `installed-apps [--current-user]`: Uninstall registrations and capability flags; no removal.
- `installed-apps-compare BEFORE.json AFTER.json`: Saved inventory registration differences; no machine changes.
- `installed-hotfixes`: Read-only bounded installed Windows hotfix inventory; JSON evidence.
- `installer-activity`: Read-only Installer process and transaction-marker evidence; JSON evidence.
- `installer-check`: Read-only Installer policy and engine readiness; JSON evidence.
- `installer-engine-files`: Read-only required Installer file presence and Authenticode summary; JSON evidence.
- `installer-events`: Read-only MSI history from the last seven days; JSON evidence.
- `installer-logging`: Read-only Installer logging and debug policy; JSON evidence.
- `installer-pending-state`: Read-only pending file operation and Installer activity correlation; JSON evidence.
- `installer-registration-repair [--execute]`: Preview by default; exact service-registration defect only; never unregisters the engine.
- `installer-registration-repair-recover`: Recheck interrupted registration without repeating commands.
- `installer-security-policy`: Read-only Installer security and patch policy; JSON evidence.
- `installer-service-events`: Read-only Service Control Manager failures tied to the local Windows Installer identity; JSON evidence.
- `installer-service-registration`: Read-only Windows Installer service registration summary; JSON evidence.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: Fresh exact eligibility preview; no removal provider.
- `mace-package-recover`: Freshly reconcile one interrupted app package-removal record; never repeats removal.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Review only; no removal.
- `msi-cache`: Read-only registered Windows Installer cache visibility; JSON evidence.
- `msi-code NONNEGATIVE_DECIMAL`: Documented Windows Installer return-code reference; no machine inspection or repair.
- `msi-product {PRODUCT-CODE}`: Read-only exact MSI registration state; JSON evidence.
- `msi-repair {PRODUCT-CODE} [--execute]`: Preview by default; exact broken or bad-configuration product only.
- `msi-repair-recover`: Recheck an interrupted exact-product repair without repeating it.
- `msi-sources`: Read-only privacy-bounded registered Windows Installer source metadata; JSON evidence.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Preview by default; retains exact running-account caches under recovery names.
- `orca-cache-reset-recover`: Verify interrupted cache retention without repeating a move.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Review only; no Outlook changes.
- `startup-apps`: Startup registrations and boot or logon task review; no disablement.
- `third-party-update ID SOURCE VERSION [--accept-agreements] [--execute]`: Preview by default; one exact version only.
- `third-party-update-recover`: Verify a pending exact version without retrying the update.
- `third-party-updates`: Read-only Microsoft.WinGet.Client application update discovery; no application changes.

## System

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Exact local Bluetooth SIG assignment lookup; no network request.
- `crash-config`: Stored Windows dump mode and paths; no changes.
- `crash-dumps`: Default Windows dump-file metadata; no content analysis or changes.
- `crash-events`: Recent system error reports; no diagnosis or changes.
- `crash-review [--knowledge PACK_PATH ...]`: Up to 32 packs; combined crash, shutdown, bounded minidump structure and optional exact bug-check reference review; no changes.
- `default-printer`: Read-only default selection and automatic-management setting; JSON evidence.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Preview by default; missing or conflicting default only.
- `default-printer-set-recover [--folder PATH]`: Recheck an interrupted selection without repeating it.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: Read-only device evidence with optional exact driver references from up to 32 packs.
- `device-code DECIMAL_OR_0xHEX`: Offline Device Manager problem-code reference.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Reference candidates only.
- `event-channel-enable CHANNEL [--execute]`: Preview by default; enable one exact unmanaged diagnostic channel and preserve other settings.
- `event-channel-enable-recover`: Read-only reconciliation without repeating the enable command.
- `event-channels`: Read-only state, retention, size and saturation for four diagnostic channels.
- `font-registrations`: Read-only bounded machine and current-user registration and file-presence check.
- `memory-diagnostic-results`: Read-only bounded Windows Memory Diagnostic history; JSON evidence.
- `minidump-analyze PATH`: Bounded structural minidump metadata; no memory pages, symbols or changes.
- `name-rename PROPOSED_NAME [--execute]`: Preview by default; standalone workgroup PCs only; never restarts Windows.
- `name-rename-recover`: Verify a pending rename without repeating it.
- `name-review PROPOSED_NAME`: Read-only identity and proposed-name review.
- `profiles --recover`: Verify recorded absence and retain recovery evidence; never retries deletion.
- `profiles --recover-backup`: Verify and archive a pending backup artifact; never retries copying.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Preview first; guarded native copy requires explicit execution and an absent local NTFS destination.
- `profiles --select SID --create-backup-artifact DESTINATION`: Compare an existing copied backup and create its fixed artifact; does not copy or remove a profile.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Read-only review; verification requires one selected SID.
- `task-check`: Read-only task inventory, classified findings and JSON evidence.
- `task-definition TASK_FOLDER TASK_NAME`: Read-only exact task XML; unavailable does not mean absent.
- `task-inventory`: Read-only task identities and states; JSON output.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Historical recovery review; never authorizes retry.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Validate and review a task draft; no registration.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Preview by default; disabled create-only Local System definition.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Read-only exact-definition reconciliation; never retries or runs a task.
- `task-xml PLAN.json YYYY-MM-DD`: Disabled review XML to stdout; local-zone calendar schedules only; no registration.
- `time-config`: Read-only Windows Time mode, peers and policy evidence.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Preview by default; custom mode requires peers.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Preview only.
- `time-recover --folder PATH`: Verify and archive a pending record; never retry changes.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Read-only; custom mode requires peers.
- `wmi-provider`: Read-only local operating-system provider query; JSON evidence.

## Knowledge, validation and engine

- `knowledge PACK_PATH CONTEXT CODE`: Local reference lookup; no repair execution.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Verified local activation; no download.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Verify active local pack; no changes.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Reference changes only; no pack installation.
- `knowledge-info PACK_PATH`: Pack metadata and per-context entry counts; no changes.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Attach contextual references to observed codes.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Select newest verified retained pack; no download.
- `knowledge-search PACK_PATH CONTEXT TEXT`: Up to 50 reference text matches; no repair execution.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Resolve one exact identity across up to 32 packs; conflicts are never selected.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Verify every configured installed database; no download or activation.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Fetch and atomically activate signed database candidates from configured HTTPS endpoints.
- `scan --output <new-file.kbench> [--json]\nkillerbench-cli open <file.kbench> [--json]`
- `validation-coverage RECORD.json [...]`: Summarize current-version native VM evidence; no diagnostics or repairs.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Read-only disposable-VM scenario plan; no diagnostics or repairs.
- `validation-record RECORD.json`: Validate one bounded native VM evidence record; no diagnostics or repairs.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Local current-version VM coverage report.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Run a validated read-only shared option plan.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Inventory only.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Focused diagnostic journal; no repairs.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Journaled review only.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: Optional exact product state included in the journal.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: Optional selected-target diagnosis included in the journal.
- `workflow storage [--volume D:] [--json]`: Journaled read-only filesystem check.
- `workflow-journal FILE.json`: Historical contents; exit 0 means read successfully, not healthy.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Show or validate host-neutral controls; no scan or repair.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Historical report only; no commands run.

## Other commands

- `<analyze|investigate> <text-log-path> [--json]`: Analyze local text evidence.
- `adapter-bindings`: Read-only core protocol binding summary; JSON evidence.
- `adapter-statistics`: Read-only adapter byte, error and discard counters; JSON evidence.
- `application-events`: Read-only recent Application errors, grouped counts and JSON evidence.
- `applocker-policy`: Read-only effective collection modes and aggregate rule counts; rule details excluded; no changes.
- `common-check [--scope NAME ...]`: All diagnostic areas by default; selected areas only when specified; read-only JSON.
- `delete-notifications`: Read-only NTFS and ReFS delete-notification configuration; JSON; no optimization or setting change.
- `diagnostic-scopes [--all]`: List 11 recommended outcome groups, or all 49 read-only areas; no collection.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Focused diagnostic journal; no repairs.
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: All diagnostic areas; no repair unless --auto-repair is explicit.
- `gateway-check`: Bounded ICMP checks of exact configured default next hops; no address scan.
- `infrastructure-service-events`: Read one bounded exact connectivity, remote-management, notification and core service failure snapshot.
- `investigate-batch FILE [FILE ...]`: Up to 12 files; JSON Lines; exit 3 for failed or partial imports.
- `licensing-check`: Read-only Windows activation state without product keys; JSON evidence.
- `mac-vendor PACK_PATH MAC_ADDRESS`: Longest-prefix local pack lookup; no network request.
- `ncsi-policy`: Read-only Windows connectivity classification policy; probe destinations excluded.
- `neighbor-cache`: Read-only privacy-bounded neighbor reachability evidence; JSON result.
- `ntfs-self-healing DRIVE:`: Read-only selected-volume online NTFS repair configuration; no repair or setting change.
- `pagefile-check`: Read-only page-file management, allocation and usage summary; full paths excluded.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Exact local pack lookup; no network request or driver selection.
- `physical-disks`: Read-only current health, operational, media, bus and usage state; JSON; no repair.
- `platform-service-events`: Read one bounded exact application, identity, profile and device-support service failure snapshot.
- `previous-windows [--execute --backup-confirmed]`: Preview by default; permanently removes Windows rollback capability through DISM.
- `previous-windows-recover`: Read-only reconciliation without repeating rollback removal.
- `provisioned-packages`: Packages staged for new users in local Windows; no removal.
- `recipes [WORKFLOW]`: List installed definitions or workflow-specific dependencies and limits; no execution.
- `repair <dism|sfc> [--execute]`: Preview by default.
- `reserved-storage`: Read-only online Windows reserved-storage state; JSON evidence.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Preview by default; creates one point after fresh checks.
- `restore-point-recover`: Verify a pending request without creating another point.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Preview only.
- `shadow-copies`: Read-only current VSS snapshot states and attributes; identities excluded; no snapshot changes.
- `shadow-storage`: Read-only VSS differential-area allocation and pressure; JSON; no resize or snapshot action.
- `uac-policy`: Read-only UAC enablement and elevation-prompt policy; no account or credential data.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Exact local UEFI registry lookup; no network request or driver selection.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Exact local pack lookup; no network request or driver selection.
- `vitals-check`: Read-only Windows, hardware, account, resource, battery and physical-disk snapshots; JSON evidence.
- `win32-code DECIMAL_OR_0xHEX`: Offline installed-Windows message lookup.
- `workflows`: List installed workflow keys and titles; no scan or repair.
