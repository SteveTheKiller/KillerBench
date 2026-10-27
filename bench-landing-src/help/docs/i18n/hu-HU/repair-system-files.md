# Rendszerfájlok javítása

Akkor használja, ha a Windows összetevői sérültek: a szolgáltatások nem települnek, a frissítések összetevőhibát jeleznek, vagy a rendszereszközök összeomlanak. A KillerBench csak akkor futtatja a DISM-et és az SFC-t, ha egy élő ellenőrzés sérülést igazol, majd ellenőrzi az eredményt.

## Mielőtt elkezdené

- Futtassa a KillerBenchet rendszergazdaként.
- Előbb rendezze a függő újraindítást.
- Felügyelt gép esetén ellenőrizze, hogy egy házirend nem irányítja-e a DISM-et egy adott javítási forrásra. A KillerBench jelzi ezt a házirendet, de nem módosítja.

## Sérülés keresése

1. Nyissa meg a **Javítás** szakaszt, és válassza a **Rendszerfájlok** eszközt.
2. Hagyja kikapcsolva a **Megerősített problémák automatikus javítása** beállítást, és nyomja meg a **Diagnózis futtatása** gombot.
3. Olvassa el a jelentést. Leltárba veszi a karbantartási csomagok, az opcionális szolgáltatások és a képességek állapotát, ellenőrzi az előfeltételeket, és megvizsgálja a védett rendszerfájlokat. Tiszta eredmény esetén nem kínál javítást.

## Igazolt sérülés javítása

1. Kapcsolja be a **Megerősített problémák automatikus javítása** beállítást, és nyomja meg a **Javítás futtatása** gombot. A KillerBench csak akkor választja a DISM-et vagy az SFC-t, ha az élő ellenőrzés sérülést igazol.
2. Várja meg az ellenőrzést. A jelentés csak akkor mutat **Igazolva** állapotot, ha a követő ellenőrzés javított állapotot olvas vissza.
3. Ha a jelentés szerint a tár nem állítható helyre, vagy a javítási forrás nem érhető el, álljon meg. A javítás megismétlése nem változtat az eredményen.

A megszakított DISM- vagy SFC-futtatás a következő rendszerindításkor egyeztetésre kerül, a karbantartási parancs megismétlése nélkül.

## Az összetevőtár tisztítása

A **Frissítések** szakaszban válassza az **Összetevőtár tisztítása** eszközt. Először megkérdezi a Windowst, hogy ajánlott-e a tisztítás. Az opcionális karbantartás eltávolítja a felülírt összetevőket, majd újra elemez. Soha nem használja a ResetBase-t, és soha nem töröl kézzel WinSxS-tartalmat.

## Parancssorból

```powershell
killerbench-cli component-repair-source
killerbench-cli repair dism
killerbench-cli repair sfc --execute
killerbench-cli servicing-logs
```

Lásd: [component-repair-source](cli.html#cmd-component-repair-source), [repair](cli.html#cmd-repair) és [servicing-logs](cli.html#cmd-servicing-logs). A `repair` alapértelmezés szerint előnézetet készít, és csak a `--execute` kapcsolóval hajt végre műveletet.

## Kapcsolódó

- [A Windows Update javítása](fix-windows-update.html)
- [Javítás szakasz](repair.html#system-files)
- [Hogyan maradnak biztonságosak a módosítások](safety.html)
