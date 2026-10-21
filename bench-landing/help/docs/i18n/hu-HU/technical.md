# A KillerBench technológiája

Bepillantás a motorháztető alá: hogyan olvassa be a KillerBench egy Windows-számítógép állapotát, hogyan tartja hitelesen a bizonyítékokat, és hogyan dönti el, mikor engedélyezett egy módosítás.

## Technológiai háttér

A KillerBench natív Windows-alkalmazás. Az asztali alkalmazás, a parancssor és az alattuk működő Engine ugyanazokat a diagnosztikai bizonyítékokat és javítási folyamatokat használja.

| Összetevő | Részletek |
| --- | --- |
| Felület | WPF .NET 10-en (`net10.0-windows`), Windows x64, egyedi ablakkeret |
| Engine | `Engine.Core`, `Engine` és `Engine.Windows`, a WPF-re vagy asztali vezérlőkre mutató hivatkozás nélkül |
| Parancssor | `killerbench-cli`, külön gazdafolyamat ugyanazon az Engine-en |
| Windows-szolgáltatók | .NET, Windows API-k, PowerShell és Windows parancssori eszközök, mindegyik rögzített idő-, rekord- és méretkorlátok között fut |
| Jelentések | Nyers kimenet, Olvasható jelentés, HTML, szöveg és JSON |
| Referenciaadatok | Verziózott tudáscsomagok hibakódokhoz, hardverazonosítókhoz, gyártókhoz és támogatási hivatkozásokhoz |
| Csomagolás | Telepítő: keretrendszerfüggő, a .NET Desktop Runtime 10 for Windows x64 szükséges hozzá. Hordozható: önálló, futtatókörnyezet telepítése nélkül |
| Megjelenés | 13 családi téma és 20 nyelv |

## Telepítés, mappák és adatok

A KillerBench kétféle formában érhető el. A telepítő a szokásos letöltés, és felhasználónkénti vagy az összes felhasználóra kiterjedő telepítést támogat. A hordozható kiadás saját .NET-futtatókörnyezetet tartalmaz, és telepítés nélkül fut.

A KillerBench az őt elindító fiók jogaival fut. Önmagának nem emel jogosultságot. Indítsa a **Futtatás rendszergazdaként** paranccsal, ha a védett bizonyítékokhoz, például a BitLocker-védőkhöz, vagy egy javításhoz rendszergazdai hozzáférés kell. Az emelt jogosultság soha nem választ ki és nem hagy jóvá javítást.

### Mit csinál a telepítő

A telepítő ellenőrzi és kicsomagolja az alkalmazást a `%LOCALAPPDATA%\Programs\KillerBench\` mappába, vagy az összes felhasználóhoz egy jogosultságemelési kérés után a `%ProgramFiles%\KillerBench\` mappába. Parancsikont ad a Start menühöz, opcionálisan az asztalhoz, valamint bejegyzést a Programok telepítése és törlése listához normál és csendes eltávolítással. A hordozható kiadás az aktuális futtatáshoz a `%LOCALAPPDATA%\KillerBench\Portable\` alá ellenőrzi és csomagolja ki a tartalmát, és a következő indításkor eltakarítja az elhagyott másolatokat.

### Hol mi található

| Hely | Tartalom |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Téma, kiemelőszín, felületméretezés és nyelv |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | Folyamatfuttatásonként egy JSON-napló: döntések, korlátozott parancskimenet és javítási eredmények |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | Végrehajtási naplók a karbantartási javításokhoz, például a DISM-hez és az SFC-hez |
| Funkciómappák a `%LOCALAPPDATA%\KillerBench\` alatt | Függő bejegyzések és eredmények az egyes védett műveletekhez, például `BitLockerBackup`, `Time`, `Firewall` és `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | A Névjegyben kiválasztott KillerMCP-javítási szint |
| `HKCU` vagy `HKLM\Software\KillerBench` | A telepítési útvonal a megfelelő telepítési hatókörhöz |
| Egy Ön által választott `.kbench` fájl | Egy mentett rögzítés a megőrzött bizonyítékokkal |

