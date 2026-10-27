# Ukazna vrstica

`killerbench-cli` izvaja isti Engine kot namizna aplikacija iz okolja PowerShell ali oddaljene konzole. Vsak ukaz, ki bi lahko spremenil računalnik, privzeto prikaže predogled in pred izvedbo zahteva izrecno zastavico, kot je `--execute`. Ukazi izpišejo dokaze JSON na standardni izhod, napredek pa na standardni izhod napak.

Navodila po korakih: [Zagon programa KillerBench iz skriptov](use-the-cli.html).

Izhodna koda 0 pomeni, da je predogled ali preverjeni rezultat uspel, 1 pomeni napako zagona, vnosa ali dnevnika, 2 pomeni neprepoznane argumente, 3 pomeni blokiran, neuspešen ali nepreverjen izid, 4 pomeni, da postopek pred preverjanjem zahteva ponovni zagon, 130 pa, da ste ga ustavili.

Zaženite `killerbench-cli` brez argumentov, da ta seznam izpišete v svojem računalniku.

## Posodobitve in servisiranje

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Pregled točno določenega prenosa ali opazovanje z dvema posnetkoma; brez ponovnega poskusa.
- `bits-jobs [--all-users]`: Stanja opravil BITS, lastniki in števila bajtov; brez sprememb prenosov.
- `bits-retry JOB_GUID [--execute]`: Privzeto predogled; izrecen ponovni poskus točno določenega opravila, brez trditve o dokončanju v aplikaciji.
- `bits-retry-recover JOB_GUID`: Dva posnetka samo za branje; arhivira stabilne izide brez novega poskusa.
- `component-repair-source`: Upravljani pravilnik virov popravil DISM in spori; brez sprememb.
- `delivery-optimization`: Stanje storitve Optimizacija dostave samo za branje; dokazi JSON.
- `delivery-optimization-activity`: Trenutna števila opravil in skupna števila bajtov z omejitvami zasebnosti; brez identitet prenosov ali sprememb.
- `delivery-optimization-policy`: Klasične nastavitve in nastavitve Policy CSP samo za branje; občutljive vrednosti in identitete ponudnikov so izključene.
- `feature-service-events`: Prebere zgodovino napak točno določenih storitev Time, Search, WMI, Event Log in zvoka.
- `feature-update-policy`: Pravilniki ciljne izdaje, odloga, začasne ustavitve in varoval samo za branje; dokazi JSON.
- `pending-file-operations`: Povzetek čakajočih datotečnih operacij samo za branje z omejitvami zasebnosti; dokazi JSON.
- `restart-check`: Viri čakajočega ponovnega zagona samo za branje; dokazi JSON.
- `servicing-logs`: Omejena analiza najnovejšega CBS, besedilnih in stisnjenih arhivov CBS ter dnevnikov DISM in Windows Setup; brez popravila.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Posnetki dejavnosti servisiranja samo za branje.
- `servicing-service-events`: Zgodovina napak točno določenih storitev verige servisiranja Windows Update samo za branje.
- `servicing-service-states`: Trenutna stanja šestih storitev verige servisiranja Windows Update samo za branje.
- `setup-code DECIMAL_OR_0xHEX`: Dokumentirana razlaga kode rezultata Windows Setup; brez pregleda računalnika ali popravila.
- `update-applicability`: Podatki o izdaji, gradnji in arhitekturi sistema Windows samo za branje.
- `update-cache`: Omejene skupne velikosti predpomnilnika SoftwareDistribution samo za branje; brez imen datotek.
- `update-code DECIMAL_OR_0xHEX`: Dokumentirana razlaga kode rezultata Windows Update; brez pregleda računalnika ali popravila.
- `update-context`: Podatki o trenutnem povišanju pravic in namizni seji samo za branje; brez identifikatorjev računov.
- `update-engine-files`: Dokazi o prisotnosti, različici in podpisu izvornega mehanizma Windows Update samo za branje.
- `update-events`: Zgodovina Windows Update Client iz zadnjih sedmih dni samo za branje; JSON s surovimi dokazi.
- `update-followup DECIMAL_OR_0xHEX`: Sveža diagnostika samo za branje, izbrana po dokumentirani kodi rezultata Windows Update.
- `update-history`: Najnovejših 100 izidov operacij Windows Update samo za branje; dokazi JSON.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: Ponovite `--select`; privzeto predogled.
- `update-policy`: Upravljani pravilniki Windows Update samo za branje; dokazi JSON.
- `update-policy-surfaces`: Dokazi o konfiguracijskih mestih klasičnih pravilnikov in Policy CSP samo za branje; vrednosti in identitete ponudnikov so izključene.
- `update-recover`: Preveri čakajočo posodobitev; nikoli je ne namesti ali ponovi.
- `update-restart-policy`: Roki skladnosti, obdobja odloga, preprečevanje ponovnega zagona in delovanje obvestil samo za branje.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: Iskanje in ujemanja v JSON; brez namestitve.
- `update-service-events`: Zgodovina napak točno določene storitve Windows Update samo za branje.
- `update-service-files`: Dokazi o registraciji in podpisu DLL točno določene storitve Windows Update samo za branje.
- `update-services`: Registracije storitev Windows Update samo za branje; dokazi JSON.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Pogojeno s svežim iskanjem; privzeto predogled; ohrani staro shrambo.
- `update-store-reset-recover`: Uskladi pripravljeno ponastavitev shrambe brez ponavljanja.
- `windows-capabilities`: Stanja zmogljivosti zagnanega sistema Windows samo za branje; rezultat JSON.
- `windows-update-task`: Stanje in zgodovina izvajanja točno določenega opravila Scheduled Start samo za branje; brez zagona opravila ali posodobitvenega dejanja.

