# KillerBench Changelog

## [1.1.0] - Unreleased

1.1.0 introduces a graphical diagnostic workspace alongside improvements to captures, report exports, keyboard access, translations and update recovery.

### Security

- Update signer details now come from the signature verification itself, and the install lock has explicit access permissions.
- An all-users install can apply later updates. The elevated update compared the certificate's full name against part of it and refused every release. Update signatures must now carry the exact publisher name and pass a revocation check, falling back to an offline check only when the revocation server cannot be reached. Update metadata downloads are capped at 1 MB, and an install and an update of one folder lock each other out.

### Fixed

- Theme and language menu labels line up with their radio buttons and check boxes again, instead of sitting a pixel high.
- The theme and language menus keep 8px between their rows and the card edge on every side, and the language menu's divider has the same gap on each side.
- Confirm dialogs size to what they say, so a short prompt is narrower. The heading sits a few pixels higher, with more room above the checkboxes, and on 98SE the dialog is spaced like a Win98 message box.
- On 98SE the theme and language menus pack their rows like Win98 menus, tab titles sit centered, and the window frame is the 4px Win98 sizing frame.
- The Mourning theme highlights selections in its original peach with dark text again, instead of pink.
- Invalid captures show a controlled error, and report exports preserve open evidence and existing files when saving fails or the destination changes.
- Storage reviews refuse redirected roots and retain their directory boundary. Browser extension reviews disclose which account they cover.
- Saved workflow exports retain detailed checks and repairs with historical labels, and HTML reports support every current accent.
- Update failures explain verification rejection, denied launch and canceled elevation while keeping retry available.
- Package reviews, license links and distribution requirements now match the app across all supported languages. The footer retains the running version after theme changes.
- Overview, Investigate and History use the same themed film grain as the evidence pane and follow its on/off switch. Picked up current KillerUI file-picker, menu-card, update recovery and 98SE caption fixes.
- Scroll shadows reach the workspace pane edges, and the obsolete chevron is gone while the far-left utility rail remains.
- The file picker loads icons and picture previews in the background, refreshes overwritten previews, and restores its position on any monitor. Scrollbar buttons have localized screen-reader names.
- Saved workflow and validation reports write their timestamps the same way on every Windows region, so Thai and Finnish settings no longer give years like 2569 or times like 14.30.
- The disk-busy and shadow-storage checks read the decimal comma that Windows tools print on German, Finnish and other regions, instead of reporting no samples or unrecognized sizes.
- A per-user update reopens KillerBench even when antivirus or a locked-down PowerShell stops the update helper from loading.
- An update started from an administrator's copy reopens KillerBench at normal rights. An update that is applied but cannot record its version in the registry now finishes and reopens instead of reporting failure.
- Update downloads stream to disk with a size cap and an overall deadline, and the update check reads only the release's own fields, so release notes that quote them cannot confuse it.
- Updating KillerBench no longer removes a relaunch another Killer app scheduled for its own update.
- No repair can start once an update is handing off, so closing for the update never cuts a repair short.
- A failed update says when another installation is running, the disk is full or Windows policy blocked it, instead of one generic message.
- The file picker no longer stalls on a disconnected network folder when it opens, a relative or malformed path typed in the path box is refused instead of being looked up against the wrong folder, a pattern such as *.log typed in the name box filters the list, and a full path typed in the name box can be accepted even when the shown folder failed to list.
- Changing the file type in the save picker while the name box holds characters no file name can carry keeps the name as typed and shows the invalid-path message.
- About opens at once instead of waiting on the signature check, which can stall for many seconds on a network that drops revocation traffic; the publisher and thumbprint fill in when it finishes.
- A folder typed into the file picker that cannot be opened keeps its reason on the status line after the picker goes back to the last folder that listed.

### Changed

