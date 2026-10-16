# Pomoc KillerBench

KillerBench to warsztat do diagnostyki i naprawy systemu Windows przeznaczony dla techników. Najpierw sprawdza komputer, wyjaśnia, co znalazł, wraz z dowodami stojącymi za każdym ustaleniem, i zmienia system Windows tylko wtedy, gdy wybierzesz konkretną poprawkę i ją potwierdzisz.

KillerBench jest w fazie rozwoju. Nie ma jeszcze publicznej wersji do pobrania. Te strony opisują bieżącą kompilację.

## Co robi

- Wykonuje diagnostykę tylko do odczytu dla Windows Update, urządzeń, magazynu, sieci, drukowania, zabezpieczeń, aplikacji i profili, a następnie tworzy czytelny raport.
- Oferuje chronione naprawy, które działają tylko wtedy, gdy świeże dowody potwierdzają dokładny problem, a potem weryfikuje wynik.
- Obejmuje prace konfiguracyjne i wzmacniające, takie jak BitLocker, ustawienia prywatności, porządkowanie systemu Windows i wdrażanie nowego komputera.
- Wykonuje diagnostykę i analizę raportów lokalnie, bez AI i bez konta KillerBench. Wybrane akcje, takie jak pobieranie plików i kopia klucza odzyskiwania w Entra ID, mogą łączyć się z usługami zewnętrznymi.
- Używa aktywnego motywu aplikacji w konsoli, raportach, potwierdzeniach oraz oknach dialogowych Otwórz, Zapisz i Folder.

## Od czego zacząć

- [Pobieranie i konfiguracja](setup.html)
- [O KillerBench](about.html)

- [Twoja pierwsza diagnoza](first-run.html) prowadzi przez pełne skanowanie systemu.
- [Obszar roboczy](workspace.html) wyjaśnia sekcje, panele i skróty.
- [Jak zmiany pozostają bezpieczne](safety.html) wyjaśnia, co KillerBench sprawdza przed każdą zmianą i po niej.
- [Przewodniki krok po kroku](fix-windows-update.html) opisują typowe zadania.
- Strony w części **Sekcje** opisują każde narzędzie, pogrupowane tak jak w aplikacji.
- [Wiersz polecenia](cli.html) zawiera wszystkie polecenia `killerbench-cli`.
- [Rozwiązywanie problemów](troubleshooting.html) omawia zablokowane akcje, ponowne uruchomienia i przerwane żądania.

Programiści, którzy chcą osadzić diagnostykę we własnych narzędziach, mogą przeczytać [Przewodnik Engine dla programistów](../engine/index.html).
