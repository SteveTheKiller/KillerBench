# Geräte

Der Bereich Geräte umfasst Drucker, Audio, Kameras, Bluetooth, Eingabe und die Windows-Dienste, auf die angeschlossene Hardware angewiesen ist.

Schritt-für-Schritt-Anleitungen: [Druckprobleme beheben](fix-printing.html) und [Abstürze und Bluescreens untersuchen](investigate-crashes.html).

## Werkzeuge in diesem Bereich

Drucken, Audio, Kameras, Bluetooth, Eingabe und die Dienste hinter angeschlossener Hardware.

### Drucken

Prüft die Druckwarteschlange (Spooler), angehaltene lokale Warteschlangen, sichtbare Druckaufträge, Offlineeinstellungen und vom Treiber gemeldete Zustände und erfasst dann die Registrierungen von Warteschlangen, Treibern, Anschlüssen, Standard-TCP/IP-Endpunkten, Druckprozessoren und Druckmonitoren. Aktuelle PrintService-Probleme werden nur zugeordnet, wenn genau eine aktuelle Warteschlangenidentität vorliegt. Zugewiesene Standard-TCP/IP-Endpunkte erhalten einen begrenzten Verbindungsversuch ohne Anwendungsdaten, SNMP oder Druckauftrag. Die automatische Reparatur kann den aktivierten Spooler starten, nur genau bezeichnete angehaltene Warteschlangen mit aktuellen Nachweisen wartender Aufträge fortsetzen und den Offlinemodus aufheben. Absichtliche Pausen ohne wartende Aufträge bleiben reine Prüfpunkte. Remoteverbindungen, SNMP-Community-Zeichenfolgen, Erweiterungsregistrierungen und Auftragsinhalte bleiben erhalten. Ein Zustandscode oder eine erfolgreiche Verbindung belegt weder die Identität des Druckers noch Verbrauchsmaterial, Mechanik, Annahme durch die Warteschlange, erfolgreiches Rendern oder den physischen Druck.

### Audio

Prüft die Identitäten und gemeinsamen Formate der Standardgeräte für Multimedia-Ein- und -Ausgabe, den Zustand von Audiogeräten und Treibern, die Windows-Audio-Endpunkterstellung und Windows-Audio. Die automatische Reparatur startet angehaltene, aktivierte Dienste und überprüft den laufenden Zustand. Starteinstellungen, Treiber, ausgewählte Geräte, exklusiver Modus und Lautstärkepegel bleiben erhalten. Wiedergabe, Aufnahme und Audioweiterleitung von Anwendungen bleiben ungetestet.

### Kameraunterstützung

Untersucht Fehler von Kamera- und Bildverarbeitungsgeräten, Hardware-IDs, installierte Treibernachweise und die Verfügbarkeit des Windows-Kamera-Frameserverdienstes. Die optionale Wiederherstellung startet nach Abhängigkeitsprüfungen nur einen aktivierten, angehaltenen Dienst. Eine Anwendung, die bereits Kamerazugriff anfordert, kann mit den vorhandenen Berechtigungen fortfahren. Datenschutzeinstellungen der Kamera, Berechtigungen, Gerätekonfiguration, Richtlinie, Treiber und Starttyp bleiben erhalten. Kein Zurücksetzen, keine Berechtigungsänderung und keine Treiberinstallation. Die Überprüfung umfasst nur die gemeldete Gerätekonfiguration und den laufenden Zustand des Dienstes, nicht Bildaufnahme, Datenschutzzugriff oder physische Funktion.

### Bluetooth-Unterstützung

Untersucht Fehler von Bluetooth-Geräten, Hardware-IDs, installierte Treibernachweise und die Verfügbarkeit des Bluetooth-Unterstützungsdienstes. Die optionale Wiederherstellung startet nach Abhängigkeitsprüfungen nur einen aktivierten, angehaltenen Dienst; gekoppelte Geräte können sich erneut verbinden. Kopplungen, Funkeinstellungen, Gerätekonfiguration, Richtlinie und Starttyp bleiben erhalten. Kein Zurücksetzen, kein Entfernen von Geräten und keine Treiberinstallation. Die Überprüfung umfasst nur die gemeldete Gerätekonfiguration und den laufenden Zustand des Dienstes, nicht Erkennung, Kopplung, Audio oder Datenübertragung.

