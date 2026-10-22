# Kommandolinje

`killerbench-cli` kjører samme Engine som skrivebordsappen fra PowerShell eller en ekstern konsoll. Alle kommandoer som kan endre maskinen, viser en forhåndsvisning som standard og krever et eksplisitt flagg som `--execute` før de handler. Kommandoene skriver JSON-bevis til standard utdata og fremdrift til standard feil.

Trinnvise veiledninger: [Kjør KillerBench fra skript](use-the-cli.html).

Avslutningskode 0 betyr at forhåndsvisningen eller det verifiserte resultatet lyktes, 1 betyr en oppstarts-, inndata- eller journalfeil, 2 betyr at argumentene ikke ble gjenkjent, 3 betyr blokkert, mislykket eller ikke verifisert, 4 betyr at en arbeidsflyt trenger en omstart før den kan verifisere, og 130 betyr at du stoppet den.

Kjør `killerbench-cli` uten argumenter for å skrive ut denne listen på din egen maskin.

## Oppdateringer og vedlikehold

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Nøyaktig gjennomgang av en overføring eller observasjon med to øyeblikksbilder; ingen nye forsøk.
- `bits-jobs [--all-users]`: Tilstander, eiere og byteantall for BITS-jobber; ingen endringer i overføringer.
- `bits-retry JOB_GUID [--execute]`: Forhåndsvisning som standard; eksplisitt nytt forsøk for en nøyaktig jobb, uten påstand om at programmet er ferdig.
- `bits-retry-recover JOB_GUID`: To skrivebeskyttede øyeblikksbilder; arkiverer stabile utfall uten nye forsøk.
- `component-repair-source`: Administrert policy for DISM-reparasjonskilde og konflikter; ingen endringer.
- `delivery-optimization`: Skrivebeskyttet tilstand for tjenesten Leveringsoptimalisering; JSON-bevis.
- `delivery-optimization-activity`: Personvernbegrenset gjeldende antall jobber og bytetotaler; ingen overføringsidentiteter eller endringer.
- `delivery-optimization-policy`: Skrivebeskyttede klassiske innstillinger og Policy CSP-innstillinger; sensitive verdier og leverandøridentiteter utelatt.
- `feature-service-events`: Leser nøyaktig feilhistorikk for tjenestene for tid, søk, WMI, hendelseslogg og lyd.
- `feature-update-policy`: Skrivebeskyttet policy for målversjon, utsettelse, pause og sikkerhetstiltak; JSON-bevis.
- `pending-file-operations`: Skrivebeskyttet, personvernbegrenset oversikt over ventende filoperasjoner; JSON-bevis.
- `restart-check`: Skrivebeskyttede kilder til ventende omstart; JSON-bevis.
- `servicing-logs`: Avgrenset analyse av nyeste CBS-logg, CBS-arkiver i ren tekst og komprimert form, samt DISM- og Windows Setup-logger; ingen reparasjon.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Skrivebeskyttede øyeblikksbilder av vedlikeholdsaktivitet.
- `servicing-service-events`: Skrivebeskyttet, nøyaktig feilhistorikk for vedlikeholdskjeden i Windows Update.
- `servicing-service-states`: Skrivebeskyttet gjeldende tilstand for de seks tjenestene i vedlikeholdskjeden i Windows Update.
- `setup-code DECIMAL_OR_0xHEX`: Dokumentert referanse for resultatkoder i Windows Setup; ingen undersøkelse av maskinen eller reparasjon.
- `update-applicability`: Skrivebeskyttede bevis for Windows-utgave, versjon og arkitektur.
- `update-cache`: Avgrensede, skrivebeskyttede totaler for SoftwareDistribution-hurtigbufferen; ingen filnavn.
- `update-code DECIMAL_OR_0xHEX`: Dokumentert referanse for resultatkoder i Windows Update; ingen undersøkelse av maskinen eller reparasjon.
- `update-context`: Skrivebeskyttede bevis for gjeldende utvidede rettigheter og skrivebordsøkt; ingen kontoidentifikatorer.
- `update-engine-files`: Skrivebeskyttede bevis for tilstedeværelse, versjon og signatur for den native Windows Update-motoren.
- `update-events`: Skrivebeskyttet historikk for Windows Update-klienten fra de siste sju dagene; JSON med rå bevis.
- `update-followup DECIMAL_OR_0xHEX`: Ferske, skrivebeskyttede diagnoser valgt etter en dokumentert resultatkode i Windows Update.
- `update-history`: Skrivebeskyttede utfall for de nyeste 100 Windows Update-operasjonene; JSON-bevis.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: Gjenta `--select`; forhåndsvisning som standard.
- `update-policy`: Skrivebeskyttet administrert policy for Windows Update; JSON-bevis.
- `update-policy-surfaces`: Skrivebeskyttede bevis for klassisk konfigurasjonsflate og Policy CSP; verdier og leverandøridentiteter utelatt.
- `update-recover`: Kontrollerer en ventende oppdatering; installerer eller prøver den aldri på nytt.
- `update-restart-policy`: Skrivebeskyttede frister for samsvar, utsettelsesperioder, undertrykking av omstart og varslingsatferd.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: Søk og treff i JSON; ingen installasjon.
- `update-service-events`: Skrivebeskyttet, nøyaktig feilhistorikk for tjenesten Windows Update.
- `update-service-files`: Skrivebeskyttede, nøyaktige bevis for registrering og signatur for DLL-en til tjenesten Windows Update.
- `update-services`: Skrivebeskyttede registreringer av Windows Update-tjenester; JSON-bevis.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Krever et ferskt søk; forhåndsvisning som standard; beholder det gamle lageret.
- `update-store-reset-recover`: Avstemmer en forberedt tilbakestilling av lageret uten å gjenta den.
- `windows-capabilities`: Skrivebeskyttede tilstander for funksjonaliteter i kjørende Windows; JSON-resultat.
- `windows-update-task`: Skrivebeskyttet, nøyaktig tilstand og kjørehistorikk for oppgaven Scheduled Start; ingen oppgavekjøring eller oppdateringsoperasjon.