Semmi sem kerül feltöltésre. Egy naplót csak akkor töröljön, ha a bizonytalan eredményhez tartozó bizonyítékra már nincs szüksége.

## Először diagnózis

A KillerBench a diagnózist és a módosítást két külön feladatként kezeli. A diagnózis csak olvas. Módosítás csak akkor történik, ha Ön kiválaszt egy konkrét javítást vagy műveletet, áttekinti a beállításait és előfeltételeit, és megerősíti.

A teljes rendszervizsgálat kizárólag diagnosztikai, akkor is, ha egy célzott folyamatban be van kapcsolva az automatikus javítás. A mentett rögzítések, az importált naplók és a korábbi események soha nem jogosítanak fel javításra.

### Korlátozott gyűjtés

Minden natív parancs és Windows-szolgáltató rögzített idő-, rekord- és méretkorlátok között fut. Egy szolgáltató rögzíti a kilépési állapotát, az időtúllépést, a csonkolást, a szabványos kimenetet és a hibakimenetet. Egy sikertelen vagy csonkolt szolgáltató soha nem értelmezhető tiszta, üres válaszként.

### Minimális megőrzés

A gyűjtők azt őrzik meg, amire egy megállapításnak szüksége van. Sok eredmény megőrzi a neveket, a kategóriákat, a darabszámokat, az állapotokat és az időbélyegeket, ugyanakkor kihagyja a hitelesítő adatokat, a parancssorokat, a teljes útvonalakat, a fiókazonosítókat, a hálózati végpontokat és a nem kapcsolódó eseményadatokat.

## Követhető bizonyítékok

Minden megállapítás ahhoz a bizonyítékhoz kötődik, amelyből született, és a jelentés mindig elkülöníti a bizonyítottat attól, amit csak megfigyelt.

| Állapot | Jelentés |
| --- | --- |
| Áttekintendő | A bizonyíték figyelmet érdemlő problémát támaszt alá. |
| Információ | A bizonyíték összegyűjtésre került, és háttérként jelenik meg. |
| Hiányos vagy Ismeretlen | Egy ellenőrzés nem tudott befejeződni, vagy a bizonyíték nem volt elérhető. Soha nem számít egészségesnek. |
| Korábbi | Egy múltbeli esemény. Megmagyarázza az előzményeket, de nem bizonyít aktuális hibát. |
| Igazolva | Egy javítást vagy módosítást az eredmény visszaolvasása erősített meg. |

### Az Ismeretlen is eredmény

Az Ismeretlen azt jelenti, hogy a hozzáférés megtagadva, egy forrás hiányzott, egy szolgáltató meghiúsult, a kimenet hibás volt, egy korlát túllépésre került, vagy a bizonyítékok ellentmondtak egymásnak. A KillerBench megmutatja ezt a korlátot és a következő hasznos lépést, ahelyett hogy elrejtené.

### Az előzmény előzmény marad

Az eseménybejegyzések és az importált naplók azt írják le, mi lett rögzítve egy korábbi időpontban. Rámutathatnak egy hibakódra vagy a következő diagnosztikai hatókörre. Nem bizonyítják, hogy a hiba még fennáll.

## Védett javítások

A javítások típusos műveletek saját jogosultsági, végrehajtási, ellenőrzési és helyreállítási szabályokkal. Nem egy naplósorból kiemelt parancssztringek. Minden támogatott javítás ugyanazt a sorrendet követi:

1. Teljes aktuális bizonyíték gyűjtése.
2. Egyetlen támogatott recept egyeztetése.
3. A szükséges beállítások és következmények áttekintése.
4. A rendszergazdai és házirendi előfeltételek megerősítése.
5. A döntő bizonyíték újragyűjtése közvetlenül a módosítás előtt.
6. Tartós előkészítési bejegyzés írása.
7. Egyetlen korlátozott natív művelet futtatása.
8. Friss ellenőrzési bizonyíték gyűjtése.
9. Igazolt, letiltott, leállított vagy bizonytalan eredmény rögzítése.

