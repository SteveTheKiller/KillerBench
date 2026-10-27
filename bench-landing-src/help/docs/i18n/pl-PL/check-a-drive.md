# Sprawdź i napraw dysk

Użyj tego przewodnika, gdy wolumin zgłasza błędy, system Windows prosi o skanowanie dysku albo pliki nie chcą się otworzyć. KillerBench najpierw sprawdza wolumin w trybie tylko do odczytu, a naprawę systemu plików planuje lub uruchamia dopiero po potwierdzeniu kopii zapasowej.

## Zanim zaczniesz

- Utwórz kopię zapasową ważnych danych z woluminu. Naprawa systemu plików może zmienić lub usunąć uszkodzone pliki.
- Uruchom KillerBench jako administrator.

## Sprawdź wolumin

1. Otwórz **Magazyn** i wybierz **Magazyn**.
2. Wybierz dysk w polu **Wolumin do sprawdzenia** i naciśnij **Uruchom diagnozę**.
3. Przeczytaj raport. Pokazuje on stan dysku i woluminu, bit zabrudzenia, samonaprawę NTFS w trybie online, błędy kontrolera i urządzeń dyskowych, liczniki niezawodności oraz ostatnie zdarzenia magazynu, takie jak resetowania, ponowienia i nieoczekiwane odłączenia.

Czysta kontrola oznacza, że żadna naprawa nie jest oferowana. Temperatura dysku, zużycie i liczniki niepoprawionych błędów we/wy są dowodami do przeglądu i same nie uruchamiają naprawy.

## Napraw system plików

1. Naciśnij **Przejrzyj naprawę systemu plików** i wybierz **Naprawa punktowa** lub **Skanowanie i naprawa offline**.
2. W przypadku woluminu z danymi zaznacz **Zezwól na odłączenie woluminu niesystemowego**, jeśli możesz zamknąć wszystko, co z niego korzysta. Woluminu systemu Windows nie można odłączyć, więc jego naprawa jest planowana na następne ponowne uruchomienie.
3. Zaznacz **Ważne dane na tym woluminie mają kopię zapasową**.
4. Potwierdź naprawę. KillerBench ponownie sprawdza dokładnie ten wolumin tuż przed działaniem i najpierw zapisuje wpis oczekujący.
5. W przypadku naprawy zaplanowanej uruchom ponownie komputer w dogodnym momencie. KillerBench weryfikuje wynik po ponownym uruchomieniu bez ponownego wykonywania naprawy.

## Optymalizacja lub retrim

To samo narzędzie może przeanalizować wolumin i uruchomić optymalizację zalecaną przez system Windows, **Retrim nośnika półprzewodnikowego** albo defragmentację dysku twardego. Każda z tych operacji to osobna, potwierdzana akcja na jednym, dokładnie wskazanym, sprawnym woluminie.

## Z wiersza polecenia

```powershell
killerbench-cli storage-check C
killerbench-cli storage-repair-plan D: OfflineScanAndFix --allow-dismount
killerbench-cli storage-repair D: OfflineScanAndFix --allow-dismount --backup-confirmed --execute
killerbench-cli storage-repair-recover
```

Zobacz [storage-check](cli.html#cmd-storage-check), [storage-repair-plan](cli.html#cmd-storage-repair-plan), [storage-repair](cli.html#cmd-storage-repair) i [volume-optimize](cli.html#cmd-volume-optimize).

## Powiązane

- [Sekcja Magazyn](storage.html)
- [Zwolnij miejsce na dysku](free-disk-space.html)
- [Rozwiązywanie problemów](troubleshooting.html)
