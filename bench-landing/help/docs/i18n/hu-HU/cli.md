# Parancssor

A `killerbench-cli` ugyanazt az Engine-t futtatja PowerShellből vagy távoli konzolról, mint az asztali alkalmazás. Minden parancs, amely módosíthatja a gépet, alapértelmezés szerint előnézetet készít, és a végrehajtás előtt kifejezett kapcsolót igényel, például `--execute`. A parancsok a JSON-bizonyítékot a szabványos kimenetre, az előrehaladást a szabványos hibakimenetre írják.

Lépésenkénti útmutatók: [A KillerBench futtatása szkriptekből](use-the-cli.html).

A 0 kilépési kód azt jelenti, hogy az előnézet vagy az igazolt eredmény sikeres volt, az 1 indítási, bemeneti vagy naplóhibát jelent, a 2 azt, hogy az argumentumok nem ismerhetők fel, a 3 letiltott, sikertelen vagy nem igazolt eredményt, a 4 azt, hogy egy folyamatnak az ellenőrzés előtt újraindításra van szüksége, a 130 pedig azt, hogy Ön leállította.

Futtassa a `killerbench-cli` parancsot argumentumok nélkül, ha ezt a listát a saját gépén szeretné kiíratni.

## Frissítések és karbantartás

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Pontos átvitel-áttekintés vagy két pillanatképes megfigyelés; nincs újrapróbálás.
- `bits-jobs [--all-users]`: BITS-feladatok állapota, tulajdonosai és bájtszámai; nincs átvitelmódosítás.
- `bits-retry JOB_GUID [--execute]`: Alapértelmezés szerint előnézet; kifejezett újrapróbálás egy pontos feladatra, alkalmazásbefejezési állítás nélkül.
- `bits-retry-recover JOB_GUID`: Két csak olvasó pillanatkép; a stabil eredményeket újabb próbálkozás nélkül archiválja.
- `component-repair-source`: Felügyelt DISM-javításiforrás-házirend és ütközések; nincs módosítás.
- `delivery-optimization`: A Kézbesítésoptimalizálás szolgáltatás állapota csak olvasva; JSON-bizonyíték.
- `delivery-optimization-activity`: Adatvédelmi szempontból korlátozott aktuális feladatszámok és bájtösszegek; nincsenek átvitelazonosítók és módosítások.
- `delivery-optimization-policy`: Klasszikus és Policy CSP-beállítások csak olvasva; a bizalmas értékek és a szolgáltatóazonosítók kizárva.
- `feature-service-events`: Az Idő, a Search, a WMI, az Eseménynapló és a hangszolgáltatások pontos hibaelőzményeinek olvasása.
- `feature-update-policy`: Célkiadás-, halasztási, szüneteltetési és védőintézkedési házirend csak olvasva; JSON-bizonyíték.
- `pending-file-operations`: Adatvédelmi szempontból korlátozott összegzés a függő fájlműveletekről, csak olvasva; JSON-bizonyíték.
- `restart-check`: Függő újraindítási források csak olvasva; JSON-bizonyíték.
- `servicing-logs`: A legújabb CBS-naplók, szöveges és tömörített CBS-archívumok, DISM- és Windows Setup-naplók korlátozott elemzése; nincs javítás.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Karbantartási tevékenység pillanatképei csak olvasva.
- `servicing-service-events`: A Windows Update karbantartási láncának pontos hibaelőzményei csak olvasva.
- `servicing-service-states`: A Windows Update hatszolgáltatásos karbantartási láncának aktuális állapota csak olvasva.
- `setup-code DECIMAL_OR_0xHEX`: Dokumentált Windows Setup eredménykód-referencia; nincs gépvizsgálat vagy javítás.
- `update-applicability`: Windows-kiadás, build és architektúra bizonyítéka csak olvasva.
- `update-cache`: Korlátozott SoftwareDistribution-gyorsítótár-összegek csak olvasva; fájlnevek nélkül.
- `update-code DECIMAL_OR_0xHEX`: Dokumentált Windows Update eredménykód-referencia; nincs gépvizsgálat vagy javítás.
- `update-context`: Az aktuális jogosultságemelés és asztali munkamenet bizonyítéka csak olvasva; fiókazonosítók nélkül.
- `update-engine-files`: A natív Windows Update motor jelenlétének, verziójának és aláírásának bizonyítéka csak olvasva.
- `update-events`: A Windows Update-ügyfél elmúlt hét napjának előzményei csak olvasva; JSON nyers bizonyítékkal.
- `update-followup DECIMAL_OR_0xHEX`: Friss, csak olvasó diagnosztika egy dokumentált Windows Update eredménykód alapján kiválasztva.
- `update-history`: A legújabb 100 Windows Update-művelet eredménye csak olvasva; JSON-bizonyíték.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: Ismételje a `--select` kapcsolót; alapértelmezés szerint előnézet.
- `update-policy`: Felügyelt Windows Update-házirend csak olvasva; JSON-bizonyíték.
- `update-policy-surfaces`: Klasszikus és Policy CSP konfigurációsfelület-bizonyíték csak olvasva; az értékek és a szolgáltatóazonosítók kizárva.
- `update-recover`: Egy függő frissítés ellenőrzése; soha nem telepíti és nem próbálja újra.
- `update-restart-policy`: Megfelelőségi határidők, türelmi időszakok, újraindítás-letiltás és értesítési viselkedés csak olvasva.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: JSON-felderítés és egyezések; nincs telepítés.
- `update-service-events`: A Windows Update szolgáltatás pontos hibaelőzményei csak olvasva.
- `update-service-files`: A Windows Update szolgáltatás-DLL pontos regisztrációs és aláírási bizonyítéka csak olvasva.
- `update-services`: Windows Update szolgáltatásregisztrációk csak olvasva; JSON-bizonyíték.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Friss kereséshez kötött; alapértelmezés szerint előnézet; a régi tárat megőrzi.
- `update-store-reset-recover`: Egy előkészített tár-alaphelyzetbe állítás egyeztetése megismétlés nélkül.
- `windows-capabilities`: A futó Windows képességállapotai csak olvasva; JSON-eredmény.
- `windows-update-task`: A Scheduled Start feladat pontos állapota és futási előzményei csak olvasva; nincs feladatfuttatás vagy frissítési művelet.

