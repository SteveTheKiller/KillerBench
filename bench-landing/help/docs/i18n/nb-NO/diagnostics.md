# Diagnostikk

Diagnostikk er der hvert besøk bør begynne. Hvert verktøy leser maskinen og skriver en rapport. Ingenting her endrer Windows. Standardverktøyet, den fulle systemskanningen, kjører et bredt sett med skrivebeskyttede kontroller og rangerer det som trenger oppmerksomhet.

Trinnvise veiledninger: [Undersøk krasj og blå skjermer](investigate-crashes.html) og [Reparer systemfiler](repair-system-files.html).

## Verktøy i denne seksjonen

Kontroller maskinen først, og les deretter i rapporten hva som trenger oppmerksomhet og hva som er neste trygge trinn.

### Full systemskanning

Undersøker 49 diagnoseområder og vurderer deretter alle installerte oppskrifter for tjenestegjenoppretting sammen med oppskriftene for vedlikehold, DHCP, utskrift, Defender, tid og WMI. Kontrollene kan ta flere minutter. Automatisk reparasjon er valgfritt og krever hver oppskrifts egne gjeldende bevis, maskinforutsetninger og avhengighetskontroller. Kvalifiserte reparasjoner kan fornye manglende DHCP-leaser, slippe utskriftsjobber i kø, synkronisere klokken på nytt, oppdatere Defender-signaturer, gjenopprette kvalifiserte tjenester og reparere bekreftet skade i Windows. Stoppede automatiske tjenester kan startes etter at oppstartsvinduet og avhengighetene er verifisert. En stoppet tjeneste med manuell oppstart forblir uendret når inaktiv tilstand er det eneste beviset; mer spesifikke arbeidsflyter kan kreve den bare når gjeldende bevis for funksjonen støtter oppstarten. Oppstartstyper, oppgavedefinisjoner, rapporter, dumper, abonnementer, påloggingsinformasjon, kanalinnstillinger, brannmurpolicy og administrerte innstillinger bevares. Historiske øyeblikksbilder og øyeblikksbilder av sikkerhetskopier og overføringer er skrivebeskyttet. Innholdet i dumper åpnes ikke. Kontroll av dirty-biten planlegger ingen reparasjon eller omstart. Denne arbeidsflyten installerer ikke oppdateringer, endrer ikke drivere, sletter ikke filer og utfører ingen fullstendig maskinvaretest.

### Nøkkeltall

Registrerer tilgjengelig minne, disktilstand rapportert av leverandøren og BitLocker-beskyttelsesstatus, og sammenholder deretter gjeldende nøkkeltall med enhetsproblemer, pålitelighetshistorikk, maskinvarefeiloppføringer, krasjbevis, uventede avslutninger og resultater fra Windows Minnediagnostikk. Omstartsindikatorer, ledig plass på systemdisken, utvidede rettigheter og konfigurasjonen av oppdateringstjenesten forblir synlige. Gjenopprettingspassord samles ikke inn. Ingen reparasjoner kjøres i denne arbeidsflyten. Det samlede resultatet er ingen fullstendig test av maskinvare, programvare eller stabilitet.

### Pålitelighetshistorikk

Leser oppføringer fra Windows Pålitelighetsovervåking for de siste 14 dagene sammen med nylige Program-feil og hendelser for uventet avslutning, og gir én avgrenset historisk konklusjon. Historiske oppføringer fastslår ikke gjeldende tilstand eller grunnårsak. Ingen reparasjon velges, og ingen programmer, tjenester eller innstillinger endres.

### Oppstartsytelse

Leser metadata for ytelseshendelser ved oppstart og avslutning av Windows for de siste 14 dagene, beholder bare leverandøridentitet og avgrensede numeriske varighetsfelt, og sammenholder dem med støttede indikatorer for ventende omstart. Stier, prosessnavn, kontoer og annen hendelsesnyttelast fjernes før bevis lagres. Historiske oppføringer om forringelse identifiserer ingen gjeldende flaskehals og gir ikke grunnlag for endringer i oppstart, tjenester, programmer, drivere, oppgaver, strøm, omstart eller policy.

### Maskinvarefeilhistorikk

Leser historikk fra Windows Hardware Error Architecture og minnediagnostikk for de siste 14 dagene, gjeldende Plug and Play-enhetsproblemer, tilstanden til fysiske disker og leverandørens pålitelighetstellere, og gir én avgrenset maskinvarekonklusjon. Oppføringene må sammenholdes med nyttelasten, fastvarediagnostikk og leverandørens maskinvaretester. Ingen defekt komponent eller grunnårsak utledes, ingen reparasjon velges, og ingen maskinvare, fastvare, driver, disk, utvalg, volum, fil, tjeneste, strøm, omstart eller innstilling endres.

### Historikk for driverinstallasjon

