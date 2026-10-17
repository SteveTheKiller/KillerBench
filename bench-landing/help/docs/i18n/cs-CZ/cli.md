# Příkazový řádek

`killerbench-cli` spouští stejný Engine jako desktopová aplikace, z PowerShellu nebo vzdálené konzole. Každý příkaz, který by mohl změnit počítač, ve výchozím stavu zobrazí náhled a před zásahem vyžaduje výslovný přepínač, například `--execute`. Příkazy vypisují důkazy JSON na standardní výstup a průběh na standardní chybový výstup.

Návody krok za krokem: [Spouštění KillerBench ze skriptů](use-the-cli.html).

Návratový kód 0 znamená, že náhled nebo ověřený výsledek proběhl úspěšně, 1 znamená chybu spuštění, vstupu nebo deníku, 2 znamená, že argumenty nebyly rozpoznány, 3 znamená zablokováno, neúspěšné nebo neověřené, 4 znamená, že postup potřebuje před ověřením restart, a 130 znamená, že jste jej zastavili.

Spuštěním `killerbench-cli` bez argumentů vypíšete tento seznam na vlastním počítači.

## Aktualizace a údržba

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Přesná kontrola přenosu nebo pozorování ve dvou snímcích; bez opakování.
- `bits-jobs [--all-users]`: Stavy, vlastníci a počty bajtů úloh BITS; bez změn přenosů.
- `bits-retry JOB_GUID [--execute]`: Ve výchozím stavu náhled; výslovné opakování přesně určené úlohy, bez tvrzení o dokončení aplikací.
- `bits-retry-recover JOB_GUID`: Dva snímky pouze pro čtení; archivuje stabilní výsledky bez dalšího opakování.
- `component-repair-source`: Spravované zásady zdroje oprav DISM a konflikty; bez změn.
- `delivery-optimization`: Stav služby Optimalizace doručení pouze pro čtení; důkazy JSON.
- `delivery-optimization-activity`: Z hlediska soukromí ohraničené aktuální počty úloh a součty bajtů; bez identit přenosů a bez změn.
- `delivery-optimization-policy`: Klasická nastavení a nastavení Policy CSP pouze pro čtení; citlivé hodnoty a identity zprostředkovatelů vyloučeny.
- `feature-service-events`: Čte přesnou historii selhání služeb času, Search, WMI, Protokolu událostí a zvuku.
- `feature-update-policy`: Zásady cílové verze, odložení, pozastavení a ochranných opatření pouze pro čtení; důkazy JSON.
- `pending-file-operations`: Z hlediska soukromí ohraničený souhrn čekajících operací se soubory pouze pro čtení; důkazy JSON.
- `restart-check`: Zdroje čekajícího restartu pouze pro čtení; důkazy JSON.
- `servicing-logs`: Ohraničená analýza nejnovějších protokolů CBS, textových a komprimovaných archivů CBS, DISM a Instalačního programu systému Windows; bez opravy.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Snímky aktivity údržby pouze pro čtení.
- `servicing-service-events`: Přesná historie selhání řetězce údržby Windows Update pouze pro čtení.
- `servicing-service-states`: Aktuální stav šesti služeb řetězce údržby Windows Update pouze pro čtení.
- `setup-code DECIMAL_OR_0xHEX`: Zdokumentovaná reference kódů výsledku Instalačního programu systému Windows; bez kontroly počítače a bez opravy.
- `update-applicability`: Důkazy o edici, sestavení a architektuře Windows pouze pro čtení.
- `update-cache`: Ohraničené součty mezipaměti SoftwareDistribution pouze pro čtení; bez názvů souborů.
- `update-code DECIMAL_OR_0xHEX`: Zdokumentovaná reference kódů výsledku Windows Update; bez kontroly počítače a bez opravy.
- `update-context`: Důkazy o aktuálním zvýšení oprávnění a relaci plochy pouze pro čtení; bez identifikátorů účtů.
- `update-engine-files`: Důkazy o přítomnosti, verzi a podpisu nativního modulu Windows Update pouze pro čtení.
- `update-events`: Historie klienta Windows Update za posledních sedm dní pouze pro čtení; JSON včetně surových důkazů.
- `update-followup DECIMAL_OR_0xHEX`: Čerstvá diagnostika pouze pro čtení vybraná podle zdokumentovaného kódu výsledku Windows Update.
- `update-history`: 100 nejnovějších výsledků operací Windows Update pouze pro čtení; důkazy JSON.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: Opakujte `--select`; ve výchozím stavu náhled.
- `update-policy`: Spravované zásady Windows Update pouze pro čtení; důkazy JSON.
- `update-policy-surfaces`: Důkazy o konfiguraci klasických nastavení a Policy CSP pouze pro čtení; hodnoty a identity zprostředkovatelů vyloučeny.
- `update-recover`: Zkontroluje čekající aktualizaci; nikdy ji neinstaluje ani neopakuje.
- `update-restart-policy`: Termíny dodržování předpisů, období odkladu, potlačení restartu a chování oznámení pouze pro čtení.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: Zjišťování a shody v JSON; bez instalace.
- `update-service-events`: Přesná historie selhání služby Windows Update pouze pro čtení.
- `update-service-files`: Přesné důkazy o registraci a podpisu knihovny DLL služby Windows Update pouze pro čtení.
- `update-services`: Registrace služeb Windows Update pouze pro čtení; důkazy JSON.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Vyžaduje čerstvé vyhledávání; ve výchozím stavu náhled; uchová staré úložiště.
- `update-store-reset-recover`: Sladí připravené resetování úložiště bez jeho opakování.
- `windows-capabilities`: Stavy možností spuštěného Windows pouze pro čtení; výsledek JSON.
- `windows-update-task`: Přesný stav a historie spuštění úlohy Scheduled Start pouze pro čtení; bez spuštění úlohy a bez operace aktualizace.

