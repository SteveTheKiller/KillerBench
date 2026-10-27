# Naprave

Razdelek Naprave zajema tiskalnike, zvok, kamere, Bluetooth, vnos in storitve sistema Windows, od katerih je odvisna priključena strojna oprema.

Navodila po korakih: [Odpravljanje težav s tiskanjem](fix-printing.html) in [Raziskovanje sesutij in modrih zaslonov](investigate-crashes.html).

## Orodja v tem razdelku

Tiskanje, zvok, kamere, Bluetooth, vnos in podporne storitve priključene strojne opreme.

### Tiskanje

Preveri storitev Tiskalni posrednik, zaustavitve lokalnih čakalnih vrst, vidna tiskalna opravila, nastavitve načina brez povezave in stanja, ki jih sporočajo gonilniki. Nato popiše registracije čakalnih vrst, gonilnikov, vrat, končnih točk Standard TCP/IP, tiskalnih procesorjev in tiskalnih monitorjev. Nedavne težave PrintService poveže le, kadar je prisotna ena točna identiteta trenutne čakalne vrste. Za dodeljene končne točke Standard TCP/IP opravi en omejen poskus povezave brez podatkov aplikacije, SNMP ali tiskalnega opravila. Samodejno popravilo lahko zažene omogočeno storitev Tiskalni posrednik, nadaljuje le točno določene začasno ustavljene čakalne vrste s svežimi dokazi o čakajočih opravilih in izklopi način brez povezave. Namerne zaustavitve brez čakajočih opravil ostanejo le za pregled. Oddaljene povezave, nizi skupnosti SNMP, registracije razširitev in vsebina opravil se ohranijo. Koda stanja ali uspešna povezava ne potrjuje identitete tiskalnika, zalog, mehanike, sprejema v čakalno vrsto, uspešnega izrisa ali fizičnega tiskanja.

### Zvok

Preveri identiteti privzetega večpredstavnostnega vhoda in izhoda ter formate v skupni rabi, stanje zvočnih naprav in gonilnikov, Audio Endpoint Builder in Windows Audio. Samodejno popravilo zažene ustavljene omogočene storitve in preveri njihovo delovanje. Nastavitve zagona, gonilniki, izbrane naprave, izključni način in ravni glasnosti se ohranijo. Predvajanje zvoka, snemanje in usmerjanje v aplikacijah ostanejo nepreizkušeni.

### Podpora za kamero

Pregleda napake kamer in slikovnih naprav, ID-je strojne opreme, podatke o nameščenih gonilnikih ter razpoložljivost Windows Camera Frame Server. Izbirna obnovitev po preverjanju odvisnosti zažene le omogočeno, ustavljeno storitev. Aplikacija, ki že zahteva dostop do kamere, lahko nadaljuje ob obstoječih dovoljenjih. Nastavitve zasebnosti kamere, dovoljenja, nastavitve naprav, pravilniki, gonilniki in vrsta zagona se ohranijo. Brez ponastavitve, spremembe dovoljenj ali namestitve gonilnikov. Preverjanje potrjuje le sporočene nastavitve naprav in delovanje storitve, ne pa zajema slike, dostopa glede na zasebnost ali fizičnega delovanja.

### Podpora za Bluetooth

Pregleda napake naprav Bluetooth, ID-je strojne opreme, podatke o nameščenih gonilnikih ter razpoložljivost Bluetooth Support Service. Izbirna obnovitev po preverjanju odvisnosti zažene le omogočeno, ustavljeno storitev; seznanjene naprave se lahko znova povežejo. Seznanitve, nastavitve radia in naprav, pravilniki ter vrsta zagona se ohranijo. Brez ponastavitve, odstranitve naprav ali namestitve gonilnikov. Preverjanje potrjuje le sporočene nastavitve naprav in delovanje storitve, ne pa odkrivanja, seznanjanja, zvoka ali prenosa podatkov.

### Podpora za pametne kartice

Pregleda napake bralnikov pametnih kartic, ID-je strojne opreme, podatke o nameščenih gonilnikih in razpoložljivost storitve Smart Card. Izbirna obnovitev po preverjanju odvisnosti zažene le omogočeno, ustavljeno storitev. Aplikacije lahko nadaljujejo zaznavanje vstavljenih kartic skladno z obstoječim pravilnikom. Potrdila, PIN-i, poverilnice, nastavitve bralnikov in kartic, pravilniki, gonilniki ter vrsta zagona se ohranijo. Brez inicializacije, vpisa, dejanj s PIN-i, ponastavitve bralnikov ali namestitve gonilnikov. Preverjanje potrjuje le sporočene nastavitve bralnikov in delovanje storitve, ne pa zaznavanja kartic, preverjanja pristnosti ali dostopa aplikacij.

### Biometrična podpora

