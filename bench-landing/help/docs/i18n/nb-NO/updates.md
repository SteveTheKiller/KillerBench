# Oppdateringer

Oppdateringer dekker Windows Update, tjenestene bak den og komponentlageret. Du kan se hva som er installert, hva som venter og hvorfor en oppdatering mislykkes, og se over tilgjengelige oppdateringer før du velger hvilke som skal installeres.

Trinnvise veiledninger: [Reparer Windows Update](fix-windows-update.html) og [Reparer systemfiler](repair-system-files.html).

## Verktøy i denne seksjonen

Følg med på vedlikeholdsaktivitet, se over tilgjengelige oppdateringer og reparer Windows Update-kjeden.

### Overvåking av vedlikeholdsaktivitet

Observerer avgrenset vedlikeholdsaktivitet i Windows Update og Setup over ett eller flere øyeblikksbilder. Alternativene styrer oppdateringsintervallet og om oppdateringshendelser, oppdateringshistorikk, den nøyaktige planlagte oppgaven for Windows Update, nøyaktige tjenestetilstander, omstartsindikatorer, bakgrunnsoverføringer for gjeldende konto, aktivitet i Leveringsoptimalisering og personvernbegrenset Windows Installer-aktivitet skal tas med. Prosess-ID-er, kommandolinjer, brukere og stier utelates. Aktivitet beviser ikke fremdrift, fullføring, heng eller feil. Ingen handling for oppdatering, oppsett, overføring, oppgave, tjeneste, prosess, policy, omstart eller reparasjon forespørres.

### Tilgjengelige oppdateringer

Finner valgte programvare- og driveroppdateringer via den konfigurerte kilden, Windows Update eller en allerede registrert Microsoft Update-tjeneste. Kilden kontrolleres før søket. Ingen installasjon, godtakelse av lisenser, kilderegistrering eller policyendringer. Dette er atskilt fra vedlikeholdsreparasjon.

### Windows Update

Lager en avgrenset oversikt over tilstanden til vedlikeholdspakker, valgfrie funksjoner og funksjonaliteter i Windows, kontrollerer omstartsindikatorer, ledig plass og oppdateringstjenester, observerer Leveringsoptimalisering uten å starte den, og skanner deretter komponentlageret og beskyttede systemfiler. Ventende tilstander for pakker, funksjoner eller funksjonaliteter krever gjennomgang, men gir ikke grunnlag for noen komponenthandling. Automatisk reparasjon kan starte de aktiverte tjenestene Windows Update, BITS, Kryptografiske tjenester, Installasjonsprogram for Windows-moduler og Update Orchestrator. Når Update Orchestrator startes, kan eksisterende planlagt oppdateringsaktivitet fortsette etter gjeldende policy. Pakker, valgfrie funksjoner, funksjonaliteter, hurtigbufferen for Leveringsoptimalisering, likemenn, båndbredde, policy, kilderegistrering, tidsplaner og oppstartsinnstillinger bevares. DISM eller SFC krever livebevis for skade. Denne arbeidsflyten velger eller godkjenner ingen oppdatering.

### Windows Update-tjeneste

Kontrollerer nøyaktig tilstand for Windows Update-tjeneste, registrert tjeneste-DLL, avgrenset gjeldende oppdateringsaktivitet og nøyaktig feilhistorikk for tjenesten. DLL-observasjoner, oppdateringsaktivitet og historiske feil kan ikke gi grunnlag for å starte tjenesten eller en oppdateringsoperasjon. Valgfri gjenoppretting starter bare en stoppet automatisk tjeneste etter nye kontroller av avhengigheter og registrering av den kjørbare filen. En stoppet tjeneste med manuell oppstart er bare til informasjon. Eksisterende oppdateringsaktivitet kan fortsette etter gjeldende policy. Oppdateringspolicy, kilderegistrering, tidsplaner, omstartsinnstillinger, pakker, BITS-jobber, tjenestetillatelser og oppstartstype bevares. KillerBench velger, godkjenner eller installerer ingen oppdatering. Verifiseringen dekker bare stabil tjenestetilstand, ikke om kilden kan nås, anvendelighet, nedlasting, installasjon, fullføring eller om maskinen er klar for omstart.

### Leveringsoptimalisering

Kontrollerer nøyaktig tilstand for tjenesten Leveringsoptimalisering. En stoppet tjeneste med manuell oppstart er bare til informasjon. Bare en nøyaktig tilstand Satt på pause kan gi tilbud om gjenoppretting. Valgfri gjenoppretting gjentar tilstandskontrollen, sender én avgrenset forespørsel om å fortsette og krever to sammenhengende observasjoner av Kjører. Oppstartstype, innholdet i hurtigbufferen, likemenn, båndbredde, oppdateringskilde og administrert policy bevares. Overføringsaktivitet, hurtigbufferens tilstand, oppdateringsfremdrift og fullføring forblir uverifisert.

