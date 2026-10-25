# Jak zmiany pozostają bezpieczne

KillerBench traktuje diagnozę i zmianę jako odrębne zadania. Diagnoza tylko odczytuje. Pełne skanowanie systemu nigdy nie wykonuje napraw. Wybierz konkretną naprawę lub akcję, przejrzyj jej opcje i wymagania, a następnie potwierdź proponowaną zmianę.

## Przed zmianą

1. **Świeże dowody.** Narzędzie ponownie odczytuje bieżący stan. Zapisany raport ani stare zdarzenie nigdy nie uprawniają do zmiany.
2. **Dokładne kwalifikowanie.** Zmiana jest oferowana tylko wtedy, gdy dowody odpowiadają dokładnie temu problemowi, który usuwa. Wszystko, co niekompletne, ją blokuje.
3. **Twoje potwierdzenie.** Przejrzyj wybraną akcję w oknie potwierdzenia. Niektóre akcje, takie jak odszyfrowanie dysku, wymagają drugiego potwierdzenia. Okno używa aktywnego motywu aplikacji; jego wygląd nie zmienia wymagań akcji.
4. **Wymagania wstępne.** Sprawdzane są uprawnienia administratora, stan ponownego uruchomienia oraz to, czy ustawienie należy do zasad lub zarządzania urządzeniami. Ustawienia należące do zasad pozostają nietknięte.
5. **Wpis oczekujący.** KillerBench zapisuje, co zamierza zrobić, zanim poprosi o to system Windows.
6. **Końcowa kontrola.** Wszystko jest sprawdzane ponownie tuż przed żądaniem, więc zmiana w międzyczasie je zatrzymuje.

## Po zmianie

- KillerBench odczytuje wynik i zgłasza **Zweryfikowano** tylko wtedy, gdy system Windows pokazuje oczekiwany stan.
- Jeśli system Windows zgłasza sukces, ale stan się nie zmienił, wynik o tym informuje i nic nie jest powtarzane.
- Każda zmiana jest zapisywana w dzienniku w `%LOCALAPPDATA%\KillerBench`.

## Jeśli coś zostanie przerwane

Obsługiwane akcje z dziennikiem zachowują wpis oczekujący, gdy żądanie mogło dotrzeć do systemu Windows, a weryfikacja się nie zakończyła. Aby sprawdzić ten wpis, użyj udokumentowanej ścieżki uzgadniania akcji lub odpowiadającego jej polecenia `-recover`. Uzgadnianie nie jest automatycznym wycofaniem. Zachowaj dziennik, gdy wynik jest niepewny, i nie powtarzaj zmiany ręcznie. Zachowanie w przypadku awarii, anulowania i późniejszego rozruchu nadal wymaga natywnej weryfikacji dla każdej akcji.

## Czego KillerBench nie zrobi

- Nie wyczyści modułu TPM, nie usunie protektorów, których nie utworzył, ani nie wyświetli haseł odzyskiwania.
- Nie zmieni ustawienia kontrolowanego przez zasady grupy lub zarządzanie urządzeniami.
- Nie uruchomi naprawy tylko dlatego, że historyczne zdarzenie lub zaimportowany raport wspomina o problemie.

Diagnostyka i analiza raportów działają lokalnie. Wybrana akcja może łączyć się z zewnętrznym miejscem docelowym, na przykład przy pobieraniu oprogramowania albo przy kopii klucza odzyskiwania w Active Directory lub Entra ID. Przed potwierdzeniem sprawdź cel. Zapisane przechwycenia i surowe dowody mogą zawierać tożsamość komputera i ścieżki źródłowe, więc przejrzyj je przed udostępnieniem.

## Stan rozwoju

Zakres napraw wciąż się poszerza. Udana kompilacja lub symulowana kontrola nie potwierdza natywnego działania naprawy.
