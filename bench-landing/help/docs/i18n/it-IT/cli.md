# Riga di comando

`killerbench-cli` esegue lo stesso Engine dell'app desktop da PowerShell o da una console remota. Ogni comando che potrebbe modificare il computer mostra un'anteprima per impostazione predefinita e richiede un'opzione esplicita, come `--execute`, prima di agire. I comandi stampano le evidenze JSON sull'output standard e l'avanzamento sull'errore standard.

Guide passo passo: [Eseguire KillerBench dagli script](use-the-cli.html).

Il codice di uscita 0 indica che l'anteprima o il risultato verificato è riuscito, 1 un errore di avvio, di input o del giornale, 2 che gli argomenti non sono stati riconosciuti, 3 bloccato, non riuscito o non verificato, 4 che un flusso di lavoro richiede un riavvio prima di poter verificare e 130 che l'hai fermato tu.

Esegui `killerbench-cli` senza argomenti per stampare questo elenco sul tuo computer.

## Aggiornamenti e manutenzione

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Esame esatto di un trasferimento o osservazione su due istantanee; nessun nuovo tentativo.
- `bits-jobs [--all-users]`: Stati, proprietari e conteggi di byte dei processi BITS; nessuna modifica ai trasferimenti.
- `bits-retry JOB_GUID [--execute]`: Anteprima per impostazione predefinita; nuovo tentativo esplicito di un processo esatto, senza affermare che l'applicazione abbia completato.
- `bits-retry-recover JOB_GUID`: Due istantanee in sola lettura; archivia gli esiti stabili senza un altro tentativo.
- `component-repair-source`: Criterio gestito dell'origine di riparazione DISM e conflitti; nessuna modifica.
- `delivery-optimization`: Stato del servizio Ottimizzazione recapito in sola lettura; evidenze JSON.
- `delivery-optimization-activity`: Conteggi attuali dei processi e totali di byte limitati per la privacy; nessuna identità dei trasferimenti né modifica.
- `delivery-optimization-policy`: Impostazioni classiche e Policy CSP in sola lettura; valori sensibili e identità dei provider esclusi.
- `feature-service-events`: Legge la cronologia esatta degli errori dei servizi di ora, ricerca, WMI, registro eventi e audio.
- `feature-update-policy`: Criteri di versione di destinazione, rinvio, pausa e protezione in sola lettura; evidenze JSON.
- `pending-file-operations`: Riepilogo delle operazioni sui file in sospeso limitato per la privacy, in sola lettura; evidenze JSON.
- `restart-check`: Origini di riavvio in sospeso in sola lettura; evidenze JSON.
- `servicing-logs`: Analisi delimitata del log CBS più recente, degli archivi CBS in testo semplice e compressi e dei log di DISM e Windows Setup; nessuna riparazione.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Istantanee dell'attività di manutenzione in sola lettura.
- `servicing-service-events`: Cronologie esatte degli errori della catena di manutenzione di Windows Update, in sola lettura.
- `servicing-service-states`: Stato attuale dei sei servizi della catena di manutenzione di Windows Update, in sola lettura.
- `setup-code DECIMAL_OR_0xHEX`: Riferimento documentato dei codici di risultato di Windows Setup; nessuna ispezione del computer né riparazione.
- `update-applicability`: Evidenze su edizione, build e architettura di Windows, in sola lettura.
- `update-cache`: Totali delimitati della cache SoftwareDistribution in sola lettura; nessun nome di file.
- `update-code DECIMAL_OR_0xHEX`: Riferimento documentato dei codici di risultato di Windows Update; nessuna ispezione del computer né riparazione.
- `update-context`: Evidenze sull'elevazione attuale e sulla sessione desktop in sola lettura; nessun identificatore di account.
- `update-engine-files`: Evidenze su presenza, versione e firma del motore nativo di Windows Update, in sola lettura.
- `update-events`: Cronologia del client Windows Update degli ultimi sette giorni in sola lettura; JSON con le evidenze grezze.
- `update-followup DECIMAL_OR_0xHEX`: Nuove diagnosi in sola lettura scelte in base a un codice di risultato documentato di Windows Update.
- `update-history`: I 100 esiti più recenti delle operazioni di Windows Update, in sola lettura; evidenze JSON.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: Ripeti `--select`; anteprima per impostazione predefinita.
- `update-policy`: Criteri gestiti di Windows Update in sola lettura; evidenze JSON.
- `update-policy-surfaces`: Evidenze sulla superficie di configurazione classica e Policy CSP in sola lettura; valori e identità dei provider esclusi.
- `update-recover`: Controlla un aggiornamento in sospeso; non lo installa né lo ritenta mai.
- `update-restart-policy`: Scadenze di conformità, periodi di tolleranza, soppressione dei riavvii e comportamento delle notifiche, in sola lettura.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: Individuazione e corrispondenze in JSON; nessuna installazione.
- `update-service-events`: Cronologia esatta degli errori del servizio Windows Update, in sola lettura.
- `update-service-files`: Evidenze esatte di registrazione e firma della DLL del servizio Windows Update, in sola lettura.
- `update-services`: Registrazioni dei servizi Windows Update in sola lettura; evidenze JSON.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Subordinato a una nuova ricerca; anteprima per impostazione predefinita; conserva il vecchio archivio.
- `update-store-reset-recover`: Riconcilia una reimpostazione dell'archivio preparata senza ripeterla.
- `windows-capabilities`: Stati delle capacità di Windows online in sola lettura; risultato JSON.
- `windows-update-task`: Stato esatto e cronologia di esecuzione dell'attività Scheduled Start, in sola lettura; nessuna esecuzione di attività né operazione di aggiornamento.

