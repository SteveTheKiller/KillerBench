# STARE implementation contract

When desktop draft inspection or CLI `task-plan --check-conflicts` observes a task at the selected path, it also requests that exact stored definition. Desktop review summarizes availability and retains the full XML in its exported inspection report; CLI JSON includes it directly. A failed follow-up read leaves the original conflict unresolved. No replacement is offered or performed.

`ScheduledTaskDefinition.ReadAsync` and CLI `task-definition TASK_FOLDER TASK_NAME` inspect one exact stored task definition through Task Scheduler. Collection is read-only, limited to 20 seconds and 1 MiB of XML. The result retains the exact provider path, raw evidence and XML; malformed, mismatched or incomplete observations do not establish task absence. XML parsing prohibits DTDs and requires the Task namespace. Registration read-back separately compares the observed definition with the exact requested semantics.

Source: `C:/Users/steve/Scripts/killer-scripts/STARE.ps1`, v1.1, inspected September 13, 2026. No scheduled tasks were created, changed or deleted during inspection.

| Source capability | Native workbench behavior to implement |
| --- | --- |
| List tasks outside the Microsoft task folder with state and next run | Bounded inventory with full task path and name, explicit coverage, state and nullable next-run data. Folder filtering is a display option, not an ownership check. |
| Create an executable action from a command line | Separate executable, arguments and working-directory fields. Resolve and preview the exact executable identity. Do not split quoted executable paths at the first space. |
| Browse for a PowerShell script | File picker and explicit host/options preview. Preserve execution policy unless an explicit supported option requires a change. Do not silently inherit the source's Bypass argument. |
| Daily trigger | Time picker with explicit local-time/time-zone behavior and a defined first occurrence. |
| Weekly trigger | Weekday selections plus time; reject empty or invalid selections before planning. |
| Monthly trigger | Individual month days or last-day selection plus time. Explain months without a selected date and validate leap-year and DST cases. Source uses all twelve months. |
| Startup trigger | Explicit startup selection and execution identity; keep separate from a scheduled clock time. |
| Register or replace task | Separate create and replace operations. Source uses SYSTEM/highest privileges and removes existing definitions before registration. Workbench must preview principal and privilege level, detect identity conflicts, retain an existing definition and verify the registered result. |
| Delete selected task | Identify by full path and name, reread the definition, retain a recovery copy and verify absence. Source deletion uses name only and suppresses errors; that does not prove success. |

## Shared options and results

Engine options must represent operation, full task identity, action, trigger and principal independently. The desktop and CLI must use the same validated plan. Inventory and preview are the starting modes. Changing any executable, target, trigger or identity invalidates a previous executable plan.

Task registration success proves registration only. It does not establish that the action can run successfully under its selected identity. Running a task is a separate operation with separate authorization and bounded observation. An unknown task result must remain unknown.

## Delivery queue

Engine, `task-inventory` CLI and Maintenance's Scheduled tasks workflow collect bounded task identities, states and reported user/group principals. A native read-only `workflow tasks --json` run saved 205 entries, including 76 group principals, with retained evidence and no repairs. This does not verify inaccessible tasks, effective permissions, actions, triggers or execution success. CLI registration is limited to a disabled, create-only Local System definition and remains pending disposable-machine validation.

1. Read-only inventory and exact-identity review in Engine, CLI and Maintenance.
2. Deterministic schedule/action plans and matching desktop controls.
3. Guarded create-only registration with fresh identity checks, durable no-repeat evidence and exact read-back.
4. Replacement and deletion providers with recovery copies, rollback and verification.
5. Disposable-machine validation for task paths, duplicate names, denied access, SYSTEM/user identity, time zones, DST and interrupted operations.

The Engine validates schedule selections, previews dates and resolves unique, ambiguous or invalid local times. CLI `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]` returns five date previews with candidate instants. Exit 3 indicates ambiguous or invalid local times. Desktop schedule controls, scheduler execution policy and registration remain pending. Preview does not create or execute tasks.

`ScheduledTaskPlan` combines exact task identity, separate executable/arguments/working directory, a copied validated schedule, explicit interactive-user or Local System principal, privilege selection and a bounded execution limit. This is a review draft, not permission to register. Account resolution, file identity/trust, time-zone binding, fresh conflict checks and registration verification remain required before execution.

CLI `task-plan PLAN.json` reads at most 128 KiB within 15 seconds and returns the validated draft. Exit 0 means draft validation only; invalid input returns 2, file access failures 1 and a read timeout 130. No task is registered. The shared Engine parser rejects unknown fields, duplicates and missing required constructor values.

Example draft (the account and executable are placeholders):

