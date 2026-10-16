# Reparasjon

Reparasjon samler verktøyene som kan rette noe. Hvert av dem stiller en diagnose først. En reparasjon tilbys bare når ferske bevis viser nøyaktig det problemet den løser, og den kjøres bare etter at du slår på **Reparer bekreftede problemer automatisk** eller bekrefter en enkelt reparasjon selv. Hver reparasjon journalføres og verifiseres etterpå.

Trinnvise veiledninger: [Reparer systemfiler](repair-system-files.html) og [Reparer Windows Update](fix-windows-update.html).

## Automatisk og manuell reparasjon

**Reparer bekreftede problemer automatisk** lar et diagnoseverktøy kjøre reparasjonene det beviser er nødvendige etter skanningen. Det er slått av som standard.

For én enkelt vedlikeholdsreparasjon åpner du **Avansert: logger og manuelle reparasjoner** og velger **Reparasjoner**. Velg **Komponentlager (DISM)** eller **Systemfiler (SFC)**, les forhåndsvisningen, merk av **Kjør denne reparasjonen på denne PC-en** og trykk **Kjør reparasjon**. Se [Slik holdes endringer trygge](safety.html) for hva som skjer før og etter enhver endring.

## Verktøy i denne seksjonen

Bruk disse etter at en diagnose har funnet et problem. Reparasjoner krever bekreftede bevis og eksplisitt godkjenning.

### Sentrale Windows-tjenester

Kontrollerer tjenestene Strøm, Plug and Play, Brukerbehandling, Gruppepolicyklient, Grensesnitt for nettverkslager, RPC-endepunktkartlegging, Eksternt prosedyrekall, DCOM Server Process Launcher, Infrastrukturtjeneste for bakgrunnsoppgaver og CoreMessaging samlet. Valgfri gjenoppretting starter bare nøyaktige, aktiverte automatiske tjenester som ferske bevis bekrefter er stoppet etter oppstartsvinduet og fullstendige avhengighetskontroller. Tjenester med manuell oppstart forblir uendret når de er inaktive. Strømplaner, enheter, drivere, kontoer, påloggingsinformasjon, profiler, policy, adaptere, adresser, ruter, DNS, brannmurkonfigurasjon, registreringer, endepunkter, oppgaveregistreringer, appdata, tjenestetillatelser og oppstartstyper bevares. Ingen policyoppdatering, enhetsskanning, kontohandling, nettverksendring, strømendring, registreringsendring eller oppgavestart forespørres. Verifiseringen dekker bare stabil tjenestetilstand, ikke hvordan funksjonene virker fra ende til ende.

### Systemfiler

Lager en avgrenset oversikt over tilstanden til vedlikeholdspakker, valgfrie funksjoner og funksjonaliteter i Windows, kontrollerer forutsetninger, skanner beskyttede systemfiler og velger SFC automatisk bare når livekontrollen bekrefter skade. Ventende tilstander for pakker, funksjoner eller funksjonaliteter krever gjennomgang, men gir ikke grunnlag for installasjon, fjerning, opprydding, tilbakerulling, aktivering eller deaktivering.

### WMI-repositorium

Kontrollerer om WMI-tjenesten er tilgjengelig, og deretter repositoriets konsistens. Automatisk reparasjon kan starte en kvalifisert stoppet tjeneste og berge bekreftet skade, og deretter verifisere hvert resultat. Innstillinger for deaktiverte tjenester bevares. Lesbare registreringer slås sammen; uleselig innhold kan kreve at leverandøren installeres på nytt. Administrasjonsklienter kan bli avbrutt. Ingen tilbakestilling, sletting eller automatisk tilbakerulling.

### Windows-systemvarsler

Leser nylig historikk fra Tjenestekontrollbehandling og kontrollerer deretter COM+ Event System og System Event Notification Service samlet. Valgfri gjenoppretting verifiserer at COM+ Event System er klar før SENS startes. Eksisterende varsler om pålogging, nettverk, strøm og COM+ kan fortsette med gjeldende konfigurasjon. COM+-kataloger, abonnementer, varselregistreringer, mottakere, kontoer, tillatelser, gjenopprettingsinnstillinger, policy og oppstartstyper bevares. Ingen abonnementer, registreringer, mottakere, kontoer, tillatelser eller policyer endres. Verifiseringen dekker bare stabil tjenestetilstand, ikke levering av varsler fra ende til ende.

### Diagnostikkpolicytjeneste

Ser over avgrenset driftshistorikk for Diagnostikkpolicy, nøyaktig feilhistorikk for tjenesten og om tjenesten er klar. Ufullstendige bevis går foran. Historiske oppføringer kan ikke gi grunnlag for gjenoppretting av tjenesten eller en feilsøkingshandling. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende diagnosescenarioer kan fortsette. Policy, feilsøkingsdata og oppstartstype bevares. Ingen feilsøkingsprogrammer eller foreslåtte løsninger kjøres. Verifiseringen dekker bare stabil tjenestetilstand, ikke feiloppdagelse, fullført diagnose eller utfallet av en reparasjon.

### Windows-feilrapportering

