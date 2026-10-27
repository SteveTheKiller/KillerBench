# Hardver

A Hardver a nyomtatókat, a hangot, a kamerákat, a Bluetootht, a beviteli eszközöket és azokat a Windows-szolgáltatásokat fedi le, amelyektől a csatlakoztatott hardver függ.

Lépésenkénti útmutatók: [Nyomtatási problémák megoldása](fix-printing.html) és [Összeomlások és kék képernyők kivizsgálása](investigate-crashes.html).

## Az ebben a szakaszban található eszközök

Nyomtatás, hang, kamerák, Bluetooth, beviteli eszközök és a csatlakoztatott hardver mögötti szolgáltatások.

### Nyomtatás

Ellenőrzi a Nyomtatásisor-kezelőt, a helyi sorok szüneteltetését, a látható nyomtatási feladatokat, az offline beállításokat és az illesztőprogramok által jelentett állapotokat, majd leltárba veszi a sor-, illesztőprogram-, port-, szabványos TCP/IP-végpont-, nyomtatásfeldolgozó- és nyomtatásfigyelő-regisztrációkat. A legutóbbi PrintService-problémákat csak akkor veti össze, ha egy pontos aktuális sorazonosító van jelen. A hozzárendelt szabványos TCP/IP-végpontok egyetlen korlátozott kapcsolódási kísérletet kapnak, alkalmazásadatok, SNMP és nyomtatási feladat nélkül. Az automatikus javítás elindíthatja az engedélyezett nyomtatásisor-kezelőt, csak a friss várakozófeladat-bizonyítékkal rendelkező, pontosan megadott szüneteltetett sorokat folytathatja, és kikapcsolhatja az offline módot. A tétlen, szándékos szüneteltetések csak áttekintésre szolgálnak. A távoli kapcsolatok, az SNMP-közösségi karakterláncok, a bővítményregisztrációk és a feladatok tartalma megmarad. Egy állapotkód vagy egy sikeres kapcsolat nem határozza meg a nyomtató azonosságát, a kellékanyagokat, a mechanikát, a sor általi elfogadást, a renderelés sikerét vagy a fizikai nyomtatást.

### Hang

Ellenőrzi az alapértelmezett multimédiás bemenet és kimenet azonosítóit és megosztott formátumait, a hangeszközök és illesztőprogramok állapotát, a Windows audiovégpont-építő szolgáltatást és a Windows Audio szolgáltatást. Az automatikus javítás elindítja a leállított, engedélyezett szolgáltatásokat, és ellenőrzi a futó állapotot. Az indítási beállítások, az illesztőprogramok, a kiválasztott eszközök, a kizárólagos mód és a hangerőszintek megmaradnak. A hanglejátszás, a felvétel és az alkalmazások hangirányítása teszteletlen marad.

### Kameratámogatás

Megvizsgálja a kamera- és képalkotóeszközök hibáit, a hardverazonosítókat, a telepített illesztőprogramok bizonyítékait és a Windows kamerakeret-kiszolgáló elérhetőségét. Az opcionális helyreállítás a függőségellenőrzések után csak egy engedélyezett, leállított szolgáltatást indít el. Egy már kamerahozzáférést kérő alkalmazás a meglévő engedélyekkel folytathatja. A kamera adatvédelmi beállításai, az engedélyek, az eszközkonfiguráció, a házirend, az illesztőprogramok és az indítási típus megmarad. Nincs alaphelyzetbe állítás, engedélymódosítás vagy illesztőprogram-telepítés. Az ellenőrzés csak a jelentett eszközkonfigurációra és a szolgáltatás futó állapotára terjed ki, nem a képrögzítésre, az adatvédelmi hozzáférésre vagy a fizikai működésre.

### Bluetooth-támogatás

Megvizsgálja a Bluetooth-eszközök hibáit, a hardverazonosítókat, a telepített illesztőprogramok bizonyítékait és a Bluetooth támogatási szolgáltatás elérhetőségét. Az opcionális helyreállítás a függőségellenőrzések után csak egy engedélyezett, leállított szolgáltatást indít el; a párosított eszközök újracsatlakozhatnak. A párosítások, a rádióbeállítások, az eszközkonfiguráció, a házirend és az indítási típus megmarad. Nincs alaphelyzetbe állítás, eszközeltávolítás vagy illesztőprogram-telepítés. Az ellenőrzés csak a jelentett eszközkonfigurációra és a szolgáltatás futó állapotára terjed ki, nem a felderítésre, a párosításra, a hangra vagy az adatátvitelre.

