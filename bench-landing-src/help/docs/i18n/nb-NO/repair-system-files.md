# Reparer systemfiler

Bruk denne veiledningen når Windows-komponenter er skadet: funksjoner lar seg ikke installere, oppdateringer melder komponentfeil, eller systemverktøy krasjer. KillerBench kjører DISM og SFC bare etter at en livekontroll har bekreftet skade, og verifiserer resultatet etterpå.

## Før du begynner

- Kjør KillerBench som administrator.
- Fullfør eventuelle ventende omstarter først.
- Hvis maskinen er administrert, kontrollerer du om en policy peker DISM mot en bestemt reparasjonskilde. KillerBench melder den policyen og endrer den ikke.

## Se etter skade

1. Åpne **Reparasjon** og velg **Systemfiler**.
2. La **Reparer bekreftede problemer automatisk** være av, og trykk **Kjør diagnose**.
3. Les rapporten. Den lager en oversikt over tilstanden til vedlikeholdspakker, valgfrie funksjoner og funksjonaliteter, kontrollerer forutsetningene og skanner beskyttede systemfiler. Et rent resultat betyr at ingen reparasjon tilbys.

## Reparer bekreftet skade

1. Slå på **Reparer bekreftede problemer automatisk** og trykk **Kjør reparasjon**. KillerBench velger DISM eller SFC bare når livekontrollen bekrefter skade.
2. Vent på verifiseringen. Rapporten viser **Verifisert** bare når oppfølgingskontrollen leser tilbake en reparert tilstand.
3. Hvis rapporten sier at lageret ikke kan gjenopprettes eller at reparasjonskilden er utilgjengelig, stopper du der. Å gjenta reparasjonen endrer ikke utfallet.

En avbrutt DISM- eller SFC-kjøring avstemmes ved neste oppstart uten at vedlikeholdskommandoen gjentas.

## Rydd i komponentlageret

Velg **Opprydding i komponentlageret** under **Oppdateringer**. Den spør først Windows om opprydding anbefales. Valgfritt vedlikehold fjerner erstattede komponenter og analyserer på nytt. Den bruker aldri ResetBase og sletter aldri WinSxS-innhold manuelt.

## Fra kommandolinjen

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

Se [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) og [servicing-logs](cli.html#cmd-servicing-logs). `repair` viser en forhåndsvisning som standard og handler bare med `--execute`.

## Relatert

- [Reparer Windows Update](fix-windows-update.html)
- [Seksjonen Reparasjon](repair.html#system-files)
- [Slik holdes endringer trygge](safety.html)