## BitLocker in TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Privzeto predogled; vklopi samodejno odklepanje šifriranega podatkovnega nosilca.
- `bitlocker-auto-unlock-recover`: Preveri prekinjeno zahtevo za samodejno odklepanje brez ponavljanja.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Privzeto predogled; le nova datoteka varnostne kopije obnovitvenih podatkov.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Preveri prekinjeno varnostno kopiranje brez ponovnega zapisovanja.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Privzeto predogled; začne dešifriranje, na primer pred ponovnim šifriranjem z zahtevano metodo.
- `bitlocker-decrypt-recover`: Preveri prekinjeno zahtevo za dešifriranje brez ponavljanja.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Privzeto predogled; zahteva obstoječa zaščitnika TPM in obnovitveno geslo.
- `bitlocker-encrypt-recover`: Preveri čakajoče stanje pretvorbe brez ponavljanja zahteve.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Privzeto predogled; varnostno kopira obstoječe obnovitveno geslo v Active Directory ali Microsoft Entra ID.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Samo predogled možnosti.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Privzeto predogled; doda enega zaščitnika TPM ali obnovitveno geslo; gesla nikoli ne izpiše.
- `bitlocker-protector-recover`: Preveri prekinjeno zahtevo za zaščitnika brez ponavljanja.
- `bitlocker-protectors DRIVE:`: Le identitete in vrste zaščitnikov; brez obnovitvenih gesel.
- `bitlocker-resume DRIVE: [--execute]`: Privzeto predogled; le točno določen popolnoma šifriran nosilec z začasno ustavljeno zaščito.
- `bitlocker-resume-recover`: Preveri čakajoče stanje zaščite brez ponavljanja zahteve.

## Varnost in zasebnost

- `certificate-chains`: Lokalno zaupanje verig osebnih potrdil samo iz predpomnilnika, samo za branje; brez preverjanja preklica ali prenosov.
- `defender-overview [--report]`: Pregled zaščite, časov pregledovanja in skupnih podatkov o grožnjah samo za branje; brez dejanj.
- `defender-protection [--report]`: Nastavitve zaščite in prepoznana mesta pravilnikov samo za branje; občutljive podrobnosti so izključene; brez sprememb.
- `defender-scan Quick|Full [--execute] [--report]`: Privzeto predogled; en izrecen varovan pregled Microsoft Defender.
- `defender-scan-history [--report]`: Časi hitrih in celovitih pregledov ter zamude samo za branje; občutljive podrobnosti so izključene; brez pregleda.
- `defender-scan-recover [--report]`: Usklajevanje samo za branje brez začetka novega pregleda.
- `defender-threats [--report]`: Skupna zgodovina groženj, trenutne oznake aktivnosti in ID-ji resnosti; občutljive podrobnosti so izključene; brez dejanj.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Privzeto predogled; le neupravljani lokalni profili.
- `firewall-enable-recover`: Pregleda čakajočo spremembo brez ponavljanja.
- `firewall-events`: Prebere omejeno zgodovino opozoril, napak in kritičnih dogodkov delovanja Windows Firewall.
- `firewall-logging`: Prebere nastavitve beleženja prometa profilov domene, zasebnega in javnega omrežja brez odpiranja dnevnikov.
- `firewall-rules`: Prebere števila veljavnih pravil in virov pravilnikov z omejitvami zasebnosti.
- `firewall-service-events`: Prebere zgodovino napak točno določenih storitev BFE in Požarni zid programa Windows Defender v Service Control Manager.
- `privacy-clipboard-history on|off [--execute]`: Privzeto predogled; točna vrednost trenutnega uporabnika z ohranitvijo pravilnikov.
- `privacy-clipboard-history-recover`: Znova preveri prekinjeno spremembo brez ponavljanja.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: Varovana obnovitev prejšnje vrednosti.
- `privacy-plan --select Camera --select ClipboardSync`: Samo pregled izbir; nastavitev ne spreminja.
- `privacy-setting KEY on|off [--execute]`: Privzeto predogled; sprememba trenutnega uporabnika z ohranitvijo pravilnikov; neveljavni ključi izpišejo podprte izbire.
- `privacy-setting-recover KEY`: Znova preveri prekinjeno spremembo brez ponavljanja.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: Varovana obnovitev točne vrednosti.
- `security-check`: TPM, Secure Boot, zaščite virtualizacije in pravilniki UAC samo za branje; dokazi JSON.

