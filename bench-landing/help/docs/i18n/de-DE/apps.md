# Apps

Der Bereich Apps prüft installierte Programme, Store-Pakete, Office und Outlook, Browsererweiterungen, Autostart-Apps und die Suche. Werkzeuge zum Entfernen und Zurücksetzen zeigen immer zuerst eine Prüfung und ändern nur das, was Sie auswählen.

Schritt-für-Schritt-Anleitungen: [Neuen Computer einrichten](set-up-new-pc.html).

## Werkzeuge in diesem Bereich

Installierte Anwendungen, Store-Pakete, Office und Outlook, Browsererweiterungen und die Suche prüfen.

### Installierte Anwendungen

Liest die Deinstallationsregistrierungen von Anwendungen im gewählten Bereich des Computers oder des ausführenden Kontos und kombiniert diesen Bereich mit begrenzten Nachweisen aus Anwendungsfehlern, Zuverlässigkeitsüberwachung und Neustarts. Andere Konten und portable Apps sind ausgeschlossen. Registrierungen und allgemeiner Verlauf belegen weder den genauen Zustand einer Anwendung noch ihre Zuständigkeit. Es wird keine Aktion für Installationsprogramm, Deinstallationsprogramm, Anwendung, Prozess, Dienst, Datei, Registrierung, Konto, Richtlinie oder Neustart ausgewählt.

### App-Pakete

Erfasst registrierte Appx/MSIX-Pakete für den gewählten Kontobereich. Standard ist das ausführende Konto; alle Benutzer erfordern Administratorzugriff. Meldet genaue Identitäten, Framework- und Nicht-entfernbar-Kennzeichen sowie die Zahl der Abhängigkeiten. Bereitgestellte Pakete, Win32-Anwendungen, verwaltete Zuständigkeit und Anwendungsdaten werden nicht bewertet. Es werden keine Pakete entfernt.

### Browsererweiterungen

Erfasst begrenzte Identitäten von Erweiterungen für Chrome, Edge, Brave und Firefox aus dem ausführenden Konto. Browserverlauf, Anmeldeinformationen, Erweiterungseinstellungen, Manifestinhalte, Befehle und Richtlinienwerte sind ausgeschlossen. Das Vorhandensein belegt weder Aktivierung noch Vertrauenswürdigkeit, Zuständigkeit oder sicheres Entfernen. Kein Browser, keine Erweiterung und keine Richtlinie wird geändert.

### Autostart-Anwendungen

Erfasst die Namen der Run- und RunOnce-Registrierungen des Computers und des ausführenden Kontos, Dateinamen im Autostart-Ordner, Windows-Genehmigungszustände für den Start sowie Namen und Zustände geplanter Aufgaben beim Start oder bei der Anmeldung und liefert eine Schlussfolgerung zur Startumgebung. Befehlszeilen, Zielpfade, Zeitstempel binärer Genehmigungen, Aufgabenaktionen, Argumente, Konten und Anmeldeinformationen werden nicht erfasst. Eine Registrierung belegt nicht, dass ein Eintrag gelaufen ist, eine Verzögerung verursacht hat, unerwünscht ist oder gefahrlos deaktiviert werden kann. Ein Genehmigungszustand belegt nicht, dass eine passende Registrierung existiert. Es wird keine Änderung an Registrierungen, Dateien, Anwendungen, Aufgaben, Diensten oder Richtlinien ausgewählt.

### Microsoft-Apps entfernen

Prüft ausgewählte Registrierungen von OneDrive, Outlook, Office/M365, Project und Teams mit Wahlmöglichkeiten für Kontobereich und Aufbewahrung. Bestände von Computer, ausführendem Konto und registrierten Paketen werden abgeglichen; die Prüfung für alle Benutzer umfasst auch Pakete, die für neue Benutzer bereitgestellt sind. Ein unterstütztes Paket des aktuellen Kontos kann in der Desktop-App oder der CLI einen eigenen genauen Absicherungsplan erhalten. Das Entfernen dieses einen Pakets ist eine eigene, abgesicherte Desktop-Aktion mit eigenen Bestätigungen. Die Prüfung selbst ändert keine Prozesse, Anwendungen, Pakete, Dateien, Anmeldeinformationen, Ordnerorte, Registrierungswerte oder Benutzerdaten.

### Outlook zurücksetzen

Prüft das neue Outlook, das klassische Outlook oder beide mit ausdrücklichen Wahlmöglichkeiten für Caches, Profileinstellungen, Authentifizierung, OST-Dateien, Add-Ins und die Neuinstallation des neuen Outlook. Die Prüfung für das ausführende Konto untersucht begrenzte bekannte Speicherorte, genaue Registrierungen und die Bereitschaft geschlossener Prozesse. Eine separat bestätigte Aktion nur für Caches bewahrt genaue Cacheverzeichnisse unter eindeutigen Wiederherstellungsnamen auf und überprüft jede Verschiebung. Die Prüfung für alle Benutzer und andere Rücksetzbereiche bleiben reine Prüfungen. Kein aufbewahrter Cache, kein Profil, keine OST-Datei, keine Anmeldeinformation, kein Add-In, kein Paket und keine Einstellung wird entfernt.

### Foxit-Inventar

Erfasst auf dem Computer installierte Foxit-Produkte und Werte des Updaters in der Registrierung in beiden Ansichten. Benutzerinstallationen, wirksame Richtlinie und Zuständigkeit für Produkte werden nicht überprüft. Keine Anwendungen oder Einstellungen werden geändert.

