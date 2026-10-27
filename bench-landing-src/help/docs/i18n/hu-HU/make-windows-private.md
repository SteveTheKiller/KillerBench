# A Windows adatvédelmének erősítése

Ezzel áttekintheti és szigoríthatja egy laptop adatvédelmét. A KillerBench beolvassa az aktuális beállításokat, megmutatja, melyek vannak már házirend vagy eszközkezelés tulajdonában, és csak az Ön által kiválasztott beállításokat módosítja, egyenként, pontos visszavonási lehetőséggel.

## Az aktuális beállítások áttekintése

1. Nyissa meg az **Adatvédelem** szakaszt.
2. Jelölje be az áttekinteni kívánt területeket. Kezdetben egyik terület sincs bejelölve.
3. Nyomja meg a **Diagnózis futtatása** gombot. A jelentés megmutatja a gép és a KillerBenchet futtató fiók aktuális értékét, és megjelöli a házirend tulajdonában lévő beállításokat.

## Egy beállítás módosítása

1. Válassza ki a beállítást az **Adatvédelmi beállítás** mezőben, például a kamerát, a mikrofont, a helyet, a diagnosztikai adatokat, a hirdetéseket, a vágólapelőzményeket, a felhőalapú tartalomkeresést vagy a keresési kiemeléseket.
2. Válassza ki az új értéket, és jelölje be az **Alkalmazás, amikor a Diagnózis futtatása lehetőséget választom** beállítást.
3. Nyomja meg a **Diagnózis futtatása** gombot. A KillerBench ellenőrzi a házirend-tulajdonost, rögzíti az aktuális értéket, végrehajtja az egyetlen módosítást, majd visszaolvassa.

A csoportházirend vagy az eszközkezelés tulajdonában lévő beállítások változatlanok maradnak. Ezeket ott módosítsa, ahol felügyelik őket.

## Egy módosítás visszavonása

Minden igazolt módosításhoz napló tartozik. A megfelelő visszavonás ismét házirend-ellenőrzés után a pontos korábbi értéket állítja vissza.

## A Windows rendbetétele

A javaslatokhoz, a fogyasztói tartalmakhoz, a Recallhoz és az AI-funkciókhoz, a tálca és a Start menü zsúfoltságához, az Edge-hez és az előre telepített szoftverekhez nyissa meg a **Karbantartás** szakaszt, és válassza a **Windows rendbetétele** eszközt. Az áttekintés csak olvas, és semmi sincs automatikusan kiválasztva.

## Parancssorból

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

Lásd: [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) és [debloat-review](cli.html#cmd-debloat-review). Nem támogatott kulcs esetén a parancs felsorolja a támogatott lehetőségeket.

## Kapcsolódó

- [Adatvédelem szakasz](privacy.html)
- [Új számítógép beállítása](set-up-new-pc.html)
