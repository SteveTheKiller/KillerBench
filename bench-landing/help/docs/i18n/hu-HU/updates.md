# Frissítések

A Frissítések a Windows Update-et, a mögötte álló szolgáltatásokat és az összetevőtárat fedi le. Láthatja, mi van telepítve, mi függőben, és miért hiúsul meg egy frissítés, és áttekintheti az elérhető frissítéseket, mielőtt bármelyiket kiválasztaná telepítésre.

Lépésenkénti útmutatók: [A Windows Update javítása](fix-windows-update.html) és [Rendszerfájlok javítása](repair-system-files.html).

## Az ebben a szakaszban található eszközök

Karbantartási tevékenység figyelése, elérhető frissítések áttekintése és a Windows Update-lánc javítása.

### Karbantartási tevékenység figyelője

Egy vagy több pillanatképen keresztül figyeli a Windows Update és a Setup korlátozott karbantartási tevékenységét. A beállítások szabályozzák a frissítés időzítését, valamint azt, hogy szerepeljenek-e a frissítési események, a frissítési előzmények, a pontos Windows Update ütemezett feladat, a pontos szolgáltatásállapotok, az újraindítási jelzők, az aktuális fiók háttérátvitelei, a Kézbesítésoptimalizálás tevékenysége és az adatvédelmi szempontból korlátozott Windows Installer-tevékenység. A folyamatazonosítók, a parancssorok, a felhasználók és az útvonalak ki vannak zárva. A tevékenység nem bizonyít előrehaladást, befejezést, elakadást vagy hibát. Nincs kérés frissítési, telepítési, átviteli, feladat-, szolgáltatás-, folyamat-, házirend-, újraindítási vagy javítási műveletre.

### Elérhető frissítések

Felderíti a kiválasztott szoftver- és illesztőprogram-frissítéseket a beállított forrás, a Windows Update vagy egy már regisztrált Microsoft Update szolgáltatás használatával. A forrás készenlétét a felderítés előtt ellenőrzi. A felderítés semmit nem telepít, nem fogad el licencet, nem regisztrál forrást és nem módosít házirendet. A kiválasztott frissítések ezután, külön, csak a megerősítő jelölőnégyzet bejelölése után telepíthetők. Ez független a karbantartási javítástól.

### Windows Update

Korlátozottan leltárba veszi a Windows karbantartási csomagjainak, opcionális szolgáltatásainak és képességeinek állapotát, ellenőrzi az újraindítási jelzőket, a szabad helyet és a frissítési szolgáltatásokat, elindítás nélkül megfigyeli a Kézbesítésoptimalizálást, majd megvizsgálja az összetevőtárat és a védett rendszerfájlokat. A függő csomag-, szolgáltatás- vagy képességállapotok áttekintést kérnek, de nem jogosítanak fel összetevő-műveletre. Az automatikus javítás elindíthatja a leállított Windows Update, BITS, Titkosítási szolgáltatások, Windows-modultelepítő és Update Orchestrator szolgáltatást, ha automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatást csak jelzi, nem indítja el. Az Update Orchestrator elindítása az aktuális házirend szerint folytathatja a meglévő ütemezett frissítési tevékenységet. A csomagok, az opcionális szolgáltatások, a képességek, a Kézbesítésoptimalizálás gyorsítótára, a társgépek, a sávszélesség, a házirend, a forrásregisztráció, az ütemezések és az indítási beállítások megmaradnak. A DISM-hez vagy az SFC-hez a sérülés élő bizonyítéka szükséges. Ez a folyamat nem választ ki és nem hagy jóvá frissítést.

### Windows Update szolgáltatás

Ellenőrzi a Windows Update szolgáltatás pontos állapotát, a regisztrált szolgáltatás-DLL-jét, a korlátozott aktuális frissítési tevékenységet és a szolgáltatás pontos hibaelőzményeit. A DLL-megfigyelések, a frissítési tevékenység és a korábbi hibák nem jogosítanak fel szolgáltatásindításra vagy frissítési műveletre. Az opcionális helyreállítás friss függőség- és végrehajtható-fájl-regisztrációs ellenőrzések után csak egy leállított automatikus szolgáltatást indít el. Egy leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A meglévő frissítési tevékenység az aktuális házirend szerint folytatódhat. A frissítési házirend, a forrásregisztráció, az ütemezések, az újraindítási beállítások, a csomagok, a BITS-feladatok, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench egyetlen frissítést sem választ ki, nem hagy jóvá és nem telepít. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a forrás elérhetőségére, az alkalmazhatóságra, a letöltésre, a telepítésre, a befejezésre vagy az újraindítási készenlétre.

### Kézbesítésoptimalizálás

Ellenőrzi a Kézbesítésoptimalizálás szolgáltatás pontos állapotát. Egy leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. Helyreállítást csak a pontos Szüneteltetve állapot kínálhat. Az opcionális helyreállítás megismétli az állapotellenőrzést, egyetlen korlátozott folytatási kérést küld, és két egybehangzó Fut megfigyelést követel meg. Az indítási típus, a gyorsítótár tartalma, a társgépek, a sávszélesség, a frissítési forrás és a felügyelt házirend megmarad. Az átviteli tevékenység, a gyorsítótár állapota, a frissítés előrehaladása és befejezése ellenőrizetlen marad.

