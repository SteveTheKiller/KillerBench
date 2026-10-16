# Updates

Der Bereich Updates umfasst Windows Update, die Dienste dahinter und den Komponentenspeicher. Sie sehen, was installiert ist, was aussteht und warum ein Update fehlschlägt, und Sie können verfügbare Updates prüfen, bevor Sie eines zur Installation auswählen.

Schritt-für-Schritt-Anleitungen: [Windows Update reparieren](fix-windows-update.html) und [Systemdateien reparieren](repair-system-files.html).

## Werkzeuge in diesem Bereich

Wartungsaktivität beobachten, verfügbare Updates prüfen und die Windows Update-Kette reparieren.

### Wartungsaktivitätsmonitor

Beobachtet die begrenzte Wartungsaktivität von Windows Update und Setup über eine oder mehrere Momentaufnahmen. Optionen steuern das Aktualisierungsintervall und ob Update-Ereignisse, Update-Verlauf, die genaue geplante Windows Update-Aufgabe, genaue Dienstzustände, Neustartanzeigen, Hintergrundübertragungen des aktuellen Kontos, Aktivität der Übermittlungsoptimierung und datenschutzbegrenzte Windows Installer-Aktivität einbezogen werden. Prozess-IDs, Befehlszeilen, Benutzer und Pfade sind ausgeschlossen. Aktivität belegt weder Fortschritt noch Abschluss, Stillstand oder Fehler. Es wird keine Aktion für Update, Setup, Übertragung, Aufgabe, Dienst, Prozess, Richtlinie, Neustart oder Reparatur angefordert.

### Verfügbare Updates

Findet ausgewählte Software- und Treiberupdates über die konfigurierte Quelle, Windows Update oder einen bereits registrierten Microsoft Update-Dienst. Die Bereitschaft der Quelle wird vor der Suche geprüft. Keine Installation, keine Annahme von Lizenzen, keine Registrierung von Quellen und keine Richtlinienänderung. Dies ist getrennt von der Wartungsreparatur.

### Windows Update

Erfasst die begrenzten Zustände von Windows-Wartungspaketen, optionalen Features und Funktionen, prüft Neustartanzeigen, freien Speicher und Update-Dienste, beobachtet die Übermittlungsoptimierung, ohne sie zu starten, und durchsucht dann den Komponentenspeicher und geschützte Systemdateien. Ausstehende Paket-, Feature- oder Funktionszustände verlangen eine Prüfung, berechtigen aber nicht zu einer Komponentenaktion. Die automatische Reparatur kann die aktivierten Dienste Windows Update, BITS, Kryptografiedienste, Windows Modules Installer und Update Orchestrator starten. Der Start von Update Orchestrator kann vorhandene geplante Update-Aktivität unter der aktuellen Richtlinie fortsetzen. Pakete, optionale Features, Funktionen, Cache, Peers, Bandbreite und Richtlinie der Übermittlungsoptimierung, Quellenregistrierung, Zeitpläne und Starteinstellungen bleiben erhalten. DISM oder SFC erfordert Live-Nachweise einer Beschädigung. Dieser Workflow wählt oder genehmigt kein Update.

### Windows Update-Dienst

Prüft den genauen Zustand des Windows Update-Dienstes, seine registrierte Dienst-DLL, die begrenzte aktuelle Update-Aktivität und den genauen Verlauf von Dienstfehlern. DLL-Beobachtungen, Update-Aktivität und historische Fehler berechtigen weder zum Start des Dienstes noch zu einem Update-Vorgang. Die optionale Wiederherstellung startet nach aktuellen Prüfungen von Abhängigkeiten und Registrierung der ausführbaren Datei nur einen angehaltenen automatischen Dienst. Ein angehaltener Dienst mit manuellem Start bleibt eine Information. Vorhandene Update-Aktivität kann unter der aktuellen Richtlinie fortgesetzt werden. Update-Richtlinie, Quellenregistrierung, Zeitpläne, Neustarteinstellungen, Pakete, BITS-Aufträge, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt, genehmigt oder installiert kein Update. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Erreichbarkeit der Quelle, Anwendbarkeit, Download, Installation, Abschluss oder Neustartbereitschaft.

### Übermittlungsoptimierung

Prüft den genauen Zustand des Dienstes Übermittlungsoptimierung. Ein angehaltener Dienst mit manuellem Start bleibt eine Information. Nur genau der Zustand „Angehalten“ (Paused) kann eine Wiederherstellung anbieten. Die optionale Wiederherstellung wiederholt die Zustandsprüfung, sendet eine begrenzte Fortsetzungsanfrage und verlangt zwei stimmige Beobachtungen des Zustands „Wird ausgeführt“. Starttyp, Cacheinhalte, Peers, Bandbreite, Update-Quelle und verwaltete Richtlinie bleiben erhalten. Übertragungsaktivität, Cachezustand, Update-Fortschritt und Abschluss bleiben ungeprüft.