## BitLocker og TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Forhåndsvisning som standard; slår på automatisk opplåsing for et kryptert datavolum.
- `bitlocker-auto-unlock-recover`: Verifiserer en avbrutt forespørsel om automatisk opplåsing uten å gjenta den.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Forhåndsvisning som standard; bare en ny sikkerhetskopifil for gjenoppretting.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Verifiserer en avbrutt sikkerhetskopiering uten å skrive den på nytt.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Forhåndsvisning som standard; starter dekryptering, for eksempel før ny kryptering med en påkrevd metode.
- `bitlocker-decrypt-recover`: Verifiserer en avbrutt dekrypteringsforespørsel uten å gjenta den.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Forhåndsvisning som standard; krever eksisterende TPM- og gjenopprettingsbeskyttere.
- `bitlocker-encrypt-recover`: Verifiserer ventende konverteringstilstand uten å gjenta forespørselen.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Forhåndsvisning som standard; sikkerhetskopierer et eksisterende gjenopprettingspassord til Active Directory eller Microsoft Entra ID.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Bare forhåndsvisning av alternativer.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Forhåndsvisning som standard; legger til én TPM-beskytter eller én beskytter med gjenopprettingspassord; passordet skrives aldri ut.
- `bitlocker-protector-recover`: Verifiserer en avbrutt beskytterforespørsel uten å gjenta den.
- `bitlocker-protectors DRIVE:`: Bare beskytteridentiteter og typer; ingen gjenopprettingspassord.
- `bitlocker-resume DRIVE: [--execute]`: Forhåndsvisning som standard; bare et nøyaktig, fullstendig kryptert volum med beskyttelse på pause.
- `bitlocker-resume-recover`: Verifiserer ventende beskyttelsestilstand uten å gjenta forespørselen.

## Sikkerhet og personvern

- `certificate-chains`: Skrivebeskyttet lokal kjedetillit bare fra hurtigbuffer for personlige sertifikater; ingen tilbakekalling eller nedlastinger.
- `defender-overview [--report]`: Skrivebeskyttet oversikt over beskyttelse, skanningstider og samlede trusler; ingen handling.
- `defender-protection [--report]`: Skrivebeskyttede beskyttelsesinnstillinger og gjenkjente policyflater; sensitive detaljer utelatt; ingen endringer.
- `defender-scan Quick|Full [--execute] [--report]`: Forhåndsvisning som standard; én eksplisitt, beskyttet Microsoft Defender-skanning.
- `defender-scan-history [--report]`: Skrivebeskyttede tider for hurtigskanning og fullstendig skanning og status for forsinkelse; sensitive detaljer utelatt; ingen skanning.
- `defender-scan-recover [--report]`: Skrivebeskyttet avstemming uten å starte en ny skanning.
- `defender-threats [--report]`: Samlet trusselhistorikk, gjeldende aktive flagg og alvorlighets-ID-er; sensitive detaljer utelatt; ingen handling.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Forhåndsvisning som standard; bare ikke-administrerte lokale profiler.
- `firewall-enable-recover`: Undersøker en ventende endring uten å gjenta den.
- `firewall-events`: Leser avgrenset driftshistorikk med advarsler, feil og kritiske hendelser fra Windows-brannmuren.
- `firewall-logging`: Leser konfigurasjonen for trafikklogging i profilene Domene, Privat og Offentlig uten å åpne loggene.
- `firewall-rules`: Leser personvernbegrensede antall gjeldende regler og policykilder.
- `firewall-service-events`: Leser nøyaktig feilhistorikk for BFE og Windows Defender-brannmur i Tjenestekontrollbehandling.
- `privacy-clipboard-history on|off [--execute]`: Forhåndsvisning som standard; nøyaktig verdi for gjeldende bruker med policyen bevart.
- `privacy-clipboard-history-recover`: Kontrollerer en avbrutt endring på nytt uten å gjenta den.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: Beskyttet gjenoppretting av forrige verdi.
- `privacy-plan --select Camera --select ClipboardSync`: Ser bare over valgene; ingen innstillinger endres.
- `privacy-setting KEY on|off [--execute]`: Forhåndsvisning som standard; policybevarende endring for gjeldende bruker; ugyldige nøkler viser de støttede valgene.
- `privacy-setting-recover KEY`: Kontrollerer en avbrutt endring på nytt uten å gjenta den.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: Beskyttet gjenoppretting av nøyaktig verdi.
- `security-check`: Skrivebeskyttet TPM, Sikker oppstart, virtualiseringsbeskyttelse og UAC-policy; JSON-bevis.

