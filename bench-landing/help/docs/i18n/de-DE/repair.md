# Reparatur

Der Bereich Reparatur sammelt die Werkzeuge, die etwas beheben können. Jedes diagnostiziert zuerst. Eine Reparatur wird nur angeboten, wenn aktuelle Nachweise genau das Problem zeigen, das sie behebt, und sie läuft erst, nachdem Sie **Bestätigte Probleme automatisch reparieren** eingeschaltet oder eine einzelne Reparatur selbst bestätigt haben. Jede Reparatur wird im Journal festgehalten und anschließend überprüft.

Schritt-für-Schritt-Anleitungen: [Systemdateien reparieren](repair-system-files.html) und [Windows Update reparieren](fix-windows-update.html).

## Automatische und manuelle Reparatur

**Bestätigte Probleme automatisch reparieren** erlaubt einem Diagnosewerkzeug, nach der Prüfung die Reparaturen auszuführen, deren Notwendigkeit es nachweist. Die Option ist standardmäßig ausgeschaltet.

Für eine einzelne Wartungsreparatur öffnen Sie **Erweitert: Protokolle und manuelle Reparaturen** und wählen **Reparaturen**. Wählen Sie **Komponentenspeicher (DISM)** oder **Systemdateien (SFC)**, lesen Sie die Vorschau, markieren Sie **Diese Reparatur auf diesem PC ausführen** und drücken Sie **Reparatur ausführen**. Was vor und nach jeder Änderung geschieht, beschreibt [Wie Änderungen sicher bleiben](safety.html).

## Werkzeuge in diesem Bereich

Verwenden Sie diese Werkzeuge, nachdem eine Diagnose ein Problem erkannt hat. Reparaturen erfordern bestätigte Nachweise und eine ausdrückliche Genehmigung.

### Windows-Kerndienste

Prüft die Dienste Energie, Plug & Play, Benutzer-Manager, Gruppenrichtlinienclient, Netzwerkspeicher-Schnittstellendienst, RPC-Endpunktzuordnung, Remoteprozeduraufruf, DCOM-Server-Prozessstart, Infrastrukturdienst für Hintergrundaufgaben und CoreMessaging gemeinsam. Die optionale Wiederherstellung startet nur genau bezeichnete, aktivierte automatische Dienste, die laut aktuellen Nachweisen nach dem Startfenster und vollständigen Abhängigkeitsprüfungen angehalten sind. Dienste mit manuellem Start bleiben im Ruhezustand unverändert. Energiepläne, Geräte, Treiber, Konten, Anmeldeinformationen, Profile, Richtlinien, Adapter, Adressen, Routen, DNS, Firewallkonfiguration, Registrierungen, Endpunkte, Aufgabenregistrierungen, Anwendungsdaten, Dienstberechtigungen und Starttypen bleiben erhalten. Es werden keine Richtlinienaktualisierung, Gerätesuche, Kontoaktion, Netzwerkänderung, Änderung der Energieeinstellungen, Registrierungsänderung oder Aufgabenstart angefordert. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht das Verhalten der Funktion von Anfang bis Ende.

### Systemdateien

Erfasst die begrenzten Zustände von Windows-Wartungspaketen, optionalen Features und Funktionen, prüft Voraussetzungen, durchsucht geschützte Systemdateien und wählt SFC automatisch nur, wenn die Live-Prüfung eine Beschädigung bestätigt. Ausstehende Paket-, Feature- oder Funktionszustände verlangen eine Prüfung, berechtigen aber nicht zu Installation, Entfernung, Bereinigung, Rücksetzung, Aktivierung oder Deaktivierung.

### WMI-Repository

Prüft die Verfügbarkeit des WMI-Dienstes und dann die Konsistenz des Repositorys. Die automatische Reparatur kann einen geeigneten angehaltenen Dienst starten und eine bestätigte Beschädigung bergen und überprüft dann jedes Ergebnis. Deaktivierte Diensteinstellungen bleiben erhalten. Lesbare Registrierungen werden zusammengeführt; nicht lesbare Inhalte können eine Neuinstallation des Anbieters erfordern. Verwaltungsclients können unterbrochen werden. Kein Zurücksetzen, kein Löschen und kein automatisches Zurückrollen.

