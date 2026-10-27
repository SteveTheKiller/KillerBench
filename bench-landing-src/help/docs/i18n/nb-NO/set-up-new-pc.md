# Konfigurer en ny datamaskin

Bruk denne veiledningen når du klargjør en ny eller nylig tilbakestilt PC. KillerBench ser over datamaskinen, programmene du planlegger å installere og navnet du vil gi den, og viser deg deretter til de beskyttede verktøyene som gjør hver endring.

## Begynn med et utgangspunkt

1. Kjør **Full systemskanning** fra **Diagnostikk** og ta vare på rapporten som et bilde av datamaskinens starttilstand.
2. Installer ventende oppdateringer. Se [Reparer Windows Update](fix-windows-update.html) hvis noen mislykkes.

## Se over programmer og konfigurasjon

1. Åpne **Klargjøring** og velg **Klargjøring**.
2. Velg programmene og konfigurasjonsgruppene du planlegger å sette opp, og trykk **Kjør diagnose**. Gjennomgangen sammenligner dem med det som allerede er installert. KillerBench laster ikke ned og kjører ikke installasjonsprogrammer.
3. Før du kjører en leverandørs installasjonsprogram, verifiserer du det fra kommandolinjen. KillerBench stoler bare på det når det har en gyldig signatur med leverandørens nøyaktige organisasjonsnavn.

## Gi datamaskinen et navn

1. Velg **Datamaskinnavn**, skriv inn navnet og trykk **Kjør diagnose** for å sammenligne det med gjeldende identitet og domenerolle.
2. På en arbeidsgruppedatamaskin merker du av **Gi denne arbeidsgruppe-PC-en nytt navn etter nye kontroller** for å endre navnet. En omstart kreves for å fullføre.

## Beskytt og rydd

- [Krypter en stasjon med BitLocker](encrypt-with-bitlocker.html) og ta sikkerhetskopi av gjenopprettingsnøkkelen.
- [Gjør Windows mer privat](make-windows-private.html) og se over **Rydd i Windows** under **Vedlikehold**.
- Opprett et gjenopprettingspunkt med **Klargjør ett gjenopprettingspunkt** når alt er på plass.

## Fra kommandolinjen

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

Se [workflow](cli.html#cmd-workflow) og [restore-point](cli.html#cmd-restore-point).

## Relatert

- [Seksjonen Klargjøring](onboarding.html)
- [Din første diagnose](first-run.html)
