# Speicherplatz freigeben

Verwenden Sie diese Anleitung, wenn ein Laufwerk knapp wird. KillerBench zeigt, wohin der Speicher geht, zeigt vorab, was jedes Bereinigungsziel entfernen würde, und löscht erst, wenn Sie die dauerhafte Bereinigung einschalten.

## Prüfen, wie viel Platz bleibt

1. Öffnen Sie **Speicher** und wählen Sie **Speicher**.
2. Drücken Sie **Diagnose ausführen**. Der Bericht markiert jedes Volume mit Laufwerksbuchstaben unter 10 % freiem Speicher und zeigt die Zuteilung für Schattenkopien, Kontingentstandards und die Speicheroptimierungsrichtlinie.
3. Für Absturzabbilder und alte Update-Inhalte führen Sie zusätzlich **Vitalwerte** unter **Diagnose** und **Windows Update** unter **Updates** aus; dort erscheint die Größe des Update-Caches.

## Eine Bereinigung in der Vorschau ansehen

1. Öffnen Sie **Wartung** und wählen Sie **Bereinigung**.
2. Markieren Sie die gewünschten Ziele, etwa **Temporäre Dateien des aktuellen Benutzers**, **Temporäre Windows-Dateien**, **Browser-Caches des aktuellen Benutzers**, **Delivery Optimization-Cache** oder **Papierkorb des aktuellen Benutzers**. Zeigen Sie auf ein Ziel, um zu lesen, welche Ordner es genau umfasst.
3. Legen Sie **Mindestalter (Tage)** und **Maximale Dateien pro Ziel** fest.
4. Drücken Sie **Diagnose ausführen**, während **Ausgewählte dauerhafte Bereinigung nach frischen Prüfungen durchführen** ausgeschaltet ist. Der Bericht listet die Kandidaten und den geschätzten Speicher für jedes Ziel. Nichts wird gelöscht.

## Die Dateien entfernen

1. Schalten Sie **Ausgewählte dauerhafte Bereinigung nach frischen Prüfungen durchführen** ein und drücken Sie **Diagnose ausführen**.
2. KillerBench prüft jeden Kandidaten vor dem Entfernen erneut, überspringt neue, geschützte oder verwendete Dateien und hält jedes Ergebnis im Journal fest.

Die dauerhafte Bereinigung umgeht den Papierkorb und lässt sich nicht rückgängig machen. Das Entfernen von Fehlerberichten und Absturzabbildern verwirft Nachweise, die Sie zur Untersuchung von Abstürzen brauchen könnten; prüfen Sie diese daher zuerst.

## Die vorherige Windows-Installation entfernen

Nach einem Funktionsupgrade kann **Vorherige Windows-Installation** unter **Bereinigung** die Kopie für das Zurücksetzen über die unterstützte Windows-Anfrage zum Entfernen der Rollback-Funktion löschen. Diese Aktion läuft für sich allein, verlangt **Ich habe alle benötigten Dateien und Einstellungen aus der vorherigen Installation gesichert** und entfernt dauerhaft die Möglichkeit, zur früheren Version zurückzukehren.

## Über die Befehlszeile

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

Siehe [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) und [previous-windows](cli.html#cmd-previous-windows).

## Verwandte Themen

- [Bereich Wartung](maintenance.html#cleanup)
- [Bereich Speicher](storage.html)
- [Laufwerk prüfen und reparieren](check-a-drive.html)
