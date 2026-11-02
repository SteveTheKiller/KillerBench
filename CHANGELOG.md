# KillerBench Changelog

## [1.0.0] - Unreleased

KillerBench is an all-in-one Windows diagnosis, repair and maintenance toolbox. Its first release combines a read-only Full system scan, focused repair tools, readable reports and an Engine and CLI for scripting and integration. It is free and you use it at your own risk.

### Data protection and repair safeguards

- Diagnosis stays read-only. Full system scan, saved reports and historical events cannot authorize repairs. Standard-user diagnostics run without an elevation prompt.
- Report and recipe exports preserve competing files created during export. Reports replace existing files only with explicit overwrite permission.
- Repairs require individual selection, explicit consent, fresh prerequisites and exact target checks. Durable journals track pending work, verify results and reconcile interrupted requests without repeating them. Malformed privacy and profile-removal recovery records stay unresolved without calling repair providers; privacy undo also requires recorded machine identity.
- File retention checks directory identity and refuses redirected children or cross-volume moves. Profile backups preserve ordinary WindowsApps files, record supported link omissions and verify copied files. EFS-encrypted sources are refused before copying; interrupted cleanup preserves partial results and reports recovery failures.
- Task registration rejects added triggers, actions and nondefault settings during verification. Malformed dates leave pending recovery unresolved.
- BitLocker recovery-file inspection accepts protected SYSTEM-created backups and rejects unexpected, inherited or insufficient access.
- Missing, failed, timed-out, contradictory or incomplete evidence stays unknown and blocks dependent repairs. Independent checks can continue when one provider fails.
- Operations have deadlines and Stop support. Cleanup rechecks cancellation before removal, late results cannot count as success, and uncertain outcomes block further repairs until reviewed.
- Installed copies use a verified installer; portable copies update their launcher. Updates verify image and publisher through staging, publication and relaunch, including queued launches. Failed updates preserve verified recovery images and instructions. Install, update and uninstall preserve preferences.
- Per-user cleanups (temporary files, shader, browser and Office caches, error reports, crash dumps, thumbnail and icon caches) clean the account signed in to Windows even when KillerBench was elevated with another administrator's credentials. The Outlook cache reset declines in that case, since Outlook's settings and credentials belong to the account KillerBench runs as.
- A repair whose tool never started (missing, refused, or out of time before launch) is reported as a failure with nothing changed, so it does not hold up later repairs.
- Registry DWORD repairs leave an existing value of another type unchanged instead of converting it.
- Restore-point descriptions, printer queue names and package identities reach elevated PowerShell as encoded data, so no character in them can end a quoted value and run as script.
- Profile copies and task registration defer app updates and closing until the active operation stops. Failed portable updates preserve executable copies and recovery instructions.
- Windows and app updates recheck the exact package and version before installation. Feature upgrades and restarts require separate consent; onboarding installers require a valid signature from the confirmed vendor.
- Service recovery checks configuration, executable files and dependencies, preserves startup settings, and requires stable Running observations before reporting success. The Windows Update data-store reset restarts every service it asked to stop, even when the stop fails partway. Network and printer repairs bind to the selected adapter or queue; interrupted MTU writes remain uncertain.
- Diagnostic reports preserve failed proxy reads and invalid Group Policy timestamps as unknown. Entra reports and journals include join and token flags without account or device identities.
- Setup checks destination ownership and elevated access, recognizes registered custom installations, and preserves recovery folders. Uninstall removes matching registration, shortcuts, the PATH entry and start-with-Windows, waits for every running copy including an elevated one, retains files and shows their location.

### Diagnosis and workflows

- Full system scan covers 49 diagnostic areas with selectable checks, numbered progress, readiness verdicts and a saved local journal. Focused diagnostics group results into 11 outcomes, with detailed evidence available through expert options.
- Tools are organized into Diagnostics, Repair, Updates, Devices, Network, Apps, Maintenance, Onboarding, Security, BitLocker, Privacy and Storage. Navigation stays visible; output opens when needed. Each section has its own tools, spaced checklists and labeled error-code lookup. The app opens on Diagnostics.
- Common problems evaluates installed recipes in one session, expands prerequisites and limits collection time for each area.
- Symptom triage connects Windows symptoms to targeted checks and reports what was repaired, ruled out or left unresolved. Versioned recipes describe effects, preservation limits, dependencies and restart requirements.
- Workflows are available through the desktop app, Engine and CLI and operate on the local computer.

### Targeted repairs and maintenance

