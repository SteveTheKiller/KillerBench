# Apper

Apper ser over installerte programmer, Store-pakker, Office og Outlook, nettleserutvidelser, oppstartsapper og søk. Verktøyene for fjerning og tilbakestilling viser alltid en gjennomgang først og endrer bare det du velger.

Trinnvise veiledninger: [Konfigurer en ny datamaskin](set-up-new-pc.html).

## Verktøy i denne seksjonen

Se over installerte programmer, Store-pakker, Office og Outlook, nettleserutvidelser og søk.

### Installerte programmer

Leser avinstalleringsregistreringer for programmer i valgt maskin- eller kontoomfang, og kombinerer omfanget med avgrensede bevis fra Program-feil, Pålitelighetsovervåking og omstarter. Andre kontoer og bærbare apper utelates. Registreringer og bred historikk er ikke nøyaktig bevis for et programs tilstand eller eierskap. Ingen handling for installasjonsprogram, avinstallasjonsprogram, program, prosess, tjeneste, fil, registrering, konto, policy eller omstart velges.

### Apppakker

Lager en oversikt over registrerte Appx/MSIX-pakker for valgt kontoomfang. Kontoen som kjører, er standard; alle brukere krever administratortilgang. Rapporterer nøyaktige identiteter, flagg for rammeverk og ikke-fjernbar, og antall avhengigheter. Klargjorte pakker, Win32-programmer, administrert eierskap og appdata vurderes ikke. Ingen pakker fjernes.

### Nettleserutvidelser

Lager en avgrenset oversikt over identiteter for Chrome-, Edge-, Brave- og Firefox-utvidelser for kontoen som kjører. Nettleserhistorikk, påloggingsinformasjon, utvidelsesinnstillinger, manifestinnhold, kommandoer og policyverdier utelates. At en utvidelse finnes, beviser ikke at den er aktivert, pålitelig, hvem som eier den eller at den trygt kan fjernes. Ingen nettleser, utvidelse eller policy endres.

### Oppstartsprogrammer

Lager en oversikt over Run- og RunOnce-registreringer for maskinen og kontoen som kjører, filnavn i Oppstart-mappen, Windows' godkjenningstilstander for oppstart og navn og tilstander for planlagte oppgaver ved oppstart eller pålogging, og gir én konklusjon om oppstartsmiljøet. Kommandolinjer, målstier, binære godkjenningstidsstempler, oppgavehandlinger, argumenter, kontoer og påloggingsinformasjon samles ikke inn. En registrering beviser ikke at et element kjørte, forårsaket en forsinkelse, er uønsket eller trygt kan deaktiveres. Godkjenningstilstanden beviser ikke at en tilsvarende registrering finnes. Ingen endring av registrering, fil, program, oppgave, tjeneste eller policy velges.

### Fjerning av Microsoft-apper

Ser over valgte registreringer for OneDrive, Outlook, Office/M365, Project og Teams med valg for kontoomfang og oppbevaring. Oversiktene for maskinen, kontoen som kjører og registrerte pakker sammenholdes; gjennomgang for alle brukere omfatter også pakker som er klargjort for nye brukere. En støttet pakke for gjeldende konto kan få en egen, nøyaktig sikkerhetsplan i skrivebordsappen eller CLI. Fjerning av den ene pakken er en egen beskyttet skrivebordshandling med egne bekreftelser. Selve gjennomgangen endrer ingen prosess, program, pakke, fil, påloggingsinformasjon, mappeplassering, registerverdi eller brukerdata.

### Tilbakestilling av Outlook

Ser over nye Outlook, klassisk Outlook eller begge, med eksplisitte valg for hurtigbuffere, profilinnstillinger, godkjenning, OST-filer, tillegg og ny installasjon av nye Outlook. Gjennomgangen for kontoen som kjører, kontrollerer avgrensede kjente plasseringer, nøyaktige registreringer og at prosessene er lukket. En egen bekreftet handling som bare gjelder hurtigbufferen, beholder de nøyaktige hurtigbuffermappene under unike gjenopprettingsnavn og verifiserer hver flytting. Gjennomgang for alle brukere og andre tilbakestillingsområder forblir bare til gjennomgang. Ingen bevart hurtigbuffer, profil, OST-fil, påloggingsinformasjon, tillegg, pakke eller innstilling fjernes.

### Foxit-oversikt

Lager en oversikt over Foxit-produkter installert på maskinen og registerverdier for oppdateringsprogrammet i begge visninger. Brukerinstallasjoner, gjeldende policy og produkteierskap verifiseres ikke. Ingen programmer eller innstillinger endres.

### Windows Installer-tjeneste

