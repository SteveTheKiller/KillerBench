# Speicher

Untersucht Datenträger, Volumes, Speicherplätze und die Wartungsdienste für Laufwerke. Die Diagnose wählt keinen Datenträgervorgang aus; jede unterstützte Aktion, die den Computer verändert, benötigt ein eigenes genaues Ziel, eigene Voraussetzungen und eine eigene Bestätigung.

Schritt-für-Schritt-Anleitungen: [Laufwerk prüfen und reparieren](check-a-drive.html) und [Speicherplatz freigeben](free-disk-space.html).

## Werkzeuge in diesem Bereich

### Speicher

Ordnet begrenzte Identitäten von Datenträgern, Partitionen und Volumes mit Laufwerksbuchstaben zu, bewertet Speicherknappheit unter 10 % freiem Speicher auf jedem Volume mit Laufwerksbuchstaben, untersucht die Zuteilung des VSS-Schattenkopiespeichers und die Kontingentstandards pro Volume, fasst den Fortschritt aktueller Aufträge des Storage-Moduls zusammen, prüft das gewählte NTFS-Volume und seinen Selbstheilungszustand im laufenden Betrieb und untersucht Datenträger- und Controllergeräte auf diesem PC. Kapazitätsschwellen, Kontingenteinstellungen und Auftragszustände sind Nachweise zur Prüfung und wählen keine Bereinigung, Größenänderung, Kontingentänderung oder Auftragssteuerung aus. Die Zuordnung der Laufwerksbuchstaben ist eine Momentaufnahme und umfasst nicht jeden Bereitstellungspfad. Gerätebefunde werden nicht dem gewählten Volume zugeordnet. Das Windows-Volume ist die Standardauswahl. Keine Dateien, Momentaufnahmen, Aufträge oder Treiber werden geändert, keine Einstellung wird geändert, kein Volume wird getrennt, und kein Neustart wird geplant.

### Speicherplätze

Erfasst nicht primordiale Speicherpools und virtuelle Datenträger von Speicherplätzen, einschließlich Zustand, Betriebszustand, Kapazität, Zuteilung und Resilienz, und liefert eine begrenzte betriebliche Schlussfolgerung. Windows kann seinen Verwaltungsanbieter für Speicherplätze bei Bedarf starten, während diese schreibgeschützte Abfrage beantwortet wird. Pools, virtuelle Datenträger, physische Datenträger, Volumes, Resilienz, Zuteilung, Wartungseinstellungen, Richtlinie und Starttyp bleiben erhalten. Es wird kein Vorgang für Pool, Datenträger, Volume, Zuteilung, Anfügen, Reparatur, Optimierung oder Einstellung angefordert. Anbieterergebnisse belegen weder die Zugänglichkeit des Pools noch Redundanz, Datenintegrität, Wiederherstellbarkeit von Sicherungen oder die E/A von Anwendungen.

### Speicherdienst

Prüft begrenzte aktuelle Aufträge des Storage-Moduls, den genauen Verlauf von Dienstfehlern und die aktuelle Bereitschaft des Speicherdienstes. Unvollständige Nachweise haben Vorrang. Auftragszustand und historische Fehler berechtigen weder zur Dienstwiederherstellung noch zu Speicheränderungen. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Speicherbenachrichtigungen und richtliniengesteuerte Speicheraktivität können fortgesetzt werden. Volumes, Partitionen, Dateisysteme, Bereitstellungspunkte, Geräte, Treiber, Einstellungen der Speicheroptimierung, Wartungsrichtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Vorgang an Aufträgen, Volumes, Partitionen, Bereitstellungspunkten, Dateien, Geräten, Treibern oder Speichereinstellungen. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Auftragsergebnis, Geräte-, Volume- oder Speicherrichtlinienbetrieb.

### Dienst zum Optimieren von Laufwerken

Prüft die Verfügbarkeit des Dienstes zum Optimieren von Laufwerken. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene, von Windows geplante Optimierungen können unter der aktuellen Wartungsrichtlinie fortgesetzt werden. Volumes, Dateisysteme, Daten, Optimierungszeitpläne, Wartungsrichtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt kein Volume aus und analysiert, trimmt, defragmentiert, optimiert oder plant kein Laufwerk. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Analyse, Optimierung oder Gerätezustand.