Ha a döntő bizonyíték a művelet futása előtt megváltozik, a KillerBench a művelet végrehajtása nélkül leáll.

### Kifejezett választások

Azokhoz a javításokhoz, amelyek egy beállítástól függenek, pontos választás kell Öntől, például egy kötet, egy nyomtatási sor, a helyreállítási kulcs célja, egy energiagazdálkodási érték, egy csomagverzió vagy egy javítási forrás. A KillerBench soha nem talál ki ilyen értékeket.

### Felügyelt számítógépek

A csoportházirend és az eszközkezelés hatástalanná vagy helytelenné tehet egy helyi módosítást. A támogatott javítások megvizsgálják, ki a beállítás tulajdonosa, és leállnak, ha a tulajdonlás felügyelt vagy ismeretlen. A házirend tulajdonában lévő beállítások változatlanok maradnak.

### Automatikus javítás

Az automatikus javítás csak a jogosult recepteket engedi tovább. Nem kerüli meg a bizonyítékot, nem választ ki hiányzó beállításokat, nem hagyja figyelmen kívül a házirendet, nem próbál újra bizonytalan műveletet, nem indítja újra a Windowst, és nem alakít át egy áttekintést igénylő megállapítást javítássá.

## Naplók és megszakítások

Mielőtt a KillerBench módosítást kérne a Windowstól, leírja, mit készül tenni. Minden módosítás egy naplóba kerül a `%LOCALAPPDATA%\KillerBench` alatt.

A módosítás után a KillerBench visszaolvassa az eredményt, és csak akkor jelez **Igazolva** állapotot, ha a Windows a várt állapotot mutatja. Ha a Windows sikert jelez, de az állapot nem változott, az eredmény ezt közli, és semmi sem ismétlődik.

Egy folyamat leállhat azután, hogy a Windows megkapta a kérést, de mielőtt az ellenőrzés befejeződött volna. A függő bejegyzés megmarad, és egy későbbi egyeztetés friss állapotot olvas be, hogy kiderítse, mi történt. Az egyeztetés nem automatikus visszaállítás, és soha nem ismétli meg az eredeti módosítást. Bizonytalan eredmény esetén őrizze meg a naplót.

## Állandók és korlátok

| Jellemző | Érték |
| --- | --- |
| Diagnosztikai lefedettség | A teljes rendszervizsgálat 49 diagnosztikai területet futtat, és 96 védett javítási receptet ellenőriz |
| Szöveges forrás olvasása | Forrásonként 1 000 000 karakter, 2 000 egyező sor megjelenítve |
| Munkamenet-források | 12 az oldalsávban; egy rögzítés legfeljebb 24 forrást és 64 MB-ot tartalmaz |
| Eseménylekérdezések | Csatornánként a legújabb 200 esemény az elmúlt 7 napból |
| Windows Update vizsgálata | Forrásonként 15 másodperc, összesen két perc |
| Naplóvizsgálat | 10 000 feldolgozott bejegyzés, 256 különböző kód, kódonként 8 darab 320 karakteres részlet |
| Tömörített CBS-archívum | 256 MiB archívum, 512 MiB naplótag, 30 másodperces kibontás |
| Csak olvasó CHKDSK | Tíz perc; egy javítás 65 percre korlátozott |
| DNS- és TCP-ellenőrzések | Alapértelmezés szerint tíz másodperc, legfeljebb egy perc |
| Szolgáltatásleltár | Legfeljebb 1 024 telepített szolgáltatás |
| Tudáscsomagok | Egyszerre legfeljebb 32 nyitott |
| Nagy fájlok áttekintése | Legalább 100 MB-os fájlok |
| Karbantartási tevékenység figyelője | 1 és 60 másodperc közötti időköz, 1 és 120 közötti pillanatkép |
| Ütemezett feladatok vázlatai | 1 és 4 320 perc közötti végrehajtási korlát |
| Egyéni időforrások | Legfeljebb nyolc NTP-társ |
| Tevékenység exportálása | Legfeljebb 150 megőrzött bejegyzés |
| Nyelvek és témák | 20 nyelv, 13 téma kiemelőszín-változatokkal |

