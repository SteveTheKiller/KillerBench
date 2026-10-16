# Wiersz polecenia

`killerbench-cli` uruchamia ten sam Engine co aplikacja komputerowa, z poziomu PowerShell lub konsoli zdalnej. Każde polecenie, które mogłoby zmienić komputer, domyślnie wykonuje podgląd i przed działaniem wymaga jawnej flagi, takiej jak `--execute`. Polecenia wypisują dowody JSON na standardowe wyjście, a postęp na standardowe wyjście błędów.

Przewodniki krok po kroku: [Uruchamiaj KillerBench ze skryptów](use-the-cli.html).

Kod zakończenia 0 oznacza, że podgląd lub zweryfikowany wynik się powiódł, 1 oznacza błąd uruchamiania, danych wejściowych lub dziennika, 2 oznacza, że argumenty nie zostały rozpoznane, 3 oznacza wynik zablokowany, nieudany lub niezweryfikowany, 4 oznacza, że proces wymaga ponownego uruchomienia przed weryfikacją, a 130 oznacza, że go zatrzymano.

Uruchom `killerbench-cli` bez argumentów, aby wyświetlić tę listę na własnym komputerze.

## Aktualizacje i obsługa

- `bits-job-review JOB_GUID [--all-users] [--observe]`: Dokładny przegląd transferu lub obserwacja w dwóch migawkach; bez ponawiania.
- `bits-jobs [--all-users]`: Stany, właściciele i liczby bajtów zadań BITS; bez zmian transferów.
- `bits-retry JOB_GUID [--execute]`: Domyślnie podgląd; jawne ponowienie dokładnie wskazanego zadania, bez twierdzenia o zakończeniu przez aplikację.
- `bits-retry-recover JOB_GUID`: Dwie migawki tylko do odczytu; archiwizuje stabilne wyniki bez kolejnego ponowienia.
- `component-repair-source`: Zarządzane zasady źródła naprawy DISM i konflikty; bez zmian.
- `delivery-optimization`: Stan usługi Optymalizacja dostarczania tylko do odczytu; dowody JSON.
- `delivery-optimization-activity`: Ograniczone ze względu na prywatność bieżące liczby zadań i sumy bajtów; bez tożsamości transferów i bez zmian.
- `delivery-optimization-policy`: Ustawienia klasyczne i Policy CSP tylko do odczytu; poufne wartości i tożsamości dostawców wykluczone.
- `feature-service-events`: Odczytuje dokładną historię awarii usług czasu, Search, WMI, Dziennika zdarzeń i dźwięku.
- `feature-update-policy`: Zasady wersji docelowej, odroczeń, wstrzymań i zabezpieczeń tylko do odczytu; dowody JSON.
- `pending-file-operations`: Ograniczone ze względu na prywatność podsumowanie oczekujących operacji na plikach, tylko do odczytu; dowody JSON.
- `restart-check`: Źródła oczekującego ponownego uruchomienia tylko do odczytu; dowody JSON.
- `servicing-logs`: Ograniczona analiza najnowszych dzienników CBS, archiwów CBS tekstowych i skompresowanych, DISM i Instalatora systemu Windows; bez naprawy.
- `servicing-monitor [--refresh 1..60] [--samples 1..120] [--skip-events] [--skip-history] [--skip-services] [--skip-restart] [--skip-setup] [--skip-compatibility] [--skip-readiness] [--skip-transfers] [--skip-delivery-optimization] [--skip-installer] [--skip-scheduled-task]`: Migawki aktywności obsługi tylko do odczytu.
- `servicing-service-events`: Dokładna historia awarii łańcucha obsługi Windows Update, tylko do odczytu.
- `servicing-service-states`: Bieżący stan sześciu usług łańcucha obsługi Windows Update, tylko do odczytu.
- `setup-code DECIMAL_OR_0xHEX`: Udokumentowany opis kodów wyniku Instalatora systemu Windows; bez sprawdzania komputera i bez naprawy.
- `update-applicability`: Dowody wersji, kompilacji i architektury systemu Windows, tylko do odczytu.
- `update-cache`: Ograniczone sumy pamięci podręcznej SoftwareDistribution tylko do odczytu; bez nazw plików.
- `update-code DECIMAL_OR_0xHEX`: Udokumentowany opis kodów wyniku Windows Update; bez sprawdzania komputera i bez naprawy.
- `update-context`: Dowody bieżącego podniesienia uprawnień i sesji pulpitu tylko do odczytu; bez identyfikatorów kont.
- `update-engine-files`: Dowody obecności, wersji i podpisu natywnego aparatu Windows Update, tylko do odczytu.
- `update-events`: Historia klienta Windows Update z ostatnich siedmiu dni tylko do odczytu; JSON z surowymi dowodami.
- `update-followup DECIMAL_OR_0xHEX`: Świeża diagnostyka tylko do odczytu dobrana według udokumentowanego kodu wyniku Windows Update.
- `update-history`: 100 najnowszych wyników operacji Windows Update tylko do odczytu; dowody JSON.
- `update-plan --select GUID:REVISION [--drivers] [--no-software] [--allow-restart-required] [--json] [--execute]`: Powtórz `--select`; domyślnie podgląd.
- `update-policy`: Zarządzane zasady Windows Update tylko do odczytu; dowody JSON.
- `update-policy-surfaces`: Dowody obszaru konfiguracji klasycznej i Policy CSP tylko do odczytu; wartości i tożsamości dostawców wykluczone.
- `update-recover`: Sprawdza oczekującą aktualizację; nigdy jej nie instaluje ani nie ponawia.
- `update-restart-policy`: Terminy zgodności, okresy prolongaty, wstrzymywanie ponownego uruchomienia i zachowanie powiadomień, tylko do odczytu.
- `update-search [--drivers] [--no-software] [--filter TITLE] [--downloaded-only] [--classification GUID] [--product GUID]`: Wykrywanie i dopasowania JSON; bez instalacji.
- `update-service-events`: Dokładna historia awarii usługi Windows Update, tylko do odczytu.
- `update-service-files`: Dokładne dowody rejestracji i podpisu biblioteki DLL usługi Windows Update, tylko do odczytu.
- `update-services`: Rejestracje usług Windows Update tylko do odczytu; dowody JSON.
- `update-store-reset [--source Configured|WindowsUpdate|MicrosoftUpdate] [--drivers] [--no-software] [--execute]`: Wymaga świeżego wyszukiwania; domyślnie podgląd; zachowuje stary magazyn.
- `update-store-reset-recover`: Uzgadnia przygotowany reset magazynu bez jego powtarzania.
- `windows-capabilities`: Stany możliwości działającego systemu Windows tylko do odczytu; wynik JSON.
- `windows-update-task`: Dokładny stan i historia uruchomień zadania Scheduled Start tylko do odczytu; bez uruchamiania zadania i bez operacji aktualizacji.

