# Aktualizacje

Aktualizacje obejmuje Windows Update, stojące za nim usługi i magazyn składników. Możesz zobaczyć, co jest zainstalowane, co oczekuje i dlaczego aktualizacja się nie udaje, a także przejrzeć dostępne aktualizacje przed wybraniem tych do instalacji.

Przewodniki krok po kroku: [Napraw Windows Update](fix-windows-update.html) i [Napraw pliki systemowe](repair-system-files.html).

## Narzędzia w tej sekcji

Obserwuj aktywność obsługi, przeglądaj dostępne aktualizacje i naprawiaj łańcuch Windows Update.

### Monitor aktywności obsługi

W ograniczonym zakresie obserwuje aktywność obsługi Windows Update i Setup w jednej lub kilku migawkach. Opcje określają częstotliwość odświeżania oraz to, czy uwzględniane są zdarzenia aktualizacji, historia aktualizacji, dokładne zaplanowane zadanie Windows Update, dokładne stany usług, wskaźniki ponownego uruchomienia, transfery w tle bieżącego konta, aktywność Optymalizacji dostarczania i ograniczona ze względu na prywatność aktywność Instalatora Windows. Identyfikatory procesów, wiersze poleceń, użytkownicy i ścieżki są wykluczone. Aktywność nie dowodzi postępu, zakończenia, zawieszenia ani usterki. Nie jest żądana żadna akcja aktualizacji, instalacji, transferu, zadania, usługi, procesu, zasad, ponownego uruchomienia ani naprawy.

### Dostępne aktualizacje

Wyszukuje wybrane aktualizacje oprogramowania i sterowników za pomocą skonfigurowanego źródła, Windows Update lub już zarejestrowanej usługi Microsoft Update. Gotowość źródła jest sprawdzana przed wyszukiwaniem. Bez instalacji, akceptacji licencji, rejestracji źródła ani zmian zasad. Jest to oddzielone od naprawy obsługi.

### Windows Update

W ograniczonym zakresie inwentaryzuje stany pakietów obsługi, funkcji opcjonalnych i możliwości systemu Windows, sprawdza wskaźniki ponownego uruchomienia, wolne miejsce i usługi aktualizacji, obserwuje Optymalizację dostarczania bez jej uruchamiania, a następnie skanuje magazyn składników i chronione pliki systemowe. Oczekujące stany pakietów, funkcji lub możliwości wymagają przeglądu, ale nie uprawniają do żadnej akcji na składnikach. Automatyczna naprawa może uruchomić włączone usługi Windows Update, BITS, Usługi kryptograficzne, Instalator modułów systemu Windows i Update Orchestrator. Uruchomienie Update Orchestrator może wznowić istniejącą zaplanowaną aktywność aktualizacji zgodnie z bieżącymi zasadami. Pakiety, funkcje opcjonalne, możliwości, pamięć podręczna Optymalizacji dostarczania, elementy równorzędne, przepustowość, zasady, rejestracja źródła, harmonogramy i ustawienia uruchamiania są zachowywane. DISM lub SFC wymagają dowodów uszkodzenia uzyskanych na żywo. Ten proces nie wybiera ani nie zatwierdza żadnej aktualizacji.

### Usługa Windows Update

Sprawdza dokładny stan usługi Usługa Windows Update, zarejestrowaną bibliotekę DLL usługi, ograniczoną bieżącą aktywność aktualizacji i dokładną historię awarii usługi. Obserwacje biblioteki DLL, aktywność aktualizacji i historyczne awarie nie mogą uprawniać do uruchomienia usługi ani operacji aktualizacji. Opcjonalne odzyskiwanie uruchamia tylko zatrzymaną usługę automatyczną po świeżych kontrolach zależności i rejestracji pliku wykonywalnego. Zatrzymana usługa uruchamiana ręcznie pozostaje wyłącznie informacją. Istniejąca aktywność aktualizacji może zostać wznowiona zgodnie z bieżącymi zasadami. Zasady aktualizacji, rejestracja źródła, harmonogramy, ustawienia ponownego uruchamiania, pakiety, zadania BITS, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera, nie zatwierdza ani nie instaluje żadnej aktualizacji. Weryfikacja obejmuje tylko stabilny stan usługi, a nie dostępność źródła, możliwość zastosowania, pobieranie, instalację, zakończenie ani gotowość do ponownego uruchomienia.

### Optymalizacja dostarczania

Sprawdza dokładny stan usługi Optymalizacja dostarczania. Zatrzymana usługa uruchamiana ręcznie pozostaje wyłącznie informacją. Odzyskiwanie może zaproponować tylko dokładny stan Wstrzymana. Opcjonalne odzyskiwanie powtarza kontrolę stanu, wysyła jedno ograniczone żądanie kontynuacji i wymaga dwóch spójnych obserwacji stanu Uruchomiona. Typ uruchomienia, zawartość pamięci podręcznej, elementy równorzędne, przepustowość, źródło aktualizacji i zarządzane zasady są zachowywane. Aktywność transferu, stan pamięci podręcznej, postęp i zakończenie aktualizacji pozostają niezweryfikowane.

