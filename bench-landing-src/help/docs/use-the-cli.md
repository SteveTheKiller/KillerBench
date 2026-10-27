# Run KillerBench from scripts

Use the command line when you want KillerBench evidence in a script, an RMM job or a remote console. `killerbench-cli` runs the same Engine as the desktop app and prints JSON.

## Get started

1. Open PowerShell where `killerbench-cli.exe` is, or add its folder to your path.
2. Run `killerbench-cli help` to list every command with its safety boundary.
3. Run `killerbench-cli diagnostic-scopes` to see the 11 recommended diagnostic groups.

## Run a scan and save a report

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` is diagnostic-only and rejects `--auto-repair`. Use a focused repair workflow when a specific action is needed. `--report` writes the same themed report the app exports. Use `--scope` to check only some areas, and `common-check` for combined JSON without a workflow journal.

## Read exit codes

| Code | Meaning |
| --- | --- |
| 0 | The preview or verified result succeeded. |
| 1 | A startup, input or journal error stopped the command. |
| 2 | The arguments were not recognized. |
| 3 | Blocked, failed or unverified. |
| 4 | A workflow needs a restart before it can verify. |
| 130 | You stopped it. |

Exit 0 on a read-only command means it ran, not that the computer is healthy. Read the JSON for findings.

## Make a change

Every command that can change the machine previews by default. Read the preview, then run the same command with `--execute`. If a request is interrupted, run the matching `-recover` command; it reports the real outcome and never repeats the change.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Reopen old results

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## Use it from an AI assistant

[KillerMCP](../mcp.html) can call the installed CLI to look up Device Manager and Windows error codes.

## Related

- [Command line reference](cli.html)
- [Engine developer guide](../engine/index.html)
- [Troubleshooting](troubleshooting.html)
