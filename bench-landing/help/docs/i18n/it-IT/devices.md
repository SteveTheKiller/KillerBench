# Dispositivi

Dispositivi copre stampanti, audio, fotocamere, Bluetooth, input e i servizi di Windows da cui dipende l'hardware collegato.

Guide passo passo: [Risolvere i problemi di stampa](fix-printing.html) e [Indagare su arresti anomali e schermate blu](investigate-crashes.html).

## Strumenti di questa sezione

Stampa, audio, fotocamere, Bluetooth, input e i servizi alla base dell'hardware collegato.

### Stampa

Controlla Spooler di stampa, le pause delle code locali, i processi di stampa visibili, le impostazioni offline e le condizioni riportate dai driver, poi fa l'inventario delle registrazioni di code, driver, porte, endpoint TCP/IP standard, processori di stampa e monitor di stampa. Mette in relazione i problemi recenti di PrintService solo quando è presente un'identità esatta della coda attuale. Gli endpoint TCP/IP standard assegnati ricevono un unico tentativo di connessione delimitato, senza dati delle applicazioni, SNMP o processi di stampa. La riparazione automatica può avviare lo spooler abilitato, riprendere solo code esatte in pausa con evidenze aggiornate di processi in attesa e disattivare la modalità offline. Le pause intenzionali senza processi restano solo in esame. Connessioni remote, stringhe community SNMP, registrazioni delle estensioni e contenuto dei processi vengono conservati. Un codice di condizione o una connessione riuscita non stabiliscono l'identità della stampante, i materiali di consumo, la meccanica, l'accettazione da parte della coda, la riuscita del rendering né la stampa fisica.

### Audio

Controlla le identità e i formati condivisi dell'input e dell'output multimediale predefiniti, lo stato dei dispositivi e dei driver audio, Generatore endpoint audio di Windows e Audio di Windows. La riparazione automatica avvia i servizi abilitati e arrestati e verifica che siano in esecuzione. Impostazioni di avvio, driver, dispositivi selezionati, modalità esclusiva e livelli di volume vengono conservati. Riproduzione e registrazione del suono e instradamento delle applicazioni restano non testati.

### Supporto fotocamera

Ispeziona errori dei dispositivi fotocamera e di imaging, ID hardware, evidenze dei driver installati e disponibilità di Server fotogrammi fotocamera di Windows. Il ripristino facoltativo avvia solo un servizio abilitato e arrestato dopo i controlli delle dipendenze. Un'applicazione che sta già richiedendo l'accesso alla fotocamera può riprendere con le autorizzazioni esistenti. Impostazioni di privacy della fotocamera, autorizzazioni, configurazione dei dispositivi, criteri, driver e tipo di avvio vengono conservati. Nessuna reimpostazione, modifica delle autorizzazioni o installazione di driver. La verifica copre solo la configurazione del dispositivo riportata e lo stato in esecuzione del servizio, non l'acquisizione di immagini, l'accesso di privacy né il funzionamento fisico.

### Supporto Bluetooth

Ispeziona errori dei dispositivi Bluetooth, ID hardware, evidenze dei driver installati e disponibilità di Servizio di supporto Bluetooth. Il ripristino facoltativo avvia solo un servizio abilitato e arrestato dopo i controlli delle dipendenze; i dispositivi associati possono riconnettersi. Associazioni, impostazioni radio, configurazione dei dispositivi, criteri e tipo di avvio vengono conservati. Nessuna reimpostazione, rimozione di dispositivi o installazione di driver. La verifica copre solo la configurazione del dispositivo riportata e lo stato in esecuzione del servizio, non l'individuazione, l'associazione, l'audio né il trasferimento di dati.

### Supporto smart card

Ispeziona errori dei lettori di smart card, ID hardware, evidenze dei driver installati e disponibilità del servizio Smart card. Il ripristino facoltativo avvia solo un servizio abilitato e arrestato dopo i controlli delle dipendenze. Le applicazioni possono tornare a rilevare le schede inserite secondo i criteri esistenti. Certificati, PIN, credenziali, configurazione di lettori e schede, criteri, driver e tipo di avvio vengono conservati. Nessuna inizializzazione, registrazione, operazione sul PIN, reimpostazione del lettore o installazione di driver. La verifica copre solo la configurazione del lettore riportata e lo stato in esecuzione del servizio, non il rilevamento delle schede, l'autenticazione né l'accesso delle applicazioni.

### Supporto biometrico

Ispeziona errori dei dispositivi biometrici, ID hardware, evidenze dei driver installati e disponibilità di Servizio di biometria Windows. Il ripristino facoltativo avvia solo un servizio abilitato e arrestato dopo i controlli delle dipendenze. Le richieste esistenti di Windows Hello e delle applicazioni possono riprendere secondo i criteri attuali. Modelli biometrici, dati di Windows Hello, credenziali, configurazione dei dispositivi, autorizzazioni, criteri, driver e tipo di avvio vengono conservati. Nessuna registrazione, eliminazione di modelli, reimpostazione del sensore, modifica delle autorizzazioni o installazione di driver. La verifica copre solo la configurazione del sensore riportata e lo stato in esecuzione del servizio, non l'acquisizione, il confronto, l'accesso né l'accesso delle applicazioni.

### Servizio dispositivi interfaccia umana

Controlla la disponibilità di Servizio dispositivi interfaccia umana. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. I tasti di scelta rapida, i controlli multimediali e le altre funzionalità HID esistenti possono riprendere. Dispositivi, driver, mappature dei pulsanti, impostazioni di accessibilità, impostazioni di input, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione su dispositivi, driver, mappature o impostazioni di input. La verifica copre solo lo stato stabile del servizio, non il comportamento di pulsanti, controlli o applicazioni.

