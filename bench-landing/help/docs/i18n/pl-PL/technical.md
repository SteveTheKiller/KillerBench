# Technologia KillerBench

Zajrzyj pod maskę: jak KillerBench odczytuje komputer z systemem Windows, dba o rzetelność dowodów i decyduje, kiedy zmiana jest dozwolona.

## Stos technologiczny

KillerBench to natywna aplikacja dla systemu Windows. Aplikacja komputerowa, wiersz polecenia i działający pod nimi Engine korzystają z tych samych dowodów diagnostycznych i tych samych procesów naprawy.

| Składnik | Szczegóły |
| --- | --- |
| Interfejs | WPF na .NET 10 (`net10.0-windows`), Windows x64, własna ramka okna |
| Engine | `Engine.Core`, `Engine` i `Engine.Windows`, bez odwołań do WPF ani kontrolek aplikacji komputerowej |
| Wiersz polecenia | `killerbench-cli`, osobny host działający na tym samym Engine |
| Dostawcy Windows | .NET, interfejsy API systemu Windows, PowerShell i narzędzia wiersza polecenia systemu Windows, każdy uruchamiany w stałych limitach czasu, liczby rekordów i rozmiaru |
| Raporty | Surowe wyjście, Czytelny raport, HTML, tekst i JSON |
| Dane referencyjne | Wersjonowane pakiety wiedzy z kodami błędów, identyfikatorami sprzętu, producentami i odnośnikami pomocy technicznej |
| Pakowanie | Instalator: zależny od platformy, wymaga .NET Desktop Runtime 10 dla Windows x64. Wersja przenośna: samodzielna, bez instalacji środowiska uruchomieniowego |
| Wygląd | 13 motywów rodziny i 20 języków |

## Instalacja, foldery i dane

KillerBench jest dostępny w dwóch postaciach. Instalator to standardowe pobranie i obsługuje instalację dla jednego użytkownika lub dla wszystkich użytkowników. Wersja przenośna zawiera własne środowisko uruchomieniowe .NET i działa bez instalacji.

KillerBench działa z uprawnieniami konta, które go uruchomiło. Nie podnosi samodzielnie swoich uprawnień. Uruchom go za pomocą **Uruchom jako administrator**, gdy chronione dowody, takie jak funkcje ochrony BitLocker, lub naprawa wymagają dostępu administratora. Podniesienie uprawnień nigdy nie wybiera ani nie zatwierdza naprawy.

### Co robi instalator

Instalator weryfikuje i rozpakowuje aplikację do `%LOCALAPPDATA%\Programs\KillerBench\` albo, po monicie o podniesienie uprawnień dla wszystkich użytkowników, do `%ProgramFiles%\KillerBench\`. Dodaje skrót w menu Start, opcjonalny skrót na pulpicie oraz wpis w obszarze Dodaj lub usuń programy ze zwykłą i cichą dezinstalacją. Wersja przenośna weryfikuje i rozpakowuje swoją zawartość w `%LOCALAPPDATA%\KillerBench\Portable\` na czas bieżącego uruchomienia i przy następnym starcie usuwa porzucone kopie.

### Gdzie co się znajduje

| Lokalizacja | Zawartość |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Motyw, kolor akcentu, skala interfejsu i język |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | Jeden dziennik JSON na każde uruchomienie procesu: decyzje, ograniczone wyjście poleceń i wyniki napraw |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | Dzienniki wykonania napraw obsługi, takich jak DISM i SFC |
| Foldery funkcji w `%LOCALAPPDATA%\KillerBench\` | Wpisy oczekujące i wyniki poszczególnych chronionych akcji, na przykład `BitLockerBackup`, `Time`, `Firewall` i `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | Poziom napraw KillerMCP wybrany w sekcji Informacje |
| `HKCU` lub `HKLM\Software\KillerBench` | Ścieżka instalacji dla odpowiedniego zakresu instalacji |
| Wybrany przez Ciebie plik `.kbench` | Zapisane przechwycenie z zachowanymi dowodami |

Nic nie jest wysyłane. Usuń dziennik dopiero wtedy, gdy dowody dotyczące niepewnego wyniku nie będą Ci już potrzebne.

## Najpierw diagnoza

KillerBench traktuje diagnozę i zmianę jako dwa różne zadania. Diagnoza tylko odczytuje. Zmiana następuje dopiero wtedy, gdy wybierzesz konkretną naprawę lub akcję, przejrzysz jej opcje i wymagania wstępne i ją potwierdzisz.

