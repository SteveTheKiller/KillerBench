# BitLocker

BitLocker sprawdza szyfrowanie dysków, funkcje ochrony i informacje odzyskiwania, a następnie pomaga utworzyć kopię zapasową, zabezpieczyć lub zmienić dysk. Działa tylko na tym komputerze.

Przewodniki krok po kroku: [Zaszyfruj dysk za pomocą funkcji BitLocker](encrypt-with-bitlocker.html).

## Korzystanie z narzędzia BitLocker

Wybierz literę dysku i opcję w polu **Operacja**, a następnie naciśnij **Uruchom diagnozę**.

- **Sprawdź** odczytuje stan szyfrowania, stan ochrony i tożsamości funkcji ochrony. Jest to wartość domyślna i niczego nie zmienia.
- **Kopia odzyskiwania** zapisuje wybrane hasło odzyskiwania do nowego pliku poza chronionym dyskiem i je weryfikuje. Najpierw naciśnij **Znajdź funkcje ochrony odzyskiwania**, wybierz funkcję ochrony, a następnie potwierdź kopię.
- **Zaszyfruj** tworzy kopię wybranego hasła odzyskiwania, weryfikuje ją, a następnie jeden raz rozpoczyna szyfrowanie wybraną metodą. Dysk systemu Windows wymaga funkcji ochrony TPM i hasła odzyskiwania. Dysk z danymi wymaga tylko hasła odzyskiwania.
- **Wznów ochronę** ponownie włącza ochronę w pełni zaszyfrowanego dysku, na którym została wstrzymana.

Każda z tych operacji wymaga osobnego pola potwierdzenia, a KillerBench ponownie sprawdza dysk tuż przed działaniem.

## Więcej akcji funkcji BitLocker

Otwórz **Więcej akcji funkcji BitLocker**, aby wykonać zmiany wykraczające poza **Uruchom diagnozę**. Wybierz akcję, naciśnij **Podgląd**, aby przeczytać dokładne wymagania, potwierdź i naciśnij **Uruchom akcję**.

- **Włącz automatyczne odblokowywanie** sprawia, że zaszyfrowany dysk z danymi odblokowuje się przy uruchamianiu systemu Windows. Dysk systemu Windows musi być w pełni zaszyfrowany i mieć włączoną ochronę.
- **Dodaj funkcję ochrony TPM** lub **Dodaj hasło odzyskiwania** tworzy jedną nową funkcję ochrony. KillerBench nigdy nie wyświetla ani nie zapisuje w dzienniku nowego hasła odzyskiwania, więc od razu utwórz jego kopię.
- **Zapisz hasło odzyskiwania w usłudze Active Directory** lub **Microsoft Entra ID** wysyła funkcję ochrony wybraną powyżej. KillerBench zgłasza, że system Windows przyjął żądanie. Nie może odczytać kopii w katalogu, więc potwierdź ją tam.
- **Odszyfruj ten dysk** rozpoczyna odszyfrowywanie, na przykład przed ponownym szyfrowaniem wymaganą metodą. Wymaga drugiego potwierdzenia, ponieważ dysk pozostaje potem bez ochrony, i odmawia działania na dysku systemu Windows, dopóki inne dyski odblokowują się z niego automatycznie.

## TPM i FIPS

Jeśli moduł TPM jest obecny, ale nie jest gotowy, sekcja Zabezpieczenia może go przygotować bez czyszczenia. KillerBench nigdy nie czyści modułu TPM, ponieważ niszczy to klucze przechowywane przez BitLocker i Windows Hello. Przełącznik zasad algorytmów FIPS jest dostępny jako jawna, odwracalna naprawa dla organizacji, które potrzebują numerów PIN funkcji BitLocker zatwierdzonych przez FIPS.

## Narzędzia w tej sekcji

Sprawdzaj szyfrowanie, funkcje ochrony i informacje odzyskiwania, a następnie twórz ich kopie lub zabezpieczaj dysk.

### BitLocker

Pokazuje podgląd opcji funkcji BitLocker i sprawdza wybrany wolumin. Ten proces nie wykonuje szyfrowania ani kopii odzyskiwania.
