# Enheter

Enheter dekker skrivere, lyd, kameraer, Bluetooth, inndata og Windows-tjenestene som tilkoblet maskinvare er avhengig av.

Trinnvise veiledninger: [Løs utskriftsproblemer](fix-printing.html) og [Undersøk krasj og blå skjermer](investigate-crashes.html).

## Verktøy i denne seksjonen

Utskrift, lyd, kameraer, Bluetooth, inndata og tjenestene bak tilkoblet maskinvare.

### Utskrift

Kontrollerer Utskriftskø, lokale køer på pause, synlige utskriftsjobber, frakoblede innstillinger og tilstander rapportert av drivere, og lager deretter en oversikt over registreringer for køer, drivere, porter, Standard TCP/IP-endepunkter, utskriftsbehandlere og utskriftsovervåkere. Sammenholder nylige PrintService-problemer bare når én nøyaktig gjeldende køidentitet finnes. Tildelte Standard TCP/IP-endepunkter får ett avgrenset tilkoblingsforsøk uten appdata, SNMP eller utskriftsjobb. Automatisk reparasjon kan starte den aktiverte utskriftskøtjenesten, gjenoppta bare nøyaktige køer på pause med ferske bevis for ventende jobber, og slå av frakoblet modus. Tilsiktede pauser uten jobber forblir bare til gjennomgang. Eksterne tilkoblinger, SNMP-fellesskapsstrenger, utvidelsesregistreringer og jobbinnhold bevares. En tilstandskode eller vellykket tilkobling fastslår ikke skriverens identitet, forbruksmateriell, mekanikk, om køen godtar jobber, vellykket gjengivelse eller fysisk utskrift.

### Lyd

Kontrollerer identiteter og delte formater for standard multimedieinndata og -utdata, tilstanden til lydenheter og drivere, Windows Audio Endpoint Builder og Windows Audio. Automatisk reparasjon starter stoppede, aktiverte tjenester og verifiserer at de kjører. Oppstartsinnstillinger, drivere, valgte enheter, eksklusiv modus og volumnivåer bevares. Lydavspilling, opptak og ruting i programmer forblir utestet.

### Kamerastøtte

Undersøker feil på kamera- og bildeenheter, maskinvare-ID-er, bevis for installerte drivere og om Windows Camera Frame Server er tilgjengelig. Valgfri gjenoppretting starter bare en aktivert og stoppet tjeneste etter avhengighetskontroller. Et program som allerede ber om kameratilgang, kan fortsette med eksisterende tillatelser. Personverninnstillinger for kamera, tillatelser, enhetskonfigurasjon, policy, drivere og oppstartstype bevares. Ingen tilbakestilling, endring av tillatelser eller driverinstallasjon. Verifiseringen dekker bare rapportert enhetskonfigurasjon og at tjenesten kjører, ikke bildeopptak, personverntilgang eller fysisk funksjon.

### Bluetooth-støtte

Undersøker feil på Bluetooth-enheter, maskinvare-ID-er, bevis for installerte drivere og om Bluetooth-støttetjenesten er tilgjengelig. Valgfri gjenoppretting starter bare en aktivert og stoppet tjeneste etter avhengighetskontroller; sammenkoblede enheter kan koble til igjen. Sammenkoblinger, radioinnstillinger, enhetskonfigurasjon, policy og oppstartstype bevares. Ingen tilbakestilling, fjerning av enheter eller driverinstallasjon. Verifiseringen dekker bare rapportert enhetskonfigurasjon og at tjenesten kjører, ikke oppdaging, sammenkobling, lyd eller dataoverføring.

### Smartkortstøtte

Undersøker feil på smartkortlesere, maskinvare-ID-er, bevis for installerte drivere og om tjenesten Smartkort er tilgjengelig. Valgfri gjenoppretting starter bare en aktivert og stoppet tjeneste etter avhengighetskontroller. Programmer kan igjen oppdage innsatte kort etter eksisterende policy. Sertifikater, PIN-koder, påloggingsinformasjon, leser- og kortkonfigurasjon, policy, drivere og oppstartstype bevares. Ingen initialisering, registrering, PIN-operasjon, tilbakestilling av leser eller driverinstallasjon. Verifiseringen dekker bare rapportert leserkonfigurasjon og at tjenesten kjører, ikke kortoppdaging, godkjenning eller tilgang for programmer.

### Biometrisk støtte

