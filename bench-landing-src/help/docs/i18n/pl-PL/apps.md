# Aplikacje

Aplikacje przegląda zainstalowane programy, pakiety Store, Office i Outlook, rozszerzenia przeglądarek, aplikacje autostartu i wyszukiwanie. Narzędzia do usuwania i resetowania zawsze najpierw pokazują przegląd i zmieniają tylko to, co wybierzesz.

Przewodniki krok po kroku: [Skonfiguruj nowy komputer](set-up-new-pc.html).

## Narzędzia w tej sekcji

Przeglądaj zainstalowane aplikacje, pakiety Store, Office i Outlook, rozszerzenia przeglądarek i wyszukiwanie.

### Zainstalowane aplikacje

Odczytuje rejestracje odinstalowywania aplikacji w wybranym zakresie komputera lub bieżącego konta, a następnie łączy ten zakres z ograniczonymi dowodami z dziennika Aplikacja, Monitora niezawodności i ponownych uruchomień. Inne konta i aplikacje przenośne są wykluczone. Rejestracje i ogólna historia nie są dokładnym dowodem stanu ani własności aplikacji. Nie jest wybierana żadna akcja instalatora, dezinstalatora, aplikacji, procesu, usługi, pliku, rejestracji, konta, zasad ani ponownego uruchomienia.

### Pakiety aplikacji

Inwentaryzuje zarejestrowane pakiety Appx/MSIX dla wybranego zakresu kont. Domyślnie jest to bieżące konto; wszyscy użytkownicy wymagają dostępu administratora. Podaje dokładne tożsamości, flagi platformy i nieusuwalności oraz liczbę zależności. Pakiety aprowizowane, aplikacje Win32, zarządzana własność i dane aplikacji nie są oceniane. Żadne pakiety nie są usuwane.

### Rozszerzenia przeglądarki

W ograniczonym zakresie inwentaryzuje tożsamości rozszerzeń Chrome, Edge, Brave i Firefox bieżącego konta. Historia przeglądarki, poświadczenia, ustawienia rozszerzeń, zawartość manifestów, polecenia i wartości zasad są wykluczone. Obecność nie dowodzi włączenia, zaufania, własności ani bezpieczeństwa usunięcia. Żadna przeglądarka, rozszerzenie ani zasada nie jest zmieniana.

### Aplikacje autostartu

Inwentaryzuje nazwy rejestracji Run i RunOnce komputera i bieżącego konta, nazwy plików folderu Autostart, stany zatwierdzenia uruchamiania systemu Windows oraz nazwy i stany zaplanowanych zadań przy rozruchu lub logowaniu, a następnie zwraca jeden wniosek o środowisku uruchamiania. Wiersze poleceń, ścieżki docelowe, binarne znaczniki czasu zatwierdzenia, akcje zadań, argumenty, konta i poświadczenia nie są zbierane. Rejestracja nie dowodzi, że element się uruchomił, spowodował opóźnienie, jest niepożądany lub można go bezpiecznie wyłączyć. Stan zatwierdzenia nie dowodzi, że istnieje odpowiadająca mu rejestracja. Nie jest wybierana żadna zmiana rejestracji, pliku, aplikacji, zadania, usługi ani zasad.

### Usuwanie aplikacji Microsoft

Przegląda wybrane rejestracje OneDrive, Outlook, Office/M365, Project i Teams z wyborem zakresu kont i przechowywania. Inwentarze komputera, bieżącego konta i zarejestrowanych pakietów są ze sobą zestawiane; przegląd dla wszystkich użytkowników obejmuje też pakiety aprowizowane dla nowych użytkowników. Obsługiwany pakiet bieżącego konta może otrzymać osobny, dokładny plan zabezpieczeń w aplikacji komputerowej lub CLI. Usunięcie tego jednego pakietu jest osobną chronioną akcją w aplikacji komputerowej z własnymi potwierdzeniami. Sam przegląd nie zmienia żadnego procesu, aplikacji, pakietu, pliku, poświadczenia, lokalizacji folderu, wartości rejestru ani danych użytkownika.

### Reset Outlooka

Przegląda nowy program Outlook, klasyczny program Outlook lub oba, z jawnymi wyborami dla pamięci podręcznych, ustawień profilu, uwierzytelniania, plików OST, dodatków i ponownej instalacji nowego programu Outlook. Przegląd bieżącego konta sprawdza ograniczone znane lokalizacje, dokładne rejestracje i to, czy procesy są zamknięte. Osobno potwierdzana akcja dotycząca tylko pamięci podręcznej zachowuje dokładne katalogi pamięci podręcznej pod unikatowymi nazwami odzyskiwania i weryfikuje każde przeniesienie. Przegląd dla wszystkich użytkowników i inne obszary resetu pozostają tylko do przeglądu. Żadna zachowana pamięć podręczna, profil, plik OST, poświadczenie, dodatek, pakiet ani ustawienie nie jest usuwane.

### Inwentarz Foxit

Inwentaryzuje produkty Foxit zainstalowane na komputerze oraz wartości rejestru aktualizatora w obu widokach. Instalacje użytkownika, obowiązujące zasady i własność produktów nie są weryfikowane. Żadne aplikacje ani ustawienia nie są zmieniane.

### Usługa Instalator Windows

