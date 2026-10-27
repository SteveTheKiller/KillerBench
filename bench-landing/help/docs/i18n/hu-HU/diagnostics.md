# Diagnosztika

Minden látogatásnak a Diagnosztikával kell kezdődnie. Minden eszköz beolvassa a gépet, és jelentést készít. Itt semmi sem módosítja a Windowst. Az alapértelmezett eszköz, a teljes rendszervizsgálat, csak olvasó ellenőrzések széles körét futtatja, és rangsorolja, mi igényel figyelmet.

Lépésenkénti útmutatók: [Összeomlások és kék képernyők kivizsgálása](investigate-crashes.html) és [Rendszerfájlok javítása](repair-system-files.html).

## Az ebben a szakaszban található eszközök

Először ellenőrizze a gépet, majd a jelentésből olvassa ki, mi igényel figyelmet, és mi a következő biztonságos lépés.

### Teljes rendszervizsgálat

Megvizsgál 49 diagnosztikai területet, és kiértékeli a telepített javítási recepteket az aktuális bizonyítékok alapján. Az ellenőrzések több percig is tarthatnak. A teljes rendszervizsgálat kizárólag diagnosztikai. Egy támogatott művelet kiválasztásához és megerősítéséhez használjon célzott javítási folyamatot, miután áttekintette a friss előfeltételeket. A korábbi, a biztonsági mentési és az átviteli pillanatképek csak olvashatók. A memóriaképek tartalma nem nyílik meg. A piszkos bit vizsgálata nem ütemez javítást vagy újraindítást. Ez a vizsgálat nem telepít frissítéseket, nem módosít illesztőprogramokat, nem töröl fájlokat, és nem végez teljes hardvertesztet.

### Életjelek

Rögzíti a rendelkezésre álló memóriát, a szolgáltató által jelentett lemezállapotot és a BitLocker-védelem állapotát, majd az aktuális életjeleket összeveti az eszközproblémákkal, a megbízhatósági előzményekkel, a hardverhiba-bejegyzésekkel, az összeomlási bizonyítékokkal, a váratlan leállásokkal és a Windows Memóriadiagnosztika eredményeivel. Az újraindítási jelzők, a rendszerlemez szabad helye, az emelt jogosultság és a frissítési szolgáltatás konfigurációja látható marad. Helyreállítási jelszavak gyűjtése nem történik. Ebben a folyamatban nem fut javítás. Az összesített eredmény nem teljes hardver-, szoftver- vagy stabilitási teszt.

### Megbízhatósági előzmények

Beolvassa a Windows Megbízhatósági figyelő elmúlt 14 napjának bejegyzéseit a legutóbbi alkalmazáshibákkal és váratlan leállási eseményekkel együtt, majd egyetlen korlátozott történeti következtetést ad. A korábbi bejegyzések nem határozzák meg az aktuális állapotot vagy a kiváltó okot. Nincs kiválasztott javítás, és egyetlen alkalmazás, szolgáltatás vagy beállítás sem változik.

### Indítási teljesítmény

Beolvassa a Windows indítási és leállítási teljesítményeseményeinek elmúlt 14 napra vonatkozó metaadatait, csak a szolgáltató azonosítóját és a korlátozott numerikus időtartammezőket megtartva, majd összeveti azokat a támogatott függő újraindítási jelzőkkel. Az útvonalak, a folyamatnevek, a fiókok és az esemény többi hasznos adata a bizonyíték mentése előtt eltávolításra kerül. A korábbi teljesítményromlási bejegyzések nem azonosítanak aktuális szűk keresztmetszetet, és nem jogosítanak fel indítási, szolgáltatás-, alkalmazás-, illesztőprogram-, feladat-, energiagazdálkodási, újraindítási vagy házirend-módosításra.

### Hardverhiba-előzmények

Beolvassa a Windows Hardware Error Architecture és a memóriadiagnosztika elmúlt 14 napjának előzményeit, az aktuális Plug and Play eszközproblémákat, a fizikai lemezek állapotát és a szolgáltatói megbízhatósági számlálókat, majd egyetlen korlátozott hardveres következtetést ad. A bejegyzéseket össze kell vetni a hasznos adataikkal, a belső vezérlőprogram diagnosztikájával és a gyártói hardvertesztekkel. Nem következtet hibás összetevőre vagy kiváltó okra, nem választ ki javítást, és nem módosít hardvert, belső vezérlőprogramot, illesztőprogramot, lemezt, készletet, kötetet, fájlt, szolgáltatást, energiagazdálkodást, újraindítást vagy beállítást.

### Illesztőprogram-telepítési előzmények

Beolvassa a Windows SetupAPI eszköztelepítési előzményeinek megőrzött végét, a sikertelen szakaszokat összeveti a pontos aktuális Plug and Play azonosítókkal, és egyetlen korlátozott következtetést ad. A korábbi naplószakaszok nem bizonyítják, hogy egy eszköz még jelen van, vagy hogy egy hiba aktuális. Egyetlen illesztőprogram sem települ, távolítódik el, áll vissza vagy frissül.

### Rendszer-visszaállítási pontok

