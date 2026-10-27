# Izboljšajte zasebnost v sistemu Windows

Uporabite za pregled in izboljšanje zasebnosti na prenosniku. KillerBench prebere trenutne nastavitve, pokaže, katere že nadzorujejo pravilniki ali upravljanje naprav, in spreminja samo izbrane nastavitve, eno naenkrat, z natančno razveljavitvijo.

## Preglejte trenutne nastavitve

1. Odprite **Zasebnost**.
2. Označite področja za pregled. Nobeno področje na začetku ni označeno.
3. Pritisnite **Zaženi diagnostiko**. Poročilo pokaže trenutno vrednost za ta računalnik in račun, ki izvaja KillerBench, ter označi nastavitve pod nadzorom pravilnikov.

## Spremenite nastavitev

1. V **Nastavitev zasebnosti** izberite nastavitev, na primer kamero, mikrofon, lokacijo, diagnostične podatke, oglaševanje, zgodovino odložišča, iskanje vsebine v oblaku ali poudarke iskanja.
2. Izberite novo vrednost in označite **Uveljavi, ko izberem Zaženi diagnostiko**.
3. Pritisnite **Zaženi diagnostiko**. KillerBench preveri nadzor pravilnikov, zabeleži trenutno vrednost, izvede eno spremembo in znova prebere rezultat.

Nastavitve pod nadzorom pravilnikov skupine ali upravljanja naprav ostanejo nespremenjene. Spremenite jih tam, kjer se upravljajo.

## Razveljavite spremembo

Vsaka preverjena sprememba ohrani dnevnik. Ustrezna razveljavitev obnovi točno prejšnjo vrednost, znova po preverjanju pravilnikov.

## Odstranite navlako iz sistema Windows

Za predloge, potrošniško vsebino, Recall in funkcije umetne inteligence, navlako v opravilni vrstici in meniju Start, Edge ter vnaprej nameščeno programsko opremo odprite **Vzdrževanje** in izberite **Odstranitev navlake iz sistema Windows**. Pregled je samo za branje in nič se ne izbere samodejno.

## Iz ukazne vrstice

```powershell
killerbench-cli privacy-plan --select Camera --select ClipboardSync
killerbench-cli privacy-setting camera-access off --execute
killerbench-cli privacy-setting-undo camera-access VERIFIED_JOURNAL --execute
killerbench-cli debloat-review --area TelemetryAndReporting
```

Glejte [privacy-plan](cli.html#cmd-privacy-plan), [privacy-setting](cli.html#cmd-privacy-setting), [privacy-setting-undo](cli.html#cmd-privacy-setting-undo) in [debloat-review](cli.html#cmd-debloat-review). Nepodprt ključ izpiše podprte možnosti.

## Povezano

- [Razdelek Zasebnost](privacy.html)
- [Pripravite nov računalnik](set-up-new-pc.html)
