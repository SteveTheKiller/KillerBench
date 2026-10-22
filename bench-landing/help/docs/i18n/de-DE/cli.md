# Befehlszeile

`killerbench-cli` führt dieselbe Engine wie die Desktop-App aus PowerShell oder einer Remotekonsole aus. Jeder Befehl, der den Computer verändern könnte, zeigt standardmäßig eine Vorschau und braucht einen ausdrücklichen Schalter wie `--execute`, bevor er handelt. Befehle geben JSON-Nachweise an die Standardausgabe und den Fortschritt an die Standardfehlerausgabe aus.

Schritt-für-Schritt-Anleitungen: [KillerBench aus Skripten ausführen](use-the-cli.html).

Exitcode 0 bedeutet, dass die Vorschau oder das überprüfte Ergebnis erfolgreich war, 1 einen Start-, Eingabe- oder Journalfehler, 2, dass die Argumente nicht erkannt wurden, 3 blockiert, fehlgeschlagen oder nicht überprüft, 4, dass ein Workflow vor der Überprüfung einen Neustart braucht, und 130, dass Sie den Befehl gestoppt haben.

Führen Sie `killerbench-cli` ohne Argumente aus, um diese Liste auf Ihrem eigenen Computer auszugeben.

## Updates und Wartung

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Genaue Prüfung einer Übertragung oder Beobachtung über zwei Momentaufnahmen; kein erneuter Versuch.
- `bits-jobs [--all-users]`: Zustände, Besitzer und Bytezahlen von BITS-Aufträgen; keine Änderungen an Übertragungen.
- `bits-retry JOB_GUID [--execute]`: Standardmäßig Vorschau; ausdrücklicher erneuter Versuch für genau einen Auftrag, keine Aussage über den Abschluss in der Anwendung.
- `bits-retry-recover JOB_GUID`: Zwei schreibgeschützte Momentaufnahmen; archiviert stabile Ergebnisse ohne weiteren Versuch.
- `component-repair-source`: Verwaltete Richtlinie für die DISM-Reparaturquelle und Konflikte; keine Änderungen.
- `delivery-optimization`: Schreibgeschützter Dienstzustand der Übermittlungsoptimierung; JSON-Nachweise.
- `delivery-optimization-activity`: Datenschutzbegrenzte aktuelle Auftragszahlen und Bytesummen; keine Übertragungsidentitäten, keine Änderungen.
- `delivery-optimization-policy`: Schreibgeschützte klassische und Policy-CSP-Einstellungen; vertrauliche Werte und Anbieteridentitäten ausgeschlossen.
- `feature-service-events`: Liest den genauen Fehlerverlauf der Dienste für Zeit, Suche, WMI, Ereignisprotokoll und Audio.
- `feature-update-policy`: Schreibgeschützte Richtlinie für Zielversion, Zurückstellung, Pause und Schutzmaßnahmen; JSON-Nachweise.
- `pending-file-operations`: Schreibgeschützte, datenschutzbegrenzte Übersicht ausstehender Dateivorgänge; JSON-Nachweise.
- `restart-check`: Schreibgeschützte Quellen ausstehender Neustarts; JSON-Nachweise.
- `servicing-logs`: Begrenzte Analyse des neuesten CBS-Protokolls, der Klartext- und komprimierten CBS-Archive sowie der DISM- und Windows Setup-Protokolle; keine Reparatur.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Schreibgeschützte Momentaufnahmen der Wartungsaktivität.
- `servicing-service-events`: Schreibgeschützte, genaue Fehlerverläufe der Windows Update-Wartungskette.
- `servicing-service-states`: Schreibgeschützter aktueller Zustand der sechs Dienste der Windows Update-Wartungskette.
- `setup-code DECIMAL_OR_0xHEX`: Dokumentierte Referenz der Windows Setup-Ergebniscodes; keine Untersuchung des Computers, keine Reparatur.
- `update-applicability`: Schreibgeschützte Nachweise zu Windows-Edition, Build und Architektur.
- `update-cache`: Begrenzte, schreibgeschützte Summen des SoftwareDistribution-Caches; keine Dateinamen.
- `update-code DECIMAL_OR_0xHEX`: Dokumentierte Referenz der Windows Update-Ergebniscodes; keine Untersuchung des Computers, keine Reparatur.
- `update-context`: Schreibgeschützte Nachweise zu aktuell erhöhten Rechten und Desktopsitzung; keine Kontokennungen.
- `update-engine-files`: Schreibgeschützte Nachweise zu Vorhandensein, Version und Signatur des nativen Windows Update-Moduls.
- `update-events`: Schreibgeschützter Verlauf des Windows Update-Clients der letzten sieben Tage; JSON einschließlich Rohnachweisen.
- `update-followup DECIMAL_OR_0xHEX`: Aktuelle schreibgeschützte Diagnose, ausgewählt anhand eines dokumentierten Windows Update-Ergebniscodes.
- `update-history`: Schreibgeschützte, neueste 100 Ergebnisse von Windows Update-Vorgängen; JSON-Nachweise.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: `--select` wiederholen; standardmäßig Vorschau.
- `update-policy`: Schreibgeschützte verwaltete Windows Update-Richtlinie; JSON-Nachweise.
- `update-policy-surfaces`: Schreibgeschützte Nachweise zu klassischen und Policy-CSP-Konfigurationsoberflächen; Werte und Anbieteridentitäten ausgeschlossen.
- `update-recover`: Prüft ein ausstehendes Update; installiert es nie und versucht es nie erneut.
- `update-restart-policy`: Schreibgeschützte Compliance-Fristen, Kulanzzeiten, Neustartunterdrückung und Benachrichtigungsverhalten.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: JSON-Suche und Treffer; keine Installation.
- `update-service-events`: Schreibgeschützter, genauer Fehlerverlauf des Windows Update-Dienstes.
- `update-service-files`: Schreibgeschützte Nachweise zu Registrierung und Signatur der genauen Dienst-DLL von Windows Update.
- `update-services`: Schreibgeschützte Dienstregistrierungen von Windows Update; JSON-Nachweise.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Durch eine neue Suche abgesichert; standardmäßig Vorschau; bewahrt den alten Speicher auf.
- `update-store-reset-recover`: Gleicht ein vorbereitetes Zurücksetzen des Speichers ab, ohne es zu wiederholen.
- `windows-capabilities`: Schreibgeschützte Zustände der Windows-Funktionen im laufenden System; JSON-Ergebnis.
- `windows-update-task`: Schreibgeschützter Zustand und Laufzeitverlauf der genauen Aufgabe „Scheduled Start“; keine Aufgabenausführung, kein Update-Vorgang.

