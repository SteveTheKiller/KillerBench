# Skonfiguruj nowy komputer

Użyj tego przewodnika podczas przygotowywania nowego lub świeżo zresetowanego komputera. KillerBench sprawdza komputer, aplikacje, które planujesz zainstalować, i nazwę, którą chcesz mu nadać, a następnie wskazuje chronione narzędzia wykonujące każdą zmianę.

## Zacznij od punktu odniesienia

1. Uruchom **Pełne skanowanie systemu** w sekcji **Diagnostyka** i zachowaj raport jako zapis stanu początkowego komputera.
2. Zainstaluj oczekujące aktualizacje. Jeśli któraś się nie powiedzie, zobacz [Napraw Windows Update](fix-windows-update.html).

## Przejrzyj aplikacje i konfigurację

1. Otwórz **Wdrażanie** i wybierz **Wdrażanie**.
2. Wybierz aplikacje i grupy konfiguracji, które planujesz przygotować, i naciśnij **Uruchom diagnozę**. Przegląd porównuje je z tym, co jest już zainstalowane. KillerBench nie pobiera ani nie uruchamia instalatorów.
3. Przed uruchomieniem instalatora producenta zweryfikuj go z wiersza polecenia. KillerBench ufa mu tylko wtedy, gdy ma ważny podpis z dokładną nazwą organizacji tego producenta.

## Nadaj nazwę komputerowi

1. Wybierz **Nazwa komputera**, wpisz nazwę i naciśnij **Uruchom diagnozę**, aby porównać ją z bieżącą tożsamością i rolą w domenie.
2. Na komputerze w grupie roboczej zaznacz **Zmień nazwę tego komputera grupy roboczej po świeżych kontrolach**, aby zmienić nazwę. Do zakończenia potrzebne jest ponowne uruchomienie.

## Zabezpiecz i uporządkuj

- [Zaszyfruj dysk za pomocą funkcji BitLocker](encrypt-with-bitlocker.html) i utwórz kopię klucza odzyskiwania.
- [Zwiększ prywatność systemu Windows](make-windows-private.html) i przejrzyj **Porządkowanie systemu Windows** w sekcji **Konserwacja**.
- Gdy wszystko będzie gotowe, utwórz punkt przywracania za pomocą **Przygotuj jeden punkt przywracania**.

## Z wiersza polecenia

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
killerbench-cli workflow computer-name --name FRONTDESK-01
killerbench-cli restore-point "Baseline after setup" --execute
```

Zobacz [workflow](cli.html#cmd-workflow) i [restore-point](cli.html#cmd-restore-point).

## Powiązane

- [Sekcja Wdrażanie](onboarding.html)
- [Twoja pierwsza diagnoza](first-run.html)