### Intelligenskártya-támogatás

Megvizsgálja az intelligenskártya-olvasók hibáit, a hardverazonosítókat, a telepített illesztőprogramok bizonyítékait és az Intelligens kártya szolgáltatás elérhetőségét. Az opcionális helyreállítás a függőségellenőrzések után csak egy engedélyezett, leállított szolgáltatást indít el. Az alkalmazások a meglévő házirend szerint folytathatják a behelyezett kártyák felismerését. A tanúsítványok, a PIN-kódok, a hitelesítő adatok, az olvasók és kártyák konfigurációja, a házirend, az illesztőprogramok és az indítási típus megmarad. Nincs inicializálás, regisztráció, PIN-művelet, olvasó-alaphelyzetbe állítás vagy illesztőprogram-telepítés. Az ellenőrzés csak a jelentett olvasókonfigurációra és a szolgáltatás futó állapotára terjed ki, nem a kártyafelismerésre, a hitelesítésre vagy az alkalmazások hozzáférésére.

### Biometrikus támogatás

Megvizsgálja a biometrikus eszközök hibáit, a hardverazonosítókat, a telepített illesztőprogramok bizonyítékait és a Windows biometrikus szolgáltatás elérhetőségét. Az opcionális helyreállítás a függőségellenőrzések után csak egy engedélyezett, leállított szolgáltatást indít el. A meglévő Windows Hello- és alkalmazáskérések az aktuális házirend szerint folytatódhatnak. A biometrikus sablonok, a Windows Hello adatai, a hitelesítő adatok, az eszközkonfiguráció, az engedélyek, a házirend, az illesztőprogramok és az indítási típus megmarad. Nincs regisztráció, sablontörlés, érzékelő-alaphelyzetbe állítás, engedélymódosítás vagy illesztőprogram-telepítés. Az ellenőrzés csak a jelentett érzékelőkonfigurációra és a szolgáltatás futó állapotára terjed ki, nem a rögzítésre, az egyeztetésre, a bejelentkezésre vagy az alkalmazások hozzáférésére.

### Emberi felületeszköz-szolgáltatás

Ellenőrzi az Emberi felületeszköz-szolgáltatás elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A meglévő gyorsgombok, médiavezérlők és egyéb HID-funkciók folytatódhatnak. Az eszközök, az illesztőprogramok, a gombhozzárendelések, a kisegítő lehetőségek beállításai, a beviteli beállítások, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench nem választ ki eszköz-, illesztőprogram-, hozzárendelési vagy beviteliebeállítás-műveletet. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a gombok, a vezérlők vagy az alkalmazások viselkedésére.

### Hordozható eszközök számbavételi szolgáltatása

Ellenőrzi a Hordozható eszközök számbavételi szolgáltatásának elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A meglévő alkalmazások folytathatják a csatlakoztatott hordozható eszközök felismerését és elérését. Az eszközök, az illesztőprogramok, a fájlok, a könyvtárak, a szinkronizálási beállítások, az engedélyek, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench nem választ ki eszköz-, fájl-, könyvtár-, szinkronizálási vagy átviteli műveletet. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a számbavételre, a hozzáférésre, a szinkronizálásra vagy az átvitelre.

### Windows képbeolvasás (WIA)

Ellenőrzi a Windows képbeolvasás szolgáltatás elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A meglévő lapolvasó- és állóképbeolvasási kérések az alkalmazások aktuális engedélyeivel folytatódhatnak. Az eszközök, az illesztőprogramok, a képadatok, az alkalmazásengedélyek, az adatvédelmi beállítások, az átviteli beállítások, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench nem választ ki eszköz-, illesztőprogram-, kép-, engedély-, átviteli vagy beolvasási műveletet. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a lapolvasó, a kamera, az átvitel vagy az alkalmazás működésére.

### Rendszerhéj hardverészlelése

Megvizsgálja a tárolóvezérlők, a lemezek és a hordozható eszközök korlátozott Plug and Play bizonyítékait és a telepített illesztőprogramokra mutató hivatkozásokat, majd ellenőrzi a Rendszerhéj hardverészlelése szolgáltatás elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A meglévő Automatikus lejátszás és hardvercsatlakoztatási értesítések az aktuális házirend szerint folytatódhatnak. Az eszközmegfigyelések és a megőrzött előzmények nem jogosítanak fel helyreállításra. Az eszközök, az illesztőprogramok, a kötetek, a csatlakoztatási pontok, az Automatikus lejátszás beállításai, a kezelők, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. Nincs kérés eszköz- vagy beállításműveletre. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem az értesítések kézbesítésére, az adathordozók készenlétére, a kötetek elérésére vagy az eszközök fizikai működésére.

