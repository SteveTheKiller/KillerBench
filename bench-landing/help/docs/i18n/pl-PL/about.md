# O KillerBench

KillerBench to warsztat do diagnostyki, naprawy i konserwacji systemu Windows autorstwa Steve the Killer, należący do [rodziny Killer Tools](https://killertools.net). Wersja 0.1.0 nie została jeszcze wydana. Aplikacja komputerowa, Engine i CLI współdzielą dowody diagnostyczne oraz obsługiwane procesy naprawy.

## Dostępność i zakres

Nie jest jeszcze dostępny żaden publiczny plik KillerBench do pobrania ani pakiet Engine. Strona [Pobieranie i konfiguracja](setup.html) opisuje bieżący cel Windows x64 i wymagania Desktop Runtime. Natywna weryfikacja napraw wciąż trwa; obecność narzędzia nie dowodzi, że przetestowano każdą usterkę, przerwę lub konfigurację systemu Windows.

## Informacje o kodzie źródłowym i dystrybucji

KillerBench jest objęty licencją **GPLv3**. Jego licencją dystrybucyjną jest [Powszechna Licencja Publiczna GNU w wersji 3](../license.txt). Licencja opisuje uprawnienia i warunki kopiowania, modyfikowania i rozpowszechniania objętego nią utworu.

Publiczne [repozytorium KillerBench](https://github.com/SteveTheKiller/KillerBench) zawiera obecnie informacje o projekcie. Pełna implementacja nie jest tam publicznie dostępna. Te przewodniki dla programistów opisują autoryzowaną kopię kodu źródłowego; nie udostępniają publicznie kodu źródłowego do pobrania.

Przed rozpowszechnieniem pliku binarnego wydawca musi udostępnić Odpowiadający kod źródłowy w sposób dozwolony przez [sekcję 6 licencji GPLv3](https://www.gnu.org/licenses/gpl-3.0.en.html#section6), w tym kod źródłowy i materiały kompilacji wymagane przez licencję. Instrukcje dostarczania kodu źródłowego dla pierwszego publicznego pakietu i dołączone informacje o elementach innych firm wciąż są w przygotowaniu. Licencja GPLv3 nie daje prawa do rozpowszechniania czcionek innych firm; bieżące uprawnienia do czcionek wymagają jeszcze weryfikacji. Przed ponownym rozpowszechnieniem opublikowanego pakietu przejrzyj jego licencję, instrukcje dotyczące kodu źródłowego i informacje.

## Prywatność i usługi zewnętrzne

Diagnostyka i analiza dzienników działają lokalnie, bez usługi AI i bez konta KillerBench. Dzienniki i zachowane dowody znajdują się w folderach `%LOCALAPPDATA%\KillerBench` lokalnego konta. Wybrane akcje mogą łączyć się z Windows Update, usługami pobierania oprogramowania, katalogami lub innymi wskazanymi miejscami docelowymi. Sprawdzanie aktualizacji przy uruchamianiu łączy się z serwisem GitHub, a aktualizacje wiedzy i strona instalatora MCP łączą się ze skonfigurowanymi usługami. Klient AI korzystający z MCP może otrzymać zwrócony wynik referencyjny.

Ta witryna przechowuje wybór motywu i koloru akcentu w pamięci lokalnej przeglądarki. Jej przewodniki nie wykonują diagnostyki na Twoim komputerze. Surowe dzienniki, zaimportowane pliki i wyeksportowane dowody mogą zawierać nazwy komputerów, konta, ścieżki lub poufny tekst źródłowy. Przejrzyj je przed udostępnieniem; nie umieszczaj kluczy odzyskiwania, poświadczeń ani danych klientów w publicznym raporcie.

## Pomoc

Informacje o projekcie są dostępne w publicznym repozytorium. Aby uzyskać pomoc, skorzystaj z danych kontaktowych opublikowanych w [witrynie autora](https://thekiller.net); podaj wersję aplikacji, kompilację systemu Windows, dokładne narzędzie, oczekiwany wynik i zaobserwowany wynik. Przed wysłaniem przejrzyj i zanonimizuj dowody, a informacje o klientach przekazuj zatwierdzonym prywatnym kanałem. Zachowuj dzienniki, gdy wynik naprawy jest niepewny.

## Powiązane przewodniki

- [Jak zmiany pozostają bezpieczne](safety.html)
- [Raporty i dowody](reports.html)
- [Przewodnik Engine dla programistów](../engine/)
- [KillerMCP](../mcp.html)
