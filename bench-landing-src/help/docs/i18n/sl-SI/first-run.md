# Vaša prva diagnostika

Začnite s stranjo [Prenos in namestitev](setup.html), nato izvedite celoten pregled sistema. Zbere diagnostične dokaze in shrani lokalni dnevnik; popravil ne izvaja. Ugotovitev opisuje pregledani obseg, ne vseh mogočih napak računalnika.

## Izvedite pregled

1. Odprite KillerBench. Odpre se razdelek **Diagnostika** z izbranim **Celotnim pregledom sistema**.
2. Pri prvem izvajanju pustite možnost **Samodejno popravi potrjene težave** izklopljeno.
3. Pritisnite **Zaženi diagnostiko**. Med preverjanji se napredek prikazuje v **Neobdelanem izpisu** na desni.
4. Po koncu se izhodno podokno preklopi na **Pregledno poročilo**.

Začnite brez povišanih pravic. Nekateri podatki sistema Windows, na primer zaščitniki BitLocker in zaščitene storitve, zahtevajo skrbniške pravice. Z običajnim računom ta preverjanja sporočijo, da dokazi niso na voljo. Če te dokaze potrebujete, končajte trenutno izvajanje, shranite poročilo in nato namenoma znova odprite KillerBench z možnostjo **Zaženi kot skrbnik**. Povišanje pravic ne izbere ali odobri popravila.

## Preberite poročilo

Poročilo se začne s številom elementov **Potreben je pregled**, **Preverjena popravila**, **Nepopolno** in **Preverjeno**, nato s priporočenim naslednjim korakom.

- **Kaj zahteva pozornost** navaja z dokazi podprte težave, najpomembnejše najprej.
- **Zbrane informacije** prikazujejo prebrane podatke, vključno z nepopolnimi ali nedostopnimi dokazi.
- Pretekli dogodki so označeni kot zgodovinski. Napaka iz prejšnjega meseca se ne obravnava kot trenutna okvara.

Manjkajoči dokazi nikoli ne pomenijo brezhibnega stanja. Če preverjanja ni bilo mogoče izvesti, poročilo to pove.

## Izberite preverjanja sami

Odprite **Izberi preverjanja (napredno)** za izbiro posameznih preverjanj namesto celotnega pregleda. Vsak razdelek aplikacije ima tudi ciljna orodja, opisana na straneh **Razdelki** v tem vodniku.

## Popravite odkrito težavo

Ko poročilo priporoči popravek, navede razdelek in orodje, ki ju morate odpreti. Pred prvim popravilom preberite [Kako spremembe ostanejo varne](safety.html). Preverjanje v dejanskem sistemu še poteka; uspešna diagnostika ne dokazuje, da je bilo vsako ponujeno popravilo preizkušeno na vaši gradnji sistema Windows. Imejte varnostne kopije ter pred vsako spremembo preglejte točen cilj in posledice.
