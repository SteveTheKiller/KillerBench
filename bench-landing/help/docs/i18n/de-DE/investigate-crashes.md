# Abstürze und Bluescreens untersuchen

Verwenden Sie diese Anleitung, wenn Windows unerwartet neu startet, einen Stoppfehler zeigt oder eine Anwendung immer wieder abstürzt. KillerBench fasst Nachweise zu Abstürzen, Herunterfahren, Hardwarefehlern und Zuverlässigkeit in einem Bericht zusammen. Es beschreibt, was aufgezeichnet wurde, und rät nie eine Ursache.

## Den Computer prüfen

1. Öffnen Sie **Diagnose** und wählen Sie **Vitalwerte**.
2. Drücken Sie **Diagnose ausführen**. Der Bericht kombiniert Arbeitsspeicher, Datenträgerzustand, Geräteprobleme, den Verlauf der Zuverlässigkeitsüberwachung, Hardwarefehlereinträge, Absturzberichte, unerwartetes Herunterfahren und die Ergebnisse der Windows-Speicherdiagnose.
3. Lesen Sie die Schlussfolgerung und die Nachweise darunter. Historische Abstürze erklären, was passiert ist; sie belegen nicht, dass ein Fehler noch besteht.

## Eingrenzen

- **Hardwarefehlerverlauf** liest WHEA- und Speicherdiagnoseeinträge zusammen mit Geräteproblemen und Datenträgerzustand.
- **Zuverlässigkeitsverlauf** fasst die Anwendungsabstürze, Hänger und unerwarteten Herunterfahrvorgänge der letzten 14 Tage zusammen.
- **Anwendungsfehler** konzentriert sich auf abstürzende Programme.
- **Treiberinstallationsverlauf** zeigt fehlgeschlagene Treiberinstallationen, zugeordnet zu Geräten, die jetzt vorhanden sind.
- **Geräteprobleme** listet Geräte mit Fehlercodes des Geräte-Managers.

## Absturzabbilder lesen

KillerBench listet die Konfiguration für Speicherabbilder und die Metadaten der Abbilddateien und kann die Struktur von Minidumps auf Ausnahme- und Modulnamen analysieren. Die Inhalte von Abbildern werden in der Desktop-App nie geöffnet. Mit einem geladenen Wissenspaket für Bugchecks werden Stoppcodes in ihre dokumentierte Bedeutung aufgelöst.

Behalten Sie die Abbilder, bis Sie die Untersuchung abgeschlossen haben. **Bereinigung** kann sie entfernen, verwirft damit aber die Nachweise.

## Über die Befehlszeile

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

Siehe [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) und [device-code](cli.html#cmd-device-code).

## Verwandte Themen

- [Bereich Diagnose](diagnostics.html#vitals)
- [Berichte und Nachweise](reports.html)
