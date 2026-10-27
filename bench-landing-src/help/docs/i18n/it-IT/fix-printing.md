# Risolvere i problemi di stampa

Usa questa guida quando i processi restano in coda, una stampante risulta offline o quella predefinita è sbagliata. KillerBench legge prima spooler, code, processi, driver e porte, poi ti permette di correggere una coda o un processo esatto.

## Controllare le stampanti

1. Apri **Dispositivi** e scegli **Stampa**.
2. Premi **Trova stampanti locali** e scegli la coda che non funziona. Per controllare tutte le code, scegli **Usa controlli generali di stampa**.
3. Lascia disattivato **Ripara automaticamente problemi confermati** e premi **Esegui diagnosi**.
4. Leggi il rapporto. Mostra lo stato di Spooler di stampa, le code in pausa, la modalità offline, i processi visibili, le condizioni dei driver, le registrazioni delle porte, gli errori recenti di PrintService e, per le stampanti di rete, un tentativo di connessione all'indirizzo assegnato alla stampante.

Una connessione riuscita o un codice di condizione pulito non dimostrano che la stampante abbia carta, toner o un meccanismo funzionante.

## Riparare la coda

1. Esegui di nuovo **Stampa** con **Ripara automaticamente problemi confermati** attivato. Può avviare lo spooler abilitato, riprendere una coda esatta in pausa che ha processi in attesa e disattivare la modalità offline.
2. Disattivare la modalità offline può rilasciare i processi in attesa, quindi assicurati che debbano ancora essere stampati.
3. Una coda messa in pausa di proposito e senza processi in attesa resta in pausa.

## Eliminare un processo bloccato

1. Premi **Trova processi di stampa bloccati**.
2. Seleziona il processo che mostra Errore o Bloccato e seleziona **Annulla solo questo processo selezionato**.
3. Premi **Esegui diagnosi**. Viene annullato solo quel processo e il risultato viene riletto.

## Correggere la stampante predefinita

Scegli la coda che dovrebbe essere quella predefinita, seleziona **Ripara una stampante predefinita mancante o in conflitto con questa coda** e premi **Esegui diagnosi**. Agisce solo quando la predefinita manca o è in conflitto.

## Dalla riga di comando

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

Vedi [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) e [default-printer-set](cli.html#cmd-default-printer-set).

## Correlati

- [Sezione Dispositivi](devices.html#printing)
- [Risolvere i problemi di rete e internet](fix-network.html)
