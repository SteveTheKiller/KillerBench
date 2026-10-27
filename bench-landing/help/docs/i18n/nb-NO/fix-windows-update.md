# Reparer Windows Update

Bruk denne veiledningen når oppdateringer mislykkes, stopper opp eller installeres på nytt gang på gang. KillerBench leser oppdateringskjeden først, forklarer feilen med bevisene bak den, og starter bare en tjeneste eller reparerer komponentlageret når ferske bevis viser nøyaktig det problemet.

## Før du begynner

- Kjør KillerBench som administrator. Oppdateringstjenestene, komponentlageret og beskyttede systemfiler krever utvidede rettigheter for fullstendige bevis.
- Start på nytt først hvis Windows allerede har en ventende omstart. Mange oppdateringsreparasjoner blokkeres så lenge en omstart venter.
- La datamaskinen være koblet til strøm under vedlikehold.

## Finn ut hvorfor oppdateringer mislykkes

1. Åpne **Oppdateringer** og velg **Windows Update** i verktøysidefeltet.
2. La **Reparer bekreftede problemer automatisk** være av første gang, og trykk **Kjør diagnose**.
3. Les **Lesbar rapport**. Den viser stoppede oppdateringstjenester, ventende omstarter, lite ledig plass, skade i komponentlageret og de nøyaktige feilkodene Windows registrerte, med et anbefalt neste trinn.
4. Hvis rapporten nevner en feilkode, kjøres de tilhørende oppfølgingskontrollene automatisk. Du kan også slå opp en kode selv med `killerbench-cli update-code 0x80070002`.

## Følg med på en oppdatering som ser ut til å henge

Velg **Overvåking av vedlikeholdsaktivitet** under **Oppdateringer** og trykk **Kjør diagnose**. Den tar gjentatte øyeblikksbilder av oppdaterings-, Setup- og Installer-aktivitet, overføringstotaler og omstartsindikatorer. Aktivitet vises bare som bevis; den hevder aldri på egen hånd at noe henger eller er feil.

## Reparer det rapporten fant

1. Kjør **Windows Update** på nytt med **Reparer bekreftede problemer automatisk** slått på. Den kan starte aktiverte oppdateringstjenester som er bekreftet stoppet, og kjører DISM eller SFC bare når livekontrollen bekrefter skade.
2. For én enkelt tjeneste velger du i stedet det tilhørende verktøyet, for eksempel **BITS-tjeneste**, **Kryptografiske tjenester**, **Installasjonsprogram for Windows-moduler** eller **Update Orchestrator-tjeneste**. Hvert av dem tilbyr gjenoppretting bare for nøyaktig den tjenesten.
3. Når rapporten ber deg starte på nytt, gjør du det før du kjører noe annet. KillerBench registrerer utfall som krever omstart, og utsetter videre arbeid til omstarten er gjort.

Hver reparasjon journalføres og leses tilbake. Se [Slik holdes endringer trygge](safety.html) for hele rekkefølgen.

## Installer en bestemt oppdatering

1. Velg **Tilgjengelige oppdateringer** under **Oppdateringer** og trykk **Kjør diagnose** for å finne oppdateringer fra den konfigurerte kilden.
2. Velg oppdateringene du vil ha, og trykk **Forhåndsvis valgte**. Forhåndsvisningen viser nøyaktig hva som vil bli installert, og eventuelle krav om omstart.
3. Les forhåndsvisningen, merk av for bekreftelsen, og trykk deretter **Installer**. Avmerkingsboksen tømmes etter hver kjøring, og KillerBench velger eller godkjenner aldri en oppdatering for deg.

## Fra kommandolinjen

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

Se [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) og [update-plan](cli.html#cmd-update-plan). Hvis en installasjon ble avbrutt, kjører du [update-recover](cli.html#cmd-update-recover); den kontrollerer utfallet og prøver aldri oppdateringen på nytt.

## Relatert

- [Seksjonen Oppdateringer](updates.html)
- [Reparer systemfiler](repair-system-files.html)
- [Feilsøking](troubleshooting.html)
