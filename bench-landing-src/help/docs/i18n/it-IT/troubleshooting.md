# Risoluzione dei problemi

## Un'azione risulta bloccata

Un risultato bloccato significa che l'azione richiesta non ha potuto procedere. Leggi il messaggio e il giornale per capire se si è fermata prima dell'invio o dopo un passaggio precedente. Non dare per scontato che un'operazione completata in parte sia stata annullata. Le cause di prerequisito più comuni sono queste.

- **È richiesto l'accesso di amministratore.** Chiudi KillerBench e riavvialo con **Esegui come amministratore**.
- **Non deve esserci un riavvio in sospeso.** Windows ha un riavvio in sospeso. Riavvia il computer, poi esegui di nuovo l'azione.
- **È presente un criterio gestito.** Criteri di gruppo o la gestione dei dispositivi controllano l'impostazione. Modificala lì.
- **Le evidenze sono cambiate.** Qualcosa è cambiato tra il primo controllo e il controllo finale. Esegui di nuovo lo strumento per leggere il nuovo stato.

## Una richiesta precedente deve essere riconciliata

KillerBench ha trovato un record in sospeso di una richiesta interrotta. Esegui di nuovo la stessa azione. Controlla cosa è successo e riporta l'esito reale senza ripetere la richiesta. Dalla riga di comando, usa il comando `-recover` corrispondente, per esempio `killerbench-cli bitlocker-auto-unlock-recover`.

## Un controllo mostra Incompleti o Sconosciuto

Non è stato possibile leggere le evidenze, spesso perché KillerBench non era in esecuzione come amministratore o un servizio di Windows non era disponibile. KillerBench lo segnala con onestà invece di presumere che il computer sia sano.

## Dove sono conservati i file

Giornali, record in sospeso ed evidenze salvate si trovano in `%LOCALAPPDATA%\KillerBench`. Ogni azione protetta ha la propria cartella. Conservali quando ti serve una traccia di cosa è cambiato.

## Segnalare un problema

KillerBench è in sviluppo. Quando segnali un problema, allega il rapporto esportato e il registro attività, e rimuovi tutto ciò che non vuoi condividere.
