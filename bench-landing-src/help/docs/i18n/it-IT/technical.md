# La tecnica di KillerBench

Uno sguardo sotto il cofano: come KillerBench legge un computer Windows, mantiene oneste le sue evidenze e decide quando una modifica è consentita.

## Stack tecnologico

KillerBench è un'app nativa di Windows. L'app desktop, la riga di comando e l'Engine sottostante condividono le stesse evidenze diagnostiche e gli stessi flussi di riparazione.

| Componente | Dettaglio |
| --- | --- |
| Interfaccia | WPF su .NET 10 (`net10.0-windows`), Windows x64, cornice della finestra personalizzata |
| Engine | `Engine.Core`, `Engine` e `Engine.Windows`, senza alcun riferimento a WPF o ai controlli desktop |
| Riga di comando | `killerbench-cli`, un host separato sullo stesso Engine |
| Provider di Windows | .NET, API di Windows, PowerShell e strumenti da riga di comando di Windows, ognuno eseguito entro limiti fissi di tempo, record e dimensione |
| Rapporti | Output grezzo, Rapporto leggibile, HTML, testo e JSON |
| Dati di riferimento | Pacchetti di conoscenze con versione per codici di errore, identificatori hardware, produttori e riferimenti di supporto |
| Pacchetti | Programma di installazione: dipendente dal framework, richiede .NET Desktop Runtime 10 per Windows x64. Portatile: autonomo, nessuna installazione del runtime |
| Aspetto | 13 temi della famiglia e 20 lingue |

## Installazione, cartelle e dati

KillerBench viene distribuito in due forme. Il programma di installazione è il download standard e supporta l'installazione per utente o per tutti gli utenti. L'edizione portatile include il proprio runtime .NET e si avvia senza installazione.

KillerBench viene eseguito con i diritti dell'account che lo ha avviato. Non si eleva da solo. Avvialo con **Esegui come amministratore** quando evidenze protette, come le protezioni BitLocker, o una riparazione richiedono l'accesso di amministratore. L'elevazione non seleziona né approva mai una riparazione.

### Cosa fa il programma di installazione

Il programma di installazione verifica ed estrae l'applicazione in `%LOCALAPPDATA%\Programs\KillerBench\`, oppure in `%ProgramFiles%\KillerBench\` dopo una richiesta di elevazione per tutti gli utenti. Aggiunge un collegamento nel menu Start, un collegamento facoltativo sul desktop e una voce in Installazione applicazioni con disinstallazione normale e silenziosa. L'edizione portatile verifica ed estrae il proprio contenuto in `%LOCALAPPDATA%\KillerBench\Portable\` per l'esecuzione corrente ed elimina le copie abbandonate all'avvio successivo.

### Dove si trovano le cose

| Percorso | Contenuto |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Tema, colore di accento, scala dell'interfaccia e lingua |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | Un giornale JSON per ogni esecuzione di un flusso di lavoro: decisioni, output dei comandi delimitato e risultati delle riparazioni |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | Giornali di esecuzione delle riparazioni di manutenzione come DISM e SFC |
| Cartelle delle funzionalità in `%LOCALAPPDATA%\KillerBench\` | Protezioni in sospeso ed esiti delle singole azioni protette, per esempio `BitLockerBackup`, `Time`, `Firewall` e `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | Il livello di riparazione di KillerMCP scelto in Informazioni |
| `HKCU` o `HKLM\Software\KillerBench` | Il percorso di installazione, per l'ambito di installazione corrispondente |
| Un file `.kbench` che scegli | Una cattura salvata con le evidenze conservate |

I giornali restano su questo computer e KillerBench non li carica. Elimina un giornale solo quando non ti servono più le evidenze di un esito incerto.

## Prima la diagnosi

KillerBench tratta diagnosi e modifica come attività distinte. La diagnosi legge soltanto. Una modifica avviene solo quando scegli una riparazione o un'azione precisa, ne esamini opzioni e prerequisiti e la confermi.