Pełne skanowanie systemu jest wyłącznie diagnostyczne, także wtedy, gdy węższy proces ma włączoną automatyczną naprawę. Zapisane przechwycenia, zaimportowane dzienniki i zdarzenia historyczne nigdy nie uprawniają do naprawy.

### Ograniczone zbieranie

Każde natywne polecenie i każdy dostawca Windows działa w stałych limitach czasu, liczby rekordów i rozmiaru. Dostawca zapisuje kod zakończenia, przekroczenie czasu, obcięcie, standardowe wyjście i wyjście błędów. Dostawca, który zawiódł lub którego wynik został obcięty, nigdy nie może zostać odczytany jako czysta, pusta odpowiedź.

### Minimalne przechowywanie

Moduły zbierające zachowują to, czego potrzebuje ustalenie. Wiele wyników zachowuje nazwy, kategorie, liczby, stany i znaczniki czasu, pomijając poświadczenia, wiersze poleceń, pełne ścieżki, tożsamości kont, punkty końcowe sieci i niezwiązany ładunek zdarzeń.

## Dowody, za którymi możesz podążać

Każde ustalenie jest powiązane z dowodami, z których powstało, a raport zawsze oddziela to, co udowodniono, od tego, co tylko zaobserwowano.

| Stan | Znaczenie |
| --- | --- |
| Do przeglądu | Dowody wskazują problem, który wymaga uwagi. |
| Informacja | Dowody zostały zebrane i są pokazane jako kontekst. |
| Niekompletne lub Nieznane | Kontrola nie mogła się zakończyć albo dowody były niedostępne. Taki wynik nigdy nie jest liczony jako prawidłowy. |
| Historyczne | Zdarzenie z przeszłości. Wyjaśnia historię, ale nie dowodzi bieżącej usterki. |
| Zweryfikowano | Naprawa lub zmiana została potwierdzona przez ponowne odczytanie wyniku. |

### Nieznane to też wynik

Nieznane oznacza, że odmówiono dostępu, brakowało źródła, dostawca zawiódł, wyjście było nieprawidłowe, przekroczono limit lub dowody były sprzeczne. KillerBench pokazuje to ograniczenie i następny przydatny krok, zamiast je ukrywać.

### Historia pozostaje historią

Rekordy zdarzeń i zaimportowane dzienniki opisują to, co zapisano w przeszłości. Mogą wskazać kod błędu lub następny zakres diagnozy. Nie dowodzą, że usterka nadal występuje.

## Chronione naprawy

Naprawy to typowane operacje z regułami kwalifikacji, wykonania, weryfikacji i odzyskiwania. Nie są to ciągi poleceń wyłowione z wiersza dziennika. Każda obsługiwana naprawa przebiega w tej samej kolejności:

1. Zbierz kompletne bieżące dowody.
2. Dopasuj jedną obsługiwaną recepturę.
3. Przejrzyj wymagane opcje i konsekwencje.
4. Potwierdź wymagania wstępne dotyczące uprawnień administracyjnych i zasad.
5. Zbierz ponownie rozstrzygające dowody tuż przed zmianą.
6. Zapisz trwały rekord przygotowania.
7. Wykonaj jedną ograniczoną natywną akcję.
8. Zbierz świeże dowody weryfikacji.
9. Zapisz wynik: zweryfikowany, zablokowany, zatrzymany lub niepewny.

Jeśli rozstrzygające dowody zmienią się przed wykonaniem akcji, KillerBench zatrzymuje się i jej nie wykonuje.

### Jawne wybory

Naprawy zależne od opcji wymagają od Ciebie dokładnego wyboru, takiego jak wolumin, kolejka drukarki, miejsce docelowe klucza odzyskiwania, wartość zasilania, wersja pakietu lub źródło naprawy. KillerBench nigdy nie wymyśla tych wartości.

### Komputery zarządzane

Zasady grupy i zarządzanie urządzeniami mogą sprawić, że lokalna zmiana będzie nieskuteczna lub niewłaściwa. Obsługiwane naprawy sprawdzają, kto jest właścicielem ustawienia, i zatrzymują się, gdy ta własność jest zarządzana lub nieznana. Ustawienia należące do zasad pozostają nietknięte.

### Automatyczna naprawa

