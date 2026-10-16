# Oprava systémových souborů

Tento postup použijte, když jsou poškozené součásti Windows: funkce se nedaří nainstalovat, aktualizace hlásí chyby komponent nebo systémové nástroje padají. KillerBench spustí DISM a SFC až poté, co živá kontrola potvrdí poškození, a potom výsledek ověří.

## Než začnete

- Spusťte KillerBench jako správce.
- Nejprve vyřešte případný čekající restart.
- Pokud je počítač spravovaný, zkontrolujte, zda zásady nesměrují DISM na konkrétní zdroj oprav. KillerBench tyto zásady ohlásí a nemění je.

## Kontrola poškození

1. Otevřete **Oprava** a zvolte **Systémové soubory**.
2. Nechte **Automaticky opravit potvrzené problémy** vypnuté a stiskněte **Spustit diagnostiku**.
3. Přečtěte si zprávu. Inventarizuje stavy balíčků údržby, volitelných funkcí a možností, kontroluje předpoklady a prověřuje chráněné systémové soubory. Čistý výsledek znamená, že žádná oprava nabídnuta není.

## Oprava potvrzeného poškození

1. Zapněte **Automaticky opravit potvrzené problémy** a stiskněte **Spustit opravu**. KillerBench zvolí DISM nebo SFC jen tehdy, když živá kontrola potvrdí poškození.
2. Počkejte na ověření. Zpráva zobrazí **Ověřeno** jen tehdy, když navazující kontrola přečte opravený stav.
3. Pokud zpráva uvádí, že úložiště nelze obnovit nebo že zdroj oprav není dostupný, zde skončete. Opakování opravy výsledek nezmění.

Přerušený běh DISM nebo SFC se při dalším spuštění systému sladí, aniž by se příkaz údržby opakoval.

## Vyčištění úložiště komponent

V části **Aktualizace** zvolte **Vyčištění úložiště komponent**. Nejprve se Windows zeptá, zda je vyčištění doporučeno. Volitelná údržba odebere nahrazené komponenty a provede analýzu znovu. Nikdy nepoužívá ResetBase a nikdy ručně nemaže obsah WinSxS.

## Z příkazového řádku

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

Viz [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) a [servicing-logs](cli.html#cmd-servicing-logs). `repair` ve výchozím stavu zobrazí náhled a jedná jen s `--execute`.

## Související

- [Oprava služby Windows Update](fix-windows-update.html)
- [Sekce Oprava](repair.html#system-files)
- [Jak změny zůstávají bezpečné](safety.html)
