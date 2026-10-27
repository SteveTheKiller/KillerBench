# Eseguire KillerBench dagli script

Usa la riga di comando quando vuoi le evidenze di KillerBench in uno script, in un processo RMM o in una console remota. `killerbench-cli` esegue lo stesso Engine dell'app desktop e stampa JSON.

## Per iniziare

1. Apri PowerShell nella cartella di `killerbench-cli.exe`, oppure aggiungi quella cartella al percorso.
2. Esegui `killerbench-cli help` per elencare tutti i comandi con il relativo limite di sicurezza.
3. Esegui `killerbench-cli diagnostic-scopes` per vedere gli 11 gruppi di diagnostica consigliati.

## Eseguire un'analisi e salvare un rapporto

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` è in sola lettura a meno che tu non aggiunga `--auto-repair`. `--report` scrive lo stesso rapporto con tema che esporta l'app. Usa `--scope` per controllare solo alcune aree e `common-check` per un JSON combinato senza giornale del flusso di lavoro.

## Leggere i codici di uscita

| Codice | Significato |
| --- | --- |
| 0 | L'anteprima o il risultato verificato è riuscito. |
| 1 | Un errore di avvio, di input o del giornale ha fermato il comando. |
| 2 | Gli argomenti non sono stati riconosciuti. |
| 3 | Bloccato, non riuscito o non verificato. |
| 4 | Un flusso di lavoro richiede un riavvio prima di poter verificare. |
| 130 | L'hai fermato tu. |

L'uscita 0 su un comando in sola lettura significa che è stato eseguito, non che il computer sia sano. Leggi il JSON per i rilevamenti.

## Apportare una modifica

Ogni comando che può modificare il computer mostra un'anteprima per impostazione predefinita. Leggi l'anteprima, poi esegui lo stesso comando con `--execute`. Se una richiesta si interrompe, esegui il comando `-recover` corrispondente; riporta l'esito reale e non ripete mai la modifica.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Riaprire risultati precedenti

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## Usarlo da un assistente di IA

[KillerMCP](../mcp.html) può chiamare la CLI installata per cercare i codici di errore di Gestione dispositivi e di Windows.

## Correlati

- [Riferimento della riga di comando](cli.html)
- [Guida per sviluppatori dell'Engine](../engine/index.html)
- [Risoluzione dei problemi](troubleshooting.html)
