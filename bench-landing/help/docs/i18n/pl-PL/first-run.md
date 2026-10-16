# Twoja pierwsza diagnoza

Zacznij od strony [Pobieranie i konfiguracja](setup.html), a następnie uruchom Pełne skanowanie systemu. Zbiera ono dowody diagnostyczne i zapisuje lokalny dziennik; nie wykonuje napraw. Ustalenie opisuje sprawdzony zakres, a nie każdą możliwą usterkę komputera.

## Uruchom skanowanie

1. Otwórz KillerBench. Sekcja **Diagnostyka** otwiera się z wybranym narzędziem **Pełne skanowanie systemu**.
2. Przy pierwszym uruchomieniu pozostaw wyłączoną opcję **Automatycznie napraw potwierdzone problemy**.
3. Naciśnij **Uruchom diagnozę**. Postęp pojawia się w panelu **Surowe wyjście** po prawej stronie podczas każdej kontroli.
4. Po zakończeniu panel wyjścia przełącza się na **Czytelny raport**.

Zacznij bez podniesionych uprawnień. Niektóre dowody systemu Windows, takie jak protektory BitLocker i chronione usługi, wymagają uprawnień administratora. Na koncie standardowym te kontrole zgłaszają niedostępne dowody. Jeśli ich potrzebujesz, dokończ bieżące uruchomienie, zachowaj raport, a następnie celowo otwórz KillerBench ponownie za pomocą **Uruchom jako administrator**. Podniesienie uprawnień nie wybiera ani nie zatwierdza żadnej naprawy.

## Przeczytaj raport

Raport zaczyna się od liczników **Do przeglądu**, **Zweryfikowane naprawy**, **Niekompletne** i **Sprawdzone**, po których następuje zalecany następny krok.

- **Co wymaga uwagi** wymienia problemy potwierdzone dowodami, od najważniejszych.
- **Zebrane informacje** pokazuje, co zostało odczytane, w tym dowody niekompletne lub niedostępne.
- Zdarzenia historyczne są oznaczone jako historia. Błąd z zeszłego miesiąca nie jest traktowany jako bieżąca usterka.

Brak dowodów nigdy nie oznacza dobrego stanu. Jeśli kontrola nie mogła zostać wykonana, raport to podaje.

## Wybierz kontrole samodzielnie

Otwórz **Wybierz kontrole (zaawansowane)**, aby wybrać poszczególne kontrole zamiast pełnego skanowania. Każda sekcja aplikacji ma też wyspecjalizowane narzędzia opisane na stronach w części **Sekcje** tego przewodnika.

## Napraw to, co znaleziono

Gdy raport zaleca poprawkę, wskazuje sekcję i narzędzie do otwarcia. Przed pierwszą naprawą przeczytaj stronę [Jak zmiany pozostają bezpieczne](safety.html). Natywna weryfikacja wciąż trwa; udana diagnoza nie oznacza, że każda oferowana naprawa została przetestowana w Twojej kompilacji systemu Windows. Przed każdą zmianą miej kopie zapasowe i przejrzyj dokładny cel oraz skutki.