## Storitve

- `service-binaries`: Prisotnost lokalnih izvršljivih datotek storitev in stanje Authenticode samo za branje; poti so izključene in nelokalne poti niso obiskane.
- `service-check NAME [NAME ...] | service-check --all`: Stanja podprtih storitev in dokazi o odvisnostih samo za branje.
- `service-dependencies`: Pregled celovitosti neposrednih odvisnosti samo za branje; brez sprememb storitev.
- `service-dlls`: Prisotnost registriranih DLL storitev in stanje Authenticode samo za branje; poti so izključene in nelokalne poti niso obiskane.
- `service-events`: Zgodovina napak storitev samo za branje; JSON s surovimi dokazi.
- `time-events`: Nedavna zgodovina dogodkov storitve Čas sistema Windows samo za branje z dokazi JSON.
- `service-logons`: Pregled razredov računov storitev in interakcije z namizjem samo za branje; identitete računov so izključene.
- `service-path-quoting`: Pregled narekovajev poti izvršljivih datotek storitev samo za branje; polne poti in argumenti so izključeni.
- `service-recovery-policy`: Povzetek obnovitvenih dejanj nameščenih storitev samo za branje; ukazi in sporočila ponovnega zagona so izključeni.
- `service-startup-config`: Pregled skladnosti načinov zagona samo za branje; brez sprememb storitev.
- `service-transitions`: Ponavljajoče opazovanje prehodnih stanj storitev samo za branje; brez dejanj s storitvami.
- `service-triggers`: Povzetek sprožilcev nameščenih storitev samo za branje; vsebina sprožilcev je izključena; brez sprememb.

## Shranjevanje, datoteke in varnostne kopije

- `backup-events`: Zgodovina VSS in volsnap; dokazi JSON; brez sprememb.
- `backup-service-events`: Prebere zgodovino napak točno določenih storitev VSS, ponudnika programskih senčnih kopij, Windows Backup in mehanizma na ravni blokov.
- `backup-summary`: Povzetek lokalnih operacij Windows Backup; brez sprememb varnostnega kopiranja.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Privzeto predogled; koš se obdela v celoti ali sploh ne, starost pa zanj ne velja.
- `disk-cleanup-task`: Stanje in zgodovina izvajanja točno določenega opravila samo za branje; brez zagona opravila ali čiščenja.
- `disk-diagnostic`: Poveže točno določena diagnostična opravila diska in zgodovino dogodkov; brez dejanj z opravili ali diskom.
- `disk-diagnostic-tasks`: Zgodovina točno določenih opravil zbiranja in razreševanja samo za branje; brez dejanj z opravili ali diskom.
- `disk-quotas`: Privzete kvote in stanja nosilcev samo za branje; identitete uporabnikov so izključene; brez sprememb.
- `file-history-events`: Števila opozoril in napak File History z omejitvami zasebnosti; brez sprememb.
- `file-history-status`: Stanje zaščite File History trenutnega računa; brez sprememb.
- `search-index-rebuild [--execute]`: Privzeto predogled; ohrani prejšnji indeks in preveri sveže stanje.
- `search-index-rebuild-recover`: Usklajevanje samo za branje brez ponavljanja ukazov za popravilo.
- `storage-check DRIVE_LETTER`: Fiksno preverjanje NTFS samo za branje; rezultati JSON in sprotni napredek.
- `storage-events`: Zgodovina shranjevanja samo za branje; JSON s surovimi dokazi.
- `storage-jobs`: Skupno stanje in napredek opravil modula Storage samo za branje; JSON; brez sprememb.
- `storage-reliability`: Temperatura, obraba in vhodno-izhodni števci samo za branje; JSON; brez popravila.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Privzeto predogled; izvedba zahteva potrditev varnostne kopije; za sistemske nosilce se načrtuje ob ponovnem zagonu.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Samo predogled; brez popravila ali načrtovanja.
- `storage-repair-recover`: Preveri dokončano ali načrtovano popravilo datotečnega sistema brez ponavljanja.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Zbere in poveže trenutne dokaze; brez popravila ali načrtovanja.
- `storage-sense-policy`: Prepoznani pravilniki čiščenja računalnika samo za branje; uporabniške nastavitve in identitete datotek so izključene.
- `storage-sense-task`: Stanje in zgodovina izvajanja točno določenega opravila samo za branje; brez zagona opravila ali čiščenja.
- `storage-service-events`: Prebere zgodovino napak točno določenih storitev Storage Service, Optimize Drives, VSS in ponudnika programskih senčnih kopij.
- `volume-capacity DRIVE:`: Razpoložljivi prostor samo za branje z dokazi JSON.
- `volume-dirty DRIVE:`: Stanje oznake dirty samo za branje; brez popravila ali ponovnega zagona.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Privzeto analiza samo za branje; zahteva točno določen zdrav nosilec in združljiv medij.
- `volume-optimize-recover`: Preveri čakajočo dokončano operacijo brez ponavljanja.
- `vss-providers`: Imena, različice in vrste ponudnikov VSS samo za branje; identifikatorji so izključeni; brez sprememb registracij.
- `vss-writers`: Sporočena stanja zapisovalnikov; razlaga angleškega izhoda; brez ponastavitve.

