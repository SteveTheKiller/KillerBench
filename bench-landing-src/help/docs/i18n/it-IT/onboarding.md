# Configurazione iniziale

Configurazione iniziale prepara un nuovo computer. Esamina le applicazioni e la configurazione che selezioni e dà un nome al computer. Non scarica né installa nulla da sola.

Guide passo passo: [Configurare un nuovo computer](set-up-new-pc.html).

## Controllare un programma di installazione prima di eseguirlo

La riga di comando può verificare il programma di installazione di un produttore prima che tu lo esegua:

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
```

KillerBench considera attendibile un programma di installazione solo se ha una firma Authenticode valida con il nome esatto dell'organizzazione del produttore. Non esegue mai il programma di installazione.

## Strumenti di questa sezione

Prepara un nuovo computer: applicazioni, nome e configurazione.

### Configurazione iniziale

Esamina le applicazioni di provisioning e i gruppi di configurazione selezionati esplicitamente. Le registrazioni attuali di disinstallazione, dei pacchetti di app e di Office Click-to-Run vengono confrontate senza scaricare né eseguire programmi di installazione. Le selezioni di configurazione descrivono le conseguenze e rimandano a flussi di lavoro protetti separati quando sono disponibili. Non avviene alcuna azione di download, installazione, account, aggiornamento, pulizia, criteri, registro, servizi, profili o riavvio.

### Nome computer

Confronta il nome proposto per il computer con l'identità attuale e il ruolo nel dominio. Nessuna ridenominazione, nessuna raccolta di credenziali e nessun riavvio. Disponibilità del nome, autorizzazioni e dipendenze delle applicazioni restano non verificate.
