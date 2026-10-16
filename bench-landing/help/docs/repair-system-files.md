# Repair system files

Use this when Windows components are corrupted: features fail to install, updates report component errors, or system tools crash. KillerBench runs DISM and SFC only after a live check confirms corruption, then verifies the result.

## Before you start

- Run KillerBench as administrator.
- Clear any pending restart first.
- If the machine is managed, check whether policy points DISM at a specific repair source. KillerBench reports that policy and does not change it.

## Check for corruption

1. Open **Repair** and choose **System files**.
2. Leave **Auto-repair confirmed issues** off and press **Run diagnosis**.
3. Read the report. It inventories servicing package, optional-feature and capability states, checks prerequisites and scans protected system files. A clean result means no repair is offered.

## Repair confirmed corruption

1. Turn on **Auto-repair confirmed issues** and press **Run repair**. KillerBench chooses DISM or SFC only when the live check confirms corruption.
2. Wait for verification. The report shows **Verified** only when the follow-up check reads back a repaired state.
3. If the report says the store is unrecoverable or the repair source is unavailable, stop there. Repeating the repair will not change the outcome.

An interrupted DISM or SFC run is reconciled on the next boot without repeating the servicing command.

## Clean up the component store

Choose **Component-store cleanup** in **Updates**. It first asks Windows whether cleanup is recommended. Optional maintenance removes superseded components and analyzes again. It never uses ResetBase and never deletes WinSxS content by hand.

## From the command line

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

See [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) and [servicing-logs](cli.html#cmd-servicing-logs). `repair` previews by default and acts only with `--execute`.

## Related

- [Fix Windows Update](fix-windows-update.html)
- [Repair section](repair.html#system-files)
- [How changes stay safe](safety.html)
