# Zaszyfruj dysk za pomocą funkcji BitLocker

Użyj tego przewodnika, aby włączyć funkcję BitLocker, utworzyć kopię klucza odzyskiwania i potwierdzić, że dysk jest chroniony. KillerBench tworzy kopię hasła odzyskiwania i weryfikuje ją przed rozpoczęciem szyfrowania, a nigdy nie wyświetla ani nie zapisuje hasła odzyskiwania w dzienniku.

## Zanim zaczniesz

- Uruchom KillerBench jako administrator.
- Dysk systemu Windows wymaga funkcji ochrony TPM i funkcji ochrony z hasłem odzyskiwania. Dysk z danymi wymaga tylko hasła odzyskiwania.
- Zdecyduj, gdzie trafi kopia klucza odzyskiwania: do pliku poza szyfrowanym dyskiem i opcjonalnie do Active Directory lub Microsoft Entra ID.

## Sprawdź dysk

1. Otwórz **BitLocker** i wybierz dysk w polu **Wolumin**.
2. Pozostaw operację **Sprawdź** i naciśnij **Uruchom diagnozę**. Raport pokazuje stan szyfrowania, stan ochrony i tożsamości funkcji ochrony.
3. Jeśli moduł TPM jest obecny, ale nie jest gotowy, najpierw przygotuj go w sekcji **Zabezpieczenia**. KillerBench nigdy nie czyści modułu TPM.

## Dodaj potrzebne funkcje ochrony

1. Otwórz **Więcej akcji funkcji BitLocker**.
2. Wybierz **Dodaj funkcję ochrony TPM** lub **Dodaj hasło odzyskiwania**, naciśnij **Podgląd**, zaznacz **Wprowadź tę zmianę na wybranym dysku po naciśnięciu Uruchom akcję**, a następnie naciśnij **Uruchom akcję**.
3. Od razu utwórz kopię nowego hasła odzyskiwania. KillerBench go nie wyświetla.

## Utwórz kopię klucza odzyskiwania i zaszyfruj

1. Naciśnij **Znajdź funkcje ochrony odzyskiwania** i wybierz funkcję ochrony z hasłem odzyskiwania.
2. Wpisz pełną ścieżkę pliku kopii zapasowej poza szyfrowanym dyskiem.
3. Wybierz **Zaszyfruj**, wybierz metodę szyfrowania i zaznacz **Utwórz kopię wybranego klucza odzyskiwania, a następnie rozpocznij szyfrowanie, gdy wybiorę Uruchom diagnozę**.
4. Naciśnij **Uruchom diagnozę**. KillerBench zapisuje kopię, weryfikuje ją, a następnie jeden raz rozpoczyna szyfrowanie.
5. Szyfrowanie trwa w tle. Później uruchom **Sprawdź**, aby potwierdzić, że dysk jest w pełni zaszyfrowany i ma włączoną ochronę.

## Kopia w Active Directory lub Entra ID

W **Więcej akcji funkcji BitLocker** wybierz kopię w katalogu dla wybranej funkcji ochrony. KillerBench zgłasza, że system Windows przyjął żądanie, ale nie może odczytać kopii w katalogu, więc potwierdź ją w Active Directory lub w centrum administracyjnym Entra.

## Dyski z danymi i automatyczne odblokowywanie

Po zaszyfrowaniu dysku z danymi **Włącz automatyczne odblokowywanie** sprawia, że odblokowuje się on przy uruchamianiu systemu Windows. Dysk systemu Windows musi być już w pełni zaszyfrowany i mieć włączoną ochronę.

## Z wiersza polecenia

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

Zobacz [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) i [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Każde z nich ma odpowiadające polecenie `-recover` dla przerwanego żądania.

## Powiązane

- [Sekcja BitLocker](bitlocker.html)
- [Sekcja Zabezpieczenia](security.html)
- [Jak zmiany pozostają bezpieczne](safety.html)
