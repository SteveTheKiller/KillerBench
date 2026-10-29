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
| D-005 | Engine/Updates/UpdateDataStoreReset.cs:385 | A service joins the restart list only after a clean stop. A stop that times out or fails partway can leave BITS, wuauserv or cryptsvc stopped for good. | Fixed |
| E-001 | Engine.Windows/Primitives/WindowsRegistryPrimitives.cs:155 | `registry.set-dword` silently converts an existing value of another type to REG_DWORD. `set-string` refuses a type mismatch. | Fixed |
| E-002 | Engine/Infrastructure/NativeTool.cs:49 | Stdin is fully written before the stdout and stderr readers start, so a large payload can deadlock until the deadline. A broken-pipe IOException also escapes. | Fixed |
| E-003 | Engine.Windows/Primitives/WindowsRegistryPrimitives.cs:185 | `registry.set-string` declares `RequiresElevation => false` but accepts HKLM. | Won't fix: elevating would point HKCU writes at the admin's hive, and unelevated HKLM fails at key open before anything is written |
| E-004 | Engine.Core/Recipes/RecipeRunner.cs:187 | `ToolNotStartedException` (an IOException) and a deadline-only `ToolStartCanceledException` are recorded as dispatched, so a repair that never started is reported as uncertain and blocks later runs. | Deferred: needs a decision |
| E-005 | Engine/Infrastructure/NativeTool.cs:72 | Progress shows only the first 500 characters of each 4096-character read. The referee rated this medium; it is cosmetic. | Won't fix: the 500-character cap is a progress throttle, and the full output is kept in the result |
| S-007 | App.Installation.cs:154 | Uninstall uses the best-effort `CommandLinePath.Unregister` instead of `UnregisterRequired`, so a stale PATH entry still reports success. | Fixed |
| S-009 | App.Installation.cs:184 | `CloseOtherCopies` skips any process whose `MainModule` throws, such as an elevated copy seen from an unelevated uninstaller. Uninstall then proceeds while that copy is running. | Fixed |

### Low

| ID | Location | Finding | Status |
| --- | --- | --- | --- |
| D-001 | Engine/SystemHealth/SystemRestoreCreation.cs:369 | Same quoting flaw in the restore-point description, which runs elevated. | Fixed |
| D-003 | Engine/Printing/PrintRecovery.cs:25 | Same quoting flaw in the selected print queue names. | Fixed |
| D-004 | Engine/Printing/PrinterOfflineRecovery.cs:24 | Same quoting flaw in the printer DeviceIDs. | Fixed |
| D-006 | Engine/Updates/UpdateDataStoreReset.cs:454 | The exception path returns Uncertain with no Finished journal record. | Fixed |
| D-009 | Engine/Updates/ThirdPartyUpdateExecution.cs:149 | A failure to delete the pending file after a verified update downgrades the result to Needs review. | Fixed |
| D-011 | Engine/Profiles/ProfileBackupExecution.cs:184 | An IOException from `JsonDocument.Parse` escapes recovery, and `OpenLocked` sits outside the try. | Fixed (read failures return Failed; the lock open still throws, which callers treat as busy) |
| S-004 | Services/UpdateService.cs:101 | A release tag without a `v` is accepted, but the download URL is rebuilt with a `v`. This is latent while every tag has the `v`. | Family: the same line is in all eight apps |
| S-005 | Services/SelfUpdate.cs:28 | A duplicate release parser is exercised only by the checks; the shipping parser in UpdateService has different rules. | Open |
| S-006 | Features/About/AboutController.cs:265 | A failed update hand-off is swallowed, and the user sees no message. | Family: the same handler is in all eight apps |
| C-001 | Checks/IsolatedDesktop.cs:26 | The harness waits on its child with INFINITE. | Fixed (15-minute limit, then the child is ended) |
| C-002 | Checks/InstallationOwnershipChecks.cs:69 | `mklink` fixture with no timeout, and stderr is read after exit. | Fixed |
| C-003 | Checks/WindowsRetentionScopeChecks.cs:283 | Junction fixture with no timeout. | Fixed |
| C-004 | Checks/ExecutableDeploymentChecks.cs:41 | Temp cleanup is not in a finally block. | Won't fix: the root sits inside the run's scratch folder, which RunChecks deletes in its own finally |
| C-005 | Checks/Program.cs:138 | An unguarded `Directory.Delete` in finally can mask the real failure. | Fixed (both `--only` branches) |
| C-006 | Checks/FilePickerListingChecks.cs:61 | Cleanup deletes the root while a listing may still be running. | Open: only a failed run hits it, and the original failure is still thrown first |
| C-007 | Checks/FilePickerListingChecks.cs:59 | The PASS message reads "completes513". | Fixed |

