# Lagring

Undersøker disker, volumer, Lagringsplasser og tjenester for stasjonsvedlikehold. Diagnosen velger ingen diskoperasjon; enhver støttet handling som endrer maskinen, trenger sitt eget nøyaktige mål, sine forutsetninger og sin bekreftelse.

Trinnvise veiledninger: [Kontroller og reparer en stasjon](check-a-drive.html) og [Frigjør diskplass](free-disk-space.html).

## Verktøy i denne seksjonen

### Lagring

Kartlegger avgrenset identiteten til disker, partisjoner og volumer med stasjonsbokstav, vurderer presset ved 10 % ledig plass på hvert volum med stasjonsbokstav, undersøker tildeling av VSS-skyggelagring og standardkvoter per volum, oppsummerer gjeldende fremdrift for jobber i Storage-modulen, kontrollerer valgt NTFS-volum og tilstanden til selvreparasjon på nett, og undersøker disk- og kontrollerenheter på denne PC-en. Kapasitetsterskler, kvoteinnstillinger og jobbtilstander er bevis til gjennomgang og velger ikke opprydding, endring av størrelse, kvoteendringer eller jobbstyring. Tilordningen av stasjonsbokstaver gjelder et bestemt tidspunkt og dekker ikke alle monteringsbaner. Enhetsfunn kobles ikke til det valgte volumet. Windows-volumet er standard. Ingen filer, øyeblikksbilder, jobber eller drivere endres, ingen innstilling endres, ingen volumer kobles fra, og ingen omstart planlegges.

### Lagringsplasser

Lager en oversikt over ikke-primordiale lagringsutvalg i Lagringsplasser og virtuelle disker, inkludert tilstand, driftsstatus, kapasitet, tildeling og robusthet, og gir én avgrenset driftskonklusjon. Windows kan starte administrasjonsleverandøren for Lagringsplasser ved behov mens den svarer på denne skrivebeskyttede spørringen. Utvalg, virtuelle disker, fysiske disker, volumer, robusthet, tildeling, vedlikeholdsinnstillinger, policy og oppstartstype bevares. Ingen operasjon for utvalg, disk, volum, tildeling, tilkobling, reparasjon, optimalisering eller innstilling forespørres. Leverandørresultater verifiserer ikke tilgjengeligheten til utvalget, redundans, dataintegritet, om sikkerhetskopier kan gjenopprettes eller I/O i programmer.

### Lagringstjeneste

Ser avgrenset over gjeldende jobber i Storage-modulen, nøyaktig feilhistorikk for tjenesten og om Lagringstjeneste er klar. Ufullstendige bevis går foran. Jobbtilstand og historiske feil kan ikke gi grunnlag for gjenoppretting av tjenesten eller lagringsendringer. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende lagringsvarsler og policystyrt lagringsaktivitet kan fortsette. Volumer, partisjoner, filsystemer, monteringspunkter, enheter, drivere, innstillinger for Lagringsassistent, vedlikeholdspolicy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ingen operasjon for jobb, volum, partisjon, monteringspunkt, fil, enhet, driver eller lagringsinnstilling. Verifiseringen dekker bare stabil tjenestetilstand, ikke jobbutfall eller operasjoner på enhet, volum eller lagringspolicy.

### Tjeneste for optimalisering av stasjoner

Kontrollerer om tjenesten for optimalisering av stasjoner er tilgjengelig. Valgfri gjenoppretting starter bare den nøyaktige aktiverte og stoppede tjenesten etter nye avhengighetskontroller. Eksisterende optimaliseringsaktivitet planlagt av Windows kan fortsette etter gjeldende vedlikeholdspolicy. Volumer, filsystemer, data, optimaliseringsplaner, vedlikeholdspolicy, tjenestetillatelser og oppstartstype bevares. KillerBench velger ikke et volum og analyserer, trimmer, defragmenterer, optimaliserer eller planlegger ingen stasjon. Verifiseringen dekker bare stabil tjenestetilstand, ikke analyse, optimalisering eller enhetens tilstand.
