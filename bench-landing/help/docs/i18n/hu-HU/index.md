# KillerBench súgó

A KillerBench technikusoknak készült Windows diagnosztikai és javítóeszköz. Először ellenőrzi a számítógépet, minden megállapítást a mögötte álló bizonyítékokkal együtt magyaráz el, és csak akkor módosítja a Windowst, ha Ön kiválaszt egy konkrét javítást, és megerősíti.

A KillerBench fejlesztés alatt áll. Nyilvános letöltés még nem érhető el. Ezek az oldalak a jelenlegi buildet írják le.

## Mit csinál

- Csak olvasási diagnosztikát futtat a Windows Update, az eszközök, a tárolás, a hálózat, a nyomtatás, a biztonság, az alkalmazások és a profilok területén, majd olvasható jelentést készít.
- Védett javításokat kínál, amelyek csak akkor futnak, ha friss bizonyítékok pontosan az adott problémát igazolják, és utána ellenőrzi az eredményt.
- Lefedi a beállítási és megerősítési feladatokat, például a BitLockert, az adatvédelmi beállításokat, a Windows rendbetételét és az új számítógép üzembe helyezését.
- A diagnosztikát és a jelentéselemzést helyben futtatja, AI és KillerBench-fiók nélkül. A kiválasztott műveletek, például a letöltések és a helyreállítási kulcs mentése az Entra ID-ba, külső szolgáltatásokkal is kapcsolatba léphetnek.
- A konzolhoz, a jelentésekhez, a megerősítésekhez, valamint a Megnyitás, Mentés és Mappa párbeszédpanelekhez az alkalmazás aktív témáját használja.

## Hol kezdje

- [Letöltés és beállítás](setup.html)
- [A KillerBench névjegye](about.html)

- [Az első diagnózis](first-run.html) végigvezeti egy teljes rendszervizsgálaton.
- [A munkaterület](workspace.html) bemutatja a szakaszokat, a paneleket és a billentyűparancsokat.
- [Hogyan maradnak biztonságosak a módosítások](safety.html) elmagyarázza, mit ellenőriz a KillerBench minden módosítás előtt és után.
- A [lépésenkénti útmutatók](fix-windows-update.html) végigvezetik a gyakori feladatokon.
- A **Szakaszok** oldalai minden eszközt ismertetnek, ugyanúgy csoportosítva, mint az alkalmazásban.
- A [Parancssor](cli.html) felsorolja az összes `killerbench-cli` parancsot.
- A [Hibaelhárítás](troubleshooting.html) a letiltott műveletekkel, az újraindításokkal és a megszakított kérésekkel foglalkozik.

Azok a fejlesztők, akik saját eszközeikbe szeretnék beépíteni a diagnosztikát, az [Engine fejlesztői útmutatóban](../engine/index.html) olvashatnak róla.
