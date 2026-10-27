# Laufwerk prüfen und reparieren

Verwenden Sie diese Anleitung, wenn ein Volume Fehler meldet, Windows zur Prüfung eines Laufwerks auffordert oder Dateien sich nicht öffnen lassen. KillerBench prüft das Volume zuerst schreibgeschützt und plant oder startet eine Dateisystemreparatur erst, nachdem Sie eine Sicherung bestätigt haben.

## Bevor Sie beginnen

- Sichern Sie wichtige Daten auf dem Volume. Eine Dateisystemreparatur kann beschädigte Dateien ändern oder entfernen.
- Führen Sie KillerBench als Administrator aus.

## Das Volume prüfen

1. Öffnen Sie **Speicher** und wählen Sie **Speicher**.
2. Wählen Sie das Laufwerk unter **Zu prüfendes Volume** und drücken Sie **Diagnose ausführen**.
3. Lesen Sie den Bericht. Er zeigt den Zustand von Datenträger und Volume, das Dirty-Bit, die NTFS-Selbstheilung im laufenden Betrieb, Fehler von Controller- und Datenträgergeräten, Zuverlässigkeitszähler und aktuelle Speicherereignisse wie Zurücksetzungen, Wiederholungen und unerwartetes Entfernen.

Eine saubere Prüfung bedeutet, dass keine Reparatur angeboten wird. Datenträgertemperatur, Verschleiß und nicht korrigierte E/A-Zähler sind Nachweise zur Prüfung und lösen für sich allein keine Reparatur aus.

## Das Dateisystem reparieren

1. Drücken Sie **Dateisystemreparatur prüfen** und wählen Sie **Spot-Reparatur** oder **Offlineprüfung und Reparatur**.
2. Markieren Sie bei einem Datenvolume **Nicht-Systemvolume offline schalten erlauben**, wenn Sie alles schließen können, was es verwendet. Das Windows-Volume kann nicht offline gehen; seine Reparatur wird daher für den nächsten Neustart geplant.
3. Markieren Sie **Wichtige Daten auf diesem Volume sind gesichert**.
4. Bestätigen Sie die Reparatur. KillerBench prüft das genaue Volume unmittelbar vor der Aktion erneut und legt zuerst einen ausstehenden Eintrag an.
5. Starten Sie bei einer geplanten Reparatur zu einem passenden Zeitpunkt neu. KillerBench überprüft das Ergebnis nach dem Neustart, ohne die Reparatur erneut auszuführen.

## Optimieren oder erneut trimmen

Dasselbe Werkzeug kann das Volume analysieren und die von Windows empfohlene Optimierung, **Solid-State-Speicher erneut trimmen** oder eine Defragmentierung der Festplatte ausführen. Jede davon ist eine eigene, bestätigte Aktion für genau ein fehlerfreies Volume.

## Über die Befehlszeile

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

Siehe [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) und [volume-optimize](cli.html#cmd-volume-optimize).

## Verwandte Themen

- [Bereich Speicher](storage.html)
- [Speicherplatz freigeben](free-disk-space.html)
- [Problembehandlung](troubleshooting.html)
