# Investigate crashes and blue screens

Use this when Windows restarts unexpectedly, shows a stop error, or an application keeps crashing. KillerBench gathers crash, shutdown, hardware-error and reliability evidence into one report. It describes what was recorded and never guesses a root cause.

## Check the computer

1. Open **Diagnostics** and choose **Vitals**.
2. Press **Run diagnosis**. The report combines memory, disk health, device problems, Reliability Monitor history, hardware-error records, crash reports, unexpected shutdowns and Windows Memory Diagnostic results.
3. Read the conclusion and the evidence under it. Historical crashes explain what happened; they do not prove a fault is still present.

## Narrow it down

- **Hardware error history** reads WHEA and memory diagnostic records together with device problems and disk health.
- **Reliability history** groups the last 14 days of application crashes, hangs and unexpected shutdowns.
- **Application errors** focuses on crashing programs.
- **Driver installation history** shows failed driver installs matched to devices that are present now.
- **Device problems** lists devices with Device Manager error codes.

## Read crash dumps

KillerBench lists dump configuration and dump-file metadata and can analyze minidump structure for the exception and module names. Dump contents are never opened in the desktop app. With a bug-check knowledge pack loaded, stop codes resolve to their documented meaning.

Keep the dumps until you finish investigating. **Cleanup** can remove them, but that discards the evidence.

## From the command line

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

See [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) and [device-code](cli.html#cmd-device-code).

## Related

- [Diagnostics section](diagnostics.html#vitals)
- [Reports and evidence](reports.html)
