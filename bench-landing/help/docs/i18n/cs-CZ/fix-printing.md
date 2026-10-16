# Řešení problémů s tiskem

Tento postup použijte, když úlohy zůstávají ve frontě, tiskárna se zobrazuje jako offline nebo je jako výchozí nastavena nesprávná tiskárna. KillerBench nejprve přečte zařazování tisku, fronty, úlohy, ovladače a porty a potom vám umožní opravit jednu přesnou frontu nebo úlohu.

## Kontrola tiskáren

1. Otevřete **Zařízení** a zvolte **Tisk**.
2. Stiskněte **Najít místní tiskárny** a vyberte frontu, která nefunguje správně. Pro kontrolu všech front zvolte **Použít obecné kontroly tisku**.
3. Nechte **Automaticky opravit potvrzené problémy** vypnuté a stiskněte **Spustit diagnostiku**.
4. Přečtěte si zprávu. Zobrazuje stav služby Zařazování tisku, pozastavené fronty, režim offline, viditelné úlohy, stavy ovladačů, registrace portů, nedávné chyby PrintService a u síťových tiskáren jeden pokus o připojení k přiřazené adrese tiskárny.

Úspěšné připojení ani čistý stavový kód neprokazují, že tiskárna má papír, toner nebo funkční mechaniku.

## Oprava fronty

1. Spusťte **Tisk** znovu se zapnutou možností **Automaticky opravit potvrzené problémy**. Může spustit povolenou službu zařazování, obnovit přesně určenou pozastavenou frontu, ve které čekají úlohy, a zrušit režim offline.
2. Zrušení režimu offline může uvolnit čekající úlohy, proto se ujistěte, že se mají stále vytisknout.
3. Fronta, kterou někdo záměrně pozastavil a ve které nic nečeká, zůstane pozastavená.

## Odstranění jedné zaseknuté úlohy

1. Stiskněte **Najít zablokované tiskové úlohy**.
2. Vyberte úlohu ve stavu Chyba nebo Blokováno a zaškrtněte **Zrušit pouze tuto vybranou úlohu**.
3. Stiskněte **Spustit diagnostiku**. Zruší se pouze tato úloha a výsledek se znovu přečte.

## Oprava výchozí tiskárny

Zvolte frontu, která má být výchozí, zaškrtněte **Opravit chybějící nebo konfliktní výchozí tiskárnu pomocí této fronty** a potom stiskněte **Spustit diagnostiku**. Akce proběhne jen tehdy, když výchozí tiskárna chybí nebo je v konfliktu.

## Z příkazového řádku

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

Viz [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) a [default-printer-set](cli.html#cmd-default-printer-set).

## Související

- [Sekce Zařízení](devices.html#printing)
- [Řešení problémů se sítí a internetem](fix-network.html)