### Smartcard-Unterstützung

Untersucht Fehler von Smartcard-Lesern, Hardware-IDs, installierte Treibernachweise und die Verfügbarkeit des Smartcard-Dienstes. Die optionale Wiederherstellung startet nach Abhängigkeitsprüfungen nur einen aktivierten, angehaltenen Dienst. Anwendungen können unter der vorhandenen Richtlinie wieder eingesteckte Karten erkennen. Zertifikate, PINs, Anmeldeinformationen, Konfiguration von Leser und Karte, Richtlinie, Treiber und Starttyp bleiben erhalten. Keine Initialisierung, keine Registrierung, kein PIN-Vorgang, kein Zurücksetzen des Lesers und keine Treiberinstallation. Die Überprüfung umfasst nur die gemeldete Leserkonfiguration und den laufenden Zustand des Dienstes, nicht Kartenerkennung, Authentifizierung oder Anwendungszugriff.

### Biometrieunterstützung

Untersucht Fehler biometrischer Geräte, Hardware-IDs, installierte Treibernachweise und die Verfügbarkeit des Windows-Biometriedienstes. Die optionale Wiederherstellung startet nach Abhängigkeitsprüfungen nur einen aktivierten, angehaltenen Dienst. Vorhandene Anfragen von Windows Hello und Anwendungen können unter der aktuellen Richtlinie fortgesetzt werden. Biometrische Vorlagen, Windows Hello-Daten, Anmeldeinformationen, Gerätekonfiguration, Berechtigungen, Richtlinie, Treiber und Starttyp bleiben erhalten. Keine Registrierung, kein Löschen von Vorlagen, kein Zurücksetzen des Sensors, keine Berechtigungsänderung und keine Treiberinstallation. Die Überprüfung umfasst nur die gemeldete Sensorkonfiguration und den laufenden Zustand des Dienstes, nicht Erfassung, Abgleich, Anmeldung oder Anwendungszugriff.

### Eingabegerätedienst (HID)

Prüft die Verfügbarkeit des Dienstes für Eingabegeräte (Human Interface Device). Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Schnelltasten, Mediensteuerungen und andere HID-Funktionen können fortgesetzt werden. Geräte, Treiber, Tastenbelegungen, Einstellungen der Barrierefreiheit, Eingabeeinstellungen, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Vorgang an Geräten, Treibern, Belegungen oder Eingabeeinstellungen. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht das Verhalten von Tasten, Steuerungen oder Anwendungen.

### Enumeratordienst für tragbare Geräte

Prüft die Verfügbarkeit des Enumeratordienstes für tragbare Geräte. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Anwendungen können angeschlossene tragbare Geräte wieder erkennen und darauf zugreifen. Geräte, Treiber, Dateien, Bibliotheken, Synchronisierungseinstellungen, Berechtigungen, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Vorgang an Geräten, Dateien, Bibliotheken, Synchronisierung oder Übertragung. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Aufzählung, Zugriff, Synchronisierung oder Übertragung.

### Windows-Bilderfassung (WIA)

Prüft die Verfügbarkeit des Dienstes Windows-Bilderfassung. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Anfragen zur Erfassung von Scanner- und Standbildern können mit den aktuellen Anwendungsberechtigungen fortgesetzt werden. Geräte, Treiber, Bilddaten, Anwendungsberechtigungen, Datenschutzeinstellungen, Übertragungseinstellungen, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Vorgang an Geräten, Treibern, Bildern, Berechtigungen, Übertragung oder Erfassung. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht den Betrieb von Scanner, Kamera, Übertragung oder Anwendung.

### Shellhardwareerkennung

Untersucht begrenzte Plug-and-Play-Nachweise von Speichercontrollern, Datenträgern und tragbaren Geräten sowie installierte Treiberverweise und prüft dann die Verfügbarkeit des Dienstes Shellhardwareerkennung. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Benachrichtigungen zu automatischer Wiedergabe und neu angeschlossener Hardware können unter der aktuellen Richtlinie fortgesetzt werden. Gerätebeobachtungen und aufbewahrter Verlauf berechtigen nicht zur Wiederherstellung. Geräte, Treiber, Volumes, Bereitstellungspunkte, Einstellungen der automatischen Wiedergabe, Handler, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. Es wird kein Geräte- oder Einstellungsvorgang angefordert. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht die Zustellung von Benachrichtigungen, Medienbereitschaft, Volumezugriff oder die physische Gerätefunktion.

