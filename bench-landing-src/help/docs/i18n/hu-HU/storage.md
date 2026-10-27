# Tárolás

Lemezek, kötetek, Tárolóhelyek és meghajtó-karbantartási szolgáltatások vizsgálata. A diagnózis nem választ ki lemezműveletet; minden támogatott, gépet módosító művelethez saját pontos cél, előfeltételek és megerősítés szükséges.

Lépésenkénti útmutatók: [Meghajtó ellenőrzése és javítása](check-a-drive.html) és [Lemezterület felszabadítása](free-disk-space.html).

## Az ebben a szakaszban található eszközök

### Tárolás

Feltérképezi a korlátozott lemez-, partíció- és meghajtóbetűjeles kötetazonosítókat, minden meghajtóbetűjeles köteten kiértékeli a 10%-os szabadhely-szűkösséget, megvizsgálja a VSS árnyékmásolat-tárolójának helyfoglalását és a kötetenkénti kvóta-alapértékeket, összefoglalja a Storage modul aktuális feladatainak előrehaladását, ellenőrzi a kiválasztott NTFS-kötetet és annak online öngyógyítási állapotát, és megvizsgálja a PC lemez- és vezérlőeszközeit. A kapacitásküszöbök, a kvótabeállítások és a feladatállapotok áttekintendő bizonyítékok, és nem választanak ki tisztítást, átméretezést, kvótamódosítást vagy feladatvezérlést. A meghajtóbetűk hozzárendelése pillanatnyi állapot, és nem fed le minden csatlakoztatási útvonalat. Az eszközmegállapítások nincsenek a kiválasztott kötethez rendelve. Az alapértelmezett a Windows-kötet. Semmilyen fájl, pillanatkép, feladat vagy illesztőprogram nem változik, semmilyen beállítás nem módosul, egyetlen kötet sem válik le, és nincs ütemezett újraindítás.

### Tárolóhelyek

Leltárba veszi a nem primordiális Tárolóhelyek-készleteket és virtuális lemezeket, beleértve az állapotot, a működési állapotot, a kapacitást, a helyfoglalást és a rugalmasságot, majd egyetlen korlátozott működési következtetést ad. A Windows igény szerint elindíthatja a Tárolóhelyek felügyeleti szolgáltatóját, miközben megválaszolja ezt a csak olvasó lekérdezést. A készletek, a virtuális lemezek, a fizikai lemezek, a kötetek, a rugalmasság, a helyfoglalás, a karbantartási beállítások, a házirend és az indítási típus megmarad. Nincs kérés készlet-, lemez-, kötet-, helyfoglalási, csatolási, javítási, optimalizálási vagy beállítási műveletre. A szolgáltatói eredmények nem igazolják a készlet elérhetőségét, a redundanciát, az adatok integritását, a biztonsági mentések helyreállíthatóságát vagy az alkalmazások I/O-műveleteit.

### Tárolási szolgáltatás

Áttekinti a Storage modul korlátozott aktuális feladatait, a szolgáltatás pontos hibaelőzményeit és a Tárolási szolgáltatás aktuális készenlétét. A hiányos bizonyíték elsőbbséget élvez. A feladatállapot és a korábbi hibák nem jogosítanak fel szolgáltatás-helyreállításra vagy tárolási módosításokra. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott, engedélyezett és leállított szolgáltatást indítja el. A meglévő tárolási értesítések és a házirend által vezérelt tárolási tevékenység folytatódhat. A kötetek, a partíciók, a fájlrendszerek, a csatlakoztatási pontok, az eszközök, az illesztőprogramok, a Tárolási érzékelő beállításai, a karbantartási házirend, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench nem választ ki feladat-, kötet-, partíció-, csatlakoztatásipont-, fájl-, eszköz-, illesztőprogram- vagy tárolásibeállítás-műveletet. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a feladatok eredményére vagy az eszköz-, kötet- és tárolásiházirend-műveletekre.

### Meghajtóoptimalizálási szolgáltatás

Ellenőrzi a Meghajtóoptimalizálási szolgáltatás elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott, engedélyezett és leállított szolgáltatást indítja el. A Windows által ütemezett meglévő optimalizálási tevékenység az aktuális karbantartási házirend szerint folytatódhat. A kötetek, a fájlrendszerek, az adatok, az optimalizálási ütemezések, a karbantartási házirend, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench nem választ ki kötetet, és egyetlen meghajtót sem elemez, trimel, töredezettségmentesít, optimalizál vagy ütemez. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem az elemzésre, az optimalizálásra vagy az eszköz állapotára.
