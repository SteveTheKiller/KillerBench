# Urządzenia

Urządzenia obejmuje drukarki, dźwięk, kamery, Bluetooth, urządzenia wejściowe i usługi systemu Windows, od których zależy podłączony sprzęt.

Przewodniki krok po kroku: [Rozwiąż problemy z drukowaniem](fix-printing.html) i [Zbadaj awarie i niebieskie ekrany](investigate-crashes.html).

## Narzędzia w tej sekcji

Drukowanie, dźwięk, kamery, Bluetooth, urządzenia wejściowe i usługi obsługujące podłączony sprzęt.

### Drukowanie

Sprawdza Bufor wydruku, wstrzymania kolejek lokalnych, widoczne zadania drukowania, ustawienia offline i stany zgłaszane przez sterowniki, a następnie inwentaryzuje rejestracje kolejek, sterowników, portów, standardowych punktów końcowych TCP/IP, procesorów wydruku i monitorów wydruku. Zestawia ostatnie problemy PrintService tylko wtedy, gdy obecna jest jedna dokładna tożsamość bieżącej kolejki. Przypisane standardowe punkty końcowe TCP/IP otrzymują jedną ograniczoną próbę połączenia bez danych aplikacji, SNMP ani zadania drukowania. Automatyczna naprawa może uruchomić włączony bufor wydruku, wznowić tylko dokładnie wskazane wstrzymane kolejki ze świeżymi dowodami oczekujących zadań i wyłączyć tryb offline. Celowe wstrzymania bez zadań pozostają tylko do przeglądu. Połączenia zdalne, ciągi społeczności SNMP, rejestracje rozszerzeń i zawartość zadań są zachowywane. Kod stanu ani udane połączenie nie określają tożsamości drukarki, materiałów eksploatacyjnych, mechaniki, przyjęcia przez kolejkę, powodzenia renderowania ani fizycznego wydruku.

### Dźwięk

Sprawdza tożsamości i współdzielone formaty domyślnego wejścia i wyjścia multimediów, stan urządzeń i sterowników audio, Konstruktor punktów końcowych audio systemu Windows i Audio systemu Windows. Automatyczna naprawa uruchamia zatrzymane, włączone usługi i weryfikuje ich działanie. Ustawienia uruchamiania, sterowniki, wybrane urządzenia, tryb wyłączności i poziomy głośności są zachowywane. Odtwarzanie i nagrywanie dźwięku oraz routing aplikacji pozostają nieprzetestowane.

### Obsługa kamer

Sprawdza błędy urządzeń kamer i obrazowania, identyfikatory sprzętu, dowody zainstalowanych sterowników i dostępność Serwera ramek kamery systemu Windows. Opcjonalne odzyskiwanie uruchamia tylko włączoną i zatrzymaną usługę po kontrolach zależności. Aplikacja, która już żąda dostępu do kamery, może kontynuować z istniejącymi uprawnieniami. Ustawienia prywatności kamery, uprawnienia, konfiguracja urządzeń, zasady, sterowniki i typ uruchomienia są zachowywane. Bez resetowania, zmiany uprawnień ani instalacji sterowników. Weryfikacja obejmuje tylko zgłoszoną konfigurację urządzenia i stan działania usługi, a nie przechwytywanie obrazu, dostęp prywatności ani fizyczne działanie.

### Obsługa Bluetooth

Sprawdza błędy urządzeń Bluetooth, identyfikatory sprzętu, dowody zainstalowanych sterowników i dostępność Usługi obsługi Bluetooth. Opcjonalne odzyskiwanie uruchamia tylko włączoną i zatrzymaną usługę po kontrolach zależności; sparowane urządzenia mogą połączyć się ponownie. Parowania, ustawienia radiowe, konfiguracja urządzeń, zasady i typ uruchomienia są zachowywane. Bez resetowania, usuwania urządzeń ani instalacji sterowników. Weryfikacja obejmuje tylko zgłoszoną konfigurację urządzenia i stan działania usługi, a nie wykrywanie, parowanie, dźwięk ani przesyłanie danych.

### Obsługa kart inteligentnych

