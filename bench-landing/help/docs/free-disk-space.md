# Free up disk space

Use this when a drive is running low. KillerBench shows where space is going, previews what each cleanup target would remove, and deletes only after you turn on permanent cleanup.

## See how much space is left

1. Open **Storage** and choose **Storage**.
2. Press **Run diagnosis**. The report flags every drive-letter volume under 10% free space and shows shadow-copy allocation, quota defaults and Storage Sense policy.
3. For crash dumps and old update content, also run **Vitals** in **Diagnostics** and **Windows Update** in **Updates**; the update cache size appears there.

## Preview a cleanup

1. Open **Maintenance** and choose **Cleanup**.
2. Tick the targets you want, such as **Current user temporary files**, **Windows temporary files**, **Current user browser caches**, **Delivery Optimization cache** or **Current user Recycle Bin**. Hover a target to read exactly which folders it covers.
3. Set **Minimum age (days)** and **Maximum files per target**.
4. Press **Run diagnosis** with **Perform the selected permanent cleanup after fresh checks** turned off. The report lists candidates and estimated space for each target. Nothing is deleted.

## Remove the files

1. Turn on **Perform the selected permanent cleanup after fresh checks** and press **Run diagnosis**.
2. KillerBench rechecks every candidate before removing it, skips files that are recent, protected or in use, and records each outcome in the journal.

Permanent cleanup bypasses the Recycle Bin and cannot be undone. Removing error reports and crash dumps discards evidence you may need to investigate crashes, so review those first.

## Remove the previous Windows installation

After a feature upgrade, **Previous Windows installation** in **Cleanup** can remove the rollback copy through the supported Windows rollback-removal request. It runs by itself, needs **I backed up any files and settings I need from the previous installation**, and permanently removes the ability to go back to the earlier version.

## From the command line

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

See [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) and [previous-windows](cli.html#cmd-previous-windows).

## Related

- [Maintenance section](maintenance.html#cleanup)
- [Storage section](storage.html)
- [Check and repair a drive](check-a-drive.html)
