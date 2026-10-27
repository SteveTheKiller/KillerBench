# Aggiornamenti

Aggiornamenti copre Windows Update, i servizi che lo fanno funzionare e l'archivio componenti. Puoi vedere cosa è installato, cosa è in sospeso e perché un aggiornamento non riesce, ed esaminare gli aggiornamenti disponibili prima di scegliere quali installare.

Guide passo passo: [Riparare Windows Update](fix-windows-update.html) e [Riparare i file di sistema](repair-system-files.html).

## Strumenti di questa sezione

Osserva l'attività di manutenzione, esamina gli aggiornamenti disponibili e ripara la catena di Windows Update.

### Monitor attività di manutenzione

Osserva in modo delimitato l'attività di manutenzione di Windows Update e Setup in una o più istantanee. Le opzioni regolano la frequenza di aggiornamento e se includere gli eventi di aggiornamento, la cronologia degli aggiornamenti, l'attività pianificata esatta di Windows Update, gli stati esatti dei servizi, gli indicatori di riavvio, i trasferimenti in background dell'account corrente, l'attività di Ottimizzazione recapito e l'attività di Windows Installer limitata per la privacy. ID dei processi, righe di comando, utenti e percorsi sono esclusi. L'attività non dimostra avanzamento, completamento, blocco o guasto. Non viene richiesta alcuna azione di aggiornamento, installazione, trasferimento, attività, servizio, processo, criterio, riavvio o riparazione.

### Aggiornamenti disponibili

Individua gli aggiornamenti software e driver selezionati usando l'origine configurata, Windows Update o un servizio Microsoft Update già registrato. La disponibilità dell'origine viene controllata prima dell'individuazione. L'individuazione non installa nulla, non accetta licenze, non registra origini e non modifica i criteri. Gli aggiornamenti selezionati possono essere installati in seguito, separatamente, solo dopo aver selezionato la casella di conferma. È separato dalla riparazione della manutenzione.

### Windows Update

Fa un inventario delimitato degli stati dei pacchetti di manutenzione, delle funzionalità facoltative e delle capacità di Windows, controlla gli indicatori di riavvio, lo spazio libero e i servizi di aggiornamento, osserva Ottimizzazione recapito senza avviarla, poi analizza l'archivio componenti e i file di sistema protetti. Gli stati in sospeso di pacchetti, funzionalità o capacità richiedono un esame ma non autorizzano alcuna azione sui componenti. La riparazione automatica può avviare i servizi Windows Update, BITS, Servizi di crittografia, Programma di installazione moduli di Windows e Update Orchestrator arrestati e impostati per l'avvio automatico. Un servizio ad avvio manuale arrestato viene segnalato, non avviato. L'avvio di Update Orchestrator può far riprendere l'attività di aggiornamento pianificata esistente secondo i criteri attuali. Pacchetti, funzionalità facoltative, capacità, cache di Ottimizzazione recapito, peer, larghezza di banda, criteri, registrazione dell'origine, pianificazioni e impostazioni di avvio vengono conservati. DISM o SFC richiedono evidenze in tempo reale di danni. Questo flusso di lavoro non seleziona né approva alcun aggiornamento.

### Servizio Windows Update

Controlla lo stato esatto di Servizio Windows Update, la DLL del servizio registrata, l'attività di aggiornamento attuale delimitata e la cronologia esatta degli errori del servizio. Le osservazioni sulla DLL, l'attività di aggiornamento e gli errori storici non possono autorizzare l'avvio del servizio né un'operazione di aggiornamento. Il ripristino facoltativo avvia solo un servizio automatico arrestato, dopo controlli aggiornati delle dipendenze e della registrazione dell'eseguibile. Un servizio con avvio a richiesta arrestato resta solo informativo. L'attività di aggiornamento esistente può riprendere secondo i criteri attuali. Criteri di aggiornamento, registrazione dell'origine, pianificazioni, impostazioni di riavvio, pacchetti, processi BITS, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona, approva né installa alcun aggiornamento. La verifica copre solo lo stato stabile del servizio, non la raggiungibilità dell'origine, l'applicabilità, il download, l'installazione, il completamento né la disponibilità al riavvio.

### Ottimizzazione recapito

Controlla lo stato esatto del servizio Ottimizzazione recapito. Un servizio con avvio a richiesta arrestato resta solo informativo. Solo uno stato esatto In pausa può offrire il ripristino. Il ripristino facoltativo ripete il controllo dello stato, invia un'unica richiesta di continuazione delimitata e richiede due osservazioni coerenti In esecuzione. Tipo di avvio, contenuto della cache, peer, larghezza di banda, origine degli aggiornamenti e criteri gestiti vengono conservati. Attività di trasferimento, stato della cache, avanzamento e completamento degli aggiornamenti restano non verificati.

### Servizio BITS