## BitLocker und TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Standardmäßig Vorschau; schaltet die automatische Entsperrung für ein verschlüsseltes Datenvolume ein.
- `bitlocker-auto-unlock-recover`: Überprüft eine unterbrochene Anfrage zur automatischen Entsperrung, ohne sie zu wiederholen.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Standardmäßig Vorschau; nur eine neue Sicherungsdatei für die Wiederherstellung.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Überprüft eine unterbrochene Sicherung, ohne sie erneut zu schreiben.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Standardmäßig Vorschau; startet die Entschlüsselung, etwa vor einer erneuten Verschlüsselung mit einer geforderten Methode.
- `bitlocker-decrypt-recover`: Überprüft eine unterbrochene Entschlüsselungsanfrage, ohne sie zu wiederholen.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Standardmäßig Vorschau; erfordert vorhandene TPM- und Wiederherstellungsschutzvorrichtungen.
- `bitlocker-encrypt-recover`: Überprüft den ausstehenden Konvertierungszustand, ohne die Anfrage zu wiederholen.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Standardmäßig Vorschau; sichert ein vorhandenes Wiederherstellungskennwort in Active Directory oder Microsoft Entra ID.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Nur Vorschau der Optionen.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Standardmäßig Vorschau; fügt eine TPM- oder Wiederherstellungskennwort-Schutzvorrichtung hinzu; das Kennwort wird nie ausgegeben.
- `bitlocker-protector-recover`: Überprüft eine unterbrochene Anfrage zu einer Schutzvorrichtung, ohne sie zu wiederholen.
- `bitlocker-protectors DRIVE:`: Nur Kennungen und Typen der Schutzvorrichtungen; keine Wiederherstellungskennwörter.
- `bitlocker-resume DRIVE: [--execute]`: Standardmäßig Vorschau; nur ein genau bezeichnetes, vollständig verschlüsseltes, angehaltenes Volume.
- `bitlocker-resume-recover`: Überprüft den ausstehenden Schutzzustand, ohne die Anfrage zu wiederholen.

## Sicherheit und Datenschutz

- `certificate-chains`: Schreibgeschütztes lokales Kettenvertrauen nur aus dem Cache für persönliche Zertifikate; keine Sperrprüfung, keine Downloads.
- `defender-overview [--report]`: Schreibgeschützte Übersicht über Schutz, Prüfzeitpunkte und zusammengefasste Bedrohungen; keine Aktion.
- `defender-protection [--report]`: Schreibgeschützte Schutzeinstellungen und erkannte Richtlinienoberflächen; vertrauliche Details ausgeschlossen; keine Änderungen.
- `defender-scan Quick|Full [--execute] [--report]`: Standardmäßig Vorschau; genau eine ausdrückliche, abgesicherte Prüfung mit Microsoft Defender.
- `defender-scan-history [--report]`: Schreibgeschützte Zeitpunkte schneller und vollständiger Prüfungen und Überfälligkeitsstatus; vertrauliche Details ausgeschlossen; keine Prüfung.
- `defender-scan-recover [--report]`: Schreibgeschützter Abgleich, ohne eine weitere Prüfung zu starten.
- `defender-threats [--report]`: Zusammengefasster Bedrohungsverlauf, aktuelle aktive Kennzeichen und Schweregrad-IDs; vertrauliche Details ausgeschlossen; keine Aktion.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Standardmäßig Vorschau; nur nicht verwaltete lokale Profile.
- `firewall-enable-recover`: Untersucht eine ausstehende Änderung, ohne sie zu wiederholen.
- `firewall-events`: Liest den begrenzten Betriebsverlauf der Windows-Firewall mit Warnungen, Fehlern und kritischen Ereignissen.
- `firewall-logging`: Liest die Konfiguration der Datenverkehrsprotokollierung für Domäne, Privat und Öffentlich, ohne Protokolle zu öffnen.
- `firewall-rules`: Liest datenschutzbegrenzte Zahlen wirksamer Regeln und Richtlinienquellen.
- `firewall-service-events`: Liest den genauen Fehlerverlauf des Dienststeuerungs-Managers für BFE und Windows Defender Firewall.
- `privacy-clipboard-history on|off [--execute]`: Standardmäßig Vorschau; genauer Wert des aktuellen Benutzers, Richtlinien bleiben erhalten.
- `privacy-clipboard-history-recover`: Prüft eine unterbrochene Änderung erneut, ohne sie zu wiederholen.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: Abgesicherte Wiederherstellung des vorherigen Werts.
- `privacy-plan --select Camera --select ClipboardSync`: Nur Prüfung der Auswahl; keine Einstellungen geändert.
- `privacy-setting KEY on|off [--execute]`: Standardmäßig Vorschau; Änderung für den aktuellen Benutzer unter Beibehaltung der Richtlinien; ungültige Schlüssel listen die unterstützten Werte.
- `privacy-setting-recover KEY`: Prüft eine unterbrochene Änderung erneut, ohne sie zu wiederholen.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: Abgesicherte Wiederherstellung des genauen Werts.
- `security-check`: Schreibgeschützte Nachweise zu TPM, sicherem Start, Virtualisierungsschutz und UAC-Richtlinie; JSON-Nachweise.