Kontrollerer Installer-policyer for begrensning, tilbakerulling, utvidede rettigheter, logging og feilsøking, markører for pågående operasjoner, motorens binærfiler og synligheten til registrerte hurtigbufrede pakker før tjenestegjenoppretting. Sammenholder nylige mislykkede og vellykkede utfall bare for nøyaktige rapporterte produktidentiteter. Hvis forutsetningene er oppfylt, kan automatisk reparasjon starte den aktiverte tjenesten og verifisere Kjører. Historiske utfall, logging, hurtigbufferens synlighet og feilsøkingsfunn gir ikke grunnlag for endringer i pakker eller policy. Policyer, logger, transaksjonsmarkører, oppstartstyper og hurtigbufrede pakker bevares. En bestemt pakkeinstallasjon testes ikke.

### Windows Search

Kontrollerer om tjenesten Windows Search er tilgjengelig, gjeldende SystemIndex-aktivitet og nylig hendelseshistorikk for Search. Automatisk reparasjon kan starte en stoppet, aktivert tjeneste og verifisere at den kjører. Et eget, eksplisitt valg for å bygge indeksen på nytt beholder den tidligere indeksen, starter en ny indeks og verifiserer nøyaktig tjeneste- og mappetilstand. Indekserte plasseringer, policy, oppstartstype og bevarte data bevares. Å starte tjenesten eller bygge indeksen på nytt kan gjenoppta indeksering i bakgrunnen. Om søkeresultatene er fullstendige, forblir utestet.

### Appklargjøring

Kontrollerer om tjenesten Appklargjøring er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende førstegangspålogging og klargjøring av programmer kan fortsette etter gjeldende policy. Pakker, registreringer, lisenser, brukerprofiler, appdata, policy og oppstartstype bevares. KillerBench velger ingen pakkeoperasjon. Verifiseringen dekker bare stabil tjenestetilstand, ikke pålogging, klargjøring eller oppstart av programmer.

### Tilstandsrepositoriumtjeneste

Kontrollerer om Tilstandsrepositoriumtjeneste er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende forespørsler om tilstand i appmodellen kan fortsette. Registreringer, pakker, lisenser, profiler, nettleser- og appdata, repositoriuminnhold, policy og oppstartstype bevares. Ingen tilbakestilling, ny registrering eller sletting forespørres. Verifiseringen dekker bare stabil tjenestetilstand, ikke repositoriets konsistens eller hvordan programmer fungerer.

### AppX-distribusjonstjeneste

Kontrollerer om AppX-distribusjonstjeneste er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende forespørsler om distribusjon, registrering eller vedlikehold kan fortsette etter gjeldende policy. Pakker, registreringer, lisenser, profiler, appdata, repositoriuminnhold, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen pakkeoperasjon. Verifiseringen dekker bare stabil tjenestetilstand, ikke distribusjon, registrering eller oppstart av programmer.

### Klientlisenstjeneste

Kontrollerer om Klientlisenstjeneste er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende lisensforespørsler for Store-apper kan fortsette etter gjeldende policy. Pakker, registreringer, lisenser, tokener, kontoer, profiler, appdata, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen operasjon for pakke, lisens, token eller konto. Verifiseringen dekker bare stabil tjenestetilstand, ikke lisensvalidering eller oppstart av programmer.

### Installasjonstjeneste for Microsoft Store

Kontrollerer nylig installasjonshistorikk for Microsoft Store og om installasjonstjenesten for Store er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende forespørsler om nedlasting og installasjon fra Store kan fortsette etter gjeldende policy. Pakker, registreringer, lisenser, kontoer, appdata, nedlastingstilstand, kilder, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen operasjon for pakke, nedlasting, lisens, konto, kilde eller installasjon. Verifiseringen dekker bare stabil tjenestetilstand, ikke utfall for nedlasting, installasjon, lisens eller program.

### Windows-lisensbehandlingstjeneste

Kontrollerer om Windows-lisensbehandlingstjeneste er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende lisenskontroller for programmer kan fortsette etter gjeldende policy. Pakker, registreringer, lisenser, tokener, kontoer, appdata, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen operasjon for pakke, lisens, token, konto, registrering eller rettighet. Verifiseringen dekker bare stabil tjenestetilstand, ikke lisensvalidering, rettigheter eller oppstart av programmer.

### Windows PushToInstall-tjeneste

Kontrollerer om Windows PushToInstall-tjeneste er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. En eksisterende, tidligere godkjent ekstern installasjonsforespørsel kan fortsette etter gjeldende policy. Pakker, registreringer, lisenser, kontoer, appdata, forespørsler i kø, kilder, policy, tjenestetillatelser og oppstartstype bevares. KillerBench oppretter, godkjenner eller velger ingen forespørsel, pakke, kilde, konto eller installasjon. Verifiseringen dekker bare stabil tjenestetilstand, ikke behandling av forespørsler, nedlasting, installasjon eller programutfall.
