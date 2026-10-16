# Problembehandlung

## Eine Aktion meldet, dass sie blockiert ist

Ein blockiertes Ergebnis bedeutet, dass die angeforderte Aktion nicht fortfahren konnte. Lesen Sie Meldung und Journal, um zu sehen, ob sie vor dem Senden oder nach einem früheren Schritt gestoppt hat. Gehen Sie nicht davon aus, dass ein teilweise abgeschlossener Vorgang zurückgesetzt wurde. Die häufigsten Gründe bei den Voraussetzungen stehen unten.

- **Administratorzugriff ist erforderlich.** Schließen Sie KillerBench und starten Sie es erneut mit **Als Administrator ausführen**.
- **Die Neustartbereitschaft muss geklärt sein.** Windows erwartet einen Neustart. Starten Sie den Computer neu und führen Sie die Aktion dann erneut aus.
- **Eine verwaltete Richtlinie ist vorhanden.** Eine Gruppenrichtlinie oder Geräteverwaltung steuert die Einstellung. Ändern Sie sie stattdessen dort.
- **Die Nachweise haben sich geändert.** Zwischen der ersten Prüfung und der letzten Gegenprüfung hat sich etwas geändert. Führen Sie das Werkzeug erneut aus, um den neuen Zustand zu lesen.

## Eine frühere Anfrage muss abgeglichen werden

KillerBench hat einen ausstehenden Eintrag einer unterbrochenen Anfrage gefunden. Führen Sie dieselbe Aktion erneut aus. Sie prüft, was passiert ist, und meldet das tatsächliche Ergebnis, ohne die Anfrage zu wiederholen. In der Befehlszeile verwenden Sie den passenden `-recover`-Befehl, zum Beispiel `killerbench-cli bitlocker-auto-unlock-recover`.

## Eine Prüfung zeigt „Unvollständig“ oder „Unbekannt“

Die Nachweise konnten nicht gelesen werden, oft weil KillerBench nicht als Administrator lief oder ein Windows-Dienst nicht verfügbar war. KillerBench meldet das ehrlich, statt anzunehmen, der Computer sei fehlerfrei.

## Wo Dateien gespeichert werden

Journale, ausstehende Einträge und gespeicherte Nachweise liegen unter `%LOCALAPPDATA%\KillerBench`. Jede abgesicherte Aktion hat ihren eigenen Ordner. Behalten Sie sie, wenn Sie eine Aufzeichnung der Änderungen benötigen.

## Ein Problem melden

KillerBench befindet sich in Entwicklung. Fügen Sie bei einer Problemmeldung den exportierten Bericht und das Aktivitätsprotokoll bei und entfernen Sie alles, was Sie nicht weitergeben würden.
