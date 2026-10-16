# Indagare su arresti anomali e schermate blu

Usa questa guida quando Windows si riavvia inaspettatamente, mostra un errore di arresto o un'applicazione continua a bloccarsi. KillerBench raccoglie in un unico rapporto le evidenze su arresti anomali, spegnimenti, errori hardware e affidabilità. Descrive ciò che è stato registrato e non tira mai a indovinare una causa principale.

## Controllare il computer

1. Apri **Diagnostica** e scegli **Parametri vitali**.
2. Premi **Esegui diagnosi**. Il rapporto combina memoria, stato dei dischi, problemi dei dispositivi, cronologia di Monitoraggio affidabilità, record degli errori hardware, rapporti di arresto anomalo, spegnimenti imprevisti e risultati di Diagnostica memoria Windows.
3. Leggi la conclusione e le evidenze sottostanti. Gli arresti anomali storici spiegano cosa è successo; non dimostrano che il guasto sia ancora presente.

## Restringere il campo

- **Cronologia errori hardware** legge i record WHEA e della diagnostica della memoria insieme ai problemi dei dispositivi e allo stato dei dischi.
- **Cronologia affidabilità** raggruppa gli arresti anomali delle applicazioni, i blocchi e gli spegnimenti imprevisti degli ultimi 14 giorni.
- **Errori delle applicazioni** si concentra sui programmi che si bloccano.
- **Cronologia installazione driver** mostra le installazioni di driver non riuscite associate ai dispositivi presenti ora.
- **Problemi dei dispositivi** elenca i dispositivi con codici di errore di Gestione dispositivi.

## Leggere i dump di arresto anomalo

KillerBench elenca la configurazione dei dump e i metadati dei file di dump e può analizzare la struttura dei minidump per ricavare l'eccezione e i nomi dei moduli. L'app desktop non apre mai il contenuto dei dump. Con un pacchetto di conoscenze di controllo bug caricato, i codici di arresto vengono risolti nel loro significato documentato.

Conserva i dump finché non hai finito di indagare. **Pulizia** può rimuoverli, ma così elimini le evidenze.

## Dalla riga di comando

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

Vedi [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) e [device-code](cli.html#cmd-device-code).

## Correlati

- [Sezione Diagnostica](diagnostics.html#vitals)
- [Rapporti ed evidenze](reports.html)
