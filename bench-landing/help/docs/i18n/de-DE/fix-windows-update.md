# Windows Update reparieren

Verwenden Sie diese Anleitung, wenn Updates fehlschlagen, hängen bleiben oder sich immer wieder neu installieren. KillerBench liest zuerst die Update-Kette, erklärt den Fehler mit den zugehörigen Nachweisen und startet einen Dienst oder repariert den Komponentenspeicher nur, wenn aktuelle Nachweise genau dieses Problem belegen.

## Bevor Sie beginnen

- Führen Sie KillerBench als Administrator aus. Update-Dienste, der Komponentenspeicher und geschützte Systemdateien benötigen für vollständige Nachweise erhöhte Rechte.
- Starten Sie zuerst neu, wenn Windows bereits einen Neustart erwartet. Viele Update-Reparaturen sind blockiert, bis die Neustartbereitschaft geklärt ist.
- Lassen Sie den Computer bei Wartungsarbeiten am Netzteil.

## Herausfinden, warum Updates fehlschlagen

1. Öffnen Sie **Updates** und wählen Sie in der Werkzeugleiste **Windows Update**.
2. Lassen Sie **Bestätigte Probleme automatisch reparieren** beim ersten Durchlauf aus und drücken Sie **Diagnose ausführen**.
3. Lesen Sie den Bericht unter **Lesbarer Bericht**. Er listet angehaltene Update-Dienste, ausstehende Neustarts, wenig freien Speicher, Beschädigungen im Komponentenspeicher und die genauen von Windows aufgezeichneten Fehlercodes mit einem empfohlenen nächsten Schritt.
4. Nennt der Bericht einen Fehlercode, laufen die passenden Folgeprüfungen automatisch. Sie können einen Code auch selbst mit `killerbench-cli update-code 0x80070002` nachschlagen.

## Ein Update beobachten, das zu hängen scheint

Wählen Sie unter **Updates** den **Wartungsaktivitätsmonitor** und drücken Sie **Diagnose ausführen**. Er erstellt wiederholte Momentaufnahmen der Update-, Setup- und Installer-Aktivität, der Übertragungssummen und der Neustartanzeigen. Die Aktivität wird nur als Nachweis gezeigt; der Monitor behauptet nie von sich aus einen Stillstand oder Fehler.

## Reparieren, was der Bericht gefunden hat

1. Führen Sie **Windows Update** erneut mit eingeschaltetem **Bestätigte Probleme automatisch reparieren** aus. Das Werkzeug kann aktivierte Update-Dienste starten, die nachweislich angehalten sind, und führt DISM oder SFC nur aus, wenn die Live-Prüfung eine Beschädigung bestätigt.
2. Für einen einzelnen Dienst wählen Sie stattdessen sein gezieltes Werkzeug, etwa **BITS-Dienst**, **Kryptografiedienste**, **Windows Modules Installer** oder **Update Orchestrator Service**. Jedes bietet die Wiederherstellung nur für genau diesen Dienst an.
3. Wenn der Bericht einen Neustart verlangt, starten Sie neu, bevor Sie etwas anderes ausführen. KillerBench hält Ergebnisse fest, die einen Neustart erfordern, und stellt spätere Arbeiten bis nach dem Neustart zurück.

Jede Reparatur wird im Journal festgehalten und zurückgelesen. Den vollständigen Ablauf beschreibt [Wie Änderungen sicher bleiben](safety.html).

## Ein bestimmtes Update installieren

1. Wählen Sie unter **Updates** das Werkzeug **Verfügbare Updates** und drücken Sie **Diagnose ausführen**, um Updates aus der konfigurierten Quelle zu finden.
2. Wählen Sie die gewünschten Updates aus und drücken Sie **Ausgewählte in der Vorschau anzeigen**. Die Vorschau listet genau, was installiert würde, und jede Neustartanforderung.
3. Installieren Sie erst, nachdem Sie die Vorschau gelesen haben. KillerBench wählt oder genehmigt nie ein Update für Sie.

## Über die Befehlszeile

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

Siehe [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) und [update-plan](cli.html#cmd-update-plan). Wurde eine Installation unterbrochen, führen Sie [update-recover](cli.html#cmd-update-recover) aus; der Befehl prüft das Ergebnis und versucht das Update nie erneut.

## Verwandte Themen

- [Bereich Updates](updates.html)
- [Systemdateien reparieren](repair-system-files.html)
- [Problembehandlung](troubleshooting.html)
