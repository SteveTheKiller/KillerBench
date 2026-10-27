# Nápověda KillerBench

KillerBench je pracovní nástroj pro diagnostiku a opravy Windows určený technikům. Nejprve počítač zkontroluje, vysvětlí, co našel, i s důkazy ke každému nálezu, a Windows mění jen tehdy, když vyberete konkrétní opravu a potvrdíte ji.

KillerBench je ve vývoji. Veřejné stažení zatím není k dispozici. Tyto stránky popisují aktuální sestavení.

## Co dělá

- Spouští diagnostiku pouze pro čtení napříč službou Windows Update, zařízeními, úložištěm, sítí, tiskem, zabezpečením, aplikacemi a profily a potom vytvoří čitelnou zprávu.
- Nabízí chráněné opravy, které se spustí jen tehdy, když čerstvé důkazy prokážou přesně daný problém, a výsledek potom ověří.
- Pokrývá nastavení a zabezpečení, například BitLocker, nastavení soukromí, pročištění Windows a zprovoznění nového počítače.
- Diagnostiku a analýzu zpráv provádí místně bez AI a bez účtu KillerBench. Vybrané akce, například stahování a zálohování klíče pro obnovení do Entra ID, mohou kontaktovat externí služby.
- Pro konzoli, zprávy, potvrzení a dialogy Otevřít, Uložit a Složka používá aktivní motiv aplikace.

## Kde začít

- [Stažení a nastavení](setup.html)
- [O KillerBench](about.html)

- [Vaše první diagnostika](first-run.html) vás provede úplnou kontrolou systému.
- [Pracovní prostor](workspace.html) vysvětluje sekce, panely a klávesové zkratky.
- [Jak změny zůstávají bezpečné](safety.html) vysvětluje, co KillerBench kontroluje před každou změnou a po ní.
- [Návody krok za krokem](fix-windows-update.html) vás provedou běžnými úkoly.
- Stránky **Sekce** popisují každý nástroj a jsou seskupené stejně jako v aplikaci.
- [Příkazový řádek](cli.html) uvádí všechny příkazy `killerbench-cli`.
- [Řešení potíží](troubleshooting.html) popisuje zablokované akce, restarty a přerušené požadavky.

Vývojáři, kteří chtějí diagnostiku začlenit do vlastních nástrojů, najdou podrobnosti v [Příručce pro vývojáře Engine](../engine/index.html).