L'analisi completa del sistema è solo diagnostica, anche quando un flusso di lavoro mirato ha la riparazione automatica attivata. Catture salvate, log importati ed eventi storici non autorizzano mai una riparazione.

### Raccolta delimitata

Ogni comando nativo e ogni provider di Windows viene eseguito entro limiti fissi di tempo, record e dimensione. Un provider registra il proprio stato di uscita, il timeout, il troncamento, l'output standard e l'output di errore. Un provider non riuscito o troncato non può mai essere letto come una risposta pulita e vuota.

### Conservazione minima

I raccoglitori conservano ciò che serve a un rilevamento. Molti risultati mantengono nomi, categorie, conteggi, stati e timestamp, tralasciando credenziali, righe di comando, percorsi completi, identità degli account, endpoint di rete e payload degli eventi non pertinenti.

## Evidenze che puoi seguire

Ogni rilevamento è legato alle evidenze che lo hanno prodotto, e il rapporto separa sempre ciò che è stato dimostrato da ciò che è stato solo osservato.

| Stato | Significato |
| --- | --- |
| Da esaminare | Le evidenze supportano un problema che merita attenzione. |
| Informazione | Sono state raccolte evidenze, mostrate come contesto. |
| Incompleti o Sconosciuto | Un controllo non è stato completato o l'evidenza non era disponibile. Non conta mai come stato sano. |
| Storico | Un evento del passato. Spiega la cronologia ma non dimostra un guasto attuale. |
| Verificato | Una riparazione o una modifica è stata confermata rileggendo il risultato. |

### Sconosciuto è un risultato

Sconosciuto significa che l'accesso è stato negato, mancava un'origine, un provider non è riuscito, l'output era malformato, un limite è stato superato o le evidenze erano in disaccordo. KillerBench mostra quel limite e il prossimo passo utile invece di nasconderlo.

### La cronologia resta cronologia

I record degli eventi e i log importati descrivono ciò che è stato registrato in un momento passato. Possono indicare un codice di errore o il prossimo ambito diagnostico. Non dimostrano che il guasto sia ancora attivo.

## Riparazioni protette

Le riparazioni sono operazioni tipizzate con regole di idoneità, esecuzione, verifica e ripristino. Non sono stringhe di comando prese da una riga di log. Ogni riparazione supportata segue la stessa sequenza:

1. Raccogliere evidenze attuali complete.
2. Corrispondere a una ricetta supportata.
3. Esaminare le opzioni richieste e le conseguenze.
4. Confermare i prerequisiti amministrativi e dei criteri.
5. Raccogliere di nuovo le evidenze decisive subito prima della modifica.
6. Scrivere un record di preparazione duraturo.
7. Eseguire un'unica azione nativa delimitata.
8. Raccogliere evidenze di verifica aggiornate.
9. Registrare un esito verificato, bloccato, interrotto o incerto.

Se le evidenze decisive cambiano prima che l'azione venga eseguita, KillerBench si ferma senza eseguirla.

### Scelte esplicite

Le riparazioni che dipendono da un'opzione richiedono una tua scelta esatta, come un volume, una coda di stampa, la destinazione della chiave di ripristino, un valore di alimentazione, la versione di un pacchetto o un'origine di riparazione. KillerBench non inventa mai quei valori.

### Computer gestiti

Criteri di gruppo e gestione dei dispositivi possono rendere una modifica locale inefficace o inappropriata. Le riparazioni supportate controllano chi gestisce un'impostazione e si fermano quando quella gestione è centralizzata o sconosciuta. Le impostazioni controllate da un criterio non vengono toccate.

### Riparazione automatica

La riparazione automatica lascia procedere solo le ricette idonee. Non aggira le evidenze, non sceglie opzioni mancanti, non ignora i criteri, non ritenta un'azione incerta, non riavvia Windows e non trasforma in riparazione un rilevamento che richiede un esame.