## Dienste

- `service-binaries`: Schreibgeschütztes Vorhandensein lokaler Dienstbinärdateien und Authenticode-Status; Pfade ausgeschlossen, nicht lokale Pfade werden nicht kontaktiert.
- `service-check NAME [NAME ...] | service-check --all`: Schreibgeschützte Zustände unterstützter Dienste und Abhängigkeitsnachweise.
- `service-dependencies`: Schreibgeschützte Prüfung der Integrität direkter Abhängigkeiten; keine Dienständerungen.
- `service-dlls`: Schreibgeschütztes Vorhandensein registrierter Dienst-DLLs und Authenticode-Status; Pfade ausgeschlossen, nicht lokale Pfade werden nicht kontaktiert.
- `service-events`: Schreibgeschützter Verlauf von Dienstfehlern; JSON einschließlich Rohnachweisen.
- `time-events`: Schreibgeschützter aktueller Ereignisverlauf der Windows-Zeit mit JSON-Nachweisen.
- `service-logons`: Schreibgeschützte Prüfung der Kontoklasse von Diensten und der Desktopinteraktion; Kontoidentitäten ausgeschlossen.
- `service-path-quoting`: Schreibgeschützte Prüfung der Anführungszeichen in Pfaden ausführbarer Dienstdateien; vollständige Pfade und Argumente ausgeschlossen.
- `service-recovery-policy`: Schreibgeschützte Übersicht der Wiederherstellungsaktionen installierter Dienste; Befehle und Neustartmeldungen ausgeschlossen.
- `service-startup-config`: Schreibgeschützte Prüfung der Stimmigkeit der Startmodi; keine Dienständerungen.
- `service-transitions`: Schreibgeschützte wiederholte Beobachtung ausstehender Dienstzustände; keine Dienstaktion.
- `service-triggers`: Schreibgeschützte Übersicht der Trigger installierter Dienste; Triggerdaten ausgeschlossen; keine Änderungen.

## Speicher, Dateien und Sicherung

- `backup-events`: Verlauf von VSS und volsnap; JSON-Nachweise; keine Änderungen.
- `backup-service-events`: Liest den genauen Fehlerverlauf von VSS, Softwareschattenkopie-Anbieter, Windows-Sicherung und Blockebenen-Sicherungsmodul.
- `backup-summary`: Übersicht der lokalen Vorgänge der Windows-Sicherung; keine Änderungen an Sicherungen.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Standardmäßig Vorschau; der Papierkorb wird ganz oder gar nicht bereinigt, das Alter gilt nicht.
- `disk-cleanup-task`: Schreibgeschützter Zustand und Laufzeitverlauf der genauen Aufgabe; keine Aufgabenausführung, keine Bereinigung.
- `disk-diagnostic`: Ordnet genaue Datenträgerdiagnoseaufgaben und Ereignisverlauf zu; keine Aktion an Aufgaben oder Datenträgern.
- `disk-diagnostic-tasks`: Schreibgeschützter Verlauf der genauen Sammler- und Behebungsaufgaben; keine Aktion an Aufgaben oder Datenträgern.
- `disk-quotas`: Schreibgeschützte Kontingentstandards und -zustände pro Volume; Benutzeridentitäten ausgeschlossen; keine Änderungen.
- `file-history-events`: Datenschutzbegrenzte Zahlen von Warnungen und Fehlern des Dateiversionsverlaufs; keine Änderungen.
- `file-history-status`: Schutzzustand des Dateiversionsverlaufs für das aktuelle Konto; keine Änderungen.
- `search-index-rebuild [--execute]`: Standardmäßig Vorschau; bewahrt den bisherigen Index auf und überprüft den neuen Zustand.
- `search-index-rebuild-recover`: Schreibgeschützter Abgleich, ohne Reparaturbefehle zu wiederholen.
- `storage-check DRIVE_LETTER`: Schreibgeschützte Prüfung eines festen NTFS-Volumes; JSON-Ergebnisse und Live-Fortschritt.
- `storage-events`: Schreibgeschützter Speicherverlauf; JSON einschließlich Rohnachweisen.
- `storage-jobs`: Schreibgeschützter zusammengefasster Auftragszustand und Fortschritt des Storage-Moduls; JSON; keine Änderungen.
- `storage-reliability`: Schreibgeschützte Temperatur-, Verschleiß- und E/A-Zähler; JSON; keine Reparatur.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Standardmäßig Vorschau; die Ausführung erfordert eine Bestätigung der Sicherung; Systemvolumes werden für den Neustart geplant.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Nur Vorschau; keine Reparatur, keine Planung.
- `storage-repair-recover`: Überprüft eine abgeschlossene oder geplante Dateisystemreparatur, ohne sie zu wiederholen.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Sammelt aktuelle Nachweise und bindet sie; keine Reparatur, keine Planung.
- `storage-sense-policy`: Schreibgeschützte erkannte Bereinigungsrichtlinie des Computers; Benutzereinstellungen und Dateiidentitäten ausgeschlossen.
- `storage-sense-task`: Schreibgeschützter Zustand und Laufzeitverlauf der genauen Aufgabe; keine Aufgabenausführung, keine Bereinigung.
- `storage-service-events`: Liest den genauen Fehlerverlauf von Speicherdienst, Laufwerksoptimierung, VSS und Softwareschattenkopie-Anbieter.
- `volume-capacity DRIVE:`: Schreibgeschützter verfügbarer Speicher mit JSON-Nachweisen.
- `volume-dirty DRIVE:`: Schreibgeschützter Status des Dirty-Bits; keine Reparatur, kein Neustart.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Standardmäßig schreibgeschützte Analyse; erfordert ein genau bezeichnetes, fehlerfreies Volume und kompatible Medien.
- `volume-optimize-recover`: Überprüft einen ausstehenden abgeschlossenen Vorgang, ohne ihn zu wiederholen.
- `vss-providers`: Schreibgeschützte Namen, Versionen und Typen der VSS-Anbieter; Kennungen ausgeschlossen; keine Registrierungsänderungen.
- `vss-writers`: Gemeldete Writer-Zustände; Auswertung der englischen Ausgabe; kein Zurücksetzen.