## BitLocker e TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Anteprima per impostazione predefinita; attiva lo sblocco automatico per un volume dati crittografato.
- `bitlocker-auto-unlock-recover`: Verifica una richiesta di sblocco automatico interrotta senza ripeterla.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Anteprima per impostazione predefinita; solo un nuovo file di backup del ripristino.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Verifica un backup interrotto senza scriverlo di nuovo.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Anteprima per impostazione predefinita; avvia la decrittografia, per esempio prima di crittografare di nuovo con un metodo obbligatorio.
- `bitlocker-decrypt-recover`: Verifica una richiesta di decrittografia interrotta senza ripeterla.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Anteprima per impostazione predefinita; richiede protezioni TPM e di ripristino esistenti.
- `bitlocker-encrypt-recover`: Verifica lo stato di conversione in sospeso senza ripetere la richiesta.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Anteprima per impostazione predefinita; salva una password di ripristino esistente in Active Directory o Microsoft Entra ID.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Solo anteprima delle opzioni.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Anteprima per impostazione predefinita; aggiunge una protezione TPM o con password di ripristino; la password non viene mai stampata.
- `bitlocker-protector-recover`: Verifica una richiesta di protezione interrotta senza ripeterla.
- `bitlocker-protectors DRIVE:`: Solo identità e tipi delle protezioni; nessuna password di ripristino.
- `bitlocker-resume DRIVE: [--execute]`: Anteprima per impostazione predefinita; solo un volume esatto completamente crittografato e sospeso.
- `bitlocker-resume-recover`: Verifica lo stato di protezione in sospeso senza ripetere la richiesta.

## Sicurezza e privacy

- `certificate-chains`: Attendibilità della catena locale solo dalla cache per i certificati personali, in sola lettura; nessuna revoca né download.
- `defender-overview [--report]`: Panoramica di protezione, pianificazione delle analisi e minacce aggregate, in sola lettura; nessuna azione.
- `defender-protection [--report]`: Preferenze di protezione e superfici dei criteri riconosciute, in sola lettura; dettagli sensibili esclusi; nessuna modifica.
- `defender-scan Quick|Full [--execute] [--report]`: Anteprima per impostazione predefinita; un'unica analisi esplicita e protetta di Microsoft Defender.
- `defender-scan-history [--report]`: Tempi delle analisi rapida e completa e stato di ritardo, in sola lettura; dettagli sensibili esclusi; nessuna analisi.
- `defender-scan-recover [--report]`: Riconciliazione in sola lettura senza avviare un'altra analisi.
- `defender-threats [--report]`: Cronologia aggregata delle minacce, indicatori attivi attuali e ID di gravità; dettagli sensibili esclusi; nessuna azione.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Anteprima per impostazione predefinita; solo profili locali non gestiti.
- `firewall-enable-recover`: Ispeziona una modifica in sospeso senza ripeterla.
- `firewall-events`: Legge la cronologia operativa delimitata di avvisi, errori ed eventi critici di Windows Firewall.
- `firewall-logging`: Legge la configurazione della registrazione del traffico dei profili Dominio, Privato e Pubblico senza aprire i log.
- `firewall-rules`: Legge conteggi di regole effettive e di origini dei criteri limitati per la privacy.
- `firewall-service-events`: Legge la cronologia esatta degli errori di BFE e Windows Defender Firewall in Gestione controllo servizi.
- `privacy-clipboard-history on|off [--execute]`: Anteprima per impostazione predefinita; valore esatto dell'utente corrente, con i criteri conservati.
- `privacy-clipboard-history-recover`: Ricontrolla una modifica interrotta senza ripeterla.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: Ripristino protetto del valore precedente.
- `privacy-plan --select Camera --select ClipboardSync`: Solo esame delle selezioni; nessuna impostazione modificata.
- `privacy-setting KEY on|off [--execute]`: Anteprima per impostazione predefinita; modifica dell'utente corrente che conserva i criteri; le chiavi non valide elencano le scelte supportate.
- `privacy-setting-recover KEY`: Ricontrolla una modifica interrotta senza ripeterla.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: Ripristino protetto del valore esatto.
- `security-check`: TPM, Avvio protetto, protezioni di virtualizzazione e criteri UAC in sola lettura; evidenze JSON.