Automatyczna naprawa pozwala jedynie kontynuować kwalifikującym się recepturom. Nie omija dowodów, nie wybiera brakujących opcji, nie ignoruje zasad, nie ponawia niepewnej akcji, nie uruchamia ponownie systemu Windows i nie zamienia ustalenia wymagającego przeglądu w naprawę.

## Dzienniki i przerwania

Zanim KillerBench poprosi system Windows o zmianę, zapisuje, co zamierza zrobić. Każda zmiana jest zapisywana w dzienniku w `%LOCALAPPDATA%\KillerBench`.

Po zmianie KillerBench ponownie odczytuje wynik i zgłasza **Zweryfikowano** tylko wtedy, gdy system Windows pokazuje oczekiwany stan. Jeśli system Windows zgłasza powodzenie, ale stan się nie zmienił, wynik to wyraźnie mówi i nic nie jest powtarzane.

Proces może się zatrzymać, gdy system Windows już otrzymał żądanie, ale weryfikacja jeszcze się nie zakończyła. Wpis oczekujący pozostaje, a późniejsze uzgadnianie odczytuje świeży stan, aby ustalić, co się stało. Uzgadnianie nie jest automatycznym wycofaniem i nigdy nie powtarza pierwotnej zmiany. Zachowaj dziennik, gdy wynik jest niepewny.

## Stałe i limity

| Parametr | Wartość |
| --- | --- |
| Zakres diagnostyki | Pełne skanowanie systemu obejmuje 49 obszarów diagnostycznych i sprawdza 96 chronionych receptur naprawy |
| Odczyt źródła tekstowego | 1 000 000 znaków na źródło, wyświetlanych 2 000 pasujących wierszy |
| Źródła sesji | 12 na pasku bocznym; przechwycenie mieści do 24 źródeł i 64 MB |
| Zapytania o zdarzenia | 200 najnowszych zdarzeń na kanał z ostatnich 7 dni |
| Skanowanie Windows Update | 15 sekund na źródło, łącznie dwie minuty |
| Badanie dzienników | 10 000 przeanalizowanych wpisów, 256 różnych kodów, 8 fragmentów po 320 znaków na kod |
| Skompresowane archiwum CBS | Archiwum 256 MiB, element dziennika 512 MiB, rozpakowywanie 30 sekund |
| CHKDSK tylko do odczytu | Dziesięć minut; naprawa jest ograniczona do 65 minut |
| Kontrole DNS i TCP | Domyślnie dziesięć sekund, najwyżej jedna minuta |
| Inwentarz usług | Do 1 024 zainstalowanych usług |
| Pakiety wiedzy | Do 32 otwartych jednocześnie |
| Przegląd dużych plików | Pliki o rozmiarze co najmniej 100 MB |
| Monitor aktywności obsługi | Odstęp od 1 do 60 sekund, od 1 do 120 migawek |
| Szkice zaplanowanych zadań | Limit czasu wykonania od 1 do 4 320 minut |
| Własne źródła czasu | Do ośmiu serwerów NTP |
| Eksport aktywności | Do 150 zachowanych wpisów |
| Języki i motywy | 20 języków, 13 motywów z wariantami akcentu |

## Czego nie zrobi

- Nie wyczyści modułu TPM, nie usunie funkcji ochrony, których nie utworzył, i nie wyświetli haseł odzyskiwania.
- Nie zmieni ustawienia kontrolowanego przez zasady grupy lub zarządzanie urządzeniami.
- Nie uruchomi naprawy tylko dlatego, że zdarzenie historyczne lub zaimportowany raport wspomina o problemie.
- Nie zamieni brakujących, częściowych ani nieudanych dowodów w werdykt o prawidłowym stanie.

## Raporty

Każde uruchomienie tworzy raport, który możesz czytać, filtrować i eksportować. Raport oddziela od siebie:

1. Co zostało sprawdzone
2. Jaki problem znaleziono
3. Jakie dowody potwierdzają ustalenie
4. Jaką akcję zaproponowano lub wykonano
5. Jak zweryfikowano wynik
6. Co pozostaje nieznane lub wymaga ponownego uruchomienia

**Surowe wyjście** zachowuje dowody techniczne z oryginalnymi numerami wierszy, dzięki czemu każde ustalenie można prześledzić do źródła. **Czytelny raport** podsumowuje to samo uruchomienie prostym językiem i zaczyna od następnego bezpiecznego kroku. Raporty HTML korzystają z systemu motywów Killer Tools i pozostają czytelne bez aplikacji. Tekst stawia na przenośność, a JSON zachowuje pola strukturalne dla innego programu.

