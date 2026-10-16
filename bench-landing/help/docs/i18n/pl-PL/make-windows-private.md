# Zwiększ prywatność systemu Windows

Użyj tego przewodnika, aby przejrzeć i zaostrzyć ustawienia prywatności na laptopie. KillerBench odczytuje bieżące ustawienia, pokazuje, które z nich należą już do zasad lub zarządzania urządzeniami, i zmienia tylko wybrane przez Ciebie ustawienia, po jednym naraz, z możliwością dokładnego cofnięcia.

## Przejrzyj bieżące ustawienia

1. Otwórz **Prywatność**.
2. Zaznacz obszary, które chcesz przejrzeć. Wszystkie obszary są początkowo niezaznaczone.
3. Naciśnij **Uruchom diagnozę**. Raport pokazuje bieżącą wartość dla tego komputera i konta, na którym działa KillerBench, oraz oznacza ustawienia należące do zasad.

## Zmień ustawienie

1. Wybierz ustawienie w polu **Ustawienie prywatności**, na przykład kamerę, mikrofon, lokalizację, dane diagnostyczne, reklamy, historię schowka, wyszukiwanie zawartości w chmurze lub wyróżnienia wyszukiwania.
2. Wybierz nową wartość i zaznacz **Zastosuj, gdy wybiorę Uruchom diagnozę**.
3. Naciśnij **Uruchom diagnozę**. KillerBench sprawdza, czy ustawienie nie należy do zasad, zapisuje bieżącą wartość, wprowadza tę jedną zmianę i odczytuje ją ponownie.

Ustawienia należące do zasad grupy lub zarządzania urządzeniami pozostają nietknięte. Zmieniaj je tam, gdzie są zarządzane.

## Cofnij zmianę

Każda zweryfikowana zmiana ma swój dziennik. Odpowiadające jej cofnięcie przywraca dokładną poprzednią wartość, również po kontroli zasad.

## Porządkowanie systemu Windows

W przypadku sugestii, treści konsumenckich, funkcji Recall i AI, nadmiaru elementów na pasku zadań i w menu Start, przeglądarki Edge i preinstalowanego oprogramowania otwórz **Konserwacja** i wybierz **Porządkowanie systemu Windows**. Przegląd jest tylko do odczytu, a nic nie jest zaznaczane automatycznie.

## Z wiersza polecenia

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

Zobacz [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) i [debloat-review](cli.html#cmd-debloat-review). Nieobsługiwany klucz wyświetla listę obsługiwanych wartości.

## Powiązane

- [Sekcja Prywatność](privacy.html)
- [Skonfiguruj nowy komputer](set-up-new-pc.html)