## Tiskanje

- `print-events`: Zgodovina PrintService Admin iz zadnjih sedmih dni samo za branje; dokazi JSON.
- `print-extension-files`: Povzetek prisotnosti in podpisov izvršljivih datotek tiskalnih procesorjev in monitorjev samo za branje; polne poti so izključene.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Privzeto predogled; le točno določeno opravilo v stanju Error ali Blocked.
- `print-job-cancel-recover [--folder PATH]`: Znova preveri prekinjen preklic brez ponavljanja.
- `print-job-list`: Izpiše trenutno vidna opravila brez spreminjanja.
- `print-operational-events`: Stanje kanala PrintService Operational in razpoložljiva zgodovina samo za branje; dokazi JSON.
- `print-policy`: Omejitve Point and Print ter pravilniki nameščanja gonilnikov samo za branje; dokazi JSON.
- `print-rendering-policy`: Pravilniki izrisa in izolacije gonilnikov samo za branje; dokazi JSON.
- `print-spooler-events`: Napake Service Control Manager, povezane z lokalno identiteto storitve Tiskalni posrednik, samo za branje; dokazi JSON.
- `printer-check --queue NAME [--queue NAME ...]`: Izbrane nastavitve lokalnih čakalnih vrst samo za branje.
- `printer-conditions`: Kode stanj lokalnih čakalnih vrst samo za branje; brez fizičnega preverjanja ali sprememb.
- `printer-driver-files`: Povzetek prisotnosti registriranih datotek in podpisov izvršljivih datotek samo za branje; polne poti so izključene.
- `printer-driver-models`: Povzetek različic 3 in 4 ter podpore paketom samo za branje; dokazi JSON.
- `printer-drivers`: Popis in ujemanje registracij čakalnih vrst ter gonilnikov samo za branje; dokazi JSON.
- `printer-endpoint-check`: Omejene povezave s točno določenimi dodeljenimi končnimi točkami tiskalnikov Standard TCP/IP; brez odkrivanja, SNMP ali tiskalnih podatkov.
- `printer-list`: Odkrije lokalne čakalne vrste brez sprememb nastavitev.
- `printer-ports`: Ujemanje registracij čakalnih vrst in vrat samo za branje; dokazi JSON.
- `printer-queue-attributes`: Atributi lokalnih čakalnih vrst za shranjevanje opravil, hranjenje in skupno rabo samo za branje.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Privzeto predogled; izklop načina brez povezave lahko sprosti čakajoča opravila.

## Omrežje