## Amit nem tesz meg

- Nem törli a TPM-et, nem töröl általa nem létrehozott védőket, és nem jelenít meg helyreállítási jelszavakat.
- Nem módosít csoportházirend vagy eszközkezelés által vezérelt beállítást.
- Nem futtat javítást csak azért, mert egy korábbi esemény vagy importált jelentés problémát említ.
- Nem alakítja át a hiányzó, részleges vagy sikertelen bizonyítékot egészséges ítéletté.

## Jelentések

Minden futtatás olyan jelentést készít, amelyet elolvashat, szűrhet és exportálhat. A jelentés elkülöníti:

1. Mit vizsgált meg
2. Milyen problémát talált
3. Milyen bizonyíték támasztja alá a megállapítást
4. Milyen műveletet javasolt vagy hajtott végre
5. Hogyan ellenőrizte az eredményt
6. Mi marad ismeretlen, vagy mi igényel újraindítást

A **Nyers kimenet** a technikai bizonyítékokat az eredeti sorszámokkal őrzi meg, így bármely megállapítás visszakövethető a forrásáig. Az **Olvasható jelentés** ugyanazt a futtatást közérthető nyelven foglalja össze, a következő biztonságos lépéssel kezdve. A HTML-jelentések a Killer Tools témarendszerét használják, és az alkalmazás nélkül is olvashatók maradnak. A szöveg a hordozhatóságot szolgálja, a JSON pedig strukturált mezőket őriz meg egy másik program számára.

## Mentett naplók és rögzítések

A KillerBench úgy is tud bizonyítékokat vizsgálni, hogy semmit sem futtat a gépen. Húzzon egy szöveges naplót az ablakra, vagy illessze be az elérési útját, és válassza a **Napló megnyitása** lehetőséget. A CBS-, CSI- és Setup-sorok az időbélyegeikkel együtt kerülnek feldolgozásra, és a megállapítások a pontos eredeti sorra hivatkoznak. Az EVTX- és az esemény-XML-fájlok ugyanígy importálhatók.

A **Windows Update vizsgálata** igény szerinti, csak olvasó gyűjtés: CBS-, DISM-, Panther- és Setup-visszaállítási naplók, frissítési és karbantartási eseménycsatornák, frissítési szolgáltatások állapota, szabad hely, üzemidő, függő újraindítási jelzők és kiválasztott frissítési házirend. A **Rögzítés mentése** egy `.kbench` fájlba írja a megőrzött bizonyítékokat. A **Rögzítés megnyitása** az eredeti fájlok nélkül nyitja meg újra, és a telepített szabályokkal ismét kiértékeli, így ugyanazok a szabályok ugyanazokat a megállapításokat adják.

## Bemutató mód

Indítsa el a `KillerBench.exe --demo` parancsot, ha egy kitalált munkaállomáson szeretné kipróbálni a **Diagnózis futtatása** funkciót. Minden folyamat szintetikus bizonyítékot és időzített előrehaladást hoz létre. Egyetlen Windows-szolgáltató sem kerül meghívásra, egyetlen javítóparancs sem fut, és egyetlen napló sem íródik. A **Diagnózis futtatása** körén kívüli külön eszközgombok élesek maradnak, ezért ne használja őket bemutató műveletként.

A `KillerBench.exe --demo-screenshot` egy csak megtekintésre szolgáló Teljes rendszervizsgálat-jelentést nyit meg egy rögzített, kitalált géphez. A munkaterületen belüli kattintások, a billentyűparancsok és a fájlok behúzása le vannak tiltva.

