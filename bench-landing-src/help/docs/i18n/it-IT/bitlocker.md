# BitLocker

BitLocker ispeziona la crittografia delle unità, le protezioni e le informazioni di ripristino, poi ti aiuta a eseguire backup, proteggere o modificare un'unità. Funziona solo su questo computer.

Guide passo passo: [Crittografare un'unità con BitLocker](encrypt-with-bitlocker.html).

## Usare lo strumento BitLocker

Scegli la lettera di unità e un'**Operazione**, poi premi **Esegui diagnosi**.

- **Ispeziona** legge lo stato della crittografia, lo stato della protezione e le identità delle protezioni. È l'opzione predefinita e non modifica nulla.
- **Backup ripristino** scrive la password di ripristino selezionata in un nuovo file al di fuori dell'unità protetta e la verifica. Premi prima **Trova protettori di ripristino**, scegli la protezione e poi conferma il backup.
- **Crittografa** esegue il backup della password di ripristino selezionata, verifica il backup, poi avvia la crittografia una sola volta con il metodo scelto. L'unità di Windows richiede una protezione TPM e una password di ripristino. Un'unità dati richiede solo una password di ripristino.
- **Riprendi protezione** riattiva la protezione su un'unità completamente crittografata in cui era stata sospesa.

Ognuna richiede la propria casella di conferma, e KillerBench ricontrolla l'unità subito prima di agire.

## Altre azioni BitLocker

Apri **Altre azioni BitLocker** per le modifiche che restano fuori da **Esegui diagnosi**. Scegli un'azione, premi **Anteprima** per leggere esattamente cosa richiede, poi conferma e premi **Esegui azione**.

- **Attiva lo sblocco automatico** fa sbloccare un'unità dati crittografata all'avvio di Windows. L'unità di Windows deve essere completamente crittografata con la protezione attiva.
- **Aggiungi una protezione TPM** o **Aggiungi una password di ripristino** crea una nuova protezione. KillerBench non mostra né registra mai una nuova password di ripristino, quindi esegui subito il backup.
- **Salva la password di ripristino in Active Directory** o **Microsoft Entra ID** invia la protezione selezionata sopra. KillerBench segnala che Windows ha accettato la richiesta. Non può leggere la copia nella directory, quindi confermala lì.
- **Decrittografa questa unità** avvia la decrittografia, per esempio prima di crittografare di nuovo con un metodo obbligatorio. Richiede una seconda conferma perché in seguito l'unità non è protetta, e viene rifiutata sull'unità di Windows finché altre unità si sbloccano automaticamente tramite essa.

## TPM e FIPS

Se il TPM è presente ma non è pronto, la sezione Sicurezza può prepararlo senza cancellarlo. KillerBench non cancella mai un TPM, perché così si distruggono le chiavi conservate da BitLocker e Windows Hello. Il passaggio del criterio degli algoritmi FIPS è disponibile come riparazione esplicita e reversibile per le organizzazioni che necessitano di PIN BitLocker approvati FIPS.

## Strumenti di questa sezione

Ispeziona crittografia, protezioni e informazioni di ripristino, poi esegui il backup o proteggi l'unità.

### BitLocker

Mostra in anteprima le opzioni di BitLocker e ispeziona il volume selezionato. Questo flusso di lavoro non esegue alcuna crittografia né alcun backup di ripristino.
