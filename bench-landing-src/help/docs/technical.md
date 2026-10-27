# The tech of KillerBench

A look under the hood at how KillerBench reads a Windows computer, keeps its evidence honest, and decides when a change is allowed.

## Tech stack

KillerBench is a native Windows app. The desktop app, the command line and the Engine underneath them share the same diagnostic evidence and repair workflows.

| Component | Detail |
| --- | --- |
| UI | WPF on .NET 10 (`net10.0-windows`), Windows x64, custom window chrome |
| Engine | `Engine.Core`, `Engine` and `Engine.Windows`, with no reference to WPF or desktop controls |
| Command line | `killerbench-cli`, a separate host on the same Engine |
| Windows providers | .NET, Windows APIs, PowerShell and Windows command-line tools, each run behind fixed time, record and size bounds |
| Reports | Raw output, Readable report, HTML, text and JSON |
| Reference data | Versioned knowledge packs for error codes, hardware identifiers, vendors and support references |
| Packaging | Installer: framework-dependent, needs .NET Desktop Runtime 10 for Windows x64. Portable: self-contained, no runtime install |
| Appearance | 13 family themes and 20 languages |

## Install, folders and data

KillerBench ships in two forms. The installer is the standard download and supports a per-user or an all-users installation. The portable edition carries its own .NET runtime and runs without installation.

KillerBench runs with the rights of the account that started it. It does not elevate itself. Start it with **Run as administrator** when protected evidence, such as BitLocker protectors, or a repair needs administrator access. Elevation never selects or approves a repair.

### What the installer does

The installer verifies and extracts the application to `%LOCALAPPDATA%\Programs\KillerBench\`, or to `%ProgramFiles%\KillerBench\` after an elevation prompt for all users. It adds a Start menu shortcut, an optional desktop shortcut and an Add or Remove Programs entry with a normal and a quiet uninstall. The portable edition verifies and extracts its payload under `%LOCALAPPDATA%\KillerBench\Portable\` for the current run and sweeps abandoned copies on the next start.

### Where things live

| Location | Holds |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Theme, accent, interface scale and language |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | One JSON journal per workflow run: decisions, bounded command output and repair results |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | Execution journals for servicing repairs such as DISM and SFC |
| Feature folders under `%LOCALAPPDATA%\KillerBench\` | Pending guards and outcomes for individual guarded actions, for example `BitLockerBackup`, `Time`, `Firewall` and `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | The KillerMCP repair level chosen in About |
| `HKCU` or `HKLM\Software\KillerBench` | The installed path, for the matching installation scope |
| A `.kbench` file you choose | A saved capture with its retained evidence |

Journals stay on this computer, and KillerBench does not upload them. Delete a journal only after you no longer need the evidence for an uncertain outcome.

## Diagnose first

KillerBench treats diagnosis and change as different jobs. Diagnosis only reads. A change happens only when you pick a specific repair or action, review its options and prerequisites, and confirm it.

The full system scan is diagnostic-only, including when a focused workflow has automatic repair enabled. Saved captures, imported logs and historical events never authorize a repair.

### Bounded collection

Every native command and Windows provider runs behind fixed time, record and size bounds. A provider records its exit status, timeout, truncation, standard output and error output. A failed or truncated provider can never be read as a clean, empty answer.

### Minimal retention

Collectors keep what a finding needs. Many results keep names, categories, counts, states and timestamps while leaving out credentials, command lines, full paths, account identities, network endpoints and unrelated event payloads.

## Evidence you can follow

Every finding is tied to the evidence that produced it, and the report always separates what was proven from what was only observed.

| State | Meaning |
| --- | --- |
| Needs review | The evidence supports a problem that deserves attention. |
| Information | Evidence was collected and is shown for context. |
| Incomplete or Unknown | A check could not finish or the evidence was unavailable. It is never counted as healthy. |
| Historical | An event from the past. It explains history but does not prove a current fault. |
| Verified | A repair or change was confirmed by reading the result back. |

### Unknown is a result