## Tudáscsomagok

A nagy méretű és gyakran frissülő referenciaadatok a diagnosztikai logika helyett tudáscsomagokban vannak: Windows- és Setup-hibakódok, gyártó- és eszközazonosítók, PCI-, USB-, ACPI- és UEFI-hivatkozások, hálózati OUI-gyártóadatok, illesztőprogram- és támogatási helyek, valamint válogatott támogatási hivatkozások.

Minden csomag azonosítja a formátumát, a sémaverzióját, a forrását, a forrás változatát, a létrehozás idejét és a tartalom kivonatát, a KillerBench pedig használat előtt ellenőrzi őket. Egy ismeretlen vagy érvénytelen csomag elérhetetlen marad, ahelyett hogy találgatásra térne át. Egy frissítés ideiglenes helyre töltődik le, ellenőrzésre kerül, majd egyetlen lépésben lecseréli az aktív csomagot, így egy sikertelen frissítés a korábbi csomagot a helyén hagyja.

A tudásadatok megmagyarázhatnak egy azonosítót, vagy javasolhatják, hol érdemes tovább keresni. Nem alakíthatnak át egy korábbi kódot aktuális hibává, nem választhatnak ki automatikusan illesztőprogramot, és nem jogosíthatnak fel javításra.

## Frissítések és aláírás

Indításkor a KillerBench négy másodperces korláttal lekérdezi a GitHubtól a legújabb stabil kiadást, és csak akkor kínál frissítést, ha az a kiadás újabb. Az ellenőrzést a Névjegy **Frissítések keresése indításkor** beállításával vagy a frissítési kérésben kapcsolhatja ki. A letöltés a futó példánynak megfelelő csomag, telepítő vagy hordozható, és az SHA-256 értékének egyeznie kell a kiadás `SHA256SUMS.txt` fájljával, mielőtt bármi települne. A befejezéshez az alkalmazás bezárja és újra megnyitja önmagát. Nincs háttérszolgáltatás.

A Névjegy kártya megmutatja a verziót, a kódaláíró kiadót és ujjlenyomatot, valamint a futó végrehajtható fájl SHA-256 értékét, így ellenőrizheti a meglévő példányát.

## KillerMCP

A KillerBench nem tartalmaz AI-ügynököt. A [KillerMCP](../mcp.html) képes egy ügynököt ugyanahhoz az Engine-hez kapcsolni. A Névjegy **KillerMCP beállítások** része határozza meg, mit tehet az ügynök:

| Beállítás | Hatás |
| --- | --- |
| Ki (alapértelmezett) | Az ügynök nem kérhet javítást. |
| Biztonságos javítások engedélyezése a KillerMCP számára | Az ügynök kérheti a biztonságosnak minősített javításokat. |
| Zavaró javítások engedélyezése is | A zavaró javítások is elérhetővé válnak, és mindegyik továbbra is jóváhagyást igényel. |

A szintet csak a számítógép előtt ülő személy emelheti. Bizonytalan javítási eredmény után a KillerMCP zárolást ír, és az ügynöki javítások letiltva maradnak, amíg valaki fel nem oldja a Névjegyben.

## Honosítás

A felület 20 nyelven érhető el: angol, bengáli, cseh, német, spanyol, francia, magyar, olasz, japán, kazah, norvég, lengyel, portugál (Brazília), orosz, szlovén, török, ukrán, vietnami, valamint egyszerűsített és hagyományos kínai. A nyelvváltás azonnal érvényes, beleértve a szakasz- és folyamatneveket, az üdvözlőszöveget és a megnyitott olvasható jelentést. A hiányzó szöveg angolul jelenik meg.

A bizonyítékok, az Engine megállapításai, a naplók és a Nyers kimenet angolul maradnak, akárcsak egy Windows-napló, így a jelentés minden gépen ugyanúgy olvasható, és pontos szövegre kereshető.