## Drucken

- `print-events`: Schreibgeschützter Verlauf des PrintService-Admin-Protokolls der letzten sieben Tage; JSON-Nachweise.
- `print-extension-files`: Schreibgeschützte Übersicht zu Vorhandensein und Signatur der Binärdateien von Druckprozessoren und -monitoren; vollständige Pfade ausgeschlossen.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Standardmäßig Vorschau; nur ein genau bezeichneter Auftrag im Zustand „Fehler“ oder „Blockiert“.
- `print-job-cancel-recover [--folder PATH]`: Prüft einen unterbrochenen Abbruch erneut, ohne ihn zu wiederholen.
- `print-job-list`: Listet die aktuell sichtbaren Aufträge, ohne sie zu ändern.
- `print-operational-events`: Schreibgeschützter Zustand und verfügbarer Verlauf des PrintService-Betriebskanals; JSON-Nachweise.
- `print-policy`: Schreibgeschützte Point-and-Print-Einschränkungen und Richtlinie zur Treiberinstallation; JSON-Nachweise.
- `print-rendering-policy`: Schreibgeschützte Richtlinie für Rendern und Treiberisolation; JSON-Nachweise.
- `print-spooler-events`: Schreibgeschützte Fehler des Dienststeuerungs-Managers zur lokalen Druckwarteschlange; JSON-Nachweise.
- `printer-check --queue NAME [--queue NAME ...]`: Schreibgeschützte Einstellungen der gewählten lokalen Warteschlangen.
- `printer-conditions`: Schreibgeschützte Zustandscodes lokaler Warteschlangen; keine physische Überprüfung, keine Änderungen.
- `printer-driver-files`: Schreibgeschützte Übersicht zu Vorhandensein registrierter Dateien und Signaturen ausführbarer Dateien; vollständige Pfade ausgeschlossen.
- `printer-driver-models`: Schreibgeschützte Übersicht zu Version 3, Version 4 und Paketunterstützung; JSON-Nachweise.
- `printer-drivers`: Schreibgeschützter Bestand und Abgleich der Registrierungen von Warteschlangen und Treibern; JSON-Nachweise.
- `printer-endpoint-check`: Begrenzte Verbindungen zu genau zugewiesenen Standard-TCP/IP-Druckerendpunkten; keine Erkennung, kein SNMP, keine Druckdaten.
- `printer-list`: Findet lokale Warteschlangen, ohne Einstellungen zu ändern.
- `printer-ports`: Schreibgeschützter Abgleich der Registrierungen von Warteschlangen und Anschlüssen; JSON-Nachweise.
- `printer-queue-attributes`: Schreibgeschützte Spool-, Aufbewahrungs- und Freigabeattribute lokaler Warteschlangen.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Standardmäßig Vorschau; das Aufheben des Offlinemodus kann wartende Aufträge freigeben.

## Netzwerk