## Tjenester

- `service-binaries`: Skrivebeskyttet tilstedeværelse av lokale tjenestebinærfiler og Authenticode-status; stier utelatt, og ikke-lokale stier kontaktes ikke.
- `service-check NAME [NAME ...] | service-check --all`: Skrivebeskyttede tilstander for støttede tjenester og bevis for avhengigheter.
- `service-dependencies`: Skrivebeskyttet gjennomgang av integriteten i direkte avhengigheter; ingen tjenesteendringer.
- `service-dlls`: Skrivebeskyttet tilstedeværelse av registrerte tjeneste-DLL-er og Authenticode-status; stier utelatt, og ikke-lokale stier kontaktes ikke.
- `service-events`: Skrivebeskyttet feilhistorikk for tjenester; JSON med rå bevis.
- `time-events`: Skrivebeskyttet nylig hendelseshistorikk for Windows-tid med JSON-bevis.
- `service-logons`: Skrivebeskyttet gjennomgang av kontoklasse og skrivebordsinteraksjon for tjenester; kontoidentiteter utelatt.
- `service-path-quoting`: Skrivebeskyttet gjennomgang av anførselstegn i stier til tjenesters kjørbare filer; fullstendige stier og argumenter utelatt.
- `service-recovery-policy`: Skrivebeskyttet oversikt over gjenopprettingshandlinger for installerte tjenester; kommandoer og omstartsmeldinger utelatt.
- `service-startup-config`: Skrivebeskyttet gjennomgang av sammenhengen i oppstartsmoduser; ingen tjenesteendringer.
- `service-transitions`: Skrivebeskyttet gjentatt observasjon av ventende tjenestetilstander; ingen tjenestehandling.
- `service-triggers`: Skrivebeskyttet oversikt over utløsere for installerte tjenester; nyttelast for utløsere utelatt; ingen endringer.

## Lagring, filer og sikkerhetskopiering

- `backup-events`: Historikk for VSS og volsnap; JSON-bevis; ingen endringer.
- `backup-service-events`: Leser nøyaktig feilhistorikk for VSS, programvareleverandøren for skyggekopier, Windows Backup og motoren for blokknivåkopiering.
- `backup-summary`: Oversikt over lokale Windows Backup-operasjoner; ingen endringer i sikkerhetskopier.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Forhåndsvisning som standard; papirkurven er alt eller ingenting, og alder gjelder ikke.
- `disk-cleanup-task`: Skrivebeskyttet, nøyaktig oppgavetilstand og kjørehistorikk; ingen oppgavekjøring eller opprydding.
- `disk-diagnostic`: Sammenholder nøyaktige diskdiagnoseoppgaver med hendelseshistorikk; ingen oppgave- eller diskhandling.
- `disk-diagnostic-tasks`: Skrivebeskyttet, nøyaktig historikk for innsamlings- og løseroppgaver; ingen oppgave- eller diskhandling.
- `disk-quotas`: Skrivebeskyttede standardkvoter og kvotetilstand per volum; brukeridentiteter utelatt; ingen endringer.
- `file-history-events`: Personvernavgrensede antall advarsler og feil fra Filhistorikk; ingen endringer.
- `file-history-status`: Beskyttelsestilstand for Filhistorikk for gjeldende konto; ingen endringer.
- `search-index-rebuild [--execute]`: Forhåndsvisning som standard; beholder forrige indeks og verifiserer fersk tilstand.
- `search-index-rebuild-recover`: Skrivebeskyttet avstemming uten å gjenta reparasjonskommandoer.
- `storage-check DRIVE_LETTER`: Skrivebeskyttet fast NTFS-kontroll; JSON-resultater og fremdrift i sanntid.
- `storage-events`: Skrivebeskyttet lagringshistorikk; JSON med rå bevis.
- `storage-jobs`: Skrivebeskyttet samlet tilstand og fremdrift for jobber i Storage-modulen; JSON; ingen endringer.
- `storage-reliability`: Skrivebeskyttede tellere for temperatur, slitasje og I/O; JSON; ingen reparasjon.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Forhåndsvisning som standard; kjøring krever bekreftet sikkerhetskopi; systemvolumer planlegges til omstart.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Bare forhåndsvisning; ingen reparasjon eller planlegging.
- `storage-repair-recover`: Verifiserer en fullført eller planlagt filsystemreparasjon uten å gjenta den.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Samler inn og binder gjeldende bevis; ingen reparasjon eller planlegging.
- `storage-sense-policy`: Skrivebeskyttet gjenkjent oppryddingspolicy for maskinen; brukerinnstillinger og filidentiteter utelatt.
- `storage-sense-task`: Skrivebeskyttet, nøyaktig oppgavetilstand og kjørehistorikk; ingen oppgavekjøring eller opprydding.
- `storage-service-events`: Leser nøyaktig feilhistorikk for Lagringstjeneste, Optimaliser stasjoner, VSS og programvareleverandøren for skyggekopier.
- `volume-capacity DRIVE:`: Skrivebeskyttet ledig plass med JSON-bevis.
- `volume-dirty DRIVE:`: Skrivebeskyttet status for dirty-biten; ingen reparasjon eller omstart.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Skrivebeskyttet analyse som standard; krever et nøyaktig sunt volum og kompatible medier.
- `volume-optimize-recover`: Verifiserer en ventende fullført operasjon uten å gjenta den.
- `vss-providers`: Skrivebeskyttede navn, versjoner og typer for VSS-leverandører; identifikatorer utelatt; ingen registreringsendringer.
- `vss-writers`: Rapporterte tilstander for VSS-writere; tolkning av engelske utdata; ingen tilbakestilling.