## Parancssor és kilépési kódok

A `killerbench-cli` az asztali alkalmazás nélkül futtatja ugyanazt az Engine-t. Az előrehaladás a szabványos hibakimenetre, a JSON a szabványos kimenetre kerül. A legtöbb parancs alapértelmezés szerint előnézetet készít, és a módosításhoz `--execute` szükséges. A megfelelő `-recover` parancsok megismétlés nélkül tekintik át a megszakított műveletet. Lásd: [A KillerBench futtatása szkriptekből](use-the-cli.html) és a [parancsreferencia](cli.html).

| Kilépési kód | Folyamatparancsok | Vizsgálat és megnyitás |
| --- | --- | --- |
| 0 | Nincs megoldatlan probléma az ellenőrzött hatókörökben | Teljes lefedettség |
| 1 | Indítási vagy naplóhiba | Hiba |
| 2 | Érvénytelen argumentumok | Érvénytelen argumentumok |
| 3 | Figyelmet igényel | Részleges gyűjtés |
| 4 | Az ellenőrzés előtt újraindítás szükséges | |
| 130 | Leállítva | Megszakítva vagy teljes időtúllépés |

Egyetlen kilépési kód sem tanúsítja, hogy egy számítógép egészséges.

## Architektúra

Az Engine három felhasználói felület nélküli projektre oszlik. Az `Engine.Core` biztosítja az alapelemeket és a deklaratív recepteket, az `Engine` felel a közös folyamatokért és bizonyítékokért, az `Engine.Windows` pedig regisztrálja a natív szolgáltatókat. Az asztali alkalmazás, a CLI és a tesztek külön gazdafolyamatok.

| Réteg | Feladat |
| --- | --- |
| Képességgyűjtők | Korlátozott Windows-állapotot olvasnak be egyetlen pontos hatókörhöz |
| Bizonyítékelemzők | Ellenőrzik az alakot, az azonosságot, a teljességet és a korlátokat |
| Diagnosztikai értelmezés | A bizonyítékot kifejezett folyamatállapotokká alakítja |
| Folyamatvezérlés | Összekapcsolja az ellenőrzéseket, az előrehaladást, a recepteket és a naplókat |
| Javításkoordinátorok | Újra ellenőrzik a jogosultságot, előkészítenek, módosítanak, ellenőriznek és egyeztetnek |
| Jelentések | Bemutatják a problémákat, a műveleteket, az eredményeket és a korlátokat |
| Tudáscsomagok | A logika módosítása nélkül oldják fel a verziózott referenciaadatokat |

A gyűjtők bizonytalanság esetén tiltanak, ha az azonosság nem egyértelmű, a kimenet hibás, egy korlát túllépésre kerül, vagy a szükséges jogosultság nem vizsgálható. Az Engine jelenleg a Windowst célozza, és nem platformfüggetlen. Az [Engine fejlesztői útmutató](../engine/index.html) bemutatja az API-t, a CLI-t és az Engine beágyazását egy másik .NET-gazdafolyamatba.

## Adatvédelem és hálózat

A diagnosztika és a naplóelemzés helyben fut, AI-szolgáltatás és KillerBench-fiók nélkül. A naplók és a megőrzött bizonyítékok a helyi fiók `%LOCALAPPDATA%\KillerBench` mappáiban maradnak.

Az Ön által kiválasztott műveletek kapcsolódhatnak a céljukhoz, például a Windows Update-hez, szoftverletöltési szolgáltatásokhoz, vagy a helyreállítási kulcs Active Directoryba vagy Entra ID-ba való mentéséhez. Az indításkori frissítéskeresés a GitHubhoz kapcsolódik, a tudásfrissítések és a KillerMCP-telepítő pedig a beállított szolgáltatásaikhoz. A [KillerMCP](../mcp.html) szolgáltatást használó AI-ügyfél megkaphatja a kért referenciaeredményt.