- `dhcp-check`: Upravičenost manjkajočih ali poteklih najemov DHCP samo za branje; dokazi JSON, brez obnove.
- `dhcp-events`: Zgodovina dogodkov DHCP Client za IPv4 in IPv6 samo za branje; rezultat JSON.
- `dhcp-leases`: Podatki o časovnih oznakah, poteku in strežniku najemov samo za branje; rezultat JSON.
- `dhcp-renew [--execute]`: Privzeto predogled; le točno določeni manjkajoči ali potekli najemi na povezanih fizičnih vmesnikih DHCP.
- `dhcp-renew-recover`: Usklajevanje samo za branje brez zahteve za nov najem.
- `dns-cache HOST [--execute]`: Privzeto predogled; varovano praznjenje lokalnega predpomnilnika s preverjanjem točno določenega gostitelja.
- `dns-cache-recover`: Znova preveri prekinjeno dejanje s predpomnilnikom brez ponovnega praznjenja.
- `dns-cache-summary`: Stanje lokalnega predpomnilnika DNS in števila vrst zapisov z omejitvami zasebnosti; brez sprememb.
- `dns-check HOST [--timeout-seconds 1..60]`: Omejeno sistemsko razreševanje imen; rezultat JSON.
- `dns-client-config`: Dokazi o priponah, registraciji, skrajševanju pripon in pravilnikih samo za branje; rezultat JSON.
- `dns-client-events`: Omejena zgodovina opozoril in napak DNS Client samo za branje z zaščito zasebnosti; rezultat JSON.
- `dns-nrpt-policy`: Dokazi o veljavnih pravilnikih imenskih prostorov DNS samo za branje; rezultat JSON.
- `ip-addresses`: Pregled trenutnega stanja, izvora in sporov naslovov IP samo za branje; dokazi JSON.
- `ip-interface-config`: Dokazi o nastavitvah vmesnikov IP samo za branje; rezultat JSON.
- `network-firewall`: Poveže aktivne omrežne dodelitve z omogočenostjo ustreznih profilov požarnega zidu.
- `network-offload`: Globalne nastavitve strojnega razbremenjevanja omrežja Windows samo za branje; rezultat JSON.
- `network-profiles`: Trenutne kategorije profilov in razvrstitve povezljivosti samo za branje; imena omrežij so izključena.
- `network-service-events`: Prebere zgodovino napak točno določenih storitev DHCP, DNS, Workstation, NLA, Network List in IP Helper.
- `proxy-check`: Shranjene nastavitve posredniškega strežnika WinHTTP in internetne nastavitve trenutnega računa samo za branje.
- `route-priority`: Primerjava veljavnih metrik privzetih poti samo za branje; dokazi JSON.
- `smb-client-config`: Nastavitve podpisovanja SMB, gostujočega dostopa in prenosa samo za branje.
- `smb-connections`: Aktivno pogajanje o protokolu samo za branje; oddaljene identitete so izključene.
- `smb-server-config`: Nastavitve protokolov SMB, podpisovanja, šifriranja in večkanalnega prenosa samo za branje.
- `smb-shares`: Omejen popis skupnih rab SMB samo za branje; poti, dovoljenja, seje in računi so izključeni.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Omejeno preverjanje povezave; ne pošilja podatkov aplikacije.
- `tcp-port-pressure`: Dinamični obsegi TCP in skupna uporaba lokalnih vrat samo za branje; končne točke in procesi so izključeni.
- `tcp-settings`: Dokazi o nastavitvah predlog TCP sistema Windows samo za branje; rezultat JSON.
- `vpn-profiles`: Nastavitve profilov VPN trenutnega računa samo za branje; poverilnice so izključene.
- `winrm-listeners`: Povzetek registracij poslušalnikov WinRM samo za branje; prstni odtisi potrdil so izključeni.
- `winsock-catalog`: Struktura izvornega kataloga protokolov Winsock samo za branje; rezultat JSON.
- `winsock-namespaces`: Struktura izvornih ponudnikov imenskih prostorov Winsock samo za branje; rezultat JSON.
- `winsock-provider-files`: Prisotnost datotek ponudnikov protokolov in podpisi samo za branje; rezultat JSON.
- `winsock-readiness`: Pripravljenost izvornih protokolov Winsock, ponudnikov imenskih prostorov in datotek ponudnikov samo za branje; rezultati JSON.

## Aplikacije

