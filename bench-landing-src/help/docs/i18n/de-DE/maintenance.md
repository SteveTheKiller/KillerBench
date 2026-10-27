# Wartung

Der Bereich Wartung hält einen Computer aufgeräumt: Dateibereinigung, alte Benutzerprofile, geplante Aufgaben und das Entrümpeln von Windows. Jedes Werkzeug zeigt vorab, was es entfernen würde, bevor etwas gelöscht wird.

Schritt-für-Schritt-Anleitungen: [Speicherplatz freigeben](free-disk-space.html) und [Windows datenschutzfreundlicher machen](make-windows-private.html).

## Werkzeuge in diesem Bereich

Dateien, Profile und geplante Aufgaben bereinigen und Windows entrümpeln.

### Bereinigung

Schätzt ausgewählte Ziele für temporäre Dateien, Caches und aufbewahrte Diagnosenachweise anhand der Alters- und Dateigrenzen im Hauptbereich. Windows Temp und Windows SystemTemp sind unabhängige Ziele. Die Übermittlungsoptimierung ist eine eigene Cacheauswahl, und entfernte Inhalte müssen möglicherweise erneut heruntergeladen werden. Das optionale dauerhafte Entfernen verwendet das separate Bereinigungskontrollkästchen, prüft aufbewahrte Kandidaten innerhalb der gewählten Grenze erneut und hält jedes Ergebnis fest. Das Löschen umgeht den Papierkorb. Ziele, Alter und Dateigrenzen lassen sich auch über den CLI-Befehl `cleanup` einstellen.

### Profile

Prüft Windows-Profile anhand der gewählten Altersgrenze, genauer ProfileList-Registrierungen und optionaler Kontoabhängigkeiten von Diensten und geplanten Aufgaben und liefert eine begrenzte Prüfschlussfolgerung. Geladene Profile, Systemprofile und das Profil des aktuellen Kontos sind von der Eignung ausgeschlossen. Kandidaten bleiben reine Prüfpunkte. Kein Profil, kein Ordner, kein Registrierungsschlüssel, kein Konto, kein Dienst, keine Aufgabe, keine Anmeldeinformation, keine Richtlinie und keine Datei wird geändert, und kein Entfernen wird ausgewählt. Anwendungsabhängigkeiten und die Wiederherstellbarkeit von Sicherungen bleiben ungeprüft.

### Geplante Aufgaben

Erfasst sichtbare geplante Aufgaben, ihre Zustände und die gemeldeten Prinzipale. Es werden keine Aufgaben gestartet, registriert oder geändert. Aktionen, Trigger und der Ausführungserfolg werden nicht überprüft.

### Windows entrümpeln

Prüft ausdrücklich gewählte Windows-Bereiche wie Recall und KI, Telemetrie, Suchvorschläge, Verbraucherinhalte, Taskleiste und Startmenü, Benachrichtigungen, Edge, Energie, Ruhezustand, Office-Sprachen und vorinstallierte Software für das ausführende Konto oder alle Profile. Die Prüfung ist schreibgeschützt. Höchstleistung, Zeitgeber für Energiesparmodus und Bildschirm sowie der Ruhezustand lassen sich jeweils ändern, wenn dieser Bereich die einzige Auswahl ist, mit aktuellen Nachweisen und ausdrücklicher Bestätigung.