## BitLocker a TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Ve výchozím stavu náhled; zapne automatické odemykání zašifrovaného datového svazku.
- `bitlocker-auto-unlock-recover`: Ověří přerušený požadavek na automatické odemykání bez jeho opakování.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Ve výchozím stavu náhled; pouze nový záložní soubor pro obnovení.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Ověří přerušené zálohování bez opětovného zápisu.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Ve výchozím stavu náhled; zahájí dešifrování, například před opětovným šifrováním požadovanou metodou.
- `bitlocker-decrypt-recover`: Ověří přerušený požadavek na dešifrování bez jeho opakování.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Ve výchozím stavu náhled; vyžaduje existující ochrany TPM a pro obnovení.
- `bitlocker-encrypt-recover`: Ověří čekající stav převodu bez opakování požadavku.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Ve výchozím stavu náhled; zálohuje existující heslo pro obnovení do Active Directory nebo Microsoft Entra ID.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Pouze náhled možností.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Ve výchozím stavu náhled; přidá jednu ochranu TPM nebo heslem pro obnovení; heslo se nikdy nevypisuje.
- `bitlocker-protector-recover`: Ověří přerušený požadavek na ochranu bez jeho opakování.
- `bitlocker-protectors DRIVE:`: Pouze identity a typy ochran; bez hesel pro obnovení.
- `bitlocker-resume DRIVE: [--execute]`: Ve výchozím stavu náhled; pouze přesně určený, plně zašifrovaný a pozastavený svazek.
- `bitlocker-resume-recover`: Ověří čekající stav ochrany bez opakování požadavku.

## Zabezpečení a soukromí

- `certificate-chains`: Důvěryhodnost řetězce osobních certifikátů pouze z místní mezipaměti, pouze pro čtení; bez kontroly odvolání a bez stahování.
- `defender-overview [--report]`: Ochrana, časování kontrol a souhrnný přehled hrozeb pouze pro čtení; bez akce.
- `defender-protection [--report]`: Předvolby ochrany a rozpoznané oblasti zásad pouze pro čtení; citlivé podrobnosti vyloučeny; bez změn.
- `defender-scan Quick|Full [--execute] [--report]`: Ve výchozím stavu náhled; jedna výslovná chráněná kontrola Microsoft Defender.
- `defender-scan-history [--report]`: Časování rychlé a úplné kontroly a stav zpoždění pouze pro čtení; citlivé podrobnosti vyloučeny; bez kontroly.
- `defender-scan-recover [--report]`: Sladění pouze pro čtení bez spuštění další kontroly.
- `defender-threats [--report]`: Souhrnná historie hrozeb, aktuální příznaky aktivity a ID závažnosti; citlivé podrobnosti vyloučeny; bez akce.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Ve výchozím stavu náhled; pouze nespravované místní profily.
- `firewall-enable-recover`: Zkontroluje čekající změnu bez jejího opakování.
- `firewall-events`: Čte ohraničenou provozní historii upozornění, chyb a kritických událostí brány Windows Firewall.
- `firewall-logging`: Čte konfiguraci protokolování provozu profilů Doména, Soukromá a Veřejná bez otevírání protokolů.
- `firewall-rules`: Čte z hlediska soukromí ohraničené počty účinných pravidel a zdrojů zásad.
- `firewall-service-events`: Čte přesnou historii selhání BFE a brány Windows Defender Firewall ze Správce řízení služeb.
- `privacy-clipboard-history on|off [--execute]`: Ve výchozím stavu náhled; přesná hodnota aktuálního uživatele se zachováním zásad.
- `privacy-clipboard-history-recover`: Znovu zkontroluje přerušenou změnu bez jejího opakování.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: Chráněné obnovení předchozí hodnoty.
- `privacy-plan --select Camera --select ClipboardSync`: Pouze kontrola voleb; žádná nastavení se nemění.
- `privacy-setting KEY on|off [--execute]`: Ve výchozím stavu náhled; změna pro aktuálního uživatele se zachováním zásad; neplatné klíče vypíší podporované volby.
- `privacy-setting-recover KEY`: Znovu zkontroluje přerušenou změnu bez jejího opakování.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: Chráněné obnovení přesné hodnoty.
- `security-check`: TPM, Zabezpečené spouštění, ochrany virtualizace a zásady UAC pouze pro čtení; důkazy JSON.

