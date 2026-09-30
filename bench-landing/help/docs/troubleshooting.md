# Troubleshooting

## An action says it is blocked

A blocked result means a safety check failed, and nothing was changed. The message names the reason. The most common reasons are below.

- **Administrator access is required.** Close KillerBench and start it again with **Run as administrator**.
- **Restart readiness must be clear.** Windows has a restart pending. Restart the computer, then run the action again.
- **Managed policy is present.** Group policy or device management owns the setting. Change it there instead.
- **Evidence changed.** Something changed between the first check and the final recheck. Run the tool again to read the new state.

## A previous request needs reconciliation

KillerBench found a pending record from an interrupted request. Run the same action again. It checks what happened and reports the real outcome without repeating the request. On the command line, use the matching `-recover` command, for example `killerbench-cli bitlocker-auto-unlock-recover`.

## A check shows Incomplete or Unknown

The evidence could not be read, often because KillerBench was not running as administrator or a Windows service was unavailable. KillerBench reports this honestly instead of assuming the computer is healthy.

## Where files are kept

Journals, pending records and saved evidence live under `%LOCALAPPDATA%\KillerBench`. Each guarded action keeps its own folder. Keep them when you need a record of what changed.

## Reporting a problem

KillerBench is in development. Include the exported report and the activity log when you report a problem, and remove anything you would not share.
