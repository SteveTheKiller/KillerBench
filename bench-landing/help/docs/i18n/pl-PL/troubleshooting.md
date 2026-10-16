# Rozwiązywanie problemów

## Akcja informuje, że jest zablokowana

Wynik zablokowany oznacza, że żądana akcja nie mogła być kontynuowana. Przeczytaj komunikat i dziennik, aby sprawdzić, czy zatrzymała się przed wysłaniem, czy po wcześniejszym kroku. Nie zakładaj, że częściowo ukończona operacja została wycofana. Najczęstsze przyczyny związane z wymaganiami podano poniżej.

- **Wymagany jest dostęp administratora.** Zamknij KillerBench i uruchom go ponownie za pomocą **Uruchom jako administrator**.
- **Nie może oczekiwać ponowne uruchomienie.** System Windows ma oczekujące ponowne uruchomienie. Uruchom ponownie komputer, a następnie wykonaj akcję jeszcze raz.
- **Obowiązują zarządzane zasady.** Ustawienie należy do zasad grupy lub zarządzania urządzeniami. Zmień je tam.
- **Dowody się zmieniły.** Coś zmieniło się między pierwszą a końcową kontrolą. Uruchom narzędzie ponownie, aby odczytać nowy stan.

## Wcześniejsze żądanie wymaga uzgodnienia

KillerBench znalazł wpis oczekujący po przerwanym żądaniu. Uruchom tę samą akcję ponownie. Sprawdzi, co się stało, i zgłosi rzeczywisty wynik bez powtarzania żądania. W wierszu polecenia użyj odpowiedniego polecenia `-recover`, na przykład `killerbench-cli bitlocker-auto-unlock-recover`.

## Kontrola pokazuje Niekompletne lub Nieznane

Nie można było odczytać dowodów, często dlatego, że KillerBench nie działał jako administrator albo usługa systemu Windows była niedostępna. KillerBench uczciwie to zgłasza, zamiast zakładać, że komputer jest w dobrym stanie.

## Gdzie są przechowywane pliki

Dzienniki, wpisy oczekujące i zapisane dowody znajdują się w `%LOCALAPPDATA%\KillerBench`. Każda chroniona akcja ma własny folder. Zachowaj je, gdy potrzebujesz zapisu tego, co się zmieniło.

## Zgłaszanie problemu

KillerBench jest w fazie rozwoju. Zgłaszając problem, dołącz wyeksportowany raport i dziennik aktywności oraz usuń wszystko, czego nie chcesz udostępniać.