Pregleda napake biometričnih naprav, ID-je strojne opreme, podatke o nameščenih gonilnikih in razpoložljivost Windows Biometric Service. Izbirna obnovitev po preverjanju odvisnosti zažene le omogočeno, ustavljeno storitev. Obstoječe zahteve Windows Hello in aplikacij se lahko nadaljujejo skladno s trenutnim pravilnikom. Biometrične predloge, podatki Windows Hello, poverilnice, nastavitve naprav, dovoljenja, pravilniki, gonilniki in vrsta zagona se ohranijo. Brez vpisa, brisanja predlog, ponastavitve senzorjev, spremembe dovoljenj ali namestitve gonilnikov. Preverjanje potrjuje le sporočene nastavitve senzorjev in delovanje storitve, ne pa zajema, ujemanja, prijave ali dostopa aplikacij.

### Storitev naprav za uporabniški vnos

Preveri razpoložljivost storitve Human Interface Device Service. Izbirna obnovitev zažene le točno določeno storitev, ko je ustavljena in nastavljena na samodejni zagon, po svežem preverjanju odvisnosti. Ustavljena storitev z zagonom na zahtevo ostane le informativna. Obstoječe posebne tipke, večpredstavnostni gumbi in druge funkcije HID lahko znova delujejo. Naprave, gonilniki, preslikave gumbov, nastavitve dostopnosti in vnosa, pravilniki, dovoljenja storitve ter vrsta zagona se ohranijo. KillerBench ne izbere dejanj z napravami, gonilniki, preslikavami ali nastavitvami vnosa. Preverjanje potrjuje le stabilno stanje storitve, ne pa delovanja gumbov, kontrolnikov ali aplikacij.

### Storitev popisovanja prenosnih naprav

Preveri razpoložljivost storitve Portable Device Enumerator Service. Izbirna obnovitev zažene le točno določeno storitev, ko je ustavljena in nastavljena na samodejni zagon, po svežem preverjanju odvisnosti. Ustavljena storitev z zagonom na zahtevo ostane le informativna. Obstoječe aplikacije lahko nadaljujejo zaznavanje priključenih prenosnih naprav in dostop do njih. Naprave, gonilniki, datoteke, knjižnice, nastavitve sinhronizacije, dovoljenja, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. KillerBench ne izbere dejanj z napravami, datotekami, knjižnicami, sinhronizacijo ali prenosi. Preverjanje potrjuje le stabilno stanje storitve, ne pa naštevanja, dostopa, sinhronizacije ali prenosa.

### Zajem slik Windows

Preveri razpoložljivost storitve Windows Image Acquisition. Izbirna obnovitev zažene le točno določeno storitev, ko je ustavljena in nastavljena na samodejni zagon, po svežem preverjanju odvisnosti. Ustavljena storitev z zagonom na zahtevo ostane le informativna. Obstoječe zahteve za optično branje in zajem fotografij se lahko nadaljujejo ob trenutnih dovoljenjih aplikacij. Naprave, gonilniki, slikovni podatki, dovoljenja aplikacij, nastavitve zasebnosti in prenosa, pravilniki, dovoljenja storitve ter vrsta zagona se ohranijo. KillerBench ne izbere dejanj z napravami, gonilniki, slikami, dovoljenji, prenosi ali zajemom. Preverjanje potrjuje le stabilno stanje storitve, ne pa delovanja optičnega bralnika, kamere, prenosa ali aplikacije.

### Zaznavanje strojne opreme lupine

Pregleda omejene podatke Plug and Play o krmilnikih za shranjevanje, diskih in prenosnih napravah ter sklice na nameščene gonilnike, nato preveri razpoložljivost storitve Shell Hardware Detection. Izbirna obnovitev zažene le točno določeno storitev, ko je ustavljena in nastavljena na samodejni zagon, po svežem preverjanju odvisnosti. Ustavljena storitev z zagonom na zahtevo ostane le informativna. Obstoječa obvestila AutoPlay in o priklopu strojne opreme se lahko nadaljujejo skladno s trenutnim pravilnikom. Opažanja o napravah in shranjena zgodovina ne morejo odobriti obnovitve. Naprave, gonilniki, nosilci, priklopne točke, nastavitve AutoPlay, obravnavalniki, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. Ne zahteva dejanj z napravami ali nastavitvami. Preverjanje potrjuje le stabilno stanje storitve, ne pa dostave obvestil, pripravljenosti medijev, dostopa do nosilcev ali fizičnega delovanja naprav.

### Storitev namestitve naprav

Preveri razpoložljivost storitve Device Install Service. Izbirna obnovitev zažene le točno določeno storitev, ko je ustavljena in nastavljena na samodejni zagon, po svežem preverjanju odvisnosti. Ustavljena storitev z zagonom na zahtevo ostane le informativna. Obstoječe nameščanje naprav, ki ga je zahteval Windows, se lahko nadaljuje skladno s trenutnim pravilnikom. Naprave, gonilniki, paketi, nastavitve, vezave, nastavitve razredov, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. KillerBench ničesar ne izbere ali namesti. Preverjanje potrjuje le stabilno stanje storitve, ne pa namestitve ali delovanja naprav.

### Storitev povezovanja naprav