## BitLocker és TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Alapértelmezés szerint előnézet; bekapcsolja az automatikus feloldást egy titkosított adatkötethez.
- `bitlocker-auto-unlock-recover`: Egy megszakított automatikusfeloldás-kérés ellenőrzése megismétlés nélkül.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Alapértelmezés szerint előnézet; csak új helyreállítási mentési fájl.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Egy megszakított mentés ellenőrzése újbóli írás nélkül.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Alapértelmezés szerint előnézet; elindítja a visszafejtést, például egy előírt módszerrel történő újratitkosítás előtt.
- `bitlocker-decrypt-recover`: Egy megszakított visszafejtési kérés ellenőrzése megismétlés nélkül.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Alapértelmezés szerint előnézet; meglévő TPM- és helyreállítási védőket igényel.
- `bitlocker-encrypt-recover`: A függő átalakítási állapot ellenőrzése a kérés megismétlése nélkül.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Alapértelmezés szerint előnézet; egy meglévő helyreállítási jelszót ment az Active Directoryba vagy a Microsoft Entra ID-ba.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Csak a beállítások előnézete.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Alapértelmezés szerint előnézet; egy TPM- vagy helyreállítási jelszó típusú védőt ad hozzá; a jelszó soha nem jelenik meg.
- `bitlocker-protector-recover`: Egy megszakított védőkérés ellenőrzése megismétlés nélkül.
- `bitlocker-protectors DRIVE:`: Csak a védők azonosítói és típusai; helyreállítási jelszavak nélkül.
- `bitlocker-resume DRIVE: [--execute]`: Alapértelmezés szerint előnézet; csak pontosan megadott, teljesen titkosított, felfüggesztett kötet.
- `bitlocker-resume-recover`: A függő védelmi állapot ellenőrzése a kérés megismétlése nélkül.

## Biztonság és adatvédelem

- `certificate-chains`: A személyes tanúsítványok láncának megbízhatósága csak a helyi gyorsítótárból, csak olvasva; nincs visszavonás-ellenőrzés vagy letöltés.
- `defender-overview [--report]`: Védelem, vizsgálati időzítés és összesített fenyegetésáttekintés csak olvasva; nincs művelet.
- `defender-protection [--report]`: Védelmi beállítások és felismert házirendfelületek csak olvasva; bizalmas részletek kizárva; nincs módosítás.
- `defender-scan Quick|Full [--execute] [--report]`: Alapértelmezés szerint előnézet; egyetlen kifejezett, védett Microsoft Defender-vizsgálat.
- `defender-scan-history [--report]`: Gyors és teljes vizsgálat időzítése és késési állapota csak olvasva; bizalmas részletek kizárva; nincs vizsgálat.
- `defender-scan-recover [--report]`: Csak olvasó egyeztetés újabb vizsgálat indítása nélkül.
- `defender-threats [--report]`: Összesített fenyegetéselőzmények, aktuális aktív jelzők és súlyossági azonosítók; bizalmas részletek kizárva; nincs művelet.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Alapértelmezés szerint előnézet; csak nem felügyelt helyi profilok.
- `firewall-enable-recover`: Egy függő módosítás vizsgálata megismétlés nélkül.
- `firewall-events`: A Windows tűzfal figyelmeztetési, hiba- és kritikus működési előzményeinek korlátozott olvasása.
- `firewall-logging`: A Tartományi, a Magán és a Nyilvános profil forgalomnaplózási konfigurációjának olvasása a naplók megnyitása nélkül.
- `firewall-rules`: A tényleges szabályok és házirendforrások adatvédelmi szempontból korlátozott számainak olvasása.
- `firewall-service-events`: A BFE és a Windows Defender tűzfal pontos Szolgáltatásvezérlő-hibaelőzményeinek olvasása.
- `privacy-clipboard-history on|off [--execute]`: Alapértelmezés szerint előnézet; pontos érték az aktuális felhasználóhoz a házirend megőrzésével.
- `privacy-clipboard-history-recover`: Egy megszakított módosítás újraellenőrzése megismétlés nélkül.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: A korábbi érték védett visszaállítása.
- `privacy-plan --select Camera --select ClipboardSync`: Csak a kiválasztások áttekintése; egyetlen beállítás sem változik.
- `privacy-setting KEY on|off [--execute]`: Alapértelmezés szerint előnézet; a házirendet megőrző módosítás az aktuális felhasználóhoz; az érvénytelen kulcsok felsorolják a támogatott lehetőségeket.
- `privacy-setting-recover KEY`: Egy megszakított módosítás újraellenőrzése megismétlés nélkül.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: A pontos érték védett visszaállítása.
- `security-check`: TPM, Biztonságos rendszerindítás, virtualizációs védelmek és UAC-házirend csak olvasva; JSON-bizonyíték.

