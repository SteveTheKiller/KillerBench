# Liberare spazio su disco

Usa questa guida quando un'unità sta esaurendo lo spazio. KillerBench mostra dove va a finire lo spazio, mostra in anteprima cosa rimuoverebbe ogni destinazione di pulizia ed elimina solo dopo che hai attivato la pulizia permanente.

## Vedere quanto spazio resta

1. Apri **Archiviazione** e scegli **Archiviazione**.
2. Premi **Esegui diagnosi**. Il rapporto segnala ogni volume con lettera di unità che ha meno del 10% di spazio libero e mostra l'allocazione delle copie shadow, le quote predefinite e il criterio di Sensore memoria.
3. Per i dump di arresto anomalo e i vecchi contenuti degli aggiornamenti, esegui anche **Parametri vitali** in **Diagnostica** e **Windows Update** in **Aggiornamenti**; lì compare la dimensione della cache degli aggiornamenti.

## Vedere l'anteprima di una pulizia

1. Apri **Manutenzione** e scegli **Pulizia**.
2. Seleziona le destinazioni che vuoi, come **File temporanei utente corrente**, **File temporanei Windows**, **Cache browser utente corrente**, **Cache Delivery Optimization** o **Cestino utente corrente**. Passa il puntatore su una destinazione per leggere esattamente quali cartelle copre.
3. Imposta **Età minima (giorni)** e **File massimi per destinazione**.
4. Premi **Esegui diagnosi** con **Esegui la pulizia permanente selezionata dopo controlli aggiornati** disattivato. Il rapporto elenca i candidati e lo spazio stimato per ogni destinazione. Non viene eliminato nulla.

## Rimuovere i file

1. Attiva **Esegui la pulizia permanente selezionata dopo controlli aggiornati** e premi **Esegui diagnosi**.
2. KillerBench ricontrolla ogni candidato prima di rimuoverlo, salta i file recenti, protetti o in uso e registra ogni esito nel giornale.

La pulizia permanente non passa dal Cestino e non può essere annullata. Rimuovere i rapporti di errore e i dump di arresto anomalo elimina evidenze che potrebbero servirti per indagare sugli arresti anomali, quindi esaminali prima.

## Rimuovere l'installazione precedente di Windows

Dopo un aggiornamento delle funzionalità, **Installazione Windows precedente** in **Pulizia** può rimuovere la copia di ripristino tramite la richiesta di rimozione supportata da Windows. Si esegue da sola, richiede **Ho eseguito il backup di tutti i file e impostazioni necessari dall'installazione precedente** e rimuove in modo permanente la possibilità di tornare alla versione precedente.

## Dalla riga di comando

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

Vedi [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) e [previous-windows](cli.html#cmd-previous-windows).

## Correlati

- [Sezione Manutenzione](maintenance.html#cleanup)
- [Sezione Archiviazione](storage.html)
- [Controllare e riparare un'unità](check-a-drive.html)
