# Diagnostika

Vsak pregled začnite z diagnostiko. Vsako orodje prebere računalnik in ustvari poročilo. Nič v tem razdelku ne spremeni sistema Windows. Privzeto orodje, celoten pregled sistema, izvede širok nabor preverjanj samo za branje in razvrsti področja, ki zahtevajo pozornost.

Navodila po korakih: [Raziščite zrušitve in modre zaslone](investigate-crashes.html) in [Popravite sistemske datoteke](repair-system-files.html).

## Orodja v tem razdelku

Najprej preverite računalnik, nato v poročilu preberite, kaj zahteva pozornost in kateri je naslednji varen korak.

### Celoten pregled sistema

Pregleda 49 diagnostičnih področij in ovrednoti nameščene postopke popravil glede na trenutne dokaze. Preverjanja lahko trajajo več minut. Celoten pregled sistema je samo diagnostičen. Za izbiro in potrditev enega podprtega dejanja po pregledu svežih pogojev uporabite ciljni potek popravila. Pretekli posnetki, posnetki varnostnih kopij in prenosov ostanejo samo za branje. Vsebina izpisov zrušitev se ne odpre. Pregled oznake umazanosti ne načrtuje popravila ali ponovnega zagona. Ta pregled ne namešča posodobitev, spreminja gonilnikov, briše datotek ali izvaja popolnega preizkusa strojne opreme.

### Ključni kazalniki

Zajame razpoložljivi pomnilnik, stanje diskov po podatkih ponudnika in stanje zaščite BitLocker ter poveže trenutne kazalnike s težavami naprav, zgodovino zanesljivosti, zapisi napak strojne opreme, dokazi o zrušitvah, nepričakovanimi zaustavitvami in rezultati diagnostike pomnilnika Windows. Kazalniki ponovnega zagona, prosti prostor na sistemskem disku, povišane pravice in nastavitve storitev posodobitev ostanejo vidni. Obnovitvena gesla se ne zbirajo. V tem poteku dela se popravila ne izvajajo. Združeni rezultat ni popoln preizkus strojne opreme, programske opreme ali stabilnosti.

### Zgodovina zanesljivosti

Prebere zadnjih 14 dni zapisov Nadzornika zanesljivosti Windows skupaj z nedavnimi napakami aplikacij in nepričakovanimi zaustavitvami ter vrne en omejen sklep o zgodovini. Pretekli zapisi ne dokazujejo trenutnega stanja ali osnovnega vzroka. Popravilo ni izbrano, aplikacije, storitve in nastavitve pa se ne spremenijo.

### Zmogljivost ob zagonu

Prebere zadnjih 14 dni metapodatkov dogodkov o zmogljivosti zagona in zaustavitve sistema Windows, ohrani samo identiteto ponudnika in omejena številska polja trajanja ter jih poveže s podprtimi kazalniki čakajočega ponovnega zagona. Poti, imena procesov, računi in druga vsebina dogodkov se odstranijo pred shranjevanjem dokazov. Pretekli zapisi poslabšanja ne prepoznajo trenutnega ozkega grla in ne dovoljujejo sprememb zagona, storitev, aplikacij, gonilnikov, opravil, napajanja, ponovnega zagona ali pravilnikov.

### Zgodovina napak strojne opreme

Prebere zadnjih 14 dni zgodovine Windows Hardware Error Architecture in diagnostike pomnilnika, trenutne težave naprav Plug and Play, stanje fizičnih diskov in števce zanesljivosti ponudnika ter vrne en omejen sklep o strojni opremi. Zapise je treba povezati z njihovo vsebino, diagnostiko vdelane programske opreme in preizkusi strojne opreme ponudnika. Ne sklepa o okvarjeni komponenti ali osnovnem vzroku, ne izbere popravila in ne spremeni strojne opreme, vdelane programske opreme, gonilnikov, diskov, zbirk, nosilcev, datotek, storitev, napajanja, ponovnega zagona ali nastavitev.

### Zgodovina namestitev gonilnikov

Prebere ohranjeni konec zgodovine namestitev naprav Windows SetupAPI, poveže neuspešne razdelke s točnimi trenutnimi identitetami Plug and Play in vrne en omejen sklep. Pretekli razdelki dnevnika ne dokazujejo, da je naprava še prisotna ali da je napaka trenutna. Noben gonilnik se ne namesti, odstrani, povrne ali posodobi.

### Obnovitvene točke sistema

