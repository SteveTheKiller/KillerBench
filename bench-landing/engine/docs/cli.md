# CLI guide

`killerbench-cli` exposes the Engine for technicians, scripts, test harnesses, and headless collection.

## Build

```powershell
dotnet build Cli\KillerBench.Cli.csproj
```

## Discover commands

```powershell
dotnet run --project Cli\KillerBench.Cli.csproj -- help
```

The help output is the authoritative command list for the current checkout. Commands include focused diagnostics, complete workflows, servicing activity, knowledge lookups, planning tools, report generation, and selected guarded repairs.

## Discover workflow controls

```powershell
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options servicing-monitor
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options servicing-monitor --set refreshSeconds=5 --set samples=4
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options network --set tcpHost=127.0.0.1 --set tcpPort=443 --set timeoutSeconds=15
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options audio --set direction=Output --set role=Communications
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options storage --set volume=D: --set optimizationReview=true --set optimizationMode=ReTrim
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options printing --set jobQueue="Office Printer" --set jobId=42 --set cancelJob=true
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options installer --set scope=Machine --set productCode={00000000-0000-0000-0000-000000000001}
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options time --set source=CustomPeers --set "peers=time.example.test 192.0.2.1"
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options eventlog --set channel=Setup --set execute=true
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options profiles --set minimumInactiveDays=180 --set checkDependencies=true
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options computer-name --set proposedName=NEW-PC --set execute=true
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options background-transfers --set scope=AllUsers
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options app-packages --set scope=AllUsers --set includeProvisioned=true
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options installed-apps --set scope=CurrentAccount
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options search --set rebuildIndex=true
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options component-cleanup --set execute=true
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options windows-recovery --set execute=true
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options firewall-services --set profile.private=true --set profile.public=true --set execute=true
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options bitlocker --set volume=C: --set operation=BackupRecovery --set backup=File --set backupFile=D:\recovery.txt --set protectorId=00000000-0000-0000-0000-000000000001 --set executeBackup=true
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow-options defender --set scanType=Quick --set startScan=true
```

The command returns host-neutral JSON for the option-rich workflows. It includes toggles, sliders, choices, text fields, defaults, bounds, dependencies and the execution boundary. Repeated `--set KEY=VALUE` arguments validate and normalize a proposed option plan, fill omitted defaults and identify an explicit machine-changing request. Validation does not execute the plan, inspect Windows or bypass the separate execution coordinator.

## Run a workflow

```powershell
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow Network
```

Add `--json` when a command supports structured output:

```powershell
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow Network --json
```

Validated shared controls can run the matching read-only workflow directly:

```powershell
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow network --set lookupHost=example.com --set timeoutSeconds=15
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow profiles --set minimumInactiveDays=180 --set checkDependencies=true --json
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow eventlog --set channel=Setup --json
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow firewall-services --set profile.private=true --set profile.public=true --json
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow computer-name --set proposedName=OFFICE-PC --json
dotnet run --project Cli\KillerBench.Cli.csproj -- workflow servicing-monitor --set refreshSeconds=5 --set samples=4
```

The workflow command accepts repeated `--set KEY=VALUE` values only after the shared validator accepts the complete plan. It refuses `--auto-repair`, recipe selection and every option marked as machine-changing. Use the dedicated preview and guarded execution command for a requested repair.

The first diagnostic result records the nondefault settings in effect. Sensitive paths, installer switches, protector IDs, package sources and deployment device names are redacted or counted. A prepared System Restore description and reason drive its creation-readiness review. Storage repair review collects exact-volume state plus a bounded read-only filesystem diagnosis, then explains backup, restart or dismount requirements without scheduling CHKDSK. A selected storage optimization method drives the exact-volume analysis. Printing selections correlate one exact queue with current condition and attribute evidence, and one exact job with the bounded visible-job inventory. A selected MSI product is reviewed against its exact registration state, current Installer activity, restart state and administrator access. A selected Windows Time source is reviewed against current role, policy, mode and peer evidence. A selected Event Log channel receives an exact configuration and policy-ownership review. Selected Firewall profiles receive an exact ActiveStore and policy-ownership review. A proposed computer name receives an exact identity, role, restart and administrator readiness review. None of these reviews performs or authorizes the proposed change.

## Run focused diagnostics

Focused commands are useful when a host needs one evidence boundary instead of a full workflow. Examples include:

```powershell
dotnet run --project Cli\KillerBench.Cli.csproj -- firewall-events
dotnet run --project Cli\KillerBench.Cli.csproj -- servicing-service-events
dotnet run --project Cli\KillerBench.Cli.csproj -- memory-diagnostic-results
```

## Live servicing monitor

```powershell
dotnet run --project Cli\KillerBench.Cli.csproj -- servicing-monitor
```

The monitor prints completed snapshots as they arrive. It reports recognized activity and evidence, not an invented completion percentage.

## Review Microsoft app removal package eligibility

```powershell
dotnet run --project Cli\KillerBench.Cli.csproj -- mace-review --outlook --json
dotnet run --project Cli\KillerBench.Cli.csproj -- mace-package-plan --outlook --package PACKAGE_FULL_NAME --json
```

The plan command repeats current Microsoft app removal inventory and safeguard collection, then evaluates one exact package full name. It is preview-only and connects no removal provider. Preserved-data eligibility is limited to loose-file development packages because Windows does not offer that safeguard for ordinary installed Store/MSIX packages. An eligible result does not remove the package.

## Export the VM validation plan

```powershell
dotnet run --project Cli\KillerBench.Cli.csproj -- validation-plan
dotnet run --project Cli\KillerBench.Cli.csproj -- validation-plan --workflow update
dotnet run --project Cli\KillerBench.Cli.csproj -- validation-plan --recipe windows.system-files
```

The command returns deterministic JSON scenario requirements for the installed recipe catalog. It runs no diagnostics or repairs. The output is a disposable-VM test plan, not evidence that a native scenario passed.

Validate records returned from disposable VMs and summarize current-version coverage:

```powershell
dotnet run --project Cli\KillerBench.Cli.csproj -- validation-record RECORD.json
dotnet run --project Cli\KillerBench.Cli.csproj -- validation-coverage RECORD.json MORE-RECORDS.json
```

Records must match the installed recipe version and scenario ID and contain every required artifact label and SHA-256 digest. Coverage keeps Passed, Failed, Blocked, Inconclusive and missing scenarios separate. These commands read the record files only.

Generate a readable local coverage report from the same validated records:

```powershell
dotnet run --project Cli\KillerBench.Cli.csproj -- validation-report coverage.html RECORD.json MORE-RECORDS.json
dotnet run --project Cli\KillerBench.Cli.csproj -- validation-report coverage.txt RECORD.json
```

HTML reports include every installed scenario, status and text filters, and the 13 Killer Tools themes. Missing evidence remains distinct from every recorded outcome. Use `--theme`, `--accent` and `--overwrite` when needed. Report generation does not inspect or change Windows.

## Execution boundary

Most commands are read-only. Commands that can change Windows require their exact execution switch and still enforce the same Engine eligibility, journaling, final-evidence, and verification rules. Unsupported execution is rejected.

## Exit behavior

Treat JSON fields and documented exit behavior as the automation contract. Do not parse decorative console text when structured output is available. Preserve the journal path for later review or reconciliation.