Controlla lo stato esatto di Servizio trasferimento intelligente in background, i processi di trasferimento dell'account corrente e la cronologia esatta e delimitata degli errori del servizio. Gli errori storici e le istantanee dei trasferimenti non possono autorizzare l'avvio del servizio. Il ripristino facoltativo avvia solo il servizio esatto quando è arrestato e impostato per l'avvio automatico, dopo controlli aggiornati delle dipendenze. Un servizio ad avvio manuale arrestato resta solo informativo. Le applicazioni esistenti possono riprendere i trasferimenti in background in coda. Processi, priorità, proprietari, destinazioni, impostazioni proxy, criteri e tipo di avvio vengono conservati. La verifica copre solo lo stato stabile del servizio, non l'avanzamento dei trasferimenti, la proprietà delle applicazioni né il completamento.

### Trasferimenti in background (BITS)

Legge i processi di trasferimento BITS nell'ambito account selezionato ed esamina le identità esatte dei processi. I nomi delle code non stabiliscono a quale applicazione appartengono. Nessun trasferimento viene ripreso, completato, annullato, eliminato o riassegnato.

### Servizi di crittografia

Controlla lo stato esatto di Servizi di crittografia, la DLL del servizio registrata e la cronologia esatta e delimitata degli errori del servizio. Gli errori storici e le osservazioni sulla DLL non possono autorizzare l'avvio del servizio né la sostituzione di componenti. Il ripristino facoltativo avvia solo il servizio esatto quando è arrestato e impostato per l'avvio automatico, dopo controlli aggiornati delle dipendenze. Un servizio ad avvio manuale arrestato resta solo informativo. Le operazioni esistenti su cataloghi, certificati e aggiornamenti possono riprendere. Certificati, chiavi, cataloghi, impostazioni di attendibilità, criteri e tipo di avvio vengono conservati. La verifica copre solo lo stato stabile del servizio, non la convalida di certificati, firme, cataloghi o attendibilità.

### Programma di installazione moduli di Windows

Controlla lo stato esatto di Programma di installazione moduli di Windows, le transizioni delimitate dei pacchetti di manutenzione e la cronologia esatta degli errori del servizio. Gli errori storici e gli stati dei pacchetti non possono autorizzare l'avvio del servizio né un'operazione sui pacchetti. Il ripristino facoltativo avvia solo il servizio esatto quando è arrestato e impostato per l'avvio automatico, dopo controlli aggiornati delle dipendenze. Un servizio ad avvio manuale arrestato resta solo informativo. L'attività di manutenzione esistente può riprendere. Pacchetti, contenuto dell'archivio componenti, origini, criteri e tipo di avvio vengono conservati. La verifica copre solo lo stato stabile del servizio, non l'integrità dell'archivio componenti, l'applicabilità dei pacchetti, il completamento della manutenzione né la disponibilità al riavvio.

### Servizio Windows Update Medic

Controlla lo stato esatto di Servizio Windows Update Medic, la DLL del servizio registrata, l'attività di aggiornamento attuale delimitata e la cronologia esatta degli errori del servizio. Le osservazioni sulla DLL, l'attività di aggiornamento e gli errori storici non possono autorizzare l'avvio del servizio né un'azione sui componenti. Un servizio con avvio a richiesta arrestato può essere normale. Quando è selezionato questo strumento esatto, il ripristino facoltativo può avviare solo il servizio abilitato e arrestato, dopo controlli aggiornati delle dipendenze. La correzione di Windows può riprendere secondo i criteri attuali e può riparare componenti di aggiornamento o attivare attività di aggiornamento. Criteri di aggiornamento, registrazione dell'origine, pianificazioni, pacchetti, processi BITS, impostazioni di riavvio, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcun aggiornamento e non modifica alcuna impostazione di aggiornamento. La verifica copre solo lo stato stabile del servizio, non la correzione, l'integrità dei componenti, l'applicabilità, il download, l'installazione né la disponibilità al riavvio.

### Servizio Update Orchestrator

Controlla lo stato esatto di Servizio Update Orchestrator, l'attività integrata esatta di Windows Update, l'attività attuale delimitata e la cronologia esatta degli errori del servizio. I risultati dell'attività, l'attività di aggiornamento e gli errori storici non possono autorizzare l'avvio del servizio né un'operazione di aggiornamento. Il ripristino facoltativo avvia solo un servizio automatico arrestato, dopo controlli aggiornati delle dipendenze e della registrazione dell'eseguibile. Un servizio con avvio a richiesta arrestato resta solo informativo. L'attività di aggiornamento pianificata esistente può riprendere secondo i criteri attuali e può scaricare o installare aggiornamenti già approvati. Criteri di aggiornamento, registrazione dell'origine, pianificazioni, impostazioni di riavvio, pacchetti, processi BITS, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona né approva alcun aggiornamento. La verifica copre solo lo stato stabile del servizio, non l'applicabilità, il download, l'installazione, il completamento né la disponibilità al riavvio.

### Pulizia dell'archivio componenti

Analizza se Windows consiglia la pulizia dell'archivio componenti. La manutenzione facoltativa rimuove subito i componenti sostituiti, poi analizza di nuovo. Nessun ResetBase né eliminazione manuale in WinSxS. Nessun annullamento automatico; lo spazio su disco recuperato non viene misurato.