Undersøker feil på biometriske enheter, maskinvare-ID-er, bevis for installerte drivere og om Windows Biometric Service er tilgjengelig. Valgfri gjenoppretting starter bare en aktivert og stoppet tjeneste etter avhengighetskontroller. Eksisterende forespørsler fra Windows Hello og programmer kan fortsette etter gjeldende policy. Biometriske maler, Windows Hello-data, påloggingsinformasjon, enhetskonfigurasjon, tillatelser, policy, drivere og oppstartstype bevares. Ingen registrering, sletting av maler, tilbakestilling av sensorer, endring av tillatelser eller driverinstallasjon. Verifiseringen dekker bare rapportert sensorkonfigurasjon og at tjenesten kjører, ikke registrering av data, samsvar, pålogging eller tilgang for programmer.

### Tjeneste for HID-enheter

Kontrollerer om Tjeneste for HID-enheter er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige tjenesten når den er stoppet og satt til automatisk start, etter nye avhengighetskontroller. En stoppet tjeneste med manuell start er bare til informasjon. Eksisterende hurtigknapper, mediekontroller og andre HID-funksjoner kan fortsette. Enheter, drivere, knappetilordninger, tilgjengelighetsinnstillinger, inndatainnstillinger, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen operasjon for enhet, driver, tilordning eller inndatainnstilling. Verifiseringen dekker bare stabil tjenestetilstand, ikke hvordan knapper, kontroller eller programmer oppfører seg.

### Opplistingstjeneste for bærbare enheter

Kontrollerer om Opplistingstjeneste for bærbare enheter er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige tjenesten når den er stoppet og satt til automatisk start, etter nye avhengighetskontroller. En stoppet tjeneste med manuell start er bare til informasjon. Eksisterende programmer kan igjen oppdage og få tilgang til tilkoblede bærbare enheter. Enheter, drivere, filer, biblioteker, synkroniseringsinnstillinger, tillatelser, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen operasjon for enhet, fil, bibliotek, synkronisering eller overføring. Verifiseringen dekker bare stabil tjenestetilstand, ikke opplisting, tilgang, synkronisering eller overføring.

### Windows Image Acquisition (WIA)

Kontrollerer om tjenesten Windows Image Acquisition er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige tjenesten når den er stoppet og satt til automatisk start, etter nye avhengighetskontroller. En stoppet tjeneste med manuell start er bare til informasjon. Eksisterende forespørsler om skanning og stillbildeinnhenting kan fortsette med gjeldende apptillatelser. Enheter, drivere, bildedata, apptillatelser, personverninnstillinger, overføringsinnstillinger, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen operasjon for enhet, driver, bilde, tillatelse, overføring eller innhenting. Verifiseringen dekker bare stabil tjenestetilstand, ikke drift av skanner, kamera, overføring eller programmer.

### Registrering av maskinvare for skall

Undersøker avgrensede Plug and Play-bevis for lagringskontrollere, disker og bærbare enheter og referanser til installerte drivere, og kontrollerer deretter om tjenesten Registrering av maskinvare for skall er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige tjenesten når den er stoppet og satt til automatisk start, etter nye avhengighetskontroller. En stoppet tjeneste med manuell start er bare til informasjon. Eksisterende varsler for Automatisk avspilling og ny maskinvare kan fortsette etter gjeldende policy. Enhetsobservasjoner og bevart historikk kan ikke gi grunnlag for gjenoppretting. Enheter, drivere, volumer, monteringspunkter, innstillinger for Automatisk avspilling, behandlere, policy, tjenestetillatelser og oppstartstype bevares. Ingen enhets- eller innstillingsoperasjon forespørres. Verifiseringen dekker bare stabil tjenestetilstand, ikke levering av varsler, om medier er klare, volumtilgang eller fysisk enhetsdrift.

### Enhetsinstallasjonstjeneste

Kontrollerer om Enhetsinstallasjonstjeneste er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige tjenesten når den er stoppet og satt til automatisk start, etter nye avhengighetskontroller. En stoppet tjeneste med manuell start er bare til informasjon. Eksisterende enhetsinstallasjon som Windows har bedt om, kan fortsette etter gjeldende policy. Enheter, drivere, pakker, konfigurasjon, bindinger, klasseinnstillinger, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger og installerer ingenting. Verifiseringen dekker bare stabil tjenestetilstand, ikke enhetsinstallasjon eller enhetsdrift.

### Enhetstilknytningstjeneste

