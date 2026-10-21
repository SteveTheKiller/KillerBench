# Posodobitve

Razdelek Posodobitve zajema Windows Update, podporne storitve in shrambo komponent. Ogledate si lahko nameščene in čakajoče posodobitve ter vzroke napak, razpoložljive posodobitve pa pregledate, preden izberete katero za namestitev.

Navodila po korakih: [Odpravljanje težav s storitvijo Windows Update](fix-windows-update.html) in [Popravilo sistemskih datotek](repair-system-files.html).

## Orodja v tem razdelku

Spremljajte servisiranje, preglejte razpoložljive posodobitve in popravite verigo storitev Windows Update.

### Spremljanje dejavnosti vzdrževanja

Opazuje omejeno dejavnost servisiranja Windows Update in namestitve v enem ali več posnetkih stanja. Možnosti določajo interval osveževanja in vključitev dogodkov posodobitev, zgodovine posodobitev, točno določenega načrtovanega opravila Windows Update, stanj točno določenih storitev, kazalnikov ponovnega zagona, prenosov trenutnega računa v ozadju, dejavnosti Delivery Optimization in dejavnosti Windows Installer z omejitvami zaradi zasebnosti. ID-ji procesov, ukazne vrstice, uporabniki in poti so izključeni. Dejavnost ne dokazuje napredka, dokončanja, zastoja ali napake. Ne zahteva nobenega dejanja s posodobitvami, namestitvijo, prenosi, opravili, storitvami, procesi, pravilniki, ponovnim zagonom ali popravili.

### Razpoložljive posodobitve

Poišče izbrane posodobitve programske opreme in gonilnikov z nastavljenim virom, storitvijo Windows Update ali že registrirano storitvijo Microsoft Update. Pred iskanjem preveri pripravljenost vira. Brez namestitve, sprejemanja licenc, registracije vira ali sprememb pravilnikov. To je ločeno od popravila servisiranja.

### Windows Update

Popiše omejen nabor stanj servisnih paketov, izbirnih funkcij in zmogljivosti sistema Windows, preveri kazalnike ponovnega zagona, prosti prostor in storitve posodabljanja, opazuje Delivery Optimization brez zagona te storitve ter pregleda shrambo komponent in zaščitene sistemske datoteke. Čakajoča stanja paketov, funkcij ali zmogljivosti zahtevajo pregled, vendar ne dovoljujejo dejanj s komponentami. Samodejno popravilo lahko zažene omogočene storitve Windows Update, BITS, Cryptographic Services, Windows Modules Installer in Update Orchestrator. Zagon Update Orchestrator lahko nadaljuje obstoječe načrtovane posodobitve skladno s trenutnim pravilnikom. Ohrani pakete, izbirne funkcije, zmogljivosti, predpomnilnik Delivery Optimization, enakovredne naprave, pasovno širino, pravilnike, vključitev virov, urnike in nastavitve zagona. DISM ali SFC zahteva sprotne dokaze o poškodbah. Ta postopek ne izbere ali odobri nobene posodobitve.

### Storitev Windows Update

Preveri stanje točno določene storitve Windows Update Service, njeno registrirano knjižnico DLL, omejeno trenutno dejavnost posodabljanja in zgodovino napak te storitve. Podatki o DLL, dejavnost posodabljanja in pretekle napake ne morejo odobriti zagona storitve ali posodobitvenega dejanja. Izbirna obnovitev zažene le ustavljeno samodejno storitev po svežem preverjanju odvisnosti in registracije izvršljive datoteke. Ustavljena storitev z zagonom na zahtevo ostane zgolj informacija. Obstoječe posodabljanje se lahko nadaljuje skladno s trenutnim pravilnikom. Ohrani pravilnike posodabljanja, vključitev virov, urnike, nastavitve ponovnega zagona, pakete, opravila BITS, dovoljenja storitve in vrsto zagona. KillerBench ne izbere, odobri ali namesti nobene posodobitve. Preverjanje potrjuje le stabilno stanje storitve, ne pa dosegljivosti vira, primernosti posodobitve, prenosa, namestitve, dokončanja ali pripravljenosti na ponovni zagon.

### Optimizacija dostave

Preveri stanje točno določene storitve Delivery Optimization. Ustavljena storitev z zagonom na zahtevo ostane zgolj informacija. Obnovitev je na voljo le pri točno določenem stanju Paused. Izbirna obnovitev ponovi preverjanje stanja, pošlje eno omejeno zahtevo za nadaljevanje in zahteva dve skladni opažanji stanja Running. Vrsta zagona, vsebina predpomnilnika, enakovredne naprave, pasovna širina, vir posodobitev in upravljani pravilniki se ohranijo. Dejavnost prenosa, stanje predpomnilnika, napredek posodabljanja in dokončanje ostanejo nepreverjeni.

### Storitev BITS

