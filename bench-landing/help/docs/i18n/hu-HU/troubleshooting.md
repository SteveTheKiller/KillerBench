# Hibaelhárítás

## Egy művelet letiltottként jelenik meg

A letiltott eredmény azt jelenti, hogy a kért művelet nem folytatódhatott. Az üzenetből és a naplóból derítse ki, hogy a kérés elküldése előtt vagy egy korábbi lépés után állt-e le. Ne feltételezze, hogy egy részben befejezett műveletet visszaállítottak. A leggyakoribb előfeltétel-okok alább olvashatók.

- **Rendszergazdai hozzáférés szükséges.** Zárja be a KillerBenchet, és indítsa újra a **Futtatás rendszergazdaként** paranccsal.
- **Az újraindítási állapotnak tisztának kell lennie.** A Windows újraindításra vár. Indítsa újra a számítógépet, majd futtassa újra a műveletet.
- **Felügyelt házirend van érvényben.** A beállítás a csoportházirend vagy az eszközkezelés tulajdonában van. Ott módosítsa.
- **A bizonyíték megváltozott.** Az első ellenőrzés és a végső újraellenőrzés között valami megváltozott. Futtassa újra az eszközt az új állapot beolvasásához.

## Egy korábbi kérés egyeztetést igényel

A KillerBench függő bejegyzést talált egy megszakított kérésből. Futtassa újra ugyanazt a műveletet. Ellenőrzi, mi történt, és a kérés megismétlése nélkül jelenti a valós eredményt. A parancssorban használja a megfelelő `-recover` parancsot, például: `killerbench-cli bitlocker-auto-unlock-recover`.

## Egy ellenőrzés Hiányos vagy Ismeretlen állapotot mutat

A bizonyítékot nem lehetett beolvasni, gyakran azért, mert a KillerBench nem rendszergazdaként futott, vagy egy Windows-szolgáltatás nem volt elérhető. A KillerBench ezt őszintén jelzi, ahelyett hogy egészségesnek feltételezné a számítógépet.

## Hol vannak a fájlok

A naplók, a függő bejegyzések és a mentett bizonyítékok a `%LOCALAPPDATA%\KillerBench` alatt találhatók. Minden védett műveletnek saját mappája van. Őrizze meg őket, ha nyilvántartásra van szüksége arról, mi változott.

## Probléma bejelentése

A KillerBench fejlesztés alatt áll. Probléma bejelentésekor csatolja az exportált jelentést és a tevékenységnaplót, és távolítson el mindent, amit nem osztana meg.