## Servizi

- `service-binaries`: Presenza dei binari dei servizi locali e stato Authenticode, in sola lettura; percorsi esclusi e percorsi non locali non contattati.
- `service-check NAME [NAME ...] | service-check --all`: Stati dei servizi supportati ed evidenze sulle dipendenze, in sola lettura.
- `service-dependencies`: Esame dell'integrità delle dipendenze dirette in sola lettura; nessuna modifica ai servizi.
- `service-dlls`: Presenza delle DLL dei servizi registrate e stato Authenticode, in sola lettura; percorsi esclusi e percorsi non locali non contattati.
- `service-events`: Cronologia degli errori dei servizi in sola lettura; JSON con le evidenze grezze.
- `time-events`: Cronologia recente degli eventi di Ora di Windows in sola lettura, con evidenze JSON.
- `service-logons`: Esame della classe di account dei servizi e dell'interazione con il desktop, in sola lettura; identità degli account escluse.
- `service-path-quoting`: Esame delle virgolette nei percorsi degli eseguibili dei servizi, in sola lettura; percorsi completi e argomenti esclusi.
- `service-recovery-policy`: Riepilogo delle azioni di ripristino dei servizi installati, in sola lettura; comandi e messaggi di riavvio esclusi.
- `service-startup-config`: Esame della coerenza delle modalità di avvio in sola lettura; nessuna modifica ai servizi.
- `service-transitions`: Osservazione ripetuta degli stati in sospeso dei servizi, in sola lettura; nessuna azione sui servizi.
- `service-triggers`: Riepilogo dei trigger dei servizi installati, in sola lettura; payload dei trigger esclusi; nessuna modifica.

## Archiviazione, file e backup

- `backup-events`: Cronologia di VSS e volsnap; evidenze JSON; nessuna modifica.
- `backup-service-events`: Legge la cronologia esatta degli errori di VSS, del provider di copie shadow software, di Backup di Windows e del motore a livello di blocco.
- `backup-summary`: Riepilogo delle operazioni locali di Backup di Windows; nessuna modifica ai backup.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Anteprima per impostazione predefinita; il Cestino è tutto o niente e l'età non si applica.
- `disk-cleanup-task`: Stato esatto e cronologia di esecuzione dell'attività, in sola lettura; nessuna esecuzione di attività né pulizia.
- `disk-diagnostic`: Mette in relazione le attività esatte di diagnostica del disco e la cronologia degli eventi; nessuna azione su attività o dischi.
- `disk-diagnostic-tasks`: Cronologia esatta delle attività di raccolta e risoluzione, in sola lettura; nessuna azione su attività o dischi.
- `disk-quotas`: Quote predefinite e stato di ogni volume, in sola lettura; identità degli utenti escluse; nessuna modifica.
- `file-history-events`: Conteggi di avvisi ed errori di Cronologia file con proiezione per la privacy; nessuna modifica.
- `file-history-status`: Stato di protezione di Cronologia file dell'account corrente; nessuna modifica.
- `search-index-rebuild [--execute]`: Anteprima per impostazione predefinita; conserva l'indice precedente e verifica il nuovo stato.
- `search-index-rebuild-recover`: Riconciliazione in sola lettura senza ripetere i comandi di riparazione.
- `storage-check DRIVE_LETTER`: Controllo NTFS fisso in sola lettura; risultati JSON e avanzamento in tempo reale.
- `storage-events`: Cronologia dell'archiviazione in sola lettura; JSON con le evidenze grezze.
- `storage-jobs`: Stato e avanzamento aggregati dei processi del modulo Storage, in sola lettura; JSON; nessuna modifica.
- `storage-reliability`: Contatori di temperatura, usura e I/O in sola lettura; JSON; nessuna riparazione.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Anteprima per impostazione predefinita; l'esecuzione richiede la conferma del backup; i volumi di sistema vengono pianificati al riavvio.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Solo anteprima; nessuna riparazione né pianificazione.
- `storage-repair-recover`: Verifica una riparazione del file system completata o pianificata senza ripeterla.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Raccoglie e associa le evidenze attuali; nessuna riparazione né pianificazione.
- `storage-sense-policy`: Criteri di pulizia del computer riconosciuti, in sola lettura; impostazioni utente e identità dei file escluse.
- `storage-sense-task`: Stato esatto e cronologia di esecuzione dell'attività, in sola lettura; nessuna esecuzione di attività né pulizia.
- `storage-service-events`: Legge la cronologia esatta degli errori di Servizio di archiviazione, Ottimizza unità, VSS e del provider di copie shadow software.
- `volume-capacity DRIVE:`: Spazio disponibile in sola lettura, con evidenze JSON.
- `volume-dirty DRIVE:`: Stato del bit dirty in sola lettura; nessuna riparazione né riavvio.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Analisi in sola lettura per impostazione predefinita; richiesti un volume esatto sano e un supporto compatibile.
- `volume-optimize-recover`: Verifica un'operazione in sospeso completata senza ripeterla.
- `vss-providers`: Nomi, versioni e tipi dei provider VSS, in sola lettura; identificatori esclusi; nessuna modifica alle registrazioni.
- `vss-writers`: Stati dei writer riportati; interpretazione dell'output in inglese; nessuna reimpostazione.

