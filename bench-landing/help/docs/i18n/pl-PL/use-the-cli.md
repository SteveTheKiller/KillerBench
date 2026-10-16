# Uruchamiaj KillerBench ze skryptów

Używaj wiersza polecenia, gdy potrzebujesz dowodów KillerBench w skrypcie, zadaniu RMM lub konsoli zdalnej. `killerbench-cli` uruchamia ten sam Engine co aplikacja komputerowa i wypisuje dane JSON.

## Pierwsze kroki

1. Otwórz PowerShell w folderze z `killerbench-cli.exe` albo dodaj ten folder do ścieżki.
2. Uruchom `killerbench-cli help`, aby wyświetlić wszystkie polecenia wraz z ich granicą bezpieczeństwa.
3. Uruchom `killerbench-cli diagnostic-scopes`, aby zobaczyć 11 zalecanych grup diagnostycznych.

## Uruchom skanowanie i zapisz raport

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` działa tylko do odczytu, chyba że dodasz `--auto-repair`. `--report` zapisuje ten sam raport z motywem, który eksportuje aplikacja. Użyj `--scope`, aby sprawdzić tylko wybrane obszary, oraz `common-check`, aby uzyskać łączny JSON bez dziennika procesu.

## Odczytaj kody wyjścia

| Kod | Znaczenie |
| --- | --- |
| 0 | Podgląd lub zweryfikowany wynik zakończył się powodzeniem. |
| 1 | Polecenie zatrzymał błąd uruchamiania, danych wejściowych lub dziennika. |
| 2 | Nie rozpoznano argumentów. |
| 3 | Zablokowane, nieudane lub niezweryfikowane. |
| 4 | Proces wymaga ponownego uruchomienia przed weryfikacją. |
| 130 | Zatrzymano przez użytkownika. |

Kod 0 dla polecenia tylko do odczytu oznacza, że polecenie zostało wykonane, a nie że komputer jest w dobrym stanie. Ustalenia znajdziesz w danych JSON.

## Wprowadź zmianę

Każde polecenie, które może zmienić komputer, domyślnie pokazuje podgląd. Przeczytaj go, a następnie uruchom to samo polecenie z `--execute`. Jeśli żądanie zostanie przerwane, uruchom odpowiednie polecenie `-recover`; zgłasza ono rzeczywisty wynik i nigdy nie powtarza zmiany.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Ponownie otwórz wcześniejsze wyniki

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## Używanie z asystentem AI

[KillerMCP](../mcp.html) może wywoływać zainstalowane CLI, aby wyszukiwać kody błędów Menedżera urządzeń i systemu Windows.

## Powiązane

- [Dokumentacja wiersza polecenia](cli.html)
- [Przewodnik Engine dla programistów](../engine/index.html)
- [Rozwiązywanie problemów](troubleshooting.html)