## Szolgáltatások

- `service-binaries`: A helyi szolgáltatás bináris fájljainak jelenléte és Authenticode-állapota csak olvasva; az útvonalak kizárva, és nem helyi útvonalakhoz nem kapcsolódik.
- `service-check NAME [NAME ...] | service-check --all`: A támogatott szolgáltatások állapota és függőségi bizonyítékai csak olvasva.
- `service-dependencies`: A közvetlen függőségek integritásának áttekintése csak olvasva; nincs szolgáltatásmódosítás.
- `service-dlls`: A regisztrált szolgáltatás-DLL-ek jelenléte és Authenticode-állapota csak olvasva; az útvonalak kizárva, és nem helyi útvonalakhoz nem kapcsolódik.
- `service-events`: Szolgáltatáshiba-előzmények csak olvasva; JSON nyers bizonyítékkal.
- `time-events`: A Windows Idő legutóbbi eseményelőzményei csak olvasva, JSON-bizonyítékkal.
- `service-logons`: A szolgáltatások fiókosztályának és asztali interakciójának áttekintése csak olvasva; fiókazonosítók kizárva.
- `service-path-quoting`: A szolgáltatások végrehajtható fájljai útvonalának idézőjelezési áttekintése csak olvasva; teljes útvonalak és argumentumok kizárva.
- `service-recovery-policy`: A telepített szolgáltatások helyreállítási műveleteinek összegzése csak olvasva; parancsok és újraindítási üzenetek kizárva.
- `service-startup-config`: Az indítási módok összhangjának áttekintése csak olvasva; nincs szolgáltatásmódosítás.
- `service-transitions`: A függő szolgáltatásállapotok ismételt megfigyelése csak olvasva; nincs szolgáltatásművelet.
- `service-triggers`: A telepített szolgáltatások eseményindítóinak összegzése csak olvasva; az eseményindítók hasznos adatai kizárva; nincs módosítás.

## Tárolás, fájlok és biztonsági mentés

- `backup-events`: VSS- és volsnap-előzmények; JSON-bizonyíték; nincs módosítás.
- `backup-service-events`: A VSS, a szoftveres árnyékmásolat-szolgáltató, a Windows biztonsági mentés és a blokkszintű mentési motor pontos hibaelőzményeinek olvasása.
- `backup-summary`: A helyi Windows biztonsági mentés műveleteinek összegzése; nincs mentésmódosítás.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Alapértelmezés szerint előnézet; a Lomtár ürítése mindent vagy semmit jelent, és a kor nem számít.
- `disk-cleanup-task`: A feladat pontos állapota és futási előzményei csak olvasva; nincs feladatfuttatás vagy tisztítás.
- `disk-diagnostic`: A pontos lemezdiagnosztikai feladatok és az eseményelőzmények összevetése; nincs feladat- vagy lemezművelet.
- `disk-diagnostic-tasks`: A gyűjtő és a feloldó feladat pontos előzményei csak olvasva; nincs feladat- vagy lemezművelet.
- `disk-quotas`: Kötetenkénti kvóta-alapértékek és -állapot csak olvasva; felhasználói azonosítók kizárva; nincs módosítás.
- `file-history-events`: A Fájlelőzmények figyelmeztetéseinek és hibáinak adatvédelmi szempontból vetített száma; nincs módosítás.
- `file-history-status`: Az aktuális fiók Fájlelőzmények védelmi állapota; nincs módosítás.
- `search-index-rebuild [--execute]`: Alapértelmezés szerint előnézet; megőrzi a korábbi indexet, és ellenőrzi a friss állapotot.
- `search-index-rebuild-recover`: Csak olvasó egyeztetés a javítóparancsok megismétlése nélkül.
- `storage-check DRIVE_LETTER`: Rögzített, csak olvasó NTFS-ellenőrzés; JSON-eredmények és élő előrehaladás.
- `storage-events`: Tárolási előzmények csak olvasva; JSON nyers bizonyítékkal.
- `storage-jobs`: A Storage modul feladatainak összesített állapota és előrehaladása csak olvasva; JSON; nincs módosítás.
- `storage-reliability`: Hőmérséklet-, kopás- és I/O-számlálók csak olvasva; JSON; nincs javítás.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Alapértelmezés szerint előnézet; a végrehajtáshoz a biztonsági mentés megerősítése szükséges; a rendszerkötetek újraindításra ütemeződnek.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Csak előnézet; nincs javítás vagy ütemezés.
- `storage-repair-recover`: Egy befejezett vagy ütemezett fájlrendszer-javítás ellenőrzése megismétlés nélkül.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Aktuális bizonyíték gyűjtése és kötése; nincs javítás vagy ütemezés.
- `storage-sense-policy`: A felismert gépi tisztítási házirend csak olvasva; a felhasználói beállítások és a fájlazonosítók kizárva.
- `storage-sense-task`: A feladat pontos állapota és futási előzményei csak olvasva; nincs feladatfuttatás vagy tisztítás.
- `storage-service-events`: A Tárolási szolgáltatás, a Meghajtóoptimalizálás, a VSS és a szoftveres árnyékmásolat-szolgáltató pontos hibaelőzményeinek olvasása.
- `volume-capacity DRIVE:`: A rendelkezésre álló hely csak olvasva, JSON-bizonyítékkal.
- `volume-dirty DRIVE:`: A piszkos bit állapota csak olvasva; nincs javítás vagy újraindítás.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Alapértelmezés szerint csak olvasó elemzés; pontos, egészséges kötet és kompatibilis adathordozó szükséges.
- `volume-optimize-recover`: Egy függő, befejezett művelet ellenőrzése megismétlés nélkül.
- `vss-providers`: VSS-szolgáltatók nevei, verziói és típusai csak olvasva; azonosítók kizárva; nincs regisztrációmódosítás.
- `vss-writers`: Jelentett íróállapotok; angol kimenet értelmezése; nincs alaphelyzetbe állítás.

