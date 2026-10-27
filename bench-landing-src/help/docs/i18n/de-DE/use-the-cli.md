# KillerBench aus Skripten ausführen

Verwenden Sie die Befehlszeile, wenn Sie KillerBench-Nachweise in einem Skript, einem RMM-Auftrag oder einer Remotekonsole brauchen. `killerbench-cli` führt dieselbe Engine wie die Desktop-App aus und gibt JSON aus.

## Erste Schritte

1. Öffnen Sie PowerShell dort, wo `killerbench-cli.exe` liegt, oder fügen Sie seinen Ordner Ihrem Pfad hinzu.
2. Führen Sie `killerbench-cli help` aus, um jeden Befehl mit seiner Sicherheitsgrenze aufzulisten.
3. Führen Sie `killerbench-cli diagnostic-scopes` aus, um die 11 empfohlenen Diagnosegruppen zu sehen.

## Eine Prüfung ausführen und einen Bericht speichern

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` dient nur der Diagnose und lehnt `--auto-repair` ab. Verwenden Sie einen gezielten Reparatur-Workflow, wenn eine bestimmte Aktion nötig ist. `--report` schreibt denselben Bericht mit Design, den die App exportiert. Mit `--scope` prüfen Sie nur einige Bereiche, und `common-check` liefert kombiniertes JSON ohne Workflow-Journal.

## Exitcodes lesen

| Code | Bedeutung |
| --- | --- |
| 0 | Die Vorschau oder das überprüfte Ergebnis war erfolgreich. |
| 1 | Ein Start-, Eingabe- oder Journalfehler hat den Befehl gestoppt. |
| 2 | Die Argumente wurden nicht erkannt. |
| 3 | Blockiert, fehlgeschlagen oder nicht überprüft. |
| 4 | Ein Workflow benötigt einen Neustart, bevor er überprüfen kann. |
| 130 | Sie haben ihn gestoppt. |

Exitcode 0 bei einem schreibgeschützten Befehl bedeutet, dass er gelaufen ist, nicht dass der Computer fehlerfrei ist. Die Befunde stehen im JSON.

## Eine Änderung vornehmen

Jeder Befehl, der den Computer verändern kann, zeigt standardmäßig eine Vorschau. Lesen Sie die Vorschau und führen Sie dann denselben Befehl mit `--execute` aus. Wird eine Anfrage unterbrochen, führen Sie den passenden `-recover`-Befehl aus; er meldet das tatsächliche Ergebnis und wiederholt die Änderung nie.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Frühere Ergebnisse erneut öffnen

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## Aus einem KI-Assistenten verwenden

[KillerMCP](../mcp.html) kann die installierte CLI aufrufen, um Codes des Geräte-Managers und Windows-Fehlercodes nachzuschlagen.

## Verwandte Themen

- [Befehlszeilenreferenz](cli.html)
- [Engine-Entwicklerhandbuch](../engine/index.html)
- [Problembehandlung](troubleshooting.html)
