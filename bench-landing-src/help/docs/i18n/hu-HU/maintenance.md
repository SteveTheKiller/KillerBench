# Karbantartás

A Karbantartás rendben tartja a gépet: fájltisztítás, régi felhasználói profilok, ütemezett feladatok és a Windows rendbetétele. Minden eszköz előnézetben mutatja, mit távolítana el, mielőtt bármi törlődne.

Lépésenkénti útmutatók: [Lemezterület felszabadítása](free-disk-space.html) és [A Windows adatvédelmének erősítése](make-windows-private.html).

## Az ebben a szakaszban található eszközök

Fájlok, profilok és ütemezett feladatok tisztítása, valamint a Windows rendbetétele.

### Tisztítás

A fő panel kor- és fájlkorlát-beállításai alapján megbecsüli a kiválasztott ideiglenes fájlok, gyorsítótárak és megőrzött diagnosztikai bizonyítékok céljait. A Windows Temp és a Windows SystemTemp független célok. A Kézbesítésoptimalizálás külön gyorsítótár-választás, és az eltávolított tartalmat lehet, hogy újra le kell tölteni. Az opcionális végleges eltávolítás a külön tisztítási jelölőnégyzetet használja, a kiválasztott korláton belül újra ellenőrzi a megőrzött jelölteket, és minden eredményt rögzít. A törlés megkerüli a Lomtárat. A célok, a kor és a fájlkorlátok a cleanup CLI-paranccsal is beállíthatók.

### Profilok

A kiválasztott korküszöb, a pontos ProfileList-regisztrációk, valamint az opcionális szolgáltatás- és ütemezettfeladat-fiókfüggőségek alapján áttekinti a Windows-profilokat, majd egyetlen korlátozott áttekintési következtetést ad. A betöltött, a rendszer- és az aktuális fiókhoz tartozó profilok nem vesznek részt az áttekintésben. A jelöltek csak áttekintésre szolgálnak. Egyetlen profil, mappa, beállításkulcs, fiók, szolgáltatás, feladat, hitelesítő adat, házirend vagy fájl sem változik, és nincs eltávolítás kiválasztva. Az alkalmazásfüggőségek és a biztonsági mentések helyreállíthatósága ellenőrizetlen marad.

### Ütemezett feladatok

Leltárba veszi a látható ütemezett feladatokat, állapotukat és a jelentett rendszerbiztonsági tagokat. Egyetlen feladat sem indul el, nem regisztrálódik és nem változik. A műveletek, az eseményindítók és a végrehajtás sikere nincs ellenőrizve.

### Windows rendbetétele

Áttekinti a Windows kifejezetten kiválasztott tisztítási területeit, például a Recallt és az AI-t, a telemetriát, a keresési javaslatokat, a fogyasztói tartalmakat, a tálcát és a Start menüt, az értesítéseket, az Edge-et, az energiagazdálkodást, a hibernálást, az Office nyelveit és az előre telepített szoftvereket a futó fiókra vagy az összes profilra vonatkozóan. Az áttekintés csak olvas. A nagy teljesítményű energiaséma, az alvó állapot és a kijelző időzítői, valamint a hibernálás egyenként módosítható, ha az adott terület az egyetlen kiválasztás, friss bizonyítékkal és kifejezett megerősítéssel.
