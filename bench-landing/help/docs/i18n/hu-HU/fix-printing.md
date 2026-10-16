# Nyomtatási problémák megoldása

Akkor használja, ha a feladatok a sorban ragadnak, egy nyomtató offline állapotot mutat, vagy rossz nyomtató az alapértelmezett. A KillerBench először beolvassa a nyomtatásisor-kezelőt, a sorokat, a feladatokat, az illesztőprogramokat és a portokat, majd lehetővé teszi egyetlen pontos sor vagy feladat javítását.

## A nyomtatók ellenőrzése

1. Nyissa meg a **Hardver** szakaszt, és válassza a **Nyomtatás** eszközt.
2. Nyomja meg a **Helyi nyomtatók keresése** gombot, és válassza ki a hibásan működő sort. Az összes sor ellenőrzéséhez válassza az **Általános nyomtatási ellenőrzések használata** lehetőséget.
3. Hagyja kikapcsolva a **Megerősített problémák automatikus javítása** beállítást, és nyomja meg a **Diagnózis futtatása** gombot.
4. Olvassa el a jelentést. Megmutatja a Nyomtatásisor-kezelő állapotát, a szüneteltetett sorokat, az offline módot, a látható feladatokat, az illesztőprogramok állapotát, a portregisztrációkat, a legutóbbi PrintService-hibákat, hálózati nyomtatók esetén pedig egy kapcsolódási kísérletet a nyomtató hozzárendelt címéhez.

Egy sikeres kapcsolat vagy egy tiszta állapotkód nem igazolja, hogy a nyomtatóban van papír, festék vagy működő mechanika.

## A sor javítása

1. Futtassa újra a **Nyomtatás** eszközt bekapcsolt **Megerősített problémák automatikus javítása** beállítással. Elindíthatja az engedélyezett nyomtatásisor-kezelőt, folytathat egy pontosan meghatározott szüneteltetett sort, amelyben feladatok várakoznak, és kikapcsolhatja az offline módot.
2. Az offline mód kikapcsolása felszabadíthatja a várakozó feladatokat, ezért győződjön meg arról, hogy ezeket még ki kell nyomtatni.
3. Az a sor, amelyet valaki szándékosan szüneteltetett, és nincs benne várakozó feladat, szüneteltetve marad.

## Egy elakadt feladat törlése

1. Nyomja meg az **Elakadt nyomtatási feladatok keresése** gombot.
2. Válassza ki a Hiba vagy Letiltva állapotú feladatot, és jelölje be a **Csak ennek a kiválasztott feladatnak a megszakítása** beállítást.
3. Nyomja meg a **Diagnózis futtatása** gombot. Csak ez a feladat szakad meg, és az eredmény visszaolvasásra kerül.

## Az alapértelmezett nyomtató javítása

Válassza ki azt a sort, amelynek alapértelmezettnek kell lennie, jelölje be a **Hiányzó vagy ütköző alapértelmezett javítása ezzel a sorral** beállítást, majd nyomja meg a **Diagnózis futtatása** gombot. Ez csak akkor hajt végre műveletet, ha az alapértelmezett nyomtató hiányzik vagy ütközik.

## Parancssorból

```powershell
killerbench-cli printer-list
killerbench-cli printer-check --queue "Office Laser"
killerbench-cli printer-repair --queue "Office Laser" --execute
killerbench-cli print-job-list
killerbench-cli print-job-cancel "Office Laser" 12 --execute
```

Lásd: [printer-list](cli.html#cmd-printer-list), [printer-check](cli.html#cmd-printer-check), [printer-repair](cli.html#cmd-printer-repair), [print-job-cancel](cli.html#cmd-print-job-cancel) és [default-printer-set](cli.html#cmd-default-printer-set).

## Kapcsolódó

- [Hardver szakasz](devices.html#printing)
- [Hálózati és internetes problémák megoldása](fix-network.html)
