# Řešení potíží

## Akce hlásí, že je zablokovaná

Zablokovaný výsledek znamená, že požadovaná akce nemohla pokračovat. Ze zprávy a deníku zjistěte, zda se zastavila před odesláním, nebo až po některém dřívějším kroku. Nepředpokládejte, že částečně dokončená operace byla vrácena zpět. Nejčastější důvody související s předpoklady jsou uvedeny níže.

- **Jsou vyžadována oprávnění správce.** Zavřete KillerBench a spusťte jej znovu pomocí **Spustit jako správce**.
- **Stav restartu musí být čistý.** Windows čeká na restart. Restartujte počítač a potom akci spusťte znovu.
- **Jsou přítomné spravované zásady.** Nastavení vlastní zásady skupiny nebo správa zařízení. Změňte jej tam.
- **Důkazy se změnily.** Mezi první kontrolou a závěrečnou kontrolou se něco změnilo. Spusťte nástroj znovu, aby přečetl nový stav.

## Předchozí požadavek vyžaduje sladění

KillerBench našel čekající záznam z přerušeného požadavku. Spusťte stejnou akci znovu. Zkontroluje, co se stalo, a ohlásí skutečný výsledek, aniž by požadavek opakovala. Na příkazovém řádku použijte odpovídající příkaz `-recover`, například `killerbench-cli bitlocker-auto-unlock-recover`.

## Kontrola ukazuje Neúplné nebo Neznámé

Důkazy nebylo možné přečíst, často proto, že KillerBench neběžel jako správce nebo nebyla dostupná některá služba Windows. KillerBench to poctivě ohlásí, místo aby předpokládal, že je počítač v pořádku.

## Kde jsou soubory uloženy

Deníky, čekající záznamy a uložené důkazy jsou v `%LOCALAPPDATA%\KillerBench`. Každá chráněná akce má vlastní složku. Ponechte si je, když potřebujete záznam o tom, co se změnilo.

## Hlášení problému

KillerBench je ve vývoji. Při hlášení problému připojte exportovanou zprávu a protokol činnosti a odstraňte vše, co byste nesdíleli.
