# La tua prima diagnosi

Inizia da [Download e configurazione](setup.html), poi esegui un'Analisi completa del sistema. Raccoglie evidenze diagnostiche e salva un giornale locale; non esegue riparazioni. Un rilevamento descrive l'ambito ispezionato, non ogni possibile guasto del computer.

## Eseguire l'analisi

1. Apri KillerBench. La sezione **Diagnostica** si apre con **Analisi completa del sistema** selezionata.
2. Lascia disattivato **Ripara automaticamente problemi confermati** alla prima esecuzione.
3. Premi **Esegui diagnosi**. L'avanzamento compare in **Output grezzo**, sulla destra, mentre viene eseguito ogni controllo.
4. Al termine, il riquadro di output passa al **Rapporto leggibile**.

Inizia senza elevazione. Alcune evidenze di Windows, come le protezioni BitLocker e i servizi protetti, richiedono diritti di amministratore. Con un account standard questi controlli segnalano che l'evidenza non è disponibile. Se ti serve, completa l'esecuzione in corso, conserva il rapporto e poi riapri volutamente KillerBench con **Esegui come amministratore**. L'elevazione non seleziona né approva alcuna riparazione.

## Leggere il rapporto

Il rapporto si apre con i conteggi di **Da esaminare**, **Riparazioni verificate**, **Incompleti** e **Controllati**, seguiti da un passaggio successivo consigliato.

- **Cosa richiede attenzione** elenca i problemi supportati dalle evidenze, a partire dai più importanti.
- **Informazioni raccolte** mostra cosa è stato letto, comprese le evidenze incomplete o non disponibili.
- Gli eventi storici sono indicati come cronologia. Un errore del mese scorso non viene trattato come un guasto attuale.

L'assenza di evidenze non conta mai come stato sano. Se un controllo non è stato eseguito, il rapporto lo dice.

## Scegliere i controlli

Apri **Scegli controlli (avanzato)** per selezionare singoli controlli invece dell'analisi completa. Ogni sezione dell'app ha anche strumenti specifici, descritti nelle pagine **Sezioni** di questa guida.

## Correggere ciò che hai trovato

Quando il rapporto consiglia una correzione, indica la sezione e lo strumento da aprire. Leggi [Come le modifiche restano sicure](safety.html) prima della tua prima riparazione. La convalida nativa è ancora in corso; una diagnosi riuscita non dimostra che ogni riparazione offerta sia stata testata sulla tua build di Windows. Tieni dei backup ed esamina la destinazione esatta e le conseguenze prima di qualsiasi modifica.