Legfeljebb 256 elérhető Rendszer-visszaállítási pontot listáz, áttekinti a pont létrehozásának kifejezett jogosultságát, és egyetlen korlátozott készenléti következtetést ad. A leltár nem bizonyítja, hogy a védelem minden kötethez engedélyezve van, vagy hogy egy visszaállítás sikeres lesz. A diagnózis során egyetlen visszaállítási pont sem jön létre, törlődik vagy kerül alkalmazásra.

### Windows helyreállítási környezet

A REAgentC segítségével beolvassa a Windows helyreállítási környezet aktuális konfigurációját, egy engedélyezett helyi helyet összevet a pontos aktuális lemezzel és partícióval, és egyetlen korlátozott készenléti következtetést ad. Az opcionális automatikus javítás engedélyezhet egy letiltott meglévő konfigurációt, majd ellenőrzi a jelentett állapotot. A helyreállítási rendszerkép nem töltődik le, nem kerül áthelyezésre és nem cserélődik, helyreállítási rendszerindítás nem indul, és a Windows nem indul újra. Az engedélyezett állapot és a partíció jelenléte nem bizonyítja, hogy a helyreállítási rendszerkép el tudja indítani vagy meg tudja javítani ezt a PC-t.

### Windows aktiválás

Beolvassa a Windows aktuális licencállapotát a termékkulcsok teljes vagy részleges gyűjtése nélkül, megőrzi a Szoftvervédelem figyelmeztetési, hiba- és kritikus eseményeinek elmúlt hét napra vonatkozó, megtisztított metaadatait, majd egyetlen korlátozott működési következtetést ad. Az esemény hasznos adatai a bizonyíték mentése előtt eltávolításra kerülnek. A korábbi bejegyzések háttérként szolgálnak, ha az aktuális licencelés Licencelt állapotot jelez. Nincs kiválasztott aktiválási, termékkulcs-, token-, licenc-, fiók-, szolgáltatás- vagy házirend-művelet. A szolgáltatói állapot és az eseményelőzmények nem bizonyítják az előfizetési jogosultságot, az aktiválási kiszolgáló elérhetőségét, a tokentároló integritását, a jövőbeli érvényességet vagy a kiváltó okot.

### Windows-szolgáltatások

Leltárba veszi a telepített szolgáltatások nevét, megjelenített nevét, állapotát, indítási módját és az adatvédelmi szempontból korlátozott helyreállításiművelet-típusait a fiókok, a végrehajtható fájlok útvonalai, a helyreállítási parancsok és az újraindítási üzenetek gyűjtése nélkül. Áttekinti az indítási módok összhangját, a közvetlen függőségek azonosítóit és ciklusait, ismételten ellenőrzi a pontos függő állapotokat, megvizsgálja a támogatott szolgáltatásokat és a leállított szolgáltatások függőségeit, majd csak akkor veti össze a Szolgáltatásvezérlő legutóbbi hibáit, ha pontosan egy szolgáltatásnév vagy egyedi megjelenített név van jelen. A betöltési sorrend csoportjainak használatát a tagság kibontása nélkül számolja, a nem egyértelmű azonosítók pedig feloldatlanok maradnak. A leállított, igény szerint induló szolgáltatások lehetnek normálisak. Egyetlen szolgáltatás sem indul el, az indítási, függőségi és helyreállítási beállítások megmaradnak. Ha egy adott funkciónak helyreállításra van szüksége, használja a Javítás szakasz megfelelő folyamatát.

### Alkalmazáshibák

Beolvassa az Alkalmazás napló legutóbbi hiba- és kritikus eseményeit a Megbízhatósági figyelő korlátozott előzményeivel együtt, majd egyetlen stabilitási előzményekre vonatkozó következtetést ad. A korábbi bejegyzések nem bizonyítanak aktuális hibát vagy kiváltó okot, csak szűkebb diagnózis felé irányítanak. Egyetlen alkalmazás sem áll le, javítódik, távolítódik el vagy konfigurálódik újra.

### Megjelenítőeszközök

Megvizsgálja a grafikus adaptereket és a monitorokat a jelentett eszközhibák, a hardverazonosítók és a telepített illesztőprogram-verziók szempontjából. Nincs illesztőprogram-telepítés, eszköz-alaphelyzetbe állítás vagy felbontásmódosítás. A képminőség és a fizikai csatlakozások nincsenek tesztelve.

### Beviteli eszközök

Megvizsgálja a billentyűzetek, egerek és HID-eszközök hibáit és az illesztőprogram-leltárt. Billentyűleütések rögzítése, eszközök alaphelyzetbe állítása vagy illesztőprogramok módosítása nem történik. A fizikai bevitel működése nincs tesztelve.

### Eszközproblémák

Megkeresi a nem nulla Plug and Play konfigurációs hibakódokat az összes eszközosztályban, majd a jelen lévő eszközök bizonyítékait összeveti az illesztőprogram-telepítés, az eszközbeállítás és a Plug and Play korlátozott SetupAPI-előzményeivel. A korábbi bejegyzések önmagukban nem határoznak meg aktuális hibát. Tekintse át a pontos eszközazonosítót és a jóváhagyott illesztőprogram bizonyítékait a hardver, az illesztőprogramok vagy a konfiguráció módosítása nélkül. A hiányzó eszközök és a fizikai működés nincsenek ellenőrizve.
