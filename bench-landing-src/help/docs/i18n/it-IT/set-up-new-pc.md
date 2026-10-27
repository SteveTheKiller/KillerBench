# Configurare un nuovo computer

Usa questa guida quando prepari un PC nuovo o appena reimpostato. KillerBench esamina il computer, le applicazioni che intendi installare e il nome che vuoi dargli, poi ti indica gli strumenti protetti che eseguono ciascuna modifica.

## Partire da uno stato di riferimento

1. Esegui **Analisi completa del sistema** da **Diagnostica** e conserva il rapporto come traccia dello stato iniziale del computer.
2. Installa gli aggiornamenti in sospeso. Vedi [Riparare Windows Update](fix-windows-update.html) se qualcuno non riesce.

## Esaminare applicazioni e configurazione

1. Apri **Configurazione iniziale** e scegli **Configurazione iniziale**.
2. Seleziona le applicazioni e i gruppi di configurazione che intendi predisporre e premi **Esegui diagnosi**. L'esame li confronta con ciò che è già installato. KillerBench non scarica né esegue programmi di installazione.
3. Prima di eseguire il programma di installazione di un produttore, verificalo dalla riga di comando. KillerBench lo considera attendibile solo se ha una firma valida con il nome esatto dell'organizzazione del produttore.

## Dare un nome al computer

1. Scegli **Nome computer**, inserisci il nome e premi **Esegui diagnosi** per confrontarlo con l'identità attuale e il ruolo nel dominio.
2. Su un computer di un gruppo di lavoro, seleziona **Rinomina questo PC gruppo di lavoro dopo controlli aggiornati** per rinominarlo. Per completare serve un riavvio.

## Proteggerlo e metterlo in ordine

- [Crittografa un'unità con BitLocker](encrypt-with-bitlocker.html) ed esegui il backup della chiave di ripristino.
- [Rendi Windows più riservato](make-windows-private.html) ed esamina **Alleggerisci Windows** in **Manutenzione**.
- Crea un punto di ripristino con **Prepara un punto di ripristino** quando tutto è a posto.

## Dalla riga di comando

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

Vedi [workflow](cli.html#cmd-workflow) e [restore-point](cli.html#cmd-restore-point).

## Correlati

- [Sezione Configurazione iniziale](onboarding.html)
- [La tua prima diagnosi](first-run.html)