Unknown means access was denied, a source was missing, a provider failed, output was malformed, a bound was exceeded, or the evidence disagreed. KillerBench shows that limitation and the next useful step instead of hiding it.

### History stays history

Event records and imported logs describe what was recorded at a past time. They can point to an error code or the next diagnostic scope. They do not prove that the fault is still active.

## Guarded repairs

Repairs are typed operations with eligibility, execution, verification and recovery rules. They are not command strings picked out of a log line. Every supported repair follows the same sequence:

1. Collect complete current evidence.
2. Match one supported recipe.
3. Review required options and consequences.
4. Confirm administrative and policy prerequisites.
5. Recollect the decisive evidence immediately before the change.
6. Write a durable preparation record.
7. Run one bounded native action.
8. Collect fresh verification evidence.
9. Record a verified, blocked, stopped or uncertain outcome.

If the decisive evidence changes before the action runs, KillerBench stops without performing it.

### Explicit choices

Repairs that depend on an option need an exact choice from you, such as a volume, printer queue, recovery-key destination, power value, package version or repair source. KillerBench never invents those values.

### Managed computers

Group Policy and device management can make a local change ineffective or inappropriate. Supported repairs inspect who owns a setting and stop when that ownership is managed or unknown. Settings that a policy owns are left alone.

### Automatic repair

Automatic repair only lets eligible recipes proceed. It does not bypass evidence, choose missing options, ignore policy, retry an uncertain action, restart Windows, or turn a finding that needs review into a repair.

## Journals and interruptions

Before KillerBench asks Windows to make a change, it writes down what it is about to do. Each change is saved in a journal under `%LOCALAPPDATA%\KillerBench`.

After the change, KillerBench reads the result back and reports **Verified** only when Windows shows the expected state. If Windows reports success but the state did not change, the result says so and nothing is repeated.

A process can stop after Windows receives a request but before verification finishes. The pending record stays, and a later reconciliation reads fresh state to work out what happened. Reconciliation is not an automatic rollback, and it never repeats the original change. Keep the journal when an outcome is uncertain.

## Constants and limits

| Specification | Value |
| --- | --- |
| Diagnostic coverage | Full system scan runs 49 diagnostic areas and checks 96 guarded repair recipes |
| Text source read | 1,000,000 characters per source, 2,000 matching lines shown |
| Session sources | 12 in the sidebar; a capture holds up to 24 sources and 64 MB |
| Event queries | Newest 200 events per channel from the last 7 days |
| Windows Update scan | 15 seconds per source, two minutes overall |
| Log investigation | 10,000 parsed entries, 256 distinct codes, 8 excerpts of 320 characters per code |
| Compressed CBS archive | 256 MiB archive, 512 MiB log member, 30 second extraction |
| Read-only CHKDSK | Ten minutes; a repair is bounded to 65 minutes |
| DNS and TCP checks | Ten seconds by default, one minute at most |
| Service inventory | Up to 1,024 installed services |
| Knowledge packs | Up to 32 open at once |
| Large file review | Files of at least 100 MB |
| Servicing activity monitor | 1 to 60 second interval, 1 to 120 snapshots |
| Scheduled task drafts | Execution limit of 1 to 4,320 minutes |
| Custom time sources | Up to eight NTP peers |
| Activity export | Up to 150 retained entries |
| Languages and themes | 20 languages, 13 themes with accent variants |

## What it will not do

- Clear a TPM, delete protectors it did not create, or display recovery passwords.
- Change a setting that group policy or device management controls.
- Run a repair just because a historical event or imported report mentions a problem.
- Turn missing, partial or failed evidence into a healthy verdict.

## Reports

Every run produces a report that you can read, filter and export. A report separates:

1. What was inspected
2. What problem was found
3. What evidence supports the finding
4. What action was proposed or performed
5. How the result was verified
6. What remains unknown or needs a restart

**Raw output** keeps the technical evidence with its original line numbers so you can trace any finding to its source. The **Readable report** summarizes the same run in plain language, with the next safe step first. HTML reports use the Killer Tools theme system and stay readable without the app. Text favors portability, and JSON keeps structured fields for another program.