Leser den bevarte enden av Windows SetupAPI-historikken for enhetsinstallasjon, sammenholder mislykkede seksjoner med nøyaktige gjeldende Plug and Play-identiteter og gir én avgrenset konklusjon. Historiske loggseksjoner beviser ikke at en enhet fortsatt finnes eller at en feil er aktuell. Ingen driver installeres, fjernes, rulles tilbake eller oppdateres.

### Systemgjenopprettingspunkter

Viser opptil 256 tilgjengelige systemgjenopprettingspunkter, ser over eksplisitt kvalifisering for oppretting og gir én avgrenset konklusjon om beredskap. Oversikten beviser ikke at beskyttelsen er slått på for alle volumer eller at en gjenoppretting vil lykkes. Ingen gjenopprettingspunkter opprettes, slettes eller tas i bruk under diagnosen.

### Windows-gjenopprettingsmiljø

Leser gjeldende konfigurasjon av Windows-gjenopprettingsmiljø med REAgentC, sammenholder en aktivert lokal plassering med nøyaktig gjeldende disk og partisjon, og gir én avgrenset konklusjon om beredskap. Valgfri automatisk reparasjon kan aktivere en eksisterende deaktivert konfigurasjon og deretter verifisere den rapporterte tilstanden. Gjenopprettingsbildet lastes ikke ned, flyttes eller erstattes, ingen gjenopprettingsoppstart settes i gang, og Windows startes ikke på nytt. Aktivert status og at partisjonen finnes, beviser ikke at gjenopprettingsbildet kan starte eller reparere denne PC-en.

### Windows-aktivering

Leser gjeldende lisensstatus for Windows uten å samle inn hele eller deler av produktnøkler, beholder anonymiserte metadata for advarsler, feil og kritiske hendelser fra Programvarebeskyttelse for de siste sju dagene, og gir én avgrenset driftskonklusjon. Hendelsesnyttelast fjernes før bevis lagres. Historiske oppføringer forblir kontekst når gjeldende lisensiering melder Lisensiert. Ingen handling for aktivering, produktnøkkel, token, lisens, konto, tjeneste eller policy velges. Leverandørstatus og hendelseshistorikk beviser ikke abonnementsrett, om aktiveringsserveren kan nås, tokenlagerets integritet, fremtidig gyldighet eller grunnårsak.

### Windows-tjenester

Lager en oversikt over installerte tjenestenavn, visningsnavn, tilstander, oppstartsmoduser og personvernbegrensede typer gjenopprettingshandlinger uten å samle inn kontoer, stier til kjørbare filer, gjenopprettingskommandoer eller omstartsmeldinger. Ser over sammenhengen i oppstartsmoduser, identiteter og sykluser i direkte avhengigheter, gjentar nøyaktige ventende tilstander, undersøker støttede tjenester og avhengighetene til stoppede tjenester, og sammenholder deretter nylige feil fra Tjenestekontrollbehandling bare når ett nøyaktig tjenestenavn eller ett unikt visningsnavn finnes. Bruk av lasterekkefølgegrupper telles uten at medlemskapet utvides, og tvetydige identiteter forblir uavklart. Stoppede tjenester med manuell oppstart kan være normalt. Ingen tjeneste startes, og innstillinger for oppstart, avhengigheter og gjenoppretting bevares. Bruk den tilhørende arbeidsflyten under Reparasjon når en bestemt funksjon trenger gjenoppretting.

### Programfeil

Leser nylige feil og kritiske hendelser i Program-loggen sammen med avgrenset historikk fra Pålitelighetsovervåking, og gir én konklusjon om stabilitetshistorikken. Historiske oppføringer beviser ikke en aktuell feil eller grunnårsak og peker bare mot mer spesifikk diagnose. Ingen programmer stoppes, repareres, fjernes eller konfigureres på nytt.

### Skjermenheter

Undersøker grafikkadaptere og skjermer for rapporterte enhetsfeil, maskinvare-ID-er og installerte driverversjoner. Ingen driverinstallasjon, tilbakestilling av enheter eller endring av oppløsning. Bildekvalitet og fysiske tilkoblinger testes ikke.

### Inndataenheter

Undersøker feil på tastatur, mus og HID-enheter og driveroversikten. Ingen tastetrykk registreres, ingen enheter tilbakestilles, og ingen drivere endres. Fysisk inndatafunksjon testes ikke.

### Enhetsproblemer

Finner Plug and Play-konfigurasjonsfeilkoder som ikke er null, på tvers av enhetsklasser, og sammenholder bevis for tilstedeværende enheter med avgrenset SetupAPI-historikk for driverinstallasjon, enhetsoppsett og Plug and Play. Historiske oppføringer alene fastslår ikke en aktuell feil. Se over nøyaktig enhetsidentitet og bevis for godkjente drivere uten å endre maskinvare, drivere eller konfigurasjon. Manglende enheter og fysisk funksjon verifiseres ikke.
