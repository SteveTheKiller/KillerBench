# Napraw pliki systemowe

Użyj tego przewodnika, gdy składniki systemu Windows są uszkodzone: funkcje nie dają się zainstalować, aktualizacje zgłaszają błędy składników albo narzędzia systemowe ulegają awarii. KillerBench uruchamia DISM i SFC dopiero wtedy, gdy kontrola na żywo potwierdzi uszkodzenie, a następnie weryfikuje wynik.

## Zanim zaczniesz

- Uruchom KillerBench jako administrator.
- Najpierw obsłuż oczekujące ponowne uruchomienie.
- Jeśli komputer jest zarządzany, sprawdź, czy zasady nie kierują DISM do określonego źródła naprawy. KillerBench zgłasza te zasady i ich nie zmienia.

## Sprawdź uszkodzenia

1. Otwórz **Naprawa** i wybierz **Pliki systemowe**.
2. Pozostaw wyłączoną opcję **Automatycznie napraw potwierdzone problemy** i naciśnij **Uruchom diagnozę**.
3. Przeczytaj raport. Inwentaryzuje on stan pakietów obsługi, funkcji opcjonalnych i możliwości, sprawdza wymagania wstępne i skanuje chronione pliki systemowe. Czysty wynik oznacza, że żadna naprawa nie jest oferowana.

## Napraw potwierdzone uszkodzenia

1. Włącz **Automatycznie napraw potwierdzone problemy** i naciśnij **Uruchom naprawę**. KillerBench wybiera DISM lub SFC tylko wtedy, gdy kontrola na żywo potwierdzi uszkodzenie.
2. Poczekaj na weryfikację. Raport pokazuje **Zweryfikowano** tylko wtedy, gdy kontrola uzupełniająca odczyta naprawiony stan.
3. Jeśli raport stwierdza, że magazynu nie da się odzyskać albo źródło naprawy jest niedostępne, zatrzymaj się. Powtarzanie naprawy nie zmieni wyniku.

Przerwane uruchomienie DISM lub SFC jest uzgadniane przy następnym rozruchu bez powtarzania polecenia obsługi.

## Wyczyść magazyn składników

Wybierz **Czyszczenie magazynu składników** w sekcji **Aktualizacje**. Najpierw pyta ono system Windows, czy czyszczenie jest zalecane. Opcjonalna konserwacja usuwa zastąpione składniki i ponownie wykonuje analizę. Nigdy nie używa ResetBase i nigdy nie usuwa ręcznie zawartości WinSxS.

## Z wiersza polecenia

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

Zobacz [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) i [servicing-logs](cli.html#cmd-servicing-logs). `repair` domyślnie pokazuje podgląd i działa tylko z `--execute`.

## Powiązane

- [Napraw Windows Update](fix-windows-update.html)
- [Sekcja Naprawa](repair.html#system-files)
- [Jak zmiany pozostają bezpieczne](safety.html)