### BITS-tjeneste

Kontrollerer nøyaktig tilstand for Background Intelligent Transfer Service, overføringsjobber for gjeldende konto og avgrenset, nøyaktig feilhistorikk for tjenesten. Historiske feil og øyeblikksbilder av overføringer kan ikke gi grunnlag for å starte tjenesten. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende programmer kan fortsette bakgrunnsoverføringer i kø. Jobber, prioriteter, eiere, mål, proxyinnstillinger, policy og oppstartstype bevares. Verifiseringen dekker bare stabil tjenestetilstand, ikke overføringsfremdrift, programeierskap eller fullføring.

### Bakgrunnsoverføringer (BITS)

Leser BITS-overføringsjobber i valgt kontoomfang og ser over nøyaktige jobbidentiteter. Kønavn fastslår ikke hvilket program jobben tilhører. Ingen overføringer gjenopptas, fullføres, avbrytes, slettes eller tildeles på nytt.

### Kryptografiske tjenester

Kontrollerer nøyaktig tilstand for Kryptografiske tjenester, registrert tjeneste-DLL og avgrenset, nøyaktig feilhistorikk for tjenesten. Historiske feil og DLL-observasjoner kan ikke gi grunnlag for å starte tjenesten eller erstatte komponenter. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende katalog-, sertifikat- og oppdateringsoperasjoner kan fortsette. Sertifikater, nøkler, kataloger, tillitsinnstillinger, policy og oppstartstype bevares. Verifiseringen dekker bare stabil tjenestetilstand, ikke validering av sertifikater, signaturer, kataloger eller tillit.

### Installasjonsprogram for Windows-moduler

Kontrollerer nøyaktig tilstand for Installasjonsprogram for Windows-moduler, avgrensede overganger i vedlikeholdspakker og nøyaktig feilhistorikk for tjenesten. Historiske feil og pakketilstander kan ikke gi grunnlag for å starte tjenesten eller en pakkeoperasjon. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende vedlikeholdsaktivitet kan fortsette. Pakker, innhold i komponentlageret, kilder, policy og oppstartstype bevares. Verifiseringen dekker bare stabil tjenestetilstand, ikke tilstanden til komponentlageret, pakkenes anvendelighet, fullført vedlikehold eller om maskinen er klar for omstart.

### Windows Update Medic-tjeneste

Kontrollerer nøyaktig tilstand for Windows Update Medic-tjeneste, registrert tjeneste-DLL, avgrenset gjeldende oppdateringsaktivitet og nøyaktig feilhistorikk for tjenesten. DLL-observasjoner, oppdateringsaktivitet og historiske feil kan ikke gi grunnlag for å starte tjenesten eller en komponenthandling. En stoppet tjeneste med manuell oppstart kan være normalt. Når nøyaktig dette verktøyet er valgt, kan valgfri gjenoppretting bare starte den aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Windows-utbedring kan fortsette etter gjeldende policy og kan reparere oppdateringskomponenter eller utløse oppdateringsaktivitet. Oppdateringspolicy, kilderegistrering, tidsplaner, pakker, BITS-jobber, omstartsinnstillinger, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen oppdatering og endrer ingen oppdateringsinnstilling. Verifiseringen dekker bare stabil tjenestetilstand, ikke utbedring, komponenttilstand, anvendelighet, nedlasting, installasjon eller om maskinen er klar for omstart.

### Update Orchestrator-tjeneste

Kontrollerer nøyaktig tilstand for Update Orchestrator-tjeneste, den nøyaktige innebygde Windows Update-oppgaven, avgrenset gjeldende aktivitet og nøyaktig feilhistorikk for tjenesten. Oppgaveresultater, oppdateringsaktivitet og historiske feil kan ikke gi grunnlag for å starte tjenesten eller en oppdateringsoperasjon. Valgfri gjenoppretting starter bare en stoppet automatisk tjeneste etter nye kontroller av avhengigheter og registrering av den kjørbare filen. En stoppet tjeneste med manuell oppstart er bare til informasjon. Eksisterende planlagt oppdateringsaktivitet kan fortsette etter gjeldende policy og kan laste ned eller installere oppdateringer som allerede er godkjent. Oppdateringspolicy, kilderegistrering, tidsplaner, omstartsinnstillinger, pakker, BITS-jobber, tjenestetillatelser og oppstartstype bevares. KillerBench velger eller godkjenner ingen oppdatering. Verifiseringen dekker bare stabil tjenestetilstand, ikke anvendelighet, nedlasting, installasjon, fullføring eller om maskinen er klar for omstart.

### Opprydding i komponentlageret

Analyserer om Windows anbefaler opprydding i komponentlageret. Valgfritt vedlikehold fjerner erstattede komponenter umiddelbart og analyserer deretter på nytt. Ingen ResetBase eller manuell sletting i WinSxS. Ingen automatisk angring; frigjort diskplass måles ikke.