Three read-only diagnostics use the same apostrophe-only quoting (EventLogCapacityDiagnostics, DefenderProtectionDiagnostics and ServiceDllDiagnostics), but the values they quote are fixed names in the code, so they were left alone.

### Needs investigation

| ID | Location | Question |
| --- | --- | --- |
| S-008 | App.Installation.cs:146 | Real, fixed. KillerNotes already removes the Run value on uninstall, so KillerBench now does too and counts a failure as incomplete. |
| D-007 | Engine/Cleanup/PreviousWindowsInstallation.cs:169 | Real, fixed. A verified removal stays Verified when its bookkeeping fails, and the pending record is left for reconciliation. |
| D-008 | Engine/Cleanup/PreviousWindowsInstallation.cs:155 | Real, fixed. `ClosePreparedAsync` no longer throws; an archive failure keeps the no-change result and leaves the pending record. |
| D-010 | Engine/Automation/OrcaCacheReset.cs:252, Engine/Cleanup/CleanupRemoval.cs:119 | Real, fixed per Steve's call to target the signed-in user. `SignedInUser` reads the owner of this session's Explorer and resolves that account's own Local AppData from its profile and User Shell Folders. File cleanups (Temp, shader, browser, Office, error reports, crash dumps, thumbnails, icons) follow it, and are reported partial and skipped when the account is ambiguous or unresolvable. The Outlook cache reset refuses under another account instead, because its add-ins, profiles, credentials and package registration belong to the running account. The Recycle Bin target still uses the running account's shell. |

### Family drift found during the review

KillerPDF and KillerShot have the same `TrustedUpdate` helper with no `/apply-verified-update` dispatch in `App.xaml.cs`. KillerNotes, KillerScan, KillerShell and Killendar do route it. Both are now fixed: KillerPDF `5d7d7f2` (1,446 headless tests pass) and KillerShot `c356ecc` (builds clean; it has no unit test project).

## Fix plan

Each cut is committed separately.

- **KB-A, done.** S-001 update helper dispatch, S-002 and S-003 preference wipe, and D-001 through D-004 quoting. The quoting fix adds `Engine/Infrastructure/PowerShellText.Literal`, which passes text into PowerShell as a base64-decoded expression, so no quote character can end the literal.
  Verification: the solution builds clean, and every non-UI check passes except `PortablePublicationChecks`. That check fails the same way on the unmodified HEAD (`f9b3be2`, "Portable relaunch must hold verified image"), so it predates this review and is tracked separately.
- **KB-B, repair outcomes, done.** D-005 restarts every service whose stop was dispatched. D-006 writes a Finished record on the interrupted path. D-009 keeps a verified update Completed when its pending record will not delete. D-011 returns Failed on a read error. All non-UI checks pass except the pre-existing portable failure.
  E-004 is deferred. RecipeRunner lives in Engine.Core, which cannot see the tool exception types in Engine, and a self-check pins "a throwing action is journaled as dispatched and uncertain" as intended behavior. Fixing it means moving `ToolNotStartedException` and `ToolStartCanceledException` into Engine.Core and changing that contract, so it needs a decision first.
- **KB-C, engine primitives, done.** E-001 refuses a type mismatch the way `set-string` does. E-002 starts the stdout and stderr readers before writing stdin and tolerates a broken pipe. E-003 and E-005 stay as they are, for the reasons in the table.
- **KB-D, install and update, done.** S-007 uses `UnregisterRequired` and counts a failure as incomplete. S-009 treats an unreadable process named KillerBench.exe as running. S-004 and S-006 live in the shared `UpdateService.cs` and `AboutController.cs`, so they move to the family list. S-005 stays open: SelfUpdate is KillerBench-only, and removing it means repointing ReleaseUpdateChecks.
- **KB-E, harness hygiene, done.** C-001, C-002, C-003, C-005 and C-007 are fixed. C-004 needs nothing, and C-006 stays open as low.
- **KB-F, investigations, done.** D-007, D-008 and S-008 are fixed. D-010 is fixed: per-user file cleanups target the signed-in user, and the Outlook reset refuses under another account. Fixture checks cover the resolver, and a live preview on this machine resolved the right profile; the two-account case still needs a lab VM run.
- **Family.** The S-001 dispatch is ported to KillerPDF and KillerShot. Still to do: fix S-004 (require the `v` or keep the matched tag) and S-006 (show why the update hand-off failed) in KillerUI first, then roll them out to every app.