## Utskrift

- `print-events`: Skrivebeskyttet administrasjonshistorikk for PrintService fra de siste sju dagene; JSON-bevis.
- `print-extension-files`: Skrivebeskyttet oversikt over tilstedeværelse og signatur for binærfiler til utskriftsbehandlere og -overvåkere; fullstendige stier utelatt.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Forhåndsvisning som standard; bare en nøyaktig jobb med Feil eller Blokkert.
- `print-job-cancel-recover [--folder PATH]`: Kontrollerer en avbrutt kansellering på nytt uten å gjenta den.
- `print-job-list`: Viser gjeldende synlige jobber uten å endre dem.
- `print-operational-events`: Skrivebeskyttet tilstand for driftskanalen til PrintService og tilgjengelig historikk; JSON-bevis.
- `print-policy`: Skrivebeskyttede Point and Print-begrensninger og policy for driverinstallasjon; JSON-bevis.
- `print-rendering-policy`: Skrivebeskyttet policy for gjengivelse og driverisolering; JSON-bevis.
- `print-spooler-events`: Skrivebeskyttede feil i Tjenestekontrollbehandling knyttet til den lokale identiteten til Utskriftskø; JSON-bevis.
- `printer-check --queue NAME [--queue NAME ...]`: Skrivebeskyttede innstillinger for valgt lokal kø.
- `printer-conditions`: Skrivebeskyttede tilstandskoder for lokale køer; ingen fysisk verifisering eller endringer.
- `printer-driver-files`: Skrivebeskyttet oversikt over registrerte filer og signaturer for kjørbare filer; fullstendige stier utelatt.
- `printer-driver-models`: Skrivebeskyttet oversikt over versjon 3, versjon 4 og pakkebevissthet; JSON-bevis.
- `printer-drivers`: Skrivebeskyttet oversikt og samsvar for kø- og driverregistreringer; JSON-bevis.
- `printer-endpoint-check`: Avgrensede tilkoblinger til nøyaktige tildelte Standard TCP/IP-skriverendepunkter; ingen oppdaging, SNMP eller utskriftsdata.
- `printer-list`: Finner lokale køer uten å endre innstillinger.
- `printer-ports`: Skrivebeskyttet samsvar mellom kø- og portregistreringer; JSON-bevis.
- `printer-queue-attributes`: Skrivebeskyttede attributter for køing, oppbevaring og deling for lokale køer.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Forhåndsvisning som standard; når frakoblet modus slås av, kan ventende jobber bli sluppet løs.

## Nettverk

