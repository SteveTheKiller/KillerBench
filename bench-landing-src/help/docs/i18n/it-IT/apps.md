# App

App esamina i programmi installati, i pacchetti di Store, Office e Outlook, le estensioni del browser, le app di avvio e la ricerca. Gli strumenti di rimozione e reimpostazione mostrano sempre prima un esame e modificano solo ciò che selezioni.

Guide passo passo: [Configurare un nuovo computer](set-up-new-pc.html).

## Strumenti di questa sezione

Esamina applicazioni installate, pacchetti di Store, Office e Outlook, estensioni del browser e ricerca.

### Applicazioni installate

Legge le registrazioni di disinstallazione delle applicazioni nell'ambito selezionato del computer o dell'account in esecuzione, poi combina quell'ambito con evidenze delimitate di errori del registro Applicazione, di Monitoraggio affidabilità e di riavvii. Altri account e app portatili sono esclusi. Le registrazioni e la cronologia generale non sono una prova esatta dello stato o della proprietà di un'applicazione. Non viene selezionata alcuna azione su programmi di installazione, disinstallazione, applicazioni, processi, servizi, file, registrazioni, account, criteri o riavvii.

### Pacchetti di app

Fa l'inventario dei pacchetti Appx/MSIX registrati per l'ambito account selezionato. L'account in esecuzione è l'impostazione predefinita; tutti gli utenti richiede l'accesso di amministratore. Riporta identità esatte, indicatori di framework e di non rimovibilità e numero di dipendenze. Pacchetti di cui è stato eseguito il provisioning, applicazioni Win32, proprietà gestita e dati delle applicazioni non vengono valutati. Nessun pacchetto viene rimosso.

### Estensioni del browser

Fa un inventario delimitato delle identità delle estensioni di Chrome, Edge, Brave e Firefox dell'account in esecuzione. Cronologia del browser, credenziali, impostazioni delle estensioni, contenuto dei manifest, comandi e valori dei criteri sono esclusi. La presenza non dimostra abilitazione, attendibilità, proprietà né che la rimozione sia sicura. Nessun browser, estensione o criterio viene modificato.

### Applicazioni di avvio

Fa l'inventario dei nomi delle registrazioni Run e RunOnce del computer e dell'account in esecuzione, dei nomi dei file della cartella Esecuzione automatica, degli stati di approvazione dell'avvio di Windows e dei nomi e stati delle attività pianificate all'avvio o all'accesso, poi restituisce un'unica conclusione sull'ambiente di avvio. Righe di comando, percorsi di destinazione, timestamp binari di approvazione, azioni delle attività, argomenti, account e credenziali non vengono raccolti. Una registrazione non dimostra che un elemento sia stato eseguito, abbia causato un ritardo, sia indesiderato o sia sicuro da disabilitare. Lo stato di approvazione non dimostra che esista una registrazione corrispondente. Non viene selezionata alcuna modifica a registrazioni, file, applicazioni, attività, servizi o criteri.

### Rimozione app Microsoft

Esamina le registrazioni selezionate di OneDrive, Outlook, Office/M365, Project e Teams con scelte di ambito account e di conservazione. Gli inventari del computer, dell'account in esecuzione e dei pacchetti registrati vengono confrontati; l'esame per tutti gli utenti include anche i pacchetti predisposti per i nuovi utenti. Un pacchetto supportato dell'account corrente può ricevere un piano di protezione esatto e separato nell'app desktop o nella CLI. La rimozione di quel singolo pacchetto è un'azione desktop protetta e separata, con conferme proprie. L'esame in sé non modifica alcun processo, applicazione, pacchetto, file, credenziale, percorso di cartella, valore del registro o dato utente.

### Reimpostazione di Outlook

Esamina il nuovo Outlook, Outlook classico o entrambi, con scelte esplicite per cache, impostazioni del profilo, autenticazione, file OST, componenti aggiuntivi e reinstallazione del nuovo Outlook. L'esame dell'account in esecuzione controlla posizioni note delimitate, registrazioni esatte e che i processi siano chiusi. Un'azione separata e confermata, limitata alla cache, conserva le directory di cache esatte con nomi di ripristino univoci e verifica ogni spostamento. L'esame per tutti gli utenti e le altre aree di reimpostazione restano solo in esame. Non viene rimossa alcuna cache conservata, profilo, file OST, credenziale, componente aggiuntivo, pacchetto o impostazione.

### Inventario Foxit

Fa l'inventario dei prodotti Foxit installati sul computer e dei valori del registro del relativo programma di aggiornamento in entrambe le viste. Installazioni utente, criterio effettivo e proprietà dei prodotti non vengono verificati. Nessuna applicazione o impostazione viene modificata.

