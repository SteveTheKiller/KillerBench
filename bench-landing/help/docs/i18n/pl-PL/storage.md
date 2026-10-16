# Magazyn

Sprawdza dyski, woluminy, Miejsca do magazynowania i usługi konserwacji dysków. Diagnoza nie wybiera żadnej operacji dyskowej; każda obsługiwana akcja zmieniająca komputer wymaga własnego dokładnego celu, wymagań wstępnych i potwierdzenia.

Przewodniki krok po kroku: [Sprawdź i napraw dysk](check-a-drive.html) i [Zwolnij miejsce na dysku](free-disk-space.html).

## Narzędzia w tej sekcji

### Magazyn

W ograniczonym zakresie mapuje tożsamości dysków, partycji i woluminów z literami dysków, ocenia presję przy 10% wolnego miejsca na każdym woluminie z literą dysku, sprawdza przydział magazynu kopii w tle VSS i domyślne przydziały dla każdego woluminu, podsumowuje bieżący postęp zadań modułu Storage, sprawdza wybrany wolumin NTFS i stan jego samonaprawy online oraz sprawdza urządzenia dyskowe i kontrolery tego komputera. Progi pojemności, ustawienia przydziałów i stany zadań to dowody do przeglądu; nie wybierają czyszczenia, zmiany rozmiaru, zmian przydziałów ani sterowania zadaniami. Mapowanie liter dysków dotyczy jednego momentu i nie obejmuje każdej ścieżki instalacji. Ustalenia dotyczące urządzeń nie są przypisywane do wybranego woluminu. Domyślnie wybrany jest wolumin systemu Windows. Żadne pliki, migawki, zadania ani sterowniki nie są zmieniane, żadne ustawienie nie jest zmieniane, żaden wolumin nie jest odinstalowywany i nie jest planowane ponowne uruchomienie.

### Miejsca do magazynowania

Inwentaryzuje pule Miejsc do magazynowania inne niż pierwotne oraz dyski wirtualne, w tym kondycję, stan operacyjny, pojemność, przydział i odporność, a następnie zwraca jeden ograniczony wniosek operacyjny. System Windows może na żądanie uruchomić dostawcę zarządzania Miejscami do magazynowania, aby odpowiedzieć na to zapytanie tylko do odczytu. Pule, dyski wirtualne, dyski fizyczne, woluminy, odporność, przydział, ustawienia konserwacji, zasady i typ uruchomienia są zachowywane. Nie jest żądana żadna operacja na pulach, dyskach, woluminach, przydziale, dołączaniu, naprawie, optymalizacji ani ustawieniach. Wyniki dostawcy nie weryfikują dostępności puli, nadmiarowości, integralności danych, możliwości odtworzenia kopii zapasowych ani operacji we/wy aplikacji.

### Usługa magazynu

W ograniczonym zakresie przegląda bieżące zadania modułu Storage, dokładną historię awarii usługi i bieżącą gotowość usługi Usługa magazynu. Niekompletne dowody mają pierwszeństwo. Stan zadań i historyczne awarie nie mogą uprawniać do odzyskiwania usługi ani zmian w magazynie. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące powiadomienia magazynu i aktywność magazynu zarządzana przez zasady mogą zostać wznowione. Woluminy, partycje, systemy plików, punkty instalacji, urządzenia, sterowniki, ustawienia Czujnika miejsca do magazynowania, zasady konserwacji, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na zadaniach, woluminach, partycjach, punktach instalacji, plikach, urządzeniach, sterownikach ani ustawieniach magazynu. Weryfikacja obejmuje tylko stabilny stan usługi, a nie wynik zadań ani operacje na urządzeniach, woluminach czy zasadach magazynu.

### Usługa Optymalizuj dyski

Sprawdza dostępność usługi Optymalizuj dyski. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejąca optymalizacja zaplanowana przez system Windows może zostać wznowiona zgodnie z bieżącymi zasadami konserwacji. Woluminy, systemy plików, dane, harmonogramy optymalizacji, zasady konserwacji, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera woluminu ani nie analizuje, nie przycina, nie defragmentuje, nie optymalizuje i nie planuje żadnego dysku. Weryfikacja obejmuje tylko stabilny stan usługi, a nie analizę, optymalizację ani kondycję urządzenia.