- `app-folder-review FOLDER_NAME [--report]`: Pregled točno določene mape pod fiksnim korenom; brez brisanja.
- `app-package-review FULL_NAME [--all-users]`: Točen cilj in odvisni paketi; brez odstranitve.
- `app-packages [--all-users]`: Samo popis registriranih Appx/MSIX; brez odstranitve.
- `browser-extensions`: Pregled identitet razširitev Chrome, Edge, Brave in Firefox; brez odstranitve.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Samo pregled; brez sprememb sistema.
- `depot-review [application and configuration selections] [--json]`: Samo pregled; brez priprave računalnika.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Preverjanje Authenticode in podpisnika ponudnika samo za branje; ničesar ne zažene.
- `foxit-inventory`: Popis izdelkov računalnika samo za branje; izhod JSON.
- `installed-apps [--current-user]`: Registracije za odstranjevanje in oznake zmogljivosti; brez odstranitve.
- `installed-apps-compare BEFORE.json AFTER.json`: Razlike registracij v shranjenih popisih; brez sprememb računalnika.
- `installed-hotfixes`: Omejen popis nameščenih hitrih popravkov Windows samo za branje; dokazi JSON.
- `installer-activity`: Dokazi o procesih Installer in oznakah transakcij samo za branje; dokazi JSON.
- `installer-check`: Pravilniki Installer in pripravljenost mehanizma samo za branje; dokazi JSON.
- `installer-engine-files`: Povzetek prisotnosti zahtevanih datotek Installer in Authenticode samo za branje; dokazi JSON.
- `installer-events`: Zgodovina MSI iz zadnjih sedmih dni samo za branje; dokazi JSON.
- `installer-logging`: Pravilniki beleženja in razhroščevanja Installer samo za branje; dokazi JSON.
- `installer-pending-state`: Povezovanje čakajočih datotečnih operacij in dejavnosti Installer samo za branje; dokazi JSON.
- `installer-registration-repair [--execute]`: Privzeto predogled; le točno določena napaka registracije storitve; nikoli ne odregistrira mehanizma.
- `installer-registration-repair-recover`: Znova preveri prekinjeno registracijo brez ponavljanja ukazov.
- `installer-security-policy`: Varnostni pravilniki in pravilniki popravkov Installer samo za branje; dokazi JSON.
- `installer-service-events`: Napake Service Control Manager, povezane z lokalno identiteto Windows Installer, samo za branje; dokazi JSON.
- `installer-service-registration`: Povzetek registracije storitve Windows Installer samo za branje; dokazi JSON.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: Svež predogled točne upravičenosti; brez ponudnika odstranjevanja.
- `mace-package-recover`: Na sveže uskladi en zapis prekinjene odstranitve paketa aplikacije; odstranitve nikoli ne ponovi.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Samo pregled; brez odstranitve.
- `msi-cache`: Vidnost registriranega predpomnilnika Windows Installer samo za branje; dokazi JSON.
- `msi-code NONNEGATIVE_DECIMAL`: Dokumentirana razlaga povratne kode Windows Installer; brez pregleda računalnika ali popravila.
- `msi-product {PRODUCT-CODE}`: Točno stanje registracije MSI samo za branje; dokazi JSON.
- `msi-repair {PRODUCT-CODE} [--execute]`: Privzeto predogled; le točno določen pokvarjen ali nepravilno nastavljen izdelek.
- `msi-repair-recover`: Znova preveri prekinjeno popravilo točno določenega izdelka brez ponavljanja.
- `msi-sources`: Metapodatki registriranih virov Windows Installer samo za branje z omejitvami zasebnosti; dokazi JSON.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Privzeto predogled; ohrani točno določene predpomnilnike trenutnega računa pod obnovitvenimi imeni.
- `orca-cache-reset-recover`: Preveri prekinjeno ohranitev predpomnilnika brez ponovnega premika.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Samo pregled; brez sprememb Outlooka.
- `startup-apps`: Pregled zagonskih registracij in opravil ob zagonu ali prijavi; brez onemogočanja.
## System

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Točno lokalno iskanje dodelitve Bluetooth SIG; brez omrežne zahteve.
- `crash-config`: Shranjeni način izpisov Windows in poti; brez sprememb.
- `crash-dumps`: Metapodatki privzetih datotek izpisov Windows; brez analize vsebine ali sprememb.
- `crash-events`: Nedavna poročila o sistemskih napakah; brez diagnostike ali sprememb.
- `crash-review [--knowledge PACK_PATH ...]`: Do 32 paketov; združen pregled sesutij, izklopov, omejene strukture minidump in izbirne točne reference kode bug-check; brez sprememb.
- `default-printer`: Privzeta izbira in nastavitev samodejnega upravljanja samo za branje; dokazi JSON.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Privzeto predogled; le manjkajoča ali nasprotujoča si privzeta izbira.
- `default-printer-set-recover [--folder PATH]`: Znova preveri prekinjeno izbiro brez ponavljanja.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: Podatki o napravah samo za branje z izbirnimi točnimi referencami gonilnikov iz največ 32 paketov.
- `device-code DECIMAL_OR_0xHEX`: Razlaga kode težave upravitelja naprav brez povezave.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Samo referenčni kandidati.
- `event-channel-enable CHANNEL [--execute]`: Privzeto predogled; omogoči en točno določen neupravljan diagnostični kanal in ohrani druge nastavitve.
- `event-channel-enable-recover`: Usklajevanje samo za branje brez ponavljanja ukaza za omogočanje.
- `event-channels`: Stanje, hranjenje, velikost in zasičenost štirih diagnostičnih kanalov samo za branje.
- `font-registrations`: Omejeno preverjanje registracij računalnika in trenutnega uporabnika ter prisotnosti datotek samo za branje.
- `memory-diagnostic-results`: Omejena zgodovina Windows Memory Diagnostic samo za branje; dokazi JSON.
- `minidump-analyze PATH`: Omejeni strukturni metapodatki minidump; brez pomnilniških strani, simbolov ali sprememb.
- `name-rename PROPOSED_NAME [--execute]`: Privzeto predogled; le samostojni računalniki v delovni skupini; nikoli ne zažene sistema Windows znova.
- `name-rename-recover`: Preveri čakajoče preimenovanje brez ponavljanja.
- `name-review PROPOSED_NAME`: Pregled identitete in predlaganega imena samo za branje.
- `profiles --recover`: Preveri zabeleženo odsotnost in ohrani obnovitvene dokaze; brisanja nikoli ne ponovi.
- `profiles --recover-backup`: Preveri in arhivira čakajoči artefakt varnostne kopije; kopiranja nikoli ne ponovi.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Najprej predogled; varovano izvorno kopiranje zahteva izrecno izvedbo in neobstoječ lokalni cilj NTFS.
- `profiles --select SID --create-backup-artifact DESTINATION`: Primerja obstoječo kopirano varnostno kopijo in ustvari njen fiksni artefakt; profila ne kopira ali odstrani.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Pregled samo za branje; preverjanje zahteva en izbrani SID.
- `task-check`: Popis opravil, razvrščene ugotovitve in dokazi JSON samo za branje.
- `task-definition TASK_FOLDER TASK_NAME`: XML točno določenega opravila samo za branje; nedostopnost ne pomeni odsotnosti.
- `task-inventory`: Identitete in stanja opravil samo za branje; izhod JSON.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Pregled pretekle obnovitve; nikoli ne odobri ponovnega poskusa.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Preveri in pregleda osnutek opravila; brez registracije.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Privzeto predogled; le ustvarjanje onemogočene definicije Local System.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Usklajevanje točne definicije samo za branje; nikoli ne ponovi ali zažene opravila.
- `task-xml PLAN.json YYYY-MM-DD`: Onemogočeni XML za pregled na standardni izhod; le koledarski urniki v lokalnem časovnem pasu; brez registracije.
- `time-config`: Način storitve Čas sistema Windows, partnerji in dokazi o pravilnikih samo za branje.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Privzeto predogled; način po meri zahteva partnerje.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Samo predogled.
- `time-recover --folder PATH`: Preveri in arhivira čakajoči zapis; sprememb nikoli ne ponovi.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Samo za branje; način po meri zahteva partnerje.
- `wmi-provider`: Lokalna poizvedba ponudnika operacijskega sistema samo za branje; dokazi JSON.