## Giornali e interruzioni

Prima di chiedere a Windows una modifica, KillerBench annota cosa sta per fare. Ogni modifica viene salvata in un giornale in `%LOCALAPPDATA%\KillerBench`.

Dopo la modifica, KillerBench rilegge il risultato e indica **Verificato** solo quando Windows mostra lo stato previsto. Se Windows segnala un esito positivo ma lo stato non è cambiato, il risultato lo dice e non viene ripetuto nulla.

Un processo può fermarsi dopo che Windows ha ricevuto una richiesta ma prima che la verifica sia completata. Il record in sospeso resta, e una riconciliazione successiva legge lo stato attuale per capire cosa è successo. La riconciliazione non è un ripristino automatico e non ripete mai la modifica originale. Conserva il giornale quando l'esito è incerto.

## Costanti e limiti

| Specifica | Valore |
| --- | --- |
| Copertura diagnostica | L'analisi completa del sistema esegue 49 aree diagnostiche e controlla 96 ricette di riparazione protette |
| Lettura di origini di testo | 1.000.000 di caratteri per origine, 2.000 righe corrispondenti mostrate |
| Fonti sessione | 12 nella barra laterale; una cattura contiene fino a 24 origini e 64 MB |
| Query sugli eventi | I 200 eventi più recenti per canale degli ultimi 7 giorni |
| Analisi di Windows Update | 15 secondi per origine, due minuti in totale |
| Indagine sui log | 10.000 voci analizzate, 256 codici distinti, 8 estratti di 320 caratteri per codice |
| Archivio CBS compresso | Archivio da 256 MiB, membro di log da 512 MiB, estrazione di 30 secondi |
| CHKDSK in sola lettura | Dieci minuti; una riparazione è limitata a 65 minuti |
| Controlli DNS e TCP | Dieci secondi per impostazione predefinita, al massimo un minuto |
| Inventario dei servizi | Fino a 1.024 servizi installati |
| Pacchetti di conoscenze | Fino a 32 aperti contemporaneamente |
| Esame dei file di grandi dimensioni | File di almeno 100 MB |
| Monitor attività di manutenzione | Intervallo da 1 a 60 secondi, da 1 a 120 istantanee |
| Bozze di attività pianificate | Limite di esecuzione da 1 a 4.320 minuti |
| Origini dell'ora personalizzate | Fino a otto peer NTP |
| Esportazione attività | Fino a 150 voci conservate |
| Lingue e temi | 20 lingue, 13 temi con varianti di accento |

## Cosa non farà

- Cancellare un TPM, eliminare protezioni che non ha creato o mostrare password di ripristino.
- Modificare un'impostazione controllata da Criteri di gruppo o dalla gestione dei dispositivi.
- Eseguire una riparazione solo perché un evento storico o un rapporto importato cita un problema.
- Trasformare evidenze mancanti, parziali o non riuscite in un verdetto di stato sano.

## Rapporti

Ogni esecuzione produce un rapporto che puoi leggere, filtrare ed esportare. Un rapporto separa:

1. Cosa è stato ispezionato
2. Quale problema è stato trovato
3. Quali evidenze supportano il rilevamento
4. Quale azione è stata proposta o eseguita
5. Come è stato verificato il risultato
6. Cosa resta sconosciuto o richiede un riavvio

**Output grezzo** conserva le evidenze tecniche con i numeri di riga originali, così puoi risalire da ogni rilevamento alla sua origine. Il **Rapporto leggibile** riassume la stessa esecuzione in linguaggio semplice, con il prossimo passo sicuro in cima. I rapporti HTML usano il sistema di temi di Killer Tools e restano leggibili senza l'app. Il testo privilegia la portabilità, e il JSON conserva campi strutturati per un altro programma.

## Log salvati e catture

