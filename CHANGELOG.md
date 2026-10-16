# Changelog

## [0.1.0] - Unreleased

KillerBench is an all-in-one Windows diagnosis, repair and maintenance toolbox. Its first release brings a read-only Full system scan, focused tools for common Windows problems, readable reports and a shared Engine and CLI for scripting and integration. It connects findings to targeted next steps and lets you review and approve supported repairs with fresh prerequisites, journals and follow-up verification. Native repair validation is still in progress.

### Data protection and repair safeguards

- Added the guarded repair model shared by the app, Engine and CLI: every repair is individually selected, rechecks fresh evidence and exact eligibility immediately before dispatch, writes a durable pending record, verifies the result, and reconciles an interrupted request on a later boot without repeating it.
- Kept diagnosis and repair separate. Full system scan was read-only, saved reports and historical events could not authorize a change, and standard-user diagnostics started without an elevation prompt while repairs kept their administrator gates.
- Kept missing, failed, timed-out, malformed, duplicate, contradictory and truncated evidence unknown, so it blocked dependent steps and repairs and never reported as healthy or verified. Independent checks continued when one provider failed.
- Bounded every provider, checkpoint, workflow, repair and journal wait with deadlines and Stop support. Late provider results could not overwrite a final journal or count as verified success, and failed persistence stopped the workflow.
- Repair parsers for DISM, SFC, component cleanup, DHCP, printing, services, Windows Time and WMI rejected command error output before accepting eligibility or verification evidence; integrity results retained raw evidence.
- Service recovery required configured automatic startup, fresh executable and ServiceDll presence, matching identity, complete stopped-service evidence, nested dependency checks with cycle and size limits, and two consecutive Running observations before reporting success. Unmapped failure codes, startup timeouts and unexpected exits deferred automatic starts for review.
- Workflow recovery kept the mutation guard through target-specific verification and prioritized interrupted work, failures, timeouts and restarts before another repair. Repair workflows stopped for review when a provider returned an unexpected repair identity.
- Windows Update planning and installation rejected failed discovery, malformed classifications, conflicting metadata, empty or duplicate identities and provider results that changed the requested scope, and repeated exact readiness checks after durable preparation.
- Required separate consent for one exact Windows feature upgrade and its restart, and a valid signature from the confirmed vendor for onboarding installers.
- DHCP renewal, print-queue resume and printer offline recovery matched exact adapter SettingIDs and queue identities, and verified results against both operation and follow-up snapshots.
- CHKDSK required one exact clean verdict line, and WMI repository checks required a single complete verdict before selecting salvage.
- Generic automatic repair turned off when a separate Defender scan was selected, and focused Storage consent carried through the main GO handoff.
- Installer readiness reported AlwaysInstallElevated exposure without changing managed settings.
- App package removal eligibility stayed blocked for ordinary Store/MSIX packages, and Outlook cache retention appeared only when exact prerequisites passed.

### Diagnosis and workflows

- Added Full system scan as the read-only entry point in the app, Engine and CLI, with numbered progress, selectable diagnostic areas, clear readiness verdicts and a saved local journal.
- Organized tools into 12 sections: Diagnostics, Repair, Updates, Devices, Network, Apps, Maintenance, Onboarding, Security, BitLocker, Privacy and Storage. KillerBench opened on Diagnostics, and Repair held the focused recovery workflows.
- Added Common problems, which runs the installed recipes in one session with a collapsed advanced check selector, prerequisite expansion and per-area collection deadlines.
- Simplified focused diagnostics to 11 outcome groups by default, with all 49 evidence areas behind an expert option in the app and CLI.
- Added symptom triage with a Windows symptom catalog, `symptom` CLI commands and a goal runner that reports what was repaired, ruled out and left unresolved.
- Added versioned repair recipes with dependency handling, diagnostic checkpoints, restart-required outcomes, and recipe selection through the app, the Engine and `--recipes` in the CLI.
- Recipe metadata and the check selector showed each repair's effects and preservation limits before a run.
- Limited KillerBench to the local computer and removed the Intune deployment review and tenant sign-in.

### Targeted repairs and maintenance

