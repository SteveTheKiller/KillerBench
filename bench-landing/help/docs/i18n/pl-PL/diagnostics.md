# Diagnostyka

Diagnostyka to miejsce, od którego powinna zaczynać się każda wizyta. Każde narzędzie odczytuje komputer i tworzy raport. Nic tutaj nie zmienia systemu Windows. Narzędzie domyślne, pełne skanowanie systemu, wykonuje szeroki zestaw kontroli tylko do odczytu i układa według ważności to, co wymaga uwagi.

Przewodniki krok po kroku: [Zbadaj awarie i niebieskie ekrany](investigate-crashes.html) i [Napraw pliki systemowe](repair-system-files.html).

## Narzędzia w tej sekcji

Najpierw sprawdź komputer, a potem przeczytaj w raporcie, co wymaga uwagi i jaki jest następny bezpieczny krok.

### Pełne skanowanie systemu

Sprawdza 49 obszarów diagnostycznych, a następnie ocenia każdą zainstalowaną recepturę odzyskiwania usług wraz z recepturami obsługi, DHCP, drukowania, Defendera, czasu i WMI. Kontrole mogą potrwać kilka minut. Automatyczna naprawa jest opcjonalna i wymaga bieżących dowodów, wymagań wstępnych komputera oraz kontroli zależności właściwych dla każdej receptury. Kwalifikujące się naprawy mogą odnowić brakujące dzierżawy DHCP, zwolnić zadania drukowania z kolejki, ponownie zsynchronizować zegar, odświeżyć sygnatury Defendera, przywrócić kwalifikujące się usługi i naprawić potwierdzone uszkodzenia systemu Windows. Zatrzymane usługi automatyczne mogą zostać uruchomione po weryfikacji okna uruchamiania i zależności. Zatrzymana usługa uruchamiana ręcznie pozostaje bez zmian, gdy jedynym dowodem jest stan bezczynności; węższe procesy mogą jej wymagać tylko wtedy, gdy bieżące dowody funkcji uzasadniają uruchomienie. Typy uruchomienia, definicje zadań, raporty, zrzuty, subskrypcje, poświadczenia, ustawienia kanałów, zasady zapory i ustawienia zarządzane są zachowywane. Migawki historyczne, kopii zapasowych i transferów są tylko do odczytu. Zawartość zrzutów nie jest otwierana. Sprawdzenie bitu zabrudzenia nie planuje naprawy ani ponownego uruchomienia. Ten proces nie instaluje aktualizacji, nie zmienia sterowników, nie usuwa plików i nie wykonuje pełnego testu sprzętu.

### Parametry życiowe

Rejestruje dostępną pamięć, stan dysków zgłaszany przez dostawcę i stan ochrony BitLocker, a następnie zestawia bieżące parametry z problemami urządzeń, historią niezawodności, zapisami błędów sprzętu, dowodami awarii, nieoczekiwanymi zamknięciami i wynikami Diagnostyki pamięci systemu Windows. Wskaźniki ponownego uruchomienia, wolne miejsce na dysku systemowym, podniesienie uprawnień i konfiguracja usługi aktualizacji pozostają widoczne. Hasła odzyskiwania nie są zbierane. Ten proces nie wykonuje napraw. Łączny wynik nie jest pełnym testem sprzętu, oprogramowania ani stabilności.

### Historia niezawodności

Odczytuje zapisy Monitora niezawodności systemu Windows z ostatnich 14 dni wraz z ostatnimi błędami z dziennika Aplikacja i zdarzeniami nieoczekiwanego zamknięcia, a następnie zwraca jeden ograniczony wniosek historyczny. Zapisy historyczne nie określają bieżącego stanu ani przyczyny źródłowej. Nie jest wybierana żadna naprawa i nie są zmieniane żadne aplikacje, usługi ani ustawienia.

### Wydajność uruchamiania

Odczytuje metadane zdarzeń wydajności uruchamiania i zamykania systemu Windows z ostatnich 14 dni, zachowując tylko tożsamość dostawcy i ograniczone liczbowe pola czasu trwania, a następnie zestawia je z obsługiwanymi wskaźnikami oczekującego ponownego uruchomienia. Ścieżki, nazwy procesów, konta i pozostały ładunek zdarzeń są usuwane przed zapisaniem dowodów. Historyczne zapisy spadku wydajności nie wskazują bieżącego wąskiego gardła ani nie uprawniają do zmian uruchamiania, usług, aplikacji, sterowników, zadań, zasilania, ponownego uruchomienia czy zasad.

### Historia błędów sprzętu

Odczytuje historię Windows Hardware Error Architecture i diagnostyki pamięci z ostatnich 14 dni, bieżące problemy urządzeń Plug and Play, stan dysków fizycznych i liczniki niezawodności dostawcy, a następnie zwraca jeden ograniczony wniosek dotyczący sprzętu. Zapisy wymagają zestawienia z ich ładunkiem, diagnostyką oprogramowania układowego i testami sprzętu producenta. Nie jest wnioskowany żaden uszkodzony składnik ani przyczyna źródłowa, nie jest wybierana żadna naprawa i nie jest zmieniany sprzęt, oprogramowanie układowe, sterownik, dysk, pula, wolumin, plik, usługa, zasilanie, ponowne uruchomienie ani ustawienie.

### Historia instalacji sterowników