## Znanje, preverjanje in Engine

- `knowledge PACK_PATH CONTEXT CODE`: Iskanje lokalne reference; brez izvajanja popravil.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Preverjena lokalna aktivacija; brez prenosa.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Preveri aktivni lokalni paket; brez sprememb.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Samo spremembe referenc; brez namestitve paketov.
- `knowledge-info PACK_PATH`: Metapodatki paketa in števila vnosov po kontekstu; brez sprememb.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Opaženim kodam doda kontekstne reference.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Izbere najnovejši preverjeni ohranjeni paket; brez prenosa.
- `knowledge-search PACK_PATH CONTEXT TEXT`: Do 50 besedilnih ujemanj referenc; brez izvajanja popravil.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Razreši eno točno identiteto v največ 32 paketih; spornih rezultatov nikoli ne izbere.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Preveri vse nastavljene nameščene zbirke podatkov; brez prenosa ali aktivacije.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Pridobi in atomsko aktivira podpisane kandidate zbirk podatkov z nastavljenih končnih točk HTTPS.
- `open <file.kbench> [--json]`: Znova odpre shranjen zajem in ga ponovno oceni z nameščenimi pravili.
- `scan --output <new-file.kbench> [--json]`: Zajem Windows Update samo za branje, shranjen v novo datoteko.
- `validation-coverage RECORD.json [...]`: Povzame izvorne dokaze navideznih računalnikov za trenutno različico; brez diagnostike ali popravil.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Načrt scenarija za začasni navidezni računalnik samo za branje; brez diagnostike ali popravil.
- `validation-record RECORD.json`: Preveri en omejen zapis izvornih dokazov navideznega računalnika; brez diagnostike ali popravil.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Lokalno poročilo o pokritosti navideznih računalnikov za trenutno različico.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Izvede preverjen skupni načrt možnosti samo za branje.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Samo popis.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Usmerjen diagnostični dnevnik; brez popravil.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Samo pregled z dnevnikom.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: Izbirno stanje točno določenega izdelka je vključeno v dnevnik.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: Izbirna diagnostika izbranega cilja je vključena v dnevnik.
- `workflow storage [--volume D:] [--json]`: Preverjanje datotečnega sistema samo za branje z dnevnikom.
- `workflow-journal FILE.json`: Pretekla vsebina; izhodna koda 0 pomeni uspešno branje, ne zdravega stanja.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Prikaže ali preveri kontrolnike, neodvisne od gostitelja; brez pregleda ali popravila.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Samo preteklo poročilo; ne izvede ukazov.

## Drugi ukazi