### BITS-Dienst

Prüft den genauen Zustand des Intelligenten Hintergrundübertragungsdienstes, die Übertragungsaufträge des aktuellen Kontos und den begrenzten genauen Verlauf von Dienstfehlern. Historische Fehler und Übertragungsmomentaufnahmen berechtigen nicht zum Start des Dienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Anwendungen können wartende Hintergrundübertragungen fortsetzen. Aufträge, Prioritäten, Besitzer, Ziele, Proxyeinstellungen, Richtlinie und Starttyp bleiben erhalten. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Übertragungsfortschritt, Zuständigkeit der Anwendung oder Abschluss.

### Hintergrundübertragungen (BITS)

Liest BITS-Übertragungsaufträge im gewählten Kontobereich und prüft genaue Auftragsidentitäten. Warteschlangennamen belegen nicht, zu welcher Anwendung ein Auftrag gehört. Keine Übertragungen werden fortgesetzt, abgeschlossen, abgebrochen, gelöscht oder neu zugewiesen.

### Kryptografiedienste

Prüft den genauen Zustand der Kryptografiedienste, ihre registrierte Dienst-DLL und den begrenzten genauen Verlauf von Dienstfehlern. Historische Fehler und DLL-Beobachtungen berechtigen weder zum Start des Dienstes noch zum Ersetzen von Komponenten. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Katalog-, Zertifikat- und Update-Vorgänge können fortgesetzt werden. Zertifikate, Schlüssel, Kataloge, Vertrauenseinstellungen, Richtlinie und Starttyp bleiben erhalten. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht die Prüfung von Zertifikaten, Signaturen, Katalogen oder Vertrauen.

### Windows Modules Installer

Prüft den genauen Zustand von Windows Modules Installer, begrenzte Übergänge von Wartungspaketen und den genauen Verlauf von Dienstfehlern. Historische Fehler und Paketzustände berechtigen weder zum Start des Dienstes noch zu einem Paketvorgang. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Wartungsaktivität kann fortgesetzt werden. Pakete, Inhalte des Komponentenspeichers, Quellen, Richtlinie und Starttyp bleiben erhalten. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht den Zustand des Komponentenspeichers, die Anwendbarkeit von Paketen, den Abschluss der Wartung oder die Neustartbereitschaft.

### Windows Update Medic Service

Prüft den genauen Zustand von Windows Update Medic Service, seine registrierte Dienst-DLL, die begrenzte aktuelle Update-Aktivität und den genauen Verlauf von Dienstfehlern. DLL-Beobachtungen, Update-Aktivität und historische Fehler berechtigen weder zum Start des Dienstes noch zu einer Komponentenaktion. Ein angehaltener Dienst mit manuellem Start kann normal sein. Wenn genau dieses Werkzeug ausgewählt ist, kann die optionale Wiederherstellung nach aktuellen Abhängigkeitsprüfungen nur den angehaltenen, aktivierten Dienst starten. Die Windows-Problemlösung kann unter der aktuellen Richtlinie fortgesetzt werden und Update-Komponenten reparieren oder Update-Aktivität auslösen. Update-Richtlinie, Quellenregistrierung, Zeitpläne, Pakete, BITS-Aufträge, Neustarteinstellungen, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt kein Update und ändert keine Update-Einstellung. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Problemlösung, Zustand der Komponenten, Anwendbarkeit, Download, Installation oder Neustartbereitschaft.

### Update Orchestrator Service

Prüft den genauen Zustand von Update Orchestrator Service, die genaue integrierte Windows Update-Aufgabe, die begrenzte aktuelle Aktivität und den genauen Verlauf von Dienstfehlern. Aufgabenergebnisse, Update-Aktivität und historische Fehler berechtigen weder zum Start des Dienstes noch zu einem Update-Vorgang. Die optionale Wiederherstellung startet nach aktuellen Prüfungen von Abhängigkeiten und Registrierung der ausführbaren Datei nur einen angehaltenen automatischen Dienst. Ein angehaltener Dienst mit manuellem Start bleibt eine Information. Vorhandene geplante Update-Aktivität kann unter der aktuellen Richtlinie fortgesetzt werden und bereits genehmigte Updates herunterladen oder installieren. Update-Richtlinie, Quellenregistrierung, Zeitpläne, Neustarteinstellungen, Pakete, BITS-Aufträge, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt oder genehmigt kein Update. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Anwendbarkeit, Download, Installation, Abschluss oder Neustartbereitschaft.

### Komponentenspeicher-Bereinigung

Analysiert, ob Windows eine Bereinigung des Komponentenspeichers empfiehlt. Die optionale Wartung entfernt ersetzte Komponenten sofort und analysiert dann erneut. Kein ResetBase und kein manuelles Löschen in WinSxS. Kein automatisches Rückgängigmachen; der freigegebene Speicherplatz wird nicht gemessen.