## Saved logs and captures

KillerBench can investigate evidence without running anything on the machine. Drop a text log on the window, or paste its path and choose **Open log**. CBS, CSI and Setup lines are parsed with their timestamps, and findings link to the exact original line. EVTX and event XML files can be imported the same way.

**Scan Windows Update** is an on-demand, read-only collection of CBS, DISM, Panther and rollback Setup logs, update and servicing event channels, update service states, free space, uptime, pending restart indicators and selected update policy. **Save capture** writes the retained evidence to a `.kbench` file. **Open capture** reopens it without the original files and evaluates it again with the installed rules, so the same rules give the same findings.

## Demo mode

Start `KillerBench.exe --demo` to try **Run diagnosis** on a fabricated workstation. Every workflow produces synthetic evidence and timed progress. No Windows provider is called, no repair command runs and no journal is written. Separate tool buttons outside **Run diagnosis** stay live, so do not use them as demo actions.

`KillerBench.exe --demo-screenshot` opens a view-only Full system scan report for a fixed fictional machine. Clicks in the workspace, shortcuts and file drops are blocked.

## Knowledge packs

Large and frequently updated reference data lives in knowledge packs instead of the diagnostic logic: Windows and Setup error codes, vendor and device identifiers, PCI, USB, ACPI and UEFI references, network OUI vendor data, driver and support locations, and curated support links.

Each pack identifies its format, schema version, source, source revision, generation time and content hash, and KillerBench validates them before use. An unknown or invalid pack stays unavailable instead of falling back to a guess. An update downloads to a temporary location, is validated, then replaces the active pack in one step, so a failed update leaves the previous pack in place.

Knowledge data can explain an identifier or suggest where to look next. It cannot turn a historical code into a current fault, choose a driver automatically, or authorize a repair.

## Updates and signing

At startup KillerBench asks GitHub for the latest stable release, with a four second limit, and offers an update only when that release is newer. You can turn the check off with **Check for updates on startup** in About, or in the update prompt. The download is the package that matches the running copy, installer or portable, and its SHA-256 must match the release's `SHA256SUMS.txt` before anything installs. The app closes and reopens itself to finish. There is no background service.

The About card shows the version, the code-signing publisher and thumbprint, and the SHA-256 of the running executable, so you can check the copy you have.

## KillerMCP

KillerBench does not include an AI agent. [KillerMCP](../mcp.html) can connect one to the same Engine. The **KillerMCP Options** in About decide what that agent may do:

| Setting | Effect |
| --- | --- |
| Off (default) | The agent cannot request a repair. |
| Allow safe repairs | The agent can request repairs that are classified as safe. |
| Also allow disruptive repairs | Disruptive repairs become available, and each still needs approval. |

Only a person at this computer can raise the level. After an uncertain repair result, KillerMCP writes a lock, and agent repairs stay blocked until someone clears it in About.

## Localization

The interface is available in 20 languages: English, Bengali, Czech, German, Spanish, French, Hungarian, Italian, Japanese, Kazakh, Norwegian, Polish, Portuguese (Brazil), Russian, Slovenian, Turkish, Ukrainian, Vietnamese, and Simplified and Traditional Chinese. A language change applies immediately, including the section and workflow names, the welcome text and an open readable report. Any missing text falls back to English.

Evidence, Engine findings, journals and Raw output stay in English, the same as a Windows log, so a report reads the same on every machine and can be searched for exact text.

## Command line and exit codes

`killerbench-cli` runs the same Engine without the desktop app. Progress goes to standard error and JSON to standard output. Most commands preview by default, and a change needs `--execute`. Matching `-recover` commands review an interrupted action without repeating it. See [Run KillerBench from scripts](use-the-cli.html) and the [command reference](cli.html).

| Exit code | Workflow commands | Scan and open |
| --- | --- | --- |
| 0 | No unresolved issue in the checked scopes | Completed coverage |
| 1 | Startup or journal error | Failure |
| 2 | Invalid arguments | Invalid arguments |
| 3 | Attention required | Partial collection |
| 4 | Restart required before verification | |
| 130 | Stopped | Canceled or overall timeout |

