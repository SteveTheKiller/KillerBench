# Hjelp for KillerBench

KillerBench er en arbeidsbenk for diagnose og reparasjon av Windows, laget for teknikere. Den kontrollerer datamaskinen først, forklarer hva den fant med bevisene bak hvert funn, og endrer bare Windows når du velger en bestemt reparasjon og bekrefter den.

KillerBench er under utvikling. Det finnes ingen offentlig nedlasting ennå. Disse sidene beskriver gjeldende versjon.

## Hva den gjør

- Kjører skrivebeskyttet diagnostikk av Windows Update, enheter, lagring, nettverk, utskrift, sikkerhet, apper og profiler, og skriver deretter en lesbar rapport.
- Tilbyr beskyttede reparasjoner som bare kjøres når ferske bevis viser det nøyaktige problemet, og verifiserer resultatet etterpå.
- Dekker oppsett- og herdingsarbeid som BitLocker, personverninnstillinger, rydding i Windows og klargjøring av en ny datamaskin.
- Kjører diagnostikk og rapportanalyse lokalt uten KI og uten KillerBench-konto. Utvalgte handlinger, som nedlastinger og sikkerhetskopiering av gjenopprettingsnøkkelen til Entra ID, kan kontakte eksterne tjenester.
- Bruker det aktive apptemaet i konsollen, rapportene, bekreftelsene og dialogboksene Åpne, Lagre og Mappe.

## Hvor du begynner

- [Nedlasting og oppsett](setup.html)
- [Om KillerBench](about.html)

- [Din første diagnose](first-run.html) går gjennom en full systemskanning.
- [Arbeidsområdet](workspace.html) forklarer seksjonene, panelene og hurtigtastene.
- [Slik holdes endringer trygge](safety.html) forklarer hva KillerBench kontrollerer før og etter hver endring.
- De [trinnvise veiledningene](fix-windows-update.html) går gjennom vanlige oppgaver.
- Sidene under **Seksjoner** beskriver hvert verktøy, gruppert på samme måte som i appen.
- [Kommandolinje](cli.html) viser alle `killerbench-cli`-kommandoer.
- [Feilsøking](troubleshooting.html) dekker blokkerte handlinger, omstarter og avbrutte forespørsler.

Utviklere som vil bygge diagnostikken inn i egne verktøy, kan lese [Utviklerveiledning for Engine](../engine/index.html).
