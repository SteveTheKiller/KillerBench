# Guida di KillerBench

KillerBench è un ambiente di lavoro per la diagnosi e la riparazione di Windows pensato per i tecnici. Prima controlla il computer, spiega cosa ha trovato con le evidenze di ogni rilevamento e modifica Windows solo quando scegli una correzione precisa e la confermi.

KillerBench è in sviluppo. Non esiste ancora un download pubblico. Queste pagine descrivono la build attuale.

## Cosa fa

- Esegue diagnosi in sola lettura su Windows Update, dispositivi, archiviazione, rete, stampa, sicurezza, app e profili, poi scrive un rapporto leggibile.
- Offre riparazioni protette che vengono eseguite solo quando evidenze aggiornate dimostrano il problema esatto, e ne verifica poi il risultato.
- Copre attività di configurazione e protezione come BitLocker, le impostazioni di privacy, l'alleggerimento di Windows e la configurazione iniziale di un nuovo computer.
- Esegue diagnosi e analisi dei rapporti in locale, senza IA e senza un account KillerBench. Alcune azioni selezionate, come i download e il backup della chiave di ripristino in Entra ID, possono contattare servizi esterni.
- Usa il tema attivo dell'app per la console, i rapporti, le conferme e le finestre di dialogo Apri, Salva e Cartella.

## Da dove iniziare

- [Download e configurazione](setup.html)
- [Informazioni su KillerBench](about.html)

- [La tua prima diagnosi](first-run.html) ti guida in un'analisi completa del sistema.
- [L'area di lavoro](workspace.html) spiega sezioni, riquadri e scorciatoie.
- [Come le modifiche restano sicure](safety.html) spiega cosa controlla KillerBench prima e dopo ogni modifica.
- Le [guide passo passo](fix-windows-update.html) illustrano le attività più comuni.
- Le pagine **Sezioni** descrivono ogni strumento, raggruppato come nell'app.
- [Riga di comando](cli.html) elenca tutti i comandi di `killerbench-cli`.
- [Risoluzione dei problemi](troubleshooting.html) tratta azioni bloccate, riavvii e richieste interrotte.

Gli sviluppatori che vogliono integrare la diagnostica nei propri strumenti possono leggere la [Guida per sviluppatori dell'Engine](../engine/index.html).