## Služby

- `service-binaries`: Přítomnost místních binárních souborů služeb a stav Authenticode pouze pro čtení; cesty vyloučeny a nemístní cesty se nekontaktují.
- `service-check NAME [NAME ...] | service-check --all`: Stavy podporovaných služeb a důkazy o závislostech pouze pro čtení.
- `service-dependencies`: Kontrola integrity přímých závislostí pouze pro čtení; bez změn služeb.
- `service-dlls`: Přítomnost zaregistrovaných knihoven DLL služeb a stav Authenticode pouze pro čtení; cesty vyloučeny a nemístní cesty se nekontaktují.
- `service-events`: Historie selhání služeb pouze pro čtení; JSON včetně surových důkazů.
- `time-events`: Nedávná historie událostí Času systému Windows pouze pro čtení s důkazy JSON.
- `service-logons`: Kontrola tříd účtů služeb a interakce s plochou pouze pro čtení; identity účtů vyloučeny.
- `service-path-quoting`: Kontrola uvozovek v cestách ke spustitelným souborům služeb pouze pro čtení; úplné cesty a argumenty vyloučeny.
- `service-recovery-policy`: Souhrn akcí obnovení nainstalovaných služeb pouze pro čtení; příkazy a zprávy o restartu vyloučeny.
- `service-startup-config`: Kontrola soudržnosti režimů spouštění pouze pro čtení; bez změn služeb.
- `service-transitions`: Opakované pozorování čekajících stavů služeb pouze pro čtení; bez akce se službami.
- `service-triggers`: Souhrn aktivačních událostí nainstalovaných služeb pouze pro čtení; datové části aktivačních událostí vyloučeny; bez změn.

## Úložiště, soubory a zálohování

- `backup-events`: Historie VSS a volsnap; důkazy JSON; bez změn.
- `backup-service-events`: Čte přesnou historii selhání VSS, zprostředkovatele softwarových stínových kopií, Zálohování Windows a modulu zálohování na úrovni bloků.
- `backup-summary`: Souhrn operací místního Zálohování Windows; bez změn záloh.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Ve výchozím stavu náhled; Koš se vyprazdňuje celý nebo vůbec a stáří se neuplatňuje.
- `disk-cleanup-task`: Přesný stav a historie spuštění úlohy pouze pro čtení; bez spuštění úlohy a bez úklidu.
- `disk-diagnostic`: Porovná přesné úlohy diagnostiky disků s historií událostí; bez akce s úlohami a disky.
- `disk-diagnostic-tasks`: Přesná historie úloh kolektoru a resolveru pouze pro čtení; bez akce s úlohami a disky.
- `disk-quotas`: Výchozí nastavení a stav kvót jednotlivých svazků pouze pro čtení; identity uživatelů vyloučeny; bez změn.
- `file-history-events`: Z hlediska soukromí ohraničené počty upozornění a chyb Historie souborů; bez změn.
- `file-history-status`: Stav ochrany Historie souborů aktuálního účtu; bez změn.
- `search-index-rebuild [--execute]`: Ve výchozím stavu náhled; uchová předchozí index a ověří čerstvý stav.
- `search-index-rebuild-recover`: Sladění pouze pro čtení bez opakování příkazů opravy.
- `storage-check DRIVE_LETTER`: Pevná kontrola NTFS pouze pro čtení; výsledky JSON a živý průběh.
- `storage-events`: Historie úložiště pouze pro čtení; JSON včetně surových důkazů.
- `storage-jobs`: Souhrnný stav a průběh úloh modulu Storage pouze pro čtení; JSON; bez změn.
- `storage-reliability`: Čítače teploty, opotřebení a V/V operací pouze pro čtení; JSON; bez opravy.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Ve výchozím stavu náhled; provedení vyžaduje potvrzení zálohy; systémové svazky se plánují na restart.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Pouze náhled; bez opravy a bez plánování.
- `storage-repair-recover`: Ověří dokončenou nebo naplánovanou opravu souborového systému bez jejího opakování.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Shromáždí a sváže aktuální důkazy; bez opravy a bez plánování.
- `storage-sense-policy`: Rozpoznané zásady úklidu počítače pouze pro čtení; uživatelská nastavení a identity souborů vyloučeny.
- `storage-sense-task`: Přesný stav a historie spuštění úlohy pouze pro čtení; bez spuštění úlohy a bez úklidu.
- `storage-service-events`: Čte přesnou historii selhání Služby úložiště, Optimalizace jednotek, VSS a zprostředkovatele softwarových stínových kopií.
- `volume-capacity DRIVE:`: Dostupné místo pouze pro čtení s důkazy JSON.
- `volume-dirty DRIVE:`: Stav příznaku nečistoty pouze pro čtení; bez opravy a bez restartu.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Ve výchozím stavu analýza pouze pro čtení; vyžaduje přesně určený zdravý svazek a kompatibilní médium.
- `volume-optimize-recover`: Ověří čekající dokončenou operaci bez jejího opakování.
- `vss-providers`: Názvy, verze a typy zprostředkovatelů VSS pouze pro čtení; identifikátory vyloučeny; bez změn registrací.
- `vss-writers`: Hlášené stavy zapisovačů; interpretace anglického výstupu; bez resetování.

