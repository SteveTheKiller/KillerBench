# Meghajtó titkosítása BitLockerrel

Ezzel kapcsolhatja be a BitLockert, készíthet biztonsági mentést a helyreállítási kulcsról, és győződhet meg arról, hogy a meghajtó védett. A KillerBench a titkosítás megkezdése előtt menti és ellenőrzi a helyreállítási jelszót, és helyreállítási jelszót soha nem jelenít meg és nem naplóz.

## Mielőtt elkezdené

- Futtassa a KillerBenchet rendszergazdaként.
- A Windows-meghajtónak TPM-védőre és helyreállítási jelszó típusú védőre van szüksége. Egy adatmeghajtóhoz csak helyreállítási jelszó kell.
- Döntse el, hová kerüljön a helyreállítási kulcs mentése: egy fájlba a titkosítandó meghajtón kívül, és opcionálisan az Active Directoryba vagy a Microsoft Entra ID-ba.

## A meghajtó ellenőrzése

1. Nyissa meg a **BitLocker** szakaszt, és válassza ki a meghajtót a **Kötet** mezőben.
2. Hagyja a műveletet **Vizsgálat** értéken, és nyomja meg a **Diagnózis futtatása** gombot. A jelentés megmutatja a titkosítás állapotát, a védelem állapotát és a védők azonosítóit.
3. Ha a TPM jelen van, de nem kész, előbb készítse elő a **Biztonság** szakaszban. A KillerBench soha nem törli a TPM-et.

## A szükséges védők hozzáadása

1. Nyissa meg a **További BitLocker-műveletek** panelt.
2. Válassza a **TPM-védő hozzáadása** vagy a **Helyreállítási jelszó hozzáadása** lehetőséget, nyomja meg az **Előnézet** gombot, jelölje be **A módosítás végrehajtása a kiválasztott meghajtón, amikor a Művelet futtatása gombra kattintok** beállítást, majd nyomja meg a **Művelet futtatása** gombot.
3. Az új helyreállítási jelszóról azonnal készítsen biztonsági mentést. A KillerBench nem jeleníti meg.

## A helyreállítási kulcs mentése és a titkosítás

1. Nyomja meg a **Helyreállítási védők keresése** gombot, és válassza ki a helyreállítási jelszó típusú védőt.
2. Adja meg a mentési fájl teljes elérési útját a titkosítandó meghajtón kívül.
3. Válassza a **Titkosítás** lehetőséget, válassza ki a titkosítási módszert, és jelölje be **A kiválasztott helyreállítási kulcs mentése, majd a titkosítás indítása, amikor a Diagnózis futtatása lehetőséget választom** beállítást.
4. Nyomja meg a **Diagnózis futtatása** gombot. A KillerBench megírja a mentést, ellenőrzi, majd egyszer elindítja a titkosítást.
5. A titkosítás a háttérben folytatódik. Később futtassa a **Vizsgálat** műveletet annak megerősítéséhez, hogy a meghajtó teljesen titkosított, és a védelem be van kapcsolva.

## Mentés az Active Directoryba vagy az Entra ID-ba

A **További BitLocker-műveletek** panelen válassza a kijelölt védő címtárba mentését. A KillerBench jelzi, hogy a Windows elfogadta a kérést, de a címtárbeli másolatot nem tudja beolvasni, ezért ellenőrizze azt az Active Directoryban vagy az Entra felügyeleti központban.

## Adatmeghajtók és automatikus feloldás

Egy adatmeghajtó titkosítása után az **Automatikus feloldás bekapcsolása** gondoskodik arról, hogy a meghajtó a Windows indításakor feloldódjon. A Windows-meghajtónak már teljesen titkosítottnak kell lennie bekapcsolt védelemmel.

## Parancssorból

```powershell
killerbench-cli bitlocker-protectors C:
killerbench-cli bitlocker-protector-add C: recovery --execute
killerbench-cli bitlocker-encrypt C: RECOVERY_PROTECTOR_GUID D:\Keys\C-recovery.txt --execute
killerbench-cli bitlocker-escrow C: RECOVERY_PROTECTOR_GUID entra --execute
```

Lásd: [bitlocker-protectors](cli.html#cmd-bitlocker-protectors), [bitlocker-protector-add](cli.html#cmd-bitlocker-protector-add), [bitlocker-encrypt](cli.html#cmd-bitlocker-encrypt) és [bitlocker-escrow](cli.html#cmd-bitlocker-escrow). Mindegyikhez tartozik egy `-recover` parancs a megszakított kérésekhez.

## Kapcsolódó

- [BitLocker szakasz](bitlocker.html)
- [Biztonság szakasz](security.html)
- [Hogyan maradnak biztonságosak a módosítások](safety.html)
