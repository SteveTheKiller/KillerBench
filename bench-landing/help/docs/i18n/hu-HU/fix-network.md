# Hálózati és internetes problémák megoldása

Akkor használja, ha egy számítógépnek nincs internetkapcsolata, nem ér el egy kiszolgálót vagy nevet, vagy folyamatosan megszakad a kapcsolata. A KillerBench sorban ellenőrzi az adaptert, a címet, az átjárót, a DNS-t és a tűzfalat, és csak akkor újít meg bérletet vagy indít el szolgáltatást, ha friss bizonyíték igazolja, hogy szükség van rá.

## A kapcsolat ellenőrzése

1. Nyissa meg a **Hálózat** szakaszt, és válassza a **Hálózat** eszközt.
2. Ha valami konkrétat szeretne tesztelni, töltse ki a **Gépnév-keresés (opcionális)** mezőt, vagy a **TCP cél (opcionális)** és a **TCP port** mezőt. Ha üresen hagyja őket, csak a helyi konfiguráció kerül ellenőrzésre. Alkalmazásadatok nem kerülnek elküldésre.
3. Lassú hálózat esetén állítsa be az **Időtúllépés keresésenként vagy kapcsolatonként (másodperc)** értékét.
4. Hagyja kikapcsolva a **Megerősített problémák automatikus javítása** beállítást, és nyomja meg a **Diagnózis futtatása** gombot.
5. Olvassa el a jelentést. Megkülönbözteti a hiányzó címet, a hiányzó vagy elérhetetlen átjárót, a DNS-konfigurációs problémákat, a proxybeállításokat, a tűzfalprofil-eltéréseket és a sikertelen keresést vagy kapcsolódást, és mindegyikhez megadja a következő lépést.

Az átjáró válasza nem igazolja az internet-hozzáférést, és a beállított névfeloldón keresztüli sikertelen keresés nem jelenti azt, hogy a webhely nem működik.

## A jelentésben talált problémák javítása

1. Futtassa újra a **Hálózat** eszközt bekapcsolt **Megerősített problémák automatikus javítása** beállítással. Elindíthatja az engedélyezett DNS-ügyfél, DHCP-ügyfél, SMB-ügyfél, Hálózati helyek felismerése, Hálózatlista és IP-segéd szolgáltatást, és megújíthatja azokat a csatlakoztatott fizikai DHCP-adaptereket, amelyeknek nincs használható IPv4-címük.
2. Megújításkor számítson a kapcsolat rövid megszakadására. A statikus címek és a virtuális adapterek soha nem változnak.
3. Egyetlen hibás gyorsítótárazott névnél a teljes gyorsítótár törlése helyett használja az adott gazdagépre vonatkozó DNS-gyorsítótár-ürítést.

Az útvonalak, a Hosts-bejegyzések, a DNS-kiszolgálók, a tűzfalszabályok, a Winsock-szolgáltatók és a hálózati profilok mindig megmaradnak.

## Vezeték nélküli hálózat, vezetékes bejelentkezés és proxyk

- A **Vezeték nélküli hálózat** ellenőrzi az adaptert és a WLAN automatikus konfigurálását, és elindíthatja a szolgáltatást, ha le van állítva.
- A **Vezetékes hitelesítés (802.1X)** ugyanezt végzi a vezetékes automatikus konfigurálással azokon a hálózatokon, amelyek a porton bejelentkezést igényelnek.
- A **WinHTTP automatikus proxy** a gép és az aktuális fiók tárolt proxybeállításait mutatja. A KillerBench diagnózis közben soha nem tölt le PAC URL-t, és nem módosítja a proxybeállításokat.

## Parancssorból

```powershell
killerbench-cli workflow network --host example.com --tcp example.com 443
killerbench-cli dns-check example.com --timeout-seconds 5
killerbench-cli tcp-check fileserver 445
killerbench-cli dhcp-renew
killerbench-cli dns-cache example.com --execute
```

Lásd: [workflow](cli.html#cmd-workflow), [dns-check](cli.html#cmd-dns-check), [tcp-check](cli.html#cmd-tcp-check), [dhcp-renew](cli.html#cmd-dhcp-renew) és [dns-cache](cli.html#cmd-dns-cache).

## Kapcsolódó

- [Hálózat szakasz](network.html)
- [Nyomtatási problémák megoldása](fix-printing.html)
- [Hogyan maradnak biztonságosak a módosítások](safety.html)