- Added guarded BitLocker encryption, data-volume encryption, auto-unlock, TPM and recovery-password protectors, protection resume, decryption with a second confirmation, recovery-file backup and recovery-password backup to Active Directory or Microsoft Entra ID. TPM preparation never clears the TPM.
- Added an explicit FIPS algorithm policy switch for BitLocker PIN compliance.
- Added guarded service recovery for Windows Update, Update Orchestrator, Update Medic, BITS, Cryptographic Services, Windows Modules Installer, Delivery Optimization, Windows Installer, Task Scheduler, Event Log, WMI, Windows Time, DNS Client, SMB client and server, firewall, backup, printing, search, audio, device, account and core infrastructure services, with dependency ordering and preserved configuration.
- Added guarded DISM and SFC repair, component-store analysis and cleanup, managed repair-source diagnosis and later-boot reconciliation that never repeats servicing.
- Added guarded Windows Update data-store reset, exact-version Windows and third-party update installation, BITS job retry and Defender signature refresh.
- Added guarded Windows Installer product and registration repair with fresh raw-evidence gates.
- Added guarded NTFS repair with dismount consent or restart scheduling, and selected-volume optimization, retrim and hard-disk defragmentation.
- Added guarded network recovery for DHCP renewal, hostname DNS cache flushing, WinHTTP proxy reset, Wired AutoConfig and WLAN services.
- Added guarded printing recovery for paused and offline queues, stuck print jobs, the default printer and the spool directory.
- Added guarded Windows Recovery Environment enablement, System Restore point creation, previous Windows installation removal through supported rollback servicing, and standalone or workgroup computer renaming.
- Added guarded Windows Search index rebuild, diagnostic event-channel enablement, Windows Firewall profile enablement and Windows Time source configuration.
- Added profile review with age controls, saved plans and backup coordination with complete source-to-copy verification. Native profile deletion was left out.
- Added configurable cleanup for temporary files, caches, error reports, crash dumps, thumbnails, icons, Delivery Optimization, SystemTemp, Office caches and the Recycle Bin, with previews, an age slider, per-target limits and per-file rechecks.
- Added guarded privacy controls for app permissions, camera, microphone, typing personalization, web search, cloud content search, search highlights and suggestions, with exact-value undo that respects policy.
- Added the Windows declutter review across Recall and AI, telemetry, suggestions, consumer content, taskbar, Edge, OEM software and vendor data, with guarded High performance, power-timer and hibernation controls.
- Added Microsoft 365 app removal review with guarded current-account package removal behind a separate red control and a second confirmation, and an Outlook reset review with guarded cache retention.
- Added onboarding provisioning review with application and setup selections.
- Added guarded scheduled-task registration for disabled signed Local System tasks, and Defender quick and full scan coordination.
- Added recipe options so targeted repairs take an exact adapter, device, volume, printer or package.

### Diagnostic coverage

- Added readiness conclusions for Windows Update, servicing, backup, system stability, security, certificates, activation, app packages, application deployment, storage, Storage Spaces, network, services, scheduled tasks, startup and recovery that combine current state with bounded history.
- Added Windows Update diagnosis covering scans, Operational and update history, engine files, servicing-chain states, error references, policy, deadlines, feature targeting, Delivery Optimization, BITS, hotfixes, cache size, reserved storage and restart state.
- Added offline CBS, DISM, Windows Setup and compressed CBS archive analysis with reviewed code guidance.
- Added the read-only servicing activity monitor for live Windows Update and Setup progress, process metrics, transfer deltas and readiness context.
- Added Windows Installer diagnosis covering policy, logging, sources, the MSI cache, activity, 17 recognized log outcomes and failure history matched to later success.
- Added Vitals with Windows, hardware, memory, battery, disk health, reliability, WHEA hardware errors, crash reports, minidump analysis, memory diagnostic results and unexpected shutdown history.
- Added Storage diagnosis covering disks, partitions, volumes, capacity, dirty bits, reliability counters, Storage Spaces, shadow storage, quotas, delete notification, Storage Sense, page files and storage event history.
- Added Network diagnosis covering IP configuration, routes, gateways, DNS, Hosts, proxies, DHCP, Winsock, TCP ports and templates, offload, neighbors, adapter bindings and counters, firewall rules and history, SMB client and shares, and selected-target reachability.
- Added Printing diagnosis covering queues, jobs, ports, Standard TCP/IP endpoints, drivers and signatures, processors and monitors, Point and Print policy and PrintService history.
- Added Services diagnosis covering configuration, dependencies, executable and DLL presence, path quoting, logon classes, recovery policy, triggers and Service Control Manager failure history, without flagging ordinary disabled or demand-start services.
- Added Security diagnosis covering TPM, Secure Boot, VBS, memory integrity, Credential Guard, Defender settings and history, Security Center, UAC, AppLocker, certificates and activation, without collecting exclusions or policy values.
- Added device diagnosis covering Device Manager problem codes, Plug and Play classes, input, audio, display, Bluetooth, camera, smart-card and biometric devices, driver references and SetupAPI install history.
- Added Apps diagnosis covering installed application inventory and comparison, app packages, startup registrations, browser extensions for Chrome, Edge, Brave and Firefox, Reliability Monitor history and application errors.
- Added scheduled-task inventory, inspection, result guidance and missed-run diagnosis, plus exported definitions for review.
- Added backup, File History, System Restore, Windows Recovery Environment, WinRM, Remote Desktop, Remote Access, Event Collector, Event Log and WMI diagnosis.
- Added duplicate-file and large-file reviews for one selected local folder, and read-only review of an exact application folder.

### Engine, CLI and reports