- BitLocker tools cover system and data-volume encryption, auto-unlock, TPM and recovery-password protectors, protection resume, recovery-file backup and recovery-password backup to Active Directory or Microsoft Entra ID. Decryption requires a second confirmation; TPM preparation never clears the TPM.
- DISM and SFC repair, component-store analysis and cleanup, and managed repair-source diagnosis include later-boot reconciliation without repeating servicing.
- Windows Update tools support data-store reset, exact-version Windows and third-party updates, BITS job retry and Defender signature refresh.
- Service recovery covers Windows Update and servicing, networking, printing, backup, security, search, audio, devices and core Windows services, with dependency ordering and preserved configuration.
- Storage tools include NTFS repair with dismount consent or restart scheduling, selected-volume optimization, retrim and hard-disk defragmentation. Duplicate review bounds actual bytes read and reports changed or replaced files as incomplete.
- Profile review includes age controls, saved plans and verified backups. Native profile deletion is excluded. Cleanup offers previews, age limits and per-file checks for temporary files, caches, error reports, crash dumps and the Recycle Bin.
- Network recovery includes DHCP renewal, DNS cache flushing, WinHTTP proxy reset, Wired AutoConfig and WLAN services. Printing recovery covers paused or offline queues, stuck jobs, the default printer and spool files.
- Windows Installer tools support product and registration repair. Recovery tools cover Windows Recovery Environment, restore points and removal of a previous Windows installation through supported rollback servicing.
- Security tools coordinate Defender quick and full scans, enable selected firewall profiles and diagnostic event channels, and register disabled signed Local System tasks. BitLocker PIN compliance has an explicit FIPS algorithm policy switch.
- Privacy and declutter tools review app permissions, camera, microphone, typing, search, Recall and AI, telemetry, suggestions, consumer content, Edge and OEM software. Supported changes have exact-value undo that respects policy.
- Additional maintenance includes Windows Search index rebuild, Windows Time source configuration, power plans and timers, hibernation, and standalone or workgroup computer renaming.
- Microsoft 365 removal and Outlook reset reviews expose supported actions only when exact prerequisites pass. Eligible current-account Microsoft 365 package removal requires a separate confirmation; ordinary Store/MSIX removal remains blocked.
- Onboarding provides application and setup selections. Targeted recipes take an exact adapter, device, volume, printer or package.

### Diagnostic coverage

- Windows Update and servicing checks cover scan and update history, component health, policy, deadlines, feature targeting, Delivery Optimization, BITS, reserved storage and restart state. Offline CBS, DISM, Windows Setup and compressed CBS logs include error guidance; a read-only activity monitor follows live servicing progress.
- Vitals brings together Windows and hardware details, memory, battery, disk health, reliability, hardware errors, crash reports, minidump analysis, memory diagnostics and unexpected shutdown history.
- Security checks cover TPM, Secure Boot, VBS, memory integrity, Credential Guard, Defender, Security Center, UAC, AppLocker, certificates and activation without collecting Defender exclusions or policy values.
- Storage checks cover disks, partitions, volumes, capacity, reliability, Storage Spaces, shadow storage, quotas, Storage Sense, page files and event history. Large-file, duplicate-file and application-folder reviews stay read-only and identify incomplete coverage.
- Browser extension review preserves valid results from other profiles when Firefox data is malformed and bounds directory enumeration before sorting.
- Network checks cover IP configuration, routes, gateways, DNS, Hosts, proxies, DHCP, Winsock, TCP, adapter settings and counters, firewall rules, SMB and selected-target reachability.
- Services and scheduled-task checks cover configuration, dependencies, executable presence, recovery settings, failure history and missed runs. Ordinary disabled or demand-start services are not treated as failures.
- Printing checks cover queues, jobs, ports, drivers and signatures, Point and Print policy and PrintService history. Device checks cover Device Manager problem codes, hardware classes, driver references and SetupAPI history.
- Apps and Windows Installer checks cover installed applications, packages, startup registrations, browser extensions, application errors, installer policy, sources, cache, logs and failure history matched to later success.
- Backup and recovery checks cover File History, System Restore, Windows Recovery Environment, WinRM, Remote Desktop, Remote Access, Event Collector, Event Log and WMI.

### Engine, CLI and reports

- The shared Engine separates platform-neutral primitives and declarative recipes from Windows providers, with a journaled runner and matching option defaults, limits and help across the app and CLI.
- Commands run through `killerbench` or `killerbench-cli`, with JSON output, live progress, distinct exit codes, capability discovery and recovery commands, including explicit power-setting reconciliation. The help landing page includes the full command guide. Installed copies add their folder to PATH; portable copies register nothing, and command runs leave desktop preferences alone.
- Readable reports prioritize findings and next steps beside raw output, with elevation guidance, original-line evidence, bounded previews and themed HTML or text export.
- Saved workflow journals, Windows Update captures, EVTX files and imported logs open in the report view. Historical reports retain their scope notice, incomplete imports retain coverage warnings, and empty imports show an explicit no-match result.
- Report findings link to the matching repair or Maintenance option without approving it. KillerMCP can use the installed CLI for Windows and Device Manager error-code lookups.

