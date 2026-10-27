# Repairs and safety

KillerBench repairs are typed operations with eligibility, execution, verification, and recovery rules. They are not arbitrary command strings selected from a log line.

## Repair sequence

1. Collect complete current evidence.
2. Match one supported recipe.
3. Review required options and consequences.
4. Confirm administrative and policy prerequisites.
5. Recollect the decisive evidence immediately before mutation.
6. Write a durable preparation record.
7. Run one bounded native action.
8. Collect fresh verification evidence.
9. Record a verified, blocked, stopped, or uncertain outcome.

If the decisive evidence changes before execution, the Engine stops without performing the action.

## Explicit choices

Option-dependent repairs require exact choices from the host. Examples include a selected volume, printer queue, recovery-key destination, power values, package version, or repair source. The generic automatic runner cannot invent these values.

## Managed systems

Group Policy, device management, and other authority evidence can make a local repair ineffective or inappropriate. Supported coordinators inspect applicable authority state and fail closed when ownership is unknown or managed.

## Durable pending records

A process can stop after Windows receives a request but before verification completes. In that case the Engine retains a pending record. A later reconciliation reads fresh state and determines what happened. Reconciliation never repeats the original mutation automatically.

## Automatic repair

`autoRepair` means the host allows eligible recipes to proceed. It does not bypass evidence requirements, choose missing options, ignore policy, retry an uncertain action, restart Windows, or turn an `Attention` finding into a repair.

The full system scan remains diagnostic-only even when a focused workflow allows automatic repair. Saved captures, imported evidence, and historical events do not authorize native dispatch.

## Host responsibility

Before enabling a repair, a host should show:

- The exact target
- The proposed change
- Required elevation or restart
- Data and settings that remain untouched
- The evidence that made the action eligible
- The journal and recovery state after execution

Use disposable virtual machines to validate native mutations across Windows versions and failure states before exposing them to production users.
