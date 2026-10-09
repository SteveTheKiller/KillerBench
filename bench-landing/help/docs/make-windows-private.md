# Make Windows more private

Use this to review and tighten privacy on a laptop. KillerBench reads the current settings, shows which ones policy or device management already own, and changes only the settings you pick, one at a time, with an exact undo.

## Review the current settings

1. Open **Privacy**.
2. Tick the areas you want to review. Every area starts unchecked.
3. Press **GO**. The report shows the current value for this machine and the account running KillerBench, and flags settings owned by policy.

## Change a setting

1. Choose the setting in **Privacy setting**, such as camera, microphone, location, diagnostic data, advertising, clipboard history, cloud content search or search highlights.
2. Choose the new value and tick **Apply when I press GO**.
3. Press **GO**. KillerBench checks for a policy owner, records the current value, makes the one change and reads it back.

Settings owned by group policy or device management are left alone. Change those where they are managed.

## Undo a change

Each verified change keeps a journal. The matching undo restores the exact previous value, again after a policy check.

## Declutter Windows

For suggestions, consumer content, Recall and AI features, taskbar and Start clutter, Edge and preinstalled software, open **Maintenance** and choose **Declutter Windows**. The review is read-only and nothing is selected automatically.

## From the command line

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

See [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) and [debloat-review](cli.html#cmd-debloat-review). An unsupported key lists the supported choices.

## Related

- [Privacy section](privacy.html)
- [Set up a new computer](set-up-new-pc.html)
