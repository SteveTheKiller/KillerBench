# Sicherheit

Der Bereich Sicherheit untersucht Microsoft Defender, die Firewall, Zertifikate und die Anmeldedienste. Er zeigt außerdem den Zustand von TPM und Plattformsicherheit, auf den BitLocker und Windows Hello angewiesen sind.

Schritt-für-Schritt-Anleitungen: [Laufwerk mit BitLocker verschlüsseln](encrypt-with-bitlocker.html) und [Windows datenschutzfreundlicher machen](make-windows-private.html).

## Werkzeuge in diesem Bereich

Schutz, Firewall, Zertifikate und Anmeldedienste prüfen.

### Microsoft Defender

Untersucht den Schutz- und Signaturstatus von Defender. Die automatische Reparatur aktualisiert Signaturen nur, wenn Defender aktiven Schutz und veraltete Signaturen meldet. Verwendet die konfigurierten Signaturquellen. Keine Änderungen an Schutz, Ausschlüssen oder Richtlinien; keine Schadsoftwareprüfung.

### Windows-Sicherheitscenter

Prüft begrenzte Antivirenregistrierungen mit dem rohen Anbieterzustand, den genauen Verlauf von Dienstfehlern und die aktuelle Bereitschaft des Windows-Sicherheitscenters, ohne den Zustand des Schutzes zu bewerten. Ein leerer Bestand ist kein Urteil über den Schutzzustand, und weder die Zahl der Registrierungen noch der rohe Zustand oder historische Fehler berechtigen zur Wiederherstellung oder zu einer Sicherheitsänderung. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Überwachung des Sicherheitsstatus und Benachrichtigungen können fortgesetzt werden. Antivirenkonfiguration, Anbieterregistrierungen, Schutzeinstellungen, Ausschlüsse, Benachrichtigungen, Richtlinie und Starttyp bleiben erhalten. Es wird keine Prüfung, kein Update und keine Schutzänderung angefordert. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Vorhandensein eines Virenschutzes, Schutzzustand, Aktualität der Signaturen, Zustellung von Benachrichtigungen, Zuständigkeit für Registrierungen oder wirksame Richtlinie.

### Firewall-Dienste

Liest die Aktivierungseinstellungen der Profile Domäne, Privat und Öffentlich aus ActiveStore und prüft dann die Dienste Basisfiltermodul und Windows Defender Firewall. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur aktivierte, angehaltene Dienste. Vorhandene Filterrichtlinien können wirksam werden und den Netzwerk- oder Remotezugriff unterbrechen. Starttypen, Dienstberechtigungen, Firewallregeln, Profile und IPsec-Einstellungen bleiben erhalten. Keine Zurücksetzungen, Regeländerungen oder Dienststopps. Die Überprüfung stellt nur den laufenden Zustand der Dienste fest; wirksamer Schutz und Konnektivität bleiben ungeprüft.

### Zertifikatablauf

Untersucht Zertifikatsdaten und bildet nur aus dem Cache lokale Vertrauensketten für die persönlichen Speicher des Computers und des aktuellen Benutzers und liefert eine begrenzte betriebliche Schlussfolgerung. Sperrung, Vorlage durch Server, Bindungen, Registrierung, Zuständigkeit von Richtlinien, Remotevertrauen und Nutzbarkeit privater Schlüssel bleiben ungeprüft. Kein Zertifikat, keine Vertrauenseinstellung, keine Registrierung, keine Bindung, kein privater Schlüssel, keine Anwendung, kein Dienst und keine Richtlinie wird geändert.

### Anmeldeinformationsverwaltung

Prüft die Verfügbarkeit des Dienstes Anmeldeinformationsverwaltung. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Anwendungen können Anfragen nach gespeicherten Anmeldeinformationen fortsetzen. Tresorinhalte, Kennwörter, Passkeys, Zertifikate, Token, Registrierungen von Anmeldeinformationen, Berechtigungen, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. Es wird kein Inhalt und kein Vorgang zu Anmeldeinformationen angefordert. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Abruf von Anmeldeinformationen, Authentifizierung oder Anwendungszugriff.

### Webkontoverwaltung

Prüft den bereinigten Verlauf des Kontobrokers und die genaue Verfügbarkeit der Webkontoverwaltung. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Anwendungen können Konto- und Tokenanfragen unter der aktuellen Richtlinie fortsetzen. Historische Ereignisse berechtigen nicht zur Wiederherstellung und belegen keinen aktuellen Kontozustand. Konten, Anmeldeinformationen, Token, Cookies, Anwendungsregistrierungen, Berechtigungen, Anmeldezustand, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Vorgang an Konten, Anmeldeinformationen, Token, Cookies, Berechtigungen oder Anmeldung. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Authentifizierung, Ausgabe von Token oder Anwendungszugriff.

### Anmelde-Assistent für Microsoft-Konten

Prüft den bereinigten Verlauf des Kontobrokers und die genaue Verfügbarkeit des Anmelde-Assistenten für Microsoft-Konten. Die optionale Wiederherstellung startet nach aktuellen Abhängigkeitsprüfungen nur den aktivierten, angehaltenen, genau bezeichneten Dienst. Vorhandene Anmeldeanfragen für Microsoft-Konten können unter der aktuellen Richtlinie fortgesetzt werden. Historische Ereignisse berechtigen nicht zur Wiederherstellung und belegen keinen aktuellen Kontozustand. Konten, Anmeldeinformationen, Token, Geräteregistrierung, Anwendungsdaten, Berechtigungen, Anmeldezustand, Richtlinie, Dienstberechtigungen und Starttyp bleiben erhalten. KillerBench wählt keinen Vorgang an Konten, Anmeldeinformationen, Token, Registrierungen, Berechtigungen oder Anmeldung. Die Überprüfung umfasst nur einen stabilen Dienstzustand, nicht Authentifizierung, Kontokonnektivität oder Anwendungszugriff.