### Windows Installer-Dienst

Prüft vor der Dienstwiederherstellung die Installer-Richtlinien für Einschränkungen, Rollback, erhöhte Rechte, Protokollierung und Debugging, Markierungen laufender Vorgänge, Binärdateien des Moduls und die Sichtbarkeit registrierter zwischengespeicherter Pakete. Aktuelle fehlgeschlagene und erfolgreiche Ergebnisse werden nur für genau gemeldete Produktidentitäten zugeordnet. Bestehen die Voraussetzungen, kann die automatische Reparatur den aktivierten Dienst starten und „Wird ausgeführt“ überprüfen. Historische Ergebnisse, Protokollierung, Sichtbarkeit des Caches und Debug-Befunde berechtigen nicht zu Paket- oder Richtlinienänderungen. Richtlinien, Protokolle, Transaktionsmarkierungen, Starttypen und zwischengespeicherte Pakete bleiben erhalten. Eine bestimmte Paketinstallation wird nicht getestet.

### Windows Search

Prüft die Verfügbarkeit des Windows Search-Dienstes, die aktuelle Aktivität von SystemIndex und den aktuellen Ereignisverlauf der Suche. Die automatische Reparatur kann einen angehaltenen, aktivierten Dienst starten und den laufenden Zustand überprüfen. Eine separate, ausdrückliche Wahl zum Neuaufbau des Index bewahrt den bisherigen Index auf, beginnt einen neuen Index und überprüft den genauen Zustand von Dienst und Verzeichnissen. Indizierte Orte, Richtlinie, Starttyp und aufbewahrte Daten bleiben erhalten. Der Start des Dienstes oder der Neuaufbau des Index kann die Indizierung im Hintergrund fortsetzen. Die Vollständigkeit der Suchergebnisse bleibt ungetestet.

### App-Vorbereitung

Prüft die Verfügbarkeit des Dienstes App-Vorbereitung. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Vorgänge bei der ersten Anmeldung und bei der Anwendungsbereitstellung können unter der aktuellen Richtlinie fortgesetzt werden. Pakete, Registrierungen, Lizenzen, Benutzerprofile, Anwendungsdaten, Richtlinie und Starttyp bleiben erhalten. KillerBench wählt keinen Paketvorgang. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Anmeldung, Bereitstellung oder Anwendungsstart.

### Statusrepositorydienst

Prüft die Verfügbarkeit des Statusrepositorydienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Anfragen zum Zustand des Anwendungsmodells können fortgesetzt werden. Registrierungen, Pakete, Lizenzen, Profile, Browser- und Anwendungsdaten, Repositoryinhalte, Richtlinie und Starttyp bleiben erhalten. Kein Zurücksetzen, keine erneute Registrierung und kein Löschen wird angefordert. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht die Konsistenz des Repositorys oder den Anwendungsbetrieb.

### AppX-Bereitstellungsdienst

Prüft die Verfügbarkeit des AppX-Bereitstellungsdienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Bereitstellungs-, Registrierungs- oder Wartungsanfragen können unter der aktuellen Richtlinie fortgesetzt werden. Pakete, Registrierungen, Lizenzen, Profile, Anwendungsdaten, Repositoryinhalte, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Paketvorgang. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Bereitstellung, Registrierung oder Anwendungsstart.

### Clientlizenzdienst

Prüft die Verfügbarkeit des Clientlizenzdienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Lizenzanfragen von Store-Anwendungen können unter der aktuellen Richtlinie fortgesetzt werden. Pakete, Registrierungen, Lizenzen, Token, Konten, Profile, Anwendungsdaten, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Vorgang an Paketen, Lizenzen, Token oder Konten. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht die Lizenzprüfung oder den Anwendungsstart.

### Microsoft Store-Installationsdienst

Prüft den aktuellen Installationsverlauf des Microsoft Store und die genaue Verfügbarkeit des Store-Installationsdienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Download- und Installationsanfragen des Store können unter der aktuellen Richtlinie fortgesetzt werden. Pakete, Registrierungen, Lizenzen, Konten, Anwendungsdaten, Downloadzustand, Quellen, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Vorgang an Paketen, Downloads, Lizenzen, Konten, Quellen oder Installationen. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Ergebnisse von Download, Installation, Lizenz oder Anwendung.

### Windows-Lizenz-Manager-Dienst

Prüft die Verfügbarkeit des Windows-Lizenz-Manager-Dienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Lizenzprüfungen von Anwendungen können unter der aktuellen Richtlinie fortgesetzt werden. Pakete, Registrierungen, Lizenzen, Token, Konten, Anwendungsdaten, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Vorgang an Paketen, Lizenzen, Token, Konten, Registrierungen oder Berechtigungsansprüchen. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Lizenzprüfung, Berechtigungsanspruch oder Anwendungsstart.

### Windows PushToInstall-Dienst

Prüft die Verfügbarkeit des Windows PushToInstall-Dienstes. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Eine vorhandene, bereits genehmigte Remoteinstallationsanfrage kann unter der aktuellen Richtlinie fortgesetzt werden. Pakete, Registrierungen, Lizenzen, Konten, Anwendungsdaten, wartende Anfragen, Quellen, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench erstellt, genehmigt und wählt keine Anfrage, kein Paket, keine Quelle, kein Konto und keine Installation. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Verarbeitung von Anfragen, Download, Installation oder Ergebnisse von Anwendungen.