- `dhcp-check`: Skrivebeskyttet kvalifisering for manglende eller utløpte DHCP-leaser; JSON-bevis, ingen fornyelse.
- `dhcp-events`: Skrivebeskyttet hendelseshistorikk for DHCP-klient for IPv4 og IPv6; JSON-resultat.
- `dhcp-leases`: Skrivebeskyttede bevis for tidsstempel, utløp og server for leasen; JSON-resultat.
- `dhcp-renew [--execute]`: Forhåndsvisning som standard; bare nøyaktige manglende eller utløpte DHCP-leaser på tilkoblede fysiske adaptere.
- `dhcp-renew-recover`: Skrivebeskyttet avstemming uten å be om en ny lease.
- `dns-cache HOST [--execute]`: Forhåndsvisning som standard; beskyttet tømming av lokal hurtigbuffer med verifisering av nøyaktig vert.
- `dns-cache-recover`: Kontrollerer en avbrutt hurtigbufferoperasjon på nytt uten å gjenta tømmingen.
- `dns-cache-summary`: Personvernbegrenset status for lokal DNS-hurtigbuffer og antall per oppføringstype; ingen endringer.
- `dns-check HOST [--timeout-seconds 1..60]`: Avgrenset navneoppslag i systemet; JSON-resultat.
- `dns-client-config`: Skrivebeskyttede bevis for suffiks, registrering, devolusjon og policy; JSON-resultat.
- `dns-client-events`: Skrivebeskyttet, personvernbegrenset historikk for advarsler og feil fra DNS-klient; JSON-resultat.
- `dns-nrpt-policy`: Skrivebeskyttede bevis for gjeldende DNS-navneområdepolicy; JSON-resultat.
- `ip-addresses`: Skrivebeskyttet gjennomgang av tilstand, opprinnelse og konflikter for gjeldende IP-adresser; JSON-bevis.
- `ip-interface-config`: Skrivebeskyttede bevis for konfigurasjon av IP-grensesnitt; JSON-resultat.
- `network-firewall`: Sammenholder aktive nettverkstildelinger med aktivering av tilsvarende brannmurprofil.
- `network-offload`: Skrivebeskyttet global konfigurasjon for nettverksavlasting i Windows; JSON-resultat.
- `network-profiles`: Skrivebeskyttede gjeldende profilkategorier og tilkoblingsklassifiseringer; nettverksnavn utelatt.
- `network-service-events`: Leser nøyaktig feilhistorikk for DHCP, DNS, Arbeidsstasjon, NLA, Nettverkslistetjeneste og IP-hjelper.
- `proxy-check`: Skrivebeskyttede bevis for lagret WinHTTP-proxy og Internett-proxy for gjeldende konto.
- `route-priority`: Skrivebeskyttet sammenligning av gjeldende metrikk for standardrute; JSON-bevis.
- `smb-client-config`: Skrivebeskyttet konfigurasjon for SMB-signering, gjestetilgang og transport.
- `smb-connections`: Skrivebeskyttet aktiv protokollforhandling; eksterne identiteter utelatt.
- `smb-server-config`: Skrivebeskyttet konfigurasjon for SMB-protokoll, signering, kryptering og flerkanal.
- `smb-shares`: Skrivebeskyttet, avgrenset oversikt over SMB-delinger; stier, tillatelser, økter og kontoer utelatt.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Avgrenset tilkoblingskontroll; ingen appdata sendes.
- `tcp-port-pressure`: Skrivebeskyttede dynamiske TCP-områder og samlet bruk av lokale porter; endepunkter og prosesser utelatt.
- `tcp-settings`: Skrivebeskyttede bevis for konfigurasjon av TCP-maler i Windows; JSON-resultat.
- `vpn-profiles`: Skrivebeskyttet konfigurasjon av VPN-profiler for gjeldende konto; påloggingsinformasjon utelatt.
- `winrm-listeners`: Skrivebeskyttet oversikt over registrering av WinRM-lyttere; sertifikatavtrykk utelatt.
- `winsock-catalog`: Skrivebeskyttet struktur for den native Winsock-protokollkatalogen; JSON-resultat.
- `winsock-namespaces`: Skrivebeskyttet struktur for native Winsock-navneområdeleverandører; JSON-resultat.
- `winsock-provider-files`: Skrivebeskyttet tilstedeværelse og signaturer for filer til protokolleverandører; JSON-resultat.
- `winsock-readiness`: Skrivebeskyttet beredskap for native Winsock-protokoller, navneområdeleverandører og leverandørfiler; JSON-resultater.

## Apper