Preveri nedavno zgodovino nastavljanja naprav in Plug and Play ter razpoložljivost storitve Device Association Service. Izbirna obnovitev zažene le točno določeno storitev, ko je ustavljena in nastavljena na samodejni zagon, po svežem preverjanju odvisnosti. Ustavljena storitev z zagonom na zahtevo ostane le informativna. Obstoječa povezovanja naprav, ki jih je zahteval Windows, se lahko nadaljujejo. Seznanjene in povezane naprave, gonilniki, poverilnice, metapodatki, dovoljenja, pravilniki, dovoljenja storitve ter vrsta zagona se ohranijo. KillerBench ne izbere naprav ali dejanj seznanjanja, odstranjevanja, ponastavitve ali spreminjanja gonilnikov. Preverjanje potrjuje le stabilno stanje storitve, ne pa odkrivanja, seznanjanja, povezovanja ali delovanja naprav.

### Storitev izboljšave prikaza

Pregleda podatke o grafičnih vmesnikih in monitorjih ter preveri razpoložljivost storitve Display Enhancement Service. Izbirna obnovitev zažene le točno določeno storitev, ko je ustavljena in nastavljena na samodejni zagon, po svežem preverjanju odvisnosti. Ustavljena storitev z zagonom na zahtevo ostane le informativna. Obstoječe zahteve za svetlost, barve in izboljšave prikaza se lahko nadaljujejo. Nastavitve zaslona, svetlost, barvni profili, nastavitve HDR, naprave, gonilniki, nastavitve porabe energije, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. Nastavitev zaslona ne spreminja. Preverjanje potrjuje le stabilno stanje storitve, ne pa delovanja izboljšav, kakovosti slike ali fizičnega delovanja zaslona.

### Storitev upravljanja vnosa besedila

Pregleda podatke o tipkovnicah, miškah in napravah HID ter preveri razpoložljivost storitve Text Input Management Service. Izbirna obnovitev zažene le točno določeno storitev, ko je ustavljena in nastavljena na samodejni zagon, po svežem preverjanju odvisnosti. Ustavljena storitev z zagonom na zahtevo ostane le informativna. Obstoječe zahteve za zaslonsko tipkovnico, rokopis, čustvenčke in napredni vnos besedila se lahko nadaljujejo. Slovarji, podatki rokopisa, jezikovne nastavitve in nastavitve tipkovnice, dostopnosti ter zasebnosti, naprave, gonilniki, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. Ne zbira vnesenega besedila, pritiskov tipk ali vsebine rokopisa. Preverjanje potrjuje le stabilno stanje storitve, ne pa vnosa besedila, prepoznavanja ali delovanja naprav.

### Storitev predpomnilnika pisav Windows

Preveri razpoložljivost storitve Windows Font Cache Service. Izbirna obnovitev zažene le točno določeno storitev, ko je ustavljena in nastavljena na samodejni zagon, po svežem preverjanju odvisnosti. Ustavljena storitev z zagonom na zahtevo ostane le informativna. Obstoječe aplikacije lahko nadaljujejo uporabo trenutnega predpomnilnika pisav. Nameščene pisave, datoteke pisav, podatki aplikacij, datoteke predpomnilnika, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. KillerBench ne namesti ali odstrani pisav ter ne izbriše ali znova zgradi predpomnilnika. Preverjanje potrjuje le stabilno stanje storitve, ne pa izrisa besedila ali delovanja aplikacij.

### Gostitelj ponudnika odkrivanja funkcij

Preveri razpoložljivost storitve Function Discovery Provider Host. Izbirna obnovitev zažene le točno določeno storitev, ko je ustavljena in nastavljena na samodejni zagon, po svežem preverjanju odvisnosti. Ustavljena storitev z zagonom na zahtevo ostane le informativna. Obstoječe aplikacije lahko prek nameščenih ponudnikov nadaljujejo odkrivanje omrežnih naprav. Omrežni profili, skupne rabe, naprave, ponudniki, pravila požarnega zidu, nastavitve odkrivanja, poverilnice, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. KillerBench ne izbere dejanj z napravami, skupnimi rabami, ponudniki, pravili požarnega zidu ali nastavitvami odkrivanja. Preverjanje potrjuje le stabilno stanje storitve, ne pa zdravja ponudnikov, odkrivanja naprav ali povezljivosti.

### Objava virov za odkrivanje funkcij

Preveri razpoložljivost storitve Function Discovery Resource Publication. Izbirna obnovitev zažene le točno določeno storitev, ko je ustavljena in nastavljena na samodejni zagon, po svežem preverjanju odvisnosti. Ustavljena storitev z zagonom na zahtevo ostane le informativna. Ta računalnik lahko postane viden skladno z obstoječimi nastavitvami odkrivanja omrežja in požarnega zidu. Omrežni profili, skupne rabe, objavljeni viri, pravila požarnega zidu, nastavitve odkrivanja, poverilnice, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. KillerBench ne izbere dejanj s skupnimi rabami, viri, pravili požarnega zidu ali nastavitvami odkrivanja. Preverjanje potrjuje le stabilno stanje storitve, ne pa objave, vidnosti, dostopa ali povezljivosti.