Sprawdza błędy czytników kart inteligentnych, identyfikatory sprzętu, dowody zainstalowanych sterowników i dostępność usługi Karta inteligentna. Opcjonalne odzyskiwanie uruchamia tylko włączoną i zatrzymaną usługę po kontrolach zależności. Aplikacje mogą wznowić wykrywanie włożonych kart zgodnie z istniejącymi zasadami. Certyfikaty, kody PIN, poświadczenia, konfiguracja czytników i kart, zasady, sterowniki i typ uruchomienia są zachowywane. Bez inicjowania, rejestracji, operacji na kodzie PIN, resetowania czytnika ani instalacji sterowników. Weryfikacja obejmuje tylko zgłoszoną konfigurację czytnika i stan działania usługi, a nie wykrywanie kart, uwierzytelnianie ani dostęp aplikacji.

### Obsługa biometrii

Sprawdza błędy urządzeń biometrycznych, identyfikatory sprzętu, dowody zainstalowanych sterowników i dostępność Usługi biometrycznej systemu Windows. Opcjonalne odzyskiwanie uruchamia tylko włączoną i zatrzymaną usługę po kontrolach zależności. Istniejące żądania Windows Hello i aplikacji mogą zostać wznowione zgodnie z bieżącymi zasadami. Szablony biometryczne, dane Windows Hello, poświadczenia, konfiguracja urządzeń, uprawnienia, zasady, sterowniki i typ uruchomienia są zachowywane. Bez rejestracji, usuwania szablonów, resetowania czujników, zmiany uprawnień ani instalacji sterowników. Weryfikacja obejmuje tylko zgłoszoną konfigurację czujnika i stan działania usługi, a nie przechwytywanie, dopasowywanie, logowanie ani dostęp aplikacji.

### Usługa urządzeń interfejsu HID

Sprawdza dostępność usługi Usługa urządzeń interfejsu HID. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące przyciski skrótów, sterowanie multimediami i inne funkcje HID mogą zostać wznowione. Urządzenia, sterowniki, mapowania przycisków, ustawienia ułatwień dostępu, ustawienia wprowadzania, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na urządzeniach, sterownikach, mapowaniach ani ustawieniach wprowadzania. Weryfikacja obejmuje tylko stabilny stan usługi, a nie działanie przycisków, elementów sterujących ani aplikacji.

### Usługa modułu wyliczającego urządzenia przenośne

Sprawdza dostępność usługi Usługa modułu wyliczającego urządzenia przenośne. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę usługę, gdy jest zatrzymana i ustawiona na uruchamianie automatyczne, po świeżych kontrolach zależności. Zatrzymana usługa uruchamiana ręcznie ma charakter wyłącznie informacyjny. Istniejące aplikacje mogą wznowić wykrywanie podłączonych urządzeń przenośnych i dostęp do nich. Urządzenia, sterowniki, pliki, biblioteki, ustawienia synchronizacji, uprawnienia, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na urządzeniach, plikach, bibliotekach, synchronizacji ani przesyłaniu. Weryfikacja obejmuje tylko stabilny stan usługi, a nie wyliczanie, dostęp, synchronizację ani przesyłanie.

### Pozyskiwanie obrazów w systemie Windows (WIA)

Sprawdza dostępność usługi Pozyskiwanie obrazów w systemie Windows. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące żądania skanerów i pozyskiwania obrazów nieruchomych mogą zostać wznowione z bieżącymi uprawnieniami aplikacji. Urządzenia, sterowniki, dane obrazów, uprawnienia aplikacji, ustawienia prywatności, ustawienia przesyłania, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na urządzeniach, sterownikach, obrazach, uprawnieniach, przesyłaniu ani pozyskiwaniu. Weryfikacja obejmuje tylko stabilny stan usługi, a nie działanie skanera, kamery, przesyłania ani aplikacji.

### Wykrywanie sprzętu powłoki

Sprawdza ograniczone dowody Plug and Play dotyczące kontrolerów magazynu, dysków i urządzeń przenośnych oraz odwołania do zainstalowanych sterowników, a następnie sprawdza dostępność usługi Wykrywanie sprzętu powłoki. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące powiadomienia Autoodtwarzania i o podłączeniu sprzętu mogą zostać wznowione zgodnie z bieżącymi zasadami. Obserwacje urządzeń i zachowana historia nie mogą uprawniać do odzyskiwania. Urządzenia, sterowniki, woluminy, punkty instalacji, ustawienia Autoodtwarzania, programy obsługi, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. Nie jest żądana żadna operacja na urządzeniach ani ustawieniach. Weryfikacja obejmuje tylko stabilny stan usługi, a nie dostarczanie powiadomień, gotowość nośników, dostęp do woluminów ani fizyczne działanie urządzeń.