KillerBench può indagare sulle evidenze senza eseguire nulla sul computer. Trascina un log di testo sulla finestra, oppure incolla il suo percorso e scegli **Apri log**. Le righe CBS, CSI e Setup vengono analizzate con i loro timestamp, e i rilevamenti rimandano alla riga originale esatta. I file EVTX e XML degli eventi si possono importare allo stesso modo.

**Analizza Windows Update** è una raccolta su richiesta e in sola lettura dei log di CBS, DISM, Panther e di rollback di Setup, dei canali degli eventi di aggiornamento e manutenzione, degli stati dei servizi di aggiornamento, dello spazio libero, del tempo di attività, degli indicatori di riavvio in sospeso e dei criteri di aggiornamento selezionati. **Salva cattura** scrive le evidenze conservate in un file `.kbench`. **Apri cattura** lo riapre senza i file originali e lo valuta di nuovo con le regole installate, così le stesse regole danno gli stessi rilevamenti.

## Modalità demo

Avvia `KillerBench.exe --demo` per provare **Esegui diagnosi** su una workstation fittizia. Ogni flusso di lavoro produce evidenze sintetiche e un avanzamento temporizzato. Non viene chiamato alcun provider di Windows, non viene eseguito alcun comando di riparazione e non viene scritto alcun giornale. I pulsanti degli strumenti separati, fuori da **Esegui diagnosi**, restano attivi, quindi non usarli come azioni dimostrative.

`KillerBench.exe --demo-screenshot` apre in sola visualizzazione un rapporto di Analisi completa del sistema per un computer fittizio fisso. Clic nell'area di lavoro, scorciatoie e trascinamento di file sono bloccati.

## Pacchetti di conoscenze

I dati di riferimento voluminosi e aggiornati spesso si trovano nei pacchetti di conoscenze invece che nella logica diagnostica: codici di errore di Windows e Setup, identificatori di produttori e dispositivi, riferimenti PCI, USB, ACPI e UEFI, dati dei produttori per OUI di rete, percorsi di driver e supporto e link di supporto selezionati.

Ogni pacchetto indica formato, versione dello schema, origine, revisione dell'origine, ora di generazione e hash del contenuto, e KillerBench li convalida prima dell'uso. Un pacchetto sconosciuto o non valido resta non disponibile invece di ripiegare su un'ipotesi. Un aggiornamento viene scaricato in un percorso temporaneo, convalidato e poi sostituisce il pacchetto attivo in un solo passaggio, così un aggiornamento non riuscito lascia al suo posto il pacchetto precedente.

I dati di conoscenza possono spiegare un identificatore o suggerire dove guardare dopo. Non possono trasformare un codice storico in un guasto attuale, scegliere automaticamente un driver né autorizzare una riparazione.

## Aggiornamenti e firma

All'avvio KillerBench chiede a GitHub l'ultima versione stabile, con un limite di quattro secondi, e propone un aggiornamento solo quando quella versione è più recente. Puoi disattivare il controllo con **All'avvio cerca aggiornamenti** in Informazioni, oppure nella richiesta di aggiornamento. Il download è il pacchetto che corrisponde alla copia in esecuzione, programma di installazione o portatile, e il suo SHA-256 deve corrispondere al `SHA256SUMS.txt` della versione prima che venga installato qualsiasi cosa. L'app si chiude e si riapre da sola per completare. Non esiste alcun servizio in background.

La scheda Informazioni mostra la versione, l'editore e l'identificazione digitale della firma del codice e lo SHA-256 dell'eseguibile in esecuzione, così puoi verificare la copia che hai.

## KillerMCP

KillerBench non include alcun agente di IA. [KillerMCP](../mcp.html) può collegarne uno allo stesso Engine. Le **Opzioni di KillerMCP** in Informazioni decidono cosa può fare quell'agente:

| Impostazione | Effetto |
| --- | --- |
| Disattivato (predefinito) | L'agente non può richiedere alcuna riparazione. |
| Consenti riparazioni sicure | L'agente può richiedere le riparazioni classificate come sicure. |
| Consenti anche riparazioni invasive | Le riparazioni invasive diventano disponibili, e ognuna richiede comunque un'approvazione. |

Solo una persona davanti a questo computer può alzare il livello. Dopo un esito di riparazione incerto, KillerMCP scrive un blocco, e le riparazioni dell'agente restano bloccate finché qualcuno non lo rimuove in Informazioni.

## Localizzazione

L'interfaccia è disponibile in 20 lingue: inglese, bengalese, ceco, tedesco, spagnolo, francese, ungherese, italiano, giapponese, kazako, norvegese, polacco, portoghese (Brasile), russo, sloveno, turco, ucraino, vietnamita e cinese semplificato e tradizionale. Un cambio di lingua si applica subito, compresi i nomi delle sezioni e dei flussi di lavoro, il testo di benvenuto e un rapporto leggibile aperto. Qualsiasi testo mancante viene mostrato in inglese.

Evidenze, rilevamenti dell'Engine, giornali e Output grezzo restano in inglese, come un log di Windows, così un rapporto si legge allo stesso modo su ogni computer e si può cercare un testo esatto.

## Riga di comando e codici di uscita

`killerbench-cli` esegue lo stesso Engine senza l'app desktop. L'avanzamento va all'errore standard e il JSON all'output standard. La maggior parte dei comandi mostra un'anteprima per impostazione predefinita, e una modifica richiede `--execute`. I comandi `-recover` corrispondenti esaminano un'azione interrotta senza ripeterla. Vedi [Eseguire KillerBench dagli script](use-the-cli.html) e il [riferimento dei comandi](cli.html).

| Codice di uscita | Comandi dei flussi di lavoro | Analisi e apertura |
| --- | --- | --- |
| 0 | Nessun problema irrisolto negli ambiti controllati | Copertura completata |
| 1 | Errore di avvio o del giornale | Errore |
| 2 | Argomenti non validi | Argomenti non validi |
| 3 | Richiesta attenzione | Raccolta parziale |
| 4 | Riavvio richiesto prima della verifica | |
| 130 | Interrotto | Annullato o timeout complessivo |

Nessun codice di uscita certifica che un computer sia sano.

## Architettura

L'Engine è suddiviso in tre progetti senza interfaccia. `Engine.Core` fornisce primitive e ricette dichiarative, `Engine` gestisce i flussi di lavoro condivisi e le evidenze, e `Engine.Windows` registra i provider nativi. L'app desktop, la CLI e i test sono host separati.

| Livello | Responsabilità |
| --- | --- |
| Raccoglitori di capacità | Leggono uno stato delimitato di Windows per un ambito esatto |
| Parser delle evidenze | Convalidano forma, identità, completezza e limiti |
| Interpretazione diagnostica | Convertono le evidenze in stati espliciti del flusso di lavoro |
| Orchestrazione dei flussi di lavoro | Combinano controlli, avanzamento, ricette e giornali |
| Coordinatori delle riparazioni | Ricontrollano l'idoneità, preparano, modificano, verificano e riconciliano |
| Rapporti | Presentano problemi, azioni, risultati e limiti |
| Pacchetti di conoscenze | Risolvono dati di riferimento con versione senza modificare la logica |