## Stampa

- `print-events`: Cronologia di amministrazione di PrintService degli ultimi sette giorni, in sola lettura; evidenze JSON.
- `print-extension-files`: Riepilogo di presenza e firma dei binari di processori e monitor di stampa, in sola lettura; percorsi completi esclusi.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Anteprima per impostazione predefinita; solo un processo esatto in stato Errore o Bloccato.
- `print-job-cancel-recover [--folder PATH]`: Ricontrolla un annullamento interrotto senza ripeterlo.
- `print-job-list`: Elenca i processi visibili attuali senza modificarli.
- `print-operational-events`: Stato del canale operativo di PrintService e cronologia disponibile, in sola lettura; evidenze JSON.
- `print-policy`: Restrizioni Point and Print e criteri di installazione dei driver, in sola lettura; evidenze JSON.
- `print-rendering-policy`: Criteri di rendering e isolamento dei driver in sola lettura; evidenze JSON.
- `print-spooler-events`: Errori di Gestione controllo servizi legati all'identità locale di Spooler di stampa, in sola lettura; evidenze JSON.
- `printer-check --queue NAME [--queue NAME ...]`: Impostazioni della coda locale selezionata, in sola lettura.
- `printer-conditions`: Codici di condizione delle code locali in sola lettura; nessuna verifica fisica né modifica.
- `printer-driver-files`: Riepilogo di presenza dei file registrati e firma degli eseguibili, in sola lettura; percorsi completi esclusi.
- `printer-driver-models`: Riepilogo versione 3, versione 4 e supporto dei pacchetti, in sola lettura; evidenze JSON.
- `printer-drivers`: Inventario e corrispondenza delle registrazioni di code e driver, in sola lettura; evidenze JSON.
- `printer-endpoint-check`: Connessioni delimitate agli endpoint esatti assegnati delle stampanti TCP/IP standard; nessuna individuazione, SNMP né dati di stampa.
- `printer-list`: Individua le code locali senza modificare le impostazioni.
- `printer-ports`: Corrispondenza delle registrazioni di code e porte, in sola lettura; evidenze JSON.
- `printer-queue-attributes`: Attributi di spooling, conservazione e condivisione delle code locali, in sola lettura.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Anteprima per impostazione predefinita; disattivare la modalità offline può rilasciare i processi in attesa.

## Rete