## Tisk

- `print-events`: Historie PrintService Admin za posledních sedm dní pouze pro čtení; důkazy JSON.
- `print-extension-files`: Souhrn přítomnosti a podpisů binárních souborů tiskových procesorů a monitorů pouze pro čtení; úplné cesty vyloučeny.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Ve výchozím stavu náhled; pouze přesně určená úloha ve stavu Chyba nebo Blokováno.
- `print-job-cancel-recover [--folder PATH]`: Znovu zkontroluje přerušené zrušení bez jeho opakování.
- `print-job-list`: Vypíše aktuální viditelné úlohy bez jejich změny.
- `print-operational-events`: Stav kanálu PrintService Operational a dostupná historie pouze pro čtení; důkazy JSON.
- `print-policy`: Omezení Point and Print a zásady instalace ovladačů pouze pro čtení; důkazy JSON.
- `print-rendering-policy`: Zásady vykreslování a izolace ovladačů pouze pro čtení; důkazy JSON.
- `print-spooler-events`: Selhání Správce řízení služeb spojená s identitou místního Zařazování tisku, pouze pro čtení; důkazy JSON.
- `printer-check --queue NAME [--queue NAME ...]`: Vybraná nastavení místních front pouze pro čtení.
- `printer-conditions`: Stavové kódy místních front pouze pro čtení; bez fyzického ověření a bez změn.
- `printer-driver-files`: Souhrn přítomnosti zaregistrovaných souborů a podpisů spustitelných souborů pouze pro čtení; úplné cesty vyloučeny.
- `printer-driver-models`: Souhrn verze 3, verze 4 a podpory balíčků pouze pro čtení; důkazy JSON.
- `printer-drivers`: Inventář a párování registrací front a ovladačů pouze pro čtení; důkazy JSON.
- `printer-endpoint-check`: Ohraničená připojení k přesně přiřazeným standardním koncovým bodům tiskáren TCP/IP; bez zjišťování, SNMP a tiskových dat.
- `printer-list`: Zjistí místní fronty beze změny nastavení.
- `printer-ports`: Párování registrací front a portů pouze pro čtení; důkazy JSON.
- `printer-queue-attributes`: Atributy zařazování, uchovávání a sdílení místních front pouze pro čtení.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Ve výchozím stavu náhled; zrušení režimu offline může uvolnit čekající úlohy.

## Síť

