# Riparare Windows Update

Usa questa guida quando gli aggiornamenti non riescono, si bloccano o continuano a reinstallarsi. KillerBench legge prima la catena di aggiornamento, spiega l'errore con le evidenze che lo supportano e avvia un servizio o ripara l'archivio componenti solo quando evidenze aggiornate dimostrano quel problema esatto.

## Prima di iniziare

- Esegui KillerBench come amministratore. I servizi di aggiornamento, l'archivio componenti e i file di sistema protetti richiedono l'elevazione per fornire evidenze complete.
- Riavvia prima se Windows ha già un riavvio in sospeso. Molte riparazioni degli aggiornamenti restano bloccate finché c'è un riavvio in sospeso.
- Tieni il computer collegato alla corrente durante la manutenzione.

## Capire perché gli aggiornamenti non riescono

1. Apri **Aggiornamenti** e scegli **Windows Update** nella barra laterale degli strumenti.
2. Lascia disattivato **Ripara automaticamente problemi confermati** al primo passaggio e premi **Esegui diagnosi**.
3. Leggi il **Rapporto leggibile**. Elenca i servizi di aggiornamento arrestati, i riavvii in sospeso, lo spazio libero insufficiente, i danni all'archivio componenti e i codici di errore esatti registrati da Windows, con un passaggio successivo consigliato.
4. Se il rapporto indica un codice di errore, i controlli di approfondimento corrispondenti vengono eseguiti automaticamente. Puoi anche cercarne uno tu con `killerbench-cli update-code 0x80070002`.

## Osservare un aggiornamento che sembra bloccato

Scegli **Monitor attività di manutenzione** in **Aggiornamenti** e premi **Esegui diagnosi**. Acquisisce istantanee ripetute dell'attività di aggiornamento, di Setup e di Installer, dei totali di trasferimento e degli indicatori di riavvio. L'attività viene mostrata solo come evidenza; non afferma mai da sola un blocco o un guasto.

## Riparare ciò che il rapporto ha trovato

1. Esegui di nuovo **Windows Update** con **Ripara automaticamente problemi confermati** attivato. Può avviare servizi di aggiornamento abilitati di cui è confermato l'arresto, ed esegue DISM o SFC solo quando il controllo in tempo reale conferma un danno.
2. Per un singolo servizio, scegli invece il suo strumento specifico, come **Servizio BITS**, **Servizi di crittografia**, **Programma di installazione moduli di Windows** o **Servizio Update Orchestrator**. Ognuno offre il ripristino solo per quel servizio esatto.
3. Quando il rapporto indica di riavviare, riavvia prima di eseguire altro. KillerBench registra gli esiti che richiedono un riavvio e rimanda le attività successive finché il riavvio non è avvenuto.

Ogni riparazione viene registrata nel giornale e riletta. Vedi [Come le modifiche restano sicure](safety.html) per la sequenza completa.

## Installare un aggiornamento specifico

1. Scegli **Aggiornamenti disponibili** in **Aggiornamenti** e premi **Esegui diagnosi** per individuare gli aggiornamenti dall'origine configurata.
2. Seleziona gli aggiornamenti che vuoi e premi **Anteprima selezionati**. L'anteprima elenca esattamente cosa verrebbe installato e qualsiasi riavvio richiesto.
3. Leggi l'anteprima, seleziona la casella di conferma, quindi premi **Installa**. La casella si deseleziona dopo ogni esecuzione e KillerBench non seleziona né approva mai un aggiornamento al posto tuo.

## Dalla riga di comando

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

Vedi [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) e [update-plan](cli.html#cmd-update-plan). Se un'installazione si è interrotta, esegui [update-recover](cli.html#cmd-update-recover); controlla l'esito e non ritenta mai l'aggiornamento.

## Correlati

- [Sezione Aggiornamenti](updates.html)
- [Riparare i file di sistema](repair-system-files.html)
- [Risoluzione dei problemi](troubleshooting.html)
