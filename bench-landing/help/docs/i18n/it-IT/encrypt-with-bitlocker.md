# Crittografare un'unità con BitLocker

Usa questa guida per attivare BitLocker, eseguire il backup della chiave di ripristino e confermare che l'unità sia protetta. KillerBench salva e verifica la password di ripristino prima di avviare la crittografia, e non mostra né registra mai una password di ripristino.

## Prima di iniziare

- Esegui KillerBench come amministratore.
- L'unità di Windows richiede una protezione TPM e una protezione con password di ripristino. Un'unità dati richiede solo una password di ripristino.
- Decidi dove salvare il backup della chiave di ripristino: un file al di fuori dell'unità che stai crittografando e, facoltativamente, Active Directory o Microsoft Entra ID.

## Controllare l'unità

1. Apri **BitLocker** e scegli l'unità in **Volume**.
2. Lascia l'operazione su **Ispeziona** e premi **Esegui diagnosi**. Il rapporto mostra lo stato della crittografia, lo stato della protezione e le identità delle protezioni.
3. Se il TPM è presente ma non è pronto, preparalo prima da **Sicurezza**. KillerBench non cancella mai un TPM.

## Aggiungere le protezioni necessarie

1. Apri **Altre azioni BitLocker**.
2. Scegli **Aggiungi una protezione TPM** o **Aggiungi una password di ripristino**, premi **Anteprima**, seleziona **Applica questa modifica all'unità selezionata quando premo Esegui azione**, poi premi **Esegui azione**.
3. Esegui subito il backup della nuova password di ripristino. KillerBench non la mostra.

## Eseguire il backup della chiave di ripristino e crittografare

1. Premi **Trova protettori di ripristino** e seleziona la protezione con password di ripristino.
2. Inserisci un percorso completo per il file di backup, al di fuori dell'unità che stai crittografando.
3. Scegli **Crittografa**, scegli il metodo di crittografia e seleziona **Esegui il backup della chiave di ripristino selezionata, poi avvia la crittografia quando seleziono Esegui diagnosi**.
4. Premi **Esegui diagnosi**. KillerBench scrive il backup, lo verifica e poi avvia la crittografia una sola volta.
5. La crittografia prosegue in background. Esegui **Ispeziona** in seguito per confermare che l'unità risulti completamente crittografata con la protezione attiva.

## Backup in Active Directory o Entra ID

In **Altre azioni BitLocker**, scegli il backup nella directory per la protezione selezionata. KillerBench segnala che Windows ha accettato la richiesta ma non può leggere la copia nella directory, quindi confermala in Active Directory o nell'interfaccia di amministrazione di Entra.

## Unità dati e sblocco automatico

Dopo aver crittografato un'unità dati, **Attiva lo sblocco automatico** la fa sbloccare all'avvio di Windows. L'unità di Windows deve essere già completamente crittografata con la protezione attiva.

## Dalla riga di comando

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

Vedi [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) e [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Ognuno ha un comando `-recover` corrispondente per una richiesta interrotta.

## Correlati

- [Sezione BitLocker](bitlocker.html)
- [Sezione Sicurezza](security.html)
- [Come le modifiche restano sicure](safety.html)