### Servizio enumeratore dispositivi portatili

Controlla la disponibilità di Servizio enumeratore dispositivi portatili. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le applicazioni esistenti possono tornare a rilevare e ad accedere ai dispositivi portatili collegati. Dispositivi, driver, file, raccolte, impostazioni di sincronizzazione, autorizzazioni, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione su dispositivi, file, raccolte, sincronizzazione o trasferimenti. La verifica copre solo lo stato stabile del servizio, non l'enumerazione, l'accesso, la sincronizzazione né il trasferimento.

### Acquisizione immagini di Windows (WIA)

Controlla la disponibilità del servizio Acquisizione immagini di Windows. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le richieste esistenti di scanner e di acquisizione di immagini fisse possono riprendere con le autorizzazioni attuali delle applicazioni. Dispositivi, driver, dati delle immagini, autorizzazioni delle applicazioni, impostazioni di privacy, impostazioni di trasferimento, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione su dispositivi, driver, immagini, autorizzazioni, trasferimenti o acquisizioni. La verifica copre solo lo stato stabile del servizio, non il funzionamento di scanner, fotocamera, trasferimento o applicazioni.

### Rilevamento hardware shell

Ispeziona evidenze Plug and Play delimitate su controller di archiviazione, dischi e dispositivi portatili e i riferimenti ai driver installati, poi controlla la disponibilità del servizio Rilevamento hardware shell. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le notifiche esistenti di AutoPlay e di arrivo di hardware possono riprendere secondo i criteri attuali. Le osservazioni sui dispositivi e la cronologia conservata non possono autorizzare un ripristino. Dispositivi, driver, volumi, punti di montaggio, impostazioni di AutoPlay, gestori, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. Non viene richiesta alcuna operazione su dispositivi o impostazioni. La verifica copre solo lo stato stabile del servizio, non la consegna delle notifiche, la disponibilità dei supporti, l'accesso ai volumi né il funzionamento fisico dei dispositivi.

### Servizio di installazione dispositivi

Controlla la disponibilità di Servizio di installazione dispositivi. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Il lavoro di installazione dei dispositivi richiesto da Windows può riprendere secondo i criteri attuali. Dispositivi, driver, pacchetti, configurazione, binding, impostazioni delle classi, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona né installa nulla. La verifica copre solo lo stato stabile del servizio, non l'installazione né il funzionamento dei dispositivi.

### Servizio di associazione dispositivi

Controlla la cronologia recente di configurazione dei dispositivi e di Plug and Play, poi la disponibilità di Servizio di associazione dispositivi. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Il lavoro di associazione dei dispositivi richiesto da Windows può riprendere. Dispositivi abbinati e associati, driver, credenziali, metadati, autorizzazioni, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione su dispositivi, associazione, rimozione, reimpostazione o driver. La verifica copre solo lo stato stabile del servizio, non l'individuazione, l'abbinamento, l'associazione né il funzionamento dei dispositivi.

### Servizio di miglioramento dello schermo

Ispeziona le evidenze su schede grafiche e monitor, poi controlla la disponibilità di Servizio di miglioramento dello schermo. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le richieste esistenti di luminosità, colore e miglioramento possono riprendere. Configurazione dello schermo, luminosità, profili colore, impostazioni HDR, dispositivi, driver, impostazioni di alimentazione, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. Nessuna impostazione dello schermo viene modificata. La verifica copre solo lo stato stabile del servizio, non il comportamento del miglioramento, la qualità dell'immagine né il funzionamento fisico dello schermo.

### Servizio di gestione input di testo

Ispeziona le evidenze su tastiera, mouse e dispositivi HID, poi controlla la disponibilità di Servizio di gestione input di testo. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le richieste esistenti di tastiera virtuale, scrittura a mano, emoji e input di testo avanzato possono riprendere. Dizionari, dati della scrittura a mano, impostazioni di lingua e tastiera, impostazioni di accessibilità e privacy, dispositivi, driver, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. Non vengono raccolti testo digitato, sequenze di tasti o contenuti scritti a mano. La verifica copre solo lo stato stabile del servizio, non l'immissione di testo, il riconoscimento né il funzionamento dei dispositivi.

### Servizio cache dei tipi di carattere di Windows

Controlla la disponibilità di Servizio cache dei tipi di carattere di Windows. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le applicazioni esistenti possono tornare a usare la cache dei tipi di carattere attuale. Tipi di carattere installati, file dei caratteri, dati delle applicazioni, file della cache, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non installa né rimuove alcun tipo di carattere e non elimina né ricostruisce la cache. La verifica copre solo lo stato stabile del servizio, non il rendering del testo né il comportamento delle applicazioni.

### Host provider di individuazione funzioni

Controlla la disponibilità di Host provider di individuazione funzioni. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le applicazioni esistenti possono tornare a individuare i dispositivi di rete tramite i provider installati. Profili di rete, condivisioni, dispositivi, provider, regole del firewall, impostazioni di individuazione, credenziali, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione su dispositivi, condivisioni, provider, regole del firewall o impostazioni di individuazione. La verifica copre solo lo stato stabile del servizio, non lo stato dei provider, l'individuazione dei dispositivi né la connettività.

### Pubblicazione risorse di individuazione funzioni

Controlla la disponibilità di Pubblicazione risorse di individuazione funzioni. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Questo PC può diventare individuabile tramite le impostazioni esistenti di individuazione della rete e del firewall. Profili di rete, condivisioni, risorse pubblicate, regole del firewall, impostazioni di individuazione, credenziali, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione su condivisioni, risorse, regole del firewall o impostazioni di individuazione. La verifica copre solo lo stato stabile del servizio, non la pubblicazione, la visibilità, l'accesso né la connettività.
