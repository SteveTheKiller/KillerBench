# Nettverk

Nettverk undersøker tilkoblinger, DNS, ruter, trådløs og kablet pålogging, fildeling og ekstern tilgang. Verktøy som kontakter en annen vert, bruker målet og tidsavbruddet du angir, og ingenting endres med mindre du bekrefter en reparasjon.

Trinnvise veiledninger: [Løs nettverks- og internettproblemer](fix-network.html).

## Verktøy i denne seksjonen

Undersøk tilkoblinger, DNS, ruter, trådløst, deling og ekstern tilgang.

### Nettverk

Undersøker adaptere, adressetilstand og opprinnelse, gatewayer, DNS-konfigurasjon, gjeldende DNS-navneområdepolicy, TCP-innstillinger, strukturen i den native Winsock-katalogen for protokoller og navneområdeleverandører, IP-rutingtabellen og tilordninger i Hosts-filen. Automatisk reparasjon kan starte de aktiverte tjenestene DNS-klient, DHCP-klient, SMB-klient, Nettverksplasseringsbevissthet, Nettverkslistetjeneste og IP-hjelper, og fornye tilkoblede fysiske DHCP-adaptere som mangler en brukbar IPv4-adresse. Når tjenester for nettverksidentifisering startes, kan nettverksidentiteten beregnes på nytt og en annen eksisterende brannmurprofil tas i bruk. IP-hjelper kan aktivere eksisterende funksjoner for IPv6-overgang, portproxy og nettverksvarsler. Tjenestegjenoppretting verifiserer at tjenesten kjører, ikke at navneoppslag, rutevalg, nettverksidentifisering eller tunneler fungerer. Ruter, Hosts-oppføringer, DNS-servere, profiler, brannmurregler, Winsock-leverandører, overgangskonfigurasjon, oppstartstyper og policyer bevares. Fornyelse kan endre leasede adresser og avbryte tilkoblinger. Statiske innstillinger og virtuelle adaptere bevares. Nøyaktige ICMP-svar fra gatewayen verifiserer ikke at internett kan nås.

### Trådløst nettverk

Undersøker nettverksenheter og om WLAN AutoConfig er tilgjengelig. Valgfri gjenoppretting starter bare en aktivert og stoppet tjeneste etter avhengighetskontroller; lagrede profiler kan koble til igjen automatisk. Profiler, påloggingsinformasjon, policy, radioinnstillinger og oppstartstyper bevares. Ingen tilbakestilling av adaptere eller driverinstallasjon. Verifiseringen dekker at tjenesten kjører, ikke trådløs godkjenning eller tilkobling.

### Kablet godkjenning (802.1X)

Undersøker nettverksenheter og Wired AutoConfig for nettverk som krever 802.1X. Valgfri gjenoppretting starter bare en aktivert og stoppet tjeneste etter avhengighetskontroller. Eksisterende godkjenningsprofiler kan aktiveres og endre nettverkstilgangen. Sertifikater, påloggingsinformasjon, profiler, policy og oppstartsinnstillinger bevares. Verifiseringen dekker bare at tjenesten kjører, ikke godkjenning eller tilkobling.

### Nettverkstilkoblinger

Ser avgrenset over gjeldende tildeling av nettverksprofiler, nøyaktig feilhistorikk for tjenesten og om tjenesten Nettverkstilkoblinger er klar. Ufullstendige bevis går foran. Offentlige profiler og historiske feil forblir kontekst og kan ikke gi grunnlag for gjenoppretting eller en nettverksendring. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende grensesnitt for nettverksadministrasjon kan fortsette. Adaptere, adresser, ruter, DNS-innstillinger, tilkoblingsprofiler, påloggingsinformasjon, brannmurkonfigurasjon, policy og oppstartstype bevares. Ingen tilkobling eller nettverksinnstilling endres. Verifiseringen dekker bare stabil tjenestetilstand, ikke adapterdrift, adressetildeling, ruting, navneoppslag eller tilkobling.

### WinHTTP Auto-Proxy

Ser over lagrede WinHTTP-standarder, Internett-innstillinger for gjeldende konto, nøyaktig feilhistorikk for tjenesten og om WinHTTP Auto-Proxy er klar. Ufullstendige bevis går foran. Proxykonfigurasjon og historiske feil kan ikke gi grunnlag for gjenoppretting av tjenesten eller oppdaging. En stoppet tjeneste med manuell oppstart kan være normalt. Når nøyaktig dette verktøyet er valgt, kan valgfri gjenoppretting bare starte den aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende programmer kan gjenoppta WPAD- eller PAC-oppdaging med gjeldende innstillinger. Proxydefinisjoner, PAC-URL-er, påloggingsinformasjon, nettverksinnstillinger, brannmurkonfigurasjon, policy, tjenestetillatelser og oppstartstype bevares. Ingen URL hentes, og ingen proxy- eller nettverksinnstilling endres. Verifiseringen dekker bare stabil tjenestetilstand, ikke oppdaging, om noe kan nås, godkjenning, policyeierskap eller bruk i programmer.

### Windows-tilkoblingsbehandling