- `dhcp-check`: Schreibgeschützte Eignung fehlender oder abgelaufener DHCP-Leases; JSON-Nachweise, keine Erneuerung.
- `dhcp-events`: Schreibgeschützter Ereignisverlauf des DHCP-Clients für IPv4 und IPv6; JSON-Ergebnis.
- `dhcp-leases`: Schreibgeschützte Nachweise zu Zeitstempel, Ablauf und Server der Lease; JSON-Ergebnis.
- `dhcp-renew [--execute]`: Standardmäßig Vorschau; nur genau bezeichnete, fehlende oder abgelaufene DHCP-Leases verbundener physischer Adapter.
- `dhcp-renew-recover`: Schreibgeschützter Abgleich, ohne eine weitere Lease anzufordern.
- `dns-cache HOST [--execute]`: Standardmäßig Vorschau; abgesichertes Leeren des lokalen Caches mit Überprüfung genau dieses Hosts.
- `dns-cache-recover`: Prüft einen unterbrochenen Cachevorgang erneut, ohne das Leeren zu wiederholen.
- `dns-cache-summary`: Datenschutzbegrenzter Status des lokalen DNS-Caches und Zahlen nach Eintragstyp; keine Änderungen.
- `dns-check HOST [--timeout-seconds 1..60]`: Begrenzte Namensauflösung des Systems; JSON-Ergebnis.
- `dns-client-config`: Schreibgeschützte Nachweise zu Suffix, Registrierung, Devolution und Richtlinie; JSON-Ergebnis.
- `dns-client-events`: Schreibgeschützter, datenschutzbegrenzter Verlauf von Warnungen und Fehlern des DNS-Clients; JSON-Ergebnis.
- `dns-nrpt-policy`: Schreibgeschützte Nachweise zur wirksamen Richtlinie für DNS-Namespaces; JSON-Ergebnis.
- `ip-addresses`: Schreibgeschützte Prüfung von Zustand, Herkunft und Konflikten aktueller IP-Adressen; JSON-Nachweise.
- `ip-interface-config`: Schreibgeschützte Nachweise zur Konfiguration der IP-Schnittstellen; JSON-Ergebnis.
- `network-firewall`: Ordnet aktive Netzwerkzuordnungen der Aktivierung des passenden Firewallprofils zu.
- `network-offload`: Schreibgeschützte globale Konfiguration der Windows-Netzwerkauslagerung; JSON-Ergebnis.
- `network-profiles`: Schreibgeschützte aktuelle Profilkategorien und Konnektivitätseinstufungen; Netzwerknamen ausgeschlossen.
- `network-service-events`: Liest den genauen Fehlerverlauf von DHCP, DNS, Arbeitsstation, NLA, Netzwerklistendienst und IP-Hilfsdienst.
- `proxy-check`: Schreibgeschützte Nachweise zum gespeicherten WinHTTP-Proxy und zum Internetproxy des aktuellen Kontos.
- `route-priority`: Schreibgeschützter Vergleich der wirksamen Metriken der Standardrouten; JSON-Nachweise.
- `smb-client-config`: Schreibgeschützte Konfiguration von SMB-Signierung, Gastzugriff und Transport.
- `smb-connections`: Schreibgeschützte aktive Protokollaushandlung; Remoteidentitäten ausgeschlossen.
- `smb-server-config`: Schreibgeschützte Konfiguration von SMB-Protokoll, Signierung, Verschlüsselung und Mehrkanal.
- `smb-shares`: Schreibgeschützter, begrenzter Bestand an SMB-Freigaben; Pfade, Berechtigungen, Sitzungen und Konten ausgeschlossen.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Begrenzte Verbindungsprüfung; es werden keine Anwendungsdaten gesendet.
- `tcp-port-pressure`: Schreibgeschützte dynamische TCP-Bereiche und zusammengefasste Nutzung lokaler Ports; Endpunkte und Prozesse ausgeschlossen.
- `tcp-settings`: Schreibgeschützte Nachweise zur Konfiguration der Windows-TCP-Vorlagen; JSON-Ergebnis.
- `vpn-profiles`: Schreibgeschützte VPN-Profilkonfiguration des aktuellen Kontos; Anmeldeinformationen ausgeschlossen.
- `winrm-listeners`: Schreibgeschützte Übersicht der WinRM-Listener-Registrierungen; Zertifikatfingerabdrücke ausgeschlossen.
- `winsock-catalog`: Schreibgeschützte Struktur des nativen Winsock-Protokollkatalogs; JSON-Ergebnis.
- `winsock-namespaces`: Schreibgeschützte Struktur der nativen Winsock-Namespace-Anbieter; JSON-Ergebnis.
- `winsock-provider-files`: Schreibgeschütztes Vorhandensein und Signaturen der Dateien von Protokollanbietern; JSON-Ergebnis.
- `winsock-readiness`: Schreibgeschützte Bereitschaft der nativen Winsock-Protokolle, Namespace-Anbieter und Anbieterdateien; JSON-Ergebnisse.

## Apps

