# Sikkerhet

Sikkerhet undersøker Microsoft Defender, brannmuren, sertifikater og påloggingstjenestene. Den viser også TPM- og plattformsikkerhetstilstanden som BitLocker og Windows Hello er avhengige av.

Trinnvise veiledninger: [Krypter en stasjon med BitLocker](encrypt-with-bitlocker.html) og [Gjør Windows mer privat](make-windows-private.html).

## Verktøy i denne seksjonen

Undersøk beskyttelse, brannmur, sertifikater og påloggingstjenester.

### Microsoft Defender

Undersøker beskyttelses- og signaturstatus i Defender. Automatisk reparasjon oppdaterer signaturer bare når Defender melder aktiv beskyttelse og utdaterte signaturer. Bruker konfigurerte signaturkilder. Ingen endringer i beskyttelse, unntak eller policy; ingen skanning etter skadelig programvare.

### Windows Sikkerhetssenter

Ser avgrenset over antivirusregistreringer med rå leverandørtilstand, nøyaktig feilhistorikk for tjenesten og om Windows Sikkerhetssenter er klar, uten å tolke beskyttelsestilstanden. En tom oversikt er ingen dom over beskyttelsen, og antall registreringer, rå tilstand eller historiske feil kan ikke gi grunnlag for gjenoppretting eller en sikkerhetsendring. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende overvåking av sikkerhetsstatus og varsler kan fortsette. Antiviruskonfigurasjon, leverandørregistreringer, beskyttelsesinnstillinger, unntak, varsler, policy og oppstartstype bevares. Ingen skanning, oppdatering eller beskyttelsesendring forespørres. Verifiseringen dekker bare stabil tjenestetilstand, ikke om antivirus finnes, beskyttelsestilstand, hvor ferske signaturene er, levering av varsler, eierskap til registreringer eller gjeldende policy.

### Brannmurtjenester

Leser aktiveringsinnstillinger for profilene Domene, Privat og Offentlig fra ActiveStore, og kontrollerer deretter tjenestene Grunnleggende filtreringsmotor og Windows Defender-brannmur. Valgfri gjenoppretting starter bare aktiverte og stoppede tjenester etter nye avhengighetskontroller. Eksisterende filtreringspolicy kan tre i kraft og avbryte nettverkstilgang eller ekstern tilgang. Oppstartstyper, tjenestetillatelser, brannmurregler, profiler og IPsec-innstillinger bevares. Ingen tilbakestillinger, regelendringer eller tjenestestopp. Verifiseringen fastslår bare at tjenesten kjører; faktisk beskyttelse og tilkobling forblir uverifisert.

### Utløp av sertifikater

Undersøker datoer på sertifikater og bygger lokale tillitskjeder bare fra hurtigbufferen for de personlige lagrene til maskinen og gjeldende bruker, og gir én avgrenset driftskonklusjon. Tilbakekalling, presentasjon på servere, bindinger, registrering, policyeierskap, ekstern tillit og om den private nøkkelen kan brukes, forblir uverifisert. Ingen sertifikater, tillitsinnstillinger, registreringer, bindinger, private nøkler, programmer, tjenester eller policyer endres.

### Legitimasjonsbehandling

Kontrollerer om tjenesten Legitimasjonsbehandling er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende programmer kan fortsette forespørsler om lagret påloggingsinformasjon. Hvelvinnhold, passord, tilgangsnøkler, sertifikater, tokener, legitimasjonsregistreringer, tillatelser, policy, tjenestetillatelser og oppstartstype bevares. Ingen legitimasjonsinnhold eller -operasjon forespørres. Verifiseringen dekker bare stabil tjenestetilstand, ikke henting av legitimasjon, godkjenning eller tilgang for programmer.

### Behandling av nettkontoer

Ser over historikk for kontomegleren med anonymisert nyttelast og om Behandling av nettkontoer er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende programmer kan fortsette konto- og tokenforespørsler etter gjeldende policy. Historiske hendelser kan ikke gi grunnlag for gjenoppretting eller fastslå gjeldende kontotilstand. Kontoer, påloggingsinformasjon, tokener, informasjonskapsler, appregistreringer, tillatelser, påloggingstilstand, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen operasjon for konto, legitimasjon, token, informasjonskapsel, tillatelse eller pålogging. Verifiseringen dekker bare stabil tjenestetilstand, ikke godkjenning, utstedelse av tokener eller tilgang for programmer.

### Påloggingsassistent for Microsoft-konto

Ser over historikk for kontomegleren med anonymisert nyttelast og om Påloggingsassistent for Microsoft-konto er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende påloggingsforespørsler med Microsoft-konto kan fortsette etter gjeldende policy. Historiske hendelser kan ikke gi grunnlag for gjenoppretting eller fastslå gjeldende kontotilstand. Kontoer, påloggingsinformasjon, tokener, enhetsregistrering, appdata, tillatelser, påloggingstilstand, policy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen operasjon for konto, legitimasjon, token, registrering, tillatelse eller pålogging. Verifiseringen dekker bare stabil tjenestetilstand, ikke godkjenning, kontotilkobling eller tilgang for programmer.
