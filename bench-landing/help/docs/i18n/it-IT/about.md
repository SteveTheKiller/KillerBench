# Informazioni su KillerBench

KillerBench è un ambiente di lavoro per la diagnosi, la riparazione e la manutenzione di Windows creato da Steve the Killer, parte della [famiglia Killer Tools](https://killertools.net). La versione 1.0.0 non è ancora stata rilasciata. L'app desktop, l'Engine e la CLI condividono le evidenze diagnostiche e i flussi di riparazione supportati.

## Disponibilità e ambito

Non è ancora disponibile alcun download pubblico di KillerBench né alcun pacchetto dell'Engine. [Download e configurazione](setup.html) descrive la destinazione Windows x64 attuale e i requisiti del Desktop Runtime. La convalida nativa delle riparazioni è ancora in corso; la presenza di uno strumento non dimostra che ogni guasto, interruzione o configurazione di Windows sia stato testato.

## Avvisi su sorgente e distribuzione

KillerBench è distribuito con licenza **GPLv3**. La sua licenza di distribuzione è la [GNU General Public License versione 3](../license.txt). La licenza descrive le autorizzazioni e le condizioni per copiare, modificare e distribuire l'opera coperta.

Il [repository pubblico di KillerBench](https://github.com/SteveTheKiller/KillerBench) fornisce per ora informazioni sul progetto. L'implementazione completa non è disponibile pubblicamente lì. Queste guide per sviluppatori descrivono una copia autorizzata del sorgente; non forniscono un download pubblico del sorgente.

Prima di distribuire un binario, chi lo pubblica deve fornire il Codice sorgente corrispondente con un metodo consentito dalla [sezione 6 della GPLv3](https://www.gnu.org/licenses/gpl-3.0.en.html#section6), incluso il sorgente e il materiale di build richiesti dalla licenza. Le istruzioni per la fornitura del sorgente del primo pacchetto pubblico e gli avvisi di terze parti inclusi sono ancora in sospeso. La GPLv3 non concede l'autorizzazione a ridistribuire caratteri di terze parti; le autorizzazioni attuali dei caratteri devono ancora essere verificate. Esamina la licenza, le istruzioni sul sorgente e gli avvisi del pacchetto pubblicato prima di ridistribuirlo.

## Privacy e servizi esterni

Diagnosi e analisi dei log vengono eseguite in locale, senza un servizio di IA né un account KillerBench. I giornali e le evidenze conservate usano le cartelle `%LOCALAPPDATA%\KillerBench` dell'account locale. Le azioni selezionate possono contattare Windows Update, servizi di download del software, directory o altre destinazioni indicate. Il controllo degli aggiornamenti all'avvio contatta GitHub, mentre gli aggiornamenti delle conoscenze e la pagina di installazione di MCP contattano i rispettivi servizi configurati. Un client di IA che usa MCP può ricevere il risultato di riferimento restituito.

Questo sito memorizza le scelte di tema e colore di accento nella memoria locale del browser. Le sue guide non eseguono alcuna diagnosi sul tuo computer. Log grezzi, file importati ed evidenze esportate possono contenere nomi di computer, account, percorsi o testo di origine sensibile. Esaminali prima di condividerli; non includere chiavi di ripristino, credenziali o dati dei clienti in un rapporto pubblico.

## Supporto

Le informazioni sul progetto sono disponibili nel repository pubblico. Per il supporto, usa i contatti pubblicati sul [sito dell'autore](https://thekiller.net); indica la versione dell'app, la build di Windows, lo strumento esatto, il risultato atteso e quello osservato. Esamina e anonimizza le evidenze prima di inviarle e usa un canale privato approvato per le informazioni dei clienti. Conserva i giornali quando l'esito di una riparazione è incerto.

## Guide correlate

- [Come le modifiche restano sicure](safety.html)
- [Rapporti ed evidenze](reports.html)
- [Guida per sviluppatori dell'Engine](../engine/)
- [KillerMCP](../mcp.html)
