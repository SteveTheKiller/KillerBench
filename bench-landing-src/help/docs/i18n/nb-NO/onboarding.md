# Klargjøring

Klargjøring forbereder en ny datamaskin. Den ser over programmene og konfigurasjonen du velger, og gir datamaskinen navn. Den laster ikke ned eller installerer noe på egen hånd.

Trinnvise veiledninger: [Konfigurer en ny datamaskin](set-up-new-pc.html).

## Kontroller et installasjonsprogram før du kjører det

Kommandolinjen kan verifisere en leverandørs installasjonsprogram før du kjører det:

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
```

KillerBench stoler bare på et installasjonsprogram når det har en gyldig Authenticode-signatur med leverandørens nøyaktige organisasjonsnavn. Den kjører aldri installasjonsprogrammet.

## Verktøy i denne seksjonen

Klargjør en ny datamaskin: programmer, navn og konfigurasjon.

### Klargjøring

Ser over eksplisitt valgte klargjøringsprogrammer og konfigurasjonsgrupper. Gjeldende avinstallerings-, apppakke- og Office Klikk og bruk-registreringer sammenstilles uten at installasjonsprogrammer lastes ned eller kjøres. Konfigurasjonsvalgene beskriver konsekvensene og viser videre til egne beskyttede arbeidsflyter når de finnes. Ingen handling for nedlasting, installasjon, konto, oppdatering, opprydding, policy, register, tjeneste, profil eller omstart utføres.

### Datamaskinnavn

Sammenligner det foreslåtte datamaskinnavnet med gjeldende identitet og domenerolle. Ingen navneendring, ingen innsamling av påloggingsinformasjon og ingen omstart. Om navnet er ledig, tillatelser og avhengigheter i programmer forblir uverifisert.
