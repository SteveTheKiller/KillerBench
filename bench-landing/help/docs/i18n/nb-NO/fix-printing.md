# Løs utskriftsproblemer

Bruk denne veiledningen når jobber blir liggende i køen, en skriver vises som frakoblet, eller feil skriver er standard. KillerBench leser utskriftskøtjenesten, køene, jobbene, driverne og portene først, og lar deg deretter rette én nøyaktig kø eller jobb.

## Kontroller skriverne

1. Åpne **Enheter** og velg **Utskrift**.
2. Trykk **Finn lokale skrivere** og velg køen som oppfører seg feil. For å kontrollere alle køer velger du **Bruk generelle utskriftskontroller**.
3. La **Reparer bekreftede problemer automatisk** være av, og trykk **Kjør diagnose**.
4. Les rapporten. Den viser tilstanden til Utskriftskø, køer satt på pause, frakoblet modus, synlige jobber, drivertilstander, portregistreringer, nylige PrintService-feil og, for nettverksskrivere, ett tilkoblingsforsøk til skriverens tildelte adresse.

En vellykket tilkobling eller en ren tilstandskode beviser ikke at skriveren har papir, toner eller en mekanisme som virker.

## Reparer køen

1. Kjør **Utskrift** på nytt med **Reparer bekreftede problemer automatisk** slått på. Den kan starte den aktiverte utskriftskøtjenesten, gjenoppta en nøyaktig kø på pause som har ventende jobber, og slå av frakoblet modus.
2. Når frakoblet modus slås av, kan ventende jobber bli sluppet løs, så kontroller at de fortsatt skal skrives ut.
3. En kø som noen har satt på pause med vilje og som ikke har noe ventende, forblir på pause.

## Fjern én fastlåst jobb

1. Trykk **Finn blokkerte utskriftsjobber**.
2. Velg jobben som viser Feil eller Blokkert, og merk av **Avbryt bare denne valgte jobben**.
3. Trykk **Kjør diagnose**. Bare den jobben avbrytes, og resultatet leses tilbake.

## Rett standardskriveren

Velg køen som skal være standard, merk av **Reparer en manglende eller motstridende standard med denne køen**, og trykk **Kjør diagnose**. Dette handler bare når standarden mangler eller er i konflikt.

## Fra kommandolinjen

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

Se [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) og [default-printer-set](cli.html#cmd-default-printer-set).

## Relatert

- [Seksjonen Enheter](devices.html#printing)
- [Løs nettverks- og internettproblemer](fix-network.html)
