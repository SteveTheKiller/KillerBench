# Az első diagnózis

Kezdje a [Letöltés és beállítás](setup.html) oldallal, majd futtasson egy Teljes rendszervizsgálatot. Ez diagnosztikai bizonyítékokat gyűjt, és helyi naplót ment; javításokat nem futtat. Egy megállapítás a vizsgált hatókört írja le, nem a számítógép összes lehetséges hibáját.

## A vizsgálat futtatása

1. Nyissa meg a KillerBenchet. A **Diagnosztika** szakasz nyílik meg a **Teljes rendszervizsgálat** kiválasztásával.
2. Az első futtatáskor hagyja kikapcsolva a **Megerősített problémák automatikus javítása** beállítást.
3. Nyomja meg a **Diagnózis futtatása** gombot. Az egyes ellenőrzések futása közben az előrehaladás jobb oldalt, a **Nyers kimenet** nézetben jelenik meg.
4. Amikor befejeződik, a kimeneti panel átvált az **Olvasható jelentés** nézetre.

Kezdje emelt jogosultság nélkül. Bizonyos Windows-bizonyítékokhoz, például a BitLocker-védőkhöz és a védett szolgáltatásokhoz, rendszergazdai jogok kellenek. Ezek az ellenőrzések normál fiókkal nem elérhető bizonyítékot jelentenek. Ha szüksége van erre a bizonyítékra, fejezze be az aktuális futtatást, őrizze meg a jelentést, majd szándékosan nyissa meg újra a KillerBenchet a **Futtatás rendszergazdaként** paranccsal. Az emelt jogosultság nem választ ki és nem hagy jóvá javítást.

## A jelentés olvasása

A jelentés az **Áttekintendő**, az **Ellenőrzött javítások**, a **Hiányos** és az **Ellenőrizve** darabszámaival kezdődik, majd egy javasolt következő lépés következik.

- A **Mi igényel figyelmet** a bizonyítékokkal alátámasztott problémákat sorolja fel, a legfontosabbal kezdve.
- Az **Összegyűjtött információk** azt mutatja, mit olvasott be, beleértve a hiányos vagy nem elérhető bizonyítékokat is.
- A korábbi események előzményként vannak megjelölve. Egy múlt havi hiba nem számít aktuális hibának.

A hiányzó bizonyíték soha nem számít egészséges állapotnak. Ha egy ellenőrzés nem futhatott le, a jelentés ezt közli.

## Ellenőrzések saját kiválasztása

A teljes vizsgálat helyett az **Ellenőrzések kiválasztása (speciális)** megnyitásával egyenként is kiválaszthatja az ellenőrzéseket. Az alkalmazás minden szakaszában célzott eszközök is vannak, amelyeket az útmutató **Szakaszok** oldalai ismertetnek.

## A talált problémák javítása

Ha a jelentés javítást javasol, megnevezi a megnyitandó szakaszt és eszközt. Az első javítás előtt olvassa el a [Hogyan maradnak biztonságosak a módosítások](safety.html) oldalt. A natív ellenőrzés még folyamatban van; a sikeres diagnózis nem igazolja, hogy minden felkínált javítást teszteltek az Ön Windows-buildjén. Minden módosítás előtt legyen biztonsági mentése, és tekintse át a pontos célt és a következményeket.