Preveri stanje točno določene storitve Background Intelligent Transfer Service, opravila prenosa trenutnega računa in omejeno zgodovino napak te storitve. Pretekle napake in posnetki prenosov ne morejo odobriti zagona storitve. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječe aplikacije lahko nadaljujejo čakajoče prenose v ozadju. Opravila, prioritete, lastniki, cilji, nastavitve posredniškega strežnika, pravilniki in vrsta zagona se ohranijo. Preverjanje potrjuje le stabilno stanje storitve, ne pa napredka prenosa, lastništva aplikacije ali dokončanja.

### Prenosi v ozadju (BITS)

Prebere opravila prenosa BITS v izbranem obsegu računov in pregleda točne identitete opravil. Imena čakalnih vrst ne dokazujejo lastništva aplikacij. Prenosov ne nadaljuje, dokonča, prekliče, izbriše ali predodeli.

### Kriptografske storitve

Preveri stanje točno določene storitve Cryptographic Services, njeno registrirano knjižnico DLL in omejeno zgodovino napak te storitve. Pretekle napake in podatki o DLL ne morejo odobriti zagona storitve ali zamenjave komponente. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječa dejanja s katalogi, potrdili in posodobitvami se lahko nadaljujejo. Potrdila, ključi, katalogi, nastavitve zaupanja, pravilniki in vrsta zagona se ohranijo. Preverjanje potrjuje le stabilno stanje storitve, ne pa veljavnosti potrdil, podpisov, katalogov ali zaupanja.

### Namestitveni program modulov Windows

Preveri stanje točno določene storitve Windows Modules Installer, omejene prehode stanj servisnih paketov in zgodovino napak te storitve. Pretekle napake in stanja paketov ne morejo odobriti zagona storitve ali dejanja s paketom. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječe servisiranje se lahko nadaljuje. Paketi, vsebina shrambe komponent, viri, pravilniki in vrsta zagona se ohranijo. Preverjanje potrjuje le stabilno stanje storitve, ne pa zdravja shrambe komponent, primernosti paketov, dokončanja servisiranja ali pripravljenosti na ponovni zagon.

### Storitev Windows Update Medic

Preveri stanje točno določene storitve Windows Update Medic Service, njeno registrirano knjižnico DLL, omejeno trenutno dejavnost posodabljanja in zgodovino napak te storitve. Podatki o DLL, dejavnost posodabljanja in pretekle napake ne morejo odobriti zagona storitve ali dejanja s komponentami. Ustavljena storitev z zagonom na zahtevo je lahko običajna. Ko je izbrano točno to orodje, lahko izbirna obnovitev po svežem preverjanju odvisnosti zažene le ustavljeno omogočeno storitev. Obnovitveni postopki sistema Windows se lahko nadaljujejo skladno s trenutnim pravilnikom ter popravijo komponente posodabljanja ali sprožijo posodobitve. Ohrani pravilnike posodabljanja, vključitev virov, urnike, pakete, opravila BITS, nastavitve ponovnega zagona, dovoljenja storitve in vrsto zagona. KillerBench ne izbere posodobitev in ne spremeni nastavitev posodabljanja. Preverjanje potrjuje le stabilno stanje storitve, ne pa uspeha obnovitve, zdravja komponent, primernosti, prenosa, namestitve ali pripravljenosti na ponovni zagon.

### Storitev usklajevanja posodobitev

Preveri stanje točno določene storitve Update Orchestrator Service, točno določeno vgrajeno opravilo Windows Update, omejeno trenutno dejavnost in zgodovino napak te storitve. Rezultati opravil, dejavnost posodabljanja in pretekle napake ne morejo odobriti zagona storitve ali posodobitvenega dejanja. Izbirna obnovitev zažene le ustavljeno samodejno storitev po svežem preverjanju odvisnosti in registracije izvršljive datoteke. Ustavljena storitev z zagonom na zahtevo ostane zgolj informacija. Obstoječe načrtovane posodobitve se lahko nadaljujejo skladno s trenutnim pravilnikom ter prenesejo ali namestijo že odobrene posodobitve. Ohrani pravilnike posodabljanja, vključitev virov, urnike, nastavitve ponovnega zagona, pakete, opravila BITS, dovoljenja storitve in vrsto zagona. KillerBench ne izbere ali odobri nobene posodobitve. Preverjanje potrjuje le stabilno stanje storitve, ne pa primernosti, prenosa, namestitve, dokončanja ali pripravljenosti na ponovni zagon.

### Čiščenje shrambe komponent

Analizira, ali Windows priporoča čiščenje shrambe komponent. Izbirno vzdrževanje takoj odstrani nadomeščene komponente in nato ponovi analizo. Brez ResetBase ali ročnega brisanja WinSxS. Samodejna razveljavitev ni na voljo; količina sproščenega prostora se ne meri.
