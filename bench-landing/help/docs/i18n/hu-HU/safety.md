# Hogyan maradnak biztonságosak a módosítások

A KillerBench a diagnózist és a módosítást két külön feladatként kezeli. A diagnózis csak olvas. A teljes rendszervizsgálat soha nem futtat javítást. Válasszon ki egy konkrét javítást vagy műveletet, tekintse át a beállításait és előfeltételeit, majd erősítse meg a javasolt módosítást.

## Módosítás előtt

1. **Friss bizonyíték.** Az eszköz újra beolvassa az aktuális állapotot. Mentett jelentés vagy régi esemény soha nem jogosít fel módosításra.
2. **Pontos jogosultság.** A módosítás csak akkor jelenik meg, ha a bizonyíték pontosan arra a problémára illik, amelyet kijavít. Bármi hiányos letiltja.
3. **Az Ön megerősítése.** Tekintse át a kiválasztott műveletet a megerősítő párbeszédpanelen. Egyes műveletekhez, például egy meghajtó visszafejtéséhez, második megerősítés kell. A párbeszédpanel az alkalmazás aktív témáját követi; a megjelenése nem változtat a művelet követelményein.
4. **Előfeltételek.** A rendszer ellenőrzi a rendszergazdai jogokat, az újraindítási állapotot, valamint azt, hogy a beállítás házirend vagy eszközkezelés tulajdonában van-e. A házirend tulajdonában lévő beállítások változatlanok maradnak.
5. **Függő bejegyzés.** A KillerBench leírja, mit készül tenni, mielőtt erre megkéri a Windowst.
6. **Végső újraellenőrzés.** A kérés előtt közvetlenül minden újra ellenőrzésre kerül, így egy időközbeni változás leállítja.

## Módosítás után

- A KillerBench visszaolvassa az eredményt, és csak akkor jelez **Igazolva** állapotot, ha a Windows a várt állapotot mutatja.
- Ha a Windows sikert jelez, de az állapot nem változott, az eredmény ezt közli, és semmi sem ismétlődik.
- Minden módosítás egy naplóba kerül a `%LOCALAPPDATA%\KillerBench` alatt.

## Ha valami megszakad

A támogatott, naplózott műveletek megtartják a függő bejegyzést, ha egy kérés elérhette a Windowst, és az ellenőrzés nem fejeződött be. A bejegyzés vizsgálatához használja a művelet dokumentált egyeztetési útvonalát vagy a megfelelő `-recover` parancsot. Az egyeztetés nem automatikus visszaállítás. Bizonytalan eredmény esetén őrizze meg a naplót, és ne ismételje meg kézzel a módosítást. Az összeomlás, a megszakítás és a későbbi rendszerindítás utáni viselkedés műveletenként még natív ellenőrzést igényel.

## Amit a KillerBench nem tesz meg

- Nem törli a TPM-et, nem töröl általa nem létrehozott védőket, és nem jelenít meg helyreállítási jelszavakat.
- Nem módosít csoportházirend vagy eszközkezelés által vezérelt beállítást.
- Nem futtat javítást csak azért, mert egy korábbi esemény vagy importált jelentés problémát említ.

A diagnosztika és a jelentéselemzés helyben fut. Az Ön által kiválasztott művelet külső célhoz is kapcsolódhat, például szoftverletöltéshez vagy a helyreállítási kulcs Active Directoryba vagy Entra ID-ba való mentéséhez. Megerősítés előtt tekintse át a célt. A mentett rögzítések és a nyers bizonyítékok tartalmazhatják a gép azonosítóit és forrásútvonalait, ezért megosztás előtt tekintse át őket.

## Fejlesztési állapot

A javítások köre még bővül. Egy sikeres build vagy szimulált ellenőrzés nem igazolja a javítás natív viselkedését.