```json
{
  "identity": { "path": "\\KillerBench\\", "name": "Example" },
  "action": { "executable": "C:\\Tools\\example.exe", "arguments": "--check" },
  "schedule": { "kind": "Daily", "time": "09:00:00" },
  "timeZoneId": "UTC",
  "principal": { "kind": "InteractiveUser", "userId": "DOMAIN\\user", "highestPrivileges": false },
  "executionLimit": "01:00:00"
}
```

Calendar drafts require an explicit timeZoneId available on the reviewing machine. Startup drafts omit it. The form exposes installed zones and saves the selected ID; older calendar drafts without a zone must be updated. DST occurrence handling and scheduler registration semantics still require verification before execution.

Desktop and CLI draft reviews show five date previews beginning with the current date in the selected zone. Today's time may already have passed. Invalid local times are marked explicitly; repeated times show both possible offsets. These are calendar previews, not registered next-run times. The Engine formatter accepts an explicit reference instant for deterministic callers and tests.

Inventory includes reported action and trigger class names. Native collection distinguishes empty trigger collections from malformed evidence. Class names do not establish action contents, trigger settings or effective permissions. The desktop inventory workflow is integrated; native desktop rendering and interaction validation remain outstanding.

Maintenance > Scheduled tasks includes Review saved task plan. It uses the same bounded Engine file reader as the CLI and displays the validated draft in the console. Run diagnosis continues to collect inventory. A collapsible New task draft form supports executable options, account/privilege selection, execution limits and all four schedule kinds, including selected weekdays, month days or last day. Form review validates options only; it does not collect conflicts or register. Save new draft validates current form values and writes a bounded JSON file using create-new semantics. Existing files are preserved. Task creation, replacement, deletion and native file-picker interaction checks remain pending; automated WPF form interaction checks cover schedules, privileges and invalid input.

Desktop draft review also collects visible tasks and checks for a case-insensitive full-path conflict. An observed match remains a conflict even in partial inventory. Incomplete evidence reports unknown availability. No observed match means only that the task was not visible, never verified absence or permission to register. CLI draft validation remains offline by default; `task-plan PLAN.json --check-conflicts` enables this read-only inventory check, retains provider evidence and returns 3 for ExistingTask or Unknown. Ctrl+C cancels collection. Exit 0 still does not authorize registration.

Saved-plan desktop review inspects the action file through ScheduledTaskExecutable, reporting a SHA-256 content snapshot and byte count for nonempty readable files up to 64 MiB. Reads honor cancellation and a 15-second cancellation deadline. Missing, unreadable, oversized or length-changing files produce no content identity. PE header inspection reports the machine field and DLL flag when available; arbitrary readable content can still have a hash without a PE header. Complete image validity, runtime compatibility, signature, account permissions and path stability are not verified; the inspected file is never executed. These observations do not authorize registration.

CLI `task-plan PLAN.json --inspect-executable` requests the same file inspection and includes its structured result. It can be combined with `--check-conflicts` in either order. Missing content identity returns 3; duplicate or unknown options return 2. Exit 0 remains draft review only, not executable trust or registration eligibility.

The form's Inspect draft on this PC validates current options and runs the same executable/conflict inspection as saved drafts, with progress and Stop support. Review draft remains an offline options preview. No draft path performs registration or execution.

Desktop inspection resolves the selected account to a SID with a 20-second provider limit. CLI enables this through `--resolve-account`, returning 3 when unresolved and retaining raw provider evidence. Translation does not prove account type, enabled state, logon rights or executable permissions. Native Local System SID resolution was verified without task changes.

Export report on a task-inspection source writes a schema-versioned inspection report containing the draft, timestamp, executable snapshot, account resolution, conflict result and provider evidence. The report explicitly records that no task was registered. Native export-dialog verification remains pending.

Desktop task inspection also collects Authenticode status, signer subject and certificate thumbprint with a 20-second provider bound. Reports retain this optional observation and raw evidence. CLI `task-plan PLAN.json --inspect-signature` requests the same check and returns review exit 3 unless Windows reports Valid. A Valid observation is not proof of executable safety or registration eligibility; the file may change after inspection, and the signature observation is not bound to the separately collected content hash. Malformed, mismatched-path, incomplete or failed evidence remains unavailable. No file is executed or task registered.

Drafts include an overlap selection: SkipNew (default), Queue, Parallel or StopExisting. The form exposes the choice and the review describes concurrency or interruption implications. This records intended policy only; no running task is stopped and no scheduler setting is changed.

