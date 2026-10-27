# Diagnose

Mit der Diagnose sollte jeder Einsatz beginnen. Jedes Werkzeug liest den Computer und schreibt einen Bericht. Hier wird nichts an Windows geändert. Das Standardwerkzeug, die vollständige Systemprüfung, führt eine breite Reihe schreibgeschützter Prüfungen aus und ordnet, was Aufmerksamkeit braucht.

Schritt-für-Schritt-Anleitungen: [Abstürze und Bluescreens untersuchen](investigate-crashes.html) und [Systemdateien reparieren](repair-system-files.html).

## Werkzeuge in diesem Bereich

Prüfen Sie zuerst den Computer und lesen Sie dann im Bericht, was Aufmerksamkeit braucht und was der nächste sichere Schritt ist.

### Vollständige Systemprüfung

Prüft 49 Diagnosebereiche und bewertet die installierten Reparaturrezepte anhand aktueller Nachweise. Die Prüfungen können einige Minuten dauern. Die vollständige Systemprüfung dient nur der Diagnose. Verwenden Sie einen gezielten Reparatur-Workflow, um nach Prüfung aktueller Voraussetzungen eine unterstützte Aktion auszuwählen und zu bestätigen. Verlaufs-, Sicherungs- und Übertragungsmomentaufnahmen bleiben schreibgeschützt. Inhalte von Speicherabbildern werden nicht geöffnet. Die Prüfung des Dirty-Bits plant weder eine Reparatur noch einen Neustart. Diese Prüfung installiert keine Updates, ändert keine Treiber, löscht keine Dateien und führt keinen vollständigen Hardwaretest durch.

### Vitalwerte

Erfasst verfügbaren Arbeitsspeicher, vom Anbieter gemeldeten Datenträgerzustand und den BitLocker-Schutzstatus und setzt die aktuellen Vitalwerte dann mit Geräteproblemen, Zuverlässigkeitsverlauf, Hardwarefehlereinträgen, Absturznachweisen, unerwartetem Herunterfahren und Ergebnissen der Windows-Speicherdiagnose in Beziehung. Neustartanzeigen, freier Speicher auf dem Systemdatenträger, Erhöhung und die Konfiguration des Update-Dienstes bleiben sichtbar. Wiederherstellungskennwörter werden nicht erfasst. In diesem Workflow laufen keine Reparaturen. Das zusammengefasste Ergebnis ist kein vollständiger Hardware-, Software- oder Stabilitätstest.

### Zuverlässigkeitsverlauf

Liest die Einträge der Windows-Zuverlässigkeitsüberwachung der letzten 14 Tage zusammen mit aktuellen Anwendungsfehlern und Ereignissen zu unerwartetem Herunterfahren und liefert eine begrenzte historische Schlussfolgerung. Historische Einträge belegen weder den aktuellen Zustand noch eine Ursache. Es wird keine Reparatur ausgewählt, und keine Anwendungen, Dienste oder Einstellungen werden geändert.

### Startleistung

Liest die Metadaten der Windows-Ereignisse zur Start- und Herunterfahrleistung der letzten 14 Tage, behält nur die Anbieterkennung und begrenzte numerische Dauerfelder und setzt sie mit unterstützten Anzeigen für ausstehende Neustarts in Beziehung. Pfade, Prozessnamen, Konten und andere Ereignisdaten werden entfernt, bevor Nachweise gespeichert werden. Historische Leistungseinbußen bezeichnen keinen aktuellen Engpass und berechtigen nicht zu Änderungen an Start, Diensten, Anwendungen, Treibern, Aufgaben, Energie, Neustart oder Richtlinien.

### Hardwarefehlerverlauf

Liest den Verlauf der Windows Hardware Error Architecture und der Speicherdiagnose der letzten 14 Tage, aktuelle Plug-and-Play-Geräteprobleme, den Zustand physischer Datenträger und die Zuverlässigkeitszähler der Anbieter und liefert eine begrenzte Hardware-Schlussfolgerung. Die Einträge müssen mit ihren Daten, der Firmwarediagnose und den Hardwaretests des Herstellers abgeglichen werden. Es wird weder eine defekte Komponente noch eine Ursache abgeleitet, keine Reparatur ausgewählt, und keine Hardware, Firmware, Treiber, Datenträger, Pools, Volumes, Dateien, Dienste, Energieeinstellungen, Neustarts oder Einstellungen werden geändert.

### Treiberinstallationsverlauf

Liest das aufbewahrte Ende des Windows-SetupAPI-Verlaufs der Geräteinstallationen, ordnet fehlgeschlagene Abschnitte genauen aktuellen Plug-and-Play-Identitäten zu und liefert eine begrenzte Schlussfolgerung. Historische Protokollabschnitte belegen nicht, dass ein Gerät noch vorhanden ist oder ein Fehler aktuell besteht. Kein Treiber wird installiert, entfernt, zurückgesetzt oder aktualisiert.

### Systemwiederherstellungspunkte