## Nyomtatás

- `print-events`: A PrintService Admin elmúlt hét napjának előzményei csak olvasva; JSON-bizonyíték.
- `print-extension-files`: A nyomtatásfeldolgozók és -figyelők bináris fájljai jelenlétének és aláírásának összegzése csak olvasva; teljes útvonalak kizárva.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Alapértelmezés szerint előnézet; csak pontosan megadott, Hiba vagy Letiltva állapotú feladat.
- `print-job-cancel-recover [--folder PATH]`: Egy megszakított megszakítás újraellenőrzése megismétlés nélkül.
- `print-job-list`: Az aktuális látható feladatok listázása módosítás nélkül.
- `print-operational-events`: A PrintService Operational csatorna állapota és elérhető előzményei csak olvasva; JSON-bizonyíték.
- `print-policy`: Point and Print korlátozások és illesztőprogram-telepítési házirend csak olvasva; JSON-bizonyíték.
- `print-rendering-policy`: Renderelési és illesztőprogram-elkülönítési házirend csak olvasva; JSON-bizonyíték.
- `print-spooler-events`: A helyi Nyomtatásisor-kezelőhöz kötött Szolgáltatásvezérlő-hibák csak olvasva; JSON-bizonyíték.
- `printer-check --queue NAME [--queue NAME ...]`: A kiválasztott helyi sorbeállítások csak olvasva.
- `printer-conditions`: A helyi sorok állapotkódjai csak olvasva; nincs fizikai ellenőrzés vagy módosítás.
- `printer-driver-files`: A regisztrált fájlok jelenlétének és a végrehajtható fájlok aláírásának összegzése csak olvasva; teljes útvonalak kizárva.
- `printer-driver-models`: A 3-as és 4-es verzió és a csomagtámogatás összegzése csak olvasva; JSON-bizonyíték.
- `printer-drivers`: Sor- és illesztőprogram-regisztrációk leltára és egyeztetése csak olvasva; JSON-bizonyíték.
- `printer-endpoint-check`: Korlátozott kapcsolatok a pontosan hozzárendelt szabványos TCP/IP-nyomtatóvégpontokhoz; nincs felderítés, SNMP vagy nyomtatási adat.
- `printer-list`: Helyi sorok felderítése a beállítások módosítása nélkül.
- `printer-ports`: Sor- és portregisztrációk egyeztetése csak olvasva; JSON-bizonyíték.
- `printer-queue-attributes`: A helyi sorok sorba állítási, megőrzési és megosztási attribútumai csak olvasva.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Alapértelmezés szerint előnézet; az offline mód kikapcsolása felszabadíthatja a várakozó feladatokat.

## Hálózat