### Servizio Windows Installer

Controlla i criteri di restrizione, rollback, elevazione, registrazione e debug di Installer, i marcatori di operazioni in corso, i binari del motore e la visibilità dei pacchetti in cache registrati prima del ripristino del servizio. Mette in relazione gli esiti recenti non riusciti e riusciti solo per le identità di prodotto esatte riportate. Se i prerequisiti sono soddisfatti, la riparazione automatica può avviare il servizio abilitato e verificare che sia In esecuzione. Esiti storici, registrazione, visibilità della cache e rilevamenti di debug non autorizzano modifiche a pacchetti o criteri. Criteri, log, marcatori di transazione, tipi di avvio e pacchetti in cache vengono conservati. L'installazione di un pacchetto specifico non viene testata.

### Windows Search

Controlla la disponibilità del servizio Windows Search, l'attività attuale di SystemIndex e la cronologia recente degli eventi di Search. La riparazione automatica può avviare un servizio abilitato e arrestato e verificare che sia in esecuzione. Una scelta esplicita e separata di ricostruzione dell'indice conserva l'indice precedente, avvia un nuovo indice e verifica lo stato esatto del servizio e della directory. Percorsi indicizzati, criteri, tipo di avvio e dati conservati vengono mantenuti. Avviare il servizio o ricostruire l'indice può far riprendere l'indicizzazione in background. La completezza dei risultati di ricerca resta non testata.

### Preparazione app

Controlla la disponibilità del servizio Preparazione app. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Il primo accesso e il provisioning delle applicazioni esistenti possono riprendere secondo i criteri attuali. Pacchetti, registrazioni, licenze, profili utente, dati delle applicazioni, criteri e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione sui pacchetti. La verifica copre solo lo stato stabile del servizio, non l'accesso, il provisioning né l'avvio delle applicazioni.

### Servizio repository di stato

Controlla la disponibilità di Servizio repository di stato. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le richieste esistenti sullo stato del modello applicativo possono riprendere. Registrazioni, pacchetti, licenze, profili, dati del browser e delle applicazioni, contenuto del repository, criteri e tipo di avvio vengono conservati. Non viene richiesta alcuna reimpostazione, nuova registrazione o eliminazione. La verifica copre solo lo stato stabile del servizio, non la coerenza del repository né il funzionamento delle applicazioni.

### Servizio di distribuzione AppX

Controlla la disponibilità di Servizio di distribuzione AppX. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le richieste esistenti di distribuzione, registrazione o manutenzione possono riprendere secondo i criteri attuali. Pacchetti, registrazioni, licenze, profili, dati delle applicazioni, contenuto del repository, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione sui pacchetti. La verifica copre solo lo stato stabile del servizio, non la distribuzione, la registrazione né l'avvio delle applicazioni.

### Servizio licenze client

Controlla la disponibilità di Servizio licenze client. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le richieste esistenti di licenze per le applicazioni di Store possono riprendere secondo i criteri attuali. Pacchetti, registrazioni, licenze, token, account, profili, dati delle applicazioni, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione su pacchetti, licenze, token o account. La verifica copre solo lo stato stabile del servizio, non la convalida delle licenze né l'avvio delle applicazioni.

### Servizio di installazione di Microsoft Store

Controlla la cronologia recente delle installazioni di Microsoft Store e la disponibilità esatta del servizio di installazione di Store. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le richieste esistenti di download e installazione da Store possono riprendere secondo i criteri attuali. Pacchetti, registrazioni, licenze, account, dati delle applicazioni, stato dei download, origini, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione su pacchetti, download, licenze, account, origini o installazioni. La verifica copre solo lo stato stabile del servizio, non gli esiti di download, installazioni, licenze o applicazioni.

### Servizio Gestione licenze Windows

Controlla la disponibilità di Servizio Gestione licenze Windows. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. I controlli esistenti delle licenze delle applicazioni possono riprendere secondo i criteri attuali. Pacchetti, registrazioni, licenze, token, account, dati delle applicazioni, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione su pacchetti, licenze, token, account, registrazioni o diritti. La verifica copre solo lo stato stabile del servizio, non la convalida delle licenze, i diritti né l'avvio delle applicazioni.

### Servizio PushToInstall di Windows

Controlla la disponibilità di Servizio PushToInstall di Windows. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Una richiesta di installazione remota già approvata può riprendere secondo i criteri attuali. Pacchetti, registrazioni, licenze, account, dati delle applicazioni, richieste in coda, origini, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non crea, approva né seleziona alcuna richiesta, pacchetto, origine, account o installazione. La verifica copre solo lo stato stabile del servizio, non l'elaborazione delle richieste, il download, l'installazione né gli esiti delle applicazioni.
