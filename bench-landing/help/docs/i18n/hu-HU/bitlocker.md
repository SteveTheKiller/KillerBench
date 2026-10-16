# BitLocker

A BitLocker megvizsgálja a meghajtótitkosítást, a védőket és a helyreállítási információkat, majd segít egy meghajtó mentésében, védelmében vagy módosításában. Csak ezen a számítógépen működik.

Lépésenkénti útmutatók: [Meghajtó titkosítása BitLockerrel](encrypt-with-bitlocker.html).

## A BitLocker eszköz használata

Válassza ki a meghajtóbetűjelet és egy **Művelet** értéket, majd nyomja meg a **Diagnózis futtatása** gombot.

- A **Vizsgálat** beolvassa a titkosítás állapotát, a védelem állapotát és a védők azonosítóit. Ez az alapértelmezett, és semmit sem változtat.
- A **Helyreállítás mentése** a kiválasztott helyreállítási jelszót egy új fájlba írja a védett meghajtón kívül, és ellenőrzi. Előbb nyomja meg a **Helyreállítási védők keresése** gombot, válassza ki a védőt, majd erősítse meg a mentést.
- A **Titkosítás** menti a kiválasztott helyreállítási jelszót, ellenőrzi a mentést, majd egyszer elindítja a titkosítást a választott módszerrel. A Windows-meghajtónak TPM-védőre és helyreállítási jelszóra van szüksége. Egy adatmeghajtóhoz csak helyreállítási jelszó kell.
- A **Védelem folytatása** visszakapcsolja a védelmet egy teljesen titkosított meghajtón, ahol fel volt függesztve.

Mindegyikhez saját megerősítő jelölőnégyzet tartozik, és a KillerBench közvetlenül a művelet előtt újra ellenőrzi a meghajtót.

## További BitLocker-műveletek

A **Diagnózis futtatása** körén kívül eső módosításokhoz nyissa meg a **További BitLocker-műveletek** panelt. Válasszon egy műveletet, az **Előnézet** gombbal olvassa el, pontosan mit igényel, majd erősítse meg, és nyomja meg a **Művelet futtatása** gombot.

- Az **Automatikus feloldás bekapcsolása** gondoskodik arról, hogy egy titkosított adatmeghajtó a Windows indításakor feloldódjon. A Windows-meghajtónak teljesen titkosítottnak kell lennie bekapcsolt védelemmel.
- A **TPM-védő hozzáadása** vagy a **Helyreállítási jelszó hozzáadása** egy új védőt hoz létre. A KillerBench új helyreállítási jelszót soha nem jelenít meg és nem naplóz, ezért azonnal készítsen róla biztonsági mentést.
- A **Helyreállítási jelszó mentése az Active Directoryba** vagy a **Microsoft Entra ID** a fent kiválasztott védőt küldi el. A KillerBench jelzi, hogy a Windows elfogadta a kérést. A címtárbeli másolatot nem tudja beolvasni, ezért ott ellenőrizze.
- **A meghajtó visszafejtése** elindítja a visszafejtést, például mielőtt egy előírt módszerrel újra titkosítana. Második megerősítés szükséges hozzá, mert a meghajtó utána védelem nélkül marad, és elutasítja a Windows-meghajtót, amíg más meghajtók automatikusan arról oldódnak fel.

## TPM és FIPS

Ha a TPM jelen van, de nem kész, a Biztonság szakasz törlés nélkül elő tudja készíteni. A KillerBench soha nem törli a TPM-et, mert az megsemmisíti a BitLocker és a Windows Hello által tárolt kulcsokat. A FIPS-algoritmusházirend kapcsolója kifejezett, visszafordítható javításként érhető el azoknak a szervezeteknek, amelyeknek FIPS által jóváhagyott BitLocker PIN-kódokra van szükségük.

## Az ebben a szakaszban található eszközök

A titkosítás, a védők és a helyreállítási információk vizsgálata, majd mentésük vagy a meghajtó védelme.

### BitLocker

Előnézetben mutatja a BitLocker beállításait, és megvizsgálja a kiválasztott kötetet. Ez a folyamat nem végez titkosítást vagy helyreállítási mentést.
