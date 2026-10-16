# Vedlikehold

Vedlikehold holder maskinen ryddig: filopprydding, gamle brukerprofiler, planlagte oppgaver og rydding i Windows. Hvert verktøy forhåndsviser hva det vil fjerne, før noe slettes.

Trinnvise veiledninger: [Frigjør diskplass](free-disk-space.html) og [Gjør Windows mer privat](make-windows-private.html).

## Verktøy i denne seksjonen

Rydd i filer, profiler og planlagte oppgaver, og rydd i Windows.

### Opprydding

Beregner valgte mål for midlertidige filer, hurtigbuffere og bevarte diagnosebevis ved hjelp av innstillingene for alder og filgrense i hovedpanelet. Windows Temp og Windows SystemTemp er uavhengige mål. Delivery Optimization er et eget hurtigbuffervalg, og fjernet innhold kan måtte lastes ned på nytt. Valgfri permanent fjerning bruker den separate oppryddingsboksen, kontrollerer bevarte kandidater innenfor valgt grense på nytt og registrerer hvert utfall. Slettingen går utenom papirkurven. Mål, alder og filgrenser kan også konfigureres med CLI-kommandoen cleanup.

### Profiler

Ser over Windows-profiler ved hjelp av valgt aldersgrense, nøyaktige ProfileList-registreringer og eventuelle kontoavhengigheter for tjenester og planlagte oppgaver, og gir én avgrenset gjennomgangskonklusjon. Innlastede profiler, systemprofiler og profilen til gjeldende konto er utelatt fra gjennomgangen. Kandidater forblir bare til gjennomgang. Ingen profil, mappe, registernøkkel, konto, tjeneste, oppgave, påloggingsinformasjon, policy eller fil endres, og ingen fjerning velges. Avhengigheter i programmer og om sikkerhetskopier kan gjenopprettes, forblir uverifisert.

### Planlagte oppgaver

Lager en oversikt over synlige planlagte oppgaver, deres tilstander og rapporterte kontohavere. Ingen oppgaver startes, registreres eller endres. Handlinger, utløsere og om kjøringen lykkes, verifiseres ikke.

### Rydd i Windows

Ser over eksplisitte oppryddingsområder i Windows, som Recall og KI, telemetri, søkeforslag, forbrukerinnhold, oppgavelinje og Start, varsler, Edge, strøm, dvalemodus, Office-språk og forhåndsinstallert programvare, for kontoen som kjører eller for alle profiler. Gjennomgangen er skrivebeskyttet. Strømplanen Høy ytelse, tidsavbrudd for hvilemodus og skjerm samt dvalemodus kan hver for seg endres når området er det eneste som er valgt, med ferske bevis og eksplisitt bekreftelse.
