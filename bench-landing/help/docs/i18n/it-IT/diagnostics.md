# Diagnostica

Diagnostica è il punto da cui dovrebbe iniziare ogni intervento. Ogni strumento legge il computer e scrive un rapporto. Niente qui modifica Windows. Lo strumento predefinito, l'analisi completa del sistema, esegue un'ampia serie di controlli in sola lettura e ordina ciò che richiede attenzione.

Guide passo passo: [Indagare su arresti anomali e schermate blu](investigate-crashes.html) e [Riparare i file di sistema](repair-system-files.html).

## Strumenti di questa sezione

Controlla prima il computer, poi leggi nel rapporto cosa richiede attenzione e qual è il prossimo passo sicuro.

### Analisi completa del sistema

Ispeziona 49 aree diagnostiche, poi valuta ogni ricetta di ripristino dei servizi installata insieme alle ricette di manutenzione, DHCP, stampa, Defender, ora e WMI. I controlli possono richiedere diversi minuti. La riparazione automatica è facoltativa e richiede le evidenze attuali, i prerequisiti del computer e i controlli delle dipendenze propri di ogni ricetta. Le riparazioni idonee possono rinnovare lease DHCP mancanti, rilasciare processi di stampa in coda, risincronizzare l'orologio, aggiornare le firme di Defender, ripristinare servizi idonei e riparare danni confermati di Windows. I servizi automatici arrestati possono essere avviati dopo aver verificato la loro finestra di avvio e le dipendenze. Un servizio con avvio a richiesta arrestato resta invariato quando lo stato inattivo è l'unica evidenza; i flussi di lavoro più mirati possono richiederlo solo quando evidenze attuali della funzionalità giustificano l'avvio. Tipi di avvio, definizioni delle attività, rapporti, dump, sottoscrizioni, credenziali, impostazioni dei canali, criteri del firewall e impostazioni gestite vengono conservati. Le istantanee storiche, di backup e di trasferimento sono in sola lettura. Il contenuto dei dump non viene aperto. L'ispezione del bit dirty non pianifica riparazioni né riavvii. Questo flusso di lavoro non installa aggiornamenti, non modifica driver, non elimina file e non esegue un test completo dell'hardware.

### Parametri vitali

Rileva la memoria disponibile, lo stato dei dischi indicato dal provider e lo stato della protezione BitLocker, poi mette in relazione i parametri vitali attuali con i problemi dei dispositivi, la cronologia di affidabilità, i record degli errori hardware, le evidenze di arresti anomali, gli spegnimenti imprevisti e i risultati di Diagnostica memoria Windows. Indicatori di riavvio, spazio libero del disco di sistema, elevazione e configurazione del servizio di aggiornamento restano visibili. Le password di ripristino non vengono raccolte. Questo flusso di lavoro non esegue riparazioni. Il risultato combinato non è un test completo di hardware, software o stabilità.

### Cronologia affidabilità

Legge i record di Monitoraggio affidabilità di Windows degli ultimi 14 giorni insieme agli errori recenti del registro Applicazione e agli eventi di spegnimento imprevisto, poi restituisce un'unica conclusione storica delimitata. I record storici non stabiliscono lo stato attuale né la causa principale. Non viene selezionata alcuna riparazione e non viene modificata alcuna applicazione, servizio o impostazione.

### Prestazioni di avvio

Legge i metadati degli eventi di prestazioni di avvio e arresto di Windows degli ultimi 14 giorni, conservando solo l'identità del provider e campi numerici di durata delimitati, poi li mette in relazione con gli indicatori di riavvio in sospeso supportati. Percorsi, nomi dei processi, account e il resto del payload degli eventi vengono rimossi prima di salvare le evidenze. I record storici di degrado non identificano un collo di bottiglia attuale né autorizzano modifiche ad avvio, servizi, applicazioni, driver, attività, alimentazione, riavvio o criteri.

### Cronologia errori hardware

Legge la cronologia di Windows Hardware Error Architecture e della diagnostica della memoria degli ultimi 14 giorni, i problemi attuali dei dispositivi Plug and Play, lo stato dei dischi fisici e i contatori di affidabilità del provider, poi restituisce un'unica conclusione hardware delimitata. I record vanno messi in relazione con il loro payload, la diagnostica del firmware e i test hardware del produttore. Non viene dedotto alcun componente guasto né alcuna causa principale, non viene selezionata alcuna riparazione e non viene modificato alcun hardware, firmware, driver, disco, pool, volume, file, servizio, alimentazione, riavvio o impostazione.

### Cronologia installazione driver

