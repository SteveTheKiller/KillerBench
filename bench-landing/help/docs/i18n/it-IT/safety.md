# Come le modifiche restano sicure

KillerBench tratta diagnosi e modifica come attività distinte. La diagnosi legge soltanto. L'analisi completa del sistema non esegue mai riparazioni. Seleziona una riparazione o un'azione precisa, esamina opzioni e prerequisiti e conferma la modifica proposta.

## Prima di una modifica

1. **Evidenze aggiornate.** Lo strumento rilegge lo stato attuale. Un rapporto salvato o un vecchio evento non autorizzano mai una modifica.
2. **Idoneità esatta.** La modifica viene offerta solo quando le evidenze corrispondono al problema esatto che risolve. Qualsiasi dato incompleto la blocca.
3. **La tua conferma.** Esamina l'azione selezionata nella finestra di conferma. Alcune azioni, come la decrittografia di un'unità, richiedono una seconda conferma. La finestra segue il tema attivo dell'app; il suo aspetto non cambia i requisiti dell'azione.
4. **Prerequisiti.** Vengono controllati i diritti di amministratore, lo stato di riavvio e il controllo da parte di criteri o della gestione dei dispositivi. Le impostazioni controllate da un criterio non vengono toccate.
5. **Un record in sospeso.** KillerBench annota cosa sta per fare prima di chiederlo a Windows.
6. **Un ultimo controllo.** Tutto viene ricontrollato subito prima della richiesta, così un cambiamento nel frattempo la ferma.

## Dopo una modifica

- KillerBench rilegge il risultato e indica **Verificato** solo quando Windows mostra lo stato previsto.
- Se Windows restituisce un esito positivo ma lo stato non è cambiato, il risultato lo dice e non viene ripetuto nulla.
- Ogni modifica viene salvata in un giornale in `%LOCALAPPDATA%\KillerBench`.

## Se qualcosa si interrompe

Le azioni con giornale supportate conservano un record in sospeso quando una richiesta potrebbe aver raggiunto Windows e la verifica non è stata completata. Usa il percorso di riconciliazione documentato dell'azione, o il comando `-recover` corrispondente, per ispezionare quel record. La riconciliazione non è un ripristino automatico. Conserva il giornale quando l'esito è incerto e non ripetere a mano la modifica. Il comportamento in caso di arresto anomalo, annullamento o avvio successivo richiede ancora una convalida nativa specifica per ogni azione.

## Cosa KillerBench non farà

- Cancellare un TPM, eliminare protezioni che non ha creato o mostrare password di ripristino.
- Modificare un'impostazione controllata da Criteri di gruppo o dalla gestione dei dispositivi.
- Eseguire una riparazione solo perché un evento storico o un rapporto importato cita un problema.

Diagnosi e analisi dei rapporti vengono eseguite in locale. Un'azione che selezioni può contattare una destinazione esterna, come i download di software o il backup della chiave di ripristino in Active Directory o Entra ID. Controlla la destinazione prima di confermare. Le catture salvate e le evidenze grezze possono contenere l'identità del computer e percorsi di origine, quindi esaminale prima di condividerle.

## Stato dello sviluppo

La copertura delle riparazioni è ancora in espansione. Una build riuscita o un controllo simulato non dimostrano il comportamento nativo di una riparazione. Il file `VM-VALIDATION.md` del repository tiene traccia degli scenari su macchine Windows usa e getta e della convalida rimanente.
