# Tehnično ozadje programa KillerBench

Pogled v ozadje: kako KillerBench bere podatke računalnika Windows, ohranja zanesljivost dokazov in odloča, kdaj je sprememba dovoljena.

## Tehnologije

KillerBench je izvorna aplikacija Windows. Namizna aplikacija, ukazna vrstica in osnovni Engine si delijo iste diagnostične dokaze in postopke popravil.

| Komponenta | Podrobnosti |
| --- | --- |
| Uporabniški vmesnik | WPF na .NET 10 (`net10.0-windows`), Windows x64, lastno ogrodje okna |
| Engine | `Engine.Core`, `Engine` in `Engine.Windows`, brez sklicev na WPF ali namizne kontrolnike |
| Ukazna vrstica | `killerbench-cli`, ločen gostitelj istega Engine |
| Ponudniki Windows | .NET, API-ji Windows, PowerShell in ukazna orodja Windows, vsak s fiksnimi omejitvami časa, števila zapisov in velikosti |
| Poročila | Surovi izhod, berljivo poročilo, HTML, besedilo in JSON |
| Referenčni podatki | Različice paketov znanja za kode napak, identifikatorje strojne opreme, ponudnike in vire podpore |
| Pakiranje | Namestitveni program: odvisen od ogrodja, potrebuje .NET Desktop Runtime 10 za Windows x64. Prenosna izdaja: samostojna, brez namestitve izvajalnega okolja |
| Videz | 13 družinskih tem in 20 jezikov |

## Namestitev, mape in podatki

KillerBench je pripravljen v dveh oblikah. Namestitveni program je standardni prenos in podpira namestitev za enega ali vse uporabnike. Prenosna izdaja vsebuje lastno izvajalno okolje .NET in deluje brez namestitve.

KillerBench se izvaja s pravicami računa, ki ga je zagnal. Sam ne poviša svojih pravic. Zaženite ga z možnostjo **Zaženi kot skrbnik**, kadar zaščiteni podatki, kot so zaščitniki BitLocker, ali popravilo zahtevajo skrbniške pravice. Povišanje pravic nikoli ne izbere ali odobri popravila.

### Kaj naredi namestitveni program

