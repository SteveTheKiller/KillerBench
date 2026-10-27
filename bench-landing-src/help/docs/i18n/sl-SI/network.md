# Omrežje

Razdelek Omrežje pregleda povezave, DNS, poti, brezžično in žično prijavo, skupno rabo datotek ter oddaljeni dostop. Orodja, ki dostopajo do drugega gostitelja, uporabijo cilj in časovno omejitev, ki ju določite. Nič se ne spremeni, dokler ne potrdite popravila.

Navodila po korakih: [Odpravljanje težav z omrežjem in internetom](fix-network.html).

## Orodja v tem razdelku

Preglejte povezave, DNS, poti, brezžično omrežje, skupno rabo in oddaljeni dostop.

### Omrežje

Pregleda vmesnike, stanje in izvor naslovov, prehode, nastavitve DNS, veljavni pravilnik imenskih prostorov DNS, nastavitve TCP, strukturo izvornega kataloga protokolov Winsock in ponudnikov imenskih prostorov, tabelo usmerjanja IP ter preslikave v datoteki Hosts. Samodejno popravilo lahko zažene omogočene storitve DNS Client, DHCP Client, SMB client, Network Location Awareness, Network List in IP Helper ter obnovi najeme na povezanih fizičnih vmesnikih DHCP brez uporabnega naslova IPv4. Zagon storitev za prepoznavanje omrežja lahko znova določi identiteto omrežja in uporabi drug obstoječi profil požarnega zidu. IP Helper lahko aktivira obstoječe funkcije prehoda IPv6, posredovanja vrat in omrežnih obvestil. Obnovitev storitve preveri delovanje, ne pa uspešnega razreševanja imen, izbire poti, prepoznavanja omrežja ali delovanja tunelov. Poti, vnosi Hosts, strežniki DNS, profili, pravila požarnega zidu, ponudniki Winsock, nastavitve prehoda, vrste zagona in pravilniki se ohranijo. Obnovitev najema lahko spremeni dodeljene naslove in prekine povezave. Statične nastavitve in navidezni vmesniki se ohranijo. Odzivi ICMP točno določenega prehoda ne potrjujejo dosegljivosti interneta.

### Brezžično omrežje

Pregleda omrežne naprave in razpoložljivost WLAN AutoConfig. Izbirna obnovitev po preverjanju odvisnosti zažene le omogočeno, ustavljeno storitev; shranjeni profili se lahko samodejno znova povežejo. Profili, poverilnice, pravilniki, nastavitve radia in vrste zagona se ohranijo. Brez ponastavitve vmesnikov ali namestitve gonilnikov. Preverjanje potrjuje delovanje storitve, ne pa brezžičnega preverjanja pristnosti ali povezljivosti.

### Preverjanje pristnosti v žičnem omrežju (802.1X)

Pregleda omrežne naprave in Wired AutoConfig za omrežja, ki zahtevajo 802.1X. Izbirna obnovitev po preverjanju odvisnosti zažene le omogočeno, ustavljeno storitev. Obstoječi profili preverjanja pristnosti se lahko aktivirajo in spremenijo omrežni dostop. Potrdila, poverilnice, profili, pravilniki in nastavitve zagona se ohranijo. Preverjanje potrjuje le delovanje storitve, ne pa preverjanja pristnosti ali povezljivosti.

### Omrežne povezave

Pregleda omejene trenutne dodelitve omrežnih profilov, zgodovino napak točno določene storitve in trenutno pripravljenost Network Connections. Nepopolni podatki imajo prednost. Javni profili in pretekle napake so le kontekst ter ne morejo odobriti obnovitve ali spremembe omrežja. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječi vmesniki za upravljanje omrežja lahko znova delujejo. Vmesniki, naslovi, poti, nastavitve DNS, profili povezav, poverilnice, nastavitve požarnega zidu, pravilniki in vrsta zagona se ohranijo. Povezav ali omrežnih nastavitev ne spreminja. Preverjanje potrjuje le stabilno stanje storitve, ne pa delovanja vmesnikov, dodeljevanja naslovov, usmerjanja, razreševanja imen ali povezljivosti.

### Samodejni posrednik WinHTTP

Pregleda shranjene privzete nastavitve WinHTTP, internetne nastavitve trenutnega računa, zgodovino napak točno določene storitve in trenutno pripravljenost WinHTTP Auto-Proxy. Nepopolni podatki imajo prednost. Nastavitve posredniškega strežnika in pretekle napake ne morejo odobriti obnovitve storitve ali odkrivanja. Ustavljena storitev z zagonom na zahtevo je lahko običajna. Ko je izbrano točno to orodje, lahko izbirna obnovitev po svežem preverjanju odvisnosti zažene le ustavljeno omogočeno storitev. Obstoječe aplikacije lahko nadaljujejo odkrivanje WPAD ali PAC ob trenutnih nastavitvah. Definicije posredniških strežnikov, URL-ji PAC, poverilnice, omrežne nastavitve, nastavitve požarnega zidu, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. Ne pridobi nobenega URL-ja in ne spremeni nastavitev posredniškega strežnika ali omrežja. Preverjanje potrjuje le stabilno stanje storitve, ne pa odkrivanja, dosegljivosti, preverjanja pristnosti, lastništva pravilnikov ali uporabe v aplikacijah.

### Upravitelj povezav Windows

