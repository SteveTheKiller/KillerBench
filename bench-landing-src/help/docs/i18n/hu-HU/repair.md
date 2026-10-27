# Javítás

A Javítás azokat az eszközöket gyűjti össze, amelyek ki tudnak javítani valamit. Mindegyik először diagnosztizál. Javítás csak akkor jelenik meg, ha friss bizonyíték pontosan azt a problémát mutatja, amelyet kijavít, és csak akkor fut le, ha bekapcsolja a **Megerősített problémák automatikus javítása** beállítást, vagy saját maga megerősít egy egyedi javítást. Minden javítás naplózásra kerül, és utána ellenőrzésre.

Lépésenkénti útmutatók: [Rendszerfájlok javítása](repair-system-files.html) és [A Windows Update javítása](fix-windows-update.html).

## Automatikus és kézi javítás

A **Megerősített problémák automatikus javítása** lehetővé teszi, hogy egy diagnosztikai eszköz a vizsgálat után lefuttassa azokat a javításokat, amelyek szükségességét igazolja. Alapértelmezés szerint ki van kapcsolva.

Egyetlen karbantartási javításhoz nyissa meg a **Speciális: naplók és kézi javítások** panelt, és válassza a **Javítások** lehetőséget. Válassza az **Összetevőtár (DISM)** vagy a **Rendszerfájlok (SFC)** elemet, olvassa el az előnézetet, jelölje be **A javítás futtatása ezen a PC-n** beállítást, és nyomja meg a **Javítás futtatása** gombot. Azt, hogy mi történik egy módosítás előtt és után, a [Hogyan maradnak biztonságosak a módosítások](safety.html) oldal írja le.

## Az ebben a szakaszban található eszközök

Akkor használja, ha egy diagnózis problémát azonosított. A javításokhoz igazolt bizonyíték és kifejezett jóváhagyás szükséges.

### Alapvető Windows-szolgáltatások

Együtt ellenőrzi az Energiaellátás, a Plug and Play, a Felhasználókezelő, a Csoportházirend-ügyfél, a Hálózati tárolófelület, az RPC-végpontleképező, a Távoli eljáráshívás (RPC), a DCOM-kiszolgálófolyamat-indító, a Háttérfeladat-infrastruktúra és a CoreMessaging szolgáltatást. Az opcionális helyreállítás csak azokat a pontosan megadott, engedélyezett automatikus szolgáltatásokat indítja el, amelyekről friss bizonyíték igazolja, hogy az indítási időablak és a teljes függőségellenőrzés után leállított állapotban vannak. Az igény szerint induló szolgáltatások tétlen állapotban változatlanok maradnak. Az energiasémák, az eszközök, az illesztőprogramok, a fiókok, a hitelesítő adatok, a profilok, a házirend, az adapterek, a címek, az útvonalak, a DNS, a tűzfal konfigurációja, a regisztrációk, a végpontok, a feladatregisztrációk, az alkalmazásadatok, a szolgáltatásengedélyek és az indítási típusok megmaradnak. Nincs kérés házirend-frissítésre, eszközvizsgálatra, fiókműveletre, hálózatmódosításra, energiabeállítás-módosításra, regisztrációmódosításra vagy feladatindításra. Az ellenőrzés csak a szolgáltatások stabil állapotára terjed ki, nem a funkciók teljes működésére.

### Rendszerfájlok

Korlátozottan leltárba veszi a Windows karbantartási csomagjainak, opcionális szolgáltatásainak és képességeinek állapotát, ellenőrzi az előfeltételeket, megvizsgálja a védett rendszerfájlokat, és csak akkor választja automatikusan az SFC-t, ha az élő ellenőrzés sérülést igazol. A függő csomag-, szolgáltatás- vagy képességállapotok áttekintést kérnek, de nem jogosítanak fel telepítésre, eltávolításra, tisztításra, visszaállításra, engedélyezésre vagy letiltásra.

### WMI-tárház

