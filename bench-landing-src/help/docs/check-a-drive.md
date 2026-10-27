# Check and repair a drive

Use this when a volume reports errors, Windows asks to scan a drive, or files fail to open. KillerBench checks the volume read-only first and only schedules or runs a filesystem repair after you confirm a backup.

## Before you start

- Back up important data on the volume. A filesystem repair can change or remove damaged files.
- Run KillerBench as administrator.

## Check the volume

1. Open **Storage** and choose **Storage**.
2. Pick the drive in **Volume to check** and press **Run diagnosis**.
3. Read the report. It shows disk and volume health, the dirty bit, online NTFS self-healing, controller and disk device errors, reliability counters and recent storage events such as resets, retries and unexpected removal.

A clean check means no repair is offered. Disk temperature, wear and uncorrected I/O counters are review evidence and do not trigger a repair by themselves.

## Repair the filesystem

1. Press **Review filesystem repair** and choose **Spot repair** or **Offline scan and repair**.
2. For a data volume, tick **Allow a non-system volume to go offline** if you can close everything that uses it. The Windows volume cannot go offline, so its repair is scheduled for the next restart instead.
3. Tick **Important data on this volume is backed up**.
4. Confirm the repair. KillerBench rechecks the exact volume immediately before it acts and records a pending entry first.
5. For a scheduled repair, restart when convenient. KillerBench verifies the result after the restart without running the repair again.

## Optimize or retrim

The same tool can analyze the volume and run the Windows-recommended optimization, **Retrim solid-state storage** or a hard-disk defragmentation. Each is a separate confirmed action on one exact healthy volume.

## From the command line

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

See [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) and [volume-optimize](cli.html#cmd-volume-optimize).

## Related

- [Storage section](storage.html)
- [Free up disk space](free-disk-space.html)
- [Troubleshooting](troubleshooting.html)
