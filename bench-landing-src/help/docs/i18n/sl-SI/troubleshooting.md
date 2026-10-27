# Odpravljanje težav

## Dejanje sporoča, da je blokirano

Blokiran rezultat pomeni, da se zahtevano dejanje ni moglo nadaljevati. Preberite sporočilo in dnevnik, da ugotovite, ali se je ustavilo pred pošiljanjem zahteve ali po prejšnjem koraku. Ne predpostavljajte, da je bilo delno opravljeno dejanje povrnjeno. Spodaj so najpogostejši razlogi, povezani s predpogoji.

- **Potrebne so skrbniške pravice.** Zaprite KillerBench in ga znova zaženite z možnostjo **Zaženi kot skrbnik**.
- **Ponovni zagon ne sme biti potreben.** Windows čaka na ponovni zagon. Znova zaženite računalnik in nato ponovite dejanje.
- **Prisoten je upravljani pravilnik.** Nastavitev nadzira pravilnik skupine ali upravljanje naprave. Spremenite jo tam.
- **Podatki so se spremenili.** Med prvim preverjanjem in končnim ponovnim preverjanjem se je nekaj spremenilo. Orodje zaženite znova, da prebere novo stanje.

## Prejšnjo zahtevo je treba uskladiti

KillerBench je našel čakajoči zapis prekinjene zahteve. Znova zaženite isto dejanje. Preverilo bo, kaj se je zgodilo, in sporočilo dejanski izid brez ponavljanja zahteve. V ukazni vrstici uporabite ustrezen ukaz `-recover`, na primer `killerbench-cli bitlocker-auto-unlock-recover`.

## Preverjanje prikaže Nepopolno ali Neznano

Podatkov ni bilo mogoče prebrati, pogosto zato, ker KillerBench ni bil zagnan kot skrbnik ali storitev Windows ni bila na voljo. KillerBench to pošteno sporoči, namesto da bi predpostavil, da je računalnik zdrav.

## Kje so shranjene datoteke

Dnevniki dejanj, čakajoči zapisi in shranjeni dokazi so v mapi `%LOCALAPPDATA%\KillerBench`. Vsako varovano dejanje ima svojo mapo. Ohranite jih, kadar potrebujete zapis sprememb.

## Prijava težave

KillerBench je v razvoju. Prijavi težave priložite izvoženo poročilo in dnevnik dejavnosti ter odstranite vse, česar ne bi želeli deliti.
