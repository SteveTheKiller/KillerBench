# Download e configurazione

KillerBench 1.0.0 non è ancora stato rilasciato. Non è ancora disponibile alcun pacchetto o download pubblico di KillerBench. Questa pagina descrive i requisiti attuali del pacchetto, così puoi prepararti senza scaricare una copia non ufficiale.

## Windows e runtime

- La destinazione di pubblicazione attuale è **Windows x64**. Windows 11 x64 è il riferimento attuale per la convalida nativa. Non sono previste distribuzioni native per ARM64, x86 o Windows Server.
- KillerBench viene distribuito in due forme. Il programma di installazione è **dipendente dal framework** e richiede **.NET Desktop Runtime 10 per Windows x64**. Il .NET Runtime di base, ASP.NET Core Runtime e .NET Framework non forniscono WPF. L'edizione portatile include il proprio runtime e non richiede installazioni separate. Lo strumento da riga di comando `killerbench-cli.exe` richiede .NET 10 Runtime per Windows x64. L'SDK serve per lo sviluppo, non per l'uso normale.
- Usa una versione di Windows supportata da [.NET 10 su Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows). Il supporto del runtime non significa che le riparazioni di KillerBench siano convalidate su ogni edizione o build supportata. La copertura di Windows 10 è ancora in sospeso e le sue edizioni supportate dipendono dagli attuali requisiti del ciclo di vita di Microsoft.
- Ottieni il Desktop Runtime solo dalla [pagina di download di .NET 10 di Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), scegliendo **.NET Desktop Runtime**, **Windows**, **x64**. Segui le regole di installazione del software della tua organizzazione.

Se `dotnet` è disponibile, `dotnet --list-runtimes` dovrebbe elencare `Microsoft.WindowsDesktop.App 10.0.x` per l'installazione x64. Un runtime installato solo per un'altra architettura non soddisfa il pacchetto di installazione x64.

## Quando verrà pubblicato un pacchetto

1. Usa il download ufficiale collegato da questo sito. Non è ancora attivo alcun link di download di KillerBench.
2. Leggi i requisiti del pacchetto, la [licenza GPLv3](../license.txt), le istruzioni per il Codice sorgente corrispondente, gli avvisi di terze parti inclusi e le informazioni su firma e checksum. Il solo nome del file non ne dimostra l'autenticità.
3. Esegui il programma di installazione per l'account corrente, oppure per tutti gli utenti dopo una richiesta di elevazione. Aggiunge un collegamento nel menu Start, un collegamento facoltativo sul desktop e una voce in Installazione applicazioni. L'edizione portatile si avvia senza installazione.
4. Avvia KillerBench con il tuo account normale e segui [La tua prima diagnosi](first-run.html). Gli errori di accesso della diagnosi restano visibili; l'elevazione è una scelta separata e consapevole.

Non aggirare gli avvisi di sicurezza di Windows per eseguire una copia non verificata. Tieni un backup aggiornato prima di selezionare qualsiasi azione che modifica il computer.

All'avvio KillerBench controlla su GitHub se esiste una versione stabile più recente e chiede conferma prima di installarla. Il download deve corrispondere al checksum SHA-256 pubblicato. Disattiva il controllo con **All'avvio cerca aggiornamenti** in Informazioni.

## Limiti attuali

La build attuale ha dodici sezioni, diagnosi in sola lettura, rapporti e superfici di riparazione selezionate singolarmente. La convalida nativa delle riparazioni è ancora in corso. Un'interfaccia funzionante, una build riuscita o il risultato di un fixture non dimostrano il comportamento di una riparazione su un computer reale. Vedi [Come le modifiche restano sicure](safety.html#development-status).

La console illustrativa nella pagina principale è un output sintetico. Le schermate definitive dell'applicazione e il primo pacchetto pubblico sono ancora in sospeso.

## Sorgente, licenza e supporto

KillerBench è distribuito con licenza GPLv3. Le istruzioni per la fornitura del codice sorgente e le autorizzazioni dei caratteri di terze parti sono ancora in sospeso per il primo pacchetto pubblico. Leggi [Informazioni su KillerBench](about.html) per la disponibilità del sorgente, gli avvisi di distribuzione, la privacy delle evidenze e le informazioni di supporto. La [guida dell'Engine](../engine/) è per sviluppatori con una copia autorizzata del sorgente; non implica la disponibilità di un pacchetto pubblico dell'Engine.
