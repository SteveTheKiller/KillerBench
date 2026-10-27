# Sieć

Sieć sprawdza połączenia, DNS, trasy, logowanie bezprzewodowe i przewodowe, udostępnianie plików i dostęp zdalny. Narzędzia łączące się z innym hostem używają ustawionego przez Ciebie celu i limitu czasu, a nic nie jest zmieniane, dopóki nie potwierdzisz naprawy.

Przewodniki krok po kroku: [Rozwiąż problemy z siecią i internetem](fix-network.html).

## Narzędzia w tej sekcji

Sprawdzaj połączenia, DNS, trasy, sieć bezprzewodową, udostępnianie i dostęp zdalny.

### Sieć

Sprawdza karty sieciowe, stan i pochodzenie adresów, bramy, konfigurację DNS, obowiązujące zasady przestrzeni nazw DNS, ustawienia TCP, strukturę natywnego katalogu protokołów i dostawców przestrzeni nazw Winsock, tablicę routingu IP oraz mapowania pliku Hosts. Automatyczna naprawa może uruchomić włączone usługi Klient DNS, Klient DHCP, klient SMB, Rozpoznawanie lokalizacji w sieci, Usługa Lista sieci i Pomocnik IP oraz odnowić podłączone fizyczne karty DHCP bez użytecznego adresu IPv4. Uruchomienie usług identyfikacji sieci może ponownie obliczyć tożsamość sieci i zastosować inny istniejący profil zapory. Pomocnik IP może aktywować istniejące funkcje przejściowe IPv6, serwera proxy portów i powiadomień sieciowych. Odzyskiwanie usług weryfikuje stan działania, a nie powodzenie rozpoznawania nazw, wyboru trasy, identyfikacji sieci ani działania tuneli. Trasy, wpisy Hosts, serwery DNS, profile, reguły zapory, dostawcy Winsock, konfiguracja przejściowa, typy uruchomienia i zasady są zachowywane. Odnowienie może zmienić dzierżawione adresy i przerwać połączenia. Ustawienia statyczne i karty wirtualne są zachowywane. Dokładne odpowiedzi ICMP bramy nie weryfikują dostępności internetu.

### Sieć bezprzewodowa

Sprawdza urządzenia sieciowe i dostępność Autokonfiguracji sieci WLAN. Opcjonalne odzyskiwanie uruchamia tylko włączoną i zatrzymaną usługę po kontrolach zależności; zapisane profile mogą połączyć się ponownie automatycznie. Profile, poświadczenia, zasady, ustawienia radiowe i typy uruchomienia są zachowywane. Bez resetowania karty ani instalacji sterowników. Weryfikacja obejmuje stan działania, a nie uwierzytelnianie bezprzewodowe ani łączność.

### Uwierzytelnianie przewodowe (802.1X)

Sprawdza urządzenia sieciowe i Autokonfigurację sieci przewodowej dla sieci wymagających 802.1X. Opcjonalne odzyskiwanie uruchamia tylko włączoną i zatrzymaną usługę po kontrolach zależności. Istniejące profile uwierzytelniania mogą się aktywować i zmienić dostęp do sieci. Certyfikaty, poświadczenia, profile, zasady i ustawienia uruchamiania są zachowywane. Weryfikacja obejmuje tylko stan działania, a nie uwierzytelnianie ani łączność.

### Połączenia sieciowe

W ograniczonym zakresie przegląda bieżące przypisania profili sieciowych, dokładną historię awarii usługi i bieżącą gotowość usługi Połączenia sieciowe. Niekompletne dowody mają pierwszeństwo. Profile publiczne i historyczne awarie pozostają kontekstem i nie mogą uprawniać do odzyskiwania ani zmiany sieci. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące interfejsy zarządzania siecią mogą zostać wznowione. Karty sieciowe, adresy, trasy, ustawienia DNS, profile połączeń, poświadczenia, konfiguracja zapory, zasady i typ uruchomienia są zachowywane. Żadne połączenie ani ustawienie sieci nie jest zmieniane. Weryfikacja obejmuje tylko stabilny stan usługi, a nie działanie kart, przydzielanie adresów, routing, rozpoznawanie nazw ani łączność.

### Automatyczny serwer proxy WinHTTP

Przegląda zapisane ustawienia domyślne WinHTTP, ustawienia internetowe bieżącego konta, dokładną historię awarii usługi i bieżącą gotowość usługi Automatyczny serwer proxy WinHTTP. Niekompletne dowody mają pierwszeństwo. Konfiguracja serwera proxy i historyczne awarie nie mogą uprawniać do odzyskiwania usługi ani wykrywania. Zatrzymana usługa uruchamiana ręcznie może być czymś normalnym. Gdy wybrane jest dokładnie to narzędzie, opcjonalne odzyskiwanie może uruchomić tylko zatrzymaną włączoną usługę po świeżych kontrolach zależności. Istniejące aplikacje mogą wznowić wykrywanie WPAD lub PAC z bieżącymi ustawieniami. Definicje serwera proxy, adresy URL plików PAC, poświadczenia, ustawienia sieci, konfiguracja zapory, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. Żaden adres URL nie jest pobierany i żadne ustawienie serwera proxy ani sieci nie jest zmieniane. Weryfikacja obejmuje tylko stabilny stan usługi, a nie wykrywanie, dostępność, uwierzytelnianie, własność zasad ani użycie przez aplikacje.

### Menedżer połączeń systemu Windows