Ellenőrzi a WMI szolgáltatás elérhetőségét, majd a tárház konzisztenciáját. Az automatikus javítás elindíthat egy jogosult, leállított szolgáltatást és helyreállíthatja az igazolt sérülést, majd minden eredményt ellenőriz. A letiltott szolgáltatás beállításai megmaradnak. Az olvasható regisztrációk egyesítésre kerülnek; az olvashatatlan tartalomhoz a szolgáltató újratelepítése lehet szükséges. A felügyeleti ügyfelek működése megszakadhat. Nincs alaphelyzetbe állítás, törlés vagy automatikus visszaállítás.

### Windows rendszerértesítések

Beolvassa a Szolgáltatásvezérlő legutóbbi előzményeit, majd együtt ellenőrzi a COM+ eseményrendszert és a Rendszeresemény-értesítési szolgáltatást. Az opcionális helyreállítás a SENS elindítása előtt ellenőrzi a COM+ eseményrendszer készenlétét. A meglévő bejelentkezési, hálózati, energiagazdálkodási és COM+-értesítések az aktuális konfiguráció szerint folytatódhatnak. A COM+-katalógusok, az előfizetések, az értesítési regisztrációk, a fogadók, a fiókok, az engedélyek, a helyreállítási beállítások, a házirend és az indítási típusok megmaradnak. Egyetlen előfizetés, regisztráció, fogadó, fiók, engedély vagy házirend sem változik. Az ellenőrzés csak a szolgáltatások stabil állapotára terjed ki, nem az értesítések teljes kézbesítésére.

### Diagnosztikai házirend szolgáltatás

Áttekinti a Diagnosztikai házirend korlátozott működési előzményeit, a szolgáltatás pontos hibaelőzményeit és a szolgáltatás aktuális készenlétét. A hiányos bizonyíték elsőbbséget élvez. A korábbi bejegyzések nem jogosítanak fel szolgáltatás-helyreállításra vagy hibaelhárítási műveletre. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott, engedélyezett és leállított szolgáltatást indítja el. A meglévő diagnosztikai forgatókönyvek folytatódhatnak. A házirend, a hibaelhárítási adatok és az indítási típus megmarad. Egyetlen hibaelhárító vagy javasolt javítás sem fut. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a hibaészlelésre, a diagnózis befejezésére vagy a javítás eredményére.

### Windows hibajelentés

Áttekinti az összeomlási memóriaképek konfigurációját, a memóriaképfájlok korlátozott metaadatait, a legutóbbi rendszerösszeomlási jelentéseket, a Windows hibajelentés szolgáltatás pontos hibaelőzményeit és a szolgáltatás aktuális készenlétét. A korábbi jelentések nem határoznak meg aktuális hibát vagy kiváltó okot, és nem jogosítanak fel helyreállításra. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott, engedélyezett és leállított szolgáltatást indítja el. A sorban álló meglévő jelentések az aktuális házirend szerint folytatódhatnak. A jelentésarchívumok, a memóriaképek, a hozzájárulás, a feltöltési házirend és az indítási típus megmarad. Nincs kérés feltöltésre vagy törlésre. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a jelentések létrehozására, feltöltésére, kézbesítésére, a probléma megoldására vagy az összeomlás diagnózisára.

### Windows eseménygyűjtő

Áttekinti a Windows eseménygyűjtő korlátozott előfizetési leltárát, a szolgáltatás pontos hibaelőzményeit és a szolgáltatás aktuális készenlétét. Az üres leltár lehet szándékos, és sem az előfizetések száma, sem a korábbi hibák nem jogosítanak fel helyreállításra. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott, engedélyezett és leállított szolgáltatást indítja el. A meglévő előfizetések folytathatják a távoli eseménygyűjtést. Az előfizetések, a továbbított események, a hitelesítő adatok, a csatornabeállítások, a házirend és az indítási típus megmarad. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a források elérhetőségére, a hitelesítésre, a távoli kézbesítésre, az események frissességére vagy az előfizetések állapotára.

### Windows eseménynapló

