# Riparazione

Riparazione raccoglie gli strumenti in grado di correggere qualcosa. Ognuno esegue prima una diagnosi. Una riparazione viene offerta solo quando evidenze aggiornate mostrano il problema esatto che risolve, e viene eseguita solo dopo che hai attivato **Ripara automaticamente problemi confermati** o hai confermato tu una singola riparazione. Ogni riparazione viene registrata nel giornale e verificata in seguito.

Guide passo passo: [Riparare i file di sistema](repair-system-files.html) e [Riparare Windows Update](fix-windows-update.html).

## Riparazione automatica e manuale

**Ripara automaticamente problemi confermati** permette a uno strumento di diagnosi di eseguire, dopo l'analisi, le riparazioni di cui dimostra la necessità. È disattivato per impostazione predefinita.

Per una singola riparazione di manutenzione, apri **Avanzato: log e riparazioni manuali** e scegli **Riparazioni**. Scegli **Archivio componenti (DISM)** o **File di sistema (SFC)**, leggi l'anteprima, seleziona **Esegui questa riparazione su questo PC** e premi **Esegui riparazione**. Vedi [Come le modifiche restano sicure](safety.html) per cosa accade prima e dopo ogni modifica.

## Strumenti di questa sezione

Usali dopo che una diagnosi ha individuato un problema. Le riparazioni richiedono evidenze confermate e un'approvazione esplicita.

### Servizi principali di Windows

Controlla insieme i servizi Alimentazione, Plug and Play, Gestione utenti, Client di Criteri di gruppo, Servizio interfaccia archivio di rete, Mapping endpoint RPC, Chiamata di procedura remota, Utilità di avvio processi server DCOM, Servizio infrastruttura attività in background e CoreMessaging. Il ripristino facoltativo avvia solo servizi automatici abilitati esatti che evidenze aggiornate confermano arrestati dopo la finestra di avvio e controlli completi delle dipendenze. I servizi con avvio a richiesta restano invariati quando sono inattivi. Combinazioni di risparmio energia, dispositivi, driver, account, credenziali, profili, criteri, schede, indirizzi, route, DNS, configurazione del firewall, registrazioni, endpoint, registrazioni delle attività, dati delle applicazioni, autorizzazioni dei servizi e tipi di avvio vengono conservati. Non viene richiesto alcun aggiornamento dei criteri, ricerca di dispositivi, azione sugli account, modifica di rete, modifica dell'alimentazione, modifica delle registrazioni o avvio di attività. La verifica copre solo lo stato stabile dei servizi, non il comportamento completo delle funzionalità.

### File di sistema

Fa un inventario delimitato degli stati dei pacchetti di manutenzione, delle funzionalità facoltative e delle capacità di Windows, controlla i prerequisiti, analizza i file di sistema protetti e sceglie automaticamente SFC solo quando il controllo in tempo reale conferma un danno. Gli stati in sospeso di pacchetti, funzionalità o capacità richiedono un esame ma non autorizzano installazione, rimozione, pulizia, ripristino, abilitazione o disabilitazione.

### Repository WMI

Controlla la disponibilità del servizio WMI, poi la coerenza del repository. La riparazione automatica può avviare un servizio arrestato idoneo e recuperare un danno confermato, poi verificare ogni risultato. Le impostazioni di servizio disabilitato vengono conservate. Le registrazioni leggibili vengono unite; il contenuto illeggibile potrebbe richiedere la reinstallazione del provider. I client di gestione potrebbero essere interrotti. Nessuna reimpostazione, eliminazione o ripristino automatico.

### Notifiche di sistema di Windows

Legge la cronologia recente di Gestione controllo servizi, poi controlla insieme Sistema eventi COM+ e Servizio notifica eventi di sistema. Il ripristino facoltativo verifica che Sistema eventi COM+ sia pronto prima di avviare SENS. Le notifiche esistenti di accesso, rete, alimentazione e COM+ possono riprendere con la configurazione attuale. Cataloghi COM+, sottoscrizioni, registrazioni delle notifiche, consumer, account, autorizzazioni, impostazioni di ripristino, criteri e tipi di avvio vengono conservati. Nessuna sottoscrizione, registrazione, consumer, account, autorizzazione o criterio viene modificato. La verifica copre solo lo stato stabile dei servizi, non la consegna completa delle notifiche.

### Servizio criteri di diagnostica

Esamina la cronologia operativa delimitata dei Criteri di diagnostica, la cronologia esatta degli errori del servizio e la disponibilità attuale del servizio. Le evidenze incomplete hanno la precedenza. I record storici non possono autorizzare il ripristino del servizio né un'azione di risoluzione dei problemi. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Gli scenari diagnostici esistenti possono riprendere. Criteri, dati di risoluzione dei problemi e tipo di avvio vengono conservati. Non viene eseguito alcuno strumento di risoluzione dei problemi né alcuna correzione suggerita. La verifica copre solo lo stato stabile del servizio, non il rilevamento dei guasti, il completamento della diagnosi né l'esito di una riparazione.

### Segnalazione errori Windows