### Windows-Systembenachrichtigungen

Liest den aktuellen Verlauf des Dienststeuerungs-Managers und prüft dann das COM+-Ereignissystem und den Systemereignisbenachrichtigungsdienst gemeinsam. Die optionale Wiederherstellung überprüft die Bereitschaft des COM+-Ereignissystems, bevor SENS gestartet wird. Vorhandene Benachrichtigungen zu Anmeldung, Netzwerk, Energie und COM+ können mit der aktuellen Konfiguration fortgesetzt werden. COM+-Kataloge, Abonnements, Benachrichtigungsregistrierungen, Empfänger, Konten, Berechtigungen, Wiederherstellungseinstellungen, Richtlinien und Starttypen bleiben erhalten. Kein Abonnement, keine Registrierung, kein Empfänger, kein Konto, keine Berechtigung und keine Richtlinie wird geändert. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht die Zustellung von Benachrichtigungen von Anfang bis Ende.

### Diagnoserichtliniendienst

Prüft den begrenzten Betriebsverlauf der Diagnoserichtlinie, den genauen Verlauf von Dienstfehlern und die aktuelle Bereitschaft des Dienstes. Unvollständige Nachweise haben Vorrang. Historische Einträge berechtigen weder zu einer Dienstwiederherstellung noch zu einer Problembehandlungsaktion. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Diagnoseszenarien können fortgesetzt werden. Richtlinie, Problembehandlungsdaten und Starttyp bleiben erhalten. Es wird keine Problembehandlung und keine vorgeschlagene Korrektur ausgeführt. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Fehlererkennung, Abschluss der Diagnose oder Ergebnis einer Reparatur.

### Windows-Fehlerberichterstattung

Prüft die Konfiguration von Absturzabbildern, begrenzte Metadaten der Abbilddateien, aktuelle Absturzberichte des Systems, den genauen Verlauf von Fehlern des Dienstes Windows-Fehlerberichterstattung und die aktuelle Bereitschaft des Dienstes. Historische Berichte belegen weder einen aktuellen Fehler noch eine Ursache und berechtigen nicht zur Wiederherstellung. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Berichte in der Warteschlange können mit der aktuellen Richtlinie fortgesetzt werden. Berichtsarchive, Speicherabbilder, Zustimmung, Upload-Richtlinie und Starttyp bleiben erhalten. Es wird kein Upload und kein Löschen angefordert. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Erstellung, Upload oder Zustellung von Berichten, die Behebung eines Problems oder die Diagnose eines Absturzes.

### Windows-Ereignissammlung

Prüft den begrenzten Bestand an Abonnements der Windows-Ereignissammlung, den genauen Verlauf von Dienstfehlern und die aktuelle Bereitschaft des Dienstes. Ein leerer Bestand kann beabsichtigt sein, und weder die Zahl der Abonnements noch historische Fehler berechtigen zur Wiederherstellung. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Abonnements können die Remote-Ereignissammlung fortsetzen. Abonnements, weitergeleitete Ereignisse, Anmeldeinformationen, Kanaleinstellungen, Richtlinie und Starttyp bleiben erhalten. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Erreichbarkeit der Quellen, Authentifizierung, Remotezustellung, Aktualität der Ereignisse oder den Zustand von Abonnements.

### Windows-Ereignisprotokoll