Négy diagnosztikai csatornán megvizsgálja az engedélyezett állapotot, a megőrzési módot, az aktuális méretet, a beállított korlátot, a bejegyzések számát és a telítettséget, majd ellenőrzi a Windows eseménynapló szolgáltatás elérhetőségét. Az automatikus javítás a függőségek ellenőrzése után elindíthat egy leállított, engedélyezett szolgáltatást, és ellenőrizheti a futó állapotot. Egy külön megerősített, célzott művelet engedélyezhet egyetlen pontosan megadott, letiltott és nem felügyelt diagnosztikai csatornát, a többi jelentett beállítás megtartásával. A naplók, a csatornaengedélyek, a megőrzés, a méretkorlátok, az indítási típusok és a felügyelt beállítások megmaradnak. Az események sikeres kézbesítése és az archívum kapacitása ellenőrizetlen marad.

### Windows Idő szolgáltatás

Ellenőrzi a Windows Idő konfigurációját, szolgáltatását és szinkronizálását. Az automatikus javítás elindíthatja a W32Time szolgáltatást, ha az aktuális állapot pontosan a leállított szolgáltatás hibáját jelzi, és a friss házirend-, mód- és függőségellenőrzések sikeresek, majd újraszinkronizálást kér, és ellenőrzi a friss állapotot. Ez a meglévő beállított források alapján módosíthatja az órát. Az indítási típus, a forráskonfiguráció és a felügyelt beállítások megmaradnak.

### Feladatütemező szolgáltatás

Korlátozottan leltárba veszi az ütemezett feladatok állapotát, futási eredményeit és a kimaradt futások számát, majd ezeket összekapcsolja a Feladatütemező szolgáltatás pontos hibaelőzményeivel, mielőtt ellenőrizné a szolgáltatás aktuális elérhetőségét. A feladatokra vonatkozó megállapítások és a szolgáltatás korábbi hibái nem jogosítanak fel szolgáltatásindításra. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott, engedélyezett és leállított szolgáltatást indítja el. A meglévő engedélyezett feladatok a beállított fiókjukkal elindulhatnak, a kimaradt eseményindítókat is beleértve. Egyetlen feladatdefiníció, rendszerbiztonsági tag, hitelesítőadat-hivatkozás, házirend vagy indítási típus sem változik, és egyetlen feladat sem indul el vagy törlődik közvetlenül. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a műveletek sikerére, a hitelesítő adatok érvényességére vagy a feladatok jövőbeli eredményeire.

### Felhasználói profil szolgáltatás

Ellenőrzi a Felhasználói profil szolgáltatás elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott, engedélyezett és leállított szolgáltatást indítja el. A meglévő bejelentkezési és profilkérések folytatódhatnak. A profilmappák, a beállításjegyzék-kaptárak, az engedélyek, a profillista-regisztrációk, a hitelesítő adatok, a felhasználói adatok, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. Nincs kérés profilműveletre. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a bejelentkezésre, a profil betöltésére vagy a felhasználói adatok elérésére.

### Biztonsági mentési szolgáltatások

Ellenőrzi a Kötet árnyékmásolata, a Microsoft szoftveres árnyékmásolat-szolgáltató, a Windows biztonsági mentés és a Blokkszintű biztonsági mentési motor szolgáltatást, majd beolvassa a VSS-írók állapotát, a regisztrált szolgáltatótípusokat, az aktuális pillanatképeket és az árnyékmásolat-tároló helyfoglalását. A leállított, igény szerint induló szolgáltatások tétlen állapotban lehetnek normálisak. Az opcionális helyreállítás a függőségellenőrzések után csak a leállított automatikus szolgáltatásokat indítja el, majd ellenőrzi a futó állapotot. A meglévő ütemezett vagy megszakított biztonsági mentési munka folytatódhat. A letiltott és az igény szerinti indítási beállítások, a pillanatképek, az árnyékmásolat-tároló, a szolgáltatói regisztrációk, a biztonsági mentési tervek, a katalógusok, az ütemezések, a célok, a hitelesítő adatok és a megőrzés megmarad. Egyetlen biztonsági mentés sincs kiválasztva vagy újrakonfigurálva, egyetlen szolgáltatás sem indul újra, és egyetlen pillanatkép vagy katalógus sem törlődik. A szolgáltatói regisztráció, az írók állapota, a tároló helyfoglalása és a szolgáltatások stabil állapota nem igazolja a biztonsági mentés befejezését, a cél elérhetőségét vagy a visszaállítás sikerét.