- The Session sources, Knowledge pack and sidebar appearance menus are KillerUI tile cards: icon tiles three across, related actions on their own row, and each tile shows its keyboard shortcut under its name. Large icons and Text under icons stay dimmed on the appearance card.
- Synced the shared controls and themes with current KillerUI: menus and cards keep an even 8px edge, labels sit centered on their check and radio marks, the Dark, Black, Light and Trash Polka menus and text are brighter for contrast, Trash Polka text fields are black, file names in the title bar use the language's own fallback fonts, and the Trash Polka art is cached on disk so it paints faster.
- Reorganized the workbench around an illustrated system map, six symptom entry points and resizable evidence with coverage charts and finding cards. One sidebar on the left lists Home, every area and History, with the console, help, language and theme buttons pinned at its foot, and it drops to icons only below 900 effective pixels or when "No text" is chosen from its right-click menu. Search stays in the title bar, and the map fills the taller default window when screen space permits. Home carries six symptom buttons above the monitor, and each one opens its area and runs the matching check. Each area page lists its checks as rows under a header that holds the area name and the Run and Stop buttons, and the selected row opens in place with that check's options and repair consent. History uses the same header and row style, with the open-workflow and open-capture buttons in its header. Selecting a History row opens it in place with the outcome and each check's state, and its Full report and Raw output buttons swap in the complete views. The separate Investigate page is gone, since the Home symptom chips do the same job in one step, so Ctrl+1, Ctrl+2 and Ctrl+3 now open Home, the current area and History. Reports open under a single header row that holds Back, the title, the Readable report and Raw output toggle and Export, and the toggle moves under the title only when the pane is narrow. An area summary uses the same page header as the area page, lists its checks as slim rows, and marks its area in the sidebar.
- The Overview opens on a coverage gauge that shows how much of the machine was actually checked, with the worst finding's color on its inner ring and counts for fixable, to-review, unknown and fine checks. The areas are drawn as a monitor and an open tower, where each area is the real part it covers: Storage is the drives, Network the Ethernet port and Wi-Fi card, BitLocker the TPM chip, Repair the power supply, and the software areas are windows and icons on the screen. Each part glows in its finding's color with a matching badge, demo results stay labeled on the part, and narrow windows fall back to a plain list. Checks that could not run lower the coverage and are never counted as healthy, and the area summary now uses the same numbers instead of counting an unknown check as a review item too. The area summary also draws those numbers as one colored bar split into fixable, to-review, unknown and fine checks, with a legend. Volumes whose capacity was measured get a used-space bar per drive, colored by that check's finding. Checks now record measured values as typed metrics in the result and its journal, so charts never read numbers back out of report text. The window opens at the height the Overview needs instead of a fixed 780 pixels, so no blank band sits under the map. The readable report opens with the same state bar and drive bars as the Overview instead of four count boxes, each finding card carries its state as a colored stripe, and cards show a next line only when it says something specific to that finding. Investigate shows its three steps as markers in the header and draws each symptom with its area's art, and Tools heads each category with its area's art. The workspace is now one pane: Home stands the system map on the pane's floor with the monitor level with the tower, and the space above the monitor carries the coverage gauge, the map's instruction until the first check, the symptom buttons and the run and capture actions, and choosing an area, a symptom or a History result, or running a tool, swaps its details in over the page, with a Back button that swaps the page back. The session activity strip spans the whole pane, and a language change keeps the open area's details in place. History gets the same illustrated header and lists the session's results on a timeline. Search results show their area's art beside each title.
- Added grouped tool and evidence search plus workspace, capture and export shortcuts, with matching tooltips, menus, keyboard maps and translated Help tables. Text fields and dialogs use the family context menu with mouse and keyboard access.
- Updated the WinGet submission tool to Komac 2.17.2.
- Demo runs and screenshots use realistic workstation findings and normal report labels.
- Single-line text fields are as tall as the dropdowns beside them, so form rows line up instead of the text boxes standing oversized.
- The copyright at the right end of the footer ends 8 px before the corner grip, the same gap in every Killer app, and the grip's dots line up with the other apps' grips.
- Every context menu and flyout is a family card with an icon on each row: the text box menu, the knowledge pack and sources menus, and the file picker's sort and pin menus. The rail menus open from the corner of the content pane, and the knowledge pack menu drops its two lookup rows that repeated the Run button.
- A translation file that fails to load reports through the family dialog instead of a plain Windows message box.
- Toolbar buttons, sidebar rows and list rows draw the film grain over their hover fill, the way menu rows and card tiles do. Trash Polka check boxes and radio wells are solid black.
- A running scan shows as a progress bar in the footer on every page, and the part being scanned pulses on the system map.
- Reports open with a plain verdict, the problems found and one Fix button that confirms before repairing. The full technical report sits behind a toggle.
- Clicking a part that was already scanned opens its report instead of scanning again.

### Added

- Trash Polka, a new theme under 98SE: black with a tattoo-style ink background in your accent color, from eight accents with Blood red as the default. Under a Windows high contrast theme the ink steps aside for the system colors.
- Added the app icon to the Chocolatey package listing.
- The Session sources card is customizable: the pencil moves tiles between the card and its Hidden drawer, and Alt+arrow keys move a tile while editing. Open capture and Save capture always show.

## [1.0.0] - 2026-11-02

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
- Tools are organized into Diagnostics, Repair, Updates, Devices, Network, Apps, Maintenance, Onboarding, Security, BitLocker, Privacy and Storage. Navigation and utility icons stay together on the left; output opens when needed with an 8-pixel resizable divider, wrapping content and a corner export icon. Rail flyouts open right. Each section has its own tools, spaced checklists and labeled error-code lookup. The app opens on Diagnostics.
- Refreshed demo screenshots, translated interface labels and report guidance, and updated help and technical pages in all 20 languages, including the full CLI reference. Original technical definitions and captured evidence retain their source language.
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
- Validation resources include disposable-VM plans, guarded fault injection, symptom coverage checks, isolated UI tests and synthetic demonstrations. Release checks use dedicated scratch folders and can run once for reuse by dry run and release when source and test binaries match. Release preflight checks packaged executable versions, signatures and checksums.
