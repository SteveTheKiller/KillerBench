# Pobieranie i konfiguracja

KillerBench 1.0.0 nie został jeszcze wydany. Nie jest jeszcze dostępny żaden publiczny pakiet ani plik do pobrania. Ta strona opisuje bieżące wymagania pakietu, aby można było się przygotować bez pobierania nieoficjalnej kopii.

## Windows i środowisko uruchomieniowe

- Bieżącym celem publikacji jest **Windows x64**. Windows 11 x64 jest obecnym punktem odniesienia dla natywnej weryfikacji. Nie ma natywnych wersji dla ARM64, x86 ani Windows Server.
- KillerBench jest pakowany na dwa sposoby. Instalator jest **zależny od platformy** i wymaga **.NET Desktop Runtime 10 dla Windows x64**. Zwykłe .NET Runtime, ASP.NET Core Runtime ani .NET Framework nie zawierają WPF. Wersja przenośna ma własne środowisko uruchomieniowe i nie wymaga osobnej instalacji. Narzędzie wiersza polecenia `killerbench-cli.exe` wymaga .NET 10 Runtime dla Windows x64. Zestaw SDK jest potrzebny do programowania, a nie do zwykłego użytku.
- Używaj wersji systemu Windows obsługiwanej przez [.NET 10 w systemie Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows). Obsługa środowiska uruchomieniowego nie oznacza, że naprawy KillerBench zostały zweryfikowane w każdej obsługiwanej edycji lub kompilacji. Obsługa Windows 10 wciąż czeka na weryfikację, a jego obsługiwane edycje zależą od bieżących wymagań cyklu życia firmy Microsoft.
- Pobieraj Desktop Runtime wyłącznie ze [strony pobierania .NET 10 firmy Microsoft](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), wybierając **.NET Desktop Runtime**, **Windows**, **x64**. Przestrzegaj zasad instalacji oprogramowania obowiązujących w Twojej organizacji.

Jeśli `dotnet` jest dostępne, `dotnet --list-runtimes` powinno wyświetlić `Microsoft.WindowsDesktop.App 10.0.x` dla instalacji x64. Środowisko zainstalowane tylko dla innej architektury nie spełnia wymagań pakietu instalacyjnego x64.

## Gdy pakiet zostanie opublikowany

1. Użyj oficjalnego pliku do pobrania, do którego prowadzi ta witryna. Żaden link do pobrania KillerBench nie jest jeszcze aktywny.
2. Przeczytaj wymagania pakietu, [licencję GPLv3](../license.txt), instrukcje dotyczące Odpowiadającego kodu źródłowego, dołączone informacje o elementach innych firm oraz informacje o podpisie i sumie kontrolnej. Sama nazwa pliku nie potwierdza autentyczności.
3. Uruchom instalator dla bieżącego konta albo dla wszystkich użytkowników po monicie o podniesienie uprawnień. Dodaje on skrót w menu Start, opcjonalny skrót na pulpicie oraz wpis w Dodaj lub usuń programy. Wersja przenośna działa bez instalacji.
4. Uruchom KillerBench na zwykłym koncie i postępuj według strony [Twoja pierwsza diagnoza](first-run.html). Błędy dostępu podczas diagnostyki pozostają widoczne; podniesienie uprawnień to osobna, świadoma decyzja.

Nie omijaj ostrzeżeń zabezpieczeń systemu Windows, aby uruchomić niezweryfikowaną kopię. Przed wybraniem jakiejkolwiek akcji zmieniającej komputer miej aktualną kopię zapasową.

Przy uruchamianiu KillerBench sprawdza w serwisie GitHub, czy jest nowsza stabilna wersja, i pyta przed jej instalacją. Pobrany plik musi odpowiadać opublikowanej sumie kontrolnej SHA-256. Wyłącz to sprawdzanie opcją **Sprawdzaj aktualizacje przy uruchamianiu** w sekcji Informacje.

## Bieżące ograniczenia

Bieżąca kompilacja ma dwanaście sekcji, diagnostykę tylko do odczytu, raporty i indywidualnie wybierane obszary napraw. Natywna weryfikacja napraw wciąż trwa. Działający interfejs, udana kompilacja ani wynik testu na danych przykładowych nie potwierdzają działania naprawy na prawdziwym komputerze. Zobacz [Jak zmiany pozostają bezpieczne](safety.html#development-status).

Przykładowa konsola na stronie głównej pokazuje syntetyczne dane. Pierwszy publiczny pakiet nadal jest w przygotowaniu.

## Kod źródłowy, licencja i pomoc

KillerBench jest objęty licencją GPLv3. Instrukcje dostarczania kodu źródłowego i uprawnienia do czcionek innych firm wciąż czekają na pierwszy publiczny pakiet. Przeczytaj stronę [O KillerBench](about.html), aby poznać dostępność kodu źródłowego, informacje o dystrybucji, prywatność dowodów i informacje o pomocy. [Przewodnik Engine](../engine/) jest przeznaczony dla programistów z autoryzowaną kopią kodu źródłowego; nie oznacza, że publiczny pakiet Engine jest dostępny.
