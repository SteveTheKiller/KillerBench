# Gjør Windows mer privat

Bruk denne veiledningen for å se over og stramme inn personvernet på en bærbar PC. KillerBench leser gjeldende innstillinger, viser hvilke som allerede eies av policy eller enhetsadministrasjon, og endrer bare innstillingene du velger, én om gangen, med nøyaktig angremulighet.

## Se over gjeldende innstillinger

1. Åpne **Personvern**.
2. Merk av områdene du vil se over. Alle områder starter uten avmerking.
3. Trykk **Kjør diagnose**. Rapporten viser gjeldende verdi for denne maskinen og kontoen som kjører KillerBench, og markerer innstillinger som eies av policy.

## Endre en innstilling

1. Velg innstillingen i **Personverninnstilling**, for eksempel kamera, mikrofon, posisjon, diagnosedata, reklame, utklippstavlelogg, skysøk i innhold eller søkehøydepunkter.
2. Velg den nye verdien og merk av **Bruk når jeg velger Kjør diagnose**.
3. Trykk **Kjør diagnose**. KillerBench ser etter en policyeier, registrerer gjeldende verdi, gjør den ene endringen og leser den tilbake.

Innstillinger som eies av gruppepolicy eller enhetsadministrasjon, blir ikke rørt. Endre dem der de administreres.

## Angre en endring

Hver verifisert endring har en journal. Den tilhørende angrehandlingen gjenoppretter nøyaktig forrige verdi, også da etter en policykontroll.

## Rydd i Windows

For forslag, forbrukerinnhold, Recall og KI-funksjoner, rot på oppgavelinjen og Start, Edge og forhåndsinstallert programvare åpner du **Vedlikehold** og velger **Rydd i Windows**. Gjennomgangen er skrivebeskyttet, og ingenting velges automatisk.

## Fra kommandolinjen

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

Se [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) og [debloat-review](cli.html#cmd-debloat-review). En nøkkel som ikke støttes, viser de støttede valgene.

## Relatert

- [Seksjonen Personvern](privacy.html)
- [Konfigurer en ny datamaskin](set-up-new-pc.html)