## BitLocker i TPM

- `bitlocker-auto-unlock DRIVE: [--execute]`: Domyślnie podgląd; włącza automatyczne odblokowywanie zaszyfrowanego woluminu danych.
- `bitlocker-auto-unlock-recover`: Weryfikuje przerwane żądanie automatycznego odblokowywania bez jego powtarzania.
- `bitlocker-backup DRIVE: PROTECTOR_GUID FILE_PATH [--execute]`: Domyślnie podgląd; tylko nowy plik kopii odzyskiwania.
- `bitlocker-backup-recover DRIVE: PROTECTOR_GUID FILE_PATH`: Weryfikuje przerwane tworzenie kopii bez ponownego zapisu.
- `bitlocker-decrypt DRIVE: [--execute --confirm-unprotected]`: Domyślnie podgląd; rozpoczyna odszyfrowywanie, na przykład przed ponownym szyfrowaniem wymaganą metodą.
- `bitlocker-decrypt-recover`: Weryfikuje przerwane żądanie odszyfrowywania bez jego powtarzania.
- `bitlocker-encrypt DRIVE: RECOVERY_PROTECTOR_GUID NEW_BACKUP_FILE [--encryption XtsAes128] [--used-space-only] [--execute]`: Domyślnie podgląd; wymaga istniejących funkcji ochrony TPM i odzyskiwania.
- `bitlocker-encrypt-recover`: Weryfikuje oczekujący stan konwersji bez powtarzania żądania.
- `bitlocker-escrow DRIVE: RECOVERY_PROTECTOR_GUID ad|entra [--execute]`: Domyślnie podgląd; tworzy kopię istniejącego hasła odzyskiwania w Active Directory lub Microsoft Entra ID.
- `bitlocker-plan --volume C: --operation Encrypt --encryption XtsAes256 --backup EntraId`: Tylko podgląd opcji.
- `bitlocker-protector-add DRIVE: tpm|recovery [--execute]`: Domyślnie podgląd; dodaje jedną funkcję ochrony TPM lub hasłem odzyskiwania; hasło nigdy nie jest wyświetlane.
- `bitlocker-protector-recover`: Weryfikuje przerwane żądanie dotyczące funkcji ochrony bez jego powtarzania.
- `bitlocker-protectors DRIVE:`: Tylko tożsamości i typy funkcji ochrony; bez haseł odzyskiwania.
- `bitlocker-resume DRIVE: [--execute]`: Domyślnie podgląd; tylko dokładnie wskazany, w pełni zaszyfrowany i wstrzymany wolumin.
- `bitlocker-resume-recover`: Weryfikuje oczekujący stan ochrony bez powtarzania żądania.

## Zabezpieczenia i prywatność

- `certificate-chains`: Zaufanie łańcucha certyfikatów osobistych tylko z lokalnej pamięci podręcznej, tylko do odczytu; bez sprawdzania odwołań i bez pobierania.
- `defender-overview [--report]`: Ochrona, czas skanowania i zbiorczy przegląd zagrożeń tylko do odczytu; bez akcji.
- `defender-protection [--report]`: Preferencje ochrony i rozpoznane obszary zasad tylko do odczytu; poufne szczegóły wykluczone; bez zmian.
- `defender-scan Quick|Full [--execute] [--report]`: Domyślnie podgląd; jedno jawne, chronione skanowanie Microsoft Defender.
- `defender-scan-history [--report]`: Czas szybkiego i pełnego skanowania oraz stan zaległości tylko do odczytu; poufne szczegóły wykluczone; bez skanowania.
- `defender-scan-recover [--report]`: Uzgadnianie tylko do odczytu bez uruchamiania kolejnego skanowania.
- `defender-threats [--report]`: Zbiorcza historia zagrożeń, bieżące flagi aktywności i identyfikatory ważności; poufne szczegóły wykluczone; bez akcji.
- `firewall-enable --profile Domain|Private|Public [--profile ...] [--execute]`: Domyślnie podgląd; tylko niezarządzane profile lokalne.
- `firewall-enable-recover`: Sprawdza oczekującą zmianę bez jej powtarzania.
- `firewall-events`: Odczytuje ograniczoną historię operacyjną ostrzeżeń, błędów i zdarzeń krytycznych Zapory systemu Windows.
- `firewall-logging`: Odczytuje konfigurację rejestrowania ruchu profili Domena, Prywatny i Publiczny bez otwierania dzienników.
- `firewall-rules`: Odczytuje ograniczone ze względu na prywatność liczby obowiązujących reguł i źródeł zasad.
- `firewall-service-events`: Odczytuje dokładną historię awarii BFE i Zapory systemu Windows Defender z Menedżera sterowania usługami.
- `privacy-clipboard-history on|off [--execute]`: Domyślnie podgląd; dokładna wartość bieżącego użytkownika z zachowaniem zasad.
- `privacy-clipboard-history-recover`: Ponownie sprawdza przerwaną zmianę bez jej powtarzania.
- `privacy-clipboard-history-undo VERIFIED_JOURNAL [--execute]`: Chronione przywrócenie poprzedniej wartości.
- `privacy-plan --select Camera --select ClipboardSync`: Tylko przegląd wyborów; żadne ustawienia nie są zmieniane.
- `privacy-setting KEY on|off [--execute]`: Domyślnie podgląd; zmiana dla bieżącego użytkownika z zachowaniem zasad; nieprawidłowe klucze wyświetlają obsługiwane wybory.
- `privacy-setting-recover KEY`: Ponownie sprawdza przerwaną zmianę bez jej powtarzania.
- `privacy-setting-undo KEY VERIFIED_JOURNAL [--execute]`: Chronione przywrócenie dokładnej wartości.
- `security-check`: TPM, Bezpieczny rozruch, zabezpieczenia wirtualizacji i zasady UAC tylko do odczytu; dowody JSON.

