# Risolvere i problemi di rete e internet

Usa questa guida quando un computer non ha internet, non riesce a raggiungere un server o un nome, o perde continuamente la connessione. KillerBench controlla in ordine scheda, indirizzo, gateway, DNS e firewall, e rinnova un lease o avvia un servizio solo quando evidenze aggiornate dimostrano che è necessario.

## Controllare la connessione

1. Apri **Rete** e scegli **Rete**.
2. Per provare qualcosa di specifico, compila **Ricerca nome host (facoltativa)**, oppure **Destinazione TCP (facoltativa)** e **Porta TCP**. Lasciali vuoti per controllare solo la configurazione locale. Non vengono inviati dati delle applicazioni.
3. Imposta **Timeout per ricerca o connessione (secondi)** se la rete è lenta.
4. Lascia disattivato **Ripara automaticamente problemi confermati** e premi **Esegui diagnosi**.
5. Leggi il rapporto. Distingue tra un indirizzo mancante, un gateway mancante o irraggiungibile, problemi di configurazione DNS, impostazioni proxy, discrepanze nel profilo del firewall e una ricerca o connessione non riuscita, e indica il passaggio successivo per ciascun caso.

Una risposta dal gateway non dimostra l'accesso a internet, e una ricerca non riuscita tramite il resolver configurato non significa che il sito sia irraggiungibile.

## Riparare ciò che il rapporto ha trovato

1. Esegui di nuovo **Rete** con **Ripara automaticamente problemi confermati** attivato. Può avviare i servizi abilitati Client DNS, Client DHCP, client SMB, Riconoscimento posizione di rete, Servizio Elenco reti e Helper IP, e rinnovare le schede DHCP fisiche connesse che non hanno un indirizzo IPv4 utilizzabile.
2. Aspettati una breve interruzione della connessione durante un rinnovo. Gli indirizzi statici e le schede virtuali non vengono mai modificati.
3. Per un solo nome errato nella cache, usa lo svuotamento della cache DNS per quell'host esatto invece di cancellare tutto.

Route, voci di Hosts, server DNS, regole del firewall, provider Winsock e profili di rete vengono sempre conservati.

## Wireless, accesso cablato e proxy

- **Rete wireless** controlla la scheda e Configurazione automatica WLAN, e può avviare il servizio se è arrestato.
- **Autenticazione cablata (802.1X)** fa lo stesso per Configurazione automatica reti cablate sulle reti che richiedono l'accesso sulla porta.
- **Proxy automatico WinHTTP** mostra le impostazioni proxy salvate per il computer e per l'account corrente. KillerBench non scarica mai un URL PAC e non modifica le impostazioni proxy durante la diagnosi.

## Dalla riga di comando

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

Vedi [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) e [dns-cache](cli.html#cmd-dns-cache).

## Correlati

- [Sezione Rete](network.html)
- [Risolvere i problemi di stampa](fix-printing.html)
- [Come le modifiche restano sicure](safety.html)