Navede do 256 razpoložljivih obnovitvenih točk, pregleda izrecne pogoje za ustvarjanje in vrne en omejen sklep o pripravljenosti. Popis ne dokazuje, da je zaščita omogočena za vsak nosilec ali da bo obnovitev uspela. Med diagnostiko se obnovitvena točka ne ustvari, izbriše ali uporabi.

### Obnovitveno okolje Windows

Prebere trenutne nastavitve obnovitvenega okolja Windows z REAgentC, poveže omogočeno lokalno mesto s točnim trenutnim diskom in particijo ter vrne en omejen sklep o pripravljenosti. Izbirno samodejno popravilo lahko omogoči onemogočeno obstoječo konfiguracijo in nato preveri sporočeno stanje. Obnovitvena slika se ne prenese, premakne ali zamenja, obnovitveni zagon se ne sproži in Windows se ne zažene znova. Omogočeno stanje in prisotnost particije ne dokazujeta, da se obnovitvena slika lahko zažene ali popravi ta računalnik.

### Aktiviranje sistema Windows

Prebere trenutno stanje licence Windows brez zbiranja celotnih ali delnih ključev izdelka, ohrani zadnjih sedem dni očiščenih metapodatkov opozoril, napak in kritičnih dogodkov Software Protection ter vrne en omejen sklep o delovanju. Vsebina dogodkov se odstrani pred shranjevanjem dokazov. Pretekli zapisi ostanejo kontekst, ko trenutna licenca poroča Licensed. Ne izbere dejanj aktiviranja, ključev izdelka, žetonov, licenc, računov, storitev ali pravilnikov. Stanje ponudnika in zgodovina dogodkov ne dokazujeta upravičenosti do naročnine, dosegljivosti strežnika za aktiviranje, celovitosti shrambe žetonov, prihodnje veljavnosti ali osnovnega vzroka.

### Storitve Windows

Popiše imena nameščenih storitev, prikazna imena, stanja, načine zagona in vrste obnovitvenih dejanj z omejitvami zasebnosti, brez zbiranja računov, poti izvršljivih datotek, obnovitvenih ukazov ali sporočil za ponovni zagon. Pregleda skladnost načinov zagona, neposredne identitete odvisnosti in cikle, znova preveri točna prehodna stanja, pregleda podprte storitve in odvisnosti ustavljenih storitev ter poveže nedavne napake Service Control Manager samo, če je prisotno eno točno ime storitve ali enolično prikazno ime. Uporaba skupin vrstnega reda nalaganja se prešteje brez razširitve članstva, dvoumne identitete pa ostanejo nerazrešene. Ustavljene storitve z zagonom na zahtevo so lahko običajne. Nobena storitev se ne zažene, nastavitve zagona, odvisnosti in obnovitve pa se ohranijo. Če določena funkcija potrebuje obnovitev, uporabite ustrezni potek Popravilo.

### Napake aplikacij

Prebere nedavne napake in kritične dogodke Application skupaj z omejeno zgodovino Nadzornika zanesljivosti ter vrne en sklep o zgodovini stabilnosti. Pretekli zapisi ne dokazujejo trenutne napake ali osnovnega vzroka in samo usmerjajo ožjo diagnostiko. Nobena aplikacija se ne ustavi, popravi, odstrani ali znova nastavi.

### Prikazovalne naprave

Pregleda grafične vmesnike in zaslone za sporočene napake naprav, ID-je strojne opreme in nameščene različice gonilnikov. Brez namestitve gonilnikov, ponastavitve naprav ali spremembe ločljivosti. Kakovost slike in fizične povezave se ne preizkusijo.

### Vhodne naprave

Pregleda napake tipkovnic, mišk in naprav HID ter popis gonilnikov. Ne zajema pritiskov tipk, ponastavlja naprav ali spreminja gonilnikov. Fizično delovanje vnosa se ne preizkusi.

### Težave z napravami

Poišče neničelne kode napak konfiguracije Plug and Play v različnih razredih naprav, nato poveže dokaze o trenutno prisotnih napravah z omejeno zgodovino namestitev gonilnikov SetupAPI, nastavitve naprav in Plug and Play. Pretekli zapisi sami ne dokazujejo trenutne napake. Preglejte točno identiteto naprave in odobrene dokaze o gonilniku brez spreminjanja strojne opreme, gonilnikov ali nastavitev. Manjkajoče naprave in fizično delovanje niso preverjeni.
