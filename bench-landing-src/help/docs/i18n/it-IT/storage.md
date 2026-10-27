# Archiviazione

Ispeziona dischi, volumi, Spazi di archiviazione e i servizi di manutenzione delle unità. La diagnosi non seleziona alcuna operazione sul disco; ogni azione supportata che modifica il computer richiede una destinazione esatta, prerequisiti e una conferma propri.

Guide passo passo: [Controllare e riparare un'unità](check-a-drive.html) e [Liberare spazio su disco](free-disk-space.html).

## Strumenti di questa sezione

### Archiviazione

Mappa in modo delimitato le identità di dischi, partizioni e volumi con lettera di unità, valuta la pressione del 10% di spazio libero su ogni volume con lettera di unità, ispeziona l'allocazione dell'archiviazione delle copie shadow VSS e le quote predefinite di ogni volume, riepiloga l'avanzamento attuale dei processi del modulo Storage, controlla il volume NTFS selezionato e il suo stato di autoriparazione online, e ispeziona i dispositivi disco e controller di questo PC. Soglie di capacità, impostazioni delle quote e stati dei processi sono evidenze da esaminare e non selezionano pulizia, ridimensionamento, modifiche alle quote né controllo dei processi. La mappatura delle lettere di unità è puntuale e non copre ogni percorso di montaggio. I rilevamenti sui dispositivi non vengono associati al volume selezionato. Il volume di Windows è quello predefinito. Non vengono modificati file, snapshot, processi o driver, non viene cambiata alcuna impostazione, non viene smontato alcun volume e non viene pianificato alcun riavvio.

### Spazi di archiviazione

Fa l'inventario dei pool di Spazi di archiviazione non primordiali e dei dischi virtuali, inclusi integrità, stato operativo, capacità, allocazione e resilienza, poi restituisce un'unica conclusione operativa delimitata. Windows può avviare su richiesta il proprio provider di gestione di Spazi di archiviazione per rispondere a questa query in sola lettura. Pool, dischi virtuali, dischi fisici, volumi, resilienza, allocazione, impostazioni di manutenzione, criteri e tipo di avvio vengono conservati. Non viene richiesta alcuna operazione su pool, dischi, volumi, allocazione, collegamento, riparazione, ottimizzazione o impostazioni. I risultati del provider non verificano l'accessibilità del pool, la ridondanza, l'integrità dei dati, la recuperabilità dei backup né l'I/O delle applicazioni.

### Servizio di archiviazione

Esamina in modo delimitato i processi attuali del modulo Storage, la cronologia esatta degli errori del servizio e la disponibilità attuale di Servizio di archiviazione. Le evidenze incomplete hanno la precedenza. Lo stato dei processi e gli errori storici non possono autorizzare il ripristino del servizio né modifiche all'archiviazione. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le notifiche di archiviazione esistenti e l'attività di archiviazione gestita dai criteri possono riprendere. Volumi, partizioni, file system, punti di montaggio, dispositivi, driver, impostazioni di Sensore memoria, criteri di manutenzione, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione su processi, volumi, partizioni, punti di montaggio, file, dispositivi, driver o impostazioni di archiviazione. La verifica copre solo lo stato stabile del servizio, non l'esito dei processi né le operazioni su dispositivi, volumi o criteri di archiviazione.

### Servizio Ottimizza unità

Controlla la disponibilità del servizio Ottimizza unità. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. L'attività di ottimizzazione già pianificata da Windows può riprendere secondo i criteri di manutenzione attuali. Volumi, file system, dati, pianificazioni di ottimizzazione, criteri di manutenzione, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcun volume e non analizza, esegue il trim, deframmenta, ottimizza o pianifica alcuna unità. La verifica copre solo lo stato stabile del servizio, non l'analisi, l'ottimizzazione né l'integrità del dispositivo.