- `dhcp-check`: A hiányzó vagy lejárt DHCP-bérletek jogosultsága csak olvasva; JSON-bizonyíték, megújítás nélkül.
- `dhcp-events`: A DHCP-ügyfél IPv4- és IPv6-eseményelőzményei csak olvasva; JSON-eredmény.
- `dhcp-leases`: A bérlet időbélyegének, lejáratának és kiszolgálójának bizonyítéka csak olvasva; JSON-eredmény.
- `dhcp-renew [--execute]`: Alapértelmezés szerint előnézet; csak pontosan meghatározott, hiányzó vagy lejárt DHCP-bérletek csatlakoztatott fizikai adaptereken.
- `dhcp-renew-recover`: Csak olvasó egyeztetés újabb bérlet kérése nélkül.
- `dns-cache HOST [--execute]`: Alapértelmezés szerint előnézet; a helyi gyorsítótár védett ürítése a pontos gazdagép ellenőrzésével.
- `dns-cache-recover`: Egy megszakított gyorsítótár-művelet újraellenőrzése az ürítés megismétlése nélkül.
- `dns-cache-summary`: A helyi DNS-gyorsítótár adatvédelmi szempontból korlátozott állapota és rekordtípus-számai; nincs módosítás.
- `dns-check HOST [--timeout-seconds 1..60]`: Korlátozott rendszerszintű névfeloldás; JSON-eredmény.
- `dns-client-config`: Utótag-, regisztrációs, öröklési és házirend-bizonyíték csak olvasva; JSON-eredmény.
- `dns-client-events`: A DNS-ügyfél adatvédelmi szempontból korlátozott figyelmeztetési és hibaelőzményei csak olvasva; JSON-eredmény.
- `dns-nrpt-policy`: A tényleges DNS-névtérházirend bizonyítéka csak olvasva; JSON-eredmény.
- `ip-addresses`: Az aktuális IP-címállapot, -eredet és -ütközés áttekintése csak olvasva; JSON-bizonyíték.
- `ip-interface-config`: Az IP-felületek konfigurációs bizonyítéka csak olvasva; JSON-eredmény.
- `network-firewall`: Az aktív hálózati hozzárendelések összevetése a megfelelő tűzfalprofilok engedélyezésével.
- `network-offload`: A Windows globális hálózati tehermentesítési konfigurációja csak olvasva; JSON-eredmény.
- `network-profiles`: Az aktuális profilkategóriák és kapcsolódási besorolások csak olvasva; hálózatnevek kizárva.
- `network-service-events`: A DHCP, a DNS, a Munkaállomás, az NLA, a Hálózatlista és az IP-segéd pontos hibaelőzményeinek olvasása.
- `proxy-check`: A tárolt WinHTTP-proxy és az aktuális fiók internetes proxybizonyítéka csak olvasva.
- `route-priority`: A tényleges alapértelmezett útvonal metrikáinak összevetése csak olvasva; JSON-bizonyíték.
- `smb-client-config`: SMB-aláírás, vendéghozzáférés és átviteli konfiguráció csak olvasva.
- `smb-connections`: Aktív protokollegyeztetés csak olvasva; távoli azonosítók kizárva.
- `smb-server-config`: SMB-protokoll, -aláírás, -titkosítás és többcsatornás konfiguráció csak olvasva.
- `smb-shares`: Korlátozott SMB-megosztásleltár csak olvasva; útvonalak, engedélyek, munkamenetek és fiókok kizárva.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Korlátozott kapcsolatellenőrzés; alkalmazásadatok nem kerülnek elküldésre.
- `tcp-port-pressure`: Dinamikus TCP-tartományok és összesített helyiport-használat csak olvasva; végpontok és folyamatok kizárva.
- `tcp-settings`: A Windows TCP-sablonok konfigurációs bizonyítéka csak olvasva; JSON-eredmény.
- `vpn-profiles`: Az aktuális fiók VPN-profiljainak konfigurációja csak olvasva; hitelesítő adatok kizárva.
- `winrm-listeners`: A WinRM-figyelők regisztrációs összegzése csak olvasva; tanúsítvány-ujjlenyomatok kizárva.
- `winsock-catalog`: A natív Winsock protokollkatalógus szerkezete csak olvasva; JSON-eredmény.
- `winsock-namespaces`: A natív Winsock névtérszolgáltatók szerkezete csak olvasva; JSON-eredmény.
- `winsock-provider-files`: A protokollszolgáltatói fájlok jelenléte és aláírása csak olvasva; JSON-eredmény.
- `winsock-readiness`: A natív Winsock protokollok, névtérszolgáltatók és szolgáltatói fájlok készenléte csak olvasva; JSON-eredmények.

## Alkalmazások

