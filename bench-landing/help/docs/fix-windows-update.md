# Fix Windows Update

Use this when updates fail, stall or keep reinstalling. KillerBench reads the update chain first, explains the failure with the evidence behind it, and only starts a service or repairs the component store when fresh evidence proves that exact problem.

## Before you start

- Run KillerBench as administrator. Update services, the component store and protected system files need elevation for complete evidence.
- Restart first if Windows already has a restart pending. Many update repairs are blocked until restart readiness is clear.
- Keep the computer on AC power for servicing work.

## Find out why updates fail

1. Open **Updates** and choose **Windows Update** in the tool sidebar.
2. Leave **Auto-repair confirmed issues** off for the first pass and press **GO**.
3. Read the **Readable report**. It lists stopped update services, pending restarts, low free space, component-store corruption and the exact error codes Windows recorded, with a recommended next step.
4. If the report names an error code, the matching follow-up checks run automatically. You can also look one up yourself with `killerbench-cli update-code 0x80070002`.

## Watch an update that seems stuck

Choose **Servicing activity monitor** in **Updates** and press **GO**. It takes repeated snapshots of update, Setup and Installer activity, transfer totals and restart indicators. Activity is shown as evidence only; it never claims a stall or a fault on its own.

## Repair what the report found

1. Run **Windows Update** again with **Auto-repair confirmed issues** turned on. It can start enabled update services that are confirmed stopped, and it runs DISM or SFC only when the live check confirms corruption.
2. For a single service, choose its focused tool instead, such as **BITS service**, **Cryptographic Services**, **Windows Modules Installer** or **Update Orchestrator Service**. Each one offers recovery only for that exact service.
3. When the report says to restart, restart before running anything else. KillerBench records restart-required outcomes and defers later work until the restart is done.

Every repair is journaled and read back. See [How changes stay safe](safety.html) for the full sequence.

## Install a specific update

1. Choose **Available updates** in **Updates** and press **GO** to discover updates from the configured source.
2. Select the updates you want and press **Preview selected**. The preview lists exactly what would install and any restart requirement.
3. Install only after reading the preview. KillerBench never selects or approves an update for you.

## From the command line

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

See [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) and [update-plan](cli.html#cmd-update-plan). If an install was interrupted, run [update-recover](cli.html#cmd-update-recover); it checks the outcome and never retries the update.

## Related

- [Updates section](updates.html)
- [Repair system files](repair-system-files.html)
- [Troubleshooting](troubleshooting.html)