- `dhcp-check`: Způsobilost chybějících nebo vypršených zapůjčení DHCP pouze pro čtení; důkazy JSON, bez obnovení.
- `dhcp-events`: Historie událostí Klienta DHCP pro IPv4 a IPv6 pouze pro čtení; výsledek JSON.
- `dhcp-leases`: Důkazy o časovém razítku, vypršení zapůjčení a serveru pouze pro čtení; výsledek JSON.
- `dhcp-renew [--execute]`: Ve výchozím stavu náhled; pouze přesně určená chybějící nebo vypršená zapůjčení DHCP připojených fyzických adaptérů.
- `dhcp-renew-recover`: Sladění pouze pro čtení bez žádosti o další zapůjčení.
- `dns-cache HOST [--execute]`: Ve výchozím stavu náhled; chráněné vyprázdnění místní mezipaměti s ověřením přesného hostitele.
- `dns-cache-recover`: Znovu zkontroluje přerušenou operaci s mezipamětí bez opakování vyprázdnění.
- `dns-cache-summary`: Z hlediska soukromí ohraničený stav místní mezipaměti DNS a počty typů záznamů; bez změn.
- `dns-check HOST [--timeout-seconds 1..60]`: Ohraničený systémový překlad názvů; výsledek JSON.
- `dns-client-config`: Důkazy o příponách, registraci, devoluci a zásadách pouze pro čtení; výsledek JSON.
- `dns-client-events`: Z hlediska soukromí ohraničená historie upozornění a selhání Klienta DNS pouze pro čtení; výsledek JSON.
- `dns-nrpt-policy`: Důkazy o účinných zásadách oboru názvů DNS pouze pro čtení; výsledek JSON.
- `ip-addresses`: Kontrola aktuálního stavu, původu a konfliktů adres IP pouze pro čtení; důkazy JSON.
- `ip-interface-config`: Důkazy o konfiguraci rozhraní IP pouze pro čtení; výsledek JSON.
- `network-firewall`: Porovná aktivní přiřazení sítí se zapnutím odpovídajících profilů brány firewall.
- `network-offload`: Globální konfigurace snižování zátěže sítě ve Windows pouze pro čtení; výsledek JSON.
- `network-profiles`: Aktuální kategorie profilů a klasifikace připojení pouze pro čtení; názvy sítí vyloučeny.
- `network-service-events`: Čte přesnou historii selhání služeb DHCP, DNS, Pracovní stanice, NLA, Seznam sítí a Pomocná služba IP.
- `proxy-check`: Uložené důkazy o proxy WinHTTP a internetovém proxy aktuálního účtu pouze pro čtení.
- `route-priority`: Porovnání metrik účinné výchozí trasy pouze pro čtení; důkazy JSON.
- `smb-client-config`: Konfigurace podepisování SMB, přístupu hostů a přenosu pouze pro čtení.
- `smb-connections`: Aktivní vyjednávání protokolu pouze pro čtení; vzdálené identity vyloučeny.
- `smb-server-config`: Konfigurace protokolu, podepisování, šifrování a vícekanálového přenosu SMB pouze pro čtení.
- `smb-shares`: Ohraničený inventář sdílených složek SMB pouze pro čtení; cesty, oprávnění, relace a účty vyloučeny.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Ohraničená kontrola připojení; neodesílají se žádná data aplikací.
- `tcp-port-pressure`: Dynamické rozsahy TCP a souhrnné využití místních portů pouze pro čtení; koncové body a procesy vyloučeny.
- `tcp-settings`: Důkazy o konfiguraci šablon TCP systému Windows pouze pro čtení; výsledek JSON.
- `vpn-profiles`: Konfigurace profilů VPN aktuálního účtu pouze pro čtení; přihlašovací údaje vyloučeny.
- `winrm-listeners`: Souhrn registrací naslouchacích procesů WinRM pouze pro čtení; kryptografické otisky certifikátů vyloučeny.
- `winsock-catalog`: Struktura nativního katalogu protokolů Winsock pouze pro čtení; výsledek JSON.
- `winsock-namespaces`: Struktura nativních zprostředkovatelů oboru názvů Winsock pouze pro čtení; výsledek JSON.
- `winsock-provider-files`: Přítomnost a podpisy souborů zprostředkovatelů protokolů pouze pro čtení; výsledek JSON.
- `winsock-readiness`: Připravenost nativních protokolů, zprostředkovatelů oboru názvů a souborů zprostředkovatelů Winsock pouze pro čtení; výsledky JSON.

## Aplikace

