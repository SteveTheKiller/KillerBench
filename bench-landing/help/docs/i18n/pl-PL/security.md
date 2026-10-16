# Zabezpieczenia

Zabezpieczenia sprawdza program Microsoft Defender, zaporę, certyfikaty i usługi logowania. Udostępnia też stan modułu TPM i zabezpieczeń platformy, od których zależą BitLocker i Windows Hello.

Przewodniki krok po kroku: [Zaszyfruj dysk za pomocą funkcji BitLocker](encrypt-with-bitlocker.html) i [Zwiększ prywatność systemu Windows](make-windows-private.html).

## Narzędzia w tej sekcji

Sprawdzaj ochronę, zaporę, certyfikaty i usługi logowania.

### Microsoft Defender

Sprawdza stan ochrony i sygnatur programu Defender. Automatyczna naprawa odświeża sygnatury tylko wtedy, gdy Defender zgłasza aktywną ochronę i nieaktualne sygnatury. Korzysta ze skonfigurowanych źródeł sygnatur. Bez zmian ochrony, wykluczeń ani zasad; bez skanowania w poszukiwaniu złośliwego oprogramowania.

### Centrum zabezpieczeń systemu Windows

W ograniczonym zakresie przegląda rejestracje programów antywirusowych z surowym stanem dostawcy, dokładną historię awarii usługi i bieżącą gotowość usługi Centrum zabezpieczeń systemu Windows, bez interpretowania stanu ochrony. Pusty inwentarz nie jest werdyktem o stanie ochrony, a liczba rejestracji, surowy stan ani historyczne awarie nie mogą uprawniać do odzyskiwania ani zmiany zabezpieczeń. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące monitorowanie stanu zabezpieczeń i powiadomienia mogą zostać wznowione. Konfiguracja programu antywirusowego, rejestracje dostawców, ustawienia ochrony, wykluczenia, powiadomienia, zasady i typ uruchomienia są zachowywane. Nie jest żądane żadne skanowanie, aktualizacja ani zmiana ochrony. Weryfikacja obejmuje tylko stabilny stan usługi, a nie obecność programu antywirusowego, stan ochrony, aktualność sygnatur, dostarczanie powiadomień, własność rejestracji ani obowiązujące zasady.

### Usługi zapory

Odczytuje ustawienia włączenia profili Domena, Prywatny i Publiczny z ActiveStore, a następnie sprawdza usługi Aparat filtrowania podstawowego i Zapora Windows Defender. Opcjonalne odzyskiwanie uruchamia tylko włączone i zatrzymane usługi po świeżych kontrolach zależności. Istniejące zasady filtrowania mogą zacząć obowiązywać i przerwać dostęp sieciowy lub zdalny. Typy uruchomienia, uprawnienia usług, reguły zapory, profile i ustawienia IPsec są zachowywane. Bez resetowania, zmian reguł i zatrzymywania usług. Weryfikacja potwierdza tylko stan działania usługi; rzeczywista ochrona i łączność pozostają niezweryfikowane.

### Wygasanie certyfikatów

Sprawdza daty certyfikatów i buduje lokalne łańcuchy zaufania wyłącznie z pamięci podręcznej dla osobistych magazynów komputera i bieżącego użytkownika, a następnie zwraca jeden ograniczony wniosek operacyjny. Odwołanie, prezentacja na serwerze, powiązania, rejestracja, własność zasad, zdalne zaufanie i przydatność klucza prywatnego pozostają niezweryfikowane. Żaden certyfikat, ustawienie zaufania, rejestracja, powiązanie, klucz prywatny, aplikacja, usługa ani zasada nie jest zmieniany.

### Menedżer poświadczeń

Sprawdza dostępność usługi Menedżer poświadczeń. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące aplikacje mogą wznowić żądania przechowywanych poświadczeń. Zawartość magazynu, hasła, klucze dostępu, certyfikaty, tokeny, rejestracje poświadczeń, uprawnienia, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. Nie jest żądana żadna zawartość ani operacja na poświadczeniach. Weryfikacja obejmuje tylko stabilny stan usługi, a nie pobieranie poświadczeń, uwierzytelnianie ani dostęp aplikacji.

### Menedżer kont sieci Web

Przegląda historię brokera kont z zanonimizowanym ładunkiem i dokładną dostępność usługi Menedżer kont sieci Web. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące aplikacje mogą wznowić żądania kont i tokenów zgodnie z bieżącymi zasadami. Zdarzenia historyczne nie mogą uprawniać do odzyskiwania ani określać bieżącego stanu kont. Konta, poświadczenia, tokeny, pliki cookie, rejestracje aplikacji, uprawnienia, stan logowania, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na kontach, poświadczeniach, tokenach, plikach cookie, uprawnieniach ani logowaniu. Weryfikacja obejmuje tylko stabilny stan usługi, a nie uwierzytelnianie, wydawanie tokenów ani dostęp aplikacji.

### Asystent logowania do konta Microsoft

Przegląda historię brokera kont z zanonimizowanym ładunkiem i dokładną dostępność usługi Asystent logowania do konta Microsoft. Opcjonalne odzyskiwanie uruchamia tylko dokładnie tę włączoną i zatrzymaną usługę po świeżych kontrolach zależności. Istniejące żądania logowania do konta Microsoft mogą zostać wznowione zgodnie z bieżącymi zasadami. Zdarzenia historyczne nie mogą uprawniać do odzyskiwania ani określać bieżącego stanu kont. Konta, poświadczenia, tokeny, rejestracja urządzenia, dane aplikacji, uprawnienia, stan logowania, zasady, uprawnienia usługi i typ uruchomienia są zachowywane. KillerBench nie wybiera żadnej operacji na kontach, poświadczeniach, tokenach, rejestracji, uprawnieniach ani logowaniu. Weryfikacja obejmuje tylko stabilny stan usługi, a nie uwierzytelnianie, łączność konta ani dostęp aplikacji.
