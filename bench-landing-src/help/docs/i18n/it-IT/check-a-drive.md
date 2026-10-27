# Controllare e riparare un'unità

Usa questa guida quando un volume segnala errori, Windows chiede di analizzare un'unità o i file non si aprono. KillerBench controlla prima il volume in sola lettura e pianifica o esegue una riparazione del file system solo dopo che hai confermato di avere un backup.

## Prima di iniziare

- Esegui il backup dei dati importanti presenti sul volume. Una riparazione del file system può modificare o rimuovere file danneggiati.
- Esegui KillerBench come amministratore.

## Controllare il volume

1. Apri **Archiviazione** e scegli **Archiviazione**.
2. Scegli l'unità in **Volume da controllare** e premi **Esegui diagnosi**.
3. Leggi il rapporto. Mostra lo stato del disco e del volume, il bit dirty, l'autoriparazione NTFS online, gli errori del controller e dei dispositivi disco, i contatori di affidabilità e gli eventi di archiviazione recenti, come reimpostazioni, nuovi tentativi e rimozioni impreviste.

Un controllo pulito significa che non viene offerta alcuna riparazione. Temperatura del disco, usura e contatori di I/O non corretti sono evidenze da esaminare e da soli non attivano una riparazione.

## Riparare il file system

1. Premi **Esamina riparazione file system** e scegli **Riparazione puntuale** o **Analisi e riparazione offline**.
2. Per un volume di dati, seleziona **Consenti disattivazione di un volume non di sistema** se puoi chiudere tutto ciò che lo usa. Il volume di Windows non può essere messo offline, quindi la sua riparazione viene pianificata per il riavvio successivo.
3. Seleziona **I dati importanti su questo volume sono salvati**.
4. Conferma la riparazione. KillerBench ricontrolla il volume esatto subito prima di agire e registra prima una voce in sospeso.
5. Per una riparazione pianificata, riavvia quando ti è comodo. KillerBench verifica il risultato dopo il riavvio senza eseguire di nuovo la riparazione.

## Ottimizzare o eseguire il retrim

Lo stesso strumento può analizzare il volume ed eseguire l'ottimizzazione consigliata da Windows, **Retrim archiviazione a stato solido** o una deframmentazione del disco rigido. Ognuna è un'azione confermata separatamente su un solo volume esatto e sano.

## Dalla riga di comando

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

Vedi [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) e [volume-optimize](cli.html#cmd-volume-optimize).

## Correlati

- [Sezione Archiviazione](storage.html)
- [Liberare spazio su disco](free-disk-space.html)
- [Risoluzione dei problemi](troubleshooting.html)