- `app-folder-review FOLDER_NAME [--report]`: Gjennomgang av nøyaktige mapper med fast rot; ingen sletting.
- `app-package-review FULL_NAME [--all-users]`: Nøyaktig mål og avhengige pakker; ingen fjerning.
- `app-packages [--all-users]`: Bare oversikt over registrerte Appx/MSIX-pakker; ingen fjerning.
- `browser-extensions`: Gjennomgang av identiteter for Chrome-, Edge-, Brave- og Firefox-utvidelser; ingen fjerning.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Bare gjennomgang; ingen systemendringer.
- `depot-review [application and configuration selections] [--json]`: Bare gjennomgang; ingen klargjøring.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Skrivebeskyttet kontroll av Authenticode og leverandørens signatar; ingenting kjøres.
- `foxit-inventory`: Skrivebeskyttet produktoversikt for maskinen; JSON-utdata.
- `installed-apps [--current-user]`: Avinstalleringsregistreringer og funksjonsflagg; ingen fjerning.
- `installed-apps-compare BEFORE.json AFTER.json`: Forskjeller i registreringer mot en lagret oversikt; ingen endringer på maskinen.
- `installed-hotfixes`: Skrivebeskyttet, avgrenset oversikt over installerte Windows-hurtigreparasjoner; JSON-bevis.
- `installer-activity`: Skrivebeskyttede bevis for Installer-prosesser og transaksjonsmarkører; JSON-bevis.
- `installer-check`: Skrivebeskyttet Installer-policy og motorberedskap; JSON-bevis.
- `installer-engine-files`: Skrivebeskyttet oversikt over tilstedeværelse og Authenticode for påkrevde Installer-filer; JSON-bevis.
- `installer-events`: Skrivebeskyttet MSI-historikk fra de siste sju dagene; JSON-bevis.
- `installer-logging`: Skrivebeskyttet policy for logging og feilsøking i Installer; JSON-bevis.
- `installer-pending-state`: Skrivebeskyttet sammenholding av ventende filoperasjoner og Installer-aktivitet; JSON-bevis.
- `installer-registration-repair [--execute]`: Forhåndsvisning som standard; bare en nøyaktig feil i tjenesteregistreringen; avregistrerer aldri motoren.
- `installer-registration-repair-recover`: Kontrollerer en avbrutt registrering på nytt uten å gjenta kommandoer.
- `installer-security-policy`: Skrivebeskyttet sikkerhets- og oppdateringspolicy for Installer; JSON-bevis.
- `installer-service-events`: Skrivebeskyttede feil i Tjenestekontrollbehandling knyttet til den lokale identiteten til Windows Installer; JSON-bevis.
- `installer-service-registration`: Skrivebeskyttet oversikt over registreringen av tjenesten Windows Installer; JSON-bevis.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: Fersk forhåndsvisning av nøyaktig kvalifisering; ingen fjerningsleverandør.
- `mace-package-recover`: Avstemmer på nytt én avbrutt oppføring for fjerning av en apppakke; gjentar aldri fjerningen.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Bare gjennomgang; ingen fjerning.
- `msi-cache`: Skrivebeskyttet synlighet for den registrerte Windows Installer-hurtigbufferen; JSON-bevis.
- `msi-code NONNEGATIVE_DECIMAL`: Dokumentert referanse for returkoder i Windows Installer; ingen undersøkelse av maskinen eller reparasjon.
- `msi-product {PRODUCT-CODE}`: Skrivebeskyttet, nøyaktig tilstand for MSI-registrering; JSON-bevis.
- `msi-repair {PRODUCT-CODE} [--execute]`: Forhåndsvisning som standard; bare et nøyaktig ødelagt eller feilkonfigurert produkt.
- `msi-repair-recover`: Kontrollerer en avbrutt reparasjon av et nøyaktig produkt på nytt uten å gjenta den.
- `msi-sources`: Skrivebeskyttede, personvernbegrensede kildemetadata for registrerte Windows Installer-produkter; JSON-bevis.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Forhåndsvisning som standard; beholder nøyaktige hurtigbuffere for kontoen som kjører, under gjenopprettingsnavn.
- `orca-cache-reset-recover`: Verifiserer avbrutt oppbevaring av hurtigbuffer uten å gjenta en flytting.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Bare gjennomgang; ingen endringer i Outlook.
- `startup-apps`: Gjennomgang av oppstartsregistreringer og oppgaver ved oppstart eller pålogging; ingen deaktivering.
## System

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Nøyaktig lokalt oppslag av Bluetooth SIG-tildelinger; ingen nettverksforespørsel.
- `crash-config`: Lagret dumpmodus og dumpstier i Windows; ingen endringer.
- `crash-dumps`: Metadata for standard dumpfiler i Windows; ingen innholdsanalyse eller endringer.
- `crash-events`: Nylige feilrapporter for systemet; ingen diagnose eller endringer.
- `crash-review [--knowledge PACK_PATH ...]`: Opptil 32 pakker; samlet gjennomgang av krasj, avslutninger, avgrenset minidumpstruktur og eventuelt nøyaktig referanse for feilkontroll; ingen endringer.
- `default-printer`: Skrivebeskyttet standardvalg og innstilling for automatisk administrasjon; JSON-bevis.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Forhåndsvisning som standard; bare når standarden mangler eller er i konflikt.
- `default-printer-set-recover [--folder PATH]`: Kontrollerer et avbrutt valg på nytt uten å gjenta det.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: Skrivebeskyttede enhetsbevis med valgfrie nøyaktige driverreferanser fra opptil 32 pakker.
- `device-code DECIMAL_OR_0xHEX`: Frakoblet referanse for problemkoder i Enhetsbehandling.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Bare referansekandidater.
- `event-channel-enable CHANNEL [--execute]`: Forhåndsvisning som standard; aktiverer én nøyaktig, ikke-administrert diagnosekanal og bevarer andre innstillinger.
- `event-channel-enable-recover`: Skrivebeskyttet avstemming uten å gjenta aktiveringskommandoen.
- `event-channels`: Skrivebeskyttet tilstand, oppbevaring, størrelse og metning for fire diagnosekanaler.
- `font-registrations`: Skrivebeskyttet, avgrenset kontroll av registreringer og filer for maskinen og gjeldende bruker.
- `memory-diagnostic-results`: Skrivebeskyttet, avgrenset historikk for Windows Minnediagnostikk; JSON-bevis.
- `minidump-analyze PATH`: Avgrensede strukturelle metadata for minidumper; ingen minnesider, symboler eller endringer.
- `name-rename PROPOSED_NAME [--execute]`: Forhåndsvisning som standard; bare frittstående PC-er i arbeidsgruppe; starter aldri Windows på nytt.
- `name-rename-recover`: Verifiserer en ventende navneendring uten å gjenta den.
- `name-review PROPOSED_NAME`: Skrivebeskyttet gjennomgang av identitet og foreslått navn.
- `profiles --recover`: Verifiserer registrert fravær og beholder gjenopprettingsbevis; prøver aldri slettingen på nytt.
- `profiles --recover-backup`: Verifiserer og arkiverer et ventende sikkerhetskopiartefakt; prøver aldri kopieringen på nytt.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Forhåndsvisning først; beskyttet native kopiering krever eksplisitt kjøring og et lokalt NTFS-mål som ikke finnes.
- `profiles --select SID --create-backup-artifact DESTINATION`: Sammenligner en eksisterende kopiert sikkerhetskopi og oppretter det faste artefaktet; kopierer eller fjerner ingen profil.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Skrivebeskyttet gjennomgang; verifisering krever én valgt SID.
- `task-check`: Skrivebeskyttet oppgaveoversikt, klassifiserte funn og JSON-bevis.
- `task-definition TASK_FOLDER TASK_NAME`: Skrivebeskyttet, nøyaktig oppgave-XML; utilgjengelig betyr ikke fraværende.
- `task-inventory`: Skrivebeskyttede oppgaveidentiteter og tilstander; JSON-utdata.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Historisk gjennomgang av gjenoppretting; gir aldri grunnlag for nye forsøk.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Validerer og ser over et oppgaveutkast; ingen registrering.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Forhåndsvisning som standard; deaktivert definisjon for bare oppretting med Lokalt system.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Skrivebeskyttet avstemming av nøyaktig definisjon; prøver aldri på nytt og kjører aldri en oppgave.
- `task-xml PLAN.json YYYY-MM-DD`: Deaktivert gjennomgangs-XML til standard utdata; bare kalenderplaner i lokal tidssone; ingen registrering.
- `time-config`: Skrivebeskyttede bevis for modus, likemenn og policy for Windows-tid.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Forhåndsvisning som standard; egendefinert modus krever likemenn.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Bare forhåndsvisning.
- `time-recover --folder PATH`: Verifiserer og arkiverer en ventende oppføring; prøver aldri endringer på nytt.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Skrivebeskyttet; egendefinert modus krever likemenn.
- `wmi-provider`: Skrivebeskyttet lokal spørring til operativsystemleverandøren; JSON-bevis.

