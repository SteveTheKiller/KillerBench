# Riparare i file di sistema

Usa questa guida quando i componenti di Windows sono danneggiati: le funzionalità non si installano, gli aggiornamenti segnalano errori dei componenti o gli strumenti di sistema si bloccano. KillerBench esegue DISM e SFC solo dopo che un controllo in tempo reale ha confermato il danno, poi verifica il risultato.

## Prima di iniziare

- Esegui KillerBench come amministratore.
- Risolvi prima qualsiasi riavvio in sospeso.
- Se il computer è gestito, controlla se un criterio indirizza DISM verso un'origine di riparazione specifica. KillerBench segnala quel criterio e non lo modifica.

## Cercare danni

1. Apri **Riparazione** e scegli **File di sistema**.
2. Lascia disattivato **Ripara automaticamente problemi confermati** e premi **Esegui diagnosi**.
3. Leggi il rapporto. Fa l'inventario degli stati dei pacchetti di manutenzione, delle funzionalità facoltative e delle capacità, controlla i prerequisiti e analizza i file di sistema protetti. Un risultato pulito significa che non viene offerta alcuna riparazione.

## Riparare danni confermati

1. Attiva **Ripara automaticamente problemi confermati** e premi **Esegui riparazione**. KillerBench sceglie DISM o SFC solo quando il controllo in tempo reale conferma un danno.
2. Attendi la verifica. Il rapporto mostra **Verificato** solo quando il controllo successivo rilegge uno stato riparato.
3. Se il rapporto indica che l'archivio non è recuperabile o che l'origine di riparazione non è disponibile, fermati lì. Ripetere la riparazione non cambierà l'esito.

Un'esecuzione interrotta di DISM o SFC viene riconciliata all'avvio successivo senza ripetere il comando di manutenzione.

## Pulire l'archivio componenti

Scegli **Pulizia dell'archivio componenti** in **Aggiornamenti**. Prima chiede a Windows se la pulizia è consigliata. La manutenzione facoltativa rimuove i componenti sostituiti e analizza di nuovo. Non usa mai ResetBase e non elimina mai a mano contenuti di WinSxS.

## Dalla riga di comando

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

Vedi [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) e [servicing-logs](cli.html#cmd-servicing-logs). `repair` mostra un'anteprima per impostazione predefinita e agisce solo con `--execute`.

## Correlati

- [Riparare Windows Update](fix-windows-update.html)
- [Sezione Riparazione](repair.html#system-files)
- [Come le modifiche restano sicure](safety.html)
