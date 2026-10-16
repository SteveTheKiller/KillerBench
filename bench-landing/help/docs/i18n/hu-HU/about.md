# A KillerBench névjegye

A KillerBench Steve the Killer Windows diagnosztikai, javító- és karbantartóeszköze, a [Killer Tools család](https://killertools.net) tagja. A 0.1.0 verzió még nem jelent meg. Az asztali alkalmazás, az Engine és a CLI közösen használja a diagnosztikai bizonyítékokat és a támogatott javítási folyamatokat.

## Elérhetőség és hatókör

Nyilvános KillerBench-letöltés vagy Engine-csomag még nem érhető el. A [Letöltés és beállítás](setup.html) a jelenlegi Windows x64-célt és a Desktop Runtime követelményeit ismerteti. A javítások natív ellenőrzése még folyamatban van; egy eszköz megléte nem bizonyítja, hogy minden hibát, megszakítást vagy Windows-konfigurációt teszteltek.

## Forráskód és terjesztési közlemények

A KillerBench a **GPLv3** licenc alatt érhető el. A terjesztési licence a [GNU General Public License 3. verziója](../license.txt). A licenc leírja a hatálya alá tartozó mű másolásának, módosításának és terjesztésének engedélyeit és feltételeit.

A nyilvános [KillerBench-tárház](https://github.com/SteveTheKiller/KillerBench) jelenleg projektinformációkat tartalmaz. A teljes megvalósítás ott nyilvánosan nem érhető el. Ezek a fejlesztői útmutatók egy jogosult forráskód-példányt írnak le; nem biztosítanak nyilvános forráskód-letöltést.

Egy bináris fájl terjesztése előtt a kiadójának a [GPLv3 6. szakasza](https://www.gnu.org/licenses/gpl-3.0.en.html#section6) által megengedett módon kell biztosítania a megfelelő forráskódot, beleértve a licenc által megkövetelt forráskódot és buildanyagokat is. Az első nyilvános csomag forráskód-átadási utasításai és a mellékelt harmadik féltől származó közlemények még függőben vannak. A GPLv3 nem ad engedélyt harmadik féltől származó betűtípusok továbbterjesztésére; a jelenlegi betűtípus-engedélyeket még ellenőrizni kell. Továbbterjesztés előtt tekintse át a közzétett csomag licencét, forráskód-utasításait és közleményeit.

## Adatvédelem és külső szolgáltatások

A diagnosztika és a naplóelemzés helyben fut, AI-szolgáltatás és KillerBench-fiók nélkül. A naplók és a megőrzött bizonyítékok a helyi fiók `%LOCALAPPDATA%\KillerBench` mappáit használják. A kiválasztott műveletek kapcsolódhatnak a Windows Update-hez, szoftverletöltési szolgáltatásokhoz, címtárakhoz vagy más megnevezett célokhoz. Az indításkori frissítéskeresés a GitHubhoz kapcsolódik, a tudásfrissítések és az MCP-telepítő oldala pedig a beállított szolgáltatásaikhoz. Az MCP-t használó AI-ügyfél megkaphatja a visszaadott referenciaeredményt.

Ez a webhely a böngésző helyi tárolójában tárolja a téma és a kiemelőszín kiválasztását. Az útmutatói nem futtatnak diagnózist az Ön számítógépén. A nyers naplók, az importált fájlok és az exportált bizonyítékok számítógépneveket, fiókokat, útvonalakat vagy bizalmas forrásszöveget tartalmazhatnak. Megosztás előtt tekintse át őket; nyilvános jelentésbe ne kerüljön helyreállítási kulcs, hitelesítő adat vagy ügyféladat.

## Támogatás

A projektinformációk a nyilvános tárházban érhetők el. Támogatásért használja [a szerző webhelyén](https://thekiller.net) közzétett elérhetőségeket; adja meg az alkalmazás verzióját, a Windows-buildet, a pontos eszközt, a várt eredményt és a tapasztalt eredményt. Küldés előtt tekintse át és takarja ki a bizonyítékokat, ügyféladatokhoz pedig jóváhagyott privát csatornát használjon. Bizonytalan javítási eredmény esetén őrizze meg a naplókat.

## Kapcsolódó útmutatók

- [Hogyan maradnak biztonságosak a módosítások](safety.html)
- [Jelentések és bizonyítékok](reports.html)
- [Engine fejlesztői útmutató](../engine/)
- [KillerMCP](../mcp.html)