- `dhcp-check`: Idoneità dei lease DHCP mancanti o scaduti, in sola lettura; evidenze JSON, nessun rinnovo.
- `dhcp-events`: Cronologia degli eventi del client DHCP IPv4 e IPv6, in sola lettura; risultato JSON.
- `dhcp-leases`: Evidenze su timestamp, scadenza e server del lease, in sola lettura; risultato JSON.
- `dhcp-renew [--execute]`: Anteprima per impostazione predefinita; solo lease DHCP esatti mancanti o scaduti di schede fisiche connesse.
- `dhcp-renew-recover`: Riconciliazione in sola lettura senza richiedere un altro lease.
- `dns-cache HOST [--execute]`: Anteprima per impostazione predefinita; svuotamento protetto della cache locale con verifica dell'host esatto.
- `dns-cache-recover`: Ricontrolla un'operazione sulla cache interrotta senza ripetere lo svuotamento.
- `dns-cache-summary`: Stato della cache DNS locale e conteggi per tipo di record, limitati per la privacy; nessuna modifica.
- `dns-check HOST [--timeout-seconds 1..60]`: Risoluzione dei nomi di sistema delimitata; risultato JSON.
- `dns-client-config`: Evidenze su suffissi, registrazione, devoluzione e criteri, in sola lettura; risultato JSON.
- `dns-client-events`: Cronologia di avvisi ed errori del client DNS limitata per la privacy, in sola lettura; risultato JSON.
- `dns-nrpt-policy`: Evidenze sul criterio effettivo dello spazio dei nomi DNS, in sola lettura; risultato JSON.
- `ip-addresses`: Esame di stato, origine e conflitti degli indirizzi IP attuali, in sola lettura; evidenze JSON.
- `ip-interface-config`: Evidenze sulla configurazione delle interfacce IP in sola lettura; risultato JSON.
- `network-firewall`: Mette in relazione le assegnazioni di rete attive con l'attivazione del profilo firewall corrispondente.
- `network-offload`: Configurazione globale dell'offload di rete di Windows in sola lettura; risultato JSON.
- `network-profiles`: Categorie attuali dei profili e classificazioni della connettività, in sola lettura; nomi delle reti esclusi.
- `network-service-events`: Legge la cronologia esatta degli errori di DHCP, DNS, Workstation, NLA, Servizio Elenco reti e Helper IP.
- `proxy-check`: Evidenze sul proxy WinHTTP salvato e sul proxy Internet dell'account corrente, in sola lettura.
- `route-priority`: Confronto della metrica effettiva della route predefinita, in sola lettura; evidenze JSON.
- `smb-client-config`: Configurazione di firma, accesso guest e trasporto SMB, in sola lettura.
- `smb-connections`: Negoziazione del protocollo attiva in sola lettura; identità remote escluse.
- `smb-server-config`: Configurazione di protocollo, firma, crittografia e multicanale SMB, in sola lettura.
- `smb-shares`: Inventario delimitato delle condivisioni SMB in sola lettura; percorsi, autorizzazioni, sessioni e account esclusi.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Controllo della connessione delimitato; nessun dato delle applicazioni inviato.
- `tcp-port-pressure`: Intervalli TCP dinamici e uso aggregato delle porte locali, in sola lettura; endpoint e processi esclusi.
- `tcp-settings`: Evidenze sulla configurazione dei modelli TCP di Windows, in sola lettura; risultato JSON.
- `vpn-profiles`: Configurazione dei profili VPN dell'account corrente in sola lettura; credenziali escluse.
- `winrm-listeners`: Riepilogo della registrazione dei listener WinRM in sola lettura; identificazioni digitali dei certificati escluse.
- `winsock-catalog`: Struttura del catalogo nativo dei protocolli Winsock, in sola lettura; risultato JSON.
- `winsock-namespaces`: Struttura nativa dei provider di spazi dei nomi Winsock, in sola lettura; risultato JSON.
- `winsock-provider-files`: Presenza e firme dei file dei provider di protocollo, in sola lettura; risultato JSON.
- `winsock-readiness`: Disponibilità nativa di protocolli, provider di spazi dei nomi e file dei provider Winsock, in sola lettura; risultati JSON.

## App