- `app-folder-review FOLDER_NAME [--report]`: Prüfung eines genauen Ordners unter festen Wurzeln; kein Löschen.
- `app-package-review FULL_NAME [--all-users]`: Genaues Ziel und abhängige Pakete; kein Entfernen.
- `app-packages [--all-users]`: Nur Bestand registrierter Appx/MSIX-Pakete; kein Entfernen.
- `browser-extensions`: Prüfung der Erweiterungsidentitäten von Chrome, Edge, Brave und Firefox; kein Entfernen.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Nur Prüfung; keine Systemänderungen.
- `depot-review [application and configuration selections] [--json]`: Nur Prüfung; keine Bereitstellung.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Schreibgeschützte Prüfung von Authenticode und Herstellersignatur; nichts wird ausgeführt.
- `foxit-inventory`: Schreibgeschützter Produktbestand des Computers; JSON-Ausgabe.
- `installed-apps [--current-user]`: Deinstallationsregistrierungen und Fähigkeitskennzeichen; kein Entfernen.
- `installed-apps-compare BEFORE.json AFTER.json`: Unterschiede der Registrierungen gespeicherter Bestände; keine Änderungen am Computer.
- `installed-hotfixes`: Schreibgeschützter, begrenzter Bestand installierter Windows-Hotfixes; JSON-Nachweise.
- `installer-activity`: Schreibgeschützte Nachweise zu Installer-Prozessen und Transaktionsmarkierungen; JSON-Nachweise.
- `installer-check`: Schreibgeschützte Installer-Richtlinie und Bereitschaft des Moduls; JSON-Nachweise.
- `installer-engine-files`: Schreibgeschützte Übersicht zum Vorhandensein benötigter Installer-Dateien und Authenticode; JSON-Nachweise.
- `installer-events`: Schreibgeschützter MSI-Verlauf der letzten sieben Tage; JSON-Nachweise.
- `installer-logging`: Schreibgeschützte Richtlinie für Installer-Protokollierung und Debugging; JSON-Nachweise.
- `installer-pending-state`: Schreibgeschützte Zuordnung ausstehender Dateivorgänge und Installer-Aktivität; JSON-Nachweise.
- `installer-registration-repair [--execute]`: Standardmäßig Vorschau; nur ein genauer Fehler der Dienstregistrierung; hebt die Registrierung des Moduls nie auf.
- `installer-registration-repair-recover`: Prüft eine unterbrochene Registrierung erneut, ohne Befehle zu wiederholen.
- `installer-security-policy`: Schreibgeschützte Sicherheits- und Patchrichtlinie des Installers; JSON-Nachweise.
- `installer-service-events`: Schreibgeschützte Fehler des Dienststeuerungs-Managers zum lokalen Windows Installer; JSON-Nachweise.
- `installer-service-registration`: Schreibgeschützte Übersicht der Dienstregistrierung von Windows Installer; JSON-Nachweise.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: Aktuelle Vorschau der genauen Eignung; kein Anbieter zum Entfernen.
- `mace-package-recover`: Gleicht einen unterbrochenen Eintrag zum Entfernen eines App-Pakets neu ab; wiederholt das Entfernen nie.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Nur Prüfung; kein Entfernen.
- `msi-cache`: Schreibgeschützte Sichtbarkeit des registrierten Windows Installer-Caches; JSON-Nachweise.
- `msi-code NONNEGATIVE_DECIMAL`: Dokumentierte Referenz der Windows Installer-Rückgabecodes; keine Untersuchung des Computers, keine Reparatur.
- `msi-product {PRODUCT-CODE}`: Schreibgeschützter genauer Zustand der MSI-Registrierung; JSON-Nachweise.
- `msi-repair {PRODUCT-CODE} [--execute]`: Standardmäßig Vorschau; nur ein genau bezeichnetes defektes oder falsch konfiguriertes Produkt.
- `msi-repair-recover`: Prüft eine unterbrochene Reparatur eines genauen Produkts erneut, ohne sie zu wiederholen.
- `msi-sources`: Schreibgeschützte, datenschutzbegrenzte Metadaten registrierter Windows Installer-Quellen; JSON-Nachweise.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Standardmäßig Vorschau; bewahrt die genauen Caches des ausführenden Kontos unter Wiederherstellungsnamen auf.
- `orca-cache-reset-recover`: Überprüft eine unterbrochene Cacheaufbewahrung, ohne eine Verschiebung zu wiederholen.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Nur Prüfung; keine Änderungen an Outlook.
- `startup-apps`: Prüfung von Autostartregistrierungen sowie Aufgaben beim Start oder bei der Anmeldung; keine Deaktivierung.
## System

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Genaue lokale Suche einer Bluetooth-SIG-Zuweisung; keine Netzwerkanfrage.
- `crash-config`: Gespeicherter Windows-Abbildmodus und Pfade; keine Änderungen.
- `crash-dumps`: Metadaten der Standard-Abbilddateien von Windows; keine Inhaltsanalyse, keine Änderungen.
- `crash-events`: Aktuelle Systemfehlerberichte; keine Diagnose, keine Änderungen.
- `crash-review [--knowledge PACK_PATH ...]`: Bis zu 32 Pakete; kombinierte Prüfung von Abstürzen, Herunterfahren, begrenzter Minidump-Struktur und optionaler genauer Bugcheck-Referenz; keine Änderungen.
- `default-printer`: Schreibgeschützte Standardauswahl und Einstellung der automatischen Verwaltung; JSON-Nachweise.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Standardmäßig Vorschau; nur ein fehlender oder widersprüchlicher Standard.
- `default-printer-set-recover [--folder PATH]`: Prüft eine unterbrochene Auswahl erneut, ohne sie zu wiederholen.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: Schreibgeschützte Gerätenachweise mit optionalen genauen Treiberverweisen aus bis zu 32 Paketen.
- `device-code DECIMAL_OR_0xHEX`: Offline-Referenz der Problemcodes des Geräte-Managers.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Nur Referenzkandidaten.
- `event-channel-enable CHANNEL [--execute]`: Standardmäßig Vorschau; aktiviert genau einen nicht verwalteten Diagnosekanal und behält die übrigen Einstellungen bei.
- `event-channel-enable-recover`: Schreibgeschützter Abgleich, ohne den Aktivierungsbefehl zu wiederholen.
- `event-channels`: Schreibgeschützter Zustand, Aufbewahrung, Größe und Auslastung von vier Diagnosekanälen.
- `font-registrations`: Schreibgeschützte, begrenzte Prüfung der Registrierungen und Dateien für Computer und aktuellen Benutzer.
- `memory-diagnostic-results`: Schreibgeschützter, begrenzter Verlauf der Windows-Speicherdiagnose; JSON-Nachweise.
- `minidump-analyze PATH`: Begrenzte strukturelle Minidump-Metadaten; keine Speicherseiten, keine Symbole, keine Änderungen.
- `name-rename PROPOSED_NAME [--execute]`: Standardmäßig Vorschau; nur eigenständige Arbeitsgruppen-PCs; startet Windows nie neu.
- `name-rename-recover`: Überprüft eine ausstehende Umbenennung, ohne sie zu wiederholen.
- `name-review PROPOSED_NAME`: Schreibgeschützte Prüfung von Identität und vorgeschlagenem Namen.
- `profiles --recover`: Überprüft die aufgezeichnete Abwesenheit und bewahrt Wiederherstellungsnachweise auf; versucht das Löschen nie erneut.
- `profiles --recover-backup`: Überprüft und archiviert ein ausstehendes Sicherungsartefakt; versucht das Kopieren nie erneut.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Zuerst Vorschau; die abgesicherte native Kopie erfordert eine ausdrückliche Ausführung und ein noch nicht vorhandenes lokales NTFS-Ziel.
- `profiles --select SID --create-backup-artifact DESTINATION`: Vergleicht eine vorhandene kopierte Sicherung und erstellt ihr festes Artefakt; kopiert oder entfernt kein Profil.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Schreibgeschützte Prüfung; die Überprüfung erfordert genau eine ausgewählte SID.
- `task-check`: Schreibgeschützter Aufgabenbestand, eingestufte Befunde und JSON-Nachweise.
- `task-definition TASK_FOLDER TASK_NAME`: Schreibgeschütztes genaues Aufgaben-XML; „nicht verfügbar“ bedeutet nicht „nicht vorhanden“.
- `task-inventory`: Schreibgeschützte Aufgabenidentitäten und -zustände; JSON-Ausgabe.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Historische Prüfung der Wiederherstellung; berechtigt nie zu einem erneuten Versuch.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Prüft und bewertet einen Aufgabenentwurf; keine Registrierung.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Standardmäßig Vorschau; deaktivierte Local-System-Definition, nur Neuanlage.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Schreibgeschützter Abgleich der genauen Definition; versucht nie erneut und führt keine Aufgabe aus.
- `task-xml PLAN.json YYYY-MM-DD`: Deaktiviertes Prüf-XML an die Standardausgabe; nur Kalenderzeitpläne in der lokalen Zone; keine Registrierung.
- `time-config`: Schreibgeschützte Nachweise zu Modus, Peers und Richtlinie der Windows-Zeit.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Standardmäßig Vorschau; der benutzerdefinierte Modus erfordert Peers.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Nur Vorschau.
- `time-recover --folder PATH`: Überprüft und archiviert einen ausstehenden Eintrag; versucht Änderungen nie erneut.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Schreibgeschützt; der benutzerdefinierte Modus erfordert Peers.
- `wmi-provider`: Schreibgeschützte Abfrage des lokalen Betriebssystemanbieters; JSON-Nachweise.