Przed odzyskaniem usługi sprawdza zasady ograniczeń, wycofywania, podnoszenia uprawnień, rejestrowania i debugowania Instalatora, znaczniki operacji w toku, pliki binarne aparatu oraz widoczność zarejestrowanych pakietów w pamięci podręcznej. Zestawia ostatnie wyniki nieudane i udane tylko dla dokładnie zgłoszonych tożsamości produktów. Jeśli wymagania wstępne są spełnione, automatyczna naprawa może uruchomić włączoną usługę i zweryfikować stan Uruchomiona. Wyniki historyczne, rejestrowanie, widoczność pamięci podręcznej i ustalenia debugowania nie uprawniają do zmian pakietów ani zasad. Zasady, dzienniki, znaczniki transakcji, typy uruchomienia i pakiety w pamięci podręcznej są zachowywane. Instalacja konkretnego pakietu nie jest testowana.

### Windows Search

Sprawdza dostępność usługi Windows Search, bieżącą aktywność SystemIndex i ostatnią historię zdarzeń Search. Automatyczna naprawa może uruchomić zatrzymaną, włączoną usługę i zweryfikować jej działanie. Osobny, jawny wybór odbudowy indeksu zachowuje poprzedni indeks, uruchamia nowy i weryfikuje dokładny stan usługi i katalogu. Indeksowane lokalizacje, zasady, typ uruchomienia i zachowane dane są zachowywane. Uruchomienie usługi lub odbudowa indeksu może wznowić indeksowanie w tle. Kompletność wyników wyszukiwania pozostaje nieprzetestowana.

### Gotowość aplikacji

Sprawdza dostępność usługi Gotowość aplikacji. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące pierwsze logowanie i aprowizacja aplikacji mogą zostać wznowione zgodnie z bieżącymi zasadami. Pakiety, rejestracje, licencje, profile użytkowników, dane aplikacji, zasady i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na pakietach. Weryfikacja obejmuje tylko stabilny stan usługi, a nie logowanie, aprowizację ani uruchamianie aplikacji.

### Usługa repozytorium stanu

Sprawdza dostępność usługi Usługa repozytorium stanu. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące żądania stanu modelu aplikacji mogą zostać wznowione. Rejestracje, pakiety, licencje, profile, dane przeglądarki i aplikacji, zawartość repozytorium, zasady i typ uruchomienia są zachowywane. Nie jest żądany żaden reset, ponowna rejestracja ani usunięcie. Weryfikacja obejmuje tylko stabilny stan usługi, a nie spójność repozytorium ani działanie aplikacji.

### Usługa wdrażania AppX

Sprawdza dostępność usługi Usługa wdrażania AppX. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące żądania wdrażania, rejestracji lub obsługi mogą zostać wznowione zgodnie z bieżącymi zasadami. Pakiety, rejestracje, licencje, profile, dane aplikacji, zawartość repozytorium, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na pakietach. Weryfikacja obejmuje tylko stabilny stan usługi, a nie wdrażanie, rejestrację ani uruchamianie aplikacji.

### Usługa licencji klienta

Sprawdza dostępność usługi Usługa licencji klienta. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące żądania licencji aplikacji Store mogą zostać wznowione zgodnie z bieżącymi zasadami. Pakiety, rejestracje, licencje, tokeny, konta, profile, dane aplikacji, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na pakietach, licencjach, tokenach ani kontach. Weryfikacja obejmuje tylko stabilny stan usługi, a nie weryfikację licencji ani uruchamianie aplikacji.

### Usługa instalacji Microsoft Store

Sprawdza ostatnią historię instalacji Microsoft Store i dokładną dostępność usługi instalacji Store. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące żądania pobierania i instalacji ze Store mogą zostać wznowione zgodnie z bieżącymi zasadami. Pakiety, rejestracje, licencje, konta, dane aplikacji, stan pobierania, źródła, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na pakietach, pobieraniu, licencjach, kontach, źródłach ani instalacji. Weryfikacja obejmuje tylko stabilny stan usługi, a nie wyniki pobierania, instalacji, licencji ani aplikacji.

### Usługa Menedżer licencji systemu Windows

Sprawdza dostępność usługi Usługa Menedżer licencji systemu Windows. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące kontrole licencji aplikacji mogą zostać wznowione zgodnie z bieżącymi zasadami. Pakiety, rejestracje, licencje, tokeny, konta, dane aplikacji, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na pakietach, licencjach, tokenach, kontach, rejestracjach ani uprawnieniach do korzystania. Weryfikacja obejmuje tylko stabilny stan usługi, a nie weryfikację licencji, uprawnień ani uruchamianie aplikacji.

### Usługa PushToInstall systemu Windows

Sprawdza dostępność usługi Usługa PushToInstall systemu Windows. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące, wcześniej zatwierdzone żądanie instalacji zdalnej może zostać wznowione zgodnie z bieżącymi zasadami. Pakiety, rejestracje, licencje, konta, dane aplikacji, żądania w kolejce, źródła, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie tworzy, nie zatwierdza ani nie wybiera żadnego żądania, pakietu, źródła, konta ani instalacji. Weryfikacja obejmuje tylko stabilny stan usługi, a nie przetwarzanie żądań, pobieranie, instalację ani wyniki aplikacji.
