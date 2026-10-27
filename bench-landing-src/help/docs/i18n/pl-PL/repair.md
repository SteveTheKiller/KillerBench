# Naprawa

Naprawa zbiera narzędzia, które mogą coś naprawić. Każde z nich najpierw diagnozuje. Naprawa jest oferowana tylko wtedy, gdy świeże dowody pokazują dokładnie ten problem, który usuwa, i uruchamia się dopiero po włączeniu **Automatycznie napraw potwierdzone problemy** albo samodzielnym potwierdzeniu pojedynczej naprawy. Każda naprawa jest zapisywana w dzienniku i weryfikowana po wykonaniu.

Przewodniki krok po kroku: [Napraw pliki systemowe](repair-system-files.html) i [Napraw Windows Update](fix-windows-update.html).

## Naprawa automatyczna i ręczna

**Automatycznie napraw potwierdzone problemy** pozwala narzędziu diagnostycznemu uruchomić po skanowaniu naprawy, których potrzebę udowodni. Opcja jest domyślnie wyłączona.

W przypadku pojedynczej naprawy obsługi otwórz **Zaawansowane: dzienniki i ręczne naprawy** i wybierz **Naprawy**. Wybierz **Magazyn składników (DISM)** lub **Pliki systemowe (SFC)**, przeczytaj podgląd, zaznacz **Uruchom tę naprawę na tym komputerze** i naciśnij **Uruchom naprawę**. Co dzieje się przed każdą zmianą i po niej, opisuje strona [Jak zmiany pozostają bezpieczne](safety.html).

## Narzędzia w tej sekcji

Używaj ich po tym, jak diagnoza wykryje problem. Naprawy wymagają potwierdzonych dowodów i jawnej zgody.

### Podstawowe usługi systemu Windows

Sprawdza łącznie usługi Zasilanie, Plug and Play, Menedżer użytkowników, Klient zasad grupy, Usługa interfejsu magazynu sieciowego, Mapowanie punktów końcowych RPC, Zdalne wywołanie procedury, Program uruchamiający procesy serwera DCOM, Usługa infrastruktury zadań w tle i CoreMessaging. Opcjonalne odzyskiwanie uruchamia tylko dokładnie te włączone usługi automatyczne, które według świeżych dowodów są zatrzymane po oknie uruchamiania i pełnych kontrolach zależności. Usługi uruchamiane ręcznie pozostają bez zmian, gdy są bezczynne. Plany zasilania, urządzenia, sterowniki, konta, poświadczenia, profile, zasady, karty sieciowe, adresy, trasy, DNS, konfiguracja zapory, rejestracje, punkty końcowe, rejestracje zadań, dane aplikacji, uprawnienia usług i typy uruchomienia są zachowywane. Nie jest żądane odświeżenie zasad, skanowanie urządzeń, akcja na koncie, zmiana sieci, zmiana zasilania, zmiana rejestracji ani uruchomienie zadania. Weryfikacja obejmuje tylko stabilny stan usług, a nie pełne działanie funkcji.

### Pliki systemowe

W ograniczonym zakresie inwentaryzuje stany pakietów obsługi, funkcji opcjonalnych i możliwości systemu Windows, sprawdza wymagania wstępne, skanuje chronione pliki systemowe i automatycznie wybiera SFC tylko wtedy, gdy kontrola na żywo potwierdzi uszkodzenie. Oczekujące stany pakietów, funkcji lub możliwości wymagają przeglądu, ale nie uprawniają do instalacji, usuwania, czyszczenia, wycofywania, włączania ani wyłączania.

### Repozytorium WMI

Sprawdza dostępność usługi WMI, a następnie spójność repozytorium. Automatyczna naprawa może uruchomić kwalifikującą się zatrzymaną usługę i odzyskać potwierdzone uszkodzenie, a potem zweryfikować każdy wynik. Ustawienia wyłączonej usługi są zachowywane. Czytelne rejestracje są scalane; nieczytelna zawartość może wymagać ponownej instalacji dostawcy. Klienci zarządzania mogą zostać przerwani. Bez resetowania, usuwania ani automatycznego wycofywania.

