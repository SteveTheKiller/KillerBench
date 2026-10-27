# Shramba

Preglejte diske, nosilce, Storage Spaces in storitve vzdrževanja pogonov. Diagnostika ne izbere dejanja z diskom; vsako podprto dejanje, ki spreminja računalnik, potrebuje svoj točen cilj, predpogoje in potrditev.

Navodila po korakih: [Preverjanje in popravilo pogona](check-a-drive.html) in [Sprostitev prostora na disku](free-disk-space.html).

## Orodja v tem razdelku

### Shramba

Preslika omejen nabor identitet diskov, particij in nosilcev s črkami pogonov, oceni prag 10 % prostega prostora na vsakem nosilcu s črko pogona, pregleda dodelitev prostora za senčne kopije VSS ter privzete kvote posameznih nosilcev, povzame trenutni napredek opravil modula Storage, preveri izbrani nosilec NTFS in stanje njegovega sprotnega samopopravljanja ter pregleda diskovne naprave in krmilnike tega računalnika. Pragovi zmogljivosti, nastavitve kvot in stanja opravil so podatki za pregled ter ne izberejo čiščenja, spreminjanja velikosti, sprememb kvot ali upravljanja opravil. Preslikava črk pogonov velja za trenutek pregleda in ne zajema vseh priklopnih poti. Ugotovitve o napravah niso preslikane na izbrani nosilec. Privzet je nosilec sistema Windows. Ne spremeni datotek, posnetkov, opravil, gonilnikov ali nastavitev, ne odklopi nosilcev in ne načrtuje ponovnega zagona.

### Prostori za shranjevanje

Popiše dejanska področja Storage Spaces (brez prvotnega področja primordial) in navidezne diske, vključno z zdravjem, stanjem delovanja, zmogljivostjo, dodelitvijo ter odpornostjo, nato poda eno omejeno operativno ugotovitev. Windows lahko med odgovarjanjem na to poizvedbo samo za branje na zahtevo zažene ponudnika upravljanja Storage Spaces. Področja, navidezni diski, fizični diski, nosilci, odpornost, dodelitev, nastavitve vzdrževanja, pravilniki in vrsta zagona se ohranijo. Ne zahteva dejanj s področji, diski, nosilci, dodelitvijo, priklopom, popravilom, optimizacijo ali nastavitvami. Rezultati ponudnika ne potrjujejo dostopnosti področij, redundance, celovitosti podatkov, možnosti obnovitve varnostnih kopij ali vhodno-izhodnih operacij aplikacij.

### Storitev shrambe

Pregleda omejena trenutna opravila modula Storage, zgodovino napak točno določene storitve in trenutno pripravljenost Storage Service. Nepopolni podatki imajo prednost. Stanje opravil in pretekle napake ne morejo odobriti obnovitve storitve ali sprememb shranjevanja. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječa obvestila o shranjevanju in dejavnost shranjevanja, ki jo upravljajo pravilniki, se lahko nadaljujejo. Nosilci, particije, datotečni sistemi, priklopne točke, naprave, gonilniki, nastavitve Zaznavanja shrambe, pravilniki vzdrževanja, dovoljenja storitve in vrsta zagona se ohranijo. KillerBench ne izbere dejanj z opravili, nosilci, particijami, priklopnimi točkami, datotekami, napravami, gonilniki ali nastavitvami shranjevanja. Preverjanje potrjuje le stabilno stanje storitve, ne pa izida opravil, delovanja naprav, nosilcev ali pravilnikov shranjevanja.

### Storitev optimizacije pogonov

Preveri razpoložljivost storitve Optimize Drives. Izbirna obnovitev zažene le omogočeno, ustavljeno točno določeno storitev po svežem preverjanju odvisnosti. Obstoječa optimizacija, ki jo je načrtoval Windows, se lahko nadaljuje skladno s trenutnimi pravilniki vzdrževanja. Nosilci, datotečni sistemi, podatki, urniki optimizacije, pravilniki vzdrževanja, dovoljenja storitve in vrsta zagona se ohranijo. KillerBench ne izbere nosilca, ne analizira, ne izvede TRIM, ne defragmentira, optimizira ali načrtuje nobenega pogona. Preverjanje potrjuje le stabilno stanje storitve, ne pa analize, optimizacije ali zdravja naprav.
