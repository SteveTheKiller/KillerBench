# Rozwiąż problemy z drukowaniem

Użyj tego przewodnika, gdy zadania utknęły w kolejce, drukarka jest wyświetlana jako offline albo domyślna jest niewłaściwa drukarka. KillerBench najpierw odczytuje bufor wydruku, kolejki, zadania, sterowniki i porty, a następnie pozwala naprawić jedną konkretną kolejkę lub jedno zadanie.

## Sprawdź drukarki

1. Otwórz **Urządzenia** i wybierz **Drukowanie**.
2. Naciśnij **Znajdź drukarki lokalne** i wybierz kolejkę, która działa nieprawidłowo. Aby sprawdzić wszystkie kolejki, wybierz **Użyj ogólnych kontroli drukowania**.
3. Pozostaw wyłączoną opcję **Automatycznie napraw potwierdzone problemy** i naciśnij **Uruchom diagnozę**.
4. Przeczytaj raport. Pokazuje on stan Bufora wydruku, wstrzymane kolejki, tryb offline, widoczne zadania, stan sterowników, rejestracje portów, ostatnie błędy PrintService, a w przypadku drukarek sieciowych jedną próbę połączenia z adresem przypisanym do drukarki.

Udane połączenie lub czysty kod stanu nie dowodzą, że drukarka ma papier, toner lub sprawny mechanizm.

## Napraw kolejkę

1. Uruchom ponownie **Drukowanie** z włączoną opcją **Automatycznie napraw potwierdzone problemy**. Może ona uruchomić włączony bufor wydruku, wznowić konkretną wstrzymaną kolejkę, w której czekają zadania, i wyłączyć tryb offline.
2. Wyłączenie trybu offline może zwolnić oczekujące zadania, więc upewnij się, że nadal powinny zostać wydrukowane.
3. Kolejka wstrzymana celowo, w której nic nie czeka, pozostaje wstrzymana.

## Usuń jedno zablokowane zadanie

1. Naciśnij **Znajdź zablokowane zadania drukowania**.
2. Wybierz zadanie ze stanem Błąd lub Zablokowane i zaznacz **Anuluj tylko to wybrane zadanie**.
3. Naciśnij **Uruchom diagnozę**. Anulowane jest tylko to zadanie, a wynik jest odczytywany ponownie.

## Napraw drukarkę domyślną

Wybierz kolejkę, która ma być domyślna, zaznacz **Napraw brakującą lub sprzeczną drukarkę domyślną, używając tej kolejki** i naciśnij **Uruchom diagnozę**. Działa to tylko wtedy, gdy drukarki domyślnej brakuje lub występuje konflikt.

## Z wiersza polecenia

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

Zobacz [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) i [default-printer-set](cli.html#cmd-default-printer-set).

## Powiązane

- [Sekcja Urządzenia](devices.html#printing)
- [Rozwiąż problemy z siecią i internetem](fix-network.html)
