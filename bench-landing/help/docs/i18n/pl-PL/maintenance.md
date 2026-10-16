# Konserwacja

Konserwacja utrzymuje porządek na komputerze: czyszczenie plików, stare profile użytkowników, zaplanowane zadania i porządkowanie systemu Windows. Każde narzędzie przed usunięciem czegokolwiek pokazuje podgląd tego, co by usunęło.

Przewodniki krok po kroku: [Zwolnij miejsce na dysku](free-disk-space.html) i [Zwiększ prywatność systemu Windows](make-windows-private.html).

## Narzędzia w tej sekcji

Czyść pliki, profile i zaplanowane zadania oraz porządkuj system Windows.

### Czyszczenie

Szacuje wybrane cele: pliki tymczasowe, pamięci podręczne i zachowane dowody diagnostyczne, używając ustawień wieku i limitu plików z panelu głównego. Windows Temp i Windows SystemTemp to niezależne cele. Delivery Optimization to osobny wybór pamięci podręcznej, a usuniętą zawartość może być trzeba pobrać ponownie. Opcjonalne trwałe usuwanie korzysta z osobnego pola wyboru czyszczenia, ponownie sprawdza zachowanych kandydatów w ramach wybranego limitu i zapisuje każdy wynik. Usuwanie omija Kosz. Cele, wiek i limity plików można też skonfigurować za pomocą polecenia cleanup w CLI.

### Profile

Przegląda profile systemu Windows według wybranego progu wieku, dokładnych rejestracji ProfileList oraz opcjonalnych zależności kont usług i zaplanowanych zadań, a następnie zwraca jeden ograniczony wniosek z przeglądu. Profile wczytane, systemowe i profil bieżącego konta są wykluczone z przeglądu. Kandydaci pozostają wyłącznie do przeglądu. Żaden profil, folder, klucz rejestru, konto, usługa, zadanie, poświadczenie, zasada ani plik nie jest zmieniany i nie jest wybierane żadne usunięcie. Zależności aplikacji i możliwość odtworzenia kopii zapasowych pozostają niezweryfikowane.

### Zaplanowane zadania

Inwentaryzuje widoczne zaplanowane zadania, ich stany i zgłoszone podmioty zabezpieczeń. Żadne zadanie nie jest uruchamiane, rejestrowane ani zmieniane. Akcje, wyzwalacze i powodzenie wykonania nie są weryfikowane.

### Porządkowanie systemu Windows

Przegląda jawne obszary porządkowania systemu Windows, takie jak Recall i AI, telemetria, sugestie wyszukiwania, treści konsumenckie, pasek zadań i menu Start, powiadomienia, Edge, zasilanie, hibernacja, języki pakietu Office i preinstalowane oprogramowanie, dla bieżącego konta lub wszystkich profili. Przegląd jest tylko do odczytu. Plan zasilania Wysoka wydajność, limity czasu uśpienia i ekranu oraz hibernację można zmienić pojedynczo, gdy dany obszar jest jedynym wybranym, przy świeżych dowodach i jawnym potwierdzeniu.