### Geräteinstallationsdienst

Prüft die Verfügbarkeit des Geräteinstallationsdienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Von Windows angeforderte Geräteinstallationen können unter der aktuellen Richtlinie fortgesetzt werden. Geräte, Treiber, Pakete, Konfiguration, Bindungen, Klasseneinstellungen, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt und installiert nichts. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Installation oder Betrieb von Geräten.

### Gerätezuordnungsdienst

Prüft den aktuellen Verlauf von Geräteeinrichtung und Plug and Play und dann die Verfügbarkeit des Gerätezuordnungsdienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Von Windows angeforderte Gerätezuordnungen können fortgesetzt werden. Gekoppelte und zugeordnete Geräte, Treiber, Anmeldeinformationen, Metadaten, Berechtigungen, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Vorgang für Geräte, Kopplung, Entfernen, Zurücksetzen oder Treiber. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Erkennung, Kopplung, Zuordnung oder Gerätebetrieb.

### Anzeigeverbesserungsdienst

Untersucht Nachweise von Grafikkarten und Monitoren und prüft dann die Verfügbarkeit des Anzeigeverbesserungsdienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Anfragen zu Helligkeit, Farbe und Verbesserungen können fortgesetzt werden. Anzeigekonfiguration, Helligkeit, Farbprofile, HDR-Einstellungen, Geräte, Treiber, Energieeinstellungen, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. Keine Anzeigeeinstellung wird geändert. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht das Verhalten der Verbesserungen, die Bildqualität oder den physischen Betrieb der Anzeige.

### Texteingabeverwaltungsdienst

Untersucht Nachweise von Tastatur, Maus und HID-Geräten und prüft dann die Verfügbarkeit des Texteingabeverwaltungsdienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Anfragen für Bildschirmtastatur, Handschrift, Emoji und erweiterte Texteingabe können fortgesetzt werden. Wörterbücher, Handschriftdaten, Sprach- und Tastatureinstellungen, Barrierefreiheits- und Datenschutzeinstellungen, Geräte, Treiber, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. Es werden keine Texteingaben, Tastenanschläge oder Handschriftinhalte erfasst. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Texteingabe, Erkennung oder Gerätebetrieb.

### Windows-Schriftartencache-Dienst

Prüft die Verfügbarkeit des Windows-Schriftartencache-Dienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Anwendungen können den aktuellen Schriftartencache wieder verwenden. Installierte Schriftarten, Schriftartdateien, Anwendungsdaten, Cachedateien, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench installiert oder entfernt keine Schriftart und löscht oder erstellt den Cache nicht neu. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Textdarstellung oder Anwendungsverhalten.

### Funktionssuche-Anbieterhost

Prüft die Verfügbarkeit des Funktionssuche-Anbieterhosts. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Anwendungen können Netzwerkgeräte wieder über installierte Anbieter erkennen. Netzwerkprofile, Freigaben, Geräte, Anbieter, Firewallregeln, Erkennungseinstellungen, Anmeldeinformationen, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Vorgang an Geräten, Freigaben, Anbietern, Firewallregeln oder Erkennungseinstellungen. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht den Zustand der Anbieter, die Geräteerkennung oder die Konnektivität.

### Funktionssuche-Ressourcenveröffentlichung

Prüft die Verfügbarkeit der Funktionssuche-Ressourcenveröffentlichung. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Dieser PC kann über die vorhandenen Einstellungen für Netzwerkerkennung und Firewall auffindbar werden. Netzwerkprofile, Freigaben, veröffentlichte Ressourcen, Firewallregeln, Erkennungseinstellungen, Anmeldeinformationen, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Vorgang an Freigaben, Ressourcen, Firewallregeln oder Erkennungseinstellungen. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Veröffentlichung, Sichtbarkeit, Zugriff oder Konnektivität.
