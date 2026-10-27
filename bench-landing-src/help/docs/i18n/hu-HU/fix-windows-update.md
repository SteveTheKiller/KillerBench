# A Windows Update javítása

Akkor használja, ha a frissítések meghiúsulnak, elakadnak vagy újra és újra települnek. A KillerBench először beolvassa a frissítési láncot, a mögötte álló bizonyítékokkal elmagyarázza a hibát, és csak akkor indít el szolgáltatást vagy javítja az összetevőtárat, ha friss bizonyíték pontosan ezt a problémát igazolja.

## Mielőtt elkezdené

- Futtassa a KillerBenchet rendszergazdaként. A frissítési szolgáltatásokhoz, az összetevőtárhoz és a védett rendszerfájlokhoz a teljes bizonyítékhoz emelt jogosultság szükséges.
- Ha a Windows már újraindításra vár, először indítsa újra. Sok frissítési javítás le van tiltva, amíg az újraindítási állapot nem tiszta.
- A karbantartási munka idejére tartsa a számítógépet hálózati tápellátáson.

## Derítse ki, miért hiúsulnak meg a frissítések

1. Nyissa meg a **Frissítések** szakaszt, és az eszközoldalsávon válassza a **Windows Update** elemet.
2. Az első körben hagyja kikapcsolva a **Megerősített problémák automatikus javítása** beállítást, és nyomja meg a **Diagnózis futtatása** gombot.
3. Olvassa el az **Olvasható jelentést**. Felsorolja a leállított frissítési szolgáltatásokat, a függő újraindításokat, a kevés szabad helyet, az összetevőtár sérülését és a Windows által rögzített pontos hibakódokat, egy javasolt következő lépéssel.
4. Ha a jelentés hibakódot nevez meg, a hozzá tartozó követő ellenőrzések automatikusan lefutnak. Egy kódot saját maga is kikereshet a `killerbench-cli update-code 0x80070002` paranccsal.

## Egy elakadtnak tűnő frissítés figyelése

A **Frissítések** szakaszban válassza a **Karbantartási tevékenység figyelője** eszközt, és nyomja meg a **Diagnózis futtatása** gombot. Ismételt pillanatképeket készít a frissítési, a Setup- és az Installer-tevékenységről, az átviteli összesítésekről és az újraindítási jelzőkről. A tevékenység csak bizonyítékként jelenik meg; önmagában soha nem állít elakadást vagy hibát.

## A jelentésben talált problémák javítása

1. Futtassa újra a **Windows Update** eszközt bekapcsolt **Megerősített problémák automatikus javítása** beállítással. Elindíthatja az igazoltan leállított, engedélyezett frissítési szolgáltatásokat, a DISM-et vagy az SFC-t pedig csak akkor futtatja, ha az élő ellenőrzés sérülést igazol.
2. Egyetlen szolgáltatáshoz inkább a célzott eszközét válassza, például a **BITS szolgáltatás**, a **Titkosítási szolgáltatások**, a **Windows-modultelepítő** vagy az **Update Orchestrator szolgáltatás** eszközt. Mindegyik csak a saját szolgáltatásához kínál helyreállítást.
3. Ha a jelentés újraindítást kér, indítsa újra a gépet, mielőtt bármi mást futtatna. A KillerBench rögzíti az újraindítást igénylő eredményeket, és a későbbi munkát az újraindításig elhalasztja.

Minden javítás naplózásra és visszaolvasásra kerül. A teljes folyamatot a [Hogyan maradnak biztonságosak a módosítások](safety.html) oldal írja le.

## Egy adott frissítés telepítése

1. A **Frissítések** szakaszban válassza az **Elérhető frissítések** eszközt, és nyomja meg a **Diagnózis futtatása** gombot a beállított forrásból elérhető frissítések felderítéséhez.
2. Jelölje ki a kívánt frissítéseket, és nyomja meg a **Kiválasztottak előnézete** gombot. Az előnézet pontosan felsorolja, mi települne, és van-e újraindítási követelmény.
3. Olvassa el az előnézetet, jelölje be a megerősítő jelölőnégyzetet, majd nyomja meg a **Telepítés** gombot. A jelölőnégyzet minden futtatás után törlődik, és a KillerBench soha nem választ ki és nem hagy jóvá frissítést Ön helyett.

## Parancssorból

```powershell
killerbench-cli update-search
killerbench-cli update-history
killerbench-cli servicing-monitor --samples 10
killerbench-cli update-plan --select GUID:REVISION --execute
```

Lásd: [update-search](cli.html#cmd-update-search), [update-history](cli.html#cmd-update-history), [servicing-monitor](cli.html#cmd-servicing-monitor) és [update-plan](cli.html#cmd-update-plan). Ha egy telepítés megszakadt, futtassa az [update-recover](cli.html#cmd-update-recover) parancsot; ellenőrzi az eredményt, és soha nem próbálja újra a frissítést.

## Kapcsolódó

- [Frissítések szakasz](updates.html)
- [Rendszerfájlok javítása](repair-system-files.html)
- [Hibaelhárítás](troubleshooting.html)