## Usługi

- `service-binaries`: Obecność lokalnych plików binarnych usług i stan Authenticode tylko do odczytu; ścieżki wykluczone, a ścieżki nielokalne nie są kontaktowane.
- `service-check NAME [NAME ...] | service-check --all`: Stany obsługiwanych usług i dowody zależności tylko do odczytu.
- `service-dependencies`: Przegląd integralności bezpośrednich zależności tylko do odczytu; bez zmian usług.
- `service-dlls`: Obecność zarejestrowanych bibliotek DLL usług i stan Authenticode tylko do odczytu; ścieżki wykluczone, a ścieżki nielokalne nie są kontaktowane.
- `service-events`: Historia awarii usług tylko do odczytu; JSON z surowymi dowodami.
- `time-events`: Ostatnia historia zdarzeń Czasu systemu Windows tylko do odczytu z dowodami JSON.
- `service-logons`: Przegląd klas kont usług i interakcji z pulpitem tylko do odczytu; tożsamości kont wykluczone.
- `service-path-quoting`: Przegląd cudzysłowów w ścieżkach plików wykonywalnych usług tylko do odczytu; pełne ścieżki i argumenty wykluczone.
- `service-recovery-policy`: Podsumowanie akcji odzyskiwania zainstalowanych usług tylko do odczytu; polecenia i komunikaty ponownego uruchomienia wykluczone.
- `service-startup-config`: Przegląd spójności trybów uruchamiania tylko do odczytu; bez zmian usług.
- `service-transitions`: Powtarzana obserwacja oczekujących stanów usług tylko do odczytu; bez akcji na usługach.
- `service-triggers`: Podsumowanie wyzwalaczy zainstalowanych usług tylko do odczytu; ładunki wyzwalaczy wykluczone; bez zmian.

## Magazyn, pliki i kopie zapasowe