- `app-folder-review FOLDER_NAME [--report]`: Kontrola přesných složek s pevným kořenem; bez mazání.
- `app-package-review FULL_NAME [--all-users]`: Přesný cílový balíček a závislé balíčky; bez odebrání.
- `app-packages [--all-users]`: Pouze inventář zaregistrovaných balíčků Appx/MSIX; bez odebrání.
- `browser-extensions`: Kontrola identit rozšíření Chrome, Edge, Brave a Firefox; bez odebrání.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Pouze kontrola; bez změn systému.
- `depot-review [application and configuration selections] [--json]`: Pouze kontrola; bez zřizování.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Kontrola Authenticode a podpisu dodavatele pouze pro čtení; nic se nespouští.
- `foxit-inventory`: Inventář produktů počítače pouze pro čtení; výstup JSON.
- `installed-apps [--current-user]`: Registrace odinstalace a příznaky možností; bez odebrání.
- `installed-apps-compare BEFORE.json AFTER.json`: Rozdíly registrací oproti uloženému inventáři; bez změn počítače.
- `installed-hotfixes`: Ohraničený inventář nainstalovaných oprav hotfix Windows pouze pro čtení; důkazy JSON.
- `installer-activity`: Důkazy o procesech a značkách transakcí Installeru pouze pro čtení; důkazy JSON.
- `installer-check`: Zásady Installeru a připravenost modulu pouze pro čtení; důkazy JSON.
- `installer-engine-files`: Souhrn přítomnosti požadovaných souborů Installeru a Authenticode pouze pro čtení; důkazy JSON.
- `installer-events`: Historie MSI za posledních sedm dní pouze pro čtení; důkazy JSON.
- `installer-logging`: Zásady protokolování a ladění Installeru pouze pro čtení; důkazy JSON.
- `installer-pending-state`: Porovnání čekajících operací se soubory s aktivitou Installeru pouze pro čtení; důkazy JSON.
- `installer-registration-repair [--execute]`: Ve výchozím stavu náhled; pouze přesně určená vada registrace služby; modul nikdy neodregistruje.
- `installer-registration-repair-recover`: Znovu zkontroluje přerušenou registraci bez opakování příkazů.
- `installer-security-policy`: Zásady zabezpečení a oprav Installeru pouze pro čtení; důkazy JSON.
- `installer-service-events`: Selhání Správce řízení služeb spojená s identitou místního Windows Installeru, pouze pro čtení; důkazy JSON.
- `installer-service-registration`: Souhrn registrace služby Windows Installer pouze pro čtení; důkazy JSON.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: Čerstvý náhled přesné způsobilosti; bez zprostředkovatele odebrání.
- `mace-package-recover`: Čerstvě sladí jeden přerušený záznam o odebrání balíčku aplikace; odebrání nikdy neopakuje.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Pouze kontrola; bez odebrání.
- `msi-cache`: Viditelnost zaregistrované mezipaměti Windows Installeru pouze pro čtení; důkazy JSON.
- `msi-code NONNEGATIVE_DECIMAL`: Zdokumentovaná reference návratových kódů Windows Installeru; bez kontroly počítače a bez opravy.
- `msi-product {PRODUCT-CODE}`: Přesný stav registrace MSI pouze pro čtení; důkazy JSON.
- `msi-repair {PRODUCT-CODE} [--execute]`: Ve výchozím stavu náhled; pouze přesně určený poškozený nebo špatně nakonfigurovaný produkt.
- `msi-repair-recover`: Znovu zkontroluje přerušenou opravu přesně určeného produktu bez jejího opakování.
- `msi-sources`: Z hlediska soukromí ohraničená metadata zaregistrovaných zdrojů Windows Installeru pouze pro čtení; důkazy JSON.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Ve výchozím stavu náhled; uchová přesné mezipaměti spuštěného účtu pod názvy pro obnovení.
- `orca-cache-reset-recover`: Ověří přerušené uchování mezipaměti bez opakování přesunu.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Pouze kontrola; bez změn Outlooku.
- `startup-apps`: Kontrola registrací při spuštění a úloh při spuštění systému nebo přihlášení; bez zakázání.
- `third-party-update ID SOURCE VERSION [--accept-agreements] [--execute]`: Ve výchozím stavu náhled; pouze jedna přesná verze.
- `third-party-update-recover`: Ověří čekající přesnou verzi bez opakování aktualizace.
- `third-party-updates`: Zjišťování aktualizací aplikací přes Microsoft.WinGet.Client pouze pro čtení; bez změn aplikací.

## Systém

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Přesné místní vyhledání přiřazení Bluetooth SIG; bez síťového požadavku.
- `crash-config`: Uložený režim a cesty výpisů paměti Windows; bez změn.
- `crash-dumps`: Metadata výchozích souborů výpisů paměti Windows; bez analýzy obsahu a bez změn.
- `crash-events`: Nedávné zprávy o chybách systému; bez diagnostiky a bez změn.
- `crash-review [--knowledge PACK_PATH ...]`: Až 32 balíčků; kombinovaná kontrola pádů, vypnutí, ohraničené struktury minivýpisů a volitelné přesné reference kódu chyby zastavení; bez změn.
- `default-printer`: Výchozí výběr a nastavení automatické správy pouze pro čtení; důkazy JSON.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Ve výchozím stavu náhled; pouze chybějící nebo konfliktní výchozí hodnota.
- `default-printer-set-recover [--folder PATH]`: Znovu zkontroluje přerušený výběr bez jeho opakování.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: Důkazy o zařízeních pouze pro čtení s volitelnými přesnými odkazy na ovladače až z 32 balíčků.
- `device-code DECIMAL_OR_0xHEX`: Offline reference kódů problémů Správce zařízení.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Pouze referenční kandidáti.
- `event-channel-enable CHANNEL [--execute]`: Ve výchozím stavu náhled; povolí jeden přesně určený nespravovaný diagnostický kanál a zachová ostatní nastavení.
- `event-channel-enable-recover`: Sladění pouze pro čtení bez opakování příkazu povolení.
- `event-channels`: Stav, uchovávání, velikost a zaplnění čtyř diagnostických kanálů pouze pro čtení.
- `font-registrations`: Ohraničená kontrola registrací a přítomnosti souborů pro počítač a aktuálního uživatele pouze pro čtení.
- `memory-diagnostic-results`: Ohraničená historie Diagnostiky paměti Windows pouze pro čtení; důkazy JSON.
- `minidump-analyze PATH`: Ohraničená strukturní metadata minivýpisů; bez stránek paměti, symbolů a změn.
- `name-rename PROPOSED_NAME [--execute]`: Ve výchozím stavu náhled; pouze samostatné počítače v pracovní skupině; Windows nikdy nerestartuje.
- `name-rename-recover`: Ověří čekající přejmenování bez jeho opakování.
- `name-review PROPOSED_NAME`: Kontrola identity a navrženého názvu pouze pro čtení.
- `profiles --recover`: Ověří zaznamenanou nepřítomnost a uchová důkazy pro obnovení; mazání nikdy neopakuje.
- `profiles --recover-backup`: Ověří a archivuje čekající artefakt zálohy; kopírování nikdy neopakuje.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Nejprve náhled; chráněné nativní kopírování vyžaduje výslovné provedení a neexistující místní cíl NTFS.
- `profiles --select SID --create-backup-artifact DESTINATION`: Porovná existující zkopírovanou zálohu a vytvoří její pevný artefakt; profil nekopíruje ani neodebírá.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Kontrola pouze pro čtení; ověření vyžaduje jeden vybraný identifikátor SID.
- `task-check`: Inventář úloh pouze pro čtení, klasifikovaná zjištění a důkazy JSON.
- `task-definition TASK_FOLDER TASK_NAME`: Přesný kód XML úlohy pouze pro čtení; nedostupné neznamená neexistující.
- `task-inventory`: Identity a stavy úloh pouze pro čtení; výstup JSON.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Kontrola historie obnovení; nikdy neopravňuje k opakování.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Ověří a zkontroluje koncept úlohy; bez registrace.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Ve výchozím stavu náhled; zakázaná definice Local System pouze k vytvoření.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Sladění přesné definice pouze pro čtení; úlohu nikdy neopakuje ani nespouští.
- `task-xml PLAN.json YYYY-MM-DD`: Zakázaný kód XML ke kontrole na stdout; pouze kalendářní plány v místním časovém pásmu; bez registrace.
- `time-config`: Důkazy o režimu, partnerech a zásadách Času systému Windows pouze pro čtení.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Ve výchozím stavu náhled; vlastní režim vyžaduje partnery.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Pouze náhled.
- `time-recover --folder PATH`: Ověří a archivuje čekající záznam; změny nikdy neopakuje.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Pouze pro čtení; vlastní režim vyžaduje partnery.
- `wmi-provider`: Dotaz na místního zprostředkovatele operačního systému pouze pro čtení; důkazy JSON.