- `app-folder-review FOLDER_NAME [--report]`: Esame di cartelle esatte con radice fissa; nessuna eliminazione.
- `app-package-review FULL_NAME [--all-users]`: Destinazione esatta e pacchetti dipendenti; nessuna rimozione.
- `app-packages [--all-users]`: Solo inventario dei pacchetti Appx/MSIX registrati; nessuna rimozione.
- `browser-extensions`: Esame delle identità delle estensioni di Chrome, Edge, Brave e Firefox; nessuna rimozione.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Solo esame; nessuna modifica al sistema.
- `depot-review [application and configuration selections] [--json]`: Solo esame; nessun provisioning.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Controllo Authenticode e del firmatario del produttore in sola lettura; non viene eseguito nulla.
- `foxit-inventory`: Inventario dei prodotti del computer in sola lettura; output JSON.
- `installed-apps [--current-user]`: Registrazioni di disinstallazione e indicatori di capacità; nessuna rimozione.
- `installed-apps-compare BEFORE.json AFTER.json`: Differenze di registrazione rispetto a un inventario salvato; nessuna modifica al computer.
- `installed-hotfixes`: Inventario delimitato degli hotfix di Windows installati, in sola lettura; evidenze JSON.
- `installer-activity`: Evidenze su processi di Installer e marcatori di transazione, in sola lettura; evidenze JSON.
- `installer-check`: Criteri di Installer e disponibilità del motore, in sola lettura; evidenze JSON.
- `installer-engine-files`: Riepilogo di presenza e Authenticode dei file richiesti da Installer, in sola lettura; evidenze JSON.
- `installer-events`: Cronologia MSI degli ultimi sette giorni in sola lettura; evidenze JSON.
- `installer-logging`: Criteri di registrazione e debug di Installer, in sola lettura; evidenze JSON.
- `installer-pending-state`: Correlazione tra operazioni sui file in sospeso e attività di Installer, in sola lettura; evidenze JSON.
- `installer-registration-repair [--execute]`: Anteprima per impostazione predefinita; solo un difetto esatto di registrazione del servizio; non annulla mai la registrazione del motore.
- `installer-registration-repair-recover`: Ricontrolla una registrazione interrotta senza ripetere i comandi.
- `installer-security-policy`: Criteri di sicurezza e patch di Installer, in sola lettura; evidenze JSON.
- `installer-service-events`: Errori di Gestione controllo servizi legati all'identità locale di Windows Installer, in sola lettura; evidenze JSON.
- `installer-service-registration`: Riepilogo della registrazione del servizio Windows Installer, in sola lettura; evidenze JSON.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: Nuova anteprima dell'idoneità esatta; nessun provider di rimozione.
- `mace-package-recover`: Riconcilia di nuovo un record interrotto di rimozione di un pacchetto di app; non ripete mai la rimozione.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Solo esame; nessuna rimozione.
- `msi-cache`: Visibilità della cache registrata di Windows Installer, in sola lettura; evidenze JSON.
- `msi-code NONNEGATIVE_DECIMAL`: Riferimento documentato dei codici restituiti da Windows Installer; nessuna ispezione del computer né riparazione.
- `msi-product {PRODUCT-CODE}`: Stato esatto della registrazione MSI in sola lettura; evidenze JSON.
- `msi-repair {PRODUCT-CODE} [--execute]`: Anteprima per impostazione predefinita; solo un prodotto esatto danneggiato o mal configurato.
- `msi-repair-recover`: Ricontrolla la riparazione interrotta di un prodotto esatto senza ripeterla.
- `msi-sources`: Metadati delle origini registrate di Windows Installer limitati per la privacy, in sola lettura; evidenze JSON.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Anteprima per impostazione predefinita; conserva le cache esatte dell'account in esecuzione con nomi di ripristino.
- `orca-cache-reset-recover`: Verifica una conservazione della cache interrotta senza ripetere uno spostamento.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Solo esame; nessuna modifica a Outlook.
- `startup-apps`: Esame delle registrazioni di avvio e delle attività all'avvio o all'accesso; nessuna disabilitazione.
## Sistema

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Ricerca locale esatta delle assegnazioni Bluetooth SIG; nessuna richiesta di rete.
- `crash-config`: Modalità e percorsi dei dump di Windows salvati; nessuna modifica.
- `crash-dumps`: Metadati dei file di dump predefiniti di Windows; nessuna analisi del contenuto né modifica.
- `crash-events`: Rapporti di errore di sistema recenti; nessuna diagnosi né modifica.
- `crash-review [--knowledge PACK_PATH ...]`: Fino a 32 pacchetti; esame combinato di arresti anomali, spegnimenti, struttura delimitata dei minidump e, facoltativamente, del riferimento esatto di controllo bug; nessuna modifica.
- `default-printer`: Selezione predefinita e impostazione di gestione automatica, in sola lettura; evidenze JSON.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Anteprima per impostazione predefinita; solo se la predefinita manca o è in conflitto.
- `default-printer-set-recover [--folder PATH]`: Ricontrolla una selezione interrotta senza ripeterla.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: Evidenze sui dispositivi in sola lettura con riferimenti esatti facoltativi ai driver da un massimo di 32 pacchetti.
- `device-code DECIMAL_OR_0xHEX`: Riferimento offline dei codici di problema di Gestione dispositivi.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Solo candidati di riferimento.
- `event-channel-enable CHANNEL [--execute]`: Anteprima per impostazione predefinita; abilita un singolo canale diagnostico esatto non gestito e conserva le altre impostazioni.
- `event-channel-enable-recover`: Riconciliazione in sola lettura senza ripetere il comando di abilitazione.
- `event-channels`: Stato, conservazione, dimensione e saturazione di quattro canali diagnostici, in sola lettura.
- `font-registrations`: Controllo delimitato di registrazioni e presenza dei file del computer e dell'utente corrente, in sola lettura.
- `memory-diagnostic-results`: Cronologia delimitata di Diagnostica memoria Windows in sola lettura; evidenze JSON.
- `minidump-analyze PATH`: Metadati strutturali delimitati dei minidump; nessuna pagina di memoria, simbolo o modifica.
- `name-rename PROPOSED_NAME [--execute]`: Anteprima per impostazione predefinita; solo PC autonomi in gruppo di lavoro; non riavvia mai Windows.
- `name-rename-recover`: Verifica una ridenominazione in sospeso senza ripeterla.
- `name-review PROPOSED_NAME`: Esame dell'identità e del nome proposto, in sola lettura.
- `profiles --recover`: Verifica l'assenza registrata e conserva le evidenze di ripristino; non ritenta mai l'eliminazione.
- `profiles --recover-backup`: Verifica e archivia un artefatto di backup in sospeso; non ritenta mai la copia.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Prima l'anteprima; la copia nativa protetta richiede un'esecuzione esplicita e una destinazione NTFS locale inesistente.
- `profiles --select SID --create-backup-artifact DESTINATION`: Confronta un backup già copiato e crea il relativo artefatto fisso; non copia né rimuove alcun profilo.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Esame in sola lettura; la verifica richiede un SID selezionato.
- `task-check`: Inventario delle attività, rilevamenti classificati ed evidenze JSON, in sola lettura.
- `task-definition TASK_FOLDER TASK_NAME`: XML esatto dell'attività in sola lettura; non disponibile non significa assente.
- `task-inventory`: Identità e stati delle attività in sola lettura; output JSON.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Esame storico del ripristino; non autorizza mai un nuovo tentativo.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Convalida ed esamina una bozza di attività; nessuna registrazione.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Anteprima per impostazione predefinita; definizione disabilitata di sola creazione con Sistema locale.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Riconciliazione in sola lettura della definizione esatta; non ritenta né esegue mai un'attività.
- `task-xml PLAN.json YYYY-MM-DD`: XML di esame disabilitato sull'output standard; solo pianificazioni di calendario nel fuso orario locale; nessuna registrazione.
- `time-config`: Evidenze su modalità, peer e criteri di Ora di Windows, in sola lettura.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Anteprima per impostazione predefinita; la modalità personalizzata richiede dei peer.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Solo anteprima.
- `time-recover --folder PATH`: Verifica e archivia un record in sospeso; non ritenta mai le modifiche.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Sola lettura; la modalità personalizzata richiede dei peer.
- `wmi-provider`: Query locale del provider del sistema operativo, in sola lettura; evidenze JSON.