## Kunnskap, validering og Engine

- `knowledge PACK_PATH CONTEXT CODE`: Lokalt referanseoppslag; ingen reparasjoner kjøres.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Verifisert lokal aktivering; ingen nedlasting.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Verifiserer den aktive lokale pakken; ingen endringer.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Bare referanseendringer; ingen pakkeinstallasjon.
- `knowledge-info PACK_PATH`: Pakkemetadata og antall oppføringer per kontekst; ingen endringer.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Knytter kontekstuelle referanser til observerte koder.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Velger nyeste verifiserte bevarte pakke; ingen nedlasting.
- `knowledge-search PACK_PATH CONTEXT TEXT`: Opptil 50 treff i referansetekst; ingen reparasjoner kjøres.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Slår opp én nøyaktig identitet i opptil 32 pakker; konflikter velges aldri.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Verifiserer alle konfigurerte installerte databaser; ingen nedlasting eller aktivering.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Henter og aktiverer atomisk signerte databasekandidater fra konfigurerte HTTPS-endepunkter.
- `open <file.kbench> [--json]`: Åpner et lagret opptak på nytt og vurderer det igjen med de installerte reglene.
- `scan --output <new-file.kbench> [--json]`: Skrivebeskyttet Windows Update-opptak lagret i en ny fil.
- `validation-coverage RECORD.json [...]`: Oppsummerer native VM-bevis for gjeldende versjon; ingen diagnose eller reparasjoner.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Skrivebeskyttet scenarioplan for virtuelle engangsmaskiner; ingen diagnose eller reparasjoner.
- `validation-record RECORD.json`: Validerer én avgrenset oppføring med native VM-bevis; ingen diagnose eller reparasjoner.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Lokal VM-dekningsrapport for gjeldende versjon.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Kjører en validert, skrivebeskyttet delt alternativplan.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Bare oversikt.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Målrettet diagnosejournal; ingen reparasjoner.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Bare journalført gjennomgang.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: Valgfri nøyaktig produkttilstand tatt med i journalen.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: Valgfri diagnose av valgt mål tatt med i journalen.
- `workflow storage [--volume D:] [--json]`: Journalført, skrivebeskyttet filsystemkontroll.
- `workflow-journal FILE.json`: Historisk innhold; avslutningskode 0 betyr vellykket lesing, ikke at alt er sunt.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Viser eller validerer vertsuavhengige kontroller; ingen skanning eller reparasjon.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Bare historisk rapport; ingen kommandoer kjøres.

## Andre kommandoer