Listet bis zu 256 verfügbare Systemwiederherstellungspunkte, prüft die ausdrückliche Eignung zum Erstellen und liefert eine begrenzte Bereitschafts-Schlussfolgerung. Die Bestandsaufnahme belegt nicht, dass der Schutz für jedes Volume aktiviert ist oder eine Wiederherstellung gelingt. Während der Diagnose wird kein Wiederherstellungspunkt erstellt, gelöscht oder angewendet.

### Windows-Wiederherstellungsumgebung

Liest die aktuelle Konfiguration der Windows-Wiederherstellungsumgebung mit REAgentC, ordnet einen aktivierten lokalen Speicherort dem genauen aktuellen Datenträger und der Partition zu und liefert eine begrenzte Bereitschafts-Schlussfolgerung. Die optionale automatische Reparatur kann eine deaktivierte vorhandene Konfiguration aktivieren und dann ihren gemeldeten Zustand überprüfen. Das Wiederherstellungsabbild wird weder heruntergeladen noch verschoben oder ersetzt, kein Wiederherstellungsstart wird ausgelöst, und Windows wird nicht neu gestartet. Aktivierter Status und vorhandene Partition belegen nicht, dass das Wiederherstellungsabbild diesen PC starten oder reparieren kann.

### Windows-Aktivierung

Liest den aktuellen Windows-Lizenzstatus, ohne vollständige oder teilweise Produktschlüssel zu erfassen, behält die bereinigten Metadaten der Warn-, Fehler- und kritischen Ereignisse des Softwareschutzes der letzten sieben Tage und liefert eine begrenzte betriebliche Schlussfolgerung. Ereignisdaten werden entfernt, bevor Nachweise gespeichert werden. Historische Einträge bleiben Kontext, wenn die aktuelle Lizenzierung „Lizenziert“ meldet. Es wird keine Aktion für Aktivierung, Produktschlüssel, Token, Lizenz, Konto, Dienst oder Richtlinie ausgewählt. Anbieterstatus und Ereignisverlauf belegen weder den Anspruch aus einem Abonnement noch die Erreichbarkeit des Aktivierungsservers, die Integrität des Tokenspeichers, die künftige Gültigkeit oder eine Ursache.

### Windows-Dienste

Erfasst installierte Dienstnamen, Anzeigenamen, Zustände, Startmodi und datenschutzbegrenzte Typen von Wiederherstellungsaktionen, ohne Konten, ausführbare Pfade, Wiederherstellungsbefehle oder Neustartmeldungen zu sammeln. Prüft die Stimmigkeit der Startmodi, direkte Abhängigkeitsidentitäten und Zyklen, wiederholt genaue ausstehende Zustände, untersucht unterstützte Dienste und die Abhängigkeiten angehaltener Dienste und setzt aktuelle Fehler des Dienststeuerungs-Managers nur dann in Beziehung, wenn genau ein Dienstname oder ein eindeutiger Anzeigename vorhanden ist. Die Verwendung von Ladereihenfolgegruppen wird gezählt, ohne die Mitgliedschaft aufzulösen, und mehrdeutige Identitäten bleiben ungeklärt. Angehaltene Dienste mit manuellem Start können normal sein. Kein Dienst wird gestartet, und Start-, Abhängigkeits- und Wiederherstellungseinstellungen bleiben erhalten. Verwenden Sie den passenden Reparatur-Workflow, wenn eine bestimmte Funktion wiederhergestellt werden muss.

### Anwendungsfehler

Liest aktuelle Fehler- und kritische Ereignisse des Anwendungsprotokolls zusammen mit dem begrenzten Verlauf der Zuverlässigkeitsüberwachung und liefert eine Schlussfolgerung zum Stabilitätsverlauf. Historische Einträge belegen weder einen aktuellen Fehler noch eine Ursache und lenken nur auf eine engere Diagnose. Keine Anwendungen werden beendet, repariert, entfernt oder umkonfiguriert.

### Anzeigegeräte

Untersucht Grafikkarten und Monitore auf gemeldete Gerätefehler, Hardware-IDs und installierte Treiberversionen. Keine Treiberinstallation, kein Zurücksetzen von Geräten und keine Änderung der Auflösung. Bildqualität und physische Verbindungen werden nicht getestet.

### Eingabegeräte

Untersucht Gerätefehler und Treiberbestand von Tastatur, Maus und HID-Geräten. Es werden keine Tastenanschläge erfasst, Geräte zurückgesetzt oder Treiber geändert. Die physische Bedienung der Eingabegeräte wird nicht getestet.

### Geräteprobleme

Findet Plug-and-Play-Konfigurationsfehlercodes ungleich null in allen Geräteklassen und setzt dann die Nachweise vorhandener Geräte mit dem begrenzten Verlauf von SetupAPI-Treiberinstallation, Geräteeinrichtung und Plug and Play in Beziehung. Historische Einträge allein belegen keinen aktuellen Fehler. Prüfen Sie die genaue Geräteidentität und freigegebene Treibernachweise, ohne Hardware, Treiber oder Konfiguration zu ändern. Fehlende Geräte und die physische Funktion werden nicht überprüft.