Kontrollerer nylig historikk for enhetsoppsett og Plug and Play, og deretter om Enhetstilknytningstjeneste er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige tjenesten når den er stoppet og satt til automatisk start, etter nye avhengighetskontroller. En stoppet tjeneste med manuell start er bare til informasjon. Eksisterende enhetstilknytning som Windows har bedt om, kan fortsette. Sammenkoblede og tilknyttede enheter, drivere, påloggingsinformasjon, metadata, tillatelser, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen operasjon for enhet, sammenkobling, fjerning, tilbakestilling eller driver. Verifiseringen dekker bare stabil tjenestetilstand, ikke oppdaging, sammenkobling, tilknytning eller enhetsdrift.

### Tjeneste for skjermforbedring

Undersøker bevis for grafikkadaptere og skjermer, og kontrollerer deretter om Tjeneste for skjermforbedring er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige tjenesten når den er stoppet og satt til automatisk start, etter nye avhengighetskontroller. En stoppet tjeneste med manuell start er bare til informasjon. Eksisterende forespørsler om lysstyrke, farge og forbedring kan fortsette. Skjermkonfigurasjon, lysstyrke, fargeprofiler, HDR-innstillinger, enheter, drivere, strøminnstillinger, policy, tjenestetillatelser og oppstartstype bevares. Ingen skjerminnstilling endres. Verifiseringen dekker bare stabil tjenestetilstand, ikke hvordan forbedringen virker, bildekvalitet eller fysisk skjermdrift.

### Tjeneste for administrasjon av tekstinndata

Undersøker bevis for tastatur, mus og HID-enheter, og kontrollerer deretter om Tjeneste for administrasjon av tekstinndata er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige tjenesten når den er stoppet og satt til automatisk start, etter nye avhengighetskontroller. En stoppet tjeneste med manuell start er bare til informasjon. Eksisterende forespørsler om berøringstastatur, håndskrift, emoji og avansert tekstinndata kan fortsette. Ordlister, håndskriftdata, språk- og tastaturinnstillinger, tilgjengelighets- og personverninnstillinger, enheter, drivere, policy, tjenestetillatelser og oppstartstype bevares. Ingen tekstinndata, tastetrykk eller håndskrift samles inn. Verifiseringen dekker bare stabil tjenestetilstand, ikke tekstinntasting, gjenkjenning eller enhetsdrift.

### Windows-skrifthurtigbuffertjeneste

Kontrollerer om Windows-skrifthurtigbuffertjeneste er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige tjenesten når den er stoppet og satt til automatisk start, etter nye avhengighetskontroller. En stoppet tjeneste med manuell start er bare til informasjon. Eksisterende programmer kan igjen bruke gjeldende skrifthurtigbuffer. Installerte skrifter, skriftfiler, appdata, hurtigbufferfiler, policy, tjenestetillatelser og oppstartstype bevares. KillerBench installerer eller fjerner ingen skrift og sletter eller bygger ikke hurtigbufferen på nytt. Verifiseringen dekker bare stabil tjenestetilstand, ikke tekstgjengivelse eller hvordan programmer oppfører seg.

### Vert for funksjonsregistreringsleverandør

Kontrollerer om Vert for funksjonsregistreringsleverandør er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige tjenesten når den er stoppet og satt til automatisk start, etter nye avhengighetskontroller. En stoppet tjeneste med manuell start er bare til informasjon. Eksisterende programmer kan igjen oppdage nettverksenheter via installerte leverandører. Nettverksprofiler, delinger, enheter, leverandører, brannmurregler, oppdagingsinnstillinger, påloggingsinformasjon, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen operasjon for enhet, deling, leverandør, brannmurregel eller oppdagingsinnstilling. Verifiseringen dekker bare stabil tjenestetilstand, ikke leverandørenes tilstand, enhetsoppdaging eller tilkobling.

### Publisering av funksjonsregistreringsressurser

Kontrollerer om Publisering av funksjonsregistreringsressurser er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige tjenesten når den er stoppet og satt til automatisk start, etter nye avhengighetskontroller. En stoppet tjeneste med manuell start er bare til informasjon. Denne PC-en kan bli synlig gjennom eksisterende innstillinger for nettverksoppdaging og brannmur. Nettverksprofiler, delinger, publiserte ressurser, brannmurregler, oppdagingsinnstillinger, påloggingsinformasjon, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen operasjon for deling, ressurs, brannmurregel eller oppdagingsinnstilling. Verifiseringen dekker bare stabil tjenestetilstand, ikke publisering, synlighet, tilgang eller tilkobling.
