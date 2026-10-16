# Rendere Windows più riservato

Usa questa guida per esaminare e rafforzare la privacy di un portatile. KillerBench legge le impostazioni attuali, mostra quali sono già controllate da criteri o dalla gestione dei dispositivi e modifica solo le impostazioni che scegli, una alla volta, con un annullamento esatto.

## Esaminare le impostazioni attuali

1. Apri **Privacy**.
2. Seleziona le aree che vuoi esaminare. Tutte le aree partono deselezionate.
3. Premi **Esegui diagnosi**. Il rapporto mostra il valore attuale per questo computer e per l'account che esegue KillerBench, e segnala le impostazioni controllate da criteri.

## Modificare un'impostazione

1. Scegli l'impostazione in **Impostazione privacy**, come fotocamera, microfono, posizione, dati di diagnostica, pubblicità, cronologia degli Appunti, ricerca dei contenuti nel cloud o punti salienti della ricerca.
2. Scegli il nuovo valore e seleziona **Applica quando seleziono Esegui diagnosi**.
3. Premi **Esegui diagnosi**. KillerBench controlla se c'è un criterio che la gestisce, registra il valore attuale, effettua quell'unica modifica e la rilegge.

Le impostazioni controllate da Criteri di gruppo o dalla gestione dei dispositivi non vengono toccate. Modificale dove vengono gestite.

## Annullare una modifica

Ogni modifica verificata conserva un giornale. L'annullamento corrispondente ripristina il valore precedente esatto, sempre dopo un controllo dei criteri.

## Alleggerire Windows

Per suggerimenti, contenuti consumer, Recall e funzionalità di IA, elementi superflui nella barra delle applicazioni e in Start, Edge e software preinstallato, apri **Manutenzione** e scegli **Alleggerisci Windows**. L'esame è in sola lettura e niente viene selezionato automaticamente.

## Dalla riga di comando

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

Vedi [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) e [debloat-review](cli.html#cmd-debloat-review). Una chiave non supportata elenca le scelte supportate.

## Correlati

- [Sezione Privacy](privacy.html)
- [Configurare un nuovo computer](set-up-new-pc.html)