Odczytuje zachowany koniec historii instalacji urządzeń Windows SetupAPI, zestawia nieudane sekcje z dokładnymi bieżącymi tożsamościami Plug and Play i zwraca jeden ograniczony wniosek. Historyczne sekcje dziennika nie dowodzą, że urządzenie nadal jest obecne ani że błąd jest bieżący. Żaden sterownik nie jest instalowany, usuwany, wycofywany ani aktualizowany.

### Punkty Przywracania systemu

Wyświetla do 256 dostępnych punktów Przywracania systemu, przegląda jawną możliwość utworzenia punktu i zwraca jeden ograniczony wniosek o gotowości. Inwentarz nie dowodzi, że ochrona jest włączona dla każdego woluminu ani że przywracanie się powiedzie. Podczas diagnozy żaden punkt przywracania nie jest tworzony, usuwany ani stosowany.

### Środowisko odzyskiwania systemu Windows

Odczytuje bieżącą konfigurację Środowiska odzyskiwania systemu Windows za pomocą REAgentC, zestawia włączoną lokalizację lokalną z dokładnym bieżącym dyskiem i partycją i zwraca jeden ograniczony wniosek o gotowości. Opcjonalna automatyczna naprawa może włączyć istniejącą wyłączoną konfigurację, a następnie zweryfikować zgłaszany stan. Obraz odzyskiwania nie jest pobierany, przenoszony ani zastępowany, nie jest inicjowany rozruch odzyskiwania i system Windows nie jest uruchamiany ponownie. Stan włączony i obecność partycji nie dowodzą, że obraz odzyskiwania może uruchomić lub naprawić ten komputer.

### Aktywacja systemu Windows

Odczytuje bieżący stan licencji systemu Windows bez zbierania pełnych ani częściowych kluczy produktu, zachowuje zanonimizowane metadane zdarzeń ostrzeżeń, błędów i krytycznych Ochrony oprogramowania z ostatnich siedmiu dni, a następnie zwraca jeden ograniczony wniosek operacyjny. Ładunek zdarzeń jest usuwany przed zapisaniem dowodów. Zapisy historyczne pozostają kontekstem, gdy bieżące licencjonowanie zgłasza stan Licencjonowany. Nie jest wybierana żadna akcja aktywacji, klucza produktu, tokenu, licencji, konta, usługi ani zasad. Stan dostawcy i historia zdarzeń nie dowodzą uprawnienia do subskrypcji, dostępności serwera aktywacji, integralności magazynu tokenów, przyszłej ważności ani przyczyny źródłowej.

### Usługi systemu Windows

Inwentaryzuje nazwy zainstalowanych usług, nazwy wyświetlane, stany, tryby uruchamiania i ograniczone ze względu na prywatność typy akcji odzyskiwania, bez zbierania kont, ścieżek plików wykonywalnych, poleceń odzyskiwania ani komunikatów ponownego uruchomienia. Przegląda spójność trybów uruchamiania, tożsamości i cykle bezpośrednich zależności, wykrywa powtarzające się dokładne stany oczekujące, sprawdza obsługiwane usługi i zależności zatrzymanych usług, a następnie zestawia ostatnie awarie Menedżera sterowania usługami tylko wtedy, gdy obecna jest jedna dokładna nazwa usługi lub unikatowa nazwa wyświetlana. Użycie grup kolejności ładowania jest liczone bez rozwijania członków, a niejednoznaczne tożsamości pozostają nierozstrzygnięte. Zatrzymane usługi uruchamiane ręcznie mogą być czymś normalnym. Żadna usługa nie jest uruchamiana, a ustawienia uruchamiania, zależności i odzyskiwania są zachowywane. Gdy konkretna funkcja wymaga odzyskiwania, użyj odpowiedniego procesu w sekcji Naprawa.

### Błędy aplikacji

Odczytuje ostatnie zdarzenia błędów i krytyczne z dziennika Aplikacja wraz z ograniczoną historią Monitora niezawodności, a następnie zwraca jeden wniosek o historii stabilności. Zapisy historyczne nie dowodzą bieżącej usterki ani przyczyny źródłowej i jedynie kierują do węższej diagnozy. Żadne aplikacje nie są zatrzymywane, naprawiane, usuwane ani ponownie konfigurowane.

### Urządzenia wyświetlające

Sprawdza karty graficzne i monitory pod kątem zgłaszanych błędów urządzeń, identyfikatorów sprzętu i wersji zainstalowanych sterowników. Bez instalacji sterowników, resetowania urządzeń ani zmiany rozdzielczości. Jakość obrazu i połączenia fizyczne nie są testowane.

### Urządzenia wejściowe

Sprawdza błędy klawiatur, myszy i urządzeń HID oraz inwentarz sterowników. Nie są rejestrowane naciśnięcia klawiszy, nie są resetowane urządzenia ani zmieniane sterowniki. Fizyczne działanie urządzeń wejściowych nie jest testowane.

### Problemy z urządzeniami

Znajduje niezerowe kody błędów konfiguracji Plug and Play we wszystkich klasach urządzeń, a następnie zestawia dowody dotyczące obecnych urządzeń z ograniczoną historią SetupAPI dotyczącą instalacji sterowników, konfiguracji urządzeń i Plug and Play. Same zapisy historyczne nie określają bieżącej usterki. Przejrzyj dokładną tożsamość urządzenia i dowody zatwierdzonych sterowników bez zmiany sprzętu, sterowników ani konfiguracji. Brakujące urządzenia i działanie fizyczne nie są weryfikowane.
