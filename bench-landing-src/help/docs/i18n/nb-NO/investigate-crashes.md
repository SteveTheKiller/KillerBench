# Undersøk krasj og blå skjermer

Bruk denne veiledningen når Windows starter på nytt uventet, viser en stoppfeil, eller et program krasjer gang på gang. KillerBench samler bevis om krasj, avslutninger, maskinvarefeil og pålitelighet i én rapport. Den beskriver det som ble registrert, og gjetter aldri på en grunnårsak.

## Kontroller datamaskinen

1. Åpne **Diagnostikk** og velg **Nøkkeltall**.
2. Trykk **Kjør diagnose**. Rapporten kombinerer minne, disktilstand, enhetsproblemer, historikk fra Pålitelighetsovervåking, maskinvarefeiloppføringer, krasjrapporter, uventede avslutninger og resultater fra Windows Minnediagnostikk.
3. Les konklusjonen og bevisene under den. Historiske krasj forklarer hva som skjedde; de beviser ikke at feilen fortsatt finnes.

## Snevre det inn

- **Maskinvarefeilhistorikk** leser WHEA- og minnediagnoseoppføringer sammen med enhetsproblemer og disktilstand.
- **Pålitelighetshistorikk** grupperer programkrasj, heng og uventede avslutninger fra de siste 14 dagene.
- **Programfeil** fokuserer på programmer som krasjer.
- **Historikk for driverinstallasjon** viser mislykkede driverinstallasjoner koblet til enheter som finnes nå.
- **Enhetsproblemer** viser enheter med feilkoder i Enhetsbehandling.

## Les krasjdumper

KillerBench viser dumpkonfigurasjon og metadata for dumpfiler, og kan analysere strukturen i minidumper for unntaket og modulnavnene. Innholdet i dumpene åpnes aldri i skrivebordsappen. Når en kunnskapspakke for feilkontroll er lastet inn, slås stoppkoder opp til sin dokumenterte betydning.

Ta vare på dumpene til du er ferdig med undersøkelsen. **Opprydding** kan fjerne dem, men da forkastes bevisene.

## Fra kommandolinjen

```powershell
killerbench-cli vitals-check
killerbench-cli crash-config
killerbench-cli crash-dumps
killerbench-cli crash-review
killerbench-cli device-code 43
```

Se [vitals-check](cli.html#cmd-vitals-check), [crash-review](cli.html#cmd-crash-review) og [device-code](cli.html#cmd-device-code).

## Relatert

- [Seksjonen Diagnostikk](diagnostics.html#vitals)
- [Rapporter og bevis](reports.html)