## Conoscenze, convalida ed Engine

- `knowledge PACK_PATH CONTEXT CODE`: Ricerca di riferimento locale; nessuna esecuzione di riparazioni.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Attivazione locale verificata; nessun download.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Verifica il pacchetto locale attivo; nessuna modifica.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Solo modifiche di riferimento; nessuna installazione di pacchetti.
- `knowledge-info PACK_PATH`: Metadati del pacchetto e conteggi delle voci per contesto; nessuna modifica.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Associa riferimenti contestuali ai codici osservati.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Seleziona il pacchetto conservato verificato più recente; nessun download.
- `knowledge-search PACK_PATH CONTEXT TEXT`: Fino a 50 corrispondenze nel testo di riferimento; nessuna esecuzione di riparazioni.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Risolve un'identità esatta in un massimo di 32 pacchetti; i conflitti non vengono mai selezionati.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Verifica ogni database installato configurato; nessun download né attivazione.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Recupera e attiva in modo atomico database candidati firmati dagli endpoint HTTPS configurati.
- `open <file.kbench> [--json]`: Riapre una cattura salvata e la valuta di nuovo con le regole installate.
- `scan --output <new-file.kbench> [--json]`: Cattura di Windows Update in sola lettura salvata in un nuovo file.
- `validation-coverage RECORD.json [...]`: Riepiloga le evidenze native su VM della versione attuale; nessuna diagnosi né riparazione.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Piano di scenari su VM usa e getta in sola lettura; nessuna diagnosi né riparazione.
- `validation-record RECORD.json`: Convalida un record delimitato di evidenze native su VM; nessuna diagnosi né riparazione.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Rapporto locale di copertura su VM della versione attuale.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Esegue un piano di opzioni condiviso convalidato in sola lettura.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Solo inventario.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Giornale diagnostico mirato; nessuna riparazione.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Solo esame registrato nel giornale.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: Stato esatto facoltativo del prodotto incluso nel giornale.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: Diagnosi facoltativa della destinazione selezionata inclusa nel giornale.
- `workflow storage [--volume D:] [--json]`: Controllo del file system in sola lettura registrato nel giornale.
- `workflow-journal FILE.json`: Contenuto storico; l'uscita 0 significa letto correttamente, non sano.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Mostra o convalida controlli indipendenti dall'host; nessuna analisi né riparazione.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Solo rapporto storico; nessun comando eseguito.

## Altri comandi

