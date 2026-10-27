# Frigjør diskplass

Bruk denne veiledningen når en stasjon begynner å bli full. KillerBench viser hvor plassen går, forhåndsviser hva hvert oppryddingsmål vil fjerne, og sletter bare etter at du har slått på permanent opprydding.

## Se hvor mye plass som er igjen

1. Åpne **Lagring** og velg **Lagring**.
2. Trykk **Kjør diagnose**. Rapporten markerer hvert volum med stasjonsbokstav som har under 10 % ledig plass, og viser tildeling for skyggekopier, standardkvoter og policy for Lagringsassistent.
3. For krasjdumper og gammelt oppdateringsinnhold kjører du også **Nøkkeltall** under **Diagnostikk** og **Windows Update** under **Oppdateringer**; størrelsen på oppdateringshurtigbufferen vises der.

## Forhåndsvis en opprydding

1. Åpne **Vedlikehold** og velg **Opprydding**.
2. Merk av målene du vil ha, for eksempel **Midlertidige filer for gjeldende bruker**, **Midlertidige Windows-filer**, **Nettleserhurtigbuffere for gjeldende bruker**, **Delivery Optimization-hurtigbuffer** eller **Papirkurven for gjeldende bruker**. Hold pekeren over et mål for å se nøyaktig hvilke mapper det dekker.
3. Angi **Minste alder (dager)** og **Maksimalt antall filer per mål**.
4. Trykk **Kjør diagnose** med **Utfør den valgte permanente oppryddingen etter nye kontroller** slått av. Rapporten viser kandidater og beregnet plass for hvert mål. Ingenting slettes.

## Fjern filene

1. Slå på **Utfør den valgte permanente oppryddingen etter nye kontroller** og trykk **Kjør diagnose**.
2. KillerBench kontrollerer hver kandidat på nytt før den fjernes, hopper over filer som er nye, beskyttet eller i bruk, og registrerer hvert utfall i journalen.

Permanent opprydding går utenom papirkurven og kan ikke angres. Når du fjerner feilrapporter og krasjdumper, forkaster du bevis du kan trenge for å undersøke krasj, så se over dem først.

## Fjern den tidligere Windows-installasjonen

Etter en funksjonsoppgradering kan **Tidligere Windows-installasjon** under **Opprydding** fjerne tilbakerullingskopien gjennom Windows' støttede forespørsel om fjerning av tilbakerulling. Den kjøres alene, krever **Jeg har sikkerhetskopiert filer og innstillinger jeg trenger fra den tidligere installasjonen**, og fjerner for godt muligheten til å gå tilbake til den tidligere versjonen.

## Fra kommandolinjen

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

Se [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) og [previous-windows](cli.html#cmd-previous-windows).

## Relatert

- [Seksjonen Vedlikehold](maintenance.html#cleanup)
- [Seksjonen Lagring](storage.html)
- [Kontroller og reparer en stasjon](check-a-drive.html)
