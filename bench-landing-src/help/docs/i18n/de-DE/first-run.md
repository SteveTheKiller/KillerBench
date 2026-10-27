# Ihre erste Diagnose

Beginnen Sie mit [Download und Einrichtung](setup.html) und führen Sie dann eine vollständige Systemprüfung aus. Sie sammelt Diagnosenachweise und speichert ein lokales Journal; Reparaturen führt sie nicht aus. Ein Befund beschreibt den geprüften Bereich, nicht jeden möglichen Fehler des Computers.

## Prüfung ausführen

1. Öffnen Sie KillerBench. Der Bereich **Diagnose** öffnet sich mit ausgewählter **Vollständige Systemprüfung**.
2. Lassen Sie **Bestätigte Probleme automatisch reparieren** beim ersten Durchlauf ausgeschaltet.
3. Drücken Sie **Diagnose ausführen**. Der Fortschritt erscheint rechts in der **Rohausgabe**, während jede Prüfung läuft.
4. Danach wechselt der Ausgabebereich zur Ansicht **Lesbarer Bericht**.

Starten Sie ohne Erhöhung. Manche Windows-Nachweise, etwa BitLocker-Schutzvorrichtungen und geschützte Dienste, erfordern Administratorrechte. Diese Prüfungen melden unter einem Standardkonto nicht verfügbare Nachweise. Wenn Sie diese Nachweise brauchen, schließen Sie den aktuellen Durchlauf ab, behalten Sie den Bericht und öffnen Sie KillerBench dann bewusst mit **Als Administrator ausführen** erneut. Die Erhöhung wählt keine Reparatur aus und genehmigt keine.

## Bericht lesen

Der Bericht beginnt mit den Zählern für **Zu prüfen**, **Überprüfte Reparaturen**, **Unvollständig** und **Geprüft** und nennt dann einen empfohlenen nächsten Schritt.

- **Was Aufmerksamkeit braucht** listet die Probleme auf, die die Nachweise stützen, das wichtigste zuerst.
- **Gesammelte Informationen** zeigt, was gelesen wurde, einschließlich unvollständiger oder nicht verfügbarer Nachweise.
- Historische Ereignisse sind als Verlauf gekennzeichnet. Ein Fehler vom letzten Monat gilt nicht als aktueller Fehler.

Fehlende Nachweise gelten nie als fehlerfrei. Wenn eine Prüfung nicht laufen konnte, sagt der Bericht das.

## Prüfungen selbst auswählen

Öffnen Sie **Prüfungen auswählen (erweitert)**, um statt der vollständigen Prüfung einzelne Prüfungen zu wählen. Jeder Bereich der App hat außerdem gezielte Werkzeuge, die auf den Seiten unter **Bereiche** in diesem Handbuch beschrieben sind.

## Das Gefundene beheben

Wenn der Bericht eine Reparatur empfiehlt, nennt er den Bereich und das Werkzeug, das Sie öffnen sollen. Lesen Sie vor Ihrer ersten Reparatur [Wie Änderungen sicher bleiben](safety.html). Die native Validierung läuft noch; eine erfolgreiche Diagnose belegt nicht, dass jede angebotene Reparatur auf Ihrem Windows-Build getestet wurde. Halten Sie Sicherungen bereit und prüfen Sie vor jeder Änderung das genaue Ziel und die Folgen.
