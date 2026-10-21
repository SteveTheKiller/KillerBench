# Popravilo

Razdelek Popravilo združuje orodja za odpravljanje težav. Vsako najprej opravi diagnostiko. Popravilo je na voljo le, ko sveži podatki potrdijo točno tisto težavo, ki jo odpravlja, in se izvede šele, ko vklopite **Samodejno popravi potrjene težave** ali sami potrdite posamezno popravilo. Vsako popravilo se zabeleži v dnevnik in nato preveri.

Navodila po korakih: [Popravilo sistemskih datotek](repair-system-files.html) in [Odpravljanje težav s storitvijo Windows Update](fix-windows-update.html).

## Samodejno in ročno popravilo

Možnost **Samodejno popravi potrjene težave** diagnostičnemu orodju dovoljuje, da po pregledu izvede popravila, za katera dokaže, da so potrebna. Privzeto je izklopljena.

Za posamezno popravilo servisiranja odprite **Napredno: dnevniki in ročna popravila** in izberite **Popravila**. Izberite **Shramba komponent (DISM)** ali **Sistemske datoteke (SFC)**, preberite predogled, označite **Izvedi to popravilo na tem računalniku** in pritisnite **Izvedi popravilo**. V razdelku [Kako so spremembe varovane](safety.html) je pojasnjeno, kaj se zgodi pred vsako spremembo in po njej.

## Orodja v tem razdelku

Uporabite jih, ko diagnostika prepozna težavo. Popravila zahtevajo potrjene dokaze in izrecno odobritev.

### Osnovne storitve Windows

Skupaj preveri storitve Power, Plug and Play, User Manager, Group Policy Client, Network Store Interface, RPC Endpoint Mapper, Remote Procedure Call, DCOM Server Process Launcher, Background Tasks Infrastructure in CoreMessaging. Izbirna obnovitev zažene le točno določene omogočene samodejne storitve, za katere sveži podatki po zagonskem obdobju in popolnem preverjanju odvisnosti potrdijo, da so ustavljene. Storitve z zagonom na zahtevo med mirovanjem ostanejo nespremenjene. Ohrani načrte porabe energije, naprave, gonilnike, račune, poverilnice, profile, pravilnike, vmesnike, naslove, poti, DNS, nastavitve požarnega zidu, registracije, končne točke, registracije opravil, podatke aplikacij, dovoljenja storitev in vrste zagona. Ne zahteva osvežitve pravilnikov, pregleda naprav, dejanj z računi, sprememb omrežja, nastavitev porabe energije ali registracij ter zagona opravil. Preverjanje potrjuje le stabilno stanje storitev, ne pa celovitega delovanja funkcij.

### Sistemske datoteke

Popiše omejen nabor stanj servisnih paketov, izbirnih funkcij in zmogljivosti sistema Windows, preveri predpogoje, pregleda zaščitene sistemske datoteke in samodejno izbere SFC le, če sprotno preverjanje potrdi poškodbe. Čakajoča stanja paketov, funkcij ali zmogljivosti zahtevajo pregled, vendar ne dovoljujejo namestitve, odstranitve, čiščenja, povrnitve, omogočanja ali onemogočanja.

### Skladišče WMI

Preveri razpoložljivost storitve WMI in nato skladnost repozitorija. Samodejno popravilo lahko zažene ustrezno ustavljeno storitev in reši potrjeno poškodovani repozitorij, nato pa preveri vsak rezultat. Nastavitve onemogočene storitve se ohranijo. Berljive registracije se združijo; neberljiva vsebina lahko zahteva ponovno namestitev ponudnika. Odjemalci za upravljanje so lahko prekinjeni. Ni ponastavitve, brisanja ali samodejne povrnitve.

### Sistemska obvestila Windows

Prebere nedavno zgodovino upravitelja Service Control Manager ter skupaj preveri COM+ Event System in System Event Notification Service. Izbirna obnovitev preveri pripravljenost COM+ Event System, preden zažene SENS. Obstoječa obvestila o prijavi, omrežju, napajanju in COM+ se lahko nadaljujejo ob trenutnih nastavitvah. Ohrani kataloge COM+, naročnine, registracije obvestil, prejemnike, račune, dovoljenja, nastavitve obnovitve, pravilnike in vrste zagona. Ne spremeni nobene naročnine, registracije, prejemnika, računa, dovoljenja ali pravilnika. Preverjanje potrjuje le stabilno stanje storitev, ne pa dostave obvestil od začetka do konca.

### Storitev diagnostičnih pravilnikov

Pregleda omejeno zgodovino delovanja Diagnostic Policy, zgodovino napak točno določene storitve in njeno trenutno pripravljenost. Nepopolni podatki imajo prednost. Pretekli zapisi ne morejo odobriti obnovitve storitve ali dejanja za odpravljanje težav. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječi diagnostični postopki se lahko nadaljujejo. Pravilniki, diagnostični podatki in vrsta zagona se ohranijo. Ne zažene orodja za odpravljanje težav ali predlaganega popravka. Preverjanje potrjuje le stabilno stanje storitve, ne pa odkrivanja napak, dokončanja diagnostike ali izida popravila.

### Poročanje o napakah sistema Windows

