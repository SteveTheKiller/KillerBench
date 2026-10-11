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
| Packaging | Framework-dependent; needs .NET Desktop Runtime 10 for Windows x64 |
| Appearance | 13 family themes and 19 languages |

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

KillerBench can also read saved Windows logs, such as CBS and Setup logs, without running anything on the machine. Findings link to the exact line that produced them.

## Knowledge packs

Large and frequently updated reference data lives in knowledge packs instead of the diagnostic logic: Windows and Setup error codes, vendor and device identifiers, PCI, USB, ACPI and UEFI references, network OUI vendor data, driver and support locations, and curated support links.

Each pack identifies its format, schema version, source, source revision, generation time and content hash, and KillerBench validates them before use. An unknown or invalid pack stays unavailable instead of falling back to a guess. An update downloads to a temporary location, is validated, then replaces the active pack in one step, so a failed update leaves the previous pack in place.

Knowledge data can explain an identifier or suggest where to look next. It cannot turn a historical code into a current fault, choose a driver automatically, or authorize a repair.

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

Actions you select can contact their destination, such as Windows Update, software download services, or recovery-key backup to Active Directory or Entra ID. Knowledge updates and the KillerMCP installer can also contact their configured services. An AI client using [KillerMCP](../mcp.html) may receive the reference result it asked for.

Raw logs, imported files and exported evidence can contain computer names, accounts, paths or sensitive source text. Review them before sharing, and never include recovery keys, credentials or customer data in a public report.

## Development status

KillerBench is in development and there is no public download yet. Repair coverage is still expanding, and native repair validation is still in progress. A successful build or simulated check does not establish native repair behavior. Disposable Windows virtual machines are used to validate each native change across Windows versions and failure states. See [How changes stay safe](safety.html) for the full safety model.