### Powiadomienia systemowe Windows

Odczytuje ostatnią historię Menedżera sterowania usługami, a następnie łącznie sprawdza System zdarzeń COM+ i Usługę powiadamiania o zdarzeniach systemowych. Opcjonalne odzyskiwanie weryfikuje gotowość Systemu zdarzeń COM+ przed uruchomieniem SENS. Istniejące powiadomienia o logowaniu, sieci, zasilaniu i COM+ mogą zostać wznowione z bieżącą konfiguracją. Katalogi COM+, subskrypcje, rejestracje powiadomień, odbiorcy, konta, uprawnienia, ustawienia odzyskiwania, zasady i typy uruchomienia są zachowywane. Żadna subskrypcja, rejestracja, odbiorca, konto, uprawnienie ani zasada nie jest zmieniana. Weryfikacja obejmuje tylko stabilny stan usług, a nie pełne dostarczanie powiadomień.

### Usługa zasad diagnostyki

Przegląda ograniczoną historię operacyjną Zasad diagnostyki, dokładną historię awarii usługi i bieżącą gotowość usługi. Niekompletne dowody mają pierwszeństwo. Zapisy historyczne nie mogą uprawniać do odzyskiwania usługi ani akcji rozwiązywania problemów. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące scenariusze diagnostyczne mogą zostać wznowione. Zasady, dane rozwiązywania problemów i typ uruchomienia są zachowywane. Nie jest uruchamiane żadne narzędzie do rozwiązywania problemów ani sugerowana poprawka. Weryfikacja obejmuje tylko stabilny stan usługi, a nie wykrywanie usterek, zakończenie diagnozy ani wynik naprawy.

### Raportowanie błędów systemu Windows

Przegląda konfigurację zrzutów awaryjnych, ograniczone metadane plików zrzutu, ostatnie raporty awarii systemu, dokładną historię awarii usługi Raportowanie błędów systemu Windows i bieżącą gotowość usługi. Raporty historyczne nie określają bieżącej usterki ani przyczyny źródłowej i nie mogą uprawniać do odzyskiwania. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące raporty w kolejce mogą zostać wznowione zgodnie z bieżącymi zasadami. Archiwa raportów, zrzuty, zgoda, zasady przesyłania i typ uruchomienia są zachowywane. Nie jest żądane przesłanie ani usunięcie. Weryfikacja obejmuje tylko stabilny stan usługi, a nie tworzenie, przesyłanie ani dostarczanie raportów, rozwiązanie problemu ani diagnozę awarii.

### Moduł zbierający zdarzenia systemu Windows

Przegląda ograniczony inwentarz subskrypcji usługi Moduł zbierający zdarzenia systemu Windows, dokładną historię awarii usługi i bieżącą gotowość usługi. Pusty inwentarz może być zamierzony, a liczba subskrypcji ani historyczne awarie nie mogą uprawniać do odzyskiwania. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące subskrypcje mogą wznowić zdalne zbieranie zdarzeń. Subskrypcje, przekazane zdarzenia, poświadczenia, ustawienia kanałów, zasady i typ uruchomienia są zachowywane. Weryfikacja obejmuje tylko stabilny stan usługi, a nie dostępność źródeł, uwierzytelnianie, zdalne dostarczanie, aktualność zdarzeń ani stan subskrypcji.

### Dziennik zdarzeń systemu Windows