Ser avgrenset over gjeldende tildeling av nettverksprofiler, nøyaktig feilhistorikk for tjenesten og om Windows-tilkoblingsbehandling er klar. Ufullstendige bevis går foran. Offentlige profiler og historiske feil forblir kontekst og kan ikke gi grunnlag for gjenoppretting eller en tilkoblingsbeslutning. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Windows kan gjenoppta automatiske tilkoblingsbeslutninger med eksisterende profiler og policy. Adaptere, profiler, påloggingsinformasjon, innstillinger for målt tilkobling, kostnad, roaming, brannmur, VPN og godkjenning samt oppstartstype bevares. Ingen profil, adapter eller innstilling endres. Verifiseringen dekker bare stabil tjenestetilstand, ikke profilenes anvendelighet, valg av tilkobling, godkjenning, ruting, navneoppslag eller tilkobling.

### Nettverksoppsettjeneste

Ser avgrenset over gjeldende protokollbindinger for adaptere, nøyaktig feilhistorikk for tjenesten og om Nettverksoppsettjeneste er klar. Ufullstendige bevis går foran. Bindingstilstand og historiske feil kan ikke gi grunnlag for gjenoppretting av tjenesten eller en oppsettendring. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende ventende nettverksoppsett kan fortsette etter Windows-policy. Adaptere, drivere, driverpakker, bindinger, adresser, ruter, DNS-innstillinger, profiler, påloggingsinformasjon, brannmurkonfigurasjon, policy og oppstartstype bevares. KillerBench velger ingen driver- eller konfigurasjonsendring. Verifiseringen dekker bare stabil tjenestetilstand, ikke tilgjengelige adaptere, egnede drivere, fullført oppsett, adressetildeling, ruting, navneoppslag eller tilkobling.

### Fildeling (SMB)

Kontrollerer om Windows SMB-server er tilgjengelig. Valgfri gjenoppretting starter bare en aktivert og stoppet Server-tjeneste etter nye avhengighetskontroller. Eksisterende fil- og skriverdelinger kan bli tilgjengelige igjen med gjeldende tillatelser og brannmurpolicy. Ingen deling, tillatelse, SMB-innstilling, brannmurregel, påloggingsinformasjon, policy eller oppstartstype endres. Verifiseringen dekker bare stabil tjenestetilstand, ikke klienttilgang eller filoverføring.

### Tilkoblingsbehandling for ekstern tilgang

Ser avgrenset over VPN-profilkonfigurasjon for gjeldende konto, nøyaktig feilhistorikk for tjenesten og om Tilkoblingsbehandling for ekstern tilgang er klar, uten å samle inn hemmeligheter. En tom oversikt kan være tilsiktet, og at profiler finnes, tilkoblingsetiketter eller historiske feil kan ikke gi grunnlag for gjenoppretting eller et tilkoblingsforsøk. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende oppringings- og VPN-forespørsler kan fortsette. Tilkoblingsprofiler, påloggingsinformasjon, ruter, adaptere, godkjenningsinnstillinger, policy og oppstartstype bevares. Ingen tilkobling startes. Verifiseringen dekker bare stabil tjenestetilstand, ikke profilenes anvendelighet, om serveren kan nås, godkjenning, tunnelforhandling, ruteinstallasjon, navneoppslag eller tilkobling.

### Eksternt skrivebord-tjenester

Ser over lokal aktivering av Eksternt skrivebord, godkjenning på nettverksnivå og lytteport, avgrenset tilkoblings- og økthistorikk, nøyaktig feilhistorikk for tjenesten og om tjenesten er klar. Ufullstendige bevis går foran. Historiske oppføringer kan ikke gi grunnlag for gjenoppretting av tjenesten eller konfigurasjonsendringer. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. En eksisterende lytter kan bli tilgjengelig igjen etter gjeldende policy, NLA, brannmurregler og tillatelser. Aktivering av Eksternt skrivebord, lyttere, sertifikater, porter, brannmurregler, NLA, tillatelser, økter, policy, tjenestetillatelser og oppstartstype bevares. Ingen lytter, brannmurregel, kontotilgang, sertifikat eller økt opprettes eller endres. Verifiseringen dekker bare stabil tjenestetilstand, ikke gjeldende policy, lytterbinding, tilgang gjennom brannmuren, sertifikatidentitet, tillatelser, godkjenning, om nettverket kan nås, vellykket tilkobling eller oppretting av økter.

### Windows Remote Management

Ser avgrenset over konfigurasjonen av WinRM-lyttere, driftshistorikk, nøyaktig feilhistorikk for tjenesten og om tjenesten er klar. Ufullstendige bevis går foran. Deaktiverte lyttere eller gjeldende tjenesteproblemer krever gjennomgang, mens historiske oppføringer ikke kan gi grunnlag for gjenoppretting eller konfigurasjonsendringer. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende WS-Management-lyttere kan bli tilgjengelige igjen med gjeldende innstillinger for godkjenning, sertifikater, brannmur, TrustedHosts og policy. Lyttere, adresser, porter, sertifikater, godkjenning, kryptering, TrustedHosts, brannmurregler, tillatelser, kvoter, policy, tjenestetillatelser og oppstartstype bevares. Ingen lytter, brannmurregel, klareringsoppføring, sertifikat, påloggingsinformasjon eller ekstern økt opprettes eller endres. Verifiseringen dekker bare stabil tjenestetilstand, ikke gjeldende policy, lytterbinding, tilgang gjennom brannmuren, sertifikatgyldighet, godkjenning, tillatelser, om klienter kan nå tjenesten eller ekstern kommandokjøring.