### BITS szolgáltatás

Ellenőrzi az Intelligens háttérátviteli szolgáltatás pontos állapotát, az aktuális fiók átviteli feladatait és a szolgáltatás korlátozott, pontos hibaelőzményeit. A korábbi hibák és az átviteli pillanatképek nem jogosítanak fel szolgáltatásindításra. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A meglévő alkalmazások folytathatják a sorban álló háttérátviteleket. A feladatok, a prioritások, a tulajdonosok, a célok, a proxybeállítások, a házirend és az indítási típus megmarad. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem az átvitel előrehaladására, az alkalmazások tulajdonlására vagy a befejezésre.

### Háttérátvitelek (BITS)

Beolvassa a BITS átviteli feladatait a kiválasztott fiókhatókörben, és áttekinti a pontos feladatazonosítókat. A sornevek nem határozzák meg, melyik alkalmazáshoz tartozik a feladat. Egyetlen átvitel sem folytatódik, fejeződik be, szakad meg, törlődik vagy kerül újra hozzárendelésre.

### Titkosítási szolgáltatások

Ellenőrzi a Titkosítási szolgáltatások pontos állapotát, a regisztrált szolgáltatás-DLL-jét és a szolgáltatás korlátozott, pontos hibaelőzményeit. A korábbi hibák és a DLL-megfigyelések nem jogosítanak fel szolgáltatásindításra vagy összetevőcserére. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A meglévő katalógus-, tanúsítvány- és frissítési műveletek folytatódhatnak. A tanúsítványok, a kulcsok, a katalógusok, a megbízhatósági beállítások, a házirend és az indítási típus megmarad. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a tanúsítványok, aláírások, katalógusok vagy a megbízhatóság érvényesítésére.

### Windows-modultelepítő

Ellenőrzi a Windows-modultelepítő pontos állapotát, a karbantartási csomagok korlátozott állapotváltásait és a szolgáltatás pontos hibaelőzményeit. A korábbi hibák és a csomagállapotok nem jogosítanak fel szolgáltatásindításra vagy csomagműveletre. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A meglévő karbantartási tevékenység folytatódhat. A csomagok, az összetevőtár tartalma, a források, a házirend és az indítási típus megmarad. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem az összetevőtár állapotára, a csomagok alkalmazhatóságára, a karbantartás befejezésére vagy az újraindítási készenlétre.

### Windows Update Medic szolgáltatás

Ellenőrzi a Windows Update Medic szolgáltatás pontos állapotát, a regisztrált szolgáltatás-DLL-jét, a korlátozott aktuális frissítési tevékenységet és a szolgáltatás pontos hibaelőzményeit. A DLL-megfigyelések, a frissítési tevékenység és a korábbi hibák nem jogosítanak fel szolgáltatásindításra vagy összetevő-műveletre. Egy leállított, igény szerint induló szolgáltatás lehet normális. Ha pontosan ez az eszköz van kiválasztva, az opcionális helyreállítás friss függőségellenőrzések után csak a leállított, engedélyezett szolgáltatást indíthatja el. A Windows-helyreállítás az aktuális házirend szerint folytatódhat, és javíthatja a frissítési összetevőket vagy frissítési tevékenységet indíthat. A frissítési házirend, a forrásregisztráció, az ütemezések, a csomagok, a BITS-feladatok, az újraindítási beállítások, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench nem választ ki frissítést, és nem módosít frissítési beállítást. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a helyreállításra, az összetevők állapotára, az alkalmazhatóságra, a letöltésre, a telepítésre vagy az újraindítási készenlétre.

### Update Orchestrator szolgáltatás

Ellenőrzi az Update Orchestrator szolgáltatás pontos állapotát, a pontos beépített Windows Update feladatot, a korlátozott aktuális tevékenységet és a szolgáltatás pontos hibaelőzményeit. A feladateredmények, a frissítési tevékenység és a korábbi hibák nem jogosítanak fel szolgáltatásindításra vagy frissítési műveletre. Az opcionális helyreállítás friss függőség- és végrehajtható-fájl-regisztrációs ellenőrzések után csak egy leállított automatikus szolgáltatást indít el. Egy leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A meglévő ütemezett frissítési tevékenység az aktuális házirend szerint folytatódhat, és letöltheti vagy telepítheti a már jóváhagyott frissítéseket. A frissítési házirend, a forrásregisztráció, az ütemezések, az újraindítási beállítások, a csomagok, a BITS-feladatok, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench egyetlen frissítést sem választ ki és nem hagy jóvá. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem az alkalmazhatóságra, a letöltésre, a telepítésre, a befejezésre vagy az újraindítási készenlétre.

### Összetevőtár tisztítása

Elemzi, hogy a Windows javasolja-e az összetevőtár tisztítását. Az opcionális karbantartás azonnal eltávolítja a felülírt összetevőket, majd újra elemez. Nincs ResetBase és nincs kézi WinSxS-törlés. Nincs automatikus visszavonás; a felszabadított lemezterület nincs mérve.