- `<analyze|investigate> <text-log-path> [--json]`: Analyserer lokale tekstbevis.
- `adapter-bindings`: Skrivebeskyttet oversikt over bindinger for kjerneprotokoller; JSON-bevis.
- `adapter-statistics`: Skrivebeskyttede tellere for byte, feil og forkastede pakker for adaptere; JSON-bevis.
- `application-events`: Skrivebeskyttede nylige Program-feil, grupperte antall og JSON-bevis.
- `applocker-policy`: Skrivebeskyttede gjeldende innsamlingsmoduser og samlede regelantall; regeldetaljer utelatt; ingen endringer.
- `common-check [--scope NAME ...]`: Alle diagnoseområder som standard; bare valgte områder når de er angitt; skrivebeskyttet JSON.
- `delete-notifications`: Skrivebeskyttet konfigurasjon for slettevarsler i NTFS og ReFS; JSON; ingen optimalisering eller innstillingsendring.
- `diagnostic-scopes [--all]`: Viser 11 anbefalte resultatgrupper, eller alle 49 skrivebeskyttede områder; ingen innsamling.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Målrettet diagnosejournal; ingen reparasjoner.
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Alle diagnoseområder; ingen reparasjon med mindre `--auto-repair` er angitt eksplisitt.
- `gateway-check`: Avgrensede ICMP-kontroller av nøyaktige konfigurerte neste standardhopp; ingen adresseskanning.
- `infrastructure-service-events`: Leser ett avgrenset, nøyaktig øyeblikksbilde av feil for tilkoblings-, ekstern administrasjons-, varslings- og kjernetjenester.
- `investigate-batch FILE [FILE ...]`: Opptil 12 filer; JSON Lines; avslutningskode 3 for mislykket eller delvis import.
- `licensing-check`: Skrivebeskyttet aktiveringstilstand for Windows uten produktnøkler; JSON-bevis.
- `mac-vendor PACK_PATH MAC_ADDRESS`: Oppslag i lokal pakke etter lengste prefiks; ingen nettverksforespørsel.
- `ncsi-policy`: Skrivebeskyttet policy for tilkoblingsklassifisering i Windows; mål for testforespørsler utelatt.
- `neighbor-cache`: Skrivebeskyttede, personvernbegrensede bevis for naboers tilgjengelighet; JSON-resultat.
- `ntfs-self-healing DRIVE:`: Skrivebeskyttet konfigurasjon for NTFS-reparasjon på nett for valgt volum; ingen reparasjon eller innstillingsendring.
- `pagefile-check`: Skrivebeskyttet oversikt over administrasjon, tildeling og bruk av sidevekslingsfilen; fullstendige stier utelatt.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Nøyaktig oppslag i lokal pakke; ingen nettverksforespørsel eller drivervalg.
- `physical-disks`: Skrivebeskyttet gjeldende tilstand for helse, drift, medier, buss og bruk; JSON; ingen reparasjon.
- `platform-service-events`: Leser ett avgrenset, nøyaktig øyeblikksbilde av feil for tjenester for programmer, identitet, profiler og enhetsstøtte.
- `previous-windows [--execute --backup-confirmed]`: Forhåndsvisning som standard; fjerner for godt muligheten til å rulle tilbake Windows via DISM.
- `previous-windows-recover`: Skrivebeskyttet avstemming uten å gjenta fjerningen av tilbakerullingen.
- `provisioned-packages`: Pakker klargjort for nye brukere i lokal Windows; ingen fjerning.
- `recipes [WORKFLOW]`: Viser installerte definisjoner eller arbeidsflytspesifikke avhengigheter og grenser; ingen kjøring.
- `repair <dism|sfc> [--execute]`: Forhåndsvisning som standard.
- `reserved-storage`: Skrivebeskyttet tilstand for reservert lagring i kjørende Windows; JSON-bevis.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Forhåndsvisning som standard; oppretter ett punkt etter nye kontroller.
- `restore-point-recover`: Verifiserer en ventende forespørsel uten å opprette et nytt punkt.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Bare forhåndsvisning.
- `shadow-copies`: Skrivebeskyttede gjeldende tilstander og attributter for VSS-øyeblikksbilder; identiteter utelatt; ingen endringer i øyeblikksbilder.
- `shadow-storage`: Skrivebeskyttet tildeling og press i VSS-differensialområdet; JSON; ingen endring av størrelse eller handling på øyeblikksbilder.
- `uac-policy`: Skrivebeskyttet UAC-aktivering og policy for forespørsel om utvidede rettigheter; ingen konto- eller legitimasjonsdata.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Nøyaktig lokalt oppslag i UEFI-registeret; ingen nettverksforespørsel eller drivervalg.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Nøyaktig oppslag i lokal pakke; ingen nettverksforespørsel eller drivervalg.
- `vitals-check`: Skrivebeskyttede øyeblikksbilder av Windows, maskinvare, konto, ressurser, batteri og fysiske disker; JSON-bevis.
- `win32-code DECIMAL_OR_0xHEX`: Frakoblet meldingsoppslag i installert Windows.
- `workflows`: Viser installerte arbeidsflytnøkler og titler; ingen skanning eller reparasjon.
