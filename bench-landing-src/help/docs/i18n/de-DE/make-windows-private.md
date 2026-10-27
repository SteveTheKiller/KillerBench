# Windows datenschutzfreundlicher machen

Verwenden Sie diese Anleitung, um den Datenschutz auf einem Laptop zu prüfen und zu verschärfen. KillerBench liest die aktuellen Einstellungen, zeigt, welche bereits von Richtlinien oder Geräteverwaltung gesteuert werden, und ändert nur die von Ihnen gewählten Einstellungen, eine nach der anderen, mit genauer Rückgängig-Möglichkeit.

## Die aktuellen Einstellungen prüfen

1. Öffnen Sie **Datenschutz**.
2. Markieren Sie die Bereiche, die Sie prüfen möchten. Jeder Bereich ist anfangs nicht markiert.
3. Drücken Sie **Diagnose ausführen**. Der Bericht zeigt den aktuellen Wert für diesen Computer und das Konto, unter dem KillerBench läuft, und kennzeichnet Einstellungen, die von Richtlinien gesteuert werden.

## Eine Einstellung ändern

1. Wählen Sie die Einstellung unter **Datenschutzeinstellung**, etwa Kamera, Mikrofon, Standort, Diagnosedaten, Werbung, Zwischenablageverlauf, Suche in Cloudinhalten oder Suchhighlights.
2. Wählen Sie den neuen Wert und markieren Sie **Anwenden, wenn ich „Diagnose ausführen“ wähle**.
3. Drücken Sie **Diagnose ausführen**. KillerBench prüft, ob eine Richtlinie zuständig ist, hält den aktuellen Wert fest, nimmt genau diese eine Änderung vor und liest sie zurück.

Einstellungen, die von Gruppenrichtlinien oder Geräteverwaltung gesteuert werden, bleiben unverändert. Ändern Sie diese dort, wo sie verwaltet werden.

## Eine Änderung rückgängig machen

Jede überprüfte Änderung behält ein Journal. Das passende Rückgängigmachen stellt den genauen vorherigen Wert wieder her, wieder nach einer Richtlinienprüfung.

## Windows entrümpeln

Für Vorschläge, Verbraucherinhalte, Recall- und KI-Funktionen, überladene Taskleiste und Startmenü, Edge und vorinstallierte Software öffnen Sie **Wartung** und wählen **Windows entrümpeln**. Die Prüfung ist schreibgeschützt, und nichts wird automatisch ausgewählt.

## Über die Befehlszeile

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

Siehe [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) und [debloat-review](cli.html#cmd-debloat-review). Ein nicht unterstützter Schlüssel listet die unterstützten Auswahlmöglichkeiten auf.

## Verwandte Themen

- [Bereich Datenschutz](privacy.html)
- [Neuen Computer einrichten](set-up-new-pc.html)