- `backup-events`: Historia VSS i volsnap; dowody JSON; bez zmian.
- `backup-service-events`: Odczytuje dokładną historię awarii VSS, dostawcy kopiowania w tle oprogramowania, Kopii zapasowej systemu Windows i aparatu kopii na poziomie bloku.
- `backup-summary`: Podsumowanie operacji lokalnej Kopii zapasowej systemu Windows; bez zmian kopii zapasowych.
- `cleanup --user-temp [--windows-temp] [--system-temp] [--shader-caches] [--browser-caches] [--office-caches] [--thumbnail-cache] [--icon-cache] [--error-reports] [--crash-dumps] [--windows-minidumps] [--delivery-optimization-cache] [--recycle-bin] [--days 7] [--max-files 100] [--json] [--execute]`: Domyślnie podgląd; Kosz jest opróżniany w całości lub wcale, a wiek plików nie ma zastosowania.
- `disk-cleanup-task`: Dokładny stan i historia uruchomień zadania tylko do odczytu; bez uruchamiania zadania i bez czyszczenia.
- `disk-diagnostic`: Zestawia dokładne zadania diagnostyki dysków z historią zdarzeń; bez akcji na zadaniach i dyskach.
- `disk-diagnostic-tasks`: Dokładna historia zadań modułu zbierającego i rozpoznawania tylko do odczytu; bez akcji na zadaniach i dyskach.
- `disk-quotas`: Domyślne ustawienia i stan przydziałów dla każdego woluminu tylko do odczytu; tożsamości użytkowników wykluczone; bez zmian.
- `file-history-events`: Liczby ostrzeżeń i błędów Historii plików z ograniczeniem ze względu na prywatność; bez zmian.
- `file-history-status`: Stan ochrony Historii plików bieżącego konta; bez zmian.
- `search-index-rebuild [--execute]`: Domyślnie podgląd; zachowuje poprzedni indeks i weryfikuje świeży stan.
- `search-index-rebuild-recover`: Uzgadnianie tylko do odczytu bez powtarzania poleceń naprawy.
- `storage-check DRIVE_LETTER`: Stała kontrola NTFS tylko do odczytu; wyniki JSON i postęp na żywo.
- `storage-events`: Historia magazynu tylko do odczytu; JSON z surowymi dowodami.
- `storage-jobs`: Zbiorczy stan i postęp zadań modułu Storage tylko do odczytu; JSON; bez zmian.
- `storage-reliability`: Liczniki temperatury, zużycia i operacji we/wy tylko do odczytu; JSON; bez naprawy.
- `storage-repair DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount] [--backup-confirmed] [--execute]`: Domyślnie podgląd; wykonanie wymaga potwierdzenia kopii zapasowej; woluminy systemowe są planowane na ponowne uruchomienie.
- `storage-repair-plan DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Tylko podgląd; bez naprawy i bez planowania.
- `storage-repair-recover`: Weryfikuje zakończoną lub zaplanowaną naprawę systemu plików bez jej powtarzania.
- `storage-repair-review DRIVE: SpotFix|OfflineScanAndFix [--allow-dismount]`: Zbiera i wiąże bieżące dowody; bez naprawy i bez planowania.
- `storage-sense-policy`: Rozpoznane zasady czyszczenia komputera tylko do odczytu; ustawienia użytkownika i tożsamości plików wykluczone.
- `storage-sense-task`: Dokładny stan i historia uruchomień zadania tylko do odczytu; bez uruchamiania zadania i bez czyszczenia.
- `storage-service-events`: Odczytuje dokładną historię awarii Usługi magazynu, Optymalizacji dysków, VSS i dostawcy kopiowania w tle oprogramowania.
- `volume-capacity DRIVE:`: Dostępne miejsce tylko do odczytu z dowodami JSON.
- `volume-dirty DRIVE:`: Stan bitu zabrudzenia tylko do odczytu; bez naprawy i bez ponownego uruchomienia.
- `volume-optimize DRIVE: [Recommended|ReTrim|Defragment] [--execute]`: Domyślnie analiza tylko do odczytu; wymagany dokładnie wskazany, sprawny wolumin i zgodny nośnik.
- `volume-optimize-recover`: Weryfikuje oczekującą zakończoną operację bez jej powtarzania.
- `vss-providers`: Nazwy, wersje i typy dostawców VSS tylko do odczytu; identyfikatory wykluczone; bez zmian rejestracji.
- `vss-writers`: Zgłaszane stany modułów zapisujących; interpretacja wyjścia w języku angielskim; bez resetowania.

## Drukowanie

- `print-events`: Historia PrintService Admin z ostatnich siedmiu dni tylko do odczytu; dowody JSON.
- `print-extension-files`: Podsumowanie obecności i podpisów plików binarnych procesorów i monitorów wydruku tylko do odczytu; pełne ścieżki wykluczone.
- `print-job-cancel QUEUE JOB_ID [--execute] [--folder PATH]`: Domyślnie podgląd; tylko dokładnie wskazane zadanie w stanie Błąd lub Zablokowane.
- `print-job-cancel-recover [--folder PATH]`: Ponownie sprawdza przerwane anulowanie bez jego powtarzania.
- `print-job-list`: Wyświetla bieżące widoczne zadania bez ich zmieniania.
- `print-operational-events`: Stan kanału PrintService Operational i dostępna historia tylko do odczytu; dowody JSON.
- `print-policy`: Ograniczenia Point and Print i zasady instalacji sterowników tylko do odczytu; dowody JSON.
- `print-rendering-policy`: Zasady renderowania i izolacji sterowników tylko do odczytu; dowody JSON.
- `print-spooler-events`: Awarie Menedżera sterowania usługami powiązane z tożsamością lokalnego Bufora wydruku, tylko do odczytu; dowody JSON.
- `printer-check --queue NAME [--queue NAME ...]`: Wybrane ustawienia kolejek lokalnych tylko do odczytu.
- `printer-conditions`: Kody stanu kolejek lokalnych tylko do odczytu; bez weryfikacji fizycznej i bez zmian.
- `printer-driver-files`: Podsumowanie obecności zarejestrowanych plików i podpisów plików wykonywalnych tylko do odczytu; pełne ścieżki wykluczone.
- `printer-driver-models`: Podsumowanie wersji 3, wersji 4 i obsługi pakietów tylko do odczytu; dowody JSON.
- `printer-drivers`: Inwentarz i dopasowanie rejestracji kolejek i sterowników tylko do odczytu; dowody JSON.
- `printer-endpoint-check`: Ograniczone połączenia z dokładnie przypisanymi standardowymi punktami końcowymi drukarek TCP/IP; bez wykrywania, SNMP i danych wydruku.
- `printer-list`: Wykrywa kolejki lokalne bez zmiany ustawień.
- `printer-ports`: Dopasowanie rejestracji kolejek i portów tylko do odczytu; dowody JSON.
- `printer-queue-attributes`: Atrybuty buforowania, przechowywania i udostępniania kolejek lokalnych tylko do odczytu.
- `printer-repair --queue NAME [--queue NAME ...] [--execute]`: Domyślnie podgląd; wyłączenie trybu offline może zwolnić oczekujące zadania.

## Sieć

- `dhcp-check`: Kwalifikacja brakujących lub wygasłych dzierżaw DHCP tylko do odczytu; dowody JSON, bez odnowienia.
- `dhcp-events`: Historia zdarzeń Klienta DHCP dla IPv4 i IPv6 tylko do odczytu; wynik JSON.
- `dhcp-leases`: Dowody znaczników czasu, wygaśnięcia dzierżawy i serwera tylko do odczytu; wynik JSON.
- `dhcp-renew [--execute]`: Domyślnie podgląd; tylko dokładnie wskazane brakujące lub wygasłe dzierżawy DHCP podłączonych kart fizycznych.
- `dhcp-renew-recover`: Uzgadnianie tylko do odczytu bez żądania kolejnej dzierżawy.
- `dns-cache HOST [--execute]`: Domyślnie podgląd; chronione opróżnienie lokalnej pamięci podręcznej z weryfikacją dokładnego hosta.
- `dns-cache-recover`: Ponownie sprawdza przerwaną operację na pamięci podręcznej bez powtarzania opróżnienia.
- `dns-cache-summary`: Ograniczony ze względu na prywatność stan lokalnej pamięci podręcznej DNS i liczby typów rekordów; bez zmian.
- `dns-check HOST [--timeout-seconds 1..60]`: Ograniczone systemowe rozpoznawanie nazw; wynik JSON.
- `dns-client-config`: Dowody sufiksów, rejestracji, dewolucji i zasad tylko do odczytu; wynik JSON.
- `dns-client-events`: Ograniczona ze względu na prywatność historia ostrzeżeń i awarii Klienta DNS tylko do odczytu; wynik JSON.
- `dns-nrpt-policy`: Dowody obowiązujących zasad przestrzeni nazw DNS tylko do odczytu; wynik JSON.
- `ip-addresses`: Przegląd bieżącego stanu, pochodzenia i konfliktów adresów IP tylko do odczytu; dowody JSON.
- `ip-interface-config`: Dowody konfiguracji interfejsów IP tylko do odczytu; wynik JSON.
- `network-firewall`: Zestawia aktywne przypisania sieci z włączeniem odpowiednich profili zapory.
- `network-offload`: Globalna konfiguracja odciążania sieci w systemie Windows tylko do odczytu; wynik JSON.
- `network-profiles`: Bieżące kategorie profili i klasyfikacje łączności tylko do odczytu; nazwy sieci wykluczone.
- `network-service-events`: Odczytuje dokładną historię awarii usług DHCP, DNS, Stacja robocza, NLA, Lista sieci i Pomocnik IP.
- `proxy-check`: Zapisane dowody serwera proxy WinHTTP i internetowego serwera proxy bieżącego konta, tylko do odczytu.
- `route-priority`: Porównanie metryk obowiązującej trasy domyślnej tylko do odczytu; dowody JSON.
- `smb-client-config`: Konfiguracja podpisywania SMB, dostępu gościa i transportu tylko do odczytu.
- `smb-connections`: Aktywne negocjowanie protokołu tylko do odczytu; tożsamości zdalne wykluczone.
- `smb-server-config`: Konfiguracja protokołu, podpisywania, szyfrowania i wielokanałowości SMB tylko do odczytu.
- `smb-shares`: Ograniczony inwentarz udziałów SMB tylko do odczytu; ścieżki, uprawnienia, sesje i konta wykluczone.
- `tcp-check HOST PORT [--timeout-seconds 1..60]`: Ograniczona kontrola połączenia; bez wysyłania danych aplikacji.
- `tcp-port-pressure`: Dynamiczne zakresy TCP i zbiorcze użycie portów lokalnych tylko do odczytu; punkty końcowe i procesy wykluczone.
- `tcp-settings`: Dowody konfiguracji szablonów TCP systemu Windows tylko do odczytu; wynik JSON.
- `vpn-profiles`: Konfiguracja profili VPN bieżącego konta tylko do odczytu; poświadczenia wykluczone.
- `winrm-listeners`: Podsumowanie rejestracji odbiorników WinRM tylko do odczytu; odciski palców certyfikatów wykluczone.
- `winsock-catalog`: Struktura natywnego katalogu protokołów Winsock tylko do odczytu; wynik JSON.
- `winsock-namespaces`: Struktura natywnych dostawców przestrzeni nazw Winsock tylko do odczytu; wynik JSON.
- `winsock-provider-files`: Obecność i podpisy plików dostawców protokołów tylko do odczytu; wynik JSON.
- `winsock-readiness`: Gotowość natywnych protokołów, dostawców przestrzeni nazw i plików dostawców Winsock tylko do odczytu; wyniki JSON.

## Aplikacje

- `app-folder-review FOLDER_NAME [--report]`: Przegląd dokładnych folderów w stałym katalogu głównym; bez usuwania.
- `app-package-review FULL_NAME [--all-users]`: Dokładny pakiet docelowy i pakiety zależne; bez usuwania.
- `app-packages [--all-users]`: Tylko inwentarz zarejestrowanych pakietów Appx/MSIX; bez usuwania.
- `browser-extensions`: Przegląd tożsamości rozszerzeń Chrome, Edge, Brave i Firefox; bez usuwania.
- `debloat-review --area AREA [--area AREA ...] [--all-profiles] [--json]`: Tylko przegląd; bez zmian w systemie.
- `depot-review [application and configuration selections] [--json]`: Tylko przegląd; bez aprowizacji.
- `depot-verify-installer onedrive|teams|chrome|acrobat|zoom|7zip FULL_PATH`: Kontrola Authenticode i podpisu producenta tylko do odczytu; nic nie jest uruchamiane.
- `foxit-inventory`: Inwentarz produktów na komputerze tylko do odczytu; wyjście JSON.
- `installed-apps [--current-user]`: Rejestracje odinstalowywania i flagi możliwości; bez usuwania.
- `installed-apps-compare BEFORE.json AFTER.json`: Różnice rejestracji względem zapisanego inwentarza; bez zmian na komputerze.
- `installed-hotfixes`: Ograniczony inwentarz zainstalowanych poprawek systemu Windows tylko do odczytu; dowody JSON.
- `installer-activity`: Dowody procesów i znaczników transakcji Instalatora tylko do odczytu; dowody JSON.
- `installer-check`: Zasady Instalatora i gotowość aparatu tylko do odczytu; dowody JSON.
- `installer-engine-files`: Podsumowanie obecności wymaganych plików Instalatora i Authenticode tylko do odczytu; dowody JSON.
- `installer-events`: Historia MSI z ostatnich siedmiu dni tylko do odczytu; dowody JSON.
- `installer-logging`: Zasady rejestrowania i debugowania Instalatora tylko do odczytu; dowody JSON.
- `installer-pending-state`: Zestawienie oczekujących operacji na plikach z aktywnością Instalatora tylko do odczytu; dowody JSON.
- `installer-registration-repair [--execute]`: Domyślnie podgląd; tylko dokładnie wskazana usterka rejestracji usługi; nigdy nie wyrejestrowuje aparatu.
- `installer-registration-repair-recover`: Ponownie sprawdza przerwaną rejestrację bez powtarzania poleceń.
- `installer-security-policy`: Zasady zabezpieczeń i poprawek Instalatora tylko do odczytu; dowody JSON.
- `installer-service-events`: Awarie Menedżera sterowania usługami powiązane z tożsamością lokalnego Instalatora Windows, tylko do odczytu; dowody JSON.
- `installer-service-registration`: Podsumowanie rejestracji usługi Instalator Windows tylko do odczytu; dowody JSON.
- `mace-package-plan (--onedrive|--outlook|--office|--project|--teams|--all-products) --package PACKAGE_FULL_NAME [--json]`: Świeży podgląd dokładnej kwalifikacji; bez dostawcy usuwania.
- `mace-package-recover`: Na świeżo uzgadnia jeden przerwany rekord usuwania pakietu aplikacji; nigdy nie powtarza usuwania.
- `mace-review (--onedrive|--outlook|--office|--project|--teams|--all-products) [--all-users] [--remove-data] [--remove-credentials] [--review-folder-restore] [--json]`: Tylko przegląd; bez usuwania.
- `msi-cache`: Widoczność zarejestrowanej pamięci podręcznej Instalatora Windows tylko do odczytu; dowody JSON.
- `msi-code NONNEGATIVE_DECIMAL`: Udokumentowany opis kodów powrotu Instalatora Windows; bez sprawdzania komputera i bez naprawy.
- `msi-product {PRODUCT-CODE}`: Dokładny stan rejestracji MSI tylko do odczytu; dowody JSON.
- `msi-repair {PRODUCT-CODE} [--execute]`: Domyślnie podgląd; tylko dokładnie wskazany uszkodzony lub źle skonfigurowany produkt.
- `msi-repair-recover`: Ponownie sprawdza przerwaną naprawę dokładnie wskazanego produktu bez jej powtarzania.
- `msi-sources`: Ograniczone ze względu na prywatność metadane zarejestrowanych źródeł Instalatora Windows tylko do odczytu; dowody JSON.
- `orca-cache-reset --mode New|Classic|Both [--execute]`: Domyślnie podgląd; zachowuje dokładne pamięci podręczne konta, które uruchomiło polecenie, pod nazwami odzyskiwania.
- `orca-cache-reset-recover`: Weryfikuje przerwane zachowywanie pamięci podręcznej bez powtarzania przeniesienia.
- `orca-review --mode New|Classic|Both [--all-users] (--clear-caches|--reset-profile-settings|--reset-authentication|--delete-ost|--remove-add-ins|--reinstall-new-outlook) [--json]`: Tylko przegląd; bez zmian w programie Outlook.
- `startup-apps`: Przegląd rejestracji autostartu oraz zadań przy rozruchu lub logowaniu; bez wyłączania.
- `third-party-update ID SOURCE VERSION [--accept-agreements] [--execute]`: Domyślnie podgląd; tylko jedna dokładna wersja.
- `third-party-update-recover`: Weryfikuje oczekującą dokładną wersję bez ponawiania aktualizacji.
- `third-party-updates`: Wykrywanie aktualizacji aplikacji przez Microsoft.WinGet.Client tylko do odczytu; bez zmian aplikacji.

## System

- `bluetooth-company PACK_PATH DECIMAL_OR_0xHEX_ID`: Dokładne lokalne wyszukiwanie przydziałów Bluetooth SIG; bez żądania sieciowego.
- `crash-config`: Zapisany tryb i ścieżki zrzutów systemu Windows; bez zmian.
- `crash-dumps`: Metadane domyślnych plików zrzutu systemu Windows; bez analizy zawartości i bez zmian.
- `crash-events`: Ostatnie raporty błędów systemu; bez diagnozy i bez zmian.
- `crash-review [--knowledge PACK_PATH ...]`: Do 32 pakietów; łączny przegląd awarii, zamknięć, ograniczonej struktury minizrzutów i opcjonalnego dokładnego opisu kodu błędu krytycznego; bez zmian.
- `default-printer`: Domyślny wybór i ustawienie automatycznego zarządzania tylko do odczytu; dowody JSON.
- `default-printer-set QUEUE [--execute] [--folder PATH]`: Domyślnie podgląd; tylko brakujące lub sprzeczne ustawienie domyślne.
- `default-printer-set-recover [--folder PATH]`: Ponownie sprawdza przerwany wybór bez jego powtarzania.
- `device-check <CATEGORY> [--knowledge PACK_PATH ...]`: Dowody urządzeń tylko do odczytu z opcjonalnymi dokładnymi odwołaniami do sterowników z maksymalnie 32 pakietów.
- `device-code DECIMAL_OR_0xHEX`: Opis kodów problemów Menedżera urządzeń offline.
- `driver-reference PACK_PATH HARDWARE_ID x86|x64|arm64 client|server BUILD`: Tylko kandydaci referencyjni.
- `event-channel-enable CHANNEL [--execute]`: Domyślnie podgląd; włącza jeden dokładnie wskazany, niezarządzany kanał diagnostyczny i zachowuje pozostałe ustawienia.
- `event-channel-enable-recover`: Uzgadnianie tylko do odczytu bez powtarzania polecenia włączenia.
- `event-channels`: Stan, przechowywanie, rozmiar i nasycenie czterech kanałów diagnostycznych, tylko do odczytu.
- `font-registrations`: Ograniczona kontrola rejestracji i obecności plików dla komputera i bieżącego użytkownika, tylko do odczytu.
- `memory-diagnostic-results`: Ograniczona historia Diagnostyki pamięci systemu Windows tylko do odczytu; dowody JSON.
- `minidump-analyze PATH`: Ograniczone metadane strukturalne minizrzutów; bez stron pamięci, symboli i zmian.
- `name-rename PROPOSED_NAME [--execute]`: Domyślnie podgląd; tylko autonomiczne komputery w grupie roboczej; nigdy nie uruchamia ponownie systemu Windows.
- `name-rename-recover`: Weryfikuje oczekującą zmianę nazwy bez jej powtarzania.
- `name-review PROPOSED_NAME`: Przegląd tożsamości i proponowanej nazwy tylko do odczytu.
- `profiles --recover`: Weryfikuje zapisany brak i zachowuje dowody odzyskiwania; nigdy nie ponawia usuwania.
- `profiles --recover-backup`: Weryfikuje i archiwizuje oczekujący artefakt kopii zapasowej; nigdy nie ponawia kopiowania.
- `profiles --select SID --backup-to NEW_DESTINATION [--execute]`: Najpierw podgląd; chronione natywne kopiowanie wymaga jawnego wykonania i nieistniejącego lokalnego miejsca docelowego NTFS.
- `profiles --select SID --create-backup-artifact DESTINATION`: Porównuje istniejącą skopiowaną kopię zapasową i tworzy jej stały artefakt; nie kopiuje ani nie usuwa profilu.
- `profiles [--days 90] [--dependencies] [--select SID ...] [--save-plan] [--verify-backup RECEIPT]`: Przegląd tylko do odczytu; weryfikacja wymaga jednego wybranego identyfikatora SID.
- `task-check`: Inwentarz zadań tylko do odczytu, sklasyfikowane ustalenia i dowody JSON.
- `task-definition TASK_FOLDER TASK_NAME`: Dokładny kod XML zadania tylko do odczytu; niedostępny nie oznacza nieobecnego.
- `task-inventory`: Tożsamości i stany zadań tylko do odczytu; wyjście JSON.
- `task-journal RECORD.json [--completion COMPLETION.json]`: Przegląd historii odzyskiwania; nigdy nie uprawnia do ponowienia.
- `task-plan PLAN.json [--check-conflicts] [--inspect-executable] [--resolve-account] [--inspect-signature]`: Sprawdza i przegląda szkic zadania; bez rejestracji.
- `task-register PLAN.json YYYY-MM-DD [--execute] [--report OUTPUT.html|OUTPUT.txt]`: Domyślnie podgląd; wyłączona definicja Local System, tylko do utworzenia.
- `task-register-recover PLAN.json [--report OUTPUT.html|OUTPUT.txt]`: Uzgadnianie dokładnej definicji tylko do odczytu; nigdy nie ponawia ani nie uruchamia zadania.
- `task-xml PLAN.json YYYY-MM-DD`: Wyłączony kod XML do przeglądu na stdout; tylko harmonogramy kalendarzowe w strefie lokalnej; bez rejestracji.
- `time-config`: Dowody trybu, serwerów i zasad Czasu systemu Windows tylko do odczytu.
- `time-configure --source DomainHierarchy|CustomPeers [--peer HOST ...] [--execute]`: Domyślnie podgląd; tryb niestandardowy wymaga serwerów.
- `time-plan [--source Existing|DomainHierarchy|CustomPeers] [--peer HOST ...]`: Tylko podgląd.
- `time-recover --folder PATH`: Weryfikuje i archiwizuje oczekujący rekord; nigdy nie ponawia zmian.
- `time-verify --source DomainHierarchy|CustomPeers [--peer HOST ...]`: Tylko do odczytu; tryb niestandardowy wymaga serwerów.
- `wmi-provider`: Zapytanie do lokalnego dostawcy systemu operacyjnego tylko do odczytu; dowody JSON.

## Wiedza, walidacja i Engine

- `knowledge PACK_PATH CONTEXT CODE`: Lokalne wyszukiwanie referencyjne; bez wykonywania napraw.
- `knowledge-activate PAYLOAD_JSON SIGNATURE_FILE PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION STORAGE_DIRECTORY`: Zweryfikowana lokalna aktywacja; bez pobierania.
- `knowledge-active STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Weryfikuje aktywny pakiet lokalny; bez zmian.
- `knowledge-compare BEFORE_JSON AFTER_JSON`: Tylko zmiany odwołań; bez instalacji pakietu.
- `knowledge-info PACK_PATH`: Metadane pakietu i liczby wpisów dla każdego kontekstu; bez zmian.
- `knowledge-log PACK_PATH CONTEXT LOG_PATH`: Dołącza odwołania kontekstowe do zaobserwowanych kodów.
- `knowledge-recover STORAGE_DIRECTORY PUBLIC_KEY_FILE PACK_ID MINIMUM_REVISION`: Wybiera najnowszy zweryfikowany zachowany pakiet; bez pobierania.
- `knowledge-search PACK_PATH CONTEXT TEXT`: Do 50 dopasowań tekstu referencyjnego; bez wykonywania napraw.
- `knowledge-set CONTEXT CODE PACK_PATH [PACK_PATH ...]`: Rozpoznaje jedną dokładną tożsamość w maksymalnie 32 pakietach; konflikty nigdy nie są wybierane.
- `knowledge-status CONFIG_JSON STORAGE_ROOT`: Weryfikuje każdą skonfigurowaną zainstalowaną bazę danych; bez pobierania i bez aktywacji.
- `knowledge-update CONFIG_JSON STORAGE_ROOT`: Pobiera i atomowo aktywuje podpisane kandydujące bazy danych ze skonfigurowanych punktów końcowych HTTPS.
- `open <file.kbench> [--json]`: Ponownie otwiera zapisane przechwycenie i ocenia je jeszcze raz zainstalowanymi regułami.
- `scan --output <new-file.kbench> [--json]`: Przechwycenie Windows Update tylko do odczytu zapisane do nowego pliku.
- `validation-coverage RECORD.json [...]`: Podsumowuje natywne dowody z maszyn wirtualnych dla bieżącej wersji; bez diagnostyki i bez napraw.
- `validation-plan [--recipe RECIPE_ID | --workflow WORKFLOW]`: Plan scenariuszy dla jednorazowych maszyn wirtualnych tylko do odczytu; bez diagnostyki i bez napraw.
- `validation-record RECORD.json`: Sprawdza jeden ograniczony natywny rekord dowodów z maszyny wirtualnej; bez diagnostyki i bez napraw.
- `validation-report OUTPUT.html|OUTPUT.txt RECORD.json [...] [--theme THEME] [--accent ACCENT] [--overwrite]`: Lokalny raport pokrycia maszyn wirtualnych dla bieżącej wersji.
- `workflow WORKFLOW --set KEY=VALUE [--set KEY=VALUE ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Uruchamia sprawdzony wspólny plan opcji tylko do odczytu.
- `workflow app-packages [--all-users] [--include-provisioned] [--json]`: Tylko inwentarz.
- `workflow common-problems --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Dziennik ukierunkowanej diagnostyki; bez napraw.
- `workflow computer-name --name PROPOSED_NAME [--json]`: Tylko przegląd z zapisem w dzienniku.
- `workflow installer [--product-code {PRODUCT-CODE}] [--auto-repair] [--json]`: Opcjonalny dokładny stan produktu dołączony do dziennika.
- `workflow network [--host HOST] [--tcp HOST PORT] [--auto-repair] [--json]`: Opcjonalna diagnoza wybranego celu dołączona do dziennika.
- `workflow storage [--volume D:] [--json]`: Kontrola systemu plików tylko do odczytu z zapisem w dzienniku.
- `workflow-journal FILE.json`: Zawartość historyczna; kod zakończenia 0 oznacza pomyślny odczyt, a nie prawidłowy stan.
- `workflow-options [WORKFLOW [--set KEY=VALUE ...]]`: Wyświetla lub sprawdza kontrolki niezależne od hosta; bez skanowania i bez naprawy.
- `workflow-report JOURNAL.json OUTPUT.html|OUTPUT.txt [--include-repairs] [--theme THEME] [--accent ACCENT] [--overwrite]`: Tylko raport historyczny; żadne polecenia nie są uruchamiane.

