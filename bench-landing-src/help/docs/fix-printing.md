# Fix printing problems

Use this when jobs sit in the queue, a printer shows offline, or the wrong printer is the default. KillerBench reads the spooler, queues, jobs, drivers and ports first, then lets you fix one exact queue or job.

## Check the printers

1. Open **Devices** and choose **Printing**.
2. Press **Find local printers** and pick the queue that is misbehaving. To check every queue, choose **Use general printing checks**.
3. Leave **Auto-repair confirmed issues** off and press **Run diagnosis**.
4. Read the report. It shows the Print Spooler state, paused queues, offline mode, visible jobs, driver conditions, port registrations, recent PrintService errors and, for network printers, one connection attempt to the printer's assigned address.

A successful connection or a clean condition code does not prove the printer has paper, toner or a working mechanism.

## Repair the queue

1. Run **Printing** again with **Auto-repair confirmed issues** turned on. It can start the enabled spooler, resume an exact paused queue that has jobs waiting, and clear offline mode.
2. Clearing offline mode can release waiting jobs, so make sure those jobs should still print.
3. A queue someone paused on purpose with nothing waiting stays paused.

## Clear one stuck job

1. Press **Find blocked print jobs**.
2. Select the job that shows Error or Blocked and tick **Cancel only this selected job**.
3. Press **Run diagnosis**. Only that job is canceled, and the result is read back.

## Fix the default printer

Choose the queue that should be the default and tick **Repair a missing or conflicting default with this queue**, then press **Run diagnosis**. This only acts when the default is missing or conflicting.

## From the command line

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

See [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) and [default-printer-set](cli.html#cmd-default-printer-set).

## Related

- [Devices section](devices.html#printing)
- [Fix network and internet problems](fix-network.html)
