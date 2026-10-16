# Manutenzione

Manutenzione tiene in ordine il computer: pulizia dei file, vecchi profili utente, attività pianificate e alleggerimento di Windows. Ogni strumento mostra in anteprima cosa rimuoverebbe prima di eliminare qualsiasi cosa.

Guide passo passo: [Liberare spazio su disco](free-disk-space.html) e [Rendere Windows più riservato](make-windows-private.html).

## Strumenti di questa sezione

Pulisci file, profili e attività pianificate e alleggerisci Windows.

### Pulizia

Stima le destinazioni selezionate di file temporanei, cache ed evidenze diagnostiche conservate usando le impostazioni di età e di limite dei file del riquadro principale. Windows Temp e Windows SystemTemp sono destinazioni indipendenti. Delivery Optimization è una scelta di cache separata e i contenuti rimossi potrebbero dover essere scaricati di nuovo. La rimozione permanente facoltativa usa la casella di pulizia separata, ricontrolla i candidati conservati entro il limite selezionato e registra ogni esito. L'eliminazione non passa dal Cestino. Destinazioni, età e limiti dei file si possono configurare anche con il comando cleanup della CLI.

### Profili

Esamina i profili di Windows usando la soglia di età selezionata, le registrazioni esatte di ProfileList e, facoltativamente, le dipendenze degli account di servizi e attività pianificate, poi restituisce un'unica conclusione d'esame delimitata. I profili caricati, quelli di sistema e quello dell'account corrente sono esclusi dall'esame. I candidati restano solo in esame. Non viene modificato alcun profilo, cartella, chiave del registro, account, servizio, attività, credenziale, criterio o file, e non viene selezionata alcuna rimozione. Le dipendenze delle applicazioni e la recuperabilità dei backup restano non verificate.

### Attività pianificate

Fa l'inventario delle attività pianificate visibili, dei loro stati e delle entità di sicurezza indicate. Nessuna attività viene avviata, registrata o modificata. Azioni, trigger e riuscita dell'esecuzione non vengono verificati.

### Alleggerisci Windows

Esamina aree di pulizia esplicite di Windows come Recall e IA, telemetria, suggerimenti di ricerca, contenuti consumer, barra delle applicazioni e Start, notifiche, Edge, alimentazione, ibernazione, lingue di Office e software preinstallato, per l'account in esecuzione o per tutti i profili. L'esame è in sola lettura. La combinazione di risparmio energia Prestazioni elevate, i timer di sospensione e dello schermo e l'ibernazione possono essere modificati singolarmente quando quell'area è l'unica selezionata, con evidenze aggiornate e una conferma esplicita.