## Zapisane dzienniki i przechwycenia

KillerBench może badać dowody bez uruchamiania czegokolwiek na komputerze. Upuść dziennik tekstowy na okno albo wklej jego ścieżkę i wybierz **Otwórz dziennik**. Wiersze CBS, CSI i Setup są analizowane wraz ze znacznikami czasu, a ustalenia prowadzą do dokładnego oryginalnego wiersza. Pliki EVTX i XML zdarzeń można zaimportować w ten sam sposób.

**Skanuj Windows Update** to uruchamiane na żądanie zbieranie tylko do odczytu dzienników CBS, DISM, Panther i wycofywania Setup, kanałów zdarzeń aktualizacji i obsługi, stanów usług aktualizacji, wolnego miejsca, czasu działania, wskaźników oczekującego ponownego uruchomienia i wybranych zasad aktualizacji. **Zapisz przechwycenie** zapisuje zachowane dowody w pliku `.kbench`. **Otwórz przechwycenie** otwiera go ponownie bez oryginalnych plików i ocenia jeszcze raz zainstalowanymi regułami, więc te same reguły dają te same ustalenia.

## Tryb demonstracyjny

Uruchom `KillerBench.exe --demo`, aby wypróbować **Uruchom diagnozę** na fikcyjnej stacji roboczej. Każdy proces tworzy syntetyczne dowody i odmierzany postęp. Żaden dostawca Windows nie jest wywoływany, żadne polecenie naprawy nie jest uruchamiane i żaden dziennik nie jest zapisywany. Osobne przyciski narzędzi poza **Uruchom diagnozę** pozostają aktywne, więc nie używaj ich jako akcji demonstracyjnych.

`KillerBench.exe --demo-screenshot` otwiera raport Pełnego skanowania systemu tylko do podglądu dla stałego, fikcyjnego komputera. Kliknięcia w obszarze roboczym, skróty i upuszczanie plików są zablokowane.

## Pakiety wiedzy

Obszerne i często aktualizowane dane referencyjne znajdują się w pakietach wiedzy, a nie w logice diagnostycznej: kody błędów systemu Windows i Setup, identyfikatory producentów i urządzeń, odwołania PCI, USB, ACPI i UEFI, dane producentów OUI kart sieciowych, lokalizacje sterowników i pomocy technicznej oraz wybrane linki pomocy.

Każdy pakiet określa swój format, wersję schematu, źródło, wersję źródła, czas wygenerowania i skrót zawartości, a KillerBench sprawdza je przed użyciem. Nieznany lub nieprawidłowy pakiet pozostaje niedostępny, zamiast prowadzić do zgadywania. Aktualizacja jest pobierana do lokalizacji tymczasowej, sprawdzana, a następnie zastępuje aktywny pakiet w jednym kroku, więc nieudana aktualizacja pozostawia poprzedni pakiet na miejscu.

Dane wiedzy mogą wyjaśnić identyfikator lub podpowiedzieć, gdzie szukać dalej. Nie mogą zamienić kodu historycznego w bieżącą usterkę, automatycznie wybrać sterownika ani uprawnić do naprawy.

## Aktualizacje i podpisywanie

Przy uruchomieniu KillerBench pyta GitHub o najnowsze stabilne wydanie, z limitem czterech sekund, i proponuje aktualizację tylko wtedy, gdy to wydanie jest nowsze. Sprawdzanie możesz wyłączyć za pomocą **Sprawdzaj aktualizacje przy uruchamianiu** w sekcji Informacje lub w monicie o aktualizację. Pobierany jest pakiet zgodny z uruchomioną kopią, instalator lub wersja przenośna, a jego SHA-256 musi zgadzać się z plikiem `SHA256SUMS.txt` wydania, zanim cokolwiek zostanie zainstalowane. Aby dokończyć, aplikacja zamyka się i ponownie uruchamia. Nie ma żadnej usługi działającej w tle.

Karta Informacje pokazuje wersję, wydawcę i odcisk palca certyfikatu podpisywania kodu oraz SHA-256 uruchomionego pliku wykonywalnego, więc możesz sprawdzić posiadaną kopię.

## KillerMCP

KillerBench nie zawiera agenta AI. [KillerMCP](../mcp.html) może połączyć agenta z tym samym Engine. **Opcje KillerMCP** w sekcji Informacje decydują, co ten agent może robić:

| Ustawienie | Skutek |
| --- | --- |
| Wyłączone (domyślnie) | Agent nie może zażądać naprawy. |
| Zezwalaj KillerMCP na bezpieczne naprawy | Agent może żądać napraw sklasyfikowanych jako bezpieczne. |
| Zezwalaj też na naprawy zakłócające pracę | Stają się dostępne naprawy zakłócające pracę, a każda z nich nadal wymaga zatwierdzenia. |

Tylko osoba przy tym komputerze może podnieść poziom. Po niepewnym wyniku naprawy KillerMCP zapisuje blokadę, a naprawy agenta pozostają zablokowane, dopóki ktoś nie usunie jej w sekcji Informacje.

## Lokalizacja

Interfejs jest dostępny w 20 językach: angielskim, bengalskim, czeskim, niemieckim, hiszpańskim, francuskim, węgierskim, włoskim, japońskim, kazachskim, norweskim, polskim, portugalskim (Brazylia), rosyjskim, słoweńskim, tureckim, ukraińskim, wietnamskim oraz chińskim uproszczonym i tradycyjnym. Zmiana języka działa natychmiast, w tym dla nazw sekcji i procesów, tekstu powitalnego i otwartego czytelnego raportu. Brakujący tekst jest wyświetlany po angielsku.

Dowody, ustalenia Engine, dzienniki i Surowe wyjście pozostają po angielsku, tak samo jak dziennik systemu Windows, więc raport wygląda tak samo na każdym komputerze i można w nim wyszukiwać dokładny tekst.

## Wiersz polecenia i kody zakończenia

`killerbench-cli` uruchamia ten sam Engine bez aplikacji komputerowej. Postęp trafia na standardowe wyjście błędów, a JSON na standardowe wyjście. Większość poleceń domyślnie wykonuje podgląd, a zmiana wymaga `--execute`. Odpowiadające im polecenia `-recover` przeglądają przerwaną akcję bez jej powtarzania. Zobacz [Uruchamiaj KillerBench ze skryptów](use-the-cli.html) i [opis poleceń](cli.html).

| Kod zakończenia | Polecenia procesów | Skanowanie i otwieranie |
| --- | --- | --- |
| 0 | Brak nierozwiązanych problemów w sprawdzonych zakresach | Pełny zakres |
| 1 | Błąd uruchamiania lub dziennika | Niepowodzenie |
| 2 | Nieprawidłowe argumenty | Nieprawidłowe argumenty |
| 3 | Wymagana uwaga | Częściowe zbieranie |
| 4 | Przed weryfikacją wymagane jest ponowne uruchomienie | |
| 130 | Zatrzymano | Anulowano lub przekroczono łączny limit czasu |

Żaden kod zakończenia nie poświadcza, że komputer jest w dobrym stanie.

## Architektura

Engine dzieli się na trzy projekty bez interfejsu użytkownika. `Engine.Core` dostarcza elementy podstawowe i deklaratywne receptury, `Engine` odpowiada za wspólne procesy i dowody, a `Engine.Windows` rejestruje natywnych dostawców. Aplikacja komputerowa, CLI i testy są osobnymi hostami.

| Warstwa | Odpowiedzialność |
| --- | --- |
| Moduły zbierające możliwości | Odczytują ograniczony stan systemu Windows dla jednego dokładnego zakresu |
| Parsery dowodów | Sprawdzają kształt, tożsamość, kompletność i limity |
| Interpretacja diagnostyczna | Przekształca dowody w jawne stany procesów |
| Orkiestracja procesów | Łączy kontrole, postęp, receptury i dzienniki |
| Koordynatory napraw | Ponownie sprawdzają kwalifikację, przygotowują, zmieniają, weryfikują i uzgadniają |
| Raporty | Przedstawiają problemy, akcje, wyniki i ograniczenia |
| Pakiety wiedzy | Rozwiązują wersjonowane dane referencyjne bez zmiany logiki |

Moduły zbierające blokują działanie, gdy tożsamość jest niejednoznaczna, wyjście jest nieprawidłowe, limit został przekroczony lub nie można sprawdzić wymaganych uprawnień. Engine jest obecnie przeznaczony dla systemu Windows i nie jest wieloplatformowy. [Przewodnik dla programistów Engine](../engine/index.html) opisuje API, CLI i osadzanie Engine w innym hoście .NET.

## Prywatność i sieć