Namestitveni program preveri in razširi aplikacijo v `%LOCALAPPDATA%\Programs\KillerBench\` ali po pozivu za povišanje pravic za vse uporabnike v `%ProgramFiles%\KillerBench\`. Doda bližnjico v meni Start, izbirno bližnjico na namizje in vnos v Dodaj ali odstrani programe z običajno ter tiho odstranitvijo. Prenosna izdaja preveri in razširi svoj tovor v `%LOCALAPPDATA%\KillerBench\Portable\` za trenutni zagon ter ob naslednjem zagonu počisti opuščene kopije.

### Kje so shranjeni podatki

| Lokacija | Vsebina |
| --- | --- |
| `%LOCALAPPDATA%\KillerBench\appearance.json` | Tema, poudarek, merilo vmesnika in jezik |
| `%LOCALAPPDATA%\KillerBench\Workflows\` | En dnevnik JSON na zagon postopka: odločitve, omejen izhod ukazov in rezultati popravil |
| `%LOCALAPPDATA%\KillerBench\Repairs\` | Dnevniki izvajanja servisnih popravil, kot sta DISM in SFC |
| Mape funkcij pod `%LOCALAPPDATA%\KillerBench\` | Čakajoča varovala in izidi posameznih varovanih dejanj, na primer `BitLockerBackup`, `Time`, `Firewall` in `Profiles` |
| `%LOCALAPPDATA%\KillerBench\agent-repairs.json` | Raven popravil KillerMCP, izbrana v razdelku O programu |
| `HKCU` ali `HKLM\Software\KillerBench` | Namestitvena pot za ustrezen obseg namestitve |
| Izbrana datoteka `.kbench` | Shranjen zajem z ohranjenimi dokazi |

Nič se ne naloži na strežnik. Dnevnik izbrišite šele, ko dokazov o negotovem izidu ne potrebujete več.

## Najprej diagnostika

KillerBench obravnava diagnostiko in spremembe kot ločeni nalogi. Diagnostika samo bere. Sprememba se zgodi le, ko izberete določeno popravilo ali dejanje, pregledate možnosti in predpogoje ter ga potrdite.

Celovit pregled sistema je samo diagnostičen, tudi če ima posamezen usmerjeni postopek omogočeno samodejno popravilo. Shranjeni zajemi, uvoženi dnevniki in pretekli dogodki nikoli ne dovoljujejo popravila.

### Omejeno zbiranje

Vsak izvorni ukaz in ponudnik Windows ima fiksne omejitve časa, števila zapisov in velikosti. Ponudnik zabeleži izhodno stanje, časovno prekoračitev, krajšanje izhoda, standardni izhod in izhod napak. Neuspešnega ali okrnjenega rezultata ponudnika nikoli ni mogoče razumeti kot brezhiben, prazen odgovor.

### Najmanjše potrebno shranjevanje

Zbiralniki ohranijo tisto, kar ugotovitev potrebuje. Številni rezultati ohranijo imena, kategorije, števila, stanja in časovne oznake, izpustijo pa poverilnice, ukazne vrstice, polne poti, identitete računov, omrežne končne točke ter nepovezano vsebino dogodkov.

## Sledljivi dokazi

Vsaka ugotovitev je povezana z dokazi, iz katerih izhaja, poročilo pa vedno loči dokazano od zgolj opaženega.

| Stanje | Pomen |
| --- | --- |
| Potreben pregled | Dokazi kažejo na težavo, ki zahteva pozornost. |
| Informacija | Podatki so bili zbrani in so prikazani za kontekst. |
| Nepopolno ali Neznano | Preverjanja ni bilo mogoče dokončati ali podatki niso bili na voljo. To se nikoli ne šteje za zdravo stanje. |
| Preteklo | Dogodek iz preteklosti. Pojasnjuje zgodovino, vendar ne dokazuje trenutne napake. |
| Preverjeno | Popravilo ali sprememba je bila potrjena s ponovnim branjem rezultata. |

### Neznano je rezultat

Neznano pomeni, da je bil dostop zavrnjen, vir manjka, ponudnik ni uspel, je izhod nepravilno oblikovan, je bila omejitev presežena ali se dokazi ne ujemajo. KillerBench to omejitev in naslednji koristen korak prikaže, namesto da bi ju prikril.

### Zgodovina ostane zgodovina

Zapisi dogodkov in uvoženi dnevniki opisujejo, kaj je bilo zabeleženo v preteklosti. Lahko pokažejo na kodo napake ali naslednje področje diagnostike. Ne dokazujejo, da je napaka še vedno aktivna.

## Varovana popravila

Popravila so tipizirane operacije s pravili upravičenosti, izvajanja, preverjanja in obnovitve. Niso nizi ukazov, izbrani iz vrstice dnevnika. Vsako podprto popravilo sledi istemu zaporedju:

1. Zberi popolne trenutne dokaze.
2. Poišči en ustrezen podprt postopek popravila.
3. Preglej zahtevane možnosti in posledice.
4. Potrdi predpogoje glede skrbniških pravic in pravilnikov.
5. Neposredno pred spremembo znova zberi odločilne dokaze.
6. Zapiši trajen zapis priprave.
7. Izvedi eno omejeno izvorno dejanje.
8. Zberi sveže dokaze za preverjanje.
9. Zabeleži preverjen, blokiran, ustavljen ali negotov izid.

Če se odločilni dokazi pred izvedbo spremenijo, se KillerBench ustavi brez izvedbe dejanja.

### Izrecne izbire

Popravila, odvisna od možnosti, zahtevajo vašo točno izbiro, na primer nosilec, tiskalno čakalno vrsto, cilj obnovitvenega ključa, vrednost porabe energije, različico paketa ali vir popravila. KillerBench teh vrednosti nikoli ne izmisli.

### Upravljani računalniki

Pravilnik skupine in upravljanje naprave lahko povzročita, da je lokalna sprememba neučinkovita ali neprimerna. Podprta popravila preverijo, kdo nadzira nastavitev, in se ustavijo, če je nadzor upravljan ali neznan. Nastavitev, ki jih nadzira pravilnik, ne spreminjajo.

### Samodejno popravilo

Samodejno popravilo dovoli le nadaljevanje upravičenih postopkov popravil. Ne obide dokazov, ne izbere manjkajočih možnosti, ne prezre pravilnikov, ne ponovi negotovega dejanja, ne zažene sistema Windows znova in ugotovitve, ki zahteva pregled, ne spremeni v popravilo.

## Dnevniki dejanj in prekinitve

Preden KillerBench od sistema Windows zahteva spremembo, zapiše, kaj namerava storiti. Vsaka sprememba je shranjena v dnevnik pod `%LOCALAPPDATA%\KillerBench`.

Po spremembi KillerBench znova prebere rezultat in sporoči **Preverjeno** le, kadar Windows pokaže pričakovano stanje. Če Windows sporoči uspeh, vendar se stanje ni spremenilo, rezultat to navede in ničesar ne ponovi.

Proces se lahko ustavi po tem, ko Windows prejme zahtevo, vendar pred koncem preverjanja. Čakajoči zapis ostane, poznejše usklajevanje pa prebere sveže stanje, da ugotovi, kaj se je zgodilo. Usklajevanje ni samodejna povrnitev in nikoli ne ponovi izvirne spremembe. Pri negotovem izidu ohranite dnevnik.

## Konstante in omejitve

| Specifikacija | Vrednost |
| --- | --- |
| Diagnostična pokritost | Celovit pregled sistema izvede 49 diagnostičnih področij in preveri 96 varovanih postopkov popravil |
| Branje besedilnega vira | 1.000.000 znakov na vir, prikazanih 2.000 ujemajočih se vrstic |
| Viri seje | 12 v stranski vrstici; zajem vsebuje do 24 virov in 64 MB |
| Poizvedbe dogodkov | Najnovejših 200 dogodkov na kanal iz zadnjih 7 dni |
| Pregled Windows Update | 15 sekund na vir, skupno dve minuti |
| Raziskovanje dnevnikov | 10.000 razčlenjenih vnosov, 256 različnih kod, 8 odlomkov po 320 znakov na kodo |
| Stisnjeni arhiv CBS | Arhiv 256 MiB, dnevnik v arhivu 512 MiB, 30 sekund za razširjanje |
| CHKDSK samo za branje | Deset minut; popravilo je omejeno na 65 minut |
| Preverjanja DNS in TCP | Privzeto deset sekund, največ ena minuta |
| Popis storitev | Do 1.024 nameščenih storitev |
| Paketi znanja | Hkrati odprtih do 32 |
| Pregled velikih datotek | Datoteke velikosti najmanj 100 MB |
| Spremljanje servisiranja | Interval od 1 do 60 sekund, od 1 do 120 posnetkov |
| Osnutki načrtovanih opravil | Omejitev izvajanja od 1 do 4.320 minut |
| Lastni časovni viri | Do osem partnerjev NTP |
| Izvoz dejavnosti | Do 150 shranjenih vnosov |
| Jeziki in teme | 20 jezikov, 13 tem z različicami poudarkov |

## Česa ne bo storil

- Počistil TPM, izbrisal zaščitnikov, ki jih ni ustvaril, ali prikazal obnovitvenih gesel.
- Spremenil nastavitve, ki jo nadzira pravilnik skupine ali upravljanje naprave.
- Zagnal popravila zgolj zato, ker pretekli dogodek ali uvoženo poročilo omenja težavo.
- Manjkajočih, delnih ali neuspešnih dokazov spremenil v oceno zdravega stanja.

## Poročila

Vsak zagon ustvari poročilo, ki ga lahko preberete, filtrirate in izvozite. Poročilo loči:

1. Kaj je bilo pregledano
2. Katera težava je bila najdena
3. Kateri dokazi podpirajo ugotovitev
4. Katero dejanje je bilo predlagano ali izvedeno
5. Kako je bil rezultat preverjen
6. Kaj ostaja neznano ali zahteva ponovni zagon

**Neobdelani izpis** ohrani tehnične dokaze z izvirnimi številkami vrstic, da lahko vsako ugotovitev povežete z virom. **Pregledno poročilo** povzame isti zagon v preprostem jeziku in najprej navede naslednji varen korak. Poročila HTML uporabljajo sistem tem Killer Tools in ostanejo berljiva brez aplikacije. Besedilo daje prednost prenosljivosti, JSON pa ohrani strukturirana polja za drug program.

## Shranjeni dnevniki in zajemi

KillerBench lahko raziskuje dokaze brez izvajanja dejanj v računalniku. Povlecite besedilni dnevnik v okno ali prilepite njegovo pot in izberite **Odpri dnevnik**. Vrstice CBS, CSI in Setup se razčlenijo s časovnimi oznakami, ugotovitve pa kažejo na točne izvirne vrstice. Enako lahko uvozite datoteke EVTX in XML dogodkov.

**Preglej Windows Update** na zahtevo samo za branje zbere dnevnike CBS, DISM, Panther in povrnitve namestitve, kanale dogodkov posodabljanja in servisiranja, stanja storitev posodabljanja, prosti prostor, čas delovanja, kazalnike čakajočega ponovnega zagona ter izbrane pravilnike posodabljanja. **Shrani zajem** zapiše ohranjene dokaze v datoteko `.kbench`. **Odpri zajem** jo znova odpre brez izvirnih datotek in ponovno oceni z nameščenimi pravili, zato ista pravila dajo iste ugotovitve.

## Predstavitveni način

Zaženite `KillerBench.exe --demo`, da preizkusite **Zaženi diagnostiko** na izmišljeni delovni postaji. Vsak postopek ustvari umetne dokaze in časovno razporejen napredek. Noben ponudnik Windows ni poklican, noben ukaz za popravilo se ne izvede in noben dnevnik se ne zapiše. Ločeni gumbi orodij zunaj **Zaženi diagnostiko** ostanejo aktivni, zato jih ne uporabljajte kot predstavitvena dejanja.

`KillerBench.exe --demo-screenshot` odpre poročilo celovitega pregleda sistema samo za ogled za določen izmišljeni računalnik. Kliki v delovnem prostoru, bližnjice in spuščanje datotek so blokirani.

## Paketi znanja

Obsežni in pogosto posodobljeni referenčni podatki so v paketih znanja namesto v diagnostični logiki: kode napak Windows in Setup, identifikatorji ponudnikov in naprav, reference PCI, USB, ACPI in UEFI, podatki o omrežnih ponudnikih OUI, lokacije gonilnikov in podpore ter izbrane povezave za podporo.

Vsak paket navaja format, različico sheme, vir, revizijo vira, čas nastanka in zgoščeno vrednost vsebine, KillerBench pa jih pred uporabo preveri. Neznan ali neveljaven paket ostane nedostopen namesto ugibanja. Posodobitev se prenese na začasno lokacijo, preveri in nato v enem koraku zamenja aktivni paket, zato ob neuspešni posodobitvi prejšnji paket ostane.

Podatki znanja lahko pojasnijo identifikator ali predlagajo, kje iskati naprej. Ne morejo spremeniti pretekle kode v trenutno napako, samodejno izbrati gonilnika ali odobriti popravila.

## Posodobitve in podpisovanje

Ob zagonu KillerBench z omejitvijo štirih sekund vpraša GitHub po najnovejši stabilni izdaji in ponudi posodobitev le, če je ta novejša. Preverjanje lahko izklopite z možnostjo **Ob zagonu preveri posodobitve** v razdelku O programu ali v pozivu za posodobitev. Prenese paket, ki ustreza zagnani kopiji, namestitveni ali prenosni. Pred namestitvijo se mora njegova vrednost SHA-256 ujemati z `SHA256SUMS.txt` izdaje. Aplikacija se zapre in znova odpre, da dokonča postopek. Storitve v ozadju ni.

Kartica O programu prikazuje različico, izdajatelja in prstni odtis podpisa kode ter SHA-256 zagnane izvršljive datoteke, da lahko preverite svojo kopijo.

## KillerMCP

KillerBench ne vključuje agenta umetne inteligence. [KillerMCP](../mcp.html) ga lahko poveže z istim Engine. **Možnosti KillerMCP** v razdelku O programu določajo, kaj sme agent storiti:

| Nastavitev | Učinek |
| --- | --- |
| Izklopljeno (privzeto) | Agent ne more zahtevati popravila. |
| Dovoli varna popravila | Agent lahko zahteva popravila, razvrščena kot varna. |
| Dovoli tudi moteča popravila | Moteča popravila postanejo na voljo, vsako pa še vedno zahteva odobritev. |

Raven lahko zviša le oseba pri tem računalniku. Po negotovem izidu popravila KillerMCP zapiše zaklep in popravila agentov ostanejo blokirana, dokler jih nekdo ne odklene v razdelku O programu.

## Lokalizacija

Vmesnik je na voljo v 20 jezikih: angleščini, bengalščini, češčini, nemščini, španščini, francoščini, madžarščini, italijanščini, japonščini, kazaščini, norveščini, poljščini, portugalščini (Brazilija), ruščini, slovenščini, turščini, ukrajinščini, vietnamščini ter poenostavljeni in tradicionalni kitajščini. Sprememba jezika velja takoj, vključno z imeni razdelkov in postopkov, pozdravnim besedilom ter odprtim berljivim poročilom. Morebitno manjkajoče besedilo se prikaže v angleščini.

Dokazi, ugotovitve Engine, dnevniki dejanj in surovi izhod ostanejo v angleščini, podobno kot dnevnik Windows, da je poročilo na vsakem računalniku enako in ga je mogoče iskati po točnem besedilu.

## Ukazna vrstica in izhodne kode

`killerbench-cli` izvaja isti Engine brez namizne aplikacije. Napredek piše na standardni izhod napak, JSON pa na standardni izhod. Večina ukazov privzeto prikaže predogled, sprememba pa zahteva `--execute`. Ustrezni ukazi `-recover` pregledajo prekinjeno dejanje brez ponavljanja. Glejte [Zagon programa KillerBench iz skriptov](use-the-cli.html) in [pregled ukazov](cli.html).

| Izhodna koda | Ukazi postopkov | Scan in open |
| --- | --- | --- |
| 0 | V preverjenih področjih ni nerešenih težav | Pokritost dokončana |
| 1 | Napaka zagona ali dnevnika | Napaka |
| 2 | Neveljavni argumenti | Neveljavni argumenti |
| 3 | Potrebna pozornost | Delno zbiranje |
| 4 | Pred preverjanjem je potreben ponovni zagon | |
| 130 | Ustavljeno | Preklicano ali presežena skupna časovna omejitev |

Nobena izhodna koda ne potrjuje, da je računalnik zdrav.

## Arhitektura

Engine je razdeljen v tri projekte brez uporabniškega vmesnika. `Engine.Core` zagotavlja osnovne gradnike in deklarativne postopke popravil, `Engine` upravlja skupne postopke in dokaze, `Engine.Windows` pa registrira izvorne ponudnike. Namizna aplikacija, CLI in preizkusi so ločeni gostitelji.

| Plast | Odgovornost |
| --- | --- |
| Zbiralniki zmogljivosti | Berejo omejeno stanje Windows za en točno določen obseg |
| Razčlenjevalniki dokazov | Preverjajo obliko, identiteto, popolnost in omejitve |
| Diagnostična razlaga | Pretvori dokaze v izrecna stanja postopkov |
| Usklajevanje postopkov | Združuje preverjanja, napredek, postopke popravil in dnevnike |
| Koordinatorji popravil | Znova preverijo upravičenost, pripravijo, spremenijo, preverijo in uskladijo |
| Poročila | Predstavijo težave, dejanja, rezultate in omejitve |
| Paketi znanja | Razrešijo različice referenčnih podatkov brez spremembe logike |

Zbiralniki ob dvoumni identiteti, nepravilnem izhodu, preseženi omejitvi ali nepreverljivem zahtevanem pooblastilu varno zavrnejo nadaljevanje. Engine je trenutno namenjen sistemu Windows in ni večplatformski. [Vodnik za razvijalce Engine](../engine/index.html) zajema API, CLI in vgradnjo Engine v drugega gostitelja .NET.

## Zasebnost in omrežje

Diagnostika in analiza dnevnikov potekata lokalno, brez storitve umetne inteligence ali računa KillerBench. Dnevniki dejanj in shranjeni dokazi ostanejo v mapah `%LOCALAPPDATA%\KillerBench` lokalnega računa.

Izbrana dejanja lahko vzpostavijo stik s cilji, kot so Windows Update, storitve za prenos programske opreme ali varnostno kopiranje obnovitvenih ključev v Active Directory ali Entra ID. Preverjanje posodobitev ob zagonu vzpostavi stik z GitHubom, posodobitve znanja in namestitveni program KillerMCP pa s svojimi nastavljenimi storitvami. Odjemalec umetne inteligence, ki uporablja [KillerMCP](../mcp.html), lahko prejme zahtevani referenčni rezultat.

Surovi dnevniki, uvožene datoteke in izvoženi dokazi lahko vsebujejo imena računalnikov, račune, poti ali občutljivo izvorno besedilo. Pred deljenjem jih preglejte in v javno poročilo nikoli ne vključujte obnovitvenih ključev, poverilnic ali podatkov strank.

## Stanje razvoja

KillerBench je v razvoju in javni prenos še ni na voljo. Obseg popravil se še širi, preverjanje izvornih popravil pa še poteka. Uspešna gradnja ali simulirano preverjanje ne dokazuje delovanja izvornih popravil. Za preverjanje vsake izvorne spremembe v različnih različicah sistema Windows in stanjih napak se uporabljajo začasni navidezni računalniki Windows. Celoten varnostni model je opisan v razdelku [Kako so spremembe varovane](safety.html).

## Slovar

Preproste razlage izrazov na tej strani v zaporedju izvirnega slovarja.

| Izraz | Pomen |
| --- | --- |
| BITS | Background Intelligent Transfer Service, storitev Windows za prenos posodobitev in drugih datotek v ozadju. |
| Zajem | Shranjena datoteka `.kbench` z ohranjenimi dokazi, ki jih je mogoče znova pregledati brez izvirnega računalnika. |
| CBS | Component-Based Servicing, podsistem Windows za nameščanje posodobitev in funkcij. Njegov dnevnik je `CBS.log`. |
| DISM | Deployment Image Servicing and Management, orodje Windows za preverjanje in popravilo shrambe komponent. |
| Povišanje pravic | Izvajanje s skrbniškimi pravicami po pozivu sistema Windows. KillerBench svojih pravic nikoli ne poviša sam. |
| Dokazi | Podatki, ki jih je KillerBench dejansko prebral iz sistema Windows in jih ohranil z virom za sledljivost ugotovitev. |
| Varna zavrnitev | Ko so dokazi dvoumni, manjkajoči ali preobsežni, je rezultat Neznano in spremembe niso dovoljene. |
| Dnevnik dejanj | Lokalni zapis JSON, ustvarjen pred spremembo in posodobljen po njej, da je mogoče prekinjeno dejanje pozneje pregledati. |
| Paket znanja | Različica preverjene datoteke referenčnih podatkov, kot so kode napak ali identifikatorji strojne opreme. |
| Panther | Mapa, v katero Windows Setup zapisuje dnevnike nadgradnje. |
| Čakajoče varovalo | Zapis, ki označuje, da je bila sprememba morda poslana sistemu Windows. Blokira ponovitev, dokler usklajevanje ne prebere izida. |
| Lastnik pravilnika | Pravilnik skupine ali upravljanje naprave, ki nadzira nastavitev. KillerBench teh nastavitev ne spreminja. |
| Postopek popravila | Tipizirano popravilo z lastnimi pravili upravičenosti, izvajanja, preverjanja in obnovitve. |
| Usklajevanje | Branje svežega stanja po prekinitvi za ugotavljanje, kaj se je zgodilo, brez ponavljanja spremembe. |
| SFC | System File Checker, orodje Windows za preverjanje in obnovitev zaščitenih sistemskih datotek. |
| TPM | Trusted Platform Module, varnostni čip, ki ga BitLocker uporablja za zaščito ključa pogona. |
| Preverjeno | Rezultat je bil znova prebran iz sistema Windows in se je ujemal s predvidenim učinkom spremembe. |
| Postopek | En zagon orodja: zbrana preverjanja, morebitni postopki popravil in ustvarjeno poročilo. |