Ser over konfigurasjon for krasjdumper, avgrensede metadata for dumpfiler, nylige krasjrapporter for systemet, nøyaktig feilhistorikk for tjenesten Windows-feilrapportering og om tjenesten er klar. Historiske rapporter fastslår ikke en aktuell feil eller grunnårsak og kan ikke gi grunnlag for gjenoppretting. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende rapporter i kø kan fortsette etter gjeldende policy. Rapportarkiver, dumper, samtykke, policy for opplasting og oppstartstype bevares. Ingen opplasting eller sletting forespørres. Verifiseringen dekker bare stabil tjenestetilstand, ikke oppretting, opplasting eller levering av rapporter, problemløsning eller krasjdiagnose.

### Windows-hendelsesinnsamling

Ser over en avgrenset oversikt over abonnementer i Windows-hendelsesinnsamling, nøyaktig feilhistorikk for tjenesten og om tjenesten er klar. En tom oversikt kan være tilsiktet, og antall abonnementer eller historiske feil kan ikke gi grunnlag for gjenoppretting. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende abonnementer kan gjenoppta ekstern hendelsesinnsamling. Abonnementer, videresendte hendelser, påloggingsinformasjon, kanalinnstillinger, policy og oppstartstype bevares. Verifiseringen dekker bare stabil tjenestetilstand, ikke om kilden kan nås, godkjenning, ekstern levering, hvor ferske hendelsene er eller abonnementenes tilstand.

### Windows-hendelseslogg

Undersøker fire diagnosekanaler for aktivert tilstand, oppbevaringsmodus, gjeldende størrelse, konfigurert grense, antall oppføringer og metning, og kontrollerer deretter om tjenesten Windows-hendelseslogg er tilgjengelig. Automatisk reparasjon kan starte en stoppet, aktivert tjeneste etter avhengighetskontroll og verifisere at den kjører. En egen bekreftet, målrettet handling kan aktivere én nøyaktig deaktivert, ikke-administrert diagnosekanal og samtidig bevare alle andre rapporterte innstillinger. Logger, kanaltillatelser, oppbevaring, størrelsesgrenser, oppstartstyper og administrerte innstillinger bevares. Vellykket levering av hendelser og arkivkapasitet forblir uverifisert.

### Windows-tidstjeneste

Kontrollerer konfigurasjon, tjeneste og synkronisering for Windows-tid. Automatisk reparasjon kan starte W32Time når gjeldende status melder den nøyaktige feilen for stoppet tjeneste og nye kontroller av policy, modus og avhengigheter består, og deretter be om ny synkronisering og verifisere ny status. Dette kan justere klokken ved hjelp av eksisterende konfigurerte kilder. Oppstartstype, kildekonfigurasjon og administrerte innstillinger bevares.

### Oppgaveplanlegger-tjeneste

Lager en avgrenset oversikt over tilstand, kjøreresultater og antall tapte kjøringer for planlagte oppgaver, og kombinerer dem med nøyaktig feilhistorikk for tjenesten Oppgaveplanlegger før gjeldende tilgjengelighet kontrolleres. Oppgavefunn og historiske tjenestefeil kan ikke gi grunnlag for å starte tjenesten. Valgfri gjenoppretting starter bare en nøyaktig aktivert og stoppet tjeneste etter nye avhengighetskontroller. Eksisterende aktiverte oppgaver kan starte under sine konfigurerte kontoer, også for tapte utløsere. Ingen oppgavedefinisjon, kontohaver, legitimasjonsreferanse, policy eller oppstartstype endres, og ingen oppgave startes eller slettes direkte. Verifiseringen dekker bare stabil tjenestetilstand, ikke om handlinger lykkes, om påloggingsinformasjon er gyldig eller fremtidige oppgaveutfall.

### Brukerprofiltjeneste

Kontrollerer om Brukerprofiltjeneste er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende påloggings- og profilforespørsler kan fortsette. Profilmapper, registerhiver, tillatelser, registreringer i profillisten, påloggingsinformasjon, brukerdata, policy, tjenestetillatelser og oppstartstype bevares. Ingen profiloperasjon forespørres. Verifiseringen dekker bare stabil tjenestetilstand, ikke pålogging, lasting av profiler eller tilgang til brukerdata.

### Sikkerhetskopitjenester

Kontrollerer tjenestene Volume Shadow Copy, Microsoft Software Shadow Copy Provider, Windows Backup og Block Level Backup Engine, og leser deretter status for VSS-skrivere, registrerte leverandørtyper, gjeldende øyeblikksbilder og tildeling av skyggelagring. Stoppede tjenester med manuell oppstart kan være normalt når de er inaktive. Valgfri gjenoppretting starter bare stoppede automatiske tjenester etter avhengighetskontroller, og verifiserer deretter at de kjører. Eksisterende planlagt eller avbrutt sikkerhetskopiering kan fortsette. Deaktiverte og manuelle oppstartsinnstillinger, øyeblikksbilder, skyggelagring, leverandørregistreringer, sikkerhetskopiplaner, kataloger, tidsplaner, mål, påloggingsinformasjon og oppbevaring bevares. Ingen sikkerhetskopi velges eller konfigureres på nytt, ingen tjeneste startes på nytt, og ingen øyeblikksbilder eller kataloger slettes. Leverandørregistrering, skriverstatus, lagringstildeling og stabil tjenestetilstand verifiserer ikke at sikkerhetskopieringen fullføres, at målet kan nås eller at en gjenoppretting lykkes.
