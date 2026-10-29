# KillerBench adversarial review, 2026-10-29

Four bug-finder passes covered Engine, Engine.Core, Engine.Windows, Checks, and the app shell (root files, Cli, Services, Shell, Features, Tools, Packaging). A defender tried to refute every finding, and a referee read the code and ruled on the ones the defender could not settle. Themes, Controls and Strings were skipped because they come from the shared KillerUI kit.

The referee ruled 26 findings valid, 0 invalid, and 4 needs-investigation. Status reflects this working tree.

## Findings

### High

| ID | Location | Finding | Status |
| --- | --- | --- | --- |
| S-001 | Services/TrustedUpdate.cs:46, App.xaml.cs | The elevated `/apply-verified-update` helper was never routed to `TrustedUpdate.Apply`. The elevated child opened a second admin GUI, the parent waited for it to close, and then the verified download was discarded with no update applied. Machine installs never auto-updated. | Fixed |
| S-002 | App.xaml.cs:68 | Installer hand-offs (`/register-user`, `/register-machine`, `/remove-user-install`) return before `Preferences.Load`, but `OnExit` still saved, so every install or update replaced `appearance.json` with an empty file. | Fixed |

### Medium

| ID | Location | Finding | Status |
| --- | --- | --- | --- |
| S-003 | Services/Preferences.cs:16 | A preferences file of 32 KB or more, or one that fails to parse, was skipped on load and then overwritten on exit. | Fixed (save only after a successful load) |
| D-002 | Engine/Updates/ThirdPartyUpdateExecution.cs:333 | The winget Id and Source went into pwsh single-quoted literals with only the ASCII apostrophe doubled. PowerShell also closes a literal at U+2018 through U+201B, so a typographic quote broke out of it. | Fixed |
| D-005 | Engine/Updates/UpdateDataStoreReset.cs:385 | A service joins the restart list only after a clean stop. A stop that times out or fails partway can leave BITS, wuauserv or cryptsvc stopped for good. | Open |
| E-001 | Engine.Windows/Primitives/WindowsRegistryPrimitives.cs:155 | `registry.set-dword` silently converts an existing value of another type to REG_DWORD. `set-string` refuses a type mismatch. | Open |
| E-002 | Engine/Infrastructure/NativeTool.cs:49 | Stdin is fully written before the stdout and stderr readers start, so a large payload can deadlock until the deadline. A broken-pipe IOException also escapes. | Open |
| E-003 | Engine.Windows/Primitives/WindowsRegistryPrimitives.cs:185 | `registry.set-string` declares `RequiresElevation => false` but accepts HKLM. | Open |
| E-004 | Engine.Core/Recipes/RecipeRunner.cs:187 | `ToolNotStartedException` (an IOException) and a deadline-only `ToolStartCanceledException` are recorded as dispatched, so a repair that never started is reported as uncertain and blocks later runs. | Open |
| E-005 | Engine/Infrastructure/NativeTool.cs:72 | Progress shows only the first 500 characters of each 4096-character read. The referee rated this medium; it is cosmetic. | Open |
| S-007 | App.Installation.cs:154 | Uninstall uses the best-effort `CommandLinePath.Unregister` instead of `UnregisterRequired`, so a stale PATH entry still reports success. | Open |
| S-009 | App.Installation.cs:184 | `CloseOtherCopies` skips any process whose `MainModule` throws, such as an elevated copy seen from an unelevated uninstaller. Uninstall then proceeds while that copy is running. | Open |

### Low