### Usługa BITS

Sprawdza dokładny stan Usługi inteligentnego transferu w tle, zadania transferu bieżącego konta i ograniczoną, dokładną historię awarii usługi. Historyczne awarie i migawki transferów nie mogą uprawniać do uruchomienia usługi. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące aplikacje mogą wznowić transfery w tle z kolejki. Zadania, priorytety, właściciele, miejsca docelowe, ustawienia serwera proxy, zasady i typ uruchomienia są zachowywane. Weryfikacja obejmuje tylko stabilny stan usługi, a nie postęp transferu, własność aplikacji ani zakończenie.

### Transfery w tle (BITS)

Odczytuje zadania transferu BITS w wybranym zakresie kont i przegląda dokładne tożsamości zadań. Nazwy kolejek nie określają, do której aplikacji należą. Żaden transfer nie jest wznawiany, kończony, anulowany, usuwany ani przypisywany ponownie.

### Usługi kryptograficzne

Sprawdza dokładny stan usługi Usługi kryptograficzne, zarejestrowaną bibliotekę DLL usługi i ograniczoną, dokładną historię awarii usługi. Historyczne awarie i obserwacje biblioteki DLL nie mogą uprawniać do uruchomienia usługi ani wymiany składników. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące operacje na katalogach, certyfikatach i aktualizacjach mogą zostać wznowione. Certyfikaty, klucze, katalogi, ustawienia zaufania, zasady i typ uruchomienia są zachowywane. Weryfikacja obejmuje tylko stabilny stan usługi, a nie weryfikację certyfikatów, podpisów, katalogów ani zaufania.

### Instalator modułów systemu Windows

Sprawdza dokładny stan usługi Instalator modułów systemu Windows, ograniczone przejścia pakietów obsługi i dokładną historię awarii usługi. Historyczne awarie i stany pakietów nie mogą uprawniać do uruchomienia usługi ani operacji na pakietach. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejąca aktywność obsługi może zostać wznowiona. Pakiety, zawartość magazynu składników, źródła, zasady i typ uruchomienia są zachowywane. Weryfikacja obejmuje tylko stabilny stan usługi, a nie kondycję magazynu składników, możliwość zastosowania pakietów, zakończenie obsługi ani gotowość do ponownego uruchomienia.

### Usługa Windows Update Medic

Sprawdza dokładny stan usługi Usługa Windows Update Medic, zarejestrowaną bibliotekę DLL usługi, ograniczoną bieżącą aktywność aktualizacji i dokładną historię awarii usługi. Obserwacje biblioteki DLL, aktywność aktualizacji i historyczne awarie nie mogą uprawniać do uruchomienia usługi ani akcji na składnikach. Zatrzymana usługa uruchamiana ręcznie może być czymś normalnym. Gdy wybrane jest dokładnie to narzędzie, opcjonalne odzyskiwanie może uruchomić tylko zatrzymaną włączoną usługę po świeżych kontrolach zależności. Naprawa przez system Windows może zostać wznowiona zgodnie z bieżącymi zasadami i może naprawić składniki aktualizacji lub wywołać aktywność aktualizacji. Zasady aktualizacji, rejestracja źródła, harmonogramy, pakiety, zadania BITS, ustawienia ponownego uruchamiania, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej aktualizacji i nie zmienia żadnego ustawienia aktualizacji. Weryfikacja obejmuje tylko stabilny stan usługi, a nie naprawę, kondycję składników, możliwość zastosowania, pobieranie, instalację ani gotowość do ponownego uruchomienia.

### Usługa Update Orchestrator

Sprawdza dokładny stan usługi Usługa Update Orchestrator, dokładne wbudowane zadanie Windows Update, ograniczoną bieżącą aktywność i dokładną historię awarii usługi. Wyniki zadań, aktywność aktualizacji i historyczne awarie nie mogą uprawniać do uruchomienia usługi ani operacji aktualizacji. Opcjonalne odzyskiwanie uruchamia tylko zatrzymaną usługę automatyczną po świeżych kontrolach zależności i rejestracji pliku wykonywalnego. Zatrzymana usługa uruchamiana ręcznie pozostaje wyłącznie informacją. Istniejąca zaplanowana aktywność aktualizacji może zostać wznowiona zgodnie z bieżącymi zasadami i może pobrać lub zainstalować już zatwierdzone aktualizacje. Zasady aktualizacji, rejestracja źródła, harmonogramy, ustawienia ponownego uruchamiania, pakiety, zadania BITS, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera ani nie zatwierdza żadnej aktualizacji. Weryfikacja obejmuje tylko stabilny stan usługi, a nie możliwość zastosowania, pobieranie, instalację, zakończenie ani gotowość do ponownego uruchomienia.

### Czyszczenie magazynu składników

Analizuje, czy system Windows zaleca czyszczenie magazynu składników. Opcjonalna konserwacja natychmiast usuwa zastąpione składniki, a następnie ponownie wykonuje analizę. Bez ResetBase i bez ręcznego usuwania zawartości WinSxS. Bez automatycznego cofania; odzyskane miejsce na dysku nie jest mierzone.
