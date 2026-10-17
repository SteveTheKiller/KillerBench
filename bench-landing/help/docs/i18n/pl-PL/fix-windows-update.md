# Napraw Windows Update

Użyj tego przewodnika, gdy aktualizacje kończą się niepowodzeniem, zawieszają się lub instalują się ciągle od nowa. KillerBench najpierw odczytuje łańcuch aktualizacji, wyjaśnia błąd wraz z dowodami, a usługę uruchamia lub magazyn składników naprawia tylko wtedy, gdy świeże dowody potwierdzają dokładnie ten problem.

## Zanim zaczniesz

- Uruchom KillerBench jako administrator. Usługi aktualizacji, magazyn składników i chronione pliki systemowe wymagają podniesionych uprawnień, aby dostarczyć pełne dowody.
- Najpierw uruchom ponownie komputer, jeśli system Windows ma już oczekujące ponowne uruchomienie. Wiele napraw aktualizacji jest blokowanych, dopóki ono oczekuje.
- Podczas obsługi systemu utrzymuj komputer podłączony do zasilania.

## Ustal, dlaczego aktualizacje się nie udają

1. Otwórz **Aktualizacje** i wybierz **Windows Update** na pasku bocznym narzędzi.
2. Przy pierwszym przebiegu pozostaw wyłączoną opcję **Automatycznie napraw potwierdzone problemy** i naciśnij **Uruchom diagnozę**.
3. Przeczytaj **Czytelny raport**. Wymienia zatrzymane usługi aktualizacji, oczekujące ponowne uruchomienia, mało wolnego miejsca, uszkodzenia magazynu składników i dokładne kody błędów zapisane przez system Windows, wraz z zalecanym następnym krokiem.
4. Jeśli raport podaje kod błędu, odpowiednie kontrole uzupełniające uruchamiają się automatycznie. Kod możesz też sprawdzić samodzielnie za pomocą `killerbench-cli update-code 0x80070002`.

## Obserwuj aktualizację, która wydaje się zawieszona

Wybierz **Monitor aktywności obsługi** w sekcji **Aktualizacje** i naciśnij **Uruchom diagnozę**. Wykonuje on powtarzane migawki aktywności aktualizacji, Setup i Installera, sum transferów oraz wskaźników ponownego uruchomienia. Aktywność jest pokazywana wyłącznie jako dowód; nigdy sama nie stwierdza zawieszenia ani usterki.

## Napraw to, co wykazał raport

1. Uruchom ponownie **Windows Update** z włączoną opcją **Automatycznie napraw potwierdzone problemy**. Może ona uruchomić włączone usługi aktualizacji, które zostały potwierdzone jako zatrzymane, a DISM lub SFC uruchamia tylko wtedy, gdy kontrola na żywo potwierdzi uszkodzenie.
2. Dla pojedynczej usługi wybierz jej dedykowane narzędzie, na przykład **Usługa BITS**, **Usługi kryptograficzne**, **Instalator modułów systemu Windows** lub **Usługa Update Orchestrator**. Każde z nich oferuje odzyskiwanie tylko dla tej konkretnej usługi.
3. Gdy raport każe uruchomić ponownie komputer, zrób to przed uruchomieniem czegokolwiek innego. KillerBench zapisuje wyniki wymagające ponownego uruchomienia i odkłada dalsze prace do czasu, aż ono nastąpi.

Każda naprawa jest zapisywana w dzienniku i odczytywana ponownie. Pełną sekwencję opisuje strona [Jak zmiany pozostają bezpieczne](safety.html).

## Zainstaluj konkretną aktualizację

1. Wybierz **Dostępne aktualizacje** w sekcji **Aktualizacje** i naciśnij **Uruchom diagnozę**, aby wyszukać aktualizacje ze skonfigurowanego źródła.
2. Zaznacz wybrane aktualizacje i naciśnij **Podgląd wybranych**. Podgląd pokazuje dokładnie, co zostałoby zainstalowane, i czy wymagane jest ponowne uruchomienie.
3. Instaluj dopiero po przeczytaniu podglądu. KillerBench nigdy nie wybiera ani nie zatwierdza aktualizacji za Ciebie.

## Z wiersza polecenia

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

Zobacz [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) i [update-plan](cli.html#cmd-update-plan). Jeśli instalacja została przerwana, uruchom [update-recover](cli.html#cmd-update-recover); sprawdza ono wynik i nigdy nie ponawia aktualizacji.

## Powiązane

- [Sekcja Aktualizacje](updates.html)
- [Napraw pliki systemowe](repair-system-files.html)
- [Rozwiązywanie problemów](troubleshooting.html)