| ID | Location | Finding | Status |
| --- | --- | --- | --- |
| D-001 | Engine/SystemHealth/SystemRestoreCreation.cs:369 | Same quoting flaw in the restore-point description, which runs elevated. | Fixed |
| D-003 | Engine/Printing/PrintRecovery.cs:25 | Same quoting flaw in the selected print queue names. | Fixed |
| D-004 | Engine/Printing/PrinterOfflineRecovery.cs:24 | Same quoting flaw in the printer DeviceIDs. | Fixed |
| D-006 | Engine/Updates/UpdateDataStoreReset.cs:454 | The exception path returns Uncertain with no Finished journal record. | Open |
| D-009 | Engine/Updates/ThirdPartyUpdateExecution.cs:149 | A failure to delete the pending file after a verified update downgrades the result to Needs review. | Open |
| D-011 | Engine/Profiles/ProfileBackupExecution.cs:184 | An IOException from `JsonDocument.Parse` escapes recovery, and `OpenLocked` sits outside the try. | Open |
| S-004 | Services/UpdateService.cs:101 | A release tag without a `v` is accepted, but the download URL is rebuilt with a `v`. This is latent while every tag has the `v`. | Open |
| S-005 | Services/SelfUpdate.cs:28 | A duplicate release parser is exercised only by the checks; the shipping parser in UpdateService has different rules. | Open |
| S-006 | Features/About/AboutController.cs:265 | A failed update hand-off is swallowed, and the user sees no message. | Open |
| C-001 | Checks/IsolatedDesktop.cs:26 | The harness waits on its child with INFINITE. | Open |
| C-002 | Checks/InstallationOwnershipChecks.cs:69 | `mklink` fixture with no timeout, and stderr is read after exit. | Open |
| C-003 | Checks/WindowsRetentionScopeChecks.cs:283 | Junction fixture with no timeout. | Open |
| C-004 | Checks/ExecutableDeploymentChecks.cs:41 | Temp cleanup is not in a finally block. | Open |
| C-005 | Checks/Program.cs:138 | An unguarded `Directory.Delete` in finally can mask the real failure. | Open |
| C-006 | Checks/FilePickerListingChecks.cs:61 | Cleanup deletes the root while a listing may still be running. | Open |
| C-007 | Checks/FilePickerListingChecks.cs:59 | The PASS message reads "completes513". | Open |

Three read-only diagnostics use the same apostrophe-only quoting (EventLogCapacityDiagnostics, DefenderProtectionDiagnostics and ServiceDllDiagnostics), but the values they quote are fixed names in the code, so they were left alone.

### Needs investigation

| ID | Location | Question |
| --- | --- | --- |
| S-008 | App.Installation.cs:146 | Uninstall never removes the start-with-Windows Run value. Should uninstall clear it? |
| D-007 | Engine/Cleanup/PreviousWindowsInstallation.cs:169 | Can a verified rollback removal be reported Uncertain when the journal write or the pending-file delete fails afterward? |
| D-008 | Engine/Cleanup/PreviousWindowsInstallation.cs:155 | Can the `ClosePreparedAsync` retry in the catch block throw out of `RunAsync`? |
| D-010 | Engine/Automation/OrcaCacheReset.cs:252, Engine/Cleanup/CleanupRemoval.cs:119 | Under over-the-shoulder UAC, do the per-user cache cleanups target the admin's profile instead of the signed-in user's? |

### Family drift found during the review

KillerPDF and KillerShot have the same `TrustedUpdate` helper with no `/apply-verified-update` dispatch in `App.xaml.cs`. KillerNotes, KillerScan, KillerShell and Killendar do route it. The KillerPDF case matters for 1.9, so it needs its own fix in each repo; this review did not touch them.

## Fix plan

Each cut is committed separately.

- **KB-A, done.** S-001 update helper dispatch, S-002 and S-003 preference wipe, and D-001 through D-004 quoting. The quoting fix adds `Engine/Infrastructure/PowerShellText.Literal`, which passes text into PowerShell as a base64-decoded expression, so no quote character can end the literal.
  Verification: the solution builds clean, and every non-UI check passes except `PortablePublicationChecks`. That check fails the same way on the unmodified HEAD (`f9b3be2`, "Portable relaunch must hold verified image"), so it predates this review and is tracked separately.
- **KB-B, repair outcomes.** D-005 restarts every service whose stop was dispatched. E-004 records tool-not-started as a no-change failure. D-009, D-006 and D-011 report verified results and close journals correctly.
- **KB-C, engine primitives.** E-001 refuses a type mismatch the way `set-string` does. E-003 requires elevation for HKLM. E-002 starts the stdout and stderr readers before writing stdin and classifies a broken pipe. E-005 reports progress line by line.
- **KB-D, install and update.** S-007 uses `UnregisterRequired`. S-009 treats an unreadable process named KillerBench.exe as running. S-006 shows a notice when the hand-off fails. S-004 requires the `v` in the tag. S-005 removes the duplicate parser and points the checks at UpdateService.
- **KB-E, harness hygiene.** C-001 through C-007.
- **KB-F, investigations.** D-007, D-008, D-010 and S-008. Each gets a ruling and then either a fix or a note explaining why the current behavior stands.
- **Family.** Port the S-001 dispatch to KillerPDF and KillerShot once Steve confirms the approach.