## Wissen, Validierung und Engine

- `knowledge PACK_PATH CONTEXT CODE`: Lokale Referenzsuche; keine Reparaturausführung.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Überprüfte lokale Aktivierung; kein Download.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Überprüft das aktive lokale Paket; keine Änderungen.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Nur Referenzänderungen; keine Paketinstallation.
- `knowledge-info PACK_PATH`: Paketmetadaten und Eintragszahlen pro Kontext; keine Änderungen.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Ordnet beobachteten Codes kontextbezogene Referenzen zu.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Wählt das neueste überprüfte aufbewahrte Paket; kein Download.
- `knowledge-search PACK_PATH CONTEXT TEXT`: Bis zu 50 Treffer im Referenztext; keine Reparaturausführung.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Löst genau eine Identität über bis zu 32 Pakete auf; Konflikte werden nie ausgewählt.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Überprüft jede konfigurierte installierte Datenbank; kein Download, keine Aktivierung.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Ruft signierte Datenbankkandidaten von konfigurierten HTTPS-Endpunkten ab und aktiviert sie atomar.
- `open <file.kbench> [--json]`: Öffnet eine gespeicherte Erfassung erneut und bewertet sie mit den installierten Regeln neu.
- `scan --output <new-file.kbench> [--json]`: Schreibgeschützte Windows Update-Erfassung, in einer neuen Datei gespeichert.
- `validation-coverage RECORD.json [...]`: Fasst native VM-Nachweise der aktuellen Version zusammen; keine Diagnose, keine Reparaturen.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Schreibgeschützter Szenarioplan für wegwerfbare VMs; keine Diagnose, keine Reparaturen.
- `validation-record RECORD.json`: Prüft genau einen begrenzten nativen VM-Nachweiseintrag; keine Diagnose, keine Reparaturen.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Lokaler VM-Abdeckungsbericht der aktuellen Version.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Führt einen geprüften, schreibgeschützten gemeinsamen Optionsplan aus.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Nur Bestandsaufnahme.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Gezieltes Diagnosejournal; keine Reparaturen.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Nur Prüfung mit Journal.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: Optionaler genauer Produktzustand im Journal.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: Optionale Diagnose des gewählten Ziels im Journal.
- `workflow storage [--volume D:] [--json]`: Schreibgeschützte Dateisystemprüfung mit Journal.
- `workflow-journal FILE.json`: Historische Inhalte; Exitcode 0 bedeutet erfolgreich gelesen, nicht fehlerfrei.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Zeigt oder prüft hostneutrale Steuerelemente; keine Prüfung, keine Reparatur.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Nur historischer Bericht; es werden keine Befehle ausgeführt.

## Weitere Befehle

