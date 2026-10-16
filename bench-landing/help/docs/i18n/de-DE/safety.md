# Wie Änderungen sicher bleiben

KillerBench behandelt Diagnose und Änderung als getrennte Aufgaben. Die Diagnose liest nur. Die vollständige Systemprüfung führt nie Reparaturen aus. Wählen Sie eine bestimmte Reparatur oder Aktion, prüfen Sie ihre Optionen und Voraussetzungen und bestätigen Sie die vorgeschlagene Änderung.

## Vor einer Änderung

1. **Aktuelle Nachweise.** Das Werkzeug liest den aktuellen Zustand erneut. Ein gespeicherter Bericht oder ein altes Ereignis berechtigt nie zu einer Änderung.
2. **Genaue Eignung.** Die Änderung wird nur angeboten, wenn die Nachweise genau das Problem zeigen, das sie behebt. Alles Unvollständige blockiert sie.
3. **Ihre Bestätigung.** Prüfen Sie die gewählte Aktion in ihrem Bestätigungsdialog. Manche Aktionen, etwa das Entschlüsseln eines Laufwerks, benötigen eine zweite Bestätigung. Der Dialog folgt dem aktiven App-Design; sein Aussehen ändert nichts an den Anforderungen der Aktion.
4. **Voraussetzungen.** Administratorrechte, Neustartzustand und die Zuständigkeit von Richtlinien oder Geräteverwaltung werden geprüft. Einstellungen, die eine Richtlinie verwaltet, bleiben unverändert.
5. **Ein ausstehender Eintrag.** KillerBench schreibt auf, was es tun wird, bevor es Windows dazu auffordert.
6. **Eine letzte Gegenprüfung.** Unmittelbar vor der Anfrage wird alles erneut geprüft, sodass eine zwischenzeitliche Änderung die Aktion stoppt.

## Nach einer Änderung

- KillerBench liest das Ergebnis zurück und meldet **Überprüft** nur, wenn Windows den erwarteten Zustand zeigt.
- Meldet Windows Erfolg, ohne dass sich der Zustand geändert hat, sagt das Ergebnis das, und nichts wird wiederholt.
- Jede Änderung wird in einem Journal unter `%LOCALAPPDATA%\KillerBench` gespeichert.

## Wenn etwas unterbrochen wird

Unterstützte Aktionen mit Journal behalten einen ausstehenden Eintrag, wenn eine Anfrage Windows möglicherweise erreicht hat und die Überprüfung nicht abgeschlossen ist. Verwenden Sie den dokumentierten Abgleich der Aktion oder ihren passenden `-recover`-Befehl, um diesen Eintrag zu prüfen. Der Abgleich ist kein automatisches Zurücksetzen. Behalten Sie das Journal, wenn ein Ergebnis unsicher ist, und wiederholen Sie die Änderung nicht von Hand. Absturz, Abbruch und das Verhalten nach einem späteren Start müssen für jede Aktion noch nativ validiert werden.

## Was KillerBench nicht tut

- Ein TPM löschen, Schutzvorrichtungen entfernen, die es nicht selbst angelegt hat, oder Wiederherstellungskennwörter anzeigen.
- Eine Einstellung ändern, die eine Gruppenrichtlinie oder Geräteverwaltung steuert.
- Eine Reparatur ausführen, nur weil ein historisches Ereignis oder ein importierter Bericht ein Problem erwähnt.

Diagnose und Berichtsanalyse laufen lokal. Eine Aktion, die Sie auswählen, kann ein externes Ziel kontaktieren, etwa Software-Downloads oder die Sicherung von Wiederherstellungsschlüsseln in Active Directory oder Entra ID. Prüfen Sie das Ziel vor der Bestätigung. Gespeicherte Erfassungen und Rohnachweise können Computeridentität und Quellpfade enthalten, prüfen Sie sie daher vor dem Weitergeben.

## Entwicklungsstand

Die Reparaturabdeckung wächst noch. Ein erfolgreicher Build oder eine simulierte Prüfung belegt kein natives Reparaturverhalten. Die Datei `VM-VALIDATION.md` im Repository verfolgt Szenarien auf wegwerfbaren Windows-Maschinen und die noch ausstehende Validierung.