A nyers naplók, az importált fájlok és az exportált bizonyítékok számítógépneveket, fiókokat, útvonalakat vagy bizalmas forrásszöveget tartalmazhatnak. Megosztás előtt tekintse át őket, és soha ne tegyen helyreállítási kulcsot, hitelesítő adatot vagy ügyféladatot nyilvános jelentésbe.

## Fejlesztési állapot

A KillerBench fejlesztés alatt áll, és nyilvános letöltés még nem érhető el. A javítások köre még bővül, és a javítások natív ellenőrzése még folyamatban van. Egy sikeres build vagy szimulált ellenőrzés nem igazolja a javítás natív viselkedését. Az egyes natív módosítások különböző Windows-verziókon és hibaállapotokban történő ellenőrzésére eldobható Windows virtuális gépek szolgálnak. A teljes biztonsági modellt a [Hogyan maradnak biztonságosak a módosítások](safety.html) oldal ismerteti.

## Szószedet

Az ezen az oldalon használt fogalmak közérthető meghatározása, betűrendben.

| Fogalom | Jelentés |
| --- | --- |
| BITS | Intelligens háttérátviteli szolgáltatás, az a Windows-szolgáltatás, amely a háttérben tölti le a frissítéseket és más fájlokat. |
| Bizonyíték | Amit a KillerBench ténylegesen beolvasott a Windowsból, a forrásával együtt megőrizve, hogy egy megállapítás visszakövethető legyen. |
| CBS | Component-Based Servicing, a Windows frissítéseket és szolgáltatásokat telepítő alrendszere. Naplója a `CBS.log`. |
| DISM | Deployment Image Servicing and Management, az összetevőtárat ellenőrző és javító Windows-eszköz. |
| Egyeztetés | Friss állapot beolvasása egy megszakítás után annak megállapítására, mi történt, a módosítás megismétlése nélkül. |
| Emelt jogosultság | Rendszergazdai jogokkal való futtatás egy Windows-kérés után. A KillerBench soha nem emel jogosultságot önmagának. |
| Folyamat | Egy eszköz egyetlen futtatása: az általa gyűjtött ellenőrzések, a használható receptek és az elkészített jelentés. |
| Függő bejegyzés | Az a bejegyzés, amely jelzi, hogy egy módosítás talán elküldésre került a Windowsnak. Megakadályozza az ismétlést, amíg az egyeztetés be nem olvassa az eredményt. |
| Házirend-tulajdonos | A beállítást vezérlő csoportházirend vagy eszközkezelés. A KillerBench a tulajdonolt beállításokat nem módosítja. |
| Igazolva | Az eredményt visszaolvasta a Windowsból, és egyezett azzal, amit a módosításnak el kellett érnie. |
| Napló | Helyi JSON-bejegyzés, amely a módosítás előtt készül és utána frissül, hogy egy megszakított művelet később áttekinthető legyen. |
| Panther | Az a mappa, amelybe a Windows telepítője a frissítési naplóit írja. |
| Recept | Típusos javítás saját jogosultsági, végrehajtási, ellenőrzési és helyreállítási szabályokkal. |
| Rögzítés | Mentett `.kbench` fájl a megőrzött bizonyítékokkal, amely az eredeti számítógép nélkül is újra áttekinthető. |
| SFC | System File Checker, a védett rendszerfájlokat ellenőrző és helyreállító Windows-eszköz. |
| Tiltás bizonytalanságnál | Ha a bizonyíték nem egyértelmű, hiányzik vagy túl nagy, az eredmény Ismeretlen, és semmilyen módosítás nem engedélyezett. |
| TPM | Trusted Platform Module, a biztonsági lapka, amelyet a BitLocker a meghajtókulcs védelmére használ. |
| Tudáscsomag | Verziózott, ellenőrzött referenciaadat-fájl, például hibakódokkal vagy hardverazonosítókkal. |