Legge la parte finale conservata della cronologia di installazione dei dispositivi di Windows SetupAPI, mette in relazione le sezioni non riuscite con le identità Plug and Play esatte attuali e restituisce un'unica conclusione delimitata. Le sezioni storiche del log non dimostrano che un dispositivo sia ancora presente né che un errore sia attuale. Nessun driver viene installato, rimosso, ripristinato o aggiornato.

### Punti di Ripristino configurazione di sistema

Elenca fino a 256 punti di Ripristino configurazione di sistema disponibili, esamina l'idoneità esplicita alla creazione e restituisce un'unica conclusione di disponibilità delimitata. L'inventario non dimostra che la protezione sia attiva per ogni volume né che un ripristino riuscirà. Durante la diagnosi non viene creato, eliminato o applicato alcun punto di ripristino.

### Ambiente ripristino Windows

Legge la configurazione attuale dell'Ambiente ripristino Windows con REAgentC, mette in relazione un percorso locale abilitato con il disco e la partizione esatti attuali e restituisce un'unica conclusione di disponibilità delimitata. La riparazione automatica facoltativa può abilitare una configurazione esistente disabilitata, poi verificarne lo stato indicato. L'immagine di ripristino non viene scaricata, spostata o sostituita, non viene avviato alcun avvio di ripristino e Windows non viene riavviato. Lo stato abilitato e la presenza della partizione non dimostrano che l'immagine di ripristino possa avviare o riparare questo PC.

### Attivazione di Windows

Legge lo stato attuale della licenza di Windows senza raccogliere codici Product Key completi o parziali, conserva i metadati anonimizzati degli eventi di avviso, errore e critici di Protezione software degli ultimi sette giorni, poi restituisce un'unica conclusione operativa delimitata. Il payload degli eventi viene rimosso prima di salvare le evidenze. I record storici restano come contesto quando la licenza attuale risulta Con licenza. Non viene selezionata alcuna azione su attivazione, Product Key, token, licenze, account, servizi o criteri. Lo stato del provider e la cronologia degli eventi non dimostrano il diritto all'abbonamento, la raggiungibilità del server di attivazione, l'integrità dell'archivio dei token, la validità futura né la causa principale.

### Servizi di Windows

Fa l'inventario di nomi dei servizi installati, nomi visualizzati, stati, modalità di avvio e tipi di azioni di ripristino limitati per la privacy, senza raccogliere account, percorsi degli eseguibili, comandi di ripristino o messaggi di riavvio. Esamina la coerenza delle modalità di avvio, le identità e i cicli delle dipendenze dirette, ripete gli stati in sospeso esatti, ispeziona i servizi supportati e le dipendenze dei servizi arrestati, poi mette in relazione gli errori recenti di Gestione controllo servizi solo quando è presente un nome di servizio esatto o un nome visualizzato univoco. L'uso dei gruppi dell'ordine di caricamento viene contato senza espanderne i membri, e le identità ambigue restano irrisolte. I servizi con avvio a richiesta arrestati possono essere normali. Nessun servizio viene avviato e le impostazioni di avvio, dipendenze e ripristino vengono conservate. Usa il flusso di lavoro Riparazione corrispondente quando una funzionalità specifica richiede un ripristino.

### Errori delle applicazioni

Legge gli eventi recenti di errore e critici del registro Applicazione insieme a una cronologia delimitata di Monitoraggio affidabilità, poi restituisce un'unica conclusione sulla cronologia di stabilità. I record storici non dimostrano un guasto attuale né una causa principale e indirizzano solo verso una diagnosi più mirata. Nessuna applicazione viene arrestata, riparata, rimossa o riconfigurata.

### Dispositivi di visualizzazione

Ispeziona schede grafiche e monitor alla ricerca di errori dei dispositivi segnalati, ID hardware e versioni dei driver installati. Nessuna installazione di driver, reimpostazione dei dispositivi o modifica della risoluzione. La qualità dell'immagine e le connessioni fisiche non vengono testate.

### Dispositivi di input

Ispeziona gli errori di tastiera, mouse e dispositivi HID e l'inventario dei driver. Non vengono acquisite sequenze di tasti, reimpostati dispositivi o modificati driver. Il funzionamento fisico dell'input non viene testato.

### Problemi dei dispositivi

Trova codici di errore di configurazione Plug and Play diversi da zero in tutte le classi di dispositivi, poi mette in relazione le evidenze dei dispositivi presenti con la cronologia delimitata di SetupAPI su installazione dei driver, configurazione dei dispositivi e Plug and Play. I record storici da soli non stabiliscono un guasto attuale. Esamina l'identità esatta del dispositivo e le evidenze dei driver approvati senza modificare hardware, driver o configurazione. I dispositivi mancanti e il funzionamento fisico non vengono verificati.
