# Architecture

The KillerBench.Engine is a UI-free project inside the KillerBench repository. The desktop, CLI, and tests reference it as separate hosts.

## Boundary

The Engine owns:

- Bounded Windows evidence collection
- Deterministic interpretation and correlation
- Workflow and recipe catalogs
- Repair eligibility and execution coordination
- Durable journals and no-repeat reconciliation
- Report models and knowledge-pack readers

The host owns:

- Navigation, dialogs, controls, and presentation
- User choices and consent
- UAC relaunch behavior
- Progress and cancellation controls
- File pickers and report destinations
- Product updates and deployment

## Project relationships

```text
KillerBench desktop
        |
        v
KillerBench.Engine <---- killerbench-cli
        ^
        |
KillerBench.Checks
```

The Engine does not reference WPF or desktop controls. Tests can substitute provider and repair hosts so parsers, eligibility, safety gates, interruption behavior, and recovery can be verified without changing the development computer.

## Internal layers

| Layer | Responsibility |
| --- | --- |
| Capability collectors | Read bounded Windows state for one exact scope |
| Evidence parsers | Validate shape, identity, completeness, and limits |
| Diagnostic interpretation | Convert evidence into explicit workflow states |
| Workflow orchestration | Combine checks, progress, recipes, and journals |
| Repair coordinators | Recheck eligibility, prepare, mutate, verify, and reconcile |
| Reports | Present problems, actions, results, and limitations |
| Knowledge packs | Resolve versioned external reference data without changing logic |

## Windows and shell modes

The current native provider layer targets Windows and uses .NET, Windows APIs, PowerShell, and Windows command-line tools where appropriate. The Engine is not cross-platform today.

A future Bash provider would be a separate host implementation behind explicit interfaces. It would need its own capability catalog, evidence contracts, safety rules, and tests. Translating PowerShell command strings into Bash would not create equivalent diagnostics.

## Failure model

Collectors fail closed when identity is ambiguous, output is malformed, a limit is exceeded, or required authority cannot be inspected. Repair coordinators separate preview, preparation, dispatch, verification, and reconciliation so an interrupted process does not automatically repeat a machine change.
