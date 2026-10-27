# Feilsøking

## En handling sier at den er blokkert

Et blokkert resultat betyr at den forespurte handlingen ikke kunne fortsette. Les meldingen og journalen for å se om den stoppet før utsending eller etter et tidligere trinn. Ikke anta at en delvis fullført operasjon ble rullet tilbake. De vanligste forutsetningsårsakene står nedenfor.

- **Administratortilgang kreves.** Lukk KillerBench og start den på nytt med **Kjør som administrator**.
- **Ingen omstart kan være ventende.** Windows har en ventende omstart. Start datamaskinen på nytt, og kjør handlingen igjen.
- **En administrert policy finnes.** Gruppepolicy eller enhetsadministrasjon eier innstillingen. Endre den der i stedet.
- **Bevisene har endret seg.** Noe endret seg mellom første kontroll og siste kontroll. Kjør verktøyet på nytt for å lese den nye tilstanden.

## En tidligere forespørsel må avstemmes

KillerBench fant en ventende oppføring fra en avbrutt forespørsel. Kjør samme handling på nytt. Den kontrollerer hva som skjedde, og melder det faktiske utfallet uten å gjenta forespørselen. På kommandolinjen bruker du den tilhørende `-recover`-kommandoen, for eksempel `killerbench-cli bitlocker-auto-unlock-recover`.

## En kontroll viser Ufullstendige eller Ukjent

Bevisene kunne ikke leses, ofte fordi KillerBench ikke kjørte som administrator eller en Windows-tjeneste var utilgjengelig. KillerBench melder dette ærlig i stedet for å anta at datamaskinen er sunn.

## Hvor filene ligger

Journaler, ventende oppføringer og lagrede bevis ligger under `%LOCALAPPDATA%\KillerBench`. Hver beskyttet handling har sin egen mappe. Ta vare på dem når du trenger en oversikt over hva som ble endret.

## Rapporter et problem

KillerBench er under utvikling. Legg ved den eksporterte rapporten og aktivitetsloggen når du rapporterer et problem, og fjern alt du ikke vil dele.