- Added The KillerBench.Engine with the platform-neutral Engine.Core, declarative recipe packs, a whitelisted primitive library, a journaled recipe runner and Windows providers in Engine.Windows.
- Added `killerbench-cli` with the full command catalog, JSON output, live progress, distinct exit codes for verified, restart, canceled and incomplete outcomes, and `-recover` commands for interrupted actions.
- Bound Engine-owned option definitions to every matching desktop control, so defaults, limits and help text matched across the app, Engine and CLI.
- Added readable reports beside raw output, with prioritized findings, next steps, elevation guidance, bounded evidence previews, and themed HTML and text export.
- Opened saved workflow journals, Windows Update captures and EVTX files in the report view with their historical scope.
- Linked report findings to the matching focused repair or Maintenance option without approving an action.
- Let KillerMCP use the installed CLI to look up Device Manager and Windows error codes.
- Replaced the old script names with plain feature names in the app, reports and CLI messages.

### Knowledge and reference tools

- Added signed knowledge databases with HTTPS retrieval, activation and retained-version recovery.
- Added offline lookups for Windows error codes, HRESULTs, NTSTATUS, bug checks, Device Manager codes, PCI and USB IDs, MAC vendors, UEFI vendors and Bluetooth company identifiers.
- Added driver-reference filtering by exact hardware ID and declared OS scope.
- Added deterministic knowledge-pack generators, schema validation, revision comparison and context-specific search in the app, Engine and CLI.

### Desktop, accessibility and localization

- Icon-only buttons give screen readers the name from their tooltip, icon glyphs stay out of what screen readers read, the tool list is named after its category, and controls reached with the keyboard show a dotted focus outline.

- Kept app icons sharp at fractional display DPI and app zoom by caching exact pixel sizes from a covering source and aligning the final draw to device pixels.

- Added the three-pane workbench: a resizable section sidebar with the icon rail, the selected tool's options in the center, and a wider output pane on the right whose Raw output opens during a run and gives way to the Readable report, plus an activity log.
- Added category icons and a slimmer default sidebar whose buttons share the available height, keep a readable minimum with scrolling, and place icons above labels when the buttons are tall.
- Added KillerMCP Options to the About card. Agent repairs through KillerMCP stayed off until turned on there, disruptive repairs needed a separate setting and desktop approval, and a locked state after an uncertain result could be cleared from the same place.
- Added 13 family themes with yellow and magenta accents on the neutral and 98SE themes. New installs and the website started on Dark with the yellow accent.
- Added a Film Grain switch to the theme menu and Alt+Shift+G to toggle it, listed on the shortcut views' new Alt+Shift layer. It was on by default, and turning it off removed the grain texture from every surface.
- Added the Windows 11 snap layouts menu to the maximize button.
- Set the wordmarks in Punktype and Lacquer, page, sidebar and dialog headings in Lacquer, and the interface text and footers in Killer Sans, in place of the typewriter font in the app and on the website.
- Added localization in 19 languages, including Vietnamese, Ukrainian, Norwegian (Bokmål) and Brazilian Portuguese, with a two-column language menu.
- Added the family List and Keyboard shortcut views from one binding table, linked to the online guide, with F12 for About.
- Used KillerUI-style confirmations and Open, Save and Folder dialogs throughout the app.
- Zoom enlarged inputs and output inside fixed panes, and the workspace stayed within the window when changing tools or zoom.
- Matched the family palette, control states, gradients, menus, rail flyout spacing, footers and About card. Submenus attached to their opening rows with native direction and screen-edge placement.
- Improved text contrast on selected sections, expanders, option labels, menus and footers.
- Centered website logos with space above and below at every screen size and standardized Lacquer headers and documentation text.
- Menu separators draw their divider line instead of leaving an empty gap.
- Used the family outline button for the run diagnosis, open log and update scan buttons instead of a solid dark block.
- Set context menus in Killer Sans UI like the rest of the interface instead of Segoe UI.

### Documentation, packaging and validation

- Silent installs return specific Windows error codes and reject a second concurrent install instead of reporting a general failure.
- A silent install started without admin rights, as the Microsoft Store runs it, installs for the current user with no prompt instead of failing.
- Added the KillerBench installer, which checks for the .NET 10 Desktop Runtime and adds Start menu, desktop and Installed Apps entries, and a portable build that carries its own runtime. Both packages and the single-file CLI include the GPLv3 license.
- Added update checks on startup and from the About card. A newer release was offered with an Always check on startup box, a Check for updates on startup checkbox at the top of the About card turned the check back on, and an accepted update was checked against the release checksum and signature before it replaced the installed or portable copy.
- Added the killerbench.net landing site with a searchable user guide, the Engine developer guide and KillerMCP setup.
- Added deterministic VM validation plans, a gated native fault-injection runner and themed coverage reports.
- Added the symptom coverage matrix with consistency checks.
- Added isolated UI tests on a separate desktop, synthetic repair demonstrations and a screenshot preset with fixed sample reports.

### Fixed
- On the Light theme's Yellow accent, buttons, links, tabs and accent icons stay yellow with a thin gray outline around yellow text and icons, instead of turning near-black.
- On the website's Light theme with the Yellow accent, links, headings and labels stay yellow with a thin gray outline instead of turning olive brown.
- The website top bar no longer leaves extra space under the wordmark and buttons.
- Theme and language radio buttons and the Film Grain checkbox line up with their labels again.

