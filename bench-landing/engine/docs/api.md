# API guide

Public types use capability namespaces such as `KillerBench.Engine.Workflows`, `KillerBench.Engine.Infrastructure` and `KillerBench.Engine.Validation`. Windows providers use `KillerBench.Engine.Windows` and its subnamespaces. Reference the provider project and call `WindowsEngine.Register()` before running native workflows; see [Getting started](getting-started.html).

## High-level entry points

| Task | Start with |
| --- | --- |
| Run one diagnostic area | `Workflows.RunAsync` and `WorkflowKind` |
| Run common diagnostics | `FullSystemScan.RunAsync` |
| Show available common areas | `CommonDiagnosticCatalog.All` |
| Show repair recipes | `RecipeCatalog.ForWorkflow` |
| Build workflow controls | `WorkflowOptionCatalog.All` or `WorkflowOptionCatalog.ForWorkflow` |
| Observe servicing activity | `ServicingActivityMonitor.ObserveAsync` |
| Interpret a completed workflow | `WorkflowResult` and `WorkflowRecoveryPlan.From` |
| Review a saved journal | `WorkflowJournalReview` |
| Generate reports | Engine report and export types |

## Core result types

| Type | Purpose |
| --- | --- |
| `WorkflowCheck` | One interpreted diagnostic result with state, detail, evidence, and optional repair |
| `WorkflowResult` | Complete workflow output plus journal location |
| `ToolResult` | Bounded native-provider result including timeout and truncation state |
| `RepairRecipe` | Eligibility and execution metadata for one supported repair |
| `RecipeResult` | Outcome of evaluating or executing a recipe |
| `RepairResult` | Native repair steps, verification, and durable outcome |
| `WorkflowOptionSet` | Host-neutral controls, defaults and execution boundary for one option-rich workflow |
| `WorkflowOptionDefinition` | A toggle, slider, choice or text control with validation metadata and dependencies |
| `WorkflowOptionValidationResult` | Normalized values, validation errors and whether an explicit machine-changing control was requested |
| `WorkflowOptionProjection` | Converts a valid read-only option plan into Engine workflow or servicing-monitor options |

## Workflow controls

`WorkflowOptionCatalog` lets a desktop, CLI or embedded host discover the same supported controls without copying UI definitions. Each option has a stable key, label, control type, default value and explanation. Sliders include their minimum, maximum and step. Choices expose exact values. Conditional controls identify the option and value that enables them.

`ChangesMachine` is true only for an implemented explicit execution control. These controls are advanced and off by default. The flag describes the control surface; it does not bypass fresh evidence, eligibility, journaling, verification or recovery requirements.

Pass submitted key/value pairs to `WorkflowOptionValidation.Validate`. The validator fills omitted defaults, normalizes booleans and integers, and rejects unknown or duplicate keys, invalid choices, out-of-range or off-step sliders, oversized text, missing required values and conditional controls that are not active. A valid machine-changing request still authorizes nothing by itself. The operation-specific coordinator must collect fresh evidence and enforce its own execution boundary.

After validation, `WorkflowOptionProjection.CreateHost` projects safe values into a `WindowsWorkflowHost`. It also adds a redacted `Selected workflow options` result so the normalized nondefault plan remains visible in reports and journals. Prepared System Restore settings drive the creation-readiness review. Selected filesystem repair settings drive exact-volume state collection, bounded read-only diagnosis and consequence review without authorizing or scheduling repair. Selected storage optimization settings drive exact-volume analysis. Selected printing targets correlate one exact queue and optional job with current bounded evidence without authorizing a queue or job change. A selected MSI product receives one exact repair review that combines current registration, Installer activity, restart state and administrator access while leaving execution with the guarded MSI coordinator. A selected Windows Time source is reviewed against the current role, policy ownership, local mode and peer configuration. A selected Event Log channel is reviewed against exact current configuration and policy ownership. Selected Firewall profiles are reviewed against complete ActiveStore state and policy ownership. A proposed computer name is reviewed against active and configured identity, role, restart state and administrator access. These reviews expose no repair identity. `CreateServicingOptions` projects the servicing monitor controls into `ServicingMonitorOptions`. Both reject invalid plans and every plan that requests a machine change. This gives CLI and embedded hosts one shared read-only execution path while leaving repair authorization with the operation-specific coordinators.

The shared catalog currently includes desktop controls for Network, Audio, System Restore, Storage, Printing, Windows Installer, Windows Time, Event Log, Profiles, Computer Name, Background Transfers, App Packages, Installed Applications, Search, Component Cleanup, Windows Recovery, Firewall Services and BitLocker. Printing distinguishes queue repair, default selection and one blocked-job cancellation. Installer accepts only an optional normalized MSI product code. Time validates existing, domain and custom-peer choices. Event Log limits enablement to the four inspected diagnostic channels. Profile controls define the inactivity threshold and optional dependency review without authorizing profile changes. Computer Name requires one valid exact proposed name and keeps rename execution explicit. Background Transfers exposes running-account and administrator-only all-users inventory scopes without authorizing a job change. Package and application controls expose only the scopes implemented by their read-only collectors. Search rebuild, component cleanup and Windows Recovery enablement remain off by default and require their existing guarded coordinators. Firewall execution requires one or more exact profile selections. BitLocker validates mutually exclusive backup, encryption and protection-resume actions, including exact protector and recovery-file prerequisites. These definitions preserve each workflow's read-only default and represent execution as a separate advanced control.

## Capability folders

| Folder | Responsibility |
| --- | --- |
| `Applications` | Installed applications, packages, updates, and deployment evidence |
| `Audio`, `Devices`, `Storage` | Hardware and device diagnostics |
| `BitLocker`, `Security`, `Backup` | Protection, recovery, and security state |
| `Network`, `Printing`, `Time` | Connectivity and peripheral workflows |
| `Installation`, `Updates`, `Services` | Servicing, setup, event, and service diagnostics |
| `Profiles`, `FileSystem`, `Cleanup`, `Debloat` | User environment and maintenance planning |
| `Evidence`, `Knowledge` | Imported evidence, catalogs, and deterministic interpretation |
| `Workflows`, `Automation` | High-level orchestration and script-derived planning surfaces |
| `Infrastructure` | Bounded tools, persistence, recovery, and shared provider helpers |

## Cancellation and progress

Long-running public operations accept `CancellationToken`. Workflow and monitor entry points also accept `IProgress<string>`. A host should cancel through the token and keep the final or pending journal. It should not terminate the process merely to stop an operation that has a supported cancellation path.

## Lower-level collectors

Capability-specific collectors are public when another host can use them safely without running a complete workflow. Their result records describe the exact data retained and whether collection was complete. Consult the source and the detailed capability catalog for the current surface.

## Versioning

The Engine currently shares KillerBench version `0.1.0`. It has not reached a stable public package contract. Pin the exact repository revision used by an embedding application.