### Knowledge and reference tools

- Signed knowledge databases support HTTPS retrieval, activation and recovery of retained versions.
- Offline references cover Windows errors, HRESULTs, NTSTATUS, bug checks, Device Manager codes, PCI and USB IDs, MAC and UEFI vendors, and Bluetooth company identifiers.
- Driver references filter by exact hardware ID and declared OS support. Knowledge packs support schema validation, revision comparison and contextual search in the app, Engine and CLI.

### Desktop, accessibility and localization

- KillerMCP repair access is off by default and controlled from About. Disruptive agent repairs require a separate setting and desktop approval; uncertain results retain a review lock.
- The three-pane workbench combines a resizable section sidebar and icon rail, tool options, readable reports, raw output and an activity log.
- Screen readers receive names for controls and file-picker rows. Keyboard users get visible focus, arrow-key navigation within option groups, and focus that enters and returns from the About and shortcuts cards.
- Interface zoom scales reports and controls inside scrollable panes. Windows fit the starting monitor's work area, Close stays reachable in short windows, and icons remain sharp at fractional display scales. Alt+drag moves any window.
- Family file and folder pickers include title-bar Close, a sort menu, a unified fade, sunken 98SE wells and a plain arrow while dragging pinned places. Folder pickers list folders only, and Select accepts the highlighted folder. Confirmation popups use the family frame and KillerBench wordmark.
- The file picker's Accept button stays off while a folder is loading or an accept is already running, so it cannot fire twice, and a mistyped folder path returns to the last folder that worked.
- Keyboard focus cues respect the theme, Windows contrast refreshes are coalesced and restore frame colors, and picker selections keep text and icons shadow-free. Recent locations and places lists are named for screen readers.
- Twenty languages, including Slovenian, cover the app, installer, tool descriptions, options, reports, themes and action labels. Dates and numbers keep the Windows region format whatever the interface language. Saved themes, accents and languages carry across starts, and language changes preserve the app's styles.
- Thirteen family themes include accent choices, Windows contrast-theme support and readable text. Dark with the yellow accent is the default; Film Grain can be toggled from the theme menu or with Alt+Shift+G.
- Shared family themes, controls, fonts, dialogs, menus, footers and About cards keep colors, grain, geometry and spacing consistent. Outline buttons have accent borders, filled hover and deeper pressed states; caption buttons use accent hover with gray Close at rest. Light fields use family grays, 98SE uses thin bevels and compact captions, and the maximize button supports Windows 11 snap layouts. File pickers use theme content colors for files and bookmarks.
- Wordmarks and headings have a subtle bevel in their own color, and wordmarks retain their drop shadow. Dark's red accent uses the brand red.
- Hovered menu and list items use readable accent text. Theme-menu hover rows have matching left and right insets.
- List and Keyboard shortcut views share one binding table and link to the online guide. F12 opens About.
- About groups settings in two columns and can hide shortcut hints in menus and tooltips without disabling keyboard shortcuts. Hover tooltips clear large pointers while preserving custom placement.
- The tray icon's menu opens from the pointer upward, out of the notification area, the same way as in every Killer app.
- In 98SE, long file-dialog paths stay clear of the recent-locations arrow, and menu hover fill follows the selected accent.
- Light is shades of gray with nothing white: the background, sidebar and footer are the darkest gray (#cccccc), menus and bars a step lighter, then panes, then fields.
- Black table headers match the content surface, softer grain keeps dark surfaces from looking gray, and the About card fades in and out like the rest of the Killer Tools family.

### Documentation, packaging and validation

- Installer, portable and Microsoft Store packages include the GPLv3 license. The installer checks for the .NET 10 Desktop Runtime, including x64 on Arm64 Windows; portable and Store packages carry their own runtime.
- Setup creates Start menu, desktop and Installed Apps entries. Unelevated silent installs use the current account, and concurrent installs or a running app return specific error codes for deployment tools.
- Startup and manual update checks offer newer releases with checksum and signature verification. Automatic checks can be controlled from About. Family update panels wait for active repairs, and an installer-start failure opens the release page.
- The killerbench.net website includes a searchable user guide, section walkthroughs, command reference, glossary, Engine developer guide and KillerMCP setup, with documentation in 20 languages.
- The website includes captioned screenshots, keyboard references and links to other Killer Tools apps, with responsive navigation, localized accessible labels and theme-aware text contrast. Its theme menu can turn Film Grain off (Alt+Shift+G).
- Validation resources include disposable-VM plans, guarded fault injection, symptom coverage checks, isolated UI tests and synthetic demonstrations. Release preflight checks packaged executable versions, signatures and checksums.