- `<analyze|investigate> <text-log-path> [--json]`: Analysiert lokale Textnachweise.
- `adapter-bindings`: Schreibgeschützte Übersicht der Kernprotokollbindungen; JSON-Nachweise.
- `adapter-statistics`: Schreibgeschützte Byte-, Fehler- und Verwerfungszähler der Adapter; JSON-Nachweise.
- `application-events`: Schreibgeschützte aktuelle Anwendungsfehler, gruppierte Zahlen und JSON-Nachweise.
- `applocker-policy`: Schreibgeschützte wirksame Sammlungsmodi und zusammengefasste Regelzahlen; Regeldetails ausgeschlossen; keine Änderungen.
- `common-check [--scope NAME ...]`: Standardmäßig alle Diagnosebereiche; nur die gewählten Bereiche, wenn angegeben; schreibgeschütztes JSON.
- `delete-notifications`: Schreibgeschützte Konfiguration der Löschbenachrichtigungen für NTFS und ReFS; JSON; keine Optimierung, keine Einstellungsänderung.
- `diagnostic-scopes [--all]`: Listet 11 empfohlene Ergebnisgruppen oder alle 49 schreibgeschützten Bereiche; keine Erfassung.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Gezieltes Diagnosejournal; keine Reparaturen.
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Alle Diagnosebereiche; keine Reparatur, solange `--auto-repair` nicht ausdrücklich angegeben ist.
- `gateway-check`: Begrenzte ICMP-Prüfungen der genau konfigurierten nächsten Standard-Hops; kein Adressscan.
- `infrastructure-service-events`: Liest eine begrenzte, genaue Momentaufnahme von Dienstfehlern für Konnektivität, Remoteverwaltung, Benachrichtigungen und Kerndienste.
- `investigate-batch FILE [FILE ...]`: Bis zu 12 Dateien; JSON Lines; Exitcode 3 bei fehlgeschlagenen oder teilweisen Importen.
- `licensing-check`: Schreibgeschützter Windows-Aktivierungszustand ohne Produktschlüssel; JSON-Nachweise.
- `mac-vendor PACK_PATH MAC_ADDRESS`: Lokale Paketsuche mit dem längsten Präfix; keine Netzwerkanfrage.
- `ncsi-policy`: Schreibgeschützte Richtlinie für die Windows-Konnektivitätseinstufung; Testziele ausgeschlossen.
- `neighbor-cache`: Schreibgeschützte, datenschutzbegrenzte Nachweise zur Erreichbarkeit von Nachbarn; JSON-Ergebnis.
- `ntfs-self-healing DRIVE:`: Schreibgeschützte Konfiguration der NTFS-Reparatur im laufenden Betrieb für das gewählte Volume; keine Reparatur, keine Einstellungsänderung.
- `pagefile-check`: Schreibgeschützte Übersicht über Verwaltung, Zuteilung und Nutzung der Auslagerungsdatei; vollständige Pfade ausgeschlossen.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Genaue lokale Paketsuche; keine Netzwerkanfrage, keine Treiberauswahl.
- `physical-disks`: Schreibgeschützter aktueller Zustand zu Integrität, Betrieb, Medium, Bus und Nutzung; JSON; keine Reparatur.
- `platform-service-events`: Liest eine begrenzte, genaue Momentaufnahme von Dienstfehlern für Anwendungen, Identität, Profile und Geräteunterstützung.
- `previous-windows [--execute --backup-confirmed]`: Standardmäßig Vorschau; entfernt die Rollback-Möglichkeit von Windows dauerhaft über DISM.
- `previous-windows-recover`: Schreibgeschützter Abgleich, ohne das Entfernen des Rollbacks zu wiederholen.
- `provisioned-packages`: Für neue Benutzer bereitgestellte Pakete im lokalen Windows; kein Entfernen.
- `recipes [WORKFLOW]`: Listet installierte Definitionen oder workflowspezifische Abhängigkeiten und Grenzen; keine Ausführung.
- `repair <dism|sfc> [--execute]`: Standardmäßig Vorschau.
- `reserved-storage`: Schreibgeschützter Zustand des reservierten Speichers im laufenden Windows; JSON-Nachweise.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Standardmäßig Vorschau; erstellt nach aktuellen Prüfungen genau einen Punkt.
- `restore-point-recover`: Überprüft eine ausstehende Anfrage, ohne einen weiteren Punkt zu erstellen.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Nur Vorschau.
- `shadow-copies`: Schreibgeschützte Zustände und Attribute aktueller VSS-Momentaufnahmen; Identitäten ausgeschlossen; keine Änderungen an Momentaufnahmen.
- `shadow-storage`: Schreibgeschützte Zuteilung und Auslastung des VSS-Differenzbereichs; JSON; keine Größenänderung, keine Aktion an Momentaufnahmen.
- `uac-policy`: Schreibgeschützte UAC-Aktivierung und Richtlinie für Erhöhungsabfragen; keine Konto- oder Anmeldedaten.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Genaue lokale Suche in der UEFI-Registrierung; keine Netzwerkanfrage, keine Treiberauswahl.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Genaue lokale Paketsuche; keine Netzwerkanfrage, keine Treiberauswahl.
- `vitals-check`: Schreibgeschützte Momentaufnahmen von Windows, Hardware, Konto, Ressourcen, Akku und physischen Datenträgern; JSON-Nachweise.
- `win32-code DECIMAL_OR_0xHEX`: Offline-Suche in den Meldungen des installierten Windows.
- `workflows`: Listet installierte Workflow-Schlüssel und -Titel; keine Prüfung, keine Reparatur.