TaskRegistration coordinates a create-only host: preview by default, fresh exact-target eligibility, one creation attempt and matching definition read-back. It reports deterministic live stages for initial eligibility, durable preparation, final eligibility, create-only dispatch, exact read-back and verified evidence persistence. A failed progress observer cannot interrupt registration. Execution requires a recovery journal that prepares a pending record before creation, then repeats exact-target eligibility immediately before entering the creation provider. Changed, unavailable, timed-out or canceled final evidence sends no creation request, archives prepared and no-dispatch records, and permits a later explicit attempt. Missing or failed preparation blocks creation; failed completion remains Unverified. Incomplete creation or verification returns Unverified without retry or deletion. Interrupted creation may have left a task and must be inspected. Strict semantic XML read-back accepts Scheduler normalization while rejecting changed requested fields and added actions, triggers or principals. Tests cover the exact progress sequence, journal failures, final eligibility changes, cancellation, later explicit attempts and semantic read-back with substituted providers and in-memory Scheduler definitions.

`task-register PLAN.json YYYY-MM-DD [--execute]` and the desktop Scheduled tasks draft panel expose the guarded Windows host. Preview is the default and performs no eligibility probe, journal write or Scheduler call. CLI stage updates use stderr while JSON remains on stdout; desktop stages appear in the main evidence display. Desktop creation requires a separate confirmation and reports whether creation was attempted. Execution requires elevation, an existing task folder, an absent exact target, the explicit Local System SID with highest privileges, a complete stable PE SHA-256 identity and Valid Authenticode status. The host repeats those checks after durable preparation, uses Task Scheduler create-only semantics, registers the definition disabled, and accepts success only after exact path, disabled state and semantic definition read-back agree. It never creates folders, enables, runs, replaces, deletes or retries a task. The stable journal directory is under LocalApplicationData and cannot be overridden from the desktop or command line. Native mutation and rendered desktop behavior remain unverified and must be tested only in a disposable VM.

FileTaskRegistrationJournal writes immutable pending, verified and no-dispatch records with create-new semantics and flushes them to disk. The target key is case-insensitive; existing pending records, including partial writes, block another attempt after reopening. New schema-2 pending records retain the exact calendar start date used for the requested XML, while schema-1 records remain readable but cannot support exact reconciliation. A completed registration still requires explicit review before reusing a target. A no-dispatch attempt receives unique prepared and result archives while freeing the target for a later explicit attempt. Callers must supply a stable, access-controlled local directory for the machine; choosing a different directory bypasses this directory-scoped guard. Records include action arguments and must be treated as private evidence. Filesystem durability under power loss and native task registration behavior remain unverified.

`TaskRegistrationRecovery.RecoverAsync`, desktop **Check interrupted registration** and CLI `task-register-recover PLAN.json` inspect one exact current task definition without repeating creation. Recovery requires a readable schema-2 record from this machine, the same complete task plan and a semantic XML match using the retained start date. It writes a create-new result containing hashes of the pending, requested and observed definitions before archiving the pending guard. A surviving result can finish archival after interruption. Missing, inaccessible, changed, malformed, foreign-machine, old-schema or plan-conflicting evidence retains the guard. Recovery never creates, enables, runs, replaces, deletes or retries a task. Substituted fixtures cover exact match, changed definition, unavailable evidence, old schema, plan conflict, cancellation, observer failure and interrupted archival. Native Scheduler behavior and rendered interaction remain disposable-VM work.

TaskJournalReview reads individual records with a 1 MiB size limit, bounded nesting and cancellation. It validates recorded plans and evidence, rejects duplicate fields, and returns Pending, DefinitionVerified, NotDispatched or Unreadable with historical evidence retained for valid records. Linked review validates the attempt, machine and chronology for no-dispatch evidence as well as verified definitions. It does not establish current scheduler state, clear retry guards or authorize another attempt. The Scheduled tasks panel exposes Review recovery record, displays historical evidence in the console and supports Stop. Native file-picker interaction validation remains pending.

New journals carry an attempt ID and machine name in both records. Engine ReadPairAsync requires matching attempt and machine, identical intended/observed definitions and ordered timestamps. Older records without linkage remain individually readable but cannot verify as a pair. Pair review is historical consistency checking, not authentication or current scheduler inspection. Desktop Review recovery records accepts one record or a pair in either selection order; native picker interaction remains unverified.

CLI `task-journal RECORD.json [--completion COMPLETION.json]` exposes historical review as JSON with retryAuthorized always false. Exit codes are 0 for a recorded verified definition or linked no-dispatch outcome, 3 for pending or an unverified pair, 1 for an unreadable single record, 2 for invalid usage and 130 for cancellation or timeout. Exit 0 does not establish current task health, execution or retry eligibility. Native CLI checks cover matching and mismatched pairs, missing completion, single records and usage outcomes without scheduler changes.