Diagnostyka i analiza dzienników działają lokalnie, bez usługi AI i bez konta KillerBench. Dzienniki i zachowane dowody pozostają w folderach `%LOCALAPPDATA%\KillerBench` lokalnego konta.

Wybrane przez Ciebie akcje mogą łączyć się ze swoim miejscem docelowym, takim jak Windows Update, usługi pobierania oprogramowania lub kopia zapasowa klucza odzyskiwania w Active Directory lub Entra ID. Sprawdzanie aktualizacji przy uruchomieniu łączy się z GitHub, a aktualizacje wiedzy i instalator KillerMCP łączą się ze skonfigurowanymi usługami. Klient AI korzystający z [KillerMCP](../mcp.html) może otrzymać wynik referencyjny, o który poprosił.

Surowe dzienniki, zaimportowane pliki i wyeksportowane dowody mogą zawierać nazwy komputerów, konta, ścieżki lub poufny tekst źródłowy. Przejrzyj je przed udostępnieniem i nigdy nie umieszczaj kluczy odzyskiwania, poświadczeń ani danych klientów w publicznym raporcie.

## Stan rozwoju

KillerBench jest w fazie rozwoju i nie ma jeszcze publicznego pobrania. Zakres napraw wciąż się poszerza, a natywna weryfikacja napraw nadal trwa. Udana kompilacja lub symulowana kontrola nie potwierdza natywnego działania napraw. Do weryfikacji każdej natywnej zmiany w różnych wersjach systemu Windows i stanach awarii używane są jednorazowe maszyny wirtualne z systemem Windows. Pełny model bezpieczeństwa opisuje strona [Jak zmiany pozostają bezpieczne](safety.html).

## Słowniczek

Proste definicje terminów używanych na tej stronie, w kolejności alfabetycznej.

| Termin | Znaczenie |
| --- | --- |
| BITS | Usługa inteligentnego transferu w tle, usługa systemu Windows, która pobiera w tle aktualizacje i inne pliki. |
| Blokowanie przy niepewności | Gdy dowody są niejednoznaczne, brakujące lub zbyt duże, wynikiem jest Nieznane i żadna zmiana nie jest dozwolona. |
| CBS | Component-Based Servicing, podsystem Windows, który instaluje aktualizacje i funkcje. Jego dziennik to `CBS.log`. |
| DISM | Deployment Image Servicing and Management, narzędzie systemu Windows, które sprawdza i naprawia magazyn składników. |
| Dowody | To, co KillerBench faktycznie odczytał z systemu Windows, przechowywane razem ze źródłem, aby można było prześledzić ustalenie. |
| Dziennik | Lokalny zapis JSON tworzony przed zmianą i aktualizowany po niej, aby przerwaną akcję można było później przejrzeć. |
| Pakiet wiedzy | Wersjonowany, sprawdzony plik danych referencyjnych, takich jak kody błędów lub identyfikatory sprzętu. |
| Panther | Folder, w którym Instalator systemu Windows zapisuje dzienniki uaktualnień. |
| Podniesienie uprawnień | Działanie z uprawnieniami administratora po monicie systemu Windows. KillerBench nigdy nie podnosi samodzielnie swoich uprawnień. |
| Proces | Jedno uruchomienie narzędzia: zbierane kontrole, receptury, których może użyć, i tworzony raport. |
| Przechwycenie | Zapisany plik `.kbench` z zachowanymi dowodami, który można przejrzeć ponownie bez oryginalnego komputera. |
| Receptura | Typowana naprawa z własnymi regułami kwalifikacji, wykonania, weryfikacji i odzyskiwania. |
| SFC | System File Checker, narzędzie systemu Windows, które weryfikuje i przywraca chronione pliki systemowe. |
| TPM | Trusted Platform Module, układ zabezpieczeń, którego BitLocker używa do ochrony klucza dysku. |
| Uzgadnianie | Odczytanie świeżego stanu po przerwaniu, aby ustalić, co się stało, bez powtarzania zmiany. |
| Właściciel zasad | Zasady grupy lub zarządzanie urządzeniami kontrolujące ustawienie. KillerBench nie zmienia ustawień, które mają właściciela. |
| Wpis oczekujący | Rekord oznaczający, że zmiana mogła zostać wysłana do systemu Windows. Blokuje jej powtórzenie, dopóki uzgadnianie nie odczyta wyniku. |
| Zweryfikowano | Wynik został odczytany z systemu Windows i zgadza się z tym, co miała osiągnąć zmiana. |