- `<analyze|investigate> <text-log-path> [--json]`: Analizira lokalne besedilne dokaze.
- `adapter-bindings`: Povzetek vezav osnovnih protokolov samo za branje; dokazi JSON.
- `adapter-statistics`: Števci bajtov, napak in zavrženih paketov vmesnikov samo za branje; dokazi JSON.
- `application-events`: Nedavne napake Application, združena števila in dokazi JSON samo za branje.
- `applocker-policy`: Veljavni načini zbirk in skupna števila pravil samo za branje; podrobnosti pravil so izključene; brez sprememb.
- `common-check [--scope NAME ...]`: Privzeto vsa diagnostična področja; le izbrana, kadar so navedena; JSON samo za branje.
- `delete-notifications`: Nastavitve obvestil o brisanju NTFS in ReFS samo za branje; JSON; brez optimizacije ali spremembe nastavitev.
- `diagnostic-scopes [--all]`: Izpiše 11 priporočenih skupin izidov ali vseh 49 področij samo za branje; brez zbiranja.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Usmerjen diagnostični dnevnik; brez popravil.
- `full-scan [--recipes ID[,ID]] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Vsa diagnostična področja; samo diagnostika, brez popravil.
- `gateway-check`: Omejena preverjanja ICMP točno določenih nastavljenih privzetih naslednjih skokov; brez pregledovanja naslovov.
- `infrastructure-service-events`: Prebere en omejen posnetek napak točno določenih storitev povezljivosti, oddaljenega upravljanja, obvestil in osnovnih storitev.
- `investigate-batch FILE [FILE ...]`: Do 12 datotek; JSON Lines; izhodna koda 3 za neuspešen ali delen uvoz.
- `licensing-check`: Stanje aktivacije Windows brez ključev izdelkov samo za branje; dokazi JSON.
- `mac-vendor PACK_PATH MAC_ADDRESS`: Iskanje najdaljše predpone v lokalnem paketu; brez omrežne zahteve.
- `ncsi-policy`: Pravilnik razvrščanja povezljivosti Windows samo za branje; cilji sond so izključeni.
- `neighbor-cache`: Podatki o dosegljivosti sosedov samo za branje z omejitvami zasebnosti; rezultat JSON.
- `ntfs-self-healing DRIVE:`: Nastavitve sprotnega popravljanja NTFS izbranega nosilca samo za branje; brez popravila ali spremembe nastavitev.
- `pagefile-check`: Povzetek upravljanja, dodelitve in uporabe ostranjevalnih datotek samo za branje; polne poti so izključene.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Točno iskanje v lokalnem paketu; brez omrežne zahteve ali izbire gonilnika.
- `physical-disks`: Trenutno zdravje, stanje delovanja, medijev, vodil in uporabe samo za branje; JSON; brez popravila.
- `platform-service-events`: Prebere en omejen posnetek napak točno določenih storitev aplikacij, identitete, profilov in podpore napravam.
- `previous-windows [--execute --backup-confirmed]`: Privzeto predogled; prek DISM trajno odstrani možnost povrnitve sistema Windows.
- `previous-windows-recover`: Usklajevanje samo za branje brez ponovne odstranitve možnosti povrnitve.
- `provisioned-packages`: Paketi, pripravljeni za nove uporabnike lokalnega sistema Windows; brez odstranitve.
- `recipes [WORKFLOW]`: Izpiše nameščene definicije ali odvisnosti in omejitve določenega postopka; brez izvajanja.
- `repair <dism|sfc> [--execute]`: Privzeto predogled.
- `reserved-storage`: Stanje rezerviranega prostora zagnanega sistema Windows samo za branje; dokazi JSON.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Privzeto predogled; po svežih preverjanjih ustvari eno točko.
- `restore-point-recover`: Preveri čakajočo zahtevo brez ustvarjanja nove točke.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Samo predogled.
- `shadow-copies`: Trenutna stanja in atributi posnetkov VSS samo za branje; identitete so izključene; brez sprememb posnetkov.
- `shadow-storage`: Dodelitev in zasedenost diferencialnega prostora VSS samo za branje; JSON; brez spreminjanja velikosti ali dejanj s posnetki.
- `uac-policy`: Omogočenost UAC in pravilniki pozivov za povišanje pravic samo za branje; brez podatkov o računih ali poverilnicah.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Točno lokalno iskanje v registru UEFI; brez omrežne zahteve ali izbire gonilnika.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Točno iskanje v lokalnem paketu; brez omrežne zahteve ali izbire gonilnika.
- `vitals-check`: Posnetki sistema Windows, strojne opreme, računa, virov, baterije in fizičnih diskov samo za branje; dokazi JSON.
- `win32-code DECIMAL_OR_0xHEX`: Iskanje sporočil nameščenega sistema Windows brez povezave.
- `workflows`: Izpiše ključe in naslove nameščenih postopkov; brez pregleda ali popravila.
