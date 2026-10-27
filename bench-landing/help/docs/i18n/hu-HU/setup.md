# Letöltés és beállítás

A KillerBench 1.0.0 még nem jelent meg. Nyilvános KillerBench-csomag vagy letöltés még nem érhető el. Ez az oldal a csomag jelenlegi követelményeit írja le, hogy nem hivatalos példány letöltése nélkül készülhessen fel.

## Windows és futtatókörnyezet

- A jelenlegi közzétételi cél a **Windows x64**. A natív ellenőrzés jelenlegi alapja a Windows 11 x64. Natív ARM64-, x86- és Windows Server-kiadás nem érhető el.
- A KillerBench kétféle csomagban érkezik. A telepítő **keretrendszerfüggő**, és a **.NET Desktop Runtime 10 for Windows x64** szükséges hozzá. A sima .NET Runtime, az ASP.NET Core Runtime és a .NET Framework nem biztosítja a WPF-et. A hordozható kiadás saját futtatókörnyezetet tartalmaz, és nincs szükség külön telepítésre. A `killerbench-cli.exe` parancssori eszközhöz a .NET 10 Runtime for Windows x64 szükséges. SDK a fejlesztéshez kell, a szokásos használathoz nem.
- Olyan Windows-verziót használjon, amelyet a [.NET 10 on Windows](https://learn.microsoft.com/en-us/dotnet/core/install/windows) támogat. A futtatókörnyezet támogatása nem jelenti azt, hogy a KillerBench javításait minden támogatott kiadáson vagy buildben ellenőrizték. A Windows 10 lefedettsége még függőben van, és a támogatott kiadásait a Microsoft jelenlegi életciklus-követelményei korlátozzák.
- A Desktop Runtime-ot csak a [Microsoft .NET 10 letöltési oldaláról](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) szerezze be, a **.NET Desktop Runtime**, **Windows**, **x64** lehetőséget választva. Kövesse a szervezete szoftvertelepítési szabályait.

Ha a `dotnet` elérhető, a `dotnet --list-runtimes` parancsnak az x64-es telepítéshez a `Microsoft.WindowsDesktop.App 10.0.x` bejegyzést kell kilistáznia. A csak más architektúrához telepített futtatókörnyezet nem felel meg az x64-es telepítőcsomagnak.

## Amikor megjelenik egy csomag

1. A webhelyről hivatkozott hivatalos letöltést használja. KillerBench-letöltési hivatkozás még nem aktív.
2. Olvassa el a csomag követelményeit, a [GPLv3 licencet](../license.txt), a megfelelő forráskódra vonatkozó utasításokat, a mellékelt harmadik féltől származó közleményeket, valamint az aláírási és ellenőrzőösszeg-adatokat. Egy fájlnév önmagában nem igazolja a hitelességet.
3. Futtassa a telepítőt az aktuális fiókhoz, vagy egy jogosultságemelési kérés után az összes felhasználóhoz. Ez parancsikont ad a Start menühöz, opcionálisan az asztalhoz, valamint bejegyzést a Programok telepítése és törlése listához. A hordozható kiadás telepítés nélkül fut.
4. Indítsa el a KillerBenchet a szokásos fiókjával, és kövesse [Az első diagnózis](first-run.html) lépéseit. A diagnosztikai hozzáférési hibák láthatók maradnak; a jogosultság emelése külön, tudatos döntés.

Ne kerülje meg a Windows biztonsági figyelmeztetéseit egy nem ellenőrzött példány futtatásához. Mielőtt a gépet módosító műveletet választana, legyen naprakész biztonsági mentése.

Indításkor a KillerBench újabb stabil kiadást keres a GitHubon, és telepítés előtt rákérdez. A letöltésnek egyeznie kell a közzétett SHA-256 ellenőrzőösszeggel. Az ellenőrzést a Névjegy **Frissítések keresése indításkor** beállításával kapcsolhatja ki.

## Jelenlegi korlátok

A jelenlegi build tizenkét szakaszt, csak olvasási diagnózist, jelentéseket és egyenként kiválasztható javításokat tartalmaz. A javítások natív ellenőrzése még folyamatban van. Egy működő felület, egy sikeres build vagy egy tesztkészlet eredménye nem igazolja a javítás viselkedését egy valódi számítógépen. Lásd: [Hogyan maradnak biztonságosak a módosítások](safety.html#development-status).

A kezdőoldal szemléltető konzolja szintetikus kimenet. Az első nyilvános csomag még várat magára.

## Forráskód, licenc és támogatás

A KillerBench a GPLv3 licenc alatt érhető el. Az első nyilvános csomaghoz tartozó forráskód-átadási utasítások és a harmadik féltől származó betűtípusok engedélyei még függőben vannak. A forráskód elérhetőségéről, a terjesztési közleményekről, a bizonyítékok védelméről és a támogatásról [A KillerBench névjegye](about.html) oldalon olvashat. Az [Engine útmutató](../engine/) a jogosult forráskód-példánnyal rendelkező fejlesztőknek szól; nem jelenti azt, hogy nyilvános Engine-csomag érhető el.