- `app-folder-review FOLDER_NAME [--report]`: Rögzített gyökerű pontos mappák áttekintése; nincs törlés.
- `app-package-review FULL_NAME [--all-users]`: Pontos célcsomag és függő csomagok; nincs eltávolítás.
- `app-packages [--all-users]`: Csak a regisztrált Appx/MSIX-csomagok leltára; nincs eltávolítás.
- `browser-extensions`: Chrome-, Edge-, Brave- és Firefox-bővítményazonosítók áttekintése; nincs eltávolítás.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Csak áttekintés; nincs rendszermódosítás.
- `depot-review [application and configuration selections] [--json]`: Csak áttekintés; nincs kiépítés.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Authenticode- és gyártói aláíróellenőrzés csak olvasva; semmi sem fut.
- `foxit-inventory`: A gépi termékleltár csak olvasva; JSON-kimenet.
- `installed-apps [--current-user]`: Eltávolítási regisztrációk és képességjelzők; nincs eltávolítás.
- `installed-apps-compare BEFORE.json AFTER.json`: Regisztrációs eltérések a mentett leltárhoz képest; nincs gépmódosítás.
- `installed-hotfixes`: A telepített Windows-gyorsjavítások korlátozott leltára csak olvasva; JSON-bizonyíték.
- `installer-activity`: Az Installer folyamatainak és tranzakciójelölőinek bizonyítéka csak olvasva; JSON-bizonyíték.
- `installer-check`: Az Installer házirendje és a motor készenléte csak olvasva; JSON-bizonyíték.
- `installer-engine-files`: A szükséges Installer-fájlok jelenlétének és Authenticode-állapotának összegzése csak olvasva; JSON-bizonyíték.
- `installer-events`: Az elmúlt hét nap MSI-előzményei csak olvasva; JSON-bizonyíték.
- `installer-logging`: Az Installer naplózási és hibakeresési házirendje csak olvasva; JSON-bizonyíték.
- `installer-pending-state`: A függő fájlműveletek és az Installer-tevékenység összevetése csak olvasva; JSON-bizonyíték.
- `installer-registration-repair [--execute]`: Alapértelmezés szerint előnézet; csak pontosan meghatározott szolgáltatásregisztrációs hiba; a motor regisztrációját soha nem szünteti meg.
- `installer-registration-repair-recover`: Egy megszakított regisztráció újraellenőrzése a parancsok megismétlése nélkül.
- `installer-security-policy`: Az Installer biztonsági és javítócsomag-házirendje csak olvasva; JSON-bizonyíték.
- `installer-service-events`: A helyi Windows Installerhez kötött Szolgáltatásvezérlő-hibák csak olvasva; JSON-bizonyíték.
- `installer-service-registration`: A Windows Installer szolgáltatásregisztrációjának összegzése csak olvasva; JSON-bizonyíték.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: A pontos jogosultság friss előnézete; nincs eltávolító szolgáltató.
- `mace-package-recover`: Egy megszakított alkalmazáscsomag-eltávolítási bejegyzés friss egyeztetése; az eltávolítást soha nem ismétli meg.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Csak áttekintés; nincs eltávolítás.
- `msi-cache`: A regisztrált Windows Installer-gyorsítótár láthatósága csak olvasva; JSON-bizonyíték.
- `msi-code NONNEGATIVE_DECIMAL`: Dokumentált Windows Installer visszatérésikód-referencia; nincs gépvizsgálat vagy javítás.
- `msi-product {PRODUCT-CODE}`: A pontos MSI-regisztrációs állapot csak olvasva; JSON-bizonyíték.
- `msi-repair {PRODUCT-CODE} [--execute]`: Alapértelmezés szerint előnézet; csak pontosan meghatározott sérült vagy hibásan konfigurált termék.
- `msi-repair-recover`: Egy pontos termék megszakított javításának újraellenőrzése megismétlés nélkül.
- `msi-sources`: A regisztrált Windows Installer-források adatvédelmi szempontból korlátozott metaadatai csak olvasva; JSON-bizonyíték.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Alapértelmezés szerint előnézet; a futó fiók pontos gyorsítótárait helyreállítási neveken őrzi meg.
- `orca-cache-reset-recover`: Egy megszakított gyorsítótár-megőrzés ellenőrzése az áthelyezés megismétlése nélkül.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Csak áttekintés; nincs Outlook-módosítás.
- `startup-apps`: Indítási regisztrációk, valamint rendszerindítási és bejelentkezési feladatok áttekintése; nincs letiltás.
- `third-party-update ID SOURCE VERSION [--accept-agreements] [--execute]`: Alapértelmezés szerint előnézet; csak egy pontos verzió.
- `third-party-update-recover`: Egy függő pontos verzió ellenőrzése a frissítés újrapróbálása nélkül.
- `third-party-updates`: Alkalmazásfrissítések felderítése a Microsoft.WinGet.Client segítségével, csak olvasva; nincs alkalmazásmódosítás.