Sprawdza cztery kanały diagnostyczne pod kątem włączenia, trybu przechowywania, bieżącego rozmiaru, skonfigurowanego limitu, liczby rekordów i nasycenia, a następnie sprawdza dostępność usługi Dziennik zdarzeń systemu Windows. Automatyczna naprawa może uruchomić zatrzymaną, włączoną usługę po sprawdzeniu zależności i zweryfikować jej działanie. Osobno potwierdzana, ukierunkowana akcja może włączyć jeden dokładnie wskazany, wyłączony i niezarządzany kanał diagnostyczny, zachowując wszystkie pozostałe zgłoszone ustawienia. Dzienniki, uprawnienia kanałów, przechowywanie, limity rozmiaru, typy uruchomienia i ustawienia zarządzane są zachowywane. Pomyślne dostarczanie zdarzeń i pojemność archiwum pozostają niezweryfikowane.

### Usługa czasu systemu Windows

Sprawdza konfigurację, usługę i synchronizację Czasu systemu Windows. Automatyczna naprawa może uruchomić W32Time, gdy bieżący stan zgłasza dokładny błąd zatrzymanej usługi, a świeże kontrole zasad, trybu i zależności zakończą się powodzeniem, a następnie zażądać ponownej synchronizacji i zweryfikować nowy stan. Może to skorygować zegar przy użyciu już skonfigurowanych źródeł. Typ uruchomienia, konfiguracja źródeł i ustawienia zarządzane są zachowywane.

### Usługa Harmonogram zadań

W ograniczonym zakresie inwentaryzuje stan zaplanowanych zadań, wyniki uruchomień i liczbę pominiętych uruchomień, a następnie łączy je z dokładną historią awarii usługi Harmonogram zadań przed sprawdzeniem bieżącej dostępności usługi. Ustalenia dotyczące zadań i historyczne awarie usługi nie mogą uprawniać do uruchomienia usługi. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące włączone zadania mogą rozpocząć się na skonfigurowanych kontach, w tym dla pominiętych wyzwalaczy. Żadna definicja zadania, podmiot zabezpieczeń, odwołanie do poświadczeń, zasada ani typ uruchomienia nie jest zmieniany i żadne zadanie nie jest bezpośrednio uruchamiane ani usuwane. Weryfikacja obejmuje tylko stabilny stan usługi, a nie powodzenie akcji, ważność poświadczeń ani przyszłe wyniki zadań.

### Usługa profilu użytkownika

Sprawdza dostępność usługi Usługa profilu użytkownika. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące żądania logowania i profilu mogą zostać wznowione. Foldery profili, gałęzie rejestru, uprawnienia, rejestracje na liście profili, poświadczenia, dane użytkownika, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. Nie jest żądana żadna operacja na profilu. Weryfikacja obejmuje tylko stabilny stan usługi, a nie logowanie, ładowanie profilu ani dostęp do danych użytkownika.

### Usługi kopii zapasowej

Sprawdza usługi Kopiowanie woluminów w tle, Dostawca kopiowania w tle oprogramowania firmy Microsoft, Kopia zapasowa systemu Windows i Usługa aparatu kopii zapasowej na poziomie bloku, a następnie odczytuje stan modułów zapisujących VSS, zarejestrowane typy dostawców, bieżące migawki i przydział magazynu kopii w tle. Zatrzymane usługi uruchamiane ręcznie mogą być czymś normalnym, gdy są bezczynne. Opcjonalne odzyskiwanie uruchamia tylko zatrzymane usługi automatyczne po kontrolach zależności, a następnie weryfikuje ich działanie. Istniejące zaplanowane lub przerwane tworzenie kopii zapasowej może zostać wznowione. Ustawienia uruchamiania wyłączonego i ręcznego, migawki, magazyn kopii w tle, rejestracje dostawców, plany kopii zapasowych, katalogi, harmonogramy, miejsca docelowe, poświadczenia i przechowywanie są zachowywane. Żadna kopia zapasowa nie jest wybierana ani ponownie konfigurowana, żadna usługa nie jest uruchamiana ponownie i żadna migawka ani katalog nie jest usuwany. Rejestracja dostawcy, stan modułów zapisujących, przydział magazynu i stabilny stan usług nie weryfikują ukończenia kopii zapasowej, dostępu do miejsca docelowego ani powodzenia przywracania.