W ograniczonym zakresie przegląda bieżące przypisania profili sieciowych, dokładną historię awarii usługi i bieżącą gotowość usługi Menedżer połączeń systemu Windows. Niekompletne dowody mają pierwszeństwo. Profile publiczne i historyczne awarie pozostają kontekstem i nie mogą uprawniać do odzyskiwania ani decyzji o połączeniu. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. System Windows może wznowić automatyczne decyzje o połączeniach zgodnie z istniejącymi profilami i zasadami. Karty sieciowe, profile, poświadczenia, ustawienia połączeń taryfowych, kosztów, roamingu, zapory, VPN i uwierzytelniania oraz typ uruchomienia są zachowywane. Żaden profil, karta ani ustawienie nie jest zmieniane. Weryfikacja obejmuje tylko stabilny stan usługi, a nie zastosowanie profili, wybór połączenia, uwierzytelnianie, routing, rozpoznawanie nazw ani łączność.

### Usługa konfiguracji sieci

W ograniczonym zakresie przegląda bieżące powiązania protokołów kart sieciowych, dokładną historię awarii usługi i bieżącą gotowość usługi Usługa konfiguracji sieci. Niekompletne dowody mają pierwszeństwo. Stan powiązań i historyczne awarie nie mogą uprawniać do odzyskiwania usługi ani zmiany konfiguracji. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejąca oczekująca konfiguracja sieci może zostać wznowiona zgodnie z zasadami systemu Windows. Karty sieciowe, sterowniki, pakiety sterowników, powiązania, adresy, trasy, ustawienia DNS, profile, poświadczenia, konfiguracja zapory, zasady i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej zmiany sterownika ani konfiguracji. Weryfikacja obejmuje tylko stabilny stan usługi, a nie dostępność kart, przydatność sterowników, zakończenie konfiguracji, przydzielanie adresów, routing, rozpoznawanie nazw ani łączność.

### Udostępnianie plików (SMB)

Sprawdza dostępność serwera SMB systemu Windows. Opcjonalne odzyskiwanie uruchamia tylko włączoną i zatrzymaną usługę Serwer po świeżych kontrolach zależności. Istniejące udziały plików i drukarek mogą znów stać się dostępne zgodnie z ich bieżącymi uprawnieniami i zasadami zapory. Żaden udział, uprawnienie, ustawienie SMB, reguła zapory, poświadczenie, zasada ani typ uruchomienia nie jest zmieniany. Weryfikacja obejmuje tylko stabilny stan usługi, a nie dostęp klientów ani przesyłanie plików.

### Menedżer połączeń dostępu zdalnego

W ograniczonym zakresie przegląda konfigurację profili VPN bieżącego konta, dokładną historię awarii usługi i bieżącą gotowość usługi Menedżer połączeń dostępu zdalnego, bez zbierania sekretów. Pusty inwentarz może być zamierzony, a obecność profili, etykiety połączeń ani historyczne awarie nie mogą uprawniać do odzyskiwania ani próby połączenia. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące żądania połączeń telefonicznych i VPN mogą zostać wznowione. Profile połączeń, poświadczenia, trasy, karty sieciowe, ustawienia uwierzytelniania, zasady i typ uruchomienia są zachowywane. Żadne połączenie nie jest inicjowane. Weryfikacja obejmuje tylko stabilny stan usługi, a nie zastosowanie profili, dostępność serwera, uwierzytelnianie, negocjowanie tunelu, instalację tras, rozpoznawanie nazw ani łączność.

### Usługi pulpitu zdalnego

Przegląda lokalne włączenie Pulpitu zdalnego, uwierzytelnianie na poziomie sieci i port nasłuchiwania, ograniczoną historię połączeń i sesji, dokładną historię awarii usługi i bieżącą gotowość usługi. Niekompletne dowody mają pierwszeństwo. Zapisy historyczne nie mogą uprawniać do odzyskiwania usługi ani zmian konfiguracji. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejący odbiornik może znów stać się dostępny zgodnie z bieżącymi zasadami, NLA, regułami zapory i uprawnieniami. Włączenie Pulpitu zdalnego, odbiorniki, certyfikaty, porty, reguły zapory, NLA, uprawnienia, sesje, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. Żaden odbiornik, reguła zapory, dostęp konta, certyfikat ani sesja nie jest tworzony ani zmieniany. Weryfikacja obejmuje tylko stabilny stan usługi, a nie obowiązujące zasady, powiązanie odbiornika, dostęp przez zaporę, tożsamość certyfikatu, uprawnienia, uwierzytelnianie, dostępność sieci, powodzenie połączenia ani tworzenie sesji.

### Zdalne zarządzanie systemem Windows

W ograniczonym zakresie przegląda konfigurację odbiorników WinRM, historię operacyjną, dokładną historię awarii usługi i bieżącą gotowość usługi. Niekompletne dowody mają pierwszeństwo. Wyłączone odbiorniki lub bieżące problemy z usługą wymagają przeglądu, a zapisy historyczne nie mogą uprawniać do odzyskiwania ani zmian konfiguracji. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące odbiorniki WS-Management mogą znów stać się dostępne zgodnie z bieżącymi ustawieniami uwierzytelniania, certyfikatów, zapory, TrustedHosts i zasad. Odbiorniki, adresy, porty, certyfikaty, uwierzytelnianie, szyfrowanie, TrustedHosts, reguły zapory, uprawnienia, przydziały, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. Żaden odbiornik, reguła zapory, wpis zaufania, certyfikat, poświadczenie ani sesja zdalna nie jest tworzony ani zmieniany. Weryfikacja obejmuje tylko stabilny stan usługi, a nie obowiązujące zasady, powiązanie odbiorników, dostęp przez zaporę, ważność certyfikatów, uwierzytelnianie, uprawnienia, dostępność dla klientów ani zdalne wykonywanie poleceń.