## Rendszer

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Pontos helyi Bluetooth SIG-hozzárendelés-keresés; nincs hálózati kérés.
- `crash-config`: A Windows tárolt memóriakép-módja és -útvonalai; nincs módosítás.
- `crash-dumps`: Az alapértelmezett Windows-memóriaképfájlok metaadatai; nincs tartalomelemzés vagy módosítás.
- `crash-events`: Legutóbbi rendszerhiba-jelentések; nincs diagnózis vagy módosítás.
- `crash-review [--knowledge PACK_PATH ...]`: Legfeljebb 32 csomag; összevont összeomlás-, leállás-, korlátozott minidumpszerkezet- és opcionális pontos leállásikód-referencia-áttekintés; nincs módosítás.
- `default-printer`: Az alapértelmezett kiválasztás és az automatikus kezelés beállítása csak olvasva; JSON-bizonyíték.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Alapértelmezés szerint előnézet; csak hiányzó vagy ütköző alapértelmezett.
- `default-printer-set-recover [--folder PATH]`: Egy megszakított kiválasztás újraellenőrzése megismétlés nélkül.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: Eszközbizonyíték csak olvasva, opcionális pontos illesztőprogram-hivatkozásokkal legfeljebb 32 csomagból.
- `device-code DECIMAL_OR_0xHEX`: Offline Eszközkezelő problémakód-referencia.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Csak referenciajelöltek.
- `event-channel-enable CHANNEL [--execute]`: Alapértelmezés szerint előnézet; egyetlen pontos, nem felügyelt diagnosztikai csatornát engedélyez, és megőrzi a többi beállítást.
- `event-channel-enable-recover`: Csak olvasó egyeztetés az engedélyező parancs megismétlése nélkül.
- `event-channels`: Négy diagnosztikai csatorna állapota, megőrzése, mérete és telítettsége csak olvasva.
- `font-registrations`: Korlátozott gépi és aktuális felhasználói regisztráció- és fájljelenlét-ellenőrzés csak olvasva.
- `memory-diagnostic-results`: A Windows Memóriadiagnosztika korlátozott előzményei csak olvasva; JSON-bizonyíték.
- `minidump-analyze PATH`: Korlátozott szerkezeti minidump-metaadatok; nincsenek memórialapok, szimbólumok vagy módosítások.
- `name-rename PROPOSED_NAME [--execute]`: Alapértelmezés szerint előnézet; csak önálló munkacsoportos PC-k; a Windowst soha nem indítja újra.
- `name-rename-recover`: Egy függő átnevezés ellenőrzése megismétlés nélkül.
- `name-review PROPOSED_NAME`: Az azonosító és a javasolt név áttekintése csak olvasva.
- `profiles --recover`: A rögzített hiány ellenőrzése és a helyreállítási bizonyíték megőrzése; a törlést soha nem próbálja újra.
- `profiles --recover-backup`: Egy függő mentési összetevő ellenőrzése és archiválása; a másolást soha nem próbálja újra.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Először előnézet; a védett natív másoláshoz kifejezett végrehajtás és egy nem létező helyi NTFS-cél szükséges.
- `profiles --select SID --create-backup-artifact DESTINATION`: Egy meglévő másolt mentés összevetése és rögzített összetevőjének létrehozása; nem másol és nem távolít el profilt.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Csak olvasó áttekintés; az ellenőrzéshez egy kiválasztott SID szükséges.
- `task-check`: Feladatleltár, osztályozott megállapítások és JSON-bizonyíték csak olvasva.
- `task-definition TASK_FOLDER TASK_NAME`: A pontos feladat-XML csak olvasva; az elérhetetlen nem jelenti azt, hogy hiányzik.
- `task-inventory`: Feladatazonosítók és -állapotok csak olvasva; JSON-kimenet.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Korábbi helyreállítás áttekintése; soha nem jogosít fel újrapróbálásra.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Feladatvázlat érvényesítése és áttekintése; nincs regisztráció.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Alapértelmezés szerint előnézet; letiltott, csak létrehozható Local System-definíció.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Pontos definíció egyeztetése csak olvasva; feladatot soha nem próbál újra és nem futtat.
- `task-xml PLAN.json YYYY-MM-DD`: Letiltott áttekintő XML a stdout kimenetre; csak helyi időzónás naptári ütemezések; nincs regisztráció.
- `time-config`: A Windows Idő módjának, társainak és házirendjének bizonyítéka csak olvasva.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Alapértelmezés szerint előnézet; az egyéni mód társakat igényel.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Csak előnézet.
- `time-recover --folder PATH`: Egy függő bejegyzés ellenőrzése és archiválása; a módosításokat soha nem próbálja újra.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Csak olvasva; az egyéni mód társakat igényel.
- `wmi-provider`: Helyi operációsrendszer-szolgáltató lekérdezése csak olvasva; JSON-bizonyíték.

## Tudás, érvényesítés és Engine

- `knowledge PACK_PATH CONTEXT CODE`: Helyi referenciakeresés; nincs javításvégrehajtás.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Ellenőrzött helyi aktiválás; nincs letöltés.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Az aktív helyi csomag ellenőrzése; nincs módosítás.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Csak referenciamódosítások; nincs csomagtelepítés.
- `knowledge-info PACK_PATH`: Csomag-metaadatok és kontextusonkénti bejegyzésszámok; nincs módosítás.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Kontextuális hivatkozások csatolása a megfigyelt kódokhoz.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: A legújabb ellenőrzött megőrzött csomag kiválasztása; nincs letöltés.
- `knowledge-search PACK_PATH CONTEXT TEXT`: Legfeljebb 50 referenciaszöveg-egyezés; nincs javításvégrehajtás.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Egyetlen pontos azonosító feloldása legfeljebb 32 csomagban; ütközést soha nem választ ki.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Minden beállított telepített adatbázis ellenőrzése; nincs letöltés vagy aktiválás.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Aláírt adatbázisjelöltek lekérése és atomi aktiválása a beállított HTTPS-végpontokról.
- `open <file.kbench> [--json]`: Egy mentett rögzítés újbóli megnyitása és ismételt kiértékelése a telepített szabályokkal.
- `scan --output <new-file.kbench> [--json]`: Csak olvasó Windows Update-rögzítés új fájlba mentve.
- `validation-coverage RECORD.json [...]`: Az aktuális verzió natív VM-bizonyítékainak összegzése; nincs diagnosztika vagy javítás.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Csak olvasó forgatókönyvterv eldobható VM-ekhez; nincs diagnosztika vagy javítás.
- `validation-record RECORD.json`: Egyetlen korlátozott natív VM-bizonyítékbejegyzés érvényesítése; nincs diagnosztika vagy javítás.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Helyi VM-lefedettségi jelentés az aktuális verzióhoz.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Érvényesített, csak olvasó közös beállításterv futtatása.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Csak leltár.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Célzott diagnosztikai napló; nincs javítás.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Csak naplózott áttekintés.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: Opcionális pontos termékállapot a naplóban.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: A kiválasztott cél opcionális diagnózisa a naplóban.
- `workflow storage [--volume D:] [--json]`: Naplózott, csak olvasó fájlrendszer-ellenőrzés.
- `workflow-journal FILE.json`: Korábbi tartalom; a 0 kilépési kód sikeres olvasást jelent, nem egészséges állapotot.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Gazdafüggetlen vezérlők megjelenítése vagy érvényesítése; nincs vizsgálat vagy javítás.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Csak korábbi jelentés; parancs nem fut.

## Egyéb parancsok

