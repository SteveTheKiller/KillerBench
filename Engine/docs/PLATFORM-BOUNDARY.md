# Platform boundary

The KillerBench.Engine is being split into a platform-neutral core and per-platform provider projects. This document records the boundary and the migration rules. Behavior does not change during the move.

## Projects

- `Engine/KillerBench.Engine.csproj` (core, `net10.0`): evidence records, findings, catalogs, planning, option validation, journals, recovery contracts, reports, parsers for saved evidence, and the host interfaces that providers implement. Core must not reference `Microsoft.Win32`, `System.ServiceProcess`, `System.Management`, `WindowsIdentity`, `SafeHandles` or P/Invoke. Launching a bounded process through `BoundedTool` is platform-neutral and stays in core; the Windows commands that core collectors currently pass to it are data, and phase B moves them into recipe packs.
- `Engine.Windows/KillerBench.Engine.Windows.csproj` (`net10.0`, Windows providers): every `Windows*Host`, native collector, native executor and Windows command construction. It references core and receives `InternalsVisibleTo` so shared bounded-tool helpers stay internal during the move.
- Future `Engine.Linux` and `Engine.MacOS` projects follow the same shape. None exists yet.

Both provider projects stay on `net10.0` rather than `net10.0-windows` so `killerbench-cli` keeps its current target framework.

## Identity scheme

`WorkflowKind` and `RepairKind` remain in core for now. They are catalog identifiers, not platform APIs, and 184 files carry about 2,400 references to them. Replacing them with string identifiers is deferred to phase B, where declarative recipe packs introduce string recipe IDs and the enums become a Windows-pack alias table. Phase A only moves compilation units.

## Provider registry

Core never constructs a platform host. `PlatformProviders` in core holds factories that a provider assembly registers at startup: `WindowsEngine.Register()` is called first thing in the desktop app, the CLI and the Checks runner. Core code that previously wrote `new WindowsWorkflowHost()` now calls `PlatformProviders.CreateWorkflowHost(progress)`; the same applies to `CreateServicingRepairCommandHost()`. An unregistered call throws `PlatformNotSupportedException` with the registration instruction. Callers that pass an explicit host are unaffected.

## Migration rules

1. Move one host or collector at a time with `git mv`, then build the solution and run the full Checks suite before the next move.
2. A moved file keeps its namespace `KillerBench.Engine` so callers do not change.
3. A core file that needs a provider gets an interface in core and a `Windows*` implementation in the provider project. Existing `I*Host` interfaces are the pattern.
4. Direct `System.Diagnostics.Process` use outside `NativeTool` is consolidated onto `BoundedTool` so one bounded launcher remains.
5. Parsers and models that mention Windows vocabulary but touch no Windows API stay in core so saved evidence can be analyzed on any platform.
6. Phase A is complete when core builds with no Windows API reference and every check passes unchanged.

## Inventory (2026-09-18)

519 Engine source files. 73 files use a true Windows API (52 `Microsoft.Win32` registry readers, 13 P/Invoke, 9 `WindowsIdentity`, plus `ServiceController` and `SafeHandles` users) and must move to the provider project or gain a provider interface. A further 150 or so files carry only an `OperatingSystem.IsWindows()` guard around a bounded Windows command; those stay in core for phase A. Movement so far: WindowsWorkflowHost, WindowsRepairHost, WorkflowOptionProjection and the task, BITS, update, MACE and servicing hosts.
