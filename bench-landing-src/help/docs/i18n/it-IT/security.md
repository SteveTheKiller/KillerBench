# Sicurezza

Sicurezza ispeziona Microsoft Defender, il firewall, i certificati e i servizi di accesso. Mostra anche lo stato del TPM e della sicurezza della piattaforma da cui dipendono BitLocker e Windows Hello.

Guide passo passo: [Crittografare un'unità con BitLocker](encrypt-with-bitlocker.html) e [Rendere Windows più riservato](make-windows-private.html).

## Strumenti di questa sezione

Ispeziona protezione, firewall, certificati e servizi di accesso.

### Microsoft Defender

Ispeziona lo stato della protezione e delle firme di Defender. La riparazione automatica aggiorna le firme solo quando Defender segnala protezione attiva e firme obsolete. Usa le origini delle firme configurate. Nessuna modifica a protezione, esclusioni o criteri; nessuna analisi antimalware.

### Centro sicurezza PC Windows

Esamina in modo delimitato le registrazioni antivirus con lo stato grezzo del provider, la cronologia esatta degli errori del servizio e la disponibilità attuale di Centro sicurezza PC Windows, senza interpretare lo stato della protezione. Un inventario vuoto non è un verdetto sullo stato della protezione, e il numero di registrazioni, lo stato grezzo o gli errori storici non possono autorizzare un ripristino né una modifica di sicurezza. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Il monitoraggio dello stato di sicurezza e le notifiche esistenti possono riprendere. Configurazione dell'antivirus, registrazioni dei provider, impostazioni di protezione, esclusioni, notifiche, criteri e tipo di avvio vengono conservati. Non viene richiesta alcuna analisi, aggiornamento o modifica della protezione. La verifica copre solo lo stato stabile del servizio, non la presenza di un antivirus, lo stato della protezione, l'aggiornamento delle firme, la consegna delle notifiche, la proprietà delle registrazioni né il criterio effettivo.

### Servizi firewall

Legge le impostazioni di attivazione dei profili Dominio, Privato e Pubblico da ActiveStore, poi controlla i servizi Base Filtering Engine e Windows Defender Firewall. Il ripristino facoltativo avvia solo servizi abilitati e arrestati, dopo controlli aggiornati delle dipendenze. Il criterio di filtro esistente può entrare in vigore e interrompere l'accesso di rete o remoto. Tipi di avvio, autorizzazioni dei servizi, regole del firewall, profili e impostazioni IPsec vengono conservati. Nessuna reimpostazione, modifica delle regole o arresto dei servizi. La verifica stabilisce solo che il servizio è in esecuzione; la protezione effettiva e la connettività restano non verificate.

### Scadenza certificati

Ispeziona le date dei certificati e costruisce catene di attendibilità locali usando solo la cache per gli archivi personali del computer e dell'utente corrente, poi restituisce un'unica conclusione operativa delimitata. Revoca, presentazione lato server, associazioni, registrazione, controllo da parte dei criteri, attendibilità remota e utilizzabilità della chiave privata restano non verificati. Non viene modificato alcun certificato, impostazione di attendibilità, registrazione, associazione, chiave privata, applicazione, servizio o criterio.

### Gestione credenziali

Controlla la disponibilità del servizio Gestione credenziali. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le applicazioni esistenti possono riprendere le richieste di credenziali archiviate. Contenuto dell'archivio, password, passkey, certificati, token, registrazioni delle credenziali, autorizzazioni, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. Non viene richiesto alcun contenuto né alcuna operazione sulle credenziali. La verifica copre solo lo stato stabile del servizio, non il recupero delle credenziali, l'autenticazione né l'accesso delle applicazioni.

### Gestione account Web

Esamina la cronologia del broker degli account con payload anonimizzato e la disponibilità esatta di Gestione account Web. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le applicazioni esistenti possono riprendere le richieste di account e token secondo i criteri attuali. Gli eventi storici non possono autorizzare un ripristino né stabilire lo stato attuale degli account. Account, credenziali, token, cookie, registrazioni delle applicazioni, autorizzazioni, stato di accesso, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione su account, credenziali, token, cookie, autorizzazioni o accesso. La verifica copre solo lo stato stabile del servizio, non l'autenticazione, l'emissione di token né l'accesso delle applicazioni.

### Assistente per l'accesso all'account Microsoft

Esamina la cronologia del broker degli account con payload anonimizzato e la disponibilità esatta di Assistente per l'accesso all'account Microsoft. Il ripristino facoltativo avvia solo il servizio esatto abilitato e arrestato, dopo controlli aggiornati delle dipendenze. Le richieste di accesso esistenti con account Microsoft possono riprendere secondo i criteri attuali. Gli eventi storici non possono autorizzare un ripristino né stabilire lo stato attuale degli account. Account, credenziali, token, registrazione del dispositivo, dati delle applicazioni, autorizzazioni, stato di accesso, criteri, autorizzazioni del servizio e tipo di avvio vengono conservati. KillerBench non seleziona alcuna operazione su account, credenziali, token, registrazione, autorizzazioni o accesso. La verifica copre solo lo stato stabile del servizio, non l'autenticazione, la connettività dell'account né l'accesso delle applicazioni.