- `<analyze|investigate> <text-log-path> [--json]`: Helyi szöveges bizonyíték elemzése.
- `adapter-bindings`: Az alapvető protokollkötések összegzése csak olvasva; JSON-bizonyíték.
- `adapter-statistics`: Az adapterek bájt-, hiba- és eldobásszámlálói csak olvasva; JSON-bizonyíték.
- `application-events`: Legutóbbi alkalmazáshibák, csoportosított számok és JSON-bizonyíték csak olvasva.
- `applocker-policy`: Tényleges gyűjtési módok és összesített szabályszámok csak olvasva; a szabályok részletei kizárva; nincs módosítás.
- `common-check [--scope NAME ...]`: Alapértelmezés szerint minden diagnosztikai terület; megadás esetén csak a kiválasztott területek; csak olvasó JSON.
- `delete-notifications`: Az NTFS és a ReFS törlésiértesítés-konfigurációja csak olvasva; JSON; nincs optimalizálás vagy beállításmódosítás.
- `diagnostic-scopes [--all]`: A 11 ajánlott eredménycsoport vagy mind a 49 csak olvasó terület listázása; nincs gyűjtés.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Célzott diagnosztikai napló; nincs javítás.
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Minden diagnosztikai terület; nincs javítás, hacsak nincs kifejezetten megadva a `--auto-repair`.
- `gateway-check`: A pontosan beállított alapértelmezett következő ugrások korlátozott ICMP-ellenőrzése; nincs címvizsgálat.
- `infrastructure-service-events`: Egyetlen korlátozott, pontos pillanatkép olvasása a kapcsolódási, távfelügyeleti, értesítési és alapvető szolgáltatások hibáiról.
- `investigate-batch FILE [FILE ...]`: Legfeljebb 12 fájl; JSON Lines; 3-as kilépési kód sikertelen vagy részleges importálás esetén.
- `licensing-check`: A Windows aktiválási állapota termékkulcsok nélkül, csak olvasva; JSON-bizonyíték.
- `mac-vendor PACK_PATH MAC_ADDRESS`: Leghosszabb előtag szerinti helyi csomagkeresés; nincs hálózati kérés.
- `ncsi-policy`: A Windows kapcsolódásibesorolás-házirendje csak olvasva; a mintavételi célok kizárva.
- `neighbor-cache`: A szomszédok elérhetőségének adatvédelmi szempontból korlátozott bizonyítéka csak olvasva; JSON-eredmény.
- `ntfs-self-healing DRIVE:`: A kiválasztott kötet online NTFS-javítási konfigurációja csak olvasva; nincs javítás vagy beállításmódosítás.
- `pagefile-check`: A lapozófájl kezelésének, foglalásának és használatának összegzése csak olvasva; teljes útvonalak kizárva.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Pontos helyi csomagkeresés; nincs hálózati kérés vagy illesztőprogram-kiválasztás.
- `physical-disks`: Az aktuális állapot, működés, adathordozó, busz és használat csak olvasva; JSON; nincs javítás.
- `platform-service-events`: Egyetlen korlátozott, pontos pillanatkép olvasása az alkalmazás-, identitás-, profil- és eszköztámogatási szolgáltatások hibáiról.
- `previous-windows [--execute --backup-confirmed]`: Alapértelmezés szerint előnézet; a DISM segítségével véglegesen eltávolítja a Windows visszaállításának lehetőségét.
- `previous-windows-recover`: Csak olvasó egyeztetés a visszaállítási lehetőség eltávolításának megismétlése nélkül.
- `provisioned-packages`: Az új felhasználók számára előkészített csomagok a helyi Windowsban; nincs eltávolítás.
- `recipes [WORKFLOW]`: A telepített definíciók vagy a folyamatspecifikus függőségek és korlátok listázása; nincs végrehajtás.
- `repair <dism|sfc> [--execute]`: Alapértelmezés szerint előnézet.
- `reserved-storage`: A futó Windows fenntartott tárhelyének állapota csak olvasva; JSON-bizonyíték.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Alapértelmezés szerint előnézet; friss ellenőrzések után egyetlen pontot hoz létre.
- `restore-point-recover`: Egy függő kérés ellenőrzése újabb pont létrehozása nélkül.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Csak előnézet.
- `shadow-copies`: Az aktuális VSS-pillanatképek állapota és attribútumai csak olvasva; azonosítók kizárva; nincs pillanatkép-módosítás.
- `shadow-storage`: A VSS különbségi terület foglalása és terhelése csak olvasva; JSON; nincs átméretezés vagy pillanatkép-művelet.
- `uac-policy`: Az UAC engedélyezése és a jogosultságemelési kérés házirendje csak olvasva; nincsenek fiók- vagy hitelesítő adatok.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Pontos helyi UEFI-jegyzékkeresés; nincs hálózati kérés vagy illesztőprogram-kiválasztás.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Pontos helyi csomagkeresés; nincs hálózati kérés vagy illesztőprogram-kiválasztás.
- `vitals-check`: Windows-, hardver-, fiók-, erőforrás-, akkumulátor- és fizikailemez-pillanatképek csak olvasva; JSON-bizonyíték.
- `win32-code DECIMAL_OR_0xHEX`: A telepített Windows üzeneteinek offline keresése.
- `workflows`: A telepített folyamatkulcsok és -címek listázása; nincs vizsgálat vagy javítás.
