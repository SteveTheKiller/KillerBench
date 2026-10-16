# Zwolnij miejsce na dysku

Użyj tego przewodnika, gdy na dysku zaczyna brakować miejsca. KillerBench pokazuje, na co zużywane jest miejsce, wyświetla podgląd tego, co usunąłby każdy cel czyszczenia, i usuwa pliki dopiero po włączeniu trwałego czyszczenia.

## Sprawdź, ile miejsca zostało

1. Otwórz **Magazyn** i wybierz **Magazyn**.
2. Naciśnij **Uruchom diagnozę**. Raport oznacza każdy wolumin z literą dysku, który ma mniej niż 10% wolnego miejsca, oraz pokazuje przydział kopii w tle, domyślne przydziały i zasady Czujnika miejsca do magazynowania.
3. Aby sprawdzić zrzuty awaryjne i starą zawartość aktualizacji, uruchom też **Parametry życiowe** w sekcji **Diagnostyka** i **Windows Update** w sekcji **Aktualizacje**; tam widać rozmiar pamięci podręcznej aktualizacji.

## Podgląd czyszczenia

1. Otwórz **Konserwacja** i wybierz **Czyszczenie**.
2. Zaznacz wybrane cele, na przykład **Pliki tymczasowe bieżącego użytkownika**, **Pliki tymczasowe Windows**, **Pamięci podręczne przeglądarek bieżącego użytkownika**, **Pamięć podręczna Delivery Optimization** lub **Kosz bieżącego użytkownika**. Najedź kursorem na cel, aby zobaczyć dokładnie, jakie foldery obejmuje.
3. Ustaw **Minimalny wiek (dni)** i **Maksymalna liczba plików na cel**.
4. Naciśnij **Uruchom diagnozę** przy wyłączonej opcji **Wykonaj wybrane trwałe czyszczenie po świeżych kontrolach**. Raport wymienia kandydatów i szacowane miejsce dla każdego celu. Nic nie jest usuwane.

## Usuń pliki

1. Włącz **Wykonaj wybrane trwałe czyszczenie po świeżych kontrolach** i naciśnij **Uruchom diagnozę**.
2. KillerBench ponownie sprawdza każdego kandydata przed usunięciem, pomija pliki nowe, chronione lub używane i zapisuje każdy wynik w dzienniku.

Trwałe czyszczenie omija Kosz i nie można go cofnąć. Usunięcie raportów błędów i zrzutów awaryjnych odrzuca dowody, które mogą być potrzebne do badania awarii, więc najpierw je przejrzyj.

## Usuń poprzednią instalację systemu Windows

Po aktualizacji funkcji **Poprzednia instalacja Windows** w narzędziu **Czyszczenie** może usunąć kopię do wycofania za pomocą obsługiwanego żądania systemu Windows. Działa samodzielnie, wymaga zaznaczenia **Utworzono kopię zapasową wszystkich plików i ustawień potrzebnych z poprzedniej instalacji** i trwale usuwa możliwość powrotu do wcześniejszej wersji.

## Z wiersza polecenia

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

Zobacz [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) i [previous-windows](cli.html#cmd-previous-windows).

## Powiązane

- [Sekcja Konserwacja](maintenance.html#cleanup)
- [Sekcja Magazyn](storage.html)
- [Sprawdź i napraw dysk](check-a-drive.html)
