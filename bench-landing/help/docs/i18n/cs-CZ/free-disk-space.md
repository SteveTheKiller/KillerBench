# Uvolnění místa na disku

Tento postup použijte, když na jednotce dochází místo. KillerBench ukáže, kam místo mizí, zobrazí náhled toho, co by jednotlivé cíle úklidu odebraly, a maže teprve poté, co zapnete trvalé vyčištění.

## Kolik místa zbývá

1. Otevřete **Úložiště** a zvolte **Úložiště**.
2. Stiskněte **Spustit diagnostiku**. Zpráva označí každý svazek s písmenem jednotky, který má méně než 10 % volného místa, a zobrazí přidělení stínových kopií, výchozí kvóty a zásady Storage Sense.
3. Kvůli výpisům paměti a starému obsahu aktualizací spusťte také **Životní funkce** v části **Diagnostika** a **Windows Update** v části **Aktualizace**; tam se zobrazí velikost mezipaměti aktualizací.

## Náhled úklidu

1. Otevřete **Údržba** a zvolte **Úklid**.
2. Zaškrtněte požadované cíle, například **Dočasné soubory aktuálního uživatele**, **Dočasné soubory Windows**, **Mezipaměti prohlížečů aktuálního uživatele**, **Cache optimalizace doručování** nebo **Koš aktuálního uživatele**. Po najetí myší na cíl si přečtete, které složky přesně pokrývá.
3. Nastavte **Minimální stáří (dny)** a **Maximální počet souborů na cíl**.
4. Stiskněte **Spustit diagnostiku** s vypnutou možností **Provést vybrané trvalé vyčištění po nových kontrolách**. Zpráva uvede kandidáty a odhadované místo pro každý cíl. Nic se nesmaže.

## Odebrání souborů

1. Zapněte **Provést vybrané trvalé vyčištění po nových kontrolách** a stiskněte **Spustit diagnostiku**.
2. KillerBench před odebráním znovu zkontroluje každého kandidáta, přeskočí soubory, které jsou nové, chráněné nebo používané, a každý výsledek zapíše do deníku.

Trvalé vyčištění obchází Koš a nelze jej vrátit zpět. Odebráním zpráv o chybách a výpisů paměti zahodíte důkazy, které můžete potřebovat ke zkoumání pádů, proto je nejprve zkontrolujte.

## Odebrání předchozí instalace Windows

Po upgradu funkcí může **Předchozí instalace Windows** v části **Úklid** odebrat kopii pro vrácení zpět prostřednictvím podporovaného požadavku Windows na odebrání možnosti vrácení. Spouští se samostatně, vyžaduje **Zálohoval jsem všechny soubory a nastavení, která potřebuji z předchozí instalace** a trvale odebere možnost vrátit se k dřívější verzi.

## Z příkazového řádku

```powershell
killerbench-cli volume-capacity C:
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500
killerbench-cli cleanup --user-temp --windows-temp --days 7 --max-files 500 --execute
```

Viz [volume-capacity](cli.html#cmd-volume-capacity), [cleanup](cli.html#cmd-cleanup) a [previous-windows](cli.html#cmd-previous-windows).

## Související

- [Sekce Údržba](maintenance.html#cleanup)
- [Sekce Úložiště](storage.html)
- [Kontrola a oprava jednotky](check-a-drive.html)