Esamina la configurazione dei dump di arresto anomalo, i metadati delimitati dei file di dump, i rapporti recenti di arresto anomalo del sistema, la cronologia esatta degli errori del servizio Segnalazione errori Windows e la disponibilità attuale del servizio. I rapporti storici non stabiliscono un guasto attuale né una causa principale e non possono autorizzare un ripristino. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. I rapporti in coda esistenti possono riprendere secondo i criteri attuali. Archivi dei rapporti, dump, consenso, criteri di caricamento e tipo di avvio vengono conservati. Non viene richiesto alcun caricamento né alcuna eliminazione. La verifica copre solo lo stato stabile del servizio, non la creazione, il caricamento o la consegna dei rapporti, la risoluzione dei problemi né la diagnosi degli arresti anomali.

### Raccolta eventi di Windows

Esamina l'inventario delimitato delle sottoscrizioni di Raccolta eventi di Windows, la cronologia esatta degli errori del servizio e la disponibilità attuale del servizio. Un inventario vuoto può essere intenzionale, e il numero di sottoscrizioni o gli errori storici non possono autorizzare un ripristino. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le sottoscrizioni esistenti possono riprendere la raccolta remota degli eventi. Sottoscrizioni, eventi inoltrati, credenziali, impostazioni dei canali, criteri e tipo di avvio vengono conservati. La verifica copre solo lo stato stabile del servizio, non la raggiungibilità delle origini, l'autenticazione, la consegna remota, l'aggiornamento degli eventi né lo stato delle sottoscrizioni.

### Registro eventi di Windows

Ispeziona quattro canali diagnostici per stato di abilitazione, modalità di conservazione, dimensione attuale, limite configurato, numero di record e saturazione, poi controlla la disponibilità del servizio Registro eventi di Windows. La riparazione automatica può avviare un servizio abilitato e arrestato dopo aver controllato le dipendenze e verificarne lo stato in esecuzione. Un'azione mirata confermata separatamente può abilitare un singolo canale diagnostico esatto disabilitato e non gestito, conservando tutte le altre impostazioni riportate. Log, autorizzazioni dei canali, conservazione, limiti di dimensione, tipi di avvio e impostazioni gestite vengono conservati. La consegna corretta degli eventi e la capacità di archiviazione restano non verificate.

### Servizio Ora di Windows

Controlla configurazione, servizio e sincronizzazione di Ora di Windows. La riparazione automatica può avviare W32Time quando lo stato attuale riporta l'errore esatto di servizio arrestato e i controlli aggiornati di criteri, modalità e dipendenze vengono superati, poi richiedere una risincronizzazione e verificare il nuovo stato. Questo può regolare l'orologio usando le origini già configurate. Tipo di avvio, configurazione delle origini e impostazioni gestite vengono conservati.

### Servizio Utilità di pianificazione

Fa un inventario delimitato dello stato delle attività pianificate, dei risultati di esecuzione e del numero di esecuzioni mancate, poi li combina con la cronologia esatta degli errori del servizio Utilità di pianificazione prima di controllare la disponibilità attuale del servizio. I rilevamenti sulle attività e gli errori storici del servizio non possono autorizzare l'avvio del servizio. Il ripristino facoltativo avvia solo un servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le attività abilitate esistenti possono partire con gli account configurati, compresi i trigger mancati. Nessuna definizione di attività, entità di sicurezza, riferimento a credenziali, criterio o tipo di avvio viene modificato, e nessuna attività viene avviata o eliminata direttamente. La verifica copre solo lo stato stabile del servizio, non la riuscita delle azioni, la validità delle credenziali né gli esiti futuri delle attività.

### Servizio profilo utente

Controlla la disponibilità di Servizio profilo utente. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le richieste esistenti di accesso e di profilo possono riprendere. Cartelle dei profili, hive del registro, autorizzazioni, registrazioni dell'elenco dei profili, credenziali, dati utente, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. Non viene richiesta alcuna operazione sui profili. La verifica copre solo lo stato stabile del servizio, non l'accesso, il caricamento dei profili né l'accesso ai dati utente.

### Servizi di backup

Controlla i servizi Copia shadow del volume, Provider di copie shadow software Microsoft, Backup di Windows e Servizio motore backup a livello di blocco, poi legge lo stato dei writer VSS, i tipi di provider registrati, le snapshot attuali e l'allocazione dell'archiviazione delle copie shadow. I servizi con avvio a richiesta arrestati possono essere normali quando sono inattivi. Il ripristino facoltativo avvia solo servizi automatici arrestati dopo i controlli delle dipendenze, poi ne verifica lo stato in esecuzione. Il lavoro di backup pianificato o interrotto esistente può riprendere. Impostazioni di avvio disabilitato e a richiesta, snapshot, archiviazione delle copie shadow, registrazioni dei provider, piani di backup, cataloghi, pianificazioni, destinazioni, credenziali e conservazione vengono mantenuti. Nessun backup viene selezionato o riconfigurato, nessun servizio viene riavviato e nessuna snapshot o catalogo viene eliminato. Registrazione dei provider, stato dei writer, allocazione dell'archiviazione e stato stabile dei servizi non verificano il completamento del backup, l'accesso alla destinazione né la riuscita del ripristino.