- `<analyze|investigate> <text-log-path> [--json]`: Analizza evidenze di testo locali.
- `adapter-bindings`: Riepilogo dei binding dei protocolli principali, in sola lettura; evidenze JSON.
- `adapter-statistics`: Contatori di byte, errori e scarti delle schede, in sola lettura; evidenze JSON.
- `application-events`: Errori recenti del registro Applicazione, conteggi raggruppati ed evidenze JSON, in sola lettura.
- `applocker-policy`: Modalità di raccolta effettive e conteggi aggregati delle regole, in sola lettura; dettagli delle regole esclusi; nessuna modifica.
- `common-check [--scope NAME ...]`: Tutte le aree diagnostiche per impostazione predefinita; solo le aree selezionate se specificate; JSON in sola lettura.
- `delete-notifications`: Configurazione della notifica di eliminazione NTFS e ReFS, in sola lettura; JSON; nessuna ottimizzazione né modifica delle impostazioni.
- `diagnostic-scopes [--all]`: Elenca 11 gruppi di risultati consigliati, oppure tutte le 49 aree in sola lettura; nessuna raccolta.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Giornale diagnostico mirato; nessuna riparazione.
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Tutte le aree diagnostiche; nessuna riparazione a meno che `--auto-repair` non sia esplicito.
- `gateway-check`: Controlli ICMP delimitati dei next hop predefiniti esatti configurati; nessuna scansione degli indirizzi.
- `infrastructure-service-events`: Legge un'istantanea delimitata ed esatta degli errori dei servizi di connettività, gestione remota, notifiche e principali.
- `investigate-batch FILE [FILE ...]`: Fino a 12 file; JSON Lines; uscita 3 per importazioni non riuscite o parziali.
- `licensing-check`: Stato di attivazione di Windows senza codici Product Key, in sola lettura; evidenze JSON.
- `mac-vendor PACK_PATH MAC_ADDRESS`: Ricerca nel pacchetto locale per prefisso più lungo; nessuna richiesta di rete.
- `ncsi-policy`: Criteri di classificazione della connettività di Windows, in sola lettura; destinazioni dei probe escluse.
- `neighbor-cache`: Evidenze sulla raggiungibilità dei vicini limitate per la privacy, in sola lettura; risultato JSON.
- `ntfs-self-healing DRIVE:`: Configurazione della riparazione NTFS online del volume selezionato, in sola lettura; nessuna riparazione né modifica delle impostazioni.
- `pagefile-check`: Riepilogo di gestione, allocazione e uso del file di paging, in sola lettura; percorsi completi esclusi.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Ricerca esatta nel pacchetto locale; nessuna richiesta di rete né selezione di driver.
- `physical-disks`: Stato attuale di integrità, operativo, dei supporti, del bus e di utilizzo, in sola lettura; JSON; nessuna riparazione.
- `platform-service-events`: Legge un'istantanea delimitata ed esatta degli errori dei servizi di applicazioni, identità, profili e supporto dei dispositivi.
- `previous-windows [--execute --backup-confirmed]`: Anteprima per impostazione predefinita; rimuove in modo permanente la capacità di rollback di Windows tramite DISM.
- `previous-windows-recover`: Riconciliazione in sola lettura senza ripetere la rimozione del rollback.
- `provisioned-packages`: Pacchetti predisposti per i nuovi utenti nel Windows locale; nessuna rimozione.
- `recipes [WORKFLOW]`: Elenca le definizioni installate o le dipendenze e i limiti di un flusso di lavoro specifico; nessuna esecuzione.
- `repair <dism|sfc> [--execute]`: Anteprima per impostazione predefinita.
- `reserved-storage`: Stato dell'archiviazione riservata di Windows online, in sola lettura; evidenze JSON.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Anteprima per impostazione predefinita; crea un punto dopo controlli aggiornati.
- `restore-point-recover`: Verifica una richiesta in sospeso senza creare un altro punto.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Solo anteprima.
- `shadow-copies`: Stati e attributi attuali delle snapshot VSS, in sola lettura; identità escluse; nessuna modifica alle snapshot.
- `shadow-storage`: Allocazione e pressione dell'area differenziale VSS, in sola lettura; JSON; nessun ridimensionamento né azione sulle snapshot.
- `uac-policy`: Abilitazione dell'UAC e criteri della richiesta di elevazione, in sola lettura; nessun dato di account o credenziali.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Ricerca locale esatta nel registro UEFI; nessuna richiesta di rete né selezione di driver.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Ricerca esatta nel pacchetto locale; nessuna richiesta di rete né selezione di driver.
- `vitals-check`: Istantanee di Windows, hardware, account, risorse, batteria e dischi fisici, in sola lettura; evidenze JSON.
- `win32-code DECIMAL_OR_0xHEX`: Ricerca offline dei messaggi nel Windows installato.
- `workflows`: Elenca le chiavi e i titoli dei flussi di lavoro installati; nessuna analisi né riparazione.