I raccoglitori falliscono in modo chiuso quando l'identità è ambigua, l'output è malformato, un limite viene superato o non è possibile ispezionare l'autorità richiesta. Oggi l'Engine è destinato a Windows e non è multipiattaforma. La [Guida per sviluppatori dell'Engine](../engine/index.html) tratta l'API, la CLI e l'integrazione dell'Engine in un altro host .NET.

## Privacy e rete

Diagnosi e analisi dei log vengono eseguite in locale, senza un servizio di IA né un account KillerBench. Giornali ed evidenze conservate restano nelle cartelle `%LOCALAPPDATA%\KillerBench` dell'account locale.

Le azioni che selezioni possono contattare la propria destinazione, come Windows Update, servizi di download del software o il backup della chiave di ripristino in Active Directory o Entra ID. Il controllo degli aggiornamenti all'avvio contatta GitHub, mentre gli aggiornamenti delle conoscenze e il programma di installazione di KillerMCP contattano i rispettivi servizi configurati. Un client di IA che usa [KillerMCP](../mcp.html) può ricevere il risultato di riferimento che ha richiesto.

Log grezzi, file importati ed evidenze esportate possono contenere nomi di computer, account, percorsi o testo di origine sensibile. Esaminali prima di condividerli e non includere mai chiavi di ripristino, credenziali o dati dei clienti in un rapporto pubblico.

## Stato dello sviluppo

KillerBench è in sviluppo e non esiste ancora un download pubblico. La copertura delle riparazioni è ancora in espansione e la convalida nativa delle riparazioni è ancora in corso. Una build riuscita o un controllo simulato non dimostrano il comportamento nativo di una riparazione. Per convalidare ogni modifica nativa su diverse versioni di Windows e stati di errore si usano macchine virtuali Windows usa e getta. Vedi [Come le modifiche restano sicure](safety.html) per il modello di sicurezza completo.

## Glossario

Definizioni in linguaggio semplice dei termini usati in questa pagina, in ordine alfabetico.

| Termine | Significato |
| --- | --- |
| BITS | Servizio trasferimento intelligente in background, il servizio di Windows che scarica aggiornamenti e altri file in background. |
| Cattura | Un file `.kbench` salvato che contiene le evidenze conservate, per poterle riesaminare senza il computer originale. |
| CBS | Component-Based Servicing, il sottosistema di Windows che installa aggiornamenti e funzionalità. Il suo log è `CBS.log`. |
| DISM | Deployment Image Servicing and Management, lo strumento di Windows che controlla e ripara l'archivio componenti. |
| Elevazione | Esecuzione con diritti di amministratore dopo una richiesta di Windows. KillerBench non si eleva mai da solo. |
| Errore chiuso | Quando le evidenze sono ambigue, mancanti o troppo grandi, il risultato è Sconosciuto e nessuna modifica è consentita. |
| Evidenza | Ciò che KillerBench ha effettivamente letto da Windows, conservato con la sua origine così da poter risalire a un rilevamento. |
| Flusso di lavoro | L'esecuzione di uno strumento: i controlli che raccoglie, le ricette che può usare e il rapporto che produce. |
| Giornale | Un record JSON locale scritto prima di una modifica e aggiornato dopo, per poter esaminare in seguito un'azione interrotta. |
| Pacchetto di conoscenze | Un file con versione e convalidato di dati di riferimento, come codici di errore o identificatori hardware. |
| Panther | La cartella in cui il programma di installazione di Windows scrive i log di aggiornamento. |
| Proprietario del criterio | Criteri di gruppo o la gestione dei dispositivi che controllano un'impostazione. KillerBench non tocca le impostazioni controllate. |
| Protezione in sospeso | Il record che indica una modifica come forse inviata a Windows. Blocca una ripetizione finché la riconciliazione non legge l'esito. |
| Ricetta | Una riparazione tipizzata con regole proprie di idoneità, esecuzione, verifica e ripristino. |
| Riconciliazione | Lettura dello stato attuale dopo un'interruzione per capire cosa è successo, senza ripetere la modifica. |
| SFC | Controllo file di sistema, lo strumento di Windows che verifica e ripristina i file di sistema protetti. |
| TPM | Trusted Platform Module, il chip di sicurezza che BitLocker usa per proteggere la chiave dell'unità. |
| Verificato | Il risultato è stato riletto da Windows e corrisponde a ciò che la modifica doveva fare. |
