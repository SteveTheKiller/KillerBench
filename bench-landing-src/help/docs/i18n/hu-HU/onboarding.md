# Üzembe helyezés

Az Üzembe helyezés egy új számítógépet készít elő. Áttekinti a kiválasztott alkalmazásokat és konfigurációt, és elnevezi a számítógépet. Önmagától semmit sem tölt le és nem telepít.

Lépésenkénti útmutatók: [Új számítógép beállítása](set-up-new-pc.html).

## Telepítő ellenőrzése futtatás előtt

A parancssor futtatás előtt ellenőrizni tudja egy gyártó telepítőjét:

```powershell
killerbench-cli depot-verify-installer chrome C:\Installers\ChromeSetup.exe
```

A KillerBench csak akkor bízik meg egy telepítőben, ha az a gyártó pontos szervezeti nevére kiállított érvényes Authenticode-aláírással rendelkezik. A telepítőt soha nem futtatja.

## Az ebben a szakaszban található eszközök

Új számítógép előkészítése: alkalmazások, név és konfiguráció.

### Üzembe helyezés

Áttekinti a kifejezetten kiválasztott kiépítési alkalmazásokat és konfigurációs csoportokat. Az aktuális eltávolítási, alkalmazáscsomag- és Office Kattintásra-futtatás regisztrációkat telepítők letöltése vagy futtatása nélkül veti össze. A konfigurációs választások leírják a következményeket, és a megvalósításukkor külön védett folyamatokhoz irányítanak. Nem történik letöltés, telepítés, illetve fiók-, frissítési, tisztítási, házirend-, beállításjegyzék-, szolgáltatás-, profil- vagy újraindítási művelet.

### Számítógépnév

Összeveti a javasolt számítógépnevet az aktuális azonosítóval és a tartományi szereppel. Nincs átnevezés, hitelesítőadat-gyűjtés vagy újraindítás. A név elérhetősége, az engedélyek és az alkalmazásfüggőségek ellenőrizetlenek maradnak.