## Znalosti, ověřování a Engine

- `knowledge PACK_PATH CONTEXT CODE`: Místní vyhledání v referencích; bez provádění oprav.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Ověřená místní aktivace; bez stahování.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Ověří aktivní místní balíček; bez změn.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Pouze změny referencí; bez instalace balíčku.
- `knowledge-info PACK_PATH`: Metadata balíčku a počty položek pro každý kontext; bez změn.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Připojí kontextové reference k pozorovaným kódům.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Vybere nejnovější ověřený uchovaný balíček; bez stahování.
- `knowledge-search PACK_PATH CONTEXT TEXT`: Až 50 shod v referenčním textu; bez provádění oprav.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Vyhodnotí jednu přesnou identitu až ve 32 balíčcích; konflikty se nikdy nevybírají.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Ověří každou nakonfigurovanou nainstalovanou databázi; bez stahování a bez aktivace.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Stáhne a atomicky aktivuje podepsané kandidátské databáze z nakonfigurovaných koncových bodů HTTPS.
- `open <file.kbench> [--json]`: Znovu otevře uložený záznam a znovu jej vyhodnotí nainstalovanými pravidly.
- `scan --output <new-file.kbench> [--json]`: Záznam Windows Update pouze pro čtení uložený do nového souboru.
- `validation-coverage RECORD.json [...]`: Shrne nativní důkazy z virtuálních počítačů pro aktuální verzi; bez diagnostiky a bez oprav.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Plán scénářů pro jednorázové virtuální počítače pouze pro čtení; bez diagnostiky a bez oprav.
- `validation-record RECORD.json`: Ověří jeden ohraničený nativní záznam důkazů z virtuálního počítače; bez diagnostiky a bez oprav.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Místní zpráva o pokrytí virtuálních počítačů pro aktuální verzi.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Spustí ověřený sdílený plán možností pouze pro čtení.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Pouze inventář.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Deník cílené diagnostiky; bez oprav.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Pouze kontrola se zápisem do deníku.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: Volitelný přesný stav produktu zahrnutý v deníku.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: Volitelná diagnostika vybraného cíle zahrnutá v deníku.
- `workflow storage [--volume D:] [--json]`: Kontrola souborového systému pouze pro čtení se zápisem do deníku.
- `workflow-journal FILE.json`: Historický obsah; kód 0 znamená úspěšné čtení, nikoli zdravý stav.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Zobrazí nebo ověří ovládací prvky nezávislé na hostiteli; bez kontroly a bez opravy.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Pouze historická zpráva; nespouštějí se žádné příkazy.

## Další příkazy