### Eszköztelepítési szolgáltatás

Ellenőrzi az Eszköztelepítési szolgáltatás elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A Windows által kért meglévő eszköztelepítési munka az aktuális házirend szerint folytatódhat. Az eszközök, az illesztőprogramok, a csomagok, a konfiguráció, a kötések, az osztálybeállítások, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench semmit sem választ ki és nem telepít. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem az eszközök telepítésére vagy működésére.

### Eszköztársítási szolgáltatás

Ellenőrzi az eszközbeállítás és a Plug and Play legutóbbi előzményeit, majd az Eszköztársítási szolgáltatás elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A Windows által kért meglévő eszköztársítási munka folytatódhat. A párosított és társított eszközök, az illesztőprogramok, a hitelesítő adatok, a metaadatok, az engedélyek, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench nem választ ki eszköz-, párosítási, eltávolítási, alaphelyzetbe állítási vagy illesztőprogram-műveletet. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a felderítésre, a párosításra, a társításra vagy az eszközök működésére.

### Megjelenítésjavító szolgáltatás

Megvizsgálja a grafikus adapterek és a monitorok bizonyítékait, majd ellenőrzi a Megjelenítésjavító szolgáltatás elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A meglévő fényerő-, szín- és javításkérések folytatódhatnak. A megjelenítési konfiguráció, a fényerő, a színprofilok, a HDR-beállítások, az eszközök, az illesztőprogramok, az energiagazdálkodási beállítások, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. Egyetlen megjelenítési beállítás sem változik. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a javítások viselkedésére, a képminőségre vagy a kijelző fizikai működésére.

### Szövegbevitel-kezelő szolgáltatás

Megvizsgálja a billentyűzetek, az egerek és a HID-eszközök bizonyítékait, majd ellenőrzi a Szövegbevitel-kezelő szolgáltatás elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. Az érintőbillentyűzet, a kézírás, az emodzsik és a speciális szövegbevitel meglévő kérései folytatódhatnak. A szótárak, a kézírásadatok, a nyelvi és billentyűzetbeállítások, a kisegítő lehetőségek és az adatvédelmi beállítások, az eszközök, az illesztőprogramok, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. Szövegbevitel, billentyűleütés vagy kézírás tartalmának gyűjtése nem történik. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a szövegbevitelre, a felismerésre vagy az eszközök működésére.

### Windows betűtípus-gyorsítótár szolgáltatás

Ellenőrzi a Windows betűtípus-gyorsítótár szolgáltatás elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A meglévő alkalmazások ismét használhatják az aktuális betűtípus-gyorsítótárat. A telepített betűtípusok, a betűtípusfájlok, az alkalmazásadatok, a gyorsítótárfájlok, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench nem telepít és nem távolít el betűtípust, és nem törli vagy építi újra a gyorsítótárat. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a szövegmegjelenítésre vagy az alkalmazások viselkedésére.

### Funkciófelderítési szolgáltató gazdagépe

Ellenőrzi a Funkciófelderítési szolgáltató gazdagépének elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. A meglévő alkalmazások a telepített szolgáltatókon keresztül folytathatják a hálózati eszközök felderítését. A hálózati profilok, a megosztások, az eszközök, a szolgáltatók, a tűzfalszabályok, a felderítési beállítások, a hitelesítő adatok, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench nem választ ki eszköz-, megosztás-, szolgáltató-, tűzfalszabály- vagy felderítésibeállítás-műveletet. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a szolgáltatók állapotára, az eszközfelderítésre vagy a kapcsolódásra.

### Funkciófelderítési erőforrások közzététele

Ellenőrzi a Funkciófelderítési erőforrások közzététele szolgáltatás elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott szolgáltatást indítja el, ha az leállt és automatikus indításra van beállítva. A leállított, igény szerint induló szolgáltatás csak tájékoztató jellegű marad. Ez a PC a meglévő hálózatfelderítési és tűzfalbeállítások révén felderíthetővé válhat. A hálózati profilok, a megosztások, a közzétett erőforrások, a tűzfalszabályok, a felderítési beállítások, a hitelesítő adatok, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench nem választ ki megosztás-, erőforrás-, tűzfalszabály- vagy felderítésibeállítás-műveletet. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a közzétételre, a láthatóságra, a hozzáférésre vagy a kapcsolódásra.
