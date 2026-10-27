# Biztonság

A Biztonság megvizsgálja a Microsoft Defendert, a tűzfalat, a tanúsítványokat és a bejelentkezési szolgáltatásokat. Megmutatja a TPM és a platformbiztonság állapotát is, amelytől a BitLocker és a Windows Hello függ.

Lépésenkénti útmutatók: [Meghajtó titkosítása BitLockerrel](encrypt-with-bitlocker.html) és [A Windows adatvédelmének erősítése](make-windows-private.html).

## Az ebben a szakaszban található eszközök

A védelem, a tűzfal, a tanúsítványok és a bejelentkezési szolgáltatások vizsgálata.

### Microsoft Defender

Megvizsgálja a Defender védelmét és a definíciók állapotát. Az automatikus javítás csak akkor frissíti a definíciókat, ha a Defender aktív védelmet és elavult definíciókat jelez. A beállított definícióforrásokat használja. Nincs védelem-, kivétel- vagy házirend-módosítás; nincs kártevővizsgálat.

### Windows Biztonsági központ

Áttekinti a korlátozott vírusvédelmi regisztrációkat a nyers szolgáltatói állapottal, a szolgáltatás pontos hibaelőzményeit és a Windows Biztonsági központ aktuális készenlétét, a védelem állapotának értelmezése nélkül. Az üres leltár nem ítélet a védelem állapotáról, és a regisztrációk száma, a nyers állapot vagy a korábbi hibák nem jogosítanak fel helyreállításra vagy biztonsági módosításra. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott, engedélyezett és leállított szolgáltatást indítja el. A meglévő biztonságiállapot-figyelés és az értesítések folytatódhatnak. A vírusvédelmi konfiguráció, a szolgáltatói regisztrációk, a védelmi beállítások, a kivételek, az értesítések, a házirend és az indítási típus megmarad. Nincs kérés vizsgálatra, frissítésre vagy védelemmódosításra. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a vírusvédelem jelenlétére, a védelem állapotára, a definíciók frissességére, az értesítések kézbesítésére, a regisztrációk tulajdonosára vagy a tényleges házirendre.

### Tűzfalszolgáltatások

Beolvassa az ActiveStore-ból a Tartományi, a Magán és a Nyilvános profil engedélyezési beállításait, majd ellenőrzi az Alapszűrő motor és a Windows Defender tűzfal szolgáltatást. Az opcionális helyreállítás friss függőségellenőrzések után csak az engedélyezett, leállított szolgáltatásokat indítja el. A meglévő szűrési házirend életbe léphet, és megszakíthatja a hálózati vagy távoli hozzáférést. Az indítási típusok, a szolgáltatásengedélyek, a tűzfalszabályok, a profilok és az IPsec-beállítások megmaradnak. Nincs alaphelyzetbe állítás, szabálymódosítás vagy szolgáltatásleállítás. Az ellenőrzés csak a szolgáltatások futó állapotát igazolja; a tényleges védelem és a kapcsolódás ellenőrizetlen marad.

### Tanúsítványok lejárata

Megvizsgálja a tanúsítványok dátumait, és csak a gyorsítótárból épít helyi megbízhatósági láncokat a gép és az aktuális felhasználó személyes tárolóihoz, majd egyetlen korlátozott működési következtetést ad. A visszavonás, a kiszolgáló általi bemutatás, a kötések, a regisztráció, a házirend-tulajdonlás, a távoli megbízhatóság és a titkos kulcsok használhatósága ellenőrizetlen marad. Egyetlen tanúsítvány, megbízhatósági beállítás, regisztráció, kötés, titkos kulcs, alkalmazás, szolgáltatás vagy házirend sem változik.

### Hitelesítőadat-kezelő

Ellenőrzi a Hitelesítőadat-kezelő szolgáltatás elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott, engedélyezett és leállított szolgáltatást indítja el. A meglévő alkalmazások folytathatják a tárolt hitelesítő adatokra vonatkozó kéréseiket. A tároló tartalma, a jelszavak, a hozzáférési kulcsok, a tanúsítványok, a tokenek, a hitelesítőadat-regisztrációk, az engedélyek, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. Nincs kérés hitelesítőadat-tartalomra vagy -műveletre. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a hitelesítő adatok lekérésére, a hitelesítésre vagy az alkalmazások hozzáférésére.

### Webes fiókkezelő

Áttekinti a hasznos adatoktól megtisztított fiókközvetítő-előzményeket és a Webes fiókkezelő pontos elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott, engedélyezett és leállított szolgáltatást indítja el. A meglévő alkalmazások az aktuális házirend szerint folytathatják a fiók- és tokenkéréseiket. A korábbi események nem jogosítanak fel helyreállításra, és nem határozzák meg a fiók aktuális állapotát. A fiókok, a hitelesítő adatok, a tokenek, a cookie-k, az alkalmazásregisztrációk, az engedélyek, a bejelentkezési állapot, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench nem választ ki fiók-, hitelesítőadat-, token-, cookie-, engedély- vagy bejelentkezési műveletet. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a hitelesítésre, a tokenkiadásra vagy az alkalmazások hozzáférésére.

### Microsoft-fiók bejelentkezési segéd

Áttekinti a hasznos adatoktól megtisztított fiókközvetítő-előzményeket és a Microsoft-fiók bejelentkezési segéd pontos elérhetőségét. Az opcionális helyreállítás friss függőségellenőrzések után csak a pontosan megadott, engedélyezett és leállított szolgáltatást indítja el. A meglévő Microsoft-fiókos bejelentkezési kérések az aktuális házirend szerint folytatódhatnak. A korábbi események nem jogosítanak fel helyreállításra, és nem határozzák meg a fiók aktuális állapotát. A fiókok, a hitelesítő adatok, a tokenek, az eszközregisztráció, az alkalmazásadatok, az engedélyek, a bejelentkezési állapot, a házirend, a szolgáltatásengedélyek és az indítási típus megmarad. A KillerBench nem választ ki fiók-, hitelesítőadat-, token-, regisztrációs, engedély- vagy bejelentkezési műveletet. Az ellenőrzés csak a szolgáltatás stabil állapotára terjed ki, nem a hitelesítésre, a fiók kapcsolódására vagy az alkalmazások hozzáférésére.