- `<analyze|investigate> <text-log-path> [--json]`: Analyzuje místní textové důkazy.
- `adapter-bindings`: Souhrn vazeb základních protokolů pouze pro čtení; důkazy JSON.
- `adapter-statistics`: Čítače bajtů, chyb a zahozených paketů adaptérů pouze pro čtení; důkazy JSON.
- `application-events`: Nedávné chyby protokolu Aplikace, seskupené počty a důkazy JSON pouze pro čtení.
- `applocker-policy`: Účinné režimy shromažďování a souhrnné počty pravidel pouze pro čtení; podrobnosti pravidel vyloučeny; bez změn.
- `common-check [--scope NAME ...]`: Ve výchozím stavu všechny diagnostické oblasti; pouze vybrané oblasti, jsou-li zadány; JSON pouze pro čtení.
- `delete-notifications`: Konfigurace oznámení o odstranění NTFS a ReFS pouze pro čtení; JSON; bez optimalizace a bez změny nastavení.
- `diagnostic-scopes [--all]`: Vypíše 11 doporučených skupin výsledků nebo všech 49 oblastí pouze pro čtení; bez shromažďování.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Deník cílené diagnostiky; bez oprav.
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Všechny diagnostické oblasti; bez opravy, pokud není výslovně zadáno `--auto-repair`.
- `gateway-check`: Ohraničené kontroly ICMP přesně nakonfigurovaných výchozích dalších směrování; bez prohledávání adres.
- `infrastructure-service-events`: Čte jeden ohraničený přesný snímek selhání služeb připojení, vzdálené správy, oznámení a základních služeb.
- `investigate-batch FILE [FILE ...]`: Až 12 souborů; JSON Lines; kód 3 pro neúspěšné nebo částečné importy.
- `licensing-check`: Stav aktivace Windows bez kódů Product Key pouze pro čtení; důkazy JSON.
- `mac-vendor PACK_PATH MAC_ADDRESS`: Místní vyhledání v balíčku podle nejdelší předpony; bez síťového požadavku.
- `ncsi-policy`: Zásady klasifikace připojení Windows pouze pro čtení; cíle sond vyloučeny.
- `neighbor-cache`: Z hlediska soukromí ohraničené důkazy o dosažitelnosti sousedů pouze pro čtení; výsledek JSON.
- `ntfs-self-healing DRIVE:`: Konfigurace online opravy NTFS vybraného svazku pouze pro čtení; bez opravy a bez změny nastavení.
- `pagefile-check`: Souhrn správy, přidělení a využití stránkovacího souboru pouze pro čtení; úplné cesty vyloučeny.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Přesné místní vyhledání v balíčku; bez síťového požadavku a bez výběru ovladače.
- `physical-disks`: Aktuální stav kondice, provozu, média, sběrnice a využití pouze pro čtení; JSON; bez opravy.
- `platform-service-events`: Čte jeden ohraničený přesný snímek selhání služeb aplikací, identit, profilů a podpory zařízení.
- `previous-windows [--execute --backup-confirmed]`: Ve výchozím stavu náhled; prostřednictvím DISM trvale odebere možnost vrátit Windows zpět.
- `previous-windows-recover`: Sladění pouze pro čtení bez opakování odebrání možnosti vrácení.
- `provisioned-packages`: Balíčky připravené pro nové uživatele v místním systému Windows; bez odebrání.
- `recipes [WORKFLOW]`: Vypíše nainstalované definice nebo závislosti a limity specifické pro postup; bez provádění.
- `repair <dism|sfc> [--execute]`: Ve výchozím stavu náhled.
- `reserved-storage`: Stav rezervovaného úložiště spuštěného Windows pouze pro čtení; důkazy JSON.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Ve výchozím stavu náhled; po čerstvých kontrolách vytvoří jeden bod.
- `restore-point-recover`: Ověří čekající požadavek bez vytvoření dalšího bodu.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Pouze náhled.
- `shadow-copies`: Aktuální stavy a atributy snímků VSS pouze pro čtení; identity vyloučeny; bez změn snímků.
- `shadow-storage`: Přidělení a zatížení rozdílové oblasti VSS pouze pro čtení; JSON; bez změny velikosti a bez akce se snímky.
- `uac-policy`: Zapnutí UAC a zásady výzvy ke zvýšení oprávnění pouze pro čtení; bez dat účtů a přihlašovacích údajů.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Přesné místní vyhledání v registru UEFI; bez síťového požadavku a bez výběru ovladače.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Přesné místní vyhledání v balíčku; bez síťového požadavku a bez výběru ovladače.
- `vitals-check`: Snímky Windows, hardwaru, účtu, prostředků, baterie a fyzických disků pouze pro čtení; důkazy JSON.
- `win32-code DECIMAL_OR_0xHEX`: Offline vyhledání zpráv nainstalovaného systému Windows.
- `workflows`: Vypíše nainstalované klíče a názvy postupů; bez kontroly a bez opravy.
