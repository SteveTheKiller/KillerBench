# Kako spremembe ostanejo varne

KillerBench obravnava diagnostiko in spreminjanje kot različni opravili. Diagnostika samo bere. Celoten pregled sistema nikoli ne izvaja popravil. Izberite določeno popravilo ali dejanje, preglejte možnosti in pogoje ter potrdite predlagano spremembo.

## Pred spremembo

1. **Sveži dokazi.** Orodje znova prebere trenutno stanje. Shranjeno poročilo ali star dogodek nikoli ne dovoljuje spremembe.
2. **Natančno izpolnjevanje pogojev.** Sprememba je ponujena samo, ko se dokazi ujemajo s točno težavo, ki jo odpravlja. Vsaka nepopolnost jo blokira.
3. **Vaša potrditev.** Preglejte izbrano dejanje v potrditvenem oknu. Nekatera dejanja, na primer dešifriranje pogona, zahtevajo drugo potrditev. Pogovorno okno sledi dejavni temi aplikacije; njegov videz ne spremeni zahtev dejanja.
4. **Pogoji.** Preverijo se skrbniške pravice, stanje ponovnega zagona in nadzor pravilnikov ali upravljanja naprav. Nastavitve pod nadzorom pravilnikov ostanejo nespremenjene.
5. **Zapis čakajoče zahteve.** KillerBench zapiše, kaj namerava narediti, preden to zahteva od sistema Windows.
6. **Končno ponovno preverjanje.** Vse se znova preveri neposredno pred zahtevo, tako da vmesna sprememba postopek ustavi.

## Po spremembi

- KillerBench znova prebere rezultat in sporoči **Preverjeno** samo, ko Windows pokaže pričakovano stanje.
- Če Windows vrne uspeh, stanje pa se ne spremeni, rezultat to pove in nič se ne ponovi.
- Vsaka sprememba se shrani v dnevnik pod `%LOCALAPPDATA%\KillerBench`.

## Če se postopek prekine

Podprta dejanja z dnevnikom ohranijo zapis čakajoče zahteve, kadar je zahteva morda dosegla Windows, preverjanje pa še ni končano. Uporabite dokumentirani postopek uskladitve tega dejanja ali ustrezni ukaz `-recover`, da pregledate zapis. Uskladitev ni samodejna razveljavitev. Če je izid negotov, ohranite dnevnik in spremembe ne ponavljajte ročno. Vedenje ob zrušitvi, preklicu in poznejšem zagonu še zahteva preverjanje posameznih dejanj v dejanskem sistemu.

## Česa KillerBench ne bo naredil

- Počistil TPM-ja, izbrisal zaščitnikov, ki jih ni ustvaril, ali prikazal obnovitvenih gesel.
- Spremenil nastavitve, ki jo nadzorujejo pravilniki skupine ali upravljanje naprav.
- Izvedel popravila samo zato, ker pretekli dogodek ali uvoženo poročilo omenja težavo.

Diagnostika in analiza poročil potekata lokalno. Izbrano dejanje lahko vzpostavi povezavo z zunanjim ciljem, na primer za prenos programske opreme ali varnostno kopiranje obnovitvenega ključa v Active Directory ali Entra ID. Pred potrditvijo preglejte cilj. Shranjeni zajemi in neobdelani dokazi lahko vključujejo identiteto računalnika in poti do virov, zato jih pred deljenjem preglejte.

## Stanje razvoja

Obseg popravil se še širi. Uspešna gradnja ali simulirani preizkus ne dokazuje delovanja popravil v dejanskem sistemu. Datoteka `VM-VALIDATION.md` v repozitoriju sledi scenarijem na začasnih računalnikih Windows in preostalemu preverjanju.