Pregleda omejene trenutne dodelitve omrežnih profilov, zgodovino napak točno določene storitve in trenutno pripravljenost Windows Connection Manager. Nepopolni podatki imajo prednost. Javni profili in pretekle napake so le kontekst ter ne morejo odobriti obnovitve ali odločitve o povezavi. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Windows lahko nadaljuje samodejno odločanje o povezavah skladno z obstoječimi profili in pravilniki. Vmesniki, profili, poverilnice, nastavitve merjenja porabe, stroškov, gostovanja, požarnega zidu, VPN in preverjanja pristnosti ter vrsta zagona se ohranijo. Profilov, vmesnikov ali nastavitev ne spreminja. Preverjanje potrjuje le stabilno stanje storitve, ne pa primernosti profilov, izbire povezave, preverjanja pristnosti, usmerjanja, razreševanja imen ali povezljivosti.

### Storitev nastavitve omrežja

Pregleda omejene trenutne vezave protokolov vmesnikov, zgodovino napak točno določene storitve in trenutno pripravljenost Network Setup Service. Nepopolni podatki imajo prednost. Stanje vezav in pretekle napake ne morejo odobriti obnovitve storitve ali spremembe nastavitev. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječe čakajoče nastavljanje omrežja se lahko nadaljuje skladno s pravilniki sistema Windows. Vmesniki, gonilniki, paketi gonilnikov, vezave, naslovi, poti, nastavitve DNS, profili, poverilnice, nastavitve požarnega zidu, pravilniki in vrsta zagona se ohranijo. KillerBench ne izbere spremembe gonilnika ali nastavitev. Preverjanje potrjuje le stabilno stanje storitve, ne pa razpoložljivosti vmesnikov, primernosti gonilnikov, dokončanja nastavljanja, dodeljevanja naslovov, usmerjanja, razreševanja imen ali povezljivosti.

### Skupna raba datotek (SMB)

Preveri razpoložljivost strežnika Windows SMB. Izbirna obnovitev po svežem preverjanju odvisnosti zažene le omogočeno, ustavljeno storitev Server. Obstoječe skupne rabe datotek in tiskalnikov lahko postanejo dosegljive ob trenutnih dovoljenjih in pravilnikih požarnega zidu. Ne spremeni skupnih rab, dovoljenj, nastavitev SMB, pravil požarnega zidu, poverilnic, pravilnikov ali vrste zagona. Preverjanje potrjuje le stabilno stanje storitve, ne pa dostopa odjemalcev ali prenosa datotek.

### Upravitelj povezav za oddaljeni dostop

Pregleda omejene nastavitve profilov VPN trenutnega računa, zgodovino napak točno določene storitve in trenutno pripravljenost Remote Access Connection Manager brez zbiranja skrivnosti. Prazen popis je lahko nameren; prisotnost profilov, oznake povezav ali pretekle napake ne morejo odobriti obnovitve ali poskusa povezave. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječe zahteve za klicne povezave in VPN se lahko nadaljujejo. Profili povezav, poverilnice, poti, vmesniki, nastavitve preverjanja pristnosti, pravilniki in vrsta zagona se ohranijo. Ne vzpostavi nobene povezave. Preverjanje potrjuje le stabilno stanje storitve, ne pa primernosti profilov, dosegljivosti strežnikov, preverjanja pristnosti, vzpostavljanja tunelov, dodajanja poti, razreševanja imen ali povezljivosti.

### Storitve oddaljenega namizja

Pregleda lokalno omogočenost oddaljenega namizja, preverjanje pristnosti na ravni omrežja in vrata poslušalnika, omejeno zgodovino povezav in sej, zgodovino napak točno določene storitve ter njeno trenutno pripravljenost. Nepopolni podatki imajo prednost. Pretekli zapisi ne morejo odobriti obnovitve storitve ali sprememb nastavitev. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječi poslušalnik lahko postane dosegljiv ob trenutnih pravilnikih, NLA, pravilih požarnega zidu in dovoljenjih. Omogočenost oddaljenega namizja, poslušalniki, potrdila, vrata, pravila požarnega zidu, NLA, dovoljenja, seje, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. Ne ustvari ali spremeni poslušalnika, pravila požarnega zidu, dostopa računa, potrdila ali seje. Preverjanje potrjuje le stabilno stanje storitve, ne pa veljavnih pravilnikov, vezave poslušalnika, dostopa skozi požarni zid, identitete potrdil, dovoljenj, preverjanja pristnosti, dosegljivosti omrežja, uspešne povezave ali ustvarjanja sej.

### Oddaljeno upravljanje sistema Windows

Pregleda omejene nastavitve poslušalnikov WinRM, zgodovino delovanja, zgodovino napak točno določene storitve in njeno trenutno pripravljenost. Nepopolni podatki imajo prednost. Onemogočeni poslušalniki ali trenutne težave storitve zahtevajo pregled, pretekli zapisi pa ne morejo odobriti obnovitve ali sprememb nastavitev. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječi poslušalniki WS-Management lahko postanejo dosegljivi ob trenutnih nastavitvah preverjanja pristnosti, potrdil, požarnega zidu, TrustedHosts in pravilnikov. Poslušalniki, naslovi, vrata, potrdila, preverjanje pristnosti, šifriranje, TrustedHosts, pravila požarnega zidu, dovoljenja, kvote, pravilniki, dovoljenja storitve in vrsta zagona se ohranijo. Ne ustvari ali spremeni poslušalnika, pravila požarnega zidu, vnosa zaupanja, potrdila, poverilnice ali oddaljene seje. Preverjanje potrjuje le stabilno stanje storitve, ne pa veljavnih pravilnikov, vezave poslušalnika, dostopa skozi požarni zid, veljavnosti potrdil, preverjanja pristnosti, dovoljenj, dosegljivosti odjemalcev ali izvajanja oddaljenih ukazov.
