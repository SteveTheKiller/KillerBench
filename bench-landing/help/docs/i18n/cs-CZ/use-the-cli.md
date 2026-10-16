# Spouštění KillerBench ze skriptů

Příkazový řádek použijte, když chcete důkazy KillerBench ve skriptu, v úloze RMM nebo ve vzdálené konzoli. `killerbench-cli` spouští stejný Engine jako desktopová aplikace a vypisuje JSON.

## Začínáme

1. Otevřete PowerShell ve složce, kde je `killerbench-cli.exe`, nebo tuto složku přidejte do cesty.
2. Spuštěním `killerbench-cli help` vypíšete všechny příkazy s jejich bezpečnostní hranicí.
3. Spuštěním `killerbench-cli diagnostic-scopes` zobrazíte 11 doporučených diagnostických skupin.

## Spuštění kontroly a uložení zprávy

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

`full-scan` je pouze pro čtení, pokud nepřidáte `--auto-repair`. `--report` zapíše stejnou zprávu s motivem, jakou exportuje aplikace. Pomocí `--scope` zkontrolujete jen některé oblasti a `common-check` vrátí kombinovaný JSON bez deníku postupu.

## Čtení návratových kódů

| Kód | Význam |
| --- | --- |
| 0 | Náhled nebo ověřený výsledek proběhl úspěšně. |
| 1 | Příkaz zastavila chyba spuštění, vstupu nebo deníku. |
| 2 | Argumenty nebyly rozpoznány. |
| 3 | Zablokováno, neúspěšné nebo neověřené. |
| 4 | Postup potřebuje před ověřením restart. |
| 130 | Zastavili jste jej. |

Kód 0 u příkazu pouze pro čtení znamená, že proběhl, nikoli že je počítač v pořádku. Nálezy si přečtěte v JSON.

## Provedení změny

Každý příkaz, který může změnit počítač, ve výchozím stavu zobrazí náhled. Přečtěte si náhled a potom spusťte stejný příkaz s `--execute`. Pokud se požadavek přeruší, spusťte odpovídající příkaz `-recover`; ohlásí skutečný výsledek a změnu nikdy neopakuje.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Opětovné otevření starších výsledků

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## Použití z asistenta AI

[KillerMCP](../mcp.html) umí volat nainstalované CLI a vyhledávat kódy Správce zařízení a kódy chyb Windows.

## Související

- [Referenční příručka příkazového řádku](cli.html)
- [Vývojářská příručka Engine](../engine/index.html)
- [Řešení potíží](troubleshooting.html)