No exit code certifies that a computer is healthy.

## Architecture

The Engine is split into three UI-free projects. `Engine.Core` supplies primitives and declarative recipes, `Engine` owns shared workflows and evidence, and `Engine.Windows` registers the native providers. The desktop app, the CLI and the tests are separate hosts.

| Layer | Responsibility |
| --- | --- |
| Capability collectors | Read bounded Windows state for one exact scope |
| Evidence parsers | Validate shape, identity, completeness and limits |
| Diagnostic interpretation | Convert evidence into explicit workflow states |
| Workflow orchestration | Combine checks, progress, recipes and journals |
| Repair coordinators | Recheck eligibility, prepare, change, verify and reconcile |
| Reports | Present problems, actions, results and limitations |
| Knowledge packs | Resolve versioned reference data without changing logic |

Collectors fail closed when identity is ambiguous, output is malformed, a limit is exceeded, or required authority cannot be inspected. The Engine targets Windows today and is not cross-platform. The [Engine developer guide](../engine/index.html) covers the API, the CLI and embedding the Engine in another .NET host.

## Privacy and network

Diagnostics and log analysis run locally, without an AI service or a KillerBench account. Journals and retained evidence stay in the local account's `%LOCALAPPDATA%\KillerBench` folders.

Actions you select can contact their destination, such as Windows Update, software download services, or recovery-key backup to Active Directory or Entra ID. The startup update check contacts GitHub, and knowledge updates and the KillerMCP installer contact their configured services. An AI client using [KillerMCP](../mcp.html) may receive the reference result it asked for.

Raw logs, imported files and exported evidence can contain computer names, accounts, paths or sensitive source text. Review them before sharing, and never include recovery keys, credentials or customer data in a public report.

## Development status

KillerBench is in development and there is no public download yet. Repair coverage is still expanding, and native repair validation is still in progress. A successful build or simulated check does not establish native repair behavior. Disposable Windows virtual machines are used to validate each native change across Windows versions and failure states. See [How changes stay safe](safety.html) for the full safety model.

## Glossary

Plain-language definitions of the terms used on this page, in alphabetical order.

| Term | What it means |
| --- | --- |
| BITS | Background Intelligent Transfer Service, the Windows service that downloads updates and other files in the background. |
| Capture | A saved `.kbench` file holding retained evidence, so it can be reviewed again without the original computer. |
| CBS | Component-Based Servicing, the Windows subsystem that installs updates and features. Its log is `CBS.log`. |
| DISM | Deployment Image Servicing and Management, the Windows tool that checks and repairs the component store. |
| Elevation | Running with administrator rights after a Windows prompt. KillerBench never elevates itself. |
| Evidence | What KillerBench actually read from Windows, kept with its source so a finding can be traced back. |
| Fail closed | When evidence is ambiguous, missing or too large, the result is Unknown and no change is allowed. |
| Journal | A local JSON record written before a change and updated after it, so an interrupted action can be reviewed later. |
| Knowledge pack | A versioned, validated file of reference data such as error codes or hardware identifiers. |
| Panther | The folder where Windows Setup writes its upgrade logs. |
| Pending guard | The record that marks a change as possibly sent to Windows. It blocks a repeat until reconciliation reads the outcome. |
| Policy owner | Group Policy or device management that controls a setting. KillerBench leaves owned settings alone. |
| Recipe | A typed repair with its own eligibility, execution, verification and recovery rules. |
| Reconciliation | Reading fresh state after an interruption to learn what happened, without repeating the change. |
| SFC | System File Checker, the Windows tool that verifies and restores protected system files. |
| TPM | Trusted Platform Module, the security chip BitLocker uses to protect the drive key. |
| Verified | The result was read back from Windows and matched what the change was meant to do. |
| Workflow | One tool's run: the checks it collects, the recipes it may use and the report it produces. |