Pregleda nastavitve izpisov ob sesutju, omejene metapodatke datotek izpisov, nedavna poročila o sesutjih sistema, zgodovino napak točno določene storitve Windows Error Reporting in njeno trenutno pripravljenost. Pretekla poročila ne potrjujejo trenutne napake ali osnovnega vzroka in ne morejo odobriti obnovitve. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječa čakajoča poročila se lahko nadaljujejo skladno s trenutnim pravilnikom. Arhivi poročil, izpisi, soglasja, pravilnik nalaganja in vrsta zagona se ohranijo. Ne zahteva nalaganja ali brisanja. Preverjanje potrjuje le stabilno stanje storitve, ne pa ustvarjanja, nalaganja ali dostave poročil, rešitve težave ali diagnostike sesutja.

### Zbiralnik dogodkov Windows

Pregleda omejen popis naročnin Windows Event Collector, zgodovino napak točno določene storitve in njeno trenutno pripravljenost. Prazen popis je lahko nameren; število naročnin ali pretekle napake ne morejo odobriti obnovitve. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječe naročnine lahko nadaljujejo zbiranje oddaljenih dogodkov. Naročnine, posredovani dogodki, poverilnice, nastavitve kanalov, pravilniki in vrsta zagona se ohranijo. Preverjanje potrjuje le stabilno stanje storitve, ne pa dosegljivosti virov, preverjanja pristnosti, oddaljene dostave, svežine dogodkov ali stanja naročnin.

### Dnevnik dogodkov Windows

Pregleda omogočenost, način hranjenja, trenutno velikost, nastavljeno omejitev, število zapisov in zasičenost štirih diagnostičnih kanalov ter preveri razpoložljivost storitve Windows Event Log. Samodejno popravilo lahko po preverjanju odvisnosti zažene ustavljeno, omogočeno storitev in preveri njeno delovanje. Ločeno potrjeno usmerjeno dejanje lahko omogoči en točno določen onemogočen neupravljan diagnostični kanal, pri čemer ohrani vse druge sporočene nastavitve. Dnevniki, dovoljenja kanalov, hranjenje, omejitve velikosti, vrste zagona in upravljane nastavitve se ohranijo. Uspešna dostava dogodkov in zmogljivost arhiva ostaneta nepreverjeni.

### Časovna storitev Windows

Preveri nastavitve, storitev in sinhronizacijo Windows Time. Samodejno popravilo lahko zažene W32Time, če trenutno stanje sporoča točno določeno napako ustavljene storitve in so sveža preverjanja pravilnikov, načina ter odvisnosti uspešna. Nato lahko zahteva ponovno sinhronizacijo in preveri sveže stanje. To lahko prilagodi uro z uporabo že nastavljenih virov. Vrsta zagona, nastavitve virov in upravljane nastavitve se ohranijo.

### Storitev razporejevalnika opravil

Popiše omejen nabor stanj načrtovanih opravil, rezultatov izvajanja in števil zamujenih zagonov ter jih združi z zgodovino napak točno določene storitve Task Scheduler, preden preveri njeno trenutno razpoložljivost. Ugotovitve o opravilih in pretekle napake storitve ne morejo odobriti njenega zagona. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječa omogočena opravila se lahko začnejo izvajati pod nastavljenimi računi, vključno z zamujenimi sprožilci. Ne spremeni definicij opravil, izvajalcev, sklicev na poverilnice, pravilnikov ali vrste zagona in nobenega opravila neposredno ne zažene ali izbriše. Preverjanje potrjuje le stabilno stanje storitve, ne pa uspeha dejanj, veljavnosti poverilnic ali prihodnjih izidov opravil.

### Storitev uporabniških profilov

Preveri razpoložljivost storitve User Profile Service. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječe zahteve za prijavo in profile se lahko nadaljujejo. Mape profilov, panji registra, dovoljenja, registracije seznamov profilov, poverilnice, uporabniški podatki, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. Ne zahteva nobenega dejanja s profili. Preverjanje potrjuje le stabilno stanje storitve, ne pa prijave, nalaganja profilov ali dostopa do uporabniških podatkov.

### Storitve varnostnega kopiranja

Preveri storitve Volume Shadow Copy, Microsoft Software Shadow Copy Provider, Windows Backup in Block Level Backup Engine ter prebere stanje zapisovalnikov VSS, vrste registriranih ponudnikov, trenutne posnetke in dodelitev prostora za senčne kopije. Ustavljene storitve z zagonom na zahtevo so med mirovanjem lahko običajne. Izbirna obnovitev po preverjanju odvisnosti zažene le ustavljene samodejne storitve in nato preveri njihovo delovanje. Obstoječa načrtovana ali prekinjena varnostna kopiranja se lahko nadaljujejo. Ohrani nastavitve onemogočenega zagona in zagona na zahtevo, posnetke, prostor za senčne kopije, registracije ponudnikov, načrte varnostnega kopiranja, kataloge, urnike, cilje, poverilnice in hranjenje. Ne izbere ali spremeni varnostnega kopiranja, ne zažene storitve znova in ne izbriše posnetka ali kataloga. Registracije ponudnikov, stanje zapisovalnikov, dodelitev prostora in stabilno stanje storitev ne potrjujejo dokončanja varnostnega kopiranja, dostopa do cilja ali uspešne obnovitve.
