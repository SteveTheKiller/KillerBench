# A KillerBench futtatása szkriptekből

A parancssort akkor használja, ha a KillerBench bizonyítékaira egy szkriptben, egy RMM-feladatban vagy egy távoli konzolon van szüksége. A `killerbench-cli` ugyanazt az Engine-t futtatja, mint az asztali alkalmazás, és JSON-t ír ki.

## Első lépések

1. Nyissa meg a PowerShellt abban a mappában, ahol a `killerbench-cli.exe` található, vagy adja hozzá a mappát az elérési úthoz.
2. A `killerbench-cli help` futtatásával az összes parancsot a biztonsági határaival együtt listázhatja.
3. A `killerbench-cli diagnostic-scopes` futtatásával megtekintheti a 11 ajánlott diagnosztikai csoportot.

## Vizsgálat futtatása és jelentés mentése

```powershell
killerbench-cli full-scan --report C:\Reports\scan.html --json
```

A `full-scan` kizárólag diagnosztikai, és elutasítja a `--auto-repair` kapcsolót. Ha konkrét műveletre van szükség, használjon egy célzott javítási folyamatot. A `--report` ugyanazt a témázott jelentést írja ki, amelyet az alkalmazás exportál. A `--scope` kapcsolóval csak egyes területeket ellenőrizhet, a `common-check` pedig folyamatnapló nélküli összesített JSON-t ad.

## Kilépési kódok értelmezése

| Kód | Jelentés |
| --- | --- |
| 0 | Az előnézet vagy az igazolt eredmény sikeres volt. |
| 1 | Indítási, bemeneti vagy naplóhiba állította le a parancsot. |
| 2 | Az argumentumok nem ismerhetők fel. |
| 3 | Blokkolva, sikertelen vagy nem igazolt. |
| 4 | Egy folyamatnak újraindításra van szüksége az ellenőrzés előtt. |
| 130 | Ön leállította. |

Csak olvasó parancsnál a 0 kilépési kód azt jelenti, hogy a parancs lefutott, nem azt, hogy a számítógép egészséges. A megállapításokat a JSON-ban olvassa el.

## Módosítás végrehajtása

Minden parancs, amely módosíthatja a gépet, alapértelmezés szerint előnézetet készít. Olvassa el az előnézetet, majd futtassa ugyanazt a parancsot a `--execute` kapcsolóval. Ha egy kérés megszakad, futtassa a megfelelő `-recover` parancsot; ez a valós eredményt jelenti, és soha nem ismétli meg a módosítást.

```powershell
killerbench-cli dhcp-renew
killerbench-cli dhcp-renew --execute
killerbench-cli dhcp-renew-recover
```

## Korábbi eredmények újbóli megnyitása

```powershell
killerbench-cli workflow-journal C:\Users\me\AppData\Local\KillerBench\journal.json
killerbench-cli workflow-report journal.json report.html --include-repairs
```

## Használat AI-asszisztensből

A [KillerMCP](../mcp.html) meghívhatja a telepített CLI-t az Eszközkezelő és a Windows hibakódjainak kikereséséhez.

## Kapcsolódó

- [Parancssori referencia](cli.html)
- [Engine fejlesztői útmutató](../engine/index.html)
- [Hibaelhárítás](troubleshooting.html)
