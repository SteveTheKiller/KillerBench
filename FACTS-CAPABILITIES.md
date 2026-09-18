# FACTS implementation contract

Source inspected in full: `C:/Users/steve/Scripts/killer-scripts/FACTS.ps1`, September 13, 2026. The synopsis labels it v2.3; the displayed version is v2.5. Neither label establishes a validated KillerBench capability.

## Actions to expose separately

| Source action | KillerBench selection and required evidence |
| --- | --- |
| Search both machine uninstall registry views for display names containing Foxit | Read-only installed-product inventory with registry view, exact key, name, version and publisher. Report inaccessible views as incomplete. A name match alone does not authorize changes. |
| Recursively find Updater keys below both Foxit Software machine hives | Read-only settings review by exact product/key. Bound traversal and retain inaccessible-path coverage. Show current value and registry type. |
| Set UpdateMode to string 0 and bNoUpdate to DWORD 1 | Separate update-policy proposal for selected product keys. Require documented product/version applicability, policy ownership, prior values and types, fresh identity checks, rollback data and post-write verification. Do not recursively apply to every matching key. |
| Stop and disable services whose names match Foxit and Update | Separate selected-service operation. Inspect executable identity, dependencies, current state and startup mode. Preserve managed configuration and verify both state and startup mode after any authorized change. |
| Stop and disable scheduled tasks matching Foxit and Update | Separate selected-task operation using full task path and name. Inspect actions, triggers, principal and current state. Retain original definition and verify the exact task. Name matching alone is insufficient. |
| Replace FACTS_Universal_Enforcement with an hourly SYSTEM task | Optional persistent enforcement, never implicit in inventory or update-setting changes. Review its complete definition, executable and scope. Detect an existing task and preserve its definition before replacement. Verify registered actions, principal and schedule. |
| Remove FACTS_Universal_Enforcement when Foxit is not detected | Explicit removal selection only after complete inventory and proof of task ownership. Incomplete discovery must not authorize task removal. Preserve the definition for recovery. |

## Result handling

The source suppresses some service/task errors and prints success without checking the resulting configuration. Its final exit code does not establish that all changes succeeded. KillerBench must retain an outcome for every selected target and verify each changed property independently. Partial completion remains partial and must not become a green overall result.

Default behavior is inventory and review. Disabling application updates is an administrative configuration choice, not evidence that an installation needs repair. Proposed changes must show their effect on future security updates and preserve existing organizational policy.

## Delivery and validation queue

1. Shared Engine inventory with explicit completeness and bounded collection, exposed through CLI and a Maintenance settings panel.
2. Exact product, registry, service and task selection with preview-only plans and persistent selections cleared when identities change.
3. Product/version policy documentation and ownership checks before implementing mutation providers.
4. Independent mutation, rollback and verification paths tested with substituted providers, including access denial, identity changes and interrupted operations.
5. Disposable-machine validation for supported Foxit versions, both registry views, existing managed settings and task restoration.

Machine product inventory is implemented in the Engine, the Maintenance desktop workflow and CLI. The native CLI workflow was exercised read-only with an empty inventory and its saved journal verified. Installed-product, denied-access and native desktop runs remain pending.

The Foxit workflow also collects UpdateMode and bNoUpdate values from Updater keys beneath both machine Foxit Software registry views. Reports retain exact paths, names, registry types and bounded string/integer values. Traversal limits are 256 keys per view and eight levels, with a 20-second observation deadline. Pending keys also fit within the per-view limit. Synchronous registry calls may finish after cancellation; no mutation is performed. Inaccessible paths, unsupported values and limits make coverage incomplete while preserving other observations. This does not establish effective policy or product/version ownership. Substituted traversal checks cover inaccessible views, missing keys, unsupported values, invalid paths, key/depth limits and cancellation. A native empty-hive workflow retained product/updater evidence. Native installed-product and denied-access validation remain pending. Every mutation operation above remains unimplemented.