### Usługa instalacji urządzeń

Sprawdza dostępność usługi Usługa instalacji urządzeń. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejąca instalacja urządzeń zlecona przez system Windows może zostać wznowiona zgodnie z bieżącymi zasadami. Urządzenia, sterowniki, pakiety, konfiguracja, powiązania, ustawienia klas, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench niczego nie wybiera ani nie instaluje. Weryfikacja obejmuje tylko stabilny stan usługi, a nie instalację ani działanie urządzeń.

### Usługa kojarzenia urządzeń

Sprawdza ostatnią historię konfiguracji urządzeń i Plug and Play, a następnie dostępność usługi Usługa kojarzenia urządzeń. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące kojarzenie urządzeń zlecone przez system Windows może zostać wznowione. Sparowane i skojarzone urządzenia, sterowniki, poświadczenia, metadane, uprawnienia, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na urządzeniach, parowaniu, usuwaniu, resetowaniu ani sterownikach. Weryfikacja obejmuje tylko stabilny stan usługi, a nie wykrywanie, parowanie, kojarzenie ani działanie urządzeń.

### Usługa ulepszania ekranu

Sprawdza dowody dotyczące kart graficznych i monitorów, a następnie dostępność usługi Usługa ulepszania ekranu. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące żądania jasności, kolorów i ulepszeń mogą zostać wznowione. Konfiguracja ekranu, jasność, profile kolorów, ustawienia HDR, urządzenia, sterowniki, ustawienia zasilania, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. Żadne ustawienie ekranu nie jest zmieniane. Weryfikacja obejmuje tylko stabilny stan usługi, a nie działanie ulepszeń, jakość obrazu ani fizyczne działanie ekranu.

### Usługa zarządzania wprowadzaniem tekstu

Sprawdza dowody dotyczące klawiatur, myszy i urządzeń HID, a następnie dostępność usługi Usługa zarządzania wprowadzaniem tekstu. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące żądania klawiatury dotykowej, pisma odręcznego, emoji i zaawansowanego wprowadzania tekstu mogą zostać wznowione. Słowniki, dane pisma odręcznego, ustawienia języka i klawiatury, ustawienia ułatwień dostępu i prywatności, urządzenia, sterowniki, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. Nie jest zbierany żaden wprowadzany tekst, naciśnięcia klawiszy ani zawartość pisma odręcznego. Weryfikacja obejmuje tylko stabilny stan usługi, a nie wprowadzanie tekstu, rozpoznawanie ani działanie urządzeń.

### Usługa buforowania czcionek systemu Windows

Sprawdza dostępność usługi Usługa buforowania czcionek systemu Windows. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące aplikacje mogą znów korzystać z bieżącej pamięci podręcznej czcionek. Zainstalowane czcionki, pliki czcionek, dane aplikacji, pliki pamięci podręcznej, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie instaluje ani nie usuwa czcionek i nie usuwa ani nie odbudowuje pamięci podręcznej. Weryfikacja obejmuje tylko stabilny stan usługi, a nie renderowanie tekstu ani działanie aplikacji.

### Host dostawcy odnajdywania funkcji

Sprawdza dostępność usługi Host dostawcy odnajdywania funkcji. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące aplikacje mogą wznowić wykrywanie urządzeń sieciowych za pośrednictwem zainstalowanych dostawców. Profile sieciowe, udziały, urządzenia, dostawcy, reguły zapory, ustawienia odnajdywania, poświadczenia, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na urządzeniach, udziałach, dostawcach, regułach zapory ani ustawieniach odnajdywania. Weryfikacja obejmuje tylko stabilny stan usługi, a nie stan dostawców, wykrywanie urządzeń ani łączność.

### Publikacja zasobów odnajdywania funkcji

Sprawdza dostępność usługi Publikacja zasobów odnajdywania funkcji. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Ten komputer może stać się wykrywalny dzięki istniejącym ustawieniom odnajdywania sieci i zapory. Profile sieciowe, udziały, opublikowane zasoby, reguły zapory, ustawienia odnajdywania, poświadczenia, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na udziałach, zasobach, regułach zapory ani ustawieniach odnajdywania. Weryfikacja obejmuje tylko stabilny stan usługi, a nie publikację, widoczność, dostęp ani łączność.