## Inne polecenia

- `<analyze|investigate> <text-log-path> [--json]`: Analizuje lokalne dowody tekstowe.
- `adapter-bindings`: Podsumowanie powiązań podstawowych protokołów tylko do odczytu; dowody JSON.
- `adapter-statistics`: Liczniki bajtów, błędów i odrzuceń kart sieciowych tylko do odczytu; dowody JSON.
- `application-events`: Ostatnie błędy z dziennika Aplikacja, zgrupowane liczby i dowody JSON, tylko do odczytu.
- `applocker-policy`: Obowiązujące tryby zbierania i zbiorcze liczby reguł tylko do odczytu; szczegóły reguł wykluczone; bez zmian.
- `common-check [--scope NAME ...]`: Domyślnie wszystkie obszary diagnostyczne; tylko wybrane obszary, gdy zostaną wskazane; JSON tylko do odczytu.
- `delete-notifications`: Konfiguracja powiadomień o usuwaniu NTFS i ReFS tylko do odczytu; JSON; bez optymalizacji i bez zmiany ustawień.
- `diagnostic-scopes [--all]`: Wyświetla 11 zalecanych grup wyników albo wszystkie 49 obszarów tylko do odczytu; bez zbierania.
- `full-scan --scope NAME [--scope NAME ...] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Dziennik ukierunkowanej diagnostyki; bez napraw.
- `full-scan [--recipes ID[,ID]] [--auto-repair] [--report OUTPUT.html|OUTPUT.txt] [--json]`: Wszystkie obszary diagnostyczne; bez naprawy, chyba że jawnie podano `--auto-repair`.
- `gateway-check`: Ograniczone kontrole ICMP dokładnie skonfigurowanych domyślnych następnych przeskoków; bez skanowania adresów.
- `infrastructure-service-events`: Odczytuje jedną ograniczoną, dokładną migawkę awarii usług łączności, zdalnego zarządzania, powiadomień i usług podstawowych.
- `investigate-batch FILE [FILE ...]`: Do 12 plików; JSON Lines; kod zakończenia 3 dla nieudanych lub częściowych importów.
- `licensing-check`: Stan aktywacji systemu Windows tylko do odczytu, bez kluczy produktu; dowody JSON.
- `mac-vendor PACK_PATH MAC_ADDRESS`: Lokalne wyszukiwanie w pakiecie według najdłuższego prefiksu; bez żądania sieciowego.
- `ncsi-policy`: Zasady klasyfikacji łączności systemu Windows tylko do odczytu; miejsca docelowe sond wykluczone.
- `neighbor-cache`: Ograniczone ze względu na prywatność dowody osiągalności sąsiadów tylko do odczytu; wynik JSON.
- `ntfs-self-healing DRIVE:`: Konfiguracja naprawy NTFS online dla wybranego woluminu tylko do odczytu; bez naprawy i bez zmiany ustawień.
- `pagefile-check`: Podsumowanie zarządzania, przydziału i użycia pliku stronicowania tylko do odczytu; pełne ścieżki wykluczone.
- `pci-vendor PACK_PATH PCI_HARDWARE_ID`: Dokładne lokalne wyszukiwanie w pakiecie; bez żądania sieciowego i bez wyboru sterownika.
- `physical-disks`: Bieżący stan kondycji, działania, nośnika, magistrali i użycia tylko do odczytu; JSON; bez naprawy.
- `platform-service-events`: Odczytuje jedną ograniczoną, dokładną migawkę awarii usług aplikacji, tożsamości, profili i obsługi urządzeń.
- `previous-windows [--execute --backup-confirmed]`: Domyślnie podgląd; trwale usuwa możliwość wycofania systemu Windows za pomocą DISM.
- `previous-windows-recover`: Uzgadnianie tylko do odczytu bez powtarzania usunięcia możliwości wycofania.
- `provisioned-packages`: Pakiety przygotowane dla nowych użytkowników w lokalnym systemie Windows; bez usuwania.
- `recipes [WORKFLOW]`: Wyświetla zainstalowane definicje lub zależności i limity właściwe dla procesu; bez wykonywania.
- `repair <dism|sfc> [--execute]`: Domyślnie podgląd.
- `reserved-storage`: Stan magazynu zarezerwowanego działającego systemu Windows tylko do odczytu; dowody JSON.
- `restore-point DESCRIPTION [ModifySettings|ApplicationInstall|DeviceDriverInstall] [--execute]`: Domyślnie podgląd; tworzy jeden punkt po świeżych kontrolach.
- `restore-point-recover`: Weryfikuje oczekujące żądanie bez tworzenia kolejnego punktu.
- `schedule-preview daily|weekly|monthly YYYY-MM-DD HH:mm TIME_ZONE_ID [Mon,Tue|1,15|last]`: Tylko podgląd.
- `shadow-copies`: Bieżące stany i atrybuty migawek VSS tylko do odczytu; tożsamości wykluczone; bez zmian migawek.
- `shadow-storage`: Przydział i obciążenie obszaru różnicowego VSS tylko do odczytu; JSON; bez zmiany rozmiaru i bez akcji na migawkach.
- `uac-policy`: Włączenie UAC i zasady monitu o podniesienie uprawnień tylko do odczytu; bez danych kont i poświadczeń.
- `uefi-vendor PACK_PATH PNP|ACPI VENDOR_ID`: Dokładne lokalne wyszukiwanie w rejestrze UEFI; bez żądania sieciowego i bez wyboru sterownika.
- `usb-vendor PACK_PATH USB_HARDWARE_ID`: Dokładne lokalne wyszukiwanie w pakiecie; bez żądania sieciowego i bez wyboru sterownika.
- `vitals-check`: Migawki systemu Windows, sprzętu, konta, zasobów, baterii i dysków fizycznych tylko do odczytu; dowody JSON.
- `win32-code DECIMAL_OR_0xHEX`: Wyszukiwanie komunikatów zainstalowanego systemu Windows offline.
- `workflows`: Wyświetla zainstalowane klucze i tytuły procesów; bez skanowania i bez naprawy.