Untersucht vier Diagnosekanäle auf Aktivierungsstatus, Aufbewahrungsmodus, aktuelle Größe, konfigurierte Obergrenze, Anzahl der Einträge und Auslastung und prüft dann die Verfügbarkeit des Dienstes Windows-Ereignisprotokoll. Die automatische Reparatur kann einen angehaltenen, aktivierten Dienst nach einer Abhängigkeitsprüfung starten und den laufenden Zustand überprüfen. Eine separat bestätigte gezielte Aktion kann genau einen deaktivierten, nicht verwalteten Diagnosekanal aktivieren und dabei jede andere gemeldete Einstellung beibehalten. Protokolle, Kanalberechtigungen, Aufbewahrung, Größenobergrenzen, Starttypen und verwaltete Einstellungen bleiben erhalten. Die erfolgreiche Zustellung von Ereignissen und die Archivkapazität bleiben ungeprüft.

### Windows-Zeitdienst

Prüft Konfiguration, Dienst und Synchronisierung der Windows-Zeit. Die automatische Reparatur kann W32Time starten, wenn der aktuelle Status genau den Fehler eines angehaltenen Dienstes meldet und aktuelle Prüfungen von Richtlinie, Modus und Abhängigkeiten bestanden sind, und fordert dann eine Neusynchronisierung an und überprüft den aktuellen Status. Dabei kann die Uhr mit den vorhandenen konfigurierten Quellen angepasst werden. Starttyp, Quellenkonfiguration und verwaltete Einstellungen bleiben erhalten.

### Aufgabenplanungsdienst

Erfasst den begrenzten Zustand geplanter Aufgaben, Laufzeitergebnisse und die Zahl verpasster Läufe und kombiniert sie mit dem genauen Verlauf von Fehlern des Aufgabenplanungsdienstes, bevor die aktuelle Verfügbarkeit des Dienstes geprüft wird. Befunde zu Aufgaben und historische Dienstfehler berechtigen nicht zum Start des Dienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur einen aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene aktivierte Aufgaben können unter ihren konfigurierten Konten starten, einschließlich verpasster Trigger. Keine Aufgabendefinition, kein Prinzipal, kein Verweis auf Anmeldeinformationen, keine Richtlinie und kein Starttyp wird geändert, und keine Aufgabe wird direkt gestartet oder gelöscht. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht den Erfolg von Aktionen, die Gültigkeit von Anmeldeinformationen oder künftige Aufgabenergebnisse.

### Benutzerprofildienst

Prüft die Verfügbarkeit des Benutzerprofildienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Anmelde- und Profilanfragen können fortgesetzt werden. Profilordner, Registrierungsstrukturen, Berechtigungen, Registrierungen in der Profilliste, Anmeldeinformationen, Benutzerdaten, Richtlinien, Dienstberechtigungen und Starttyp bleiben erhalten. Es wird kein Profilvorgang angefordert. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Anmeldung, Laden des Profils oder Zugriff auf Benutzerdaten.

### Sicherungsdienste

Prüft die Dienste Volumeschattenkopie, Microsoft-Softwareschattenkopie-Anbieter, Windows-Sicherung und Blockebenen-Sicherungsmodul und liest dann den Status der VSS-Writer, registrierte Anbietertypen, aktuelle Momentaufnahmen und die Zuteilung des Schattenkopiespeichers. Angehaltene Dienste mit manuellem Start können im Ruhezustand normal sein. Die optionale Wiederherstellung startet nach Abhängigkeitsprüfungen nur angehaltene automatische Dienste und überprüft dann den laufenden Zustand. Vorhandene geplante oder unterbrochene Sicherungen können fortgesetzt werden. Deaktivierte und manuelle Starteinstellungen, Momentaufnahmen, Schattenkopiespeicher, Anbieterregistrierungen, Sicherungspläne, Kataloge, Zeitpläne, Ziele, Anmeldeinformationen und Aufbewahrung bleiben erhalten. Es wird keine Sicherung ausgewählt oder umkonfiguriert, kein Dienst neu gestartet und keine Momentaufnahme oder kein Katalog gelöscht. Anbieterregistrierung, Writer-Status, Speicherzuteilung und stabiler Dienstzustand belegen weder den Abschluss einer Sicherung noch den Zugriff auf das Ziel oder den Erfolg einer Wiederherstellung.
